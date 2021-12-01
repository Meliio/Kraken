using Kraken.Models;
using System.Net;
using Yove.Proxy;

namespace Kraken
{
    public class HttpClientManager
    {
        private readonly CustomHttpClient[] _httpClients;
        private readonly Random _random;

        public HttpClientManager()
        {
            var httpClientHandler = new HttpClientHandler()
            {
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.All,
                UseCookies = false
            };

            var httpClient = new CustomHttpClient(httpClientHandler)
            { 
                Timeout = TimeSpan.FromSeconds(10) 
            };

            _httpClients = new CustomHttpClient[] { httpClient };
            _random = new Random();
        }

        public HttpClientManager(IEnumerable<string> proxies, ProxyType proxyType)
        {
            var proxyClients = proxies.Select(p => BuildProxyClient(p, proxyType));

            var httpClientHandlers = proxyClients.Select(p => BuildPRoxyHttpClientHandler(p));

            var httpClients = httpClientHandlers.Select(h => new CustomHttpClient(h)
            {
                Timeout = TimeSpan.FromSeconds(10)
            });

            _httpClients = httpClients.ToArray();
            _random = new Random();
        }

        public CustomHttpClient GetRandomHttpClient()
        {
            var httpClients = _httpClients.Where(h => h.IsValid);

            if (httpClients.Any())
            {
                return httpClients.ElementAt(_random.Next(httpClients.Count()));
            }

            foreach (var httpClient in _httpClients)
            {
                httpClient.IsValid = true;
            }

            return _httpClients[_random.Next(_httpClients.Length)];
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

        private static HttpClientHandler BuildPRoxyHttpClientHandler(ProxyClient proxyClient) => new()
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.All,
            Proxy = proxyClient,
            UseCookies = false
        };
    }
}
