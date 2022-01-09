namespace Kraken.Models.Blocks
{
    public class Keycheck
    {
        public List<Keychain> Keychains { get; set; } = new List<Keychain>();
        public bool BanOnToCheck { get; set; } = true;
    }
}
