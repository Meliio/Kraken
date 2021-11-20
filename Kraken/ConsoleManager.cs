using System.Text;

namespace Kraken
{
    public class ConsoleManager
    {
        private readonly Checker _checker;

        public ConsoleManager(Checker checker)
        {
            _checker = checker;
        }

        public async Task StartUpdatingConsoleCheckerStatsAsync()
        {
            var checkerStats = new StringBuilder();

            while (true)
            {
                checkerStats
                    .Append("[STATS] ")
                    .Append(_checker.Stats.Progress * 100 / _checker.Stats.WordListLenght)
                    .Append("% - Success: ")
                    .Append(_checker.Stats.Success)
                    .Append(" Free: ")
                    .Append(_checker.Stats.Free)
                    .Append(" Failure: ")
                    .Append(_checker.Stats.Failure)
                    .Append(" ToCheck: ")
                    .Append(_checker.Stats.ToCheck)
                    .Append(" Retry: ")
                    .Append(_checker.Stats.Retry)
                    .Append(" Ban: ")
                    .Append(_checker.Stats.Ban)
                    .Append(" [ CPM ")
                    .Append(_checker.Stats.Cpm)
                    .Append(" ]");

                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(checkerStats);

                checkerStats.Clear();

                await Task.Delay(10000);
            }
        }
    }
}
