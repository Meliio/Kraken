using Kraken.Blocks;
using Kraken.Enums;
using Kraken.Models;
using Spectre.Console;

namespace Kraken
{
    public class Debugger
    {
        private readonly IEnumerable<Block> _blocks;
        private readonly BotInput _botInput;
        private readonly CustomHttpClient _httpClient;

        public Debugger(IEnumerable<Block> blocks, BotInput botInputs, CustomHttpClient httpClient)
        {
            _blocks = blocks;
            _botInput = botInputs;
            _httpClient = httpClient;
        }

        public async Task StartAsync()
        {
            var botData = new BotData(_botInput, _httpClient);

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