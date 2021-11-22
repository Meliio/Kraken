using Kraken.Models;
using System.Net;
using Yove.Proxy;

namespace Kraken
{
    public class HttpClientManager
    {
        private readonly List<CustomHttpClient> _customHttpClients;
        private readonly Random _random;

        public HttpClientManager()
        {
            var httpClientHandler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.All,
                UseCookies = false
            };

            var customHttpClient = new CustomHttpClient(httpClientHandler);

            _customHttpClients = new List<CustomHttpClient>()
            {
                customHttpClient
            };
            _random = new Random();
        }

        public HttpClientManager(string[] proxiesPath)
        {
            var proxies = File.ReadAllLines(proxiesPath[0]).Select(p => BuildProxyClient(p, Enum.TryParse<ProxyType>(proxiesPath[1], true, out var proxyType) ? proxyType : ProxyType.Http));

            var httpClientHandlers = proxies.Select(p => new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.All,
                UseCookies = false,
                Proxy = p
            });

            var customHttpClients = httpClientHandlers.Select(h => new CustomHttpClient(h));

            _customHttpClients = new List<CustomHttpClient>(customHttpClients);
            _random = new Random();
        }

        public CustomHttpClient GetRandomCustomHttpClient()
        {
            var customHttpClients = _customHttpClients.Where(h => h.IsValid);

            if (customHttpClients.Any())
            {
                return customHttpClients.ElementAt(_random.Next(customHttpClients.Count()));
            }

            _customHttpClients.ForEach(h => h.IsValid = true);

            return _customHttpClients[_random.Next(_customHttpClients.Count)];
        }

        private static ProxyClient BuildProxyClient(string proxy, ProxyType proxyType)
        {
            var proxySplit = proxy.Split(':');

            var proxyClient = new ProxyClient(proxySplit[0], int.Parse(proxySplit[1]), proxyType);

            if (proxySplit.Length == 4)
            {
                proxyClient.Credentials = new NetworkCredential(proxySplit[2], proxySplit[3]);
            }

            return proxyClient;
        }
    }
}
