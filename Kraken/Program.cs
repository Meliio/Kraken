using CommandLine;
using Kraken.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Kraken
{
    [Verb("run")]
    public class RunOptions
    {
        [Option('c', "config", Required = true)]
        public string ConfigFile { get; set; }

        [Option('w', "wordlist", Required = true)]
        public string WordlistFile { get; set; }

        [Option('p', "proxies")]
        public IEnumerable<string> ProxiesFile { get; set; }

        [Option('s', "skip")]
        public int Skip { get; set; }

        [Option('b', "bots", Default = 1)]
        public int Bots { get; set; }
    }

    [Verb("debug")]
    public class DebugOptions
    {
        [Option('c', "config", Required = true)]
        public string ConfigFile { get; set; }

        [Option('i', "input", Required = true)]
        public string BotInput { get; set; }

        [Option('p', "proxy")]
        public IEnumerable<string> Proxy { get; set; }
    }

    public class Program
    {
        private const string settingsFile = "settings.json";

        public static async Task Main(string[] args)
        {
            await GenerateSettingsFile();

            await Parser.Default.ParseArguments<RunOptions, DebugOptions>(args).MapResult(
                (RunOptions options) => Run(options),
                (DebugOptions options) => Debug(options),
                errors => Task.FromResult(0));
        }

        private static async Task GenerateSettingsFile()
        {
            if (File.Exists(settingsFile))
            {
                return;
            }

            var krakenSettings = new KrakenSettings();

            using var streamWriter = new StreamWriter(settingsFile);

            await streamWriter.WriteAsync(JsonConvert.SerializeObject(krakenSettings, Formatting.Indented, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            }));
        }

        private static async Task Run(RunOptions options)
        {
            var checker = new CheckerBuilder(options.ConfigFile, options.WordlistFile, options.ProxiesFile, options.Skip, options.Bots).Build();

            var consoleManager = new ConsoleManager(checker);

            _ = consoleManager.StartUpdatingConsoleCheckerStatsAsync();

            await checker.StartAsync();
        }

        private static async Task Debug(DebugOptions options)
        {
            var debugger = new DebuggerBuilder(options.ConfigFile, options.BotInput, options.Proxy).Build();

            await debugger.StartAsync();
        }
    }
}