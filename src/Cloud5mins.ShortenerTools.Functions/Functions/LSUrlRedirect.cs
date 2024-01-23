using Cloud5mins.ShortenerTools.Core.Domain;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Cloud5mins.ShortenerTools.Functions
{
    public class LSUrlRedirect
    {
        private readonly ILogger _logger;
        private readonly ShortenerSettings _settings;

        public LSUrlRedirect(ILoggerFactory loggerFactory, ShortenerSettings settings)
        {
            _logger = loggerFactory.CreateLogger<LSUrlRedirect>();
            _settings = settings;
        }

        [Function("LSUrlRedirect")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/{shortUrl}")]
            HttpRequestData req,
            string shortUrl,
            ExecutionContext context)
        {
            string redirectUrl = "https://www.luzsaude.pt/pt/?ExpiredShortUrl";


            if (!string.IsNullOrWhiteSpace(shortUrl))
            {
                redirectUrl = _settings.DefaultRedirectUrl ?? redirectUrl;

                StorageTableHelper stgHelper = new StorageTableHelper(_settings.DataStorage);

                var tempUrl = new MyShortUrlEntity(string.Empty, shortUrl);
                var newUrl = await stgHelper.GetShortUrlEntity(tempUrl);

                if (newUrl != null)
                {
                    if (newUrl.ExpiresAt.HasValue && newUrl.ExpiresAt > System.DateTime.UtcNow)
                    {
                        _logger.LogInformation($"Found it: {newUrl.Url}");
                        newUrl.Clicks++;
                        await stgHelper.SaveClickStatsEntity(new ClickStatsEntity(newUrl.RowKey));
                        await stgHelper.SaveShortUrlEntity(newUrl);
                        redirectUrl = WebUtility.UrlDecode(newUrl.ActiveUrl);
                    }
                    else
                        _logger.LogInformation($"Found it: {newUrl.Url}, but ShortUrl has Expired:  {newUrl.ExpiresAt}");
                    
                }
            }
            else
            {
                _logger.LogInformation("Bad Link, resorting to fallback.");
            }

            var res = req.CreateResponse(HttpStatusCode.Redirect);
            res.Headers.Add("Location", redirectUrl);
            return res;

        }
    }
}
