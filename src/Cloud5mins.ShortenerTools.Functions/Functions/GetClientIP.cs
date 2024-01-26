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
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Cloud5mins.ShortenerTools.Functions
{
    public class GetClientIP
    {

        private readonly ILogger _logger;
        private readonly ShortenerSettings _settings;

        public GetClientIP(ILoggerFactory loggerFactory, ShortenerSettings settings)
        {
            _logger = loggerFactory.CreateLogger<GetClientIP>();
            _settings = settings;
        }

        [Function("GetClientIP")]
        public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "api/GetClientIP")] HttpRequestData req, ExecutionContext context, string name)
        {
            _logger.LogInformation($"Starting GetClientIP...");

            string ipAddressString = "NOT FOUND";
            string name_ = name;

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            name_ = name_ ?? data?.Name;

            string responseMessage = string.IsNullOrEmpty(name) 
                ? "This HTTP triggered funtion executed successfully. Pass a name in the queru string or in the request body for personalized response."
                : $"Hello, {name_}. This HTTP triggered funtion executed successfully.";


            //Retrieve client IP
            ipAddressString = GetIpFromRequestHeaders(req);
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(ipAddressString);
            return response;
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
