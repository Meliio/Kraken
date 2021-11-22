using Kraken.Blocks;
using Kraken.Models;
using Kraken.Models.Blocks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using YamlDotNet.Serialization;

namespace Kraken
{
    public class CheckerBuilder
    {
        private readonly string _wordListPath;
        private readonly string[] _proxiesPath;
        private readonly string _configPath;
        private readonly int _threads;
        private readonly Dictionary<string, Func<string, Block>> _buildBlockFunctions;

        public CheckerBuilder(string wordListPath, string[] proxiesPath, string configPath, int threads)
        {
            _wordListPath = wordListPath;
            _proxiesPath = proxiesPath;
            _configPath = configPath;
            _threads = (threads == 0) ? Environment.ProcessorCount : threads;
            _buildBlockFunctions = new Dictionary<string, Func<string, Block>>(StringComparer.OrdinalIgnoreCase)
            {
                { "request", BuildBlockRequest },
                { "extractor", BuildBlockExtractor },
                { "keycheck", BuildBlockKeycheck }
            };
        }

        public Checker Build()
        {
            var botInputs = File.ReadAllLines(_wordListPath).Select(w => new BotInput(w));
            
            var httpClientManager = (_proxiesPath.Any()) ? new HttpClientManager(_proxiesPath) : new HttpClientManager();

            var stringReader = new StringReader(File.ReadAllText(_configPath));          

            var deserializer = new DeserializerBuilder().Build();

            var yamlObject = deserializer.Deserialize(stringReader);

            var serializer = new SerializerBuilder().JsonCompatible().Build();

            var json = serializer.Serialize(yamlObject);

            var config = JsonConvert.DeserializeObject<JObject>(json);

            var configSettings = config.TryGetValue("settings", out var token) ? JsonConvert.DeserializeObject<ConfigSettings>(token.ToString()) : new ConfigSettings();

            if (string.IsNullOrEmpty(configSettings.Name))
            {
                configSettings.Name = Path.GetFileNameWithoutExtension(_configPath);
            }

            var blocks = BuildBlocks(config.GetValue("blocks"));

            var krakenSettings = JsonConvert.DeserializeObject<KrakenSettings>(File.ReadAllText("settings.json"));
            
            var record = GetRecord(configSettings.Name);

            Directory.CreateDirectory(Path.Combine(krakenSettings.OutputDirectory, configSettings.Name));

            Console.OutputEncoding = Encoding.UTF8;

            return new Checker(botInputs, httpClientManager, configSettings, blocks, _threads, krakenSettings, record);
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
            var block = JObject.Parse(json);

            var raw = block.GetValue("raw");

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

            var loadContent = !block.TryGetValue("loadContent", out var token) || (bool)token;

            var request = new Request(httpMethod, url, headers, content, loadContent);

            return new BlockRequest(request);
        }

        private BlockExtractor BuildBlockExtractor(string json) => new(JsonConvert.DeserializeObject<Extractor>(json));

        private BlockKeycheck BuildBlockKeycheck(string json) => new(JsonConvert.DeserializeObject<Keycheck>(json));

        private Record GetRecord(string configName)
        {
            using var database = new LiteDB.LiteDatabase("Kraken.db");

            var collection = database.GetCollection<Record>("records");

            if (collection.Exists(r => r.ConfigName == configName && r.WordListLocation == _wordListPath))
            {
                return collection.FindOne(r => r.ConfigName == configName && r.WordListLocation == _wordListPath);
            };

            var record = new Record()
            {
                ConfigName = configName,
                WordListLocation = _wordListPath,
                Progress = 0
            };

            collection.Insert(record);

            return record;
        }
    }
}
