using Kraken.Models;
using System.Text.RegularExpressions;

namespace Kraken.Blocks
{
    public abstract class Block
    {
        private readonly Regex _regex;
        private readonly Dictionary<string, Func<string, string, BotData, string>> _replaceFunctions;

        public Block()
        {
            _regex = new Regex("<([^<>]+)>", RegexOptions.Compiled);
            _replaceFunctions = new Dictionary<string, Func<string, string, BotData, string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "input", ReplaceWithInput },
                { "input.user", ReplaceWithInputUsername },
                { "input.pass", ReplaceWithInputPassword },
                { "input.username", ReplaceWithInputUsername },
                { "input.password", ReplaceWithInputPassword },
                { "response.cookies", ReplaceWithResponseCookies }
            };
        }

        public abstract Task Run(BotData botData);

        public abstract Task Debug(BotData botData);

        protected string ReplaceValues(string input, BotData botData)
        {
            foreach (Match match in _regex.Matches(input))
            {
                input = _replaceFunctions.ContainsKey(match.Groups[1].Value) ? _replaceFunctions[match.Groups[1].Value].Invoke(input, match.Value, botData) : ReplaceWithVariableValue(input, match, botData);
            }

            return input;
        }

        private string ReplaceWithInput(string input, string match, BotData botData) => input.Replace(match, botData.Input.ToString());

        private string ReplaceWithInputUsername(string input, string match, BotData botData) => input.Replace(match, botData.Input.Combo.Username);

        private string ReplaceWithInputPassword(string input, string match, BotData botData) => input.Replace(match, botData.Input.Combo.Password);

        private string ReplaceWithResponseCookies(string input, string match, BotData botData) => input.Replace(match, string.Join(Environment.NewLine, botData.CookieContainer.GetAllCookies()));

        private static string ReplaceWithVariableValue(string input, Match match, BotData botData) => botData.Variables.TryGetValue(match.Groups[1].Value, out var value) ? input.Replace(match.Value, value) : input;
    }
}