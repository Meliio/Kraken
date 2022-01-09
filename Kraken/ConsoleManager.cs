using Kraken.Enums;

namespace Kraken
{
    public class ConsoleManager
    {
        private readonly Checker _checker;

        public ConsoleManager(Checker checker)
        {
            _checker = checker;
        }

        public async Task StartListeningKeysAsync()
        {
            while (_checker.Status == CheckerStatus.Idle)
            {
                await Task.Delay(100);
            }

            while (_checker.Status != CheckerStatus.Done)
            {
                var key = Console.ReadKey(true).Key;

                switch (key)
                {
                    case ConsoleKey.Spacebar:
                        _checker.Status = _checker.Status == CheckerStatus.Running ? CheckerStatus.Paused : CheckerStatus.Running;
                        break;
                    case ConsoleKey.E:
                        Environment.Exit(0);
                        break;
                }
            }
        }
    }
}
