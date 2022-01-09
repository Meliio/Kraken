using Kraken.Blocks;
using Kraken.Models;
using Kraken.Models.Blocks;
using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace Kraken
{
    public class LoliScriptManager
    {
        private readonly Dictionary<string, Func<string, Block>> _buildBlockFunctions;

        public LoliScriptManager()
        {
            _buildBlockFunctions = new Dictionary<string, Func<string, Block>>(StringComparer.OrdinalIgnoreCase)
            {
                { "request", BuildBlockRequest },
                { "parse", BuildBlockExtractor },
                { "keycheck", BuildBlockKeycheck }
            };
        }

        public (ConfigSettings, IEnumerable<Block>) Build(string configPath)
        {
            var lines = File.ReadAllLines(configPath).Where(l => !string.IsNullOrEmpty(l));

            var settings = new StringBuilder();

            var script = new List<string>();

            var isScript = false;

            foreach (var line in lines.Skip(1))
            {
                if (line.Equals("[SCRIPT]"))
                {
                    isScript = true;
                }
                else
                {
                    if (isScript)
                    {
                        if (line.StartsWith(' '))
                        {
                            script[^1] += $" {line.Trim()}";
                        }
                        else
                        {
                            script.Add(line);
                        }
                    }
                    else
                    {
                        settings.Append(line.Trim());
                    }
                }
            }

            var configSettings = JsonConvert.DeserializeObject<ConfigSettings>(settings.ToString());

            var blocks = new List<Block>();

            foreach (var line in script)
            {
                var blockName = Regex.Match(line, @"^([\w\-]+)").Value;

                if (_buildBlockFunctions.ContainsKey(blockName))
                {
                    blocks.Add(_buildBlockFunctions[blockName].Invoke(line[blockName.Length..].TrimStart()));
                };
            }

            return (configSettings, blocks);
        }

        private BlockRequest BuildBlockRequest(string script)
        {
            var httpMethod = new HttpMethod(GetToken(ref script));

            var url = GetToken(ref script, true);

            var headers = new Dictionary<string, string>();

            var cookies = new List<string>();

            var content = string.Empty;

            var contentType = "application/x-www-form-urlencoded";

            var redirect = true;

            var loadContent = true;

            while (!string.IsNullOrEmpty(script))
            {
                var token = GetToken(ref script);

                switch (token.ToUpper())
                {
                    case "HEADER":
                        var headerSplit = GetToken(ref script, true).Split(": ");
                        if (headerSplit.Length == 2)
                        {
                            if (headerSplit[0].Equals("Cookie", StringComparison.OrdinalIgnoreCase))
                            {
                                cookies.AddRange(headerSplit[1].Split("; "));
                            }
                            else
                            {
                                headers.Add(headerSplit[0], headerSplit[1]);
                            }
                        }
                        break;
                    case "COOKIE":
                        var cookieSplit = GetToken(ref script, true).Split(": ");
                        if (cookieSplit.Length == 2)
                        {
                            cookies.Add(string.Join('=', cookieSplit));
                        }     
                        break;
                    case "CONTENT":
                        content = GetToken(ref script, true);
                        break;
                    case "CONTENTTYPE":
                        contentType = GetToken(ref script, true).Split(';')[0];
                        break;
                    default:
                        var tokenSplit = token.Split('=');
                        if (tokenSplit.Length == 2)
                        {
                            if (bool.TryParse(tokenSplit[1], out var result))
                            {
                                switch (tokenSplit[0].ToUpper())
                                {
                                    case "REDIRECT":
                                        redirect = result;
                                        break;
                                    case "LOADCONTENT":
                                        loadContent = result;
                                        break;
                                }
                            }
                        }
                        break;
                }
            }

            var request = new Request(httpMethod, url, headers, string.Join(", ", cookies), content, contentType, redirect, loadContent);

            return new BlockRequest(request);
        }

        private BlockExtractor BuildBlockExtractor(string script)
        {
            var extractor = new Extractor
            {
                Source = GetToken(ref script, true)
            };

            while (!string.IsNullOrEmpty(script))
            {
                var token = GetToken(ref script);

                switch (token.ToUpper())
                {
                    case "LR":
                        extractor.Type = token;
                        extractor.Left = GetToken(ref script, true);
                        extractor.Right = GetToken(ref script, true);
                        break;
                    case "CSS":
                        extractor.Type = token;
                        extractor.Selector = GetToken(ref script, true);
                        extractor.Attribute = GetToken(ref script, true);
                        break;
                    case "JSON":
                        extractor.Type = token;
                        extractor.Json = GetToken(ref script, true);
                        break;
                    case "REGEX":
                        extractor.Type = token;
                        extractor.Regex = GetToken(ref script, true);
                        extractor.Group = GetToken(ref script, true);
                        break;
                    default:
                        if (token.Equals("->"))
                        {
                            extractor.Capture = GetToken(ref script).Equals("CAP", StringComparison.OrdinalIgnoreCase);
                            extractor.Name = GetToken(ref script, true);
                        }
                        break;
                }
            }

            return new BlockExtractor(extractor);
        }

        private BlockKeycheck BuildBlockKeycheck(string script)
        {
            var keycheck = new Keycheck();

            while (!string.IsNullOrEmpty(script))
            {
                var token = GetToken(ref script);

                switch (token.ToUpper())
                {
                    case "KEYCHAIN":
                        keycheck.Keychains.Add(new Keychain
                        {
                            Status = GetToken(ref script),
                            Condition = GetToken(ref script)
                        });
                        break;
                    case "KEY":
                        var item = GetToken(ref script, true);
                        if (script.StartsWith('"'))
                        {
                            keycheck.Keychains[^1].Keys.Add(new Key
                            {
                                Source = item,
                                Condition = GetToken(ref script, true),
                                Value = GetToken(ref script, true)
                            });
                        }
                        else
                        {
                            keycheck.Keychains[^1].Keys.Add(new Key
                            {
                                Source = "<data.source>",
                                Condition = "contains",
                                Value = item
                            });
                        }
                        break;
                    default:
                        var tokenSplit = token.Split('=');
                        if (tokenSplit.Length == 2)
                        {
                            if (bool.TryParse(tokenSplit[1], out var result))
                            {
                                switch (tokenSplit[0].ToUpper())
                                {
                                    case "BANONTOCHECK":
                                        keycheck.BanOnToCheck = result;
                                        break;
                                }
                            }
                        }
                        break;
                }
            }

            return new BlockKeycheck(keycheck);
        }

        private string GetToken(ref string input, bool isLiteral = false)
        {
            var match = isLiteral ? Regex.Match(input, "\"(\\\\.|[^\\\"])*\"") : Regex.Match(input, "^[^ ]*");

            input = input[match.Value.Length..].TrimStart();

            return isLiteral ? match.Value.Trim('"') : match.Value;
        }
    }
}
