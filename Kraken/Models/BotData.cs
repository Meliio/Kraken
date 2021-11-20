using Kraken.Enums;
using System.Net;

namespace Kraken.Models
{
    public class BotData
    {
        public BotStatus Status { get; set; }
        public Combo Combo { get; }
        public CustomHttpClient HttpClient { get; }
        public CookieContainer CookieContainer { get; }
        public Dictionary<string, string> Variables { get; }
        public Dictionary<string, string> Captures { get; }

        public BotData(Combo combo, CustomHttpClient customHttpClient)
        {
            Status = BotStatus.None;
            Combo = combo;
            HttpClient = customHttpClient;
            CookieContainer = new CookieContainer();
            Variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Captures = new Dictionary<string, string>();
        }
    }
}