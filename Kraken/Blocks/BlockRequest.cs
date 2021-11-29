using Kraken.Enums;
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
                requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value);  
            }

            if (!string.IsNullOrEmpty(botData.CookieContainer.GetCookieHeader(requestMessage.RequestUri)))
            {
                requestMessage.Headers.Add("Cookie", botData.CookieContainer.GetCookieHeader(requestMessage.RequestUri));
            }

            if (requestMessage.Method == HttpMethod.Post)
            {
                requestMessage.Content = new StringContent(ReplaceValues(_request.Content, botData), Encoding.UTF8, _request.Headers["Content-Type"]);
            }

            try
            {
                using var responseMessage = await botData.HttpClient.SendAsync(requestMessage);

                if (responseMessage.Headers.TryGetValues("Set-Cookie", out var values))
                {
                    botData.CookieContainer.SetCookies(responseMessage.RequestMessage.RequestUri, string.Join(", ", values));
                }

                var location = responseMessage.Headers.Contains("Location") ? responseMessage.Headers.Location.IsAbsoluteUri ? responseMessage.Headers.Location : new Uri(requestMessage.RequestUri.GetLeftPart(UriPartial.Authority) + responseMessage.Headers.Location) : null;

                if (location is not null && _request.AllowAutoRedirect)
                {
                    while (true)
                    {
                        using var redirectRequestMessage = new HttpRequestMessage(HttpMethod.Get, location);

                        if (!string.IsNullOrEmpty(botData.CookieContainer.GetCookieHeader(redirectRequestMessage.RequestUri)))
                        {
                            redirectRequestMessage.Headers.Add("Cookie", botData.CookieContainer.GetCookieHeader(redirectRequestMessage.RequestUri));
                        }

                        using var redirecResponseMessage = await botData.HttpClient.SendAsync(redirectRequestMessage);

                        if (redirecResponseMessage.Headers.TryGetValues("Set-Cookie", out var redirecValues))
                        {
                            botData.CookieContainer.SetCookies(redirecResponseMessage.RequestMessage.RequestUri, string.Join(", ", redirecValues));
                        }

                        location = redirecResponseMessage.Headers.Contains("Location") ? redirecResponseMessage.Headers.Location.IsAbsoluteUri ? redirecResponseMessage.Headers.Location : new Uri(redirectRequestMessage.RequestUri.GetLeftPart(UriPartial.Authority) + redirecResponseMessage.Headers.Location) : null;

                        if (location is null)
                        {
                            botData.Variables["response.address"] = redirecResponseMessage.RequestMessage.RequestUri.AbsoluteUri;
                            botData.Variables["response.statusCode"] = ((int)redirecResponseMessage.StatusCode).ToString();
                            botData.Variables["response.headers"] = redirecResponseMessage.Headers.ToString();
                            botData.Variables["response.cookies"] = botData.CookieContainer.GetCookieHeader(redirecResponseMessage.RequestMessage.RequestUri);
                            botData.Variables["response.content"] = _request.LoadContent ? await redirecResponseMessage.Content.ReadAsStringAsync() : string.Empty;

                            break;
                        }
                    }
                }
                else
                {
                    botData.Variables["response.address"] = responseMessage.RequestMessage.RequestUri.AbsoluteUri;
                    botData.Variables["response.statusCode"] = ((int)responseMessage.StatusCode).ToString();
                    botData.Variables["response.headers"] = responseMessage.Headers.ToString();
                    botData.Variables["response.cookies"] = botData.CookieContainer.GetCookieHeader(responseMessage.RequestMessage.RequestUri);
                    botData.Variables["response.content"] = _request.LoadContent ? await responseMessage.Content.ReadAsStringAsync() : string.Empty;
                }
            }
            catch
            {
                botData.HttpClient.IsValid = false;
                botData.Status = BotStatus.Retry;
            }
        }
    }
}