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

        public BlockKeycheck(Keycheck keycheck)
        {
            _keycheck = keycheck;
            _keyConditionFunctions = new Dictionary<string, Func<string, string, bool>>(StringComparer.OrdinalIgnoreCase)
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
                var results = keychain.Keys.Select(k => _keyConditionFunctions[k.Condition].Invoke(ReplaceValues(k.Value, botData), ReplaceValues(k.Source, botData)));

                if (keychain.Condition.Equals("and", StringComparison.OrdinalIgnoreCase) ? results.All(r => r) : results.Any(r => r))
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

        public override Task Debug(BotData botData)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("[--- Executing Block KEY CHECK ---]");

            var success = false;

            foreach (var keychain in _keycheck.Keychains)
            {
                var results = new List<(bool result, string value, string condition, string source)>();

                foreach (var key in keychain.Keys)
                {
                    var value = ReplaceValues(key.Value, botData);
                    var source = ReplaceValues(key.Source, botData);

                    var result = _keyConditionFunctions[key.Condition].Invoke(value, source);

                    results.Add((result, value, key.Condition, source));
                }

                if (keychain.Condition.Equals("and", StringComparison.OrdinalIgnoreCase) ? results.All(r => r.result) : results.Any(r => r.result))
                {
                    botData.Status = Enum.Parse<BotStatus>(keychain.Status, true);
                    success = true;

                    foreach (var result in results.Where(r => r.result))
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine($"Found '{keychain.Condition.ToUpper()}' Key {(result.source.Length < 30 ? result.source.Replace("\n", string.Empty) : $"{result.source[..30].Replace("\n", string.Empty).TrimEnd()} [...]")} {result.condition} \"{result.value}\"");
                    }
                }
            }

            Console.WriteLine();

            if (success)
            {
                return Task.CompletedTask;
            }

            botData.Status = _keycheck.BanOnToCheck ? BotStatus.Ban : BotStatus.ToCheck;

            return Task.CompletedTask;
        }

        private static bool LessThan(string value, string part) => int.Parse(part) < int.Parse(value);

        private static bool GreaterThan(string value, string part) => int.Parse(part) > int.Parse(value);

        private static bool EqualTo(string value, string part) => part.Equals(value);

        private static bool NotEqualTo(string value, string part) => !part.Equals(value);

        private static bool Contains(string value, string part) => part.Contains(value);

        private static bool DoesNotContain(string value, string part) => !part.Contains(value);

        private static bool MatchesRegex(string value, string part) => Regex.IsMatch(part, value);

        private static bool DoesNotMatchRegex(string value, string part) => !Regex.IsMatch(part, value);
    }
}
