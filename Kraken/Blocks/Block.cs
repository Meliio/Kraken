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
                { "input", ReplaceWithInput },
                { "input.user", ReplaceWithInputUsername },
                { "input.pass", ReplaceWithInputPassword },
                { "input.username", ReplaceWithInputUsername },
                { "input.password", ReplaceWithInputPassword }
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

        private string ReplaceWithInput(string input, Match match, BotData botData) => input.Replace(match.Value, botData.Input.ToString());

        private string ReplaceWithInputUsername(string input, Match match, BotData botData) => botData.Input.Combo.IsValid ? input.Replace(match.Value, botData.Input.Combo.Username) : input;

        private string ReplaceWithInputPassword(string input, Match match, BotData botData) => botData.Input.Combo.IsValid ? input.Replace(match.Value, botData.Input.Combo.Password) : input;

        private static string ReplaceWithVariableValue(string input, Match match, BotData botData) => botData.Variables.TryGetValue(match.Groups[1].Value, out var value) ? input.Replace(match.Value, value) : input;
    }
}