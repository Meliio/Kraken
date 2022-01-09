using Kraken.Models;
using System.Net;
using Yove.Proxy;

namespace Kraken
{
    public class HttpClientManager
    {
        private readonly List<CustomHttpClient> _httpClients;
        private readonly Random _random;

        public HttpClientManager()
        {
            var httpClient = new CustomHttpClient(BuildHttpClientHandler())
            {
                Timeout = TimeSpan.FromSeconds(15)
            };;

            _httpClients = new List<CustomHttpClient>()
            {
                httpClient
            };
            _random = new Random();
        }

        public HttpClientManager(IEnumerable<string> proxies, ProxyType proxyType)
        {
            var proxyClients = proxies.Select(p => BuildProxyClient(p, proxyType));

            var httpClientHandlers = proxyClients.Select(p => BuildHttpClientHandler(p));

            var httpClients = httpClientHandlers.Select(h => new CustomHttpClient(h)
            {
                Timeout = TimeSpan.FromSeconds(15),
            });

            _httpClients = new List<CustomHttpClient>(httpClients);
            _random = new Random();
        }

        public CustomHttpClient GetRandomHttpClient()
        {
            lock (_httpClients)
            {
                var httpClients = _httpClients.Where(h => h.IsValid);

                if (httpClients.Any())
                {
                    return httpClients.ElementAt(_random.Next(httpClients.Count()));
                }

                _httpClients.ForEach(h => h.IsValid = true);

                return _httpClients[_random.Next(_httpClients.Count)];
            }
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

        private static HttpClientHandler BuildHttpClientHandler(ProxyClient? proxyClient = null) => new()
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.All,
            Proxy = proxyClient,
            UseCookies = false    
        };
    }
}
