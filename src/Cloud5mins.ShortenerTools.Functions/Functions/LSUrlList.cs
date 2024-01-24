/*
```c#
Input:


Output:
    {
        "Url": "https://SOME_URL",
        "Clicks": 0,
        "PartitionKey": "d",
        "title": "Quickstart: Create your first function in Azure using Visual Studio"
        "RowKey": "doc",
        "Timestamp": "0001-01-01T00:00:00+00:00",
        "ETag": "W/\"datetime'2020-05-06T14%3A33%3A51.2639969Z'\""
    }
*/

using Cloud5mins.ShortenerTools.Core.Domain;
using Cloud5mins.ShortenerTools.Core.Messages;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Cloud5mins.ShortenerTools.Functions
{
    public class LSUrlList
    {

        private readonly ILogger _logger;
        private readonly ShortenerSettings _settings;

        public LSUrlList(ILoggerFactory loggerFactory, ShortenerSettings settings)
        {
            _logger = loggerFactory.CreateLogger<LSUrlList>();
            _settings = settings;
        }

        [Function("LSUrlList")]
        public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/LSUrlList")] HttpRequestData req, ExecutionContext context)
        {
            _logger.LogInformation($"Starting LSUrlList...");

            var result = new ListResponse();
            string userId = string.Empty;

            StorageTableHelper stgHelper = new StorageTableHelper(_settings.DataStorage);

            try
            {
                result.LSUrlList = await stgHelper.LSGetAllShortUrlEntities();
                result.LSUrlList = result.LSUrlList.Where(p => !(p.IsArchived ?? false)).ToList();
                var host = string.IsNullOrEmpty(_settings.CustomDomain) ? req.Url.Host : _settings.CustomDomain;
                foreach (ShortUrlEntity url in result.UrlList)
                {
                    url.ShortUrl = Utility.GetShortUrl(host, url.RowKey);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error was encountered.");
                var badres = req.CreateResponse(HttpStatusCode.BadRequest);
                await badres.WriteAsJsonAsync(new { ex.Message });
                return badres;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);
            return response;
        }
    }
}
