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
                            redirecResponseMessage.Headers.Remove("Set-Cookie");
                            botData.Variables["response.headers"] = redirecResponseMessage.Headers.ToString();
                            botData.Variables["response.content"] = _request.LoadContent ? await redirecResponseMessage.Content.ReadAsStringAsync() : string.Empty;
                            break;
                        }
                    }
                }
                else
                {
                    botData.Variables["response.address"] = responseMessage.RequestMessage.RequestUri.AbsoluteUri;
                    botData.Variables["response.statusCode"] = ((int)responseMessage.StatusCode).ToString();
                    responseMessage.Headers.Remove("Set-Cookie");
                    botData.Variables["response.headers"] = responseMessage.Headers.ToString();
                    botData.Variables["response.content"] = _request.LoadContent ? await responseMessage.Content.ReadAsStringAsync() : string.Empty;
                }
            }
            catch
            {
                botData.HttpClient.IsValid = false;
                botData.Status = BotStatus.Retry;
            }
        }

        public override async Task Debug(BotData botData)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(Environment.NewLine + "<--- Executing Block REQUEST --->");

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

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Calling URL: {requestMessage.RequestUri.AbsoluteUri}");

            if (requestMessage.Method == HttpMethod.Post)
            {
                Console.WriteLine($"Post Data: {await requestMessage.Content.ReadAsStringAsync()}");
            }

            Console.WriteLine($"Sent Headers:");

            if (requestMessage.Headers.Any())
            {
                Console.WriteLine(requestMessage.Headers.ToString().Trim());
            }

            try
            {
                using var responseMessage = await botData.HttpClient.SendAsync(requestMessage);

                if (responseMessage.Headers.TryGetValues("Set-Cookie", out var values))
                {
                    botData.CookieContainer.SetCookies(responseMessage.RequestMessage.RequestUri, string.Join(", ", values));
                }

                var responseHeaders = string.Empty;

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
                            responseHeaders = redirecResponseMessage.Headers.ToString();
                            redirecResponseMessage.Headers.Remove("Set-Cookie");
                            botData.Variables["response.headers"] = redirecResponseMessage.Headers.ToString();
                            botData.Variables["response.content"] = _request.LoadContent ? await redirecResponseMessage.Content.ReadAsStringAsync() : string.Empty;
                            break;
                        }
                    }
                }
                else
                {
                    botData.Variables["response.address"] = responseMessage.RequestMessage.RequestUri.AbsoluteUri;
                    botData.Variables["response.statusCode"] = ((int)responseMessage.StatusCode).ToString();
                    responseHeaders = responseMessage.Headers.ToString();
                    responseMessage.Headers.Remove("Set-Cookie");
                    botData.Variables["response.headers"] = responseMessage.Headers.ToString();
                    botData.Variables["response.content"] = _request.LoadContent ? await responseMessage.Content.ReadAsStringAsync() : string.Empty;
                }

                Console.WriteLine($"{Environment.NewLine}Address: {botData.Variables["response.address"]}");
                Console.WriteLine($"Response code: {botData.Variables["response.statusCode"]}");

                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                Console.WriteLine("Received headers:");
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine(responseHeaders.Trim());

                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("Response Source:");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(_request.LoadContent ? await responseMessage.Content.ReadAsStringAsync() : "[SKIPPED]");
            }
            catch
            {
                botData.HttpClient.IsValid = false;
                botData.Status = BotStatus.Retry;
            }
        }
    }
}