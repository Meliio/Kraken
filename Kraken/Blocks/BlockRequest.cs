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

            if (requestMessage.Method == HttpMethod.Post)
            {
                requestMessage.Content = new StringContent(ReplaceValues(_request.Content, botData), Encoding.UTF8, _request.Headers["Content-Type"]);
            }

            var cookieHeader = botData.CookieContainer.GetCookieHeader(requestMessage.RequestUri);

            if (!string.IsNullOrEmpty(cookieHeader))
            {
                requestMessage.Headers.Add("Cookie", cookieHeader);
            }

            try
            {
                using var responseMessage = await botData.HttpClient.SendAsync(requestMessage);

                botData.Variables["response.address"] = responseMessage.RequestMessage.RequestUri.AbsoluteUri;
                botData.Variables["response.statusCode"] = ((int)responseMessage.StatusCode).ToString();
                botData.Variables["response.headers"] = responseMessage.Headers.ToString();
                botData.Variables["response.content"] = _request.LoadContent ? await responseMessage.Content.ReadAsStringAsync() : string.Empty;

                if (responseMessage.Headers.TryGetValues("Set-Cookie", out var values))
                {
                    botData.CookieContainer.SetCookies(responseMessage.RequestMessage.RequestUri, string.Join(", ", values));
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