using Kraken.Blocks;
using Kraken.Models;
using Kraken.Models.Blocks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using YamlDotNet.Serialization;
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
        private readonly Dictionary<string, Func<string, Block>> _buildBlockFunctions;

        public CheckerBuilder(string configFile, string wordlistFile, IEnumerable<string> proxies, int skip, int bots, bool verbose)
        {
            _configFile = configFile;
            _wordlistFile = wordlistFile;
            _proxies = proxies;
            _skip = skip;
            _bots = bots;
            _verbose = verbose;
            _buildBlockFunctions = new Dictionary<string, Func<string, Block>>(StringComparer.OrdinalIgnoreCase)
            {
                { "request", BuildBlockRequest },
                { "extractor", BuildBlockExtractor },
                { "keycheck", BuildBlockKeycheck }
            };
        }

        public Checker Build()
        {
            var stringReader = new StringReader(File.ReadAllText(_configFile));

            var deserializer = new DeserializerBuilder().Build();

            var yamlObject = deserializer.Deserialize(stringReader);

            var serializer = new SerializerBuilder().JsonCompatible().Build();

            var json = serializer.Serialize(yamlObject);

            var config = JsonConvert.DeserializeObject<JObject>(json);

            var configSettings = config.TryGetValue("settings", out var token) ? JsonConvert.DeserializeObject<ConfigSettings>(token.ToString()) : new ConfigSettings();

            if (string.IsNullOrEmpty(configSettings.Name))
            {
                configSettings.Name = Path.GetFileNameWithoutExtension(_configFile);
            }

            var blocks = BuildBlocks(config.GetValue("blocks"));

            var botInputs = File.ReadAllLines(_wordlistFile).Where(w => !string.IsNullOrEmpty(w)).Select(w => new BotInput(w));

            var httpClientManager = _proxies.Any() ? new HttpClientManager(File.ReadAllLines(_proxies.First()).Where(p => !string.IsNullOrEmpty(p)), _proxies.Count() == 2 ? Enum.Parse<ProxyType>(_proxies.ElementAt(1), true) : ProxyType.Http) : new HttpClientManager();

            var parallelOptions = new ParallelOptions()
            {
                MaxDegreeOfParallelism = _bots
            };

            var krakenSettings = JsonConvert.DeserializeObject<KrakenSettings>(File.ReadAllText("settings.json"));

            var record = GetRecord(configSettings.Name);

            Directory.CreateDirectory(Path.Combine(krakenSettings.OutputDirectory, configSettings.Name));

            Console.OutputEncoding = Encoding.UTF8;

            return new Checker(configSettings, blocks, botInputs, httpClientManager, _skip, parallelOptions, _verbose, krakenSettings, record);
        }

        private IEnumerable<Block> BuildBlocks(JToken token)
        {
            var blocks = new List<Block>();

            foreach (var item in token.AsEnumerable())
            {
                var block = JObject.Parse(item.ToString());
                var blockName = block.Properties().First().Name;
                blocks.Add(_buildBlockFunctions[blockName].Invoke(block.GetValue(blockName).ToString()));
            }

            return blocks;
        }

        private BlockRequest BuildBlockRequest(string json)
        {
            var requestBlock = JObject.Parse(json);

            var raw = requestBlock.GetValue("raw");

            var lines = raw.ToString().Trim().Split("\n");

            var firstLineSplit = lines[0].Split(' ');

            var httpMethod = new HttpMethod(firstLineSplit[0]);

            var headers = new Dictionary<string, string>();

            var content = string.Empty;

            foreach (var line in lines.Skip(1))
            {
                if (line.Contains(": "))
                {
                    var headerSplit = line.Split(": ");

                    headers.Add(headerSplit[0], headerSplit[1]);
                }
                else
                {
                    content = line;
                }
            }

            if (httpMethod == HttpMethod.Post && !headers.ContainsKey("Content-Type"))
            {
                headers.Add("Content-Type", "application/x-www-form-urlencoded");
            }

            var url = firstLineSplit[1].StartsWith('/') ? $"https://{headers["Host"]}{firstLineSplit[1]}" : firstLineSplit[1];

            var request = new Request(httpMethod, url, headers, content, !requestBlock.TryGetValue("redirect", out var redirect) || (bool)redirect, !requestBlock.TryGetValue("loadContent", out var loadContent) || (bool)loadContent);

            return new BlockRequest(request);
        }

        private BlockExtractor BuildBlockExtractor(string json) => new(JsonConvert.DeserializeObject<Extractor>(json));

        private BlockKeycheck BuildBlockKeycheck(string json) => new(JsonConvert.DeserializeObject<Keycheck>(json));

        private Record GetRecord(string configName)
        {
            using var database = new LiteDB.LiteDatabase("Kraken.db");

            var collection = database.GetCollection<Record>("records");

            if (collection.Exists(r => r.ConfigName == configName && r.WordListLocation == _wordlistFile))
            {
                return collection.FindOne(r => r.ConfigName == configName && r.WordListLocation == _wordlistFile);
            };

            var record = new Record()
            {
                ConfigName = configName,
                WordListLocation = _wordlistFile,
                Progress = 0
            };

            collection.Insert(record);

            return record;
        }
    }
}