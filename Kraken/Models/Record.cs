namespace Kraken.Models
{
    public class Record
    {
        public int Id { get; set; }
        public string ConfigName { get; set; } = string.Empty;
        public string WordlistLocation { get; set; } = string.Empty;
        public int Progress { get; set; }
    }
}
