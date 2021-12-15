using Kraken.Enums;
using System.Net;

namespace Kraken.Models
{
    public class BotData
    {
        public BotStatus Status { get; set; }
        public BotInput Input { get; }
        public CustomHttpClient HttpClient { get; }
        public IEnumerable<KeyValuePair<string, IEnumerable<string>>> Headers { get; set; }
        public CookieContainer CookieContainer { get; }
        public Dictionary<string, string> Variables { get; }
        public Dictionary<string, string> Captures { get; }

        public BotData(BotInput input, CustomHttpClient httpClient)
        {
            Status = BotStatus.None;
            Input = input;
            HttpClient = httpClient;
            Headers = Array.Empty<KeyValuePair<string, IEnumerable<string>>>();
            CookieContainer = new CookieContainer();
            Variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Captures = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}