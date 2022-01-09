using Kraken.Models;
using Kraken.Models.Blocks;
using System.Text;

namespace Kraken.Blocks
{
    public class BlockRequest : Block
    {
        private readonly Request _request;

        public BlockRequest(Request request)
        {
            _request = request;
        }

        public override async Task Run(BotData botData)
        {
            using var requestMessage = new HttpRequestMessage(_request.Method, ReplaceValues(_request.Url, botData));

            foreach (var header in _request.Headers)
            {
                requestMessage.Headers.TryAddWithoutValidation(ReplaceValues(header.Key, botData), ReplaceValues(header.Value, botData));  
            }

            botData.CookieContainer.SetCookies(requestMessage.RequestUri, ReplaceValues(_request.CookieHeader, botData));

            var cookieHeader = botData.CookieContainer.GetCookieHeader(requestMessage.RequestUri);

            if (!string.IsNullOrEmpty(cookieHeader))
            {
                requestMessage.Headers.Add("Cookie", botData.CookieContainer.GetCookieHeader(requestMessage.RequestUri));
            }

            if (requestMessage.Method == HttpMethod.Post)
            {
                requestMessage.Content = new StringContent(ReplaceValues(_request.Content, botData), Encoding.UTF8, _request.ContentType);
            }

            using var responseMessage = await botData.HttpClient.SendAsync(requestMessage);

            if (responseMessage.Headers.TryGetValues("Set-Cookie", out var values))
            {
                botData.CookieContainer.SetCookies(responseMessage.RequestMessage.RequestUri, string.Join(", ", values));
            }

            if (responseMessage.Headers.Contains("Location") && _request.AllowAutoRedirect)
            {
                var location = responseMessage.Headers.Location.IsAbsoluteUri ? responseMessage.Headers.Location.AbsoluteUri : new Uri(requestMessage.RequestUri.GetLeftPart(UriPartial.Authority) + responseMessage.Headers.Location).AbsoluteUri;

                while (true)
                {
                    using var redirectRequestMessage = new HttpRequestMessage(HttpMethod.Get, location);

                    cookieHeader = botData.CookieContainer.GetCookieHeader(redirectRequestMessage.RequestUri);

                    if (!string.IsNullOrEmpty(cookieHeader))
                    {
                        redirectRequestMessage.Headers.Add("Cookie", botData.CookieContainer.GetCookieHeader(redirectRequestMessage.RequestUri));
                    }

                    using var redirecResponseMessage = await botData.HttpClient.SendAsync(redirectRequestMessage);

                    if (redirecResponseMessage.Headers.TryGetValues("Set-Cookie", out var redirectValues))
                    {
                        botData.CookieContainer.SetCookies(redirecResponseMessage.RequestMessage.RequestUri, string.Join(", ", redirectValues));
                    }

                    if (redirecResponseMessage.Headers.Contains("Location"))
                    {
                        location = redirecResponseMessage.Headers.Location.IsAbsoluteUri ? redirecResponseMessage.Headers.Location.AbsoluteUri : new Uri(redirectRequestMessage.RequestUri.GetLeftPart(UriPartial.Authority) + redirecResponseMessage.Headers.Location).AbsoluteUri;
                    }
                    else
                    {
                        botData.Variables["data.address"] = redirecResponseMessage.RequestMessage.RequestUri.AbsoluteUri;
                        botData.Variables["data.statusCode"] = ((int)redirecResponseMessage.StatusCode).ToString();
                        botData.Headers = redirecResponseMessage.Headers.ToArray();
                        botData.Variables["data.source"] = _request.LoadContent ? await redirecResponseMessage.Content.ReadAsStringAsync() : string.Empty;
                        break;
                    }
                }
            }
            else
            {
                botData.Variables["data.address"] = responseMessage.RequestMessage.RequestUri.AbsoluteUri;
                botData.Variables["data.statusCode"] = ((int)responseMessage.StatusCode).ToString();
                botData.Headers = responseMessage.Headers.ToArray();
                botData.Variables["data.source"] = _request.LoadContent ? await responseMessage.Content.ReadAsStringAsync() : string.Empty;
            }
        }
    }
}