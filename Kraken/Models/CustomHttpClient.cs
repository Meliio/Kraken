namespace Kraken.Models
{
    public class CustomHttpClient : HttpClient
    {
        public bool IsValid { get; set; }

        public CustomHttpClient(HttpClientHandler httpClientHandler) : base(httpClientHandler)
        {
            IsValid = true;
        }
    }
}
