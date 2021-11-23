using CommandLine;
using Kraken.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Kraken
{
    public class Options
    {
        [Option('w', "wordlist", Required = true, HelpText = "Path of the word list file")]
        public string WordListPath { get; set; } = String.Empty;

        [Option('p', "proxies", HelpText = "Path of the proxies file")]
        public IEnumerable<string> ProxiesPath { get; set; } = Array.Empty<string>();

        [Option('c', "config", Required = true, HelpText = "Path of the config file")]
        public string ConfigPath { get; set; } = String.Empty;

        [Option('t', "threads", HelpText = "Number of threads")]
        public int Threads { get; set; }
    }

    public class Program
    {
        private const string settingsFile = "settings.json";

        public static async Task Main(string[] args)
        {
            await GenerateSettingsFile();

            await Parser.Default.ParseArguments<Options>(args).WithParsedAsync(Run);
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

        private static async Task Run(Options options)
        {
            var checker = new CheckerBuilder(options.WordListPath, options.ProxiesPath.ToArray(), options.ConfigPath, options.Threads).Build();

            var consoleManager = new ConsoleManager(checker);

            _ = consoleManager.StartUpdatingConsoleCheckerStatsAsync();

            await checker.StartAsync();

            await Task.Delay(1000);
        }
    }
}
