/*
```c#
Input:

    {
        // [Required] The url you wish to have a short version for
        "url": "https://docs.microsoft.com/en-ca/azure/azure-functions/functions-create-your-first-function-visual-studio",
        
        // [Optional] Title of the page, or text description of your choice.
        "title": "Quickstart: Create your first function in Azure using Visual Studio"

        // [Optional] the end of the URL. If nothing one will be generated for you.
        "vanity": "azFunc"
    }

Output:
    {
        "ShortUrl": "http://c5m.ca/azFunc",
        "LongUrl": "https://docs.microsoft.com/en-ca/azure/azure-functions/functions-create-your-first-function-visual-studio"
    }
*/

using Cloud5mins.ShortenerTools.Core.Domain;
using Cloud5mins.ShortenerTools.Core.Messages;
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

    public class MyUrlCreate
    {
        private readonly ILogger _logger;
        private readonly ShortenerSettings _settings;

        public MyUrlCreate(ILoggerFactory loggerFactory, ShortenerSettings settings)
        {
            _logger = loggerFactory.CreateLogger<UrlList>();
            _settings = settings;
        }

        [Function("MyUrlCreate")]
        public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "api/MyUrlCreate")] HttpRequestData req,
            ExecutionContext context
        )
        {
            _logger.LogInformation($"__trace creating shortURL: {req}");

            string userId = string.Empty;
            LSShortRequest input;
            var result = new LSShortResponse();

            try
            {
                // Validation of the inputs
                if (req == null)
                {
                    return req.CreateResponse(HttpStatusCode.NotFound);
                }

                using (var reader = new StreamReader(req.Body))
                {
                    var strBody = await reader.ReadToEndAsync();
                    input = JsonSerializer.Deserialize<LSShortRequest>(strBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (input == null)
                    {
                        return req.CreateResponse(HttpStatusCode.NotFound);
                    }
                }

                // If the Url parameter only contains whitespaces or is empty return with BadRequest.
                if (string.IsNullOrWhiteSpace(input.Url))
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteAsJsonAsync(new { Message = "The url parameter can not be empty." });
                    return badResponse;
                }

                // Validates if input.url is a valid aboslute url, aka is a complete refrence to the resource, ex: http(s)://google.com
                if (!Uri.IsWellFormedUriString(input.Url, UriKind.Absolute))
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteAsJsonAsync(new { Message = $"{input.Url} is not a valid absolute Url. The Url parameter must start with 'http://' or 'http://'." });
                    return badResponse;
                }

                StorageTableHelper stgHelper = new StorageTableHelper(_settings.DataStorage);

                string longUrl = input.Url.Trim();
                string vanity = string.IsNullOrWhiteSpace(input.Vanity) ? "" : input.Vanity.Trim();
                string title = string.IsNullOrWhiteSpace(input.Title) ? "" : input.Title.Trim();

                Nullable<DateTime> expiresat = input.ExpiresAt;


                MyShortUrlEntity newRow;

                _logger.LogInformation($"__trace vanity : {vanity}");

                if (!string.IsNullOrEmpty(vanity))
                {
                    newRow = new MyShortUrlEntity(longUrl, vanity, title, expiresat, input.Schedules);

                    _logger.LogInformation($"__trace IfShortUrlEntityExist start: {DateTime.Now.ToString("hh.mm.ss.ffffff")}");

                    if (await stgHelper.IfShortUrlEntityExist(newRow))
                    {
                        var badResponse = req.CreateResponse(HttpStatusCode.Conflict);
                        await badResponse.WriteAsJsonAsync(new { Message = "This Short URL already exist." });
                        return badResponse;
                    }

                    _logger.LogInformation($"__trace IfShortUrlEntityExist end: {DateTime.Now.ToString("hh.mm.ss.ffffff")}");
                }
                else
                {
                    _logger.LogInformation($"__trace MyShortUrlEntity start: {DateTime.Now.ToString("hh.mm.ss.ffffff")}");

                    newRow = new MyShortUrlEntity(longUrl, await Utility.GetValidEndUrl(vanity, stgHelper), title, expiresat, input.Schedules);

                    _logger.LogInformation($"__trace MyShortUrlEntity end: {DateTime.Now.ToString("hh.mm.ss.ffffff")}");
                }

                _logger.LogInformation($"__trace SaveShortUrlEntity start: {DateTime.Now.ToString("hh.mm.ss.ffffff")}");

                await stgHelper.SaveShortUrlEntity(newRow);

                _logger.LogInformation($"__trace SaveShortUrlEntity end: {DateTime.Now.ToString("hh.mm.ss.ffffff")}");

                var host = string.IsNullOrEmpty(_settings.CustomDomain) ? req.Url.Host : _settings.CustomDomain.ToString();
                result = new LSShortResponse(host, newRow.Url, newRow.RowKey, newRow.Title, expiresat);

                _logger.LogInformation($"Short Url created: {DateTime.Now.ToString("hh.mm.ss.ffffff")}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error was encountered.");

                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { ex.Message });
                return badResponse;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);

            return response;
        }
    }
}
