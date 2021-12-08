namespace Kraken.Models.Blocks
{
    public class Keycheck
    {
        public IEnumerable<Keychain> Keychains { get; set; } = Array.Empty<Keychain>();
        public bool BanOnToCheck { get; set; } = true;
    }
}
