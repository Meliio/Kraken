﻿namespace Kraken.Models.Blocks
{
    public class Keychain
    {
        public string Status { get; set; } = String.Empty;
        public string Condition { get; set; } = "or";
        public Key[] Keys { get; set; } = Array.Empty<Key>();
    }
}
