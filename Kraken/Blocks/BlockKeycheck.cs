﻿using Kraken.Enums;
using Kraken.Models;
using Kraken.Models.Blocks;
using System.Text.RegularExpressions;

namespace Kraken.Blocks
{
    public class BlockKeycheck : Block
    {
        private readonly Keycheck _keycheck;
        private readonly Dictionary<string, Func<IEnumerable<bool>, bool>> _keychainConditionFunctions;
        private readonly Dictionary<string, Func<string, string, bool>> _keyConditionFunctions;

        public BlockKeycheck(Keycheck keycheck)
        {
            _keycheck = keycheck;
            _keychainConditionFunctions = new Dictionary<string, Func<IEnumerable<bool>, bool>>(StringComparer.OrdinalIgnoreCase)
            {
                { "or", Any },
                { "and", All }
            };
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
                if (_keychainConditionFunctions[keychain.Condition].Invoke(keychain.Keys.Select(k => _keyConditionFunctions[k.Condition].Invoke(ReplaceValues(k.Value, botData), ReplaceValues(k.Source, botData)))))
                {
                    if (Enum.TryParse<BotStatus>(keychain.Status, true, out var botStatus))
                    {
                        botData.Status = botStatus;
                        success = true;
                    }
                }
            }

            if (success)
            {
                return Task.CompletedTask;
            }

            botData.Status = _keycheck.OtherwiseBan ? BotStatus.Ban : BotStatus.ToCheck;

            return Task.CompletedTask;
        }

        public override Task Debug(BotData botData)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(Environment.NewLine + "<--- Executing Block KEY CHECK --->");

            var success = false;

            foreach (var keychain in _keycheck.Keychains)
            {
                var results = new List<(bool, string, string, string)>();

                foreach (var key in keychain.Keys)
                {
                    var value = ReplaceValues(key.Value, botData);
                    var condition = key.Condition;
                    var source = ReplaceValues(key.Source, botData);

                    var result = _keyConditionFunctions[key.Condition].Invoke(value, source);

                    results.Add((result, value, condition, source));
                }

                if (_keychainConditionFunctions[keychain.Condition].Invoke(results.Select(r => r.Item1)))
                {
                    if (Enum.TryParse<BotStatus>(keychain.Status, true, out var botStatus))
                    {
                        botData.Status = botStatus;
                        success = true;

                        Console.ForegroundColor = ConsoleColor.White;

                        foreach (var result in results.Where(r => r.Item1))
                        {
                            Console.WriteLine($"Found '{keychain.Condition.ToUpper()}' Key ${(result.Item4.Length < 20 ? result.Item4 : result.Item4[..20])} [...] {char.ToUpper(result.Item3[0]) + result.Item3[1..]} {result.Item2}");
                        }
                    }
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

        private static bool DoesNotEqual(string value, string part) => !value.Equals(part);

        private static bool Contains(string value, string part) => part.Contains(value);

        private static bool DoesNotContain(string value, string part) => !part.Contains(value);

        private static bool RegexMatch(string value, string part) => Regex.IsMatch(part, value);

        private bool Any(IEnumerable<bool> items) => items.Any(i => i);

        private bool All(IEnumerable<bool> items) => items.All(i => i);
    }
}
