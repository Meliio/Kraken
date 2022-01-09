using Kraken.Models;
using Newtonsoft.Json;
using Spectre.Console;
using System.Text.RegularExpressions;
using Yove.Proxy;

namespace Kraken
{
    public class CheckerBuilder
    {
        private readonly string _configFile;
        private readonly string _wordlistFile;
        private readonly IEnumerable<string> _proxies;
        private readonly int _skip;
        private readonly int _bots;
        private readonly bool _verbose;
        private readonly Dictionary<string, Func<BotInput, InputRule, bool>> _checkInputRuleFunctions;

        public CheckerBuilder(string configFile, string wordlistFile, IEnumerable<string> proxies, int skip, int bots, bool verbose)
        {
            _configFile = configFile;
            _wordlistFile = wordlistFile;
            _proxies = proxies;
            _skip = skip;
            _bots = bots;
            _verbose = verbose;
            _checkInputRuleFunctions = new Dictionary<string, Func<BotInput, InputRule, bool>>(StringComparer.OrdinalIgnoreCase)
            {
                { "input", CheckInputRule },
                { "input.user", CheckInputUsernameRule },
                { "input.pass", CheckInputPasswordRule },
                { "input.username", CheckInputUsernameRule },
                { "input.password", CheckInputPasswordRule }
            };
        }

        public Checker Build()
        {
            var loliScriptManager = new LoliScriptManager();

            (var configSettings, var blocks) = loliScriptManager.Build(_configFile);
;
            if (string.IsNullOrEmpty(configSettings.Name))
            {
                configSettings.Name = Path.GetFileNameWithoutExtension(_configFile);
            }

            if (configSettings.CustomInputs.Any())
            {
                AnsiConsole.Write(new Rule("[darkorange]Custom input[/]").RuleStyle("grey").LeftAligned());

                foreach (var customInput in configSettings.CustomInputs)
                {
                    customInput.Value = AnsiConsole.Ask<string>($"{customInput.Description}:");
                }
            }

            var botInputs = File.ReadAllLines(_wordlistFile).Where(w => !string.IsNullOrEmpty(w)).Select(w => new BotInput(w));

            if (configSettings.InputRules.Any())
            {
                botInputs = botInputs.Where(b => configSettings.InputRules.All(i => _checkInputRuleFunctions.ContainsKey(i.Name) ? _checkInputRuleFunctions[i.Name].Invoke(b, i) : false));
            }

            var proxies = _proxies.Any() ? File.ReadAllLines(_proxies.First()).Where(p => !string.IsNullOrEmpty(p)) : Array.Empty<string>();
            
            var proxyType = _proxies.Count() == 2 ? Enum.Parse<ProxyType>(proxies.ElementAt(1), true) : ProxyType.Http;

            var httpClientManager = proxies.Any() ? new HttpClientManager(proxies, proxyType) : new HttpClientManager();

            var parallelOptions = new ParallelOptions()
            {
                MaxDegreeOfParallelism = _bots
            };

            var krakenSettings = JsonConvert.DeserializeObject<KrakenSettings>(File.ReadAllText("settings.json"));

            var record = GetRecord(configSettings.Name);

            Directory.CreateDirectory(Path.Combine(krakenSettings.OutputDirectory, configSettings.Name));

            return new Checker(configSettings, blocks, botInputs, httpClientManager, _skip, parallelOptions, _verbose, krakenSettings, record);
        }

        private bool CheckInputRule(BotInput botInput, InputRule inputRule) => Regex.IsMatch(botInput.ToString(), inputRule.Regex);

        private bool CheckInputUsernameRule(BotInput botInput, InputRule inputRule) => Regex.IsMatch(botInput.Combo.Username, inputRule.Regex);

        private bool CheckInputPasswordRule(BotInput botInput, InputRule inputRule) => Regex.IsMatch(botInput.Combo.Password, inputRule.Regex);

        private Record GetRecord(string configName)
        {
            using var database = new LiteDB.LiteDatabase("Kraken.db");

            var collection = database.GetCollection<Record>("records");

            var record = collection.FindOne(r => r.ConfigName == configName && r.WordlistLocation == _wordlistFile);

            if (record is null)
            {
                record = new Record()
                {
                    ConfigName = configName,
                    WordlistLocation = _wordlistFile,
                    Progress = 0
                };

                collection.Insert(record);
            }

            return record;
        }
    }
}