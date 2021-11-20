using System.Collections.Generic;
using System.Net.Http;

namespace Kraken.Models.Blocks
{
    public class Request
    {
        public HttpMethod Method { get; }
        public string Url { get; }
        public Dictionary<string, string> Headers { get; }
        public string Content { get; }
        public bool LoadContent { get; }

        public Request(HttpMethod method, string url, Dictionary<string, string> headers, string content, bool loadContent)
        {
            Method = method;
            Url = url;
            Headers = headers;
            Content = content;
            LoadContent = loadContent;
        }
    }
}
