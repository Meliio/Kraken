using Kraken.Models;
using System.Net;
using Yove.Proxy;

namespace Kraken
{
    public class DebuggerBuilder
    {
        private readonly string _configFile;
        private readonly string _botInput;
        private readonly IEnumerable<string> _proxy;

        public DebuggerBuilder(string configFile, string botInput, IEnumerable<string> proxy)
        {
            _configFile = configFile;
            _botInput = botInput;
            _proxy = proxy;
        }

        public Debugger Build()
        {
            var loliScriptManager = new LoliScriptManager();

            (var configSettings, var blocks) = loliScriptManager.Build(_configFile);

            if (string.IsNullOrEmpty(configSettings.Name))
            {
                configSettings.Name = Path.GetFileNameWithoutExtension(_configFile);
            }

            var botInputs = new BotInput(_botInput);

            var proxyClient = BuildProxyClient(_proxy);

            var httpClient = new CustomHttpClient(BuildHttpClientHandler(proxyClient));

            return new Debugger(blocks, botInputs, httpClient);
        }

        private static ProxyClient? BuildProxyClient(IEnumerable<string> proxy)
        {
            if (proxy.Any())
            {
                var proxySplit = proxy.First().Split(':');

                var proxyClient = new ProxyClient(proxySplit[0], int.Parse(proxySplit[1]), proxy.Count() == 2 ? Enum.Parse<ProxyType>(proxy.ElementAt(1), true) : ProxyType.Http);

                if (proxySplit.Length == 4)
                {
                    proxyClient.Credentials = new NetworkCredential(proxySplit[2], proxySplit[3]);
                }

                return proxyClient;
            }

            return null;
        }

        private static HttpClientHandler BuildHttpClientHandler(ProxyClient? proxyClient = null) => new()
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.All,
            Proxy = proxyClient,
            UseCookies = false
        };
    }
}