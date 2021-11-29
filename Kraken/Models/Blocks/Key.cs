using Newtonsoft.Json;

namespace Kraken.Models.Blocks
{
    public class Key
    {
        public string Source { get; set; } = "<response.content>";
        public string Condition { get; set; } = "contains";
        [JsonProperty("key")]
        public string Value { get; set; } = String.Empty;
    }
}
