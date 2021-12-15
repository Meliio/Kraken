using Kraken.Blocks;
using Kraken.Enums;
using Kraken.Models;
using Spectre.Console;

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

            foreach (var block in _blocks)
            {
                try
                {
                    await block.Debug(botData);
                }
                catch (Exception error)
                {
                    AnsiConsole.WriteException(error);
                    botData.Status = BotStatus.Error;
                }

                if (botData.Status is not BotStatus.None and not BotStatus.Success)
                {
                    break;
                }
            }

            Console.WriteLine($"===== DEBUGGER ENDED WITH STATUS: {botData.Status.ToString().ToUpper()} =====");

            Console.ResetColor();
        }
    }
}
