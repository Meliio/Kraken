using Kraken.Blocks;
using Kraken.Enums;
using Kraken.Models;
using Spectre.Console;
using System.Diagnostics;
using System.Text;

namespace Kraken
{
    public  class Debugger
    {
        private readonly IEnumerable<Block> _blocks;
        private readonly BotInput _botInput;
        private readonly HttpClientManager _httpClientManager;

        public Debugger(IEnumerable<Block> blocks, BotInput botInputs, HttpClientManager httpClientManager)
        {
            _blocks = blocks;
            _botInput = botInputs;
            _httpClientManager = httpClientManager;
        }

        public async Task StartAsync()
        {
            var httpClient = _httpClientManager.GetRandomHttpClient();

            var botData = new BotData(_botInput, httpClient);

            var stringBuilder = new StringBuilder();

            var stopwatch = new Stopwatch();

            stopwatch.Start();

            foreach (var block in _blocks)
            {
                await block.Debug(botData, stringBuilder);

                if (botData.Status is not BotStatus.None and not BotStatus.Success)
                {
                    break;
                }
            }

            stopwatch.Stop();

            stringBuilder.AppendLine($"===== DEBUGGER ENDED AFTER {stopwatch.Elapsed.TotalSeconds} SECOND(S) WITH STATUS: {botData.Status.ToString().ToUpper()} =====");

            AnsiConsole.MarkupLine(stringBuilder.ToString());
        }
    }
}
