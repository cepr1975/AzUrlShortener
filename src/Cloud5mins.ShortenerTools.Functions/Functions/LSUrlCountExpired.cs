/*
```c#
Input:
    {
         // [Required]
        "PartitionKey": "d",

         // [Required]
        "RowKey": "doc",

        // [Optional] New Title for this URL, or text description of your choice.
        "title": "Quickstart: Create your first function in Azure using Visual Studio"

        // [Optional] New long Url where the the user will be redirect
        "Url": "https://SOME_URL"
    }


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
// using Microsoft.Azure.WebJobs;
// using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Cloud5mins.ShortenerTools.Functions
{
    public class LSUrlCountExpired
    {
        private readonly ILogger _logger;
        private readonly ShortenerSettings _settings;

        public LSUrlCountExpired(ILoggerFactory loggerFactory, ShortenerSettings settings)
        {
            _logger = loggerFactory.CreateLogger<UrlList>();
            _settings = settings;
        }

        [Function("LSUrlCountExpired")]
        public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/LSUrlCountExpired")] HttpRequestData req,
                                    ExecutionContext context
                                )
        {
            _logger.LogInformation($"HTTP trigger - LSUrlCountExpired");

            try
            {
                StorageTableHelper stgHelper = new StorageTableHelper(_settings.DataStorage);

                int DeleteEntitiesCreatedNNumberDaysBeforeToday = Convert.ToInt32(_settings.DeleteEntitiesCreatedNNumberDaysBeforeToday);
                string result = await stgHelper.CountExpiredItemsAsync(DeleteEntitiesCreatedNNumberDaysBeforeToday);//LS

                _logger.LogInformation($"CountExpiredItemsAsync(): " + result);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error was encountered.");

                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteAsJsonAsync(new { ex.Message });
                return badRequest;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            return response;
        }
    }
}