using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Data.Tables;
using OpenApi;

namespace Editor
{
    public static class RewriteEmail
    {
        [FunctionName("RewriteEmail")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            int maxRequestTextLength = int.Parse(Environment.GetEnvironmentVariable("MaxRequestTextLength"));
            var buffer = new char[maxRequestTextLength];
            await new StreamReader(req.Body).ReadAsync(buffer, 0, maxRequestTextLength);
            dynamic request = JsonConvert.DeserializeObject(new string(buffer));
            
            if (request == null)
            {
                log.LogWarning("request is null.");
                return new BadRequestResult();
            }

            var client = new OpenApiClient(Environment.GetEnvironmentVariable("OpenApiKey")); // { MaxTokens = 7 };

            string prefix = Environment.GetEnvironmentVariable("Prefix");

            //var completion = await client.GenerateCompletion(prefix + request.text);
            var completion = client.GenerateCompletionStub(prefix + request.text);

            await StoreRewriteLog(prefix, request, completion);

            log.LogInformation("C# HTTP trigger function processed a request.");

            return new OkObjectResult(new { Text = completion.Text });
        }

        private static async Task StoreRewriteLog(string prefix, dynamic request, Completion completion)
        {
            var storageConnectionString = Environment.GetEnvironmentVariable("StorageConnectionString");

            var client = new TableClient(storageConnectionString, "rewrites");

            await client.CreateIfNotExistsAsync();

            var entity = new TableEntity(DateTime.UtcNow.ToString("yyyy-MM"), completion.Id)
            {
                { "Prefix", prefix },
                { "RequestTextLength", ((string)request.text).Length },
                { "PromptTokens", completion.PromptTokens },
                { "CompletionTokens", completion.CompletionTokens },
                { "TotalTokens", completion.TotalTokens },
            };

            if ((bool)request.allowLog)
            {
                entity["input"] = request.text;
                entity["output"] = completion.Text;
            }

            await client.AddEntityAsync(entity);
        }
    }
}
