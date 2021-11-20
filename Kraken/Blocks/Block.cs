using Kraken.Models;
using System.Text.RegularExpressions;

namespace Kraken.Blocks
{
    public abstract class Block
    {
        private readonly Regex _regex;
        private readonly Dictionary<string, Func<string, Match, BotData, string>> _replaceFunctions;

        public Block()
        {
            _regex = new Regex("<([^<>]+)>", RegexOptions.Compiled);
            _replaceFunctions = new Dictionary<string, Func<string, Match, BotData, string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "combo", ReplaceWithCombo },
                { "combo.username", ReplaceWithComboUsername },
                { "combo.password", ReplaceWithComboPassword }
            };
        }

        public abstract Task Run(BotData botData);

        protected string ReplaceValues(string input, BotData botData)
        {
            foreach (Match match in _regex.Matches(input))
            {
                input = _replaceFunctions.ContainsKey(match.Groups[1].Value) ? _replaceFunctions[match.Groups[1].Value].Invoke(input, match, botData) : ReplaceWithVariableValue(input, match, botData);
            }

            return input;
        }
        private string ReplaceWithCombo(string input, Match match, BotData botData) => input.Replace(match.Value, botData.Combo.ToString());

        private string ReplaceWithComboUsername(string input, Match match, BotData botData) => input.Replace(match.Value, botData.Combo.Username);

        private string ReplaceWithComboPassword(string input, Match match, BotData botData) => input.Replace(match.Value, botData.Combo.Password);

        private static string ReplaceWithVariableValue(string input, Match match, BotData botData) => botData.Variables.TryGetValue(match.Groups[1].Value, out var value) ? input.Replace(match.Value, value) : input; 
    }
}