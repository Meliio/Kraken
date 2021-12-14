using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Kraken.Models;
using Kraken.Models.Blocks;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Kraken.Blocks
{
    public class BlockExtractor : Block
    {
        private readonly Extractor _extractor;
        private readonly Dictionary<string, Func<string, string>> _extractorFunctions;
        private readonly Dictionary<string, Func<IElement, string>> _getSelectorAttributeFunctions;

        public BlockExtractor(Extractor extractor)
        {
            _extractor = extractor;
            _extractorFunctions = new Dictionary<string, Func<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "lr", LeftRightExtractor },
                { "json", JsonExtractor },
                { "css", CssExtractor },
                { "regex", RegexExtractor }
            };
            _getSelectorAttributeFunctions = new Dictionary<string, Func<IElement, string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "innerHtml", AttributeInnerHtml },
                { "outerHtml", AttributeOuterHtml },
                { "innerText", AttributeInnerText }
            };
        }

        public override Task Run(BotData botData)
        {
            var result = _extractorFunctions[_extractor.Type].Invoke(ReplaceValues(_extractor.Source, botData));

            if (_extractor.Capture)
            {
                if (!string.IsNullOrEmpty(result))
                {
                    botData.Captures[_extractor.Name] = result;
                }
            }
            else
            {
                botData.Variables[_extractor.Name] = result;
            }

            return Task.CompletedTask;
        }

        public override Task Debug(BotData botData, StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine("[orange3]<--- Executing EXTRACTOR --->[/]");

            var result = _extractorFunctions[_extractor.Type].Invoke(ReplaceValues(_extractor.Source, botData));

            if (_extractor.Capture)
            {
                if (!string.IsNullOrEmpty(result))
                {
                    botData.Captures[_extractor.Name] = result;
                    stringBuilder.AppendLine($"[red3_1]Extracted variable | Name: {_extractor.Name} | Value: {result} | Capture: {_extractor.Capture}[/]");
                }
            }
            else
            {
                botData.Variables[_extractor.Name] = result;
                stringBuilder.AppendLine($"[darkorange3_1]Extracted variable | Name: {_extractor.Name} | Value: {result} | Capture: {_extractor.Capture}[/]");
            }

            return Task.CompletedTask;
        }

        private string LeftRightExtractor(string source)
        {
            var indexOfBegin = source.IndexOf(_extractor.Left);

            if (indexOfBegin == -1)
            {
                return string.Empty;
            }

            source = source[(indexOfBegin + _extractor.Left.Length)..];

            var indexOfEnd = source.IndexOf(_extractor.Right);

            if (indexOfEnd == -1)
            {
                return string.Empty;
            }

            return source[..indexOfEnd];
        }

        private string JsonExtractor(string source)
        {
            var token = JObject.Parse(source).SelectToken(_extractor.Json);

            if (token is null)
            {
                return string.Empty;
            }

            return token.ToString();
        }

        private string CssExtractor(string source)
        {
            var htmlParser = new HtmlParser();

            using var document = htmlParser.ParseDocument(source);

            var element = document.QuerySelector(_extractor.Selector);

            if (element is null)
            {
                return string.Empty;
            }

            return _getSelectorAttributeFunctions.ContainsKey(_extractor.Attribute) ? _getSelectorAttributeFunctions[_extractor.Attribute].Invoke(element) : element.HasAttribute(_extractor.Attribute) ? element.GetAttribute(_extractor.Attribute) : string.Empty;
        }

        private static string AttributeInnerHtml(IElement element) => element.InnerHtml;

        private static string AttributeOuterHtml(IElement element) => element.OuterHtml;

        private static string AttributeInnerText(IElement element) => element.TextContent;

        private string RegexExtractor(string source) => Regex.Match(source, _extractor.Regex).Groups[_extractor.Group].Value;
    }
}
