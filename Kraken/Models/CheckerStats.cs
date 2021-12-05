namespace Kraken.Models
{
    public class CheckerStats
    {
        public int WordlistLenght { get; }
        public int Progress { get => _checkPoint + _checked; }
        public int ToCheck => _toCheck;
        public int Success => _success;
        public int Free => _free;
        public int Failure => _failure;
        public int Retry => _retry;
        public int Ban => _ban;
        public int Checked => _checked;
        public int Cpm { get; set; }

        private readonly int _checkPoint;

        private int _toCheck;
        private int _success;
        private int _free;
        private int _failure;
        private int _retry;
        private int _ban;
        private int _checked;

        public CheckerStats(int wordlistLenght, int checkPoint)
        {
            WordlistLenght = wordlistLenght;
            _checkPoint = checkPoint;
        }

        public int IncrementToCheck() => Interlocked.Increment(ref _toCheck);
        public int IncrementSuccess() => Interlocked.Increment(ref _success);
        public int IncrementFree() => Interlocked.Increment(ref _free);
        public int IncrementFailure() => Interlocked.Increment(ref _failure);
        public int IncrementRetry() => Interlocked.Increment(ref _retry);
        public int IncrementBan() => Interlocked.Increment(ref _ban);
        public int IncrementChecked() => Interlocked.Increment(ref _checked);
    }
}
