namespace Kraken.Models.Blocks
{
    public class Request
    {
        public HttpMethod Method { get; }
        public string Url { get; }
        public Dictionary<string, string> Headers { get; }
        public string Content { get; }
        public bool AllowAutoRedirect { get; }
        public bool LoadContent { get; }

        public Request(HttpMethod method, string url, Dictionary<string, string> headers, string content, bool allowAutoRedirect, bool loadContent)
        {
            Method = method;
            Url = url;
            Headers = headers;
            Content = content;
            AllowAutoRedirect = allowAutoRedirect;
            LoadContent = loadContent;
        }
    }
}
