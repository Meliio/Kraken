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
            _regex = new Regex(@"<([^ ].+?)(?:\[([^ ].+?)\])?>", RegexOptions.Compiled);
            _replaceFunctions = new Dictionary<string, Func<string, Match, BotData, string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "input", ReplaceWithInput },
                { "input.user", ReplaceWithInputUsername },
                { "input.pass", ReplaceWithInputPassword },
                { "input.username", ReplaceWithInputUsername },
                { "input.password", ReplaceWithInputPassword },
                { "data.headers", ReplaceWithResponseHeaders },
                { "data.cookies", ReplaceWithResponseCookies },
                { "data.header", ReplaceWithResponseHeaderValue },
                { "data.cookie", ReplaceWithResponseCookieValue }
            };
        }

        public abstract Task Run(BotData botData);

        public abstract Task Debug(BotData botData);

        protected string ReplaceValues(string input, BotData botData)
        {
            foreach (Match match in _regex.Matches(input))
            {
                input = _replaceFunctions.ContainsKey(match.Groups[1].Value) ? _replaceFunctions[match.Groups[1].Value].Invoke(input, match, botData) : ReplaceWithVariableValue(input, match, botData);
            }

            return input;
        }

        private string ReplaceWithInput(string input, Match match, BotData botData) => input.Replace(match.Value, botData.Input.ToString());

        private string ReplaceWithInputUsername(string input, Match match, BotData botData) => input.Replace(match.Value, botData.Input.Combo.Username);

        private string ReplaceWithInputPassword(string input, Match match, BotData botData) => input.Replace(match.Value, botData.Input.Combo.Password);

        private string ReplaceWithResponseHeaders(string input, Match match, BotData botData) => input.Replace(match.Value, string.Join(Environment.NewLine, botData.Headers.Select(h => $"{h.Key}: {string.Join(' ', h.Value)}")));

        private string ReplaceWithResponseCookies(string input, Match match, BotData botData) => input.Replace(match.Value, string.Join(Environment.NewLine, botData.CookieContainer.GetAllCookies()));

        private string ReplaceWithResponseHeaderValue(string input, Match match, BotData botData)
        {
            var header = botData.Headers.SingleOrDefault(h => h.Key.Equals(match.Groups[2].Value, StringComparison.OrdinalIgnoreCase));

            return header.Key is null ? input : input.Replace(match.Value, string.Join(' ', header.Value));
        }

        private string ReplaceWithResponseCookieValue(string input, Match match, BotData botData)
        {
            var cookie = botData.CookieContainer.GetAllCookies().SingleOrDefault(c => c.Name.Equals(match.Groups[2].Value, StringComparison.OrdinalIgnoreCase));

            return cookie is null ? input : input.Replace(match.Value, cookie.Value);
        }

        private static string ReplaceWithVariableValue(string input, Match match, BotData botData) => botData.Variables.TryGetValue(match.Groups[1].Value, out var value) ? input.Replace(match.Value, value) : input;
    }
}