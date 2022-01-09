using Kraken.Blocks;
using Kraken.Enums;
using Kraken.Models;
using LiteDB;
using Spectre.Console;
using System.Runtime.InteropServices;
using System.Text;

namespace Kraken
{
    public class Checker
    {
        public CheckerStatus Status { get; set; }
        public CheckerStats Stats { get; }

        private readonly ConfigSettings _configSettings;
        private readonly IEnumerable<Block> _blocks;
        private readonly IEnumerable<BotInput> _botInputs;
        private readonly HttpClientManager _httpClientManager;
        private readonly int _skip;
        private readonly ParallelOptions _parallelOptions;
        private readonly bool _verbose;
        private readonly KrakenSettings _krakenSettings;
        private readonly Record _record;
        private readonly IEnumerable<BotStatus> _validStatuses;
        private readonly ReaderWriterLock _readerWriterLock;
        private readonly List<CheckerOutput> _outputs;

        public Checker(ConfigSettings configSettings, IEnumerable<Block> blocks, IEnumerable<BotInput> botInputs, HttpClientManager httpClientManager, int skip, ParallelOptions parallelOptions, bool verbose, KrakenSettings krakenSettings, Record record)
        {
            Status = CheckerStatus.Idle;
            Stats = new CheckerStats(botInputs.Count(), record.Progress);
            _configSettings = configSettings;
            _blocks = blocks;
            _botInputs = botInputs;
            _httpClientManager = httpClientManager;
            _skip = skip;
            _parallelOptions = parallelOptions;
            _verbose = verbose;
            _krakenSettings = krakenSettings;
            _record = record;
            _validStatuses = new BotStatus[] { BotStatus.None, BotStatus.Success, BotStatus.Free };
            _readerWriterLock = new ReaderWriterLock();
            _outputs = new List<CheckerOutput>();
        }

        public async Task StartAsync()
        {
            AnsiConsole.MarkupLine($"[grey]LOG:[/] started at {DateTime.Now}");

            _ = StartUpdatingConsoleCheckerStatsAsync();

            _ = StartCpmCalculator();

            _ = StartUpdatingRecordAsync();

            Status = CheckerStatus.Running;

            await AnsiConsole.Status().Spinner(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? Spinner.Known.Point : Spinner.Known.Default).StartAsync("Progress", async ctx =>
            {
                await Parallel.ForEachAsync(_botInputs.Skip(_skip == 0 ? _record.Progress : _skip), _parallelOptions, async (input, _) =>
                {
                    while (Status == CheckerStatus.Paused)
                    {
                        await Task.Delay(1000, _);
                    }

                    BotData botData = null;

                    for (var attempts = 0; attempts < 8; attempts++)
                    {
                        var httpClient = _httpClientManager.GetRandomHttpClient();

                        botData = new BotData(input, httpClient);

                        foreach (var customInput in _configSettings.CustomInputs)
                        {
                            botData.Variables.Add(customInput.Name, customInput.Value);
                        }

                        foreach (var block in _blocks)
                        {
                            try
                            {
                                await block.Run(botData);
                            }
                            catch (HttpRequestException)
                            {
                                httpClient.IsValid = false;
                                botData.Status = BotStatus.Retry;
                            }
                            catch (Exception error)
                            {
                                if (error.Message.Contains("HttpClient.Timeout"))
                                {
                                    httpClient.IsValid = false;
                                    botData.Status = BotStatus.Retry;
                                }
                                else
                                {
                                    if (_verbose)
                                    {
                                        AnsiConsole.WriteException(error);
                                    }
                                    botData.Status = BotStatus.Error;
                                }
                            }

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
                        else if (botData.Status == BotStatus.Error)
                        {
                            Stats.IncrementError();
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
                                    AnsiConsole.MarkupLine($"NONE: {output}");
                                    Stats.IncrementSuccess();
                                    break;
                                case BotStatus.Success:
                                    AnsiConsole.MarkupLine($"[green4]SUCCESS:[/] {output}");
                                    Stats.IncrementSuccess();
                                    break;
                                case BotStatus.Free:
                                    AnsiConsole.MarkupLine($"[orange3]FREE:[/] {output}");
                                    Stats.IncrementFree();
                                    break;
                            }
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[cyan3]TOCHECK:[/] {input}");
                            Stats.IncrementToCheck();
                        }
                    }

                    _outputs.Add(new CheckerOutput());

                    Stats.IncrementChecked();
                });
            });

            Status = CheckerStatus.Done;

            AnsiConsole.MarkupLine($"[grey]LOG:[/] completed at {DateTime.Now}");
        }

        private async Task StartUpdatingConsoleCheckerStatsAsync()
        {
            var periodicTimer = new PeriodicTimer(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? TimeSpan.FromMilliseconds(100) : TimeSpan.FromSeconds(10));

            var checkerStats = new StringBuilder();

            while (true)
            {
                checkerStats
                    .Append(Stats.Progress * 100 / Stats.WordlistLenght)
                    .Append("% Success: ")
                    .Append(Stats.Success)
                    .Append(" Free: ")
                    .Append(Stats.Free)
                    .Append(" Failure: ")
                    .Append(Stats.Failure)
                    .Append(" ToCheck: ")
                    .Append(Stats.ToCheck)
                    .Append(" Retry: ")
                    .Append(Stats.Retry)
                    .Append(" Ban: ")
                    .Append(Stats.Ban)
                    .Append(" Error: ")
                    .Append(Stats.Error)
                    .Append(" | CPM ")
                    .Append(Stats.Cpm);

                Console.Title = checkerStats.ToString();

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    AnsiConsole.MarkupLine($"[grey]LOG:[/] {checkerStats}");
                }

                checkerStats.Clear();

                await periodicTimer.WaitForNextTickAsync();
            }
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
            var periodicTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));

            using var database = new LiteDatabase("Kraken.db");

            var collection = database.GetCollection<Record>("records");

            while (true)
            {
                _record.Progress = Stats.Progress;

                collection.Update(_record);

                await periodicTimer.WaitForNextTickAsync();
            }
        }

        private async Task StartCpmCalculator()
        {
            var periodicTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));

            while (true)
            {
                var cpm = 0;

                var outputs = _outputs.OrderByDescending(o => o.DateTime);

                foreach (var output in outputs)
                {
                    if ((DateTime.Now - output.DateTime).TotalSeconds > 60)
                    {
                        break;
                    }

                    cpm++;
                }

                Stats.Cpm = cpm;

                await periodicTimer.WaitForNextTickAsync();
            }
        }
    }
}
