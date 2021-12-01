﻿using Kraken.Blocks;
using Kraken.Models;
using Kraken.Models.Blocks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using YamlDotNet.Serialization;
using Yove.Proxy;

namespace Kraken
{
    public class DebuggerBuilder
    {
        private readonly string _configFile;
        private readonly string _botInput;
        private readonly IEnumerable<string> _proxy;
        private readonly Dictionary<string, Func<string, Block>> _buildBlockFunctions;

        public DebuggerBuilder(string configFile, string botInput, IEnumerable<string> proxy)
        {
            _configFile = configFile;
            _botInput = botInput;
            _proxy = proxy;
            _buildBlockFunctions = new Dictionary<string, Func<string, Block>>(StringComparer.OrdinalIgnoreCase)
            {
                { "request", BuildBlockRequest },
                { "extractor", BuildBlockExtractor },
                { "keycheck", BuildBlockKeycheck }
            };
        }

        public Debugger Build()
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

            var botInputs = new BotInput(_botInput);

            var httpClientManager = _proxy.Any() ? new HttpClientManager(new string[] { _proxy.First() }, _proxy.Count() == 2 ? Enum.Parse<ProxyType>(_proxy.ElementAt(1), true) : ProxyType.Http) : new HttpClientManager();

            Console.OutputEncoding = Encoding.UTF8;

            return new Debugger(blocks, botInputs, httpClientManager);
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

            var request = new Request(httpMethod, url, headers, content, !block.TryGetValue("redirect", out var redirect) || (bool)redirect, !block.TryGetValue("loadContent", out var loadContent) || (bool)loadContent);

            return new BlockRequest(request);
        }

        private BlockExtractor BuildBlockExtractor(string json) => new(JsonConvert.DeserializeObject<Extractor>(json));

        private BlockKeycheck BuildBlockKeycheck(string json) => new(JsonConvert.DeserializeObject<Keycheck>(json));
    }
}