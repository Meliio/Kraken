using Kraken.Models;
using Kraken.Models.Blocks;
using System.Net;
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

            if (!string.IsNullOrEmpty(botData.CookieContainer.GetCookieHeader(requestMessage.RequestUri)))
            {
                requestMessage.Headers.Add("Cookie", botData.CookieContainer.GetCookieHeader(requestMessage.RequestUri));
            }

            if (requestMessage.Method == HttpMethod.Post)
            {
                requestMessage.Content = new StringContent(ReplaceValues(_request.Content, botData), Encoding.UTF8, _request.Headers["Content-Type"]);
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

                    if (!string.IsNullOrEmpty(botData.CookieContainer.GetCookieHeader(redirectRequestMessage.RequestUri)))
                    {
                        redirectRequestMessage.Headers.Add("Cookie", botData.CookieContainer.GetCookieHeader(redirectRequestMessage.RequestUri));
                    }

                    using var redirecResponseMessage = await botData.HttpClient.SendAsync(redirectRequestMessage);

                    if (redirecResponseMessage.Headers.TryGetValues("Set-Cookie", out var redirecValues))
                    {
                        botData.CookieContainer.SetCookies(redirecResponseMessage.RequestMessage.RequestUri, string.Join(", ", redirecValues));
                    }

                    location = redirecResponseMessage.Headers.Contains("Location") ? redirecResponseMessage.Headers.Location.IsAbsoluteUri ? redirecResponseMessage.Headers.Location.AbsoluteUri : new Uri(redirectRequestMessage.RequestUri.GetLeftPart(UriPartial.Authority) + redirecResponseMessage.Headers.Location).AbsoluteUri : string.Empty;

                    if (string.IsNullOrEmpty(location))
                    {
                        botData.Variables["response.address"] = redirecResponseMessage.RequestMessage.RequestUri.AbsoluteUri;
                        botData.Variables["response.statusCode"] = ((int)redirecResponseMessage.StatusCode).ToString();
                        botData.Headers = redirecResponseMessage.Headers.ToArray();
                        botData.Variables["response.content"] = _request.LoadContent ? WebUtility.HtmlDecode(await redirecResponseMessage.Content.ReadAsStringAsync()) : string.Empty;
                        break;
                    }
                }
            }
            else
            {
                botData.Variables["response.address"] = responseMessage.RequestMessage.RequestUri.AbsoluteUri;
                botData.Variables["response.statusCode"] = ((int)responseMessage.StatusCode).ToString();
                botData.Headers = responseMessage.Headers.ToArray();
                botData.Variables["response.content"] = _request.LoadContent ? WebUtility.HtmlDecode(await responseMessage.Content.ReadAsStringAsync()) : string.Empty;
            }
        }

        public override async Task Debug(BotData botData)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("[--- Executing Block REQUEST ---]");

            using var requestMessage = new HttpRequestMessage(_request.Method, ReplaceValues(_request.Url, botData));

            foreach (var header in _request.Headers)
            {
                requestMessage.Headers.TryAddWithoutValidation(ReplaceValues(header.Key, botData), ReplaceValues(header.Value, botData));
            }

            botData.CookieContainer.SetCookies(requestMessage.RequestUri, ReplaceValues(_request.CookieHeader, botData));

            if (!string.IsNullOrEmpty(botData.CookieContainer.GetCookieHeader(requestMessage.RequestUri)))
            {
                requestMessage.Headers.Add("Cookie", botData.CookieContainer.GetCookieHeader(requestMessage.RequestUri));
            }

            if (requestMessage.Method == HttpMethod.Post)
            {
                requestMessage.Content = new StringContent(ReplaceValues(_request.Content, botData), Encoding.UTF8, _request.Headers["Content-Type"]);
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"{requestMessage.Method} {requestMessage.RequestUri.AbsoluteUri}");

            if (requestMessage.Headers.Any())
            {
                Console.WriteLine(requestMessage.Headers.ToString().TrimEnd());
            }

            if (requestMessage.Method == HttpMethod.Post)
            {
                Console.WriteLine(await requestMessage.Content.ReadAsStringAsync());
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

                    if (!string.IsNullOrEmpty(botData.CookieContainer.GetCookieHeader(redirectRequestMessage.RequestUri)))
                    {
                        redirectRequestMessage.Headers.Add("Cookie", botData.CookieContainer.GetCookieHeader(redirectRequestMessage.RequestUri));
                    }

                    using var redirecResponseMessage = await botData.HttpClient.SendAsync(redirectRequestMessage);

                    if (redirecResponseMessage.Headers.TryGetValues("Set-Cookie", out var redirecValues))
                    {
                        botData.CookieContainer.SetCookies(redirecResponseMessage.RequestMessage.RequestUri, string.Join(", ", redirecValues));
                    }

                    location = redirecResponseMessage.Headers.Contains("Location") ? redirecResponseMessage.Headers.Location.IsAbsoluteUri ? redirecResponseMessage.Headers.Location.AbsoluteUri : new Uri(redirectRequestMessage.RequestUri.GetLeftPart(UriPartial.Authority) + redirecResponseMessage.Headers.Location).AbsoluteUri : string.Empty;

                    if (string.IsNullOrEmpty(location))
                    {
                        botData.Variables["response.address"] = redirecResponseMessage.RequestMessage.RequestUri.AbsoluteUri;
                        botData.Variables["response.statusCode"] = ((int)redirecResponseMessage.StatusCode).ToString();
                        botData.Headers = redirecResponseMessage.Headers.ToArray();
                        botData.Variables["response.content"] = _request.LoadContent ? WebUtility.HtmlDecode(await redirecResponseMessage.Content.ReadAsStringAsync()) : string.Empty;
                        break;
                    }
                }
            }
            else
            {
                botData.Variables["response.address"] = responseMessage.RequestMessage.RequestUri.AbsoluteUri;
                botData.Variables["response.statusCode"] = ((int)responseMessage.StatusCode).ToString();
                botData.Headers = responseMessage.Headers.ToArray();
                botData.Variables["response.content"] = _request.LoadContent ? WebUtility.HtmlDecode(await responseMessage.Content.ReadAsStringAsync()) : string.Empty;
            }

            Console.WriteLine();
            Console.WriteLine($"Address: {botData.Variables["response.address"]}");
            Console.WriteLine($"Response code: {botData.Variables["response.statusCode"]}");
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine("Received headers:");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(string.Join(Environment.NewLine, botData.Headers.Select(h => $"{h.Key}: {string.Join(' ', h.Value)}")));
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("All cookies:");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(string.Join(Environment.NewLine, botData.CookieContainer.GetAllCookies().Select(c => $"{c.Name}: {c.Value}")));
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Response Source:");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(_request.LoadContent ? botData.Variables["response.content"] : string.Empty);
        }
    }
}