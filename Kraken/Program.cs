using CommandLine;
using Kraken.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Spectre.Console;

namespace Kraken
{
    [Verb("run")]
    public class RunOptions
    {
        [Option('c', "config", Required = true, HelpText = "Configuration file.")]
        public string ConfigFile { get; set; } = string.Empty;

        [Option('w', "wordlist", Required = true, HelpText = "File contains a list of words.")]
        public string WordlistFile { get; set; } = string.Empty;

        [Option('p', "proxies", HelpText = "File contains a list of proxy.")]
        public IEnumerable<string> Proxies { get; set; } = Array.Empty<string>();

        [Option('s', "skip", Default = 0, HelpText = "Number of lines to skip in the wordlist.")]
        public int Skip { get; set; }

        [Option('b', "bots", Default = 1, HelpText = "Number of bots.")]
        public int Bots { get; set; }

        [Option('v', "verbose", Default = false, HelpText = "Prints task errors.")]
        public bool Verbose { get; set; }
    }

    public class Program
    {
        private const string settingsFile = "settings.json";

        public static async Task Main(string[] args)
        {
            //var class1 = new Class1();
            //await class1.Run();





            //await GenerateSettingsFile();

            //await Parser.Default.ParseArguments<RunOptions>(args).WithParsedAsync(Run);

            await Run(new RunOptions() { Bots = 1, ConfigFile = "test.txt", Proxies = new string[] { }, Skip = 0, Verbose = false, WordlistFile = "combos.txt" });
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

            AnsiConsole.MarkupLine("[grey]LOG:[/] checker initialized succesfully");

            var consoleManager = new ConsoleManager(checker);

            _ = consoleManager.StartListeningKeysAsync();

            await checker.StartAsync();
        }
    }
}