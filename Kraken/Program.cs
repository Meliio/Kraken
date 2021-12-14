using CommandLine;
using Kraken.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Text;

namespace Kraken
{
    [Verb("run")]
    public class RunOptions
    {
        [Option('c', "config", Required = true)]
        public string ConfigFile { get; set; } = string.Empty;

        [Option('w', "wordlist", Required = true)]
        public string WordlistFile { get; set; } = string.Empty;

        [Option('p', "proxies")]
        public IEnumerable<string> Proxies { get; set; } = Array.Empty<string>();

        [Option('s', "skip", Default = 0)]
        public int Skip { get; set; }

        [Option('b', "bots", Default = 1)]
        public int Bots { get; set; }

        [Option('v', "verbose", Default = false)]
        public bool Verbose { get; set; }
    }

    [Verb("debug")]
    public class DebugOptions
    {
        [Option('c', "config", Required = true)]
        public string ConfigFile { get; set; } = string.Empty;

        [Option('i', "input")]
        public string BotInput { get; set; } = string.Empty;

        [Option('p', "proxy")]
        public IEnumerable<string> Proxy { get; set; } = Array.Empty<string>();
    }

    public class Program
    {
        private const string settingsFile = "settings.json";

        public static async Task Main(string[] args)
        {
            await GenerateSettingsFile();

            Console.OutputEncoding = Encoding.UTF8;

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

        private static async Task Run(RunOptions runOptions)
        {
            var checker = new CheckerBuilder(runOptions.ConfigFile, runOptions.WordlistFile, runOptions.Proxies, runOptions.Skip, runOptions.Bots, runOptions.Verbose).Build();

            await checker.StartAsync();
        }

        private static async Task Debug(DebugOptions debugOptions)
        {
            var debugger = new DebuggerBuilder(debugOptions.ConfigFile, debugOptions.BotInput, debugOptions.Proxy).Build();

            await debugger.StartAsync();
        }
    }
}