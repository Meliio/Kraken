using Kraken.Enums;
using Kraken.Models;
using Kraken.Models.Blocks;
using System.Text.RegularExpressions;

namespace Kraken.Blocks
{
    public class BlockKeycheck : Block
    {
        private readonly Keycheck _keycheck;
        private readonly Dictionary<string, Func<string, string, bool>> _keyConditionFunctions;
        private readonly Dictionary<string, Func<IEnumerable<bool>, bool>> _keychainConditionFunctions;

        public BlockKeycheck(Keycheck keycheck)
        {
            _keycheck = keycheck;
            _keyConditionFunctions = new Dictionary<string, Func<string, string, bool>>()
            {
                { "equals", Equals },
                { "contains", Contains },
                { "regex", RegexMatch }
            };
            _keychainConditionFunctions = new Dictionary<string, Func<IEnumerable<bool>, bool>>(StringComparer.OrdinalIgnoreCase)
            {
                { "or", Any },
                { "and", All }
            };
        }

        public override Task Run(BotData botData)
        {
            var success = false;

            foreach (var keychain in _keycheck.Keychains)
            {
                if (_keychainConditionFunctions[keychain.Condition].Invoke(keychain.Keys.Select(k => _keyConditionFunctions[k.Condition].Invoke(ReplaceValues(k.Value, botData), ReplaceValues(k.Source, botData)))))
                {
                    botData.Status = Enum.TryParse<BotStatus>(keychain.Status, true, out var botStatus) ? botStatus : BotStatus.None;
                    success = true;
                }
            }

            if (success)
            {
                return Task.CompletedTask;
            }

            botData.Status = _keycheck.OtherwiseBan ? BotStatus.Ban : BotStatus.ToCheck;

            return Task.CompletedTask;
        }

        private static bool Equals(string value, string part) => value.Equals(part);

        private static bool Contains(string value, string part) => part.Contains(value);

        private static bool RegexMatch(string value, string part) => Regex.IsMatch(part, value);

        private bool Any(IEnumerable<bool> items) => items.Any(i => i);

        private bool All(IEnumerable<bool> items) => items.All(i => i);
    }
}
