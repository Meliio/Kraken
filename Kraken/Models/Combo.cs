using System.Text.RegularExpressions;

namespace Kraken.Models
{
    public class Combo
    {
        public string Username { get; } = String.Empty;
        public string Password { get; } = String.Empty;
        public bool IsValid { get; }

        public Combo(string combo)
        {
            var comboSplit = combo.Split(':', 2, StringSplitOptions.RemoveEmptyEntries);

            if (comboSplit.Length == 2 && Regex.IsMatch(comboSplit[0], "(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*|\"(?:[\\x01-\\x08\\x0b\\x0c\\x0e-\\x1f\\x21\\x23-\\x5b\\x5d-\\x7f]|\\\\[\\x01-\\x09\\x0b\\x0c\\x0e-\\x7f])*\")@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\\[(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?|[a-z0-9-]*[a-z0-9]:(?:[\\x01-\\x08\\x0b\\x0c\\x0e-\\x1f\\x21-\\x5a\\x53-\\x7f]|\\\\[\\x01-\\x09\\x0b\\x0c\\x0e-\\x7f])+)\\])"))
            {
                Username = comboSplit[0];
                Password = comboSplit[1];
                IsValid = true;
            }
        }
    }
}
