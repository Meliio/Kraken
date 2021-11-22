using System;

namespace Kraken.Models
{
    public class CheckerOutput
    {
        public DateTime DateTime { get => new DateTime(1970, 1, 1).AddSeconds(_unixDate); }

        private readonly int _unixDate;

        public CheckerOutput()
        {
            _unixDate = (int)Math.Round((DateTime.Now - new DateTime(1970, 1, 1)).TotalSeconds);
        }
    }
}
