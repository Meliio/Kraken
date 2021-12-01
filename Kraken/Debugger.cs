using Kraken.Blocks;
using Kraken.Enums;
using Kraken.Models;
using System.Diagnostics;

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
            Console.WriteLine($"INPUT = {_botInput}");

            var httpClient = _httpClientManager.GetRandomHttpClient();

            var botData = new BotData(_botInput, httpClient);

            var stopwatch = new Stopwatch();

            stopwatch.Start();

            foreach (var block in _blocks)
            {
                await block.Debug(botData);

                if (botData.Status is not BotStatus.None and not BotStatus.Success)
                {
                    break;
                }
            }

            stopwatch.Stop();

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"===== DEBUGGER ENDED AFTER {stopwatch.Elapsed.TotalSeconds} SECOND(S) WITH STATUS: {botData.Status.ToString().ToUpper()} =====");
        }
    }
}
