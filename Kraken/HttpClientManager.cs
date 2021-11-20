using Kraken.Models;
using System.Net;

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

        public HttpClientManager(string proxiesPath)
        {
            var proxies = File.ReadAllLines(proxiesPath).Select(p => new Proxy(p));

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
    }
}
