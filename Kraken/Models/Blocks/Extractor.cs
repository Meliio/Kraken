namespace Kraken.Models.Blocks
{
    public class Extractor
    {
        public string Type { get; set; } = String.Empty;
        public string Name { get; set; } = String.Empty;
        public string Prefix { get; set; } = String.Empty;
        public string Suffix { get; set; } = String.Empty;
        public string Left { get; set; } = String.Empty;
        public string Right { get; set; } = String.Empty;
        public string Json { get; set; } = String.Empty;
        public string Selector { get; set; } = String.Empty;
        public string Attribute { get; set; } = String.Empty;
        public string Regex { get; set; } = String.Empty;
        public string Group { get; set; } = String.Empty;
        public string Source { get; set; } = String.Empty;
        public bool Capture { get; set; } = false;
    }
}
