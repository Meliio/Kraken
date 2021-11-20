using Newtonsoft.Json;

namespace Kraken.Models.Blocks
{
    public class Key
    {
        public string Source { get; set; } = String.Empty;
        public string Condition { get; set; } = String.Empty;
        [JsonProperty("key")]
        public string Value { get; set; } = String.Empty;
    }
}
