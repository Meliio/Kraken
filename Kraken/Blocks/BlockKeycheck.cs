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
            _keyConditionFunctions = new Dictionary<string, Func<string, string, bool>>()
            {
                { "equals", Equals },
                { "doesNotEqual", DoesNotEqual },
                { "contains", Contains },
                { "doesNotContain", DoesNotContain },
                { "regex", RegexMatch }
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
                var results = new List<(bool, string, string, string)>();

                foreach (var key in keychain.Keys)
                {
                    var value = ReplaceValues(key.Value, botData);
                    var source = ReplaceValues(key.Source, botData);

                    var result = _keyConditionFunctions[key.Condition].Invoke(value, source);

                    results.Add((result, value, key.Condition, source));
                }

                if (keychain.Condition.Equals("and", StringComparison.OrdinalIgnoreCase) ? results.All(r => r.Item1) : results.Any(r => r.Item1))
                {
                    botData.Status = Enum.Parse<BotStatus>(keychain.Status, true);
                    success = true;

                    foreach (var result in results.Where(r => r.Item1))
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine($"Found '{keychain.Condition.ToUpper()}' Key {(result.Item4.Length < 30 ? result.Item4.Replace("\n", string.Empty) : $"{result.Item4[..30].Replace("\n", string.Empty)}[...]")} {result.Item3} {result.Item2}");
                    }
                }
            }

            if (success)
            {
                return Task.CompletedTask;
            }

            botData.Status = _keycheck.BanOnToCheck ? BotStatus.Ban : BotStatus.ToCheck;

            return Task.CompletedTask;
        }

        private static bool Equals(string value, string part) => value.Equals(part);

        private static bool DoesNotEqual(string value, string part) => !value.Equals(part);

        private static bool Contains(string value, string part) => part.Contains(value);

        private static bool DoesNotContain(string value, string part) => !part.Contains(value);

        private static bool RegexMatch(string value, string part) => Regex.IsMatch(part, value);
    }
}
