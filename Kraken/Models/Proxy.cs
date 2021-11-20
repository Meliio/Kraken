using System.Net;

namespace Kraken.Models
{
    public class Proxy : WebProxy
    {
        public Proxy(string proxy)
        {
            var proxySplit = proxy.Split(':');

            Address = new Uri($"http://{proxySplit[0]}:{proxySplit[1]}");

            if (proxySplit.Length == 4)
            {
                Credentials = new NetworkCredential(proxySplit[2], proxySplit[3]);
            }
        }
    }
}
