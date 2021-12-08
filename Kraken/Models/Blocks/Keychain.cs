﻿namespace Kraken.Models.Blocks
{
    public class Keychain
    {
        public string Status { get; set; } = string.Empty;
        public string Condition { get; set; } = "or";
        public IEnumerable<Key> Keys { get; set; } = Array.Empty<Key>();
    }
}
