using Cloud5mins.ShortenerTools.Core.Domain;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Linq;
using System;
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
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{shortUrl}")]
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
                    if (newUrl.ExpiresAt == null)
                    {
                        _logger.LogInformation($"Found it: {newUrl.Url}");
                        newUrl.Clicks++;

                        //await stgHelper.SaveClickStatsEntity(new ClickStatsEntity(newUrl.RowKey));
                        string clientIP = GetIpFromRequestHeaders(req);
                        string[] resClicks = stgHelper.CountClicksByClientIP(newUrl.RowKey.ToString(), clientIP, Convert.ToInt32(_settings.ClickTimeintervalinMinutes), Convert.ToInt32(_settings.MaxClicksPerPeriod)).ToArray();
                        _logger.LogInformation($"CountClicksByClientIP(): " + resClicks[0]);

                        if (resClicks[1] == "AllowRedirectOK")//Control #Clicks per Period of Time
                        {
                            await stgHelper.SaveClickStatsEntity(new ClickStatsEntity(newUrl.RowKey, clientIP));
                            await stgHelper.SaveShortUrlEntity(newUrl);
                            redirectUrl = WebUtility.UrlDecode(newUrl.ActiveUrl);
                        }
                    }
                    else 
                        if(newUrl.ExpiresAt.HasValue && newUrl.ExpiresAt < System.DateTime.UtcNow)
                            _logger.LogInformation($"Found it: {newUrl.Url}, but ShortUrl has Expired:  {newUrl.ExpiresAt}");
                        else
                        {
                            _logger.LogInformation($"Found it: {newUrl.Url}");
                        
                            string clientIP = GetIpFromRequestHeaders(req);
                            string[] resClicks = stgHelper.CountClicksByClientIP(newUrl.RowKey.ToString(), clientIP, Convert.ToInt32(_settings.ClickTimeintervalinMinutes), Convert.ToInt32(_settings.MaxClicksPerPeriod)).ToArray();
                            _logger.LogInformation($"CountClicksByClientIP(): " + resClicks[0]);

                            newUrl.Clicks++;
                            if (resClicks[1] == "AllowRedirectOK")//Control #Clicks per Period of Time
                            {
                            await stgHelper.SaveClickStatsEntity(new ClickStatsEntity(newUrl.RowKey, clientIP));
                            await stgHelper.SaveShortUrlEntity(newUrl);
                                redirectUrl = WebUtility.UrlDecode(newUrl.ActiveUrl);
                            }
                        }
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

        private static string GetIpFromRequestHeaders(HttpRequestData req)
        {
            var ipAddressString = "";
            var headerDictionary = req.Headers.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
            var key = "x-forwarded-for";
            if (headerDictionary.ContainsKey(key))
            {
                IPAddress? ipAddress = null;
                var headerValues = headerDictionary[key];
                var ipn = headerValues?.FirstOrDefault()?.Split(new char[] { ',' }).FirstOrDefault()?.Split(new char[] { ':' }).FirstOrDefault();
                if (IPAddress.TryParse(ipn, out ipAddress))
                {
                    ipAddressString = ipAddress.ToString();
                }
            }

            return ipAddressString;
        }
    }
}
