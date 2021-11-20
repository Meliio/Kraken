namespace Kraken.Models
{
    public class Combo
    {
        public string Username { get; }
        public string Password { get; }
        public bool IsValid { get; }

        public Combo(string combo)
        {
            var comboSplit = combo.Split(':', 2, StringSplitOptions.RemoveEmptyEntries);

            if (comboSplit.Length == 2)
            {
                Username = comboSplit[0];
                Password = comboSplit[1];
                IsValid = true;
            }
        }

        public override string ToString() => string.Join(':', Username, Password);
    }
}
