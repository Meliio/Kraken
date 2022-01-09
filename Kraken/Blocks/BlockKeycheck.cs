using Kraken.Enums;
using Kraken.Models;
using Kraken.Models.Blocks;
using System.Text.RegularExpressions;

namespace Kraken.Blocks
{
    public class BlockKeycheck : Block
    {
        private readonly Keycheck _keycheck;
        private readonly Dictionary<string, Func<string, string, BotData, bool>> _keyConditionFunctions;

        public BlockKeycheck(Keycheck keycheck)
        {
            _keycheck = keycheck;
            _keyConditionFunctions = new Dictionary<string, Func<string, string, BotData, bool>>(StringComparer.OrdinalIgnoreCase)
            {
                { "lessThan", LessThan },
                { "greaterThan", GreaterThan },
                { "equals", EqualTo },
                { "notEqualTo", NotEqualTo },
                { "contains", Contains },
                { "doesNotContain", DoesNotContain },
                { "matchesRegex", MatchesRegex },
                { "doesNotMatchRegex", DoesNotMatchRegex }
            };
        }

        public override Task Run(BotData botData)
        {
            var success = false;

            foreach (var keychain in _keycheck.Keychains)
            {
                var results = keychain.Keys.Select(k => _keyConditionFunctions[k.Condition].Invoke(ReplaceValues(k.Value, botData), ReplaceValues(k.Source, botData), botData));

                if (keychain.Condition.Equals("AND", StringComparison.OrdinalIgnoreCase) ? results.All(r => r) : results.Any(r => r))
                {
                    botData.Status = Enum.Parse<BotStatus>(keychain.Status, true);
                    success = true;
                }
            }

            if (success)
            {
                return Task.CompletedTask;
            }

            botData.Status = _keycheck.BanOnToCheck ? BotStatus.Ban : BotStatus.ToCheck;

            return Task.CompletedTask;
        }

        private static bool LessThan(string value, string part, BotData botData) => int.Parse(part) < int.Parse(value);

        private static bool GreaterThan(string value, string part, BotData botData) => int.Parse(part) > int.Parse(value);

        private static bool EqualTo(string value, string part, BotData botData) => part.Equals(value);

        private static bool NotEqualTo(string value, string part, BotData botData) => !part.Equals(value);

        private static bool Contains(string value, string part, BotData botData) => part.Contains(value);

        private static bool DoesNotContain(string value, string part, BotData botData) => !part.Contains(value);

        private static bool MatchesRegex(string value, string part, BotData botData) => Regex.IsMatch(part, value);

        private static bool DoesNotMatchRegex(string value, string part, BotData botData) => !Regex.IsMatch(part, value);
    }
}
