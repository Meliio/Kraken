using System;

namespace Kraken.Models.Blocks
{
    public class Keycheck
    {
        public Keychain[] Keychains { get; set; } = Array.Empty<Keychain>();
        public bool OtherwiseBan { get; set; } = true;
    }
}
