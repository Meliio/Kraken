using Kraken.Blocks;
using Kraken.Enums;
using Kraken.Models;
using LiteDB;
using System.Text;

namespace Kraken
{
    public class Checker
    {
        public CheckerStats Stats { get; }

        private readonly ConfigSettings _configSettings;
        private readonly IEnumerable<Block> _blocks;
        private readonly IEnumerable<BotInput> _botInputs;
        private readonly HttpClientManager _httpClientManager;
        private readonly int _bots;
        private readonly KrakenSettings _krakenSettings;
        private readonly Record _record;
        private readonly IEnumerable<BotStatus> _validStatuses;
        private readonly ReaderWriterLock _readerWriterLock;
        private readonly List<CheckerOutput> _outputs;

        public Checker(ConfigSettings configSettings, IEnumerable<Block> blocks, IEnumerable<BotInput> botInputs, HttpClientManager httpClientManager, int bots, KrakenSettings krakenSettings, Record record)
        {
            Stats = new CheckerStats(botInputs.Count(), record.Progress);
            _configSettings = configSettings;
            _blocks = blocks;
            _botInputs = botInputs;
            _httpClientManager = httpClientManager;
            _bots = bots;
            _krakenSettings = krakenSettings;
            _record = record;
            _validStatuses = new BotStatus[] { BotStatus.None, BotStatus.Success, BotStatus.Free };
            _readerWriterLock = new ReaderWriterLock();
            _outputs = new List<CheckerOutput>();
        }

        public async Task StartAsync()
        {
            _ = StartCpmCalculator();

            _ = StartUpdatingRecordAsync();

            await Parallel.ForEachAsync(_botInputs.Skip(_record.Progress), new ParallelOptions() { MaxDegreeOfParallelism = _bots }, async (input, _) =>
            {
                BotData botData = null;

                for (var attempts = 0; attempts < 10; attempts++)
                {
                    var httpClient = _httpClientManager.GetRandomHttpClient();

                    botData = new BotData(input, httpClient);

                    foreach (var block in _blocks)
                    {
                        await block.Run(botData);

                        if (botData.Status is not BotStatus.None and not BotStatus.Success)
                        {
                            break;
                        }
                    }

                    if (botData.Status == BotStatus.Retry)
                    {
                        Stats.IncrementRetry();
                    }
                    else if (botData.Status == BotStatus.Ban)
                    {
                        httpClient.IsValid = false;
                        Stats.IncrementBan();
                    }
                    else
                    {
                        break;
                    }
                }

                if (botData.Status == BotStatus.Failure)
                {
                    Stats.IncrementFailure();
                }
                else
                {
                    if (_validStatuses.Contains(botData.Status))
                    {
                        var outputPath = Path.Combine(_krakenSettings.OutputDirectory, _configSettings.Name, $"{botData.Status.ToString().ToLower()}.txt");
                        var output = OutputBuilder(botData);

                        await AppendOutputToFileAsync(outputPath, output);

                        switch (botData.Status)
                        {
                            case BotStatus.None:
                                Console.ForegroundColor = ConsoleColor.White;
                                Console.WriteLine($"[NONE] {output}");
                                Stats.IncrementSuccess();
                                break;
                            case BotStatus.Success:
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"[SUCCESS] {output}");
                                Stats.IncrementSuccess();
                                break;
                            case BotStatus.Free:
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine($"[FREE] {output}");
                                Stats.IncrementFree();
                                break;
                        }
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                        Console.WriteLine($"[TOCHECK] {input}");
                        Stats.IncrementToCheck();
                    }
                }

                _outputs.Add(new CheckerOutput());

                Stats.IncrementChecked();
            });
        }

        private string OutputBuilder(BotData botData) => botData.Captures.Any() ? new StringBuilder().Append(botData.Input.ToString()).Append(_krakenSettings.OutputSeparator).AppendJoin(_krakenSettings.OutputSeparator, botData.Captures.Select(c => $"{c.Key} = {c.Value}")).ToString() : botData.Input.ToString();

        private async Task AppendOutputToFileAsync(string path, string content)
        {
            try
            {
                _readerWriterLock.AcquireWriterLock(int.MaxValue);
                using var streamWriter = File.AppendText(path);
                await streamWriter.WriteLineAsync(content);
            }
            finally
            {
                _readerWriterLock.ReleaseWriterLock();
            }
        }

        private async Task StartUpdatingRecordAsync()
        {
            using var database = new LiteDatabase("Kraken.db");

            var collection = database.GetCollection<Record>("records");

            while (true)
            {
                _record.Progress = Stats.Progress;

                collection.Update(_record);

                await Task.Delay(100);
            }
        }

        private async Task StartCpmCalculator()
        {
            while (true)
            {
                var cpm = 0;

                foreach (var output in _outputs.OrderByDescending(x => x.DateTime))
                {
                    if ((DateTime.Now - output.DateTime).TotalSeconds > 60)
                    {
                        break;
                    }

                    cpm++;
                }

                Stats.Cpm = cpm;

                await Task.Delay(100);
            }
        }
    }
}
