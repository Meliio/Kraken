namespace Kraken.Models.Blocks
{
    public class Request
    {
        public HttpMethod Method { get; }
        public string Url { get; }
        public Dictionary<string, string> Headers { get; }
        public string CookieHeader { get; }
        public string Content { get; }
        public string ContentType { get; }
        public bool AllowAutoRedirect { get; }
        public bool LoadContent { get; }

        public Request(HttpMethod method, string url, Dictionary<string, string> headers, string cookieHeader, string content, string contentType, bool allowAutoRedirect, bool loadContent)
        {
            Method = method;
            Url = url;
            Headers = headers;
            CookieHeader = cookieHeader;
            Content = content;
            ContentType = contentType;
            AllowAutoRedirect = allowAutoRedirect;
            LoadContent = loadContent;
        }
    }
}
