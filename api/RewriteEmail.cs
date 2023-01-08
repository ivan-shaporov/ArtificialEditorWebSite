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
            var overlength = 100;
            var buffer = new char[maxRequestTextLength + overlength];
            int length = await new StreamReader(req.Body).ReadAsync(buffer, 0, maxRequestTextLength + overlength);
            
            if (length >= maxRequestTextLength + overlength)
            {
                return new OkObjectResult(new { Text = "Your request is too long. Try a shorter one." });
            }

            var request = JsonConvert.DeserializeObject<RewriteRequest>(new string(buffer));
            
            if (request == null)
            {
                log.LogWarning("request is null.");
                return new BadRequestResult();
            }
            
            if (request.Text.Length > maxRequestTextLength)
            {
                return new OkObjectResult(new { Text = "Your input is too long. Try a shorter one." });
            }

            var client = new OpenApiClient(Environment.GetEnvironmentVariable("OpenApiKey"))
            {
                MaxTokens = int.Parse(Environment.GetEnvironmentVariable("MaxCompletionTokens"))
            };

            string prefix = Environment.GetEnvironmentVariable("Prefix");

            var completion = await client.GenerateCompletion(prefix + request.Text);
            //var completion = client.GenerateCompletionStub(request.Text);

            await StoreRewriteLog(prefix, request, completion);

            log.LogInformation("C# HTTP trigger function processed a request.");

            return new OkObjectResult(new { Text = prefix + "|" +  completion.Text });
        }

        private static async Task StoreRewriteLog(string prefix, RewriteRequest request, Completion completion)
        {
            var storageConnectionString = Environment.GetEnvironmentVariable("StorageConnectionString");

            var client = new TableClient(storageConnectionString, "rewrites");

            await client.CreateIfNotExistsAsync();

            var entity = new TableEntity(partitionKey: DateTime.UtcNow.ToString("yyyy-MM"), rowKey: completion.Id)
            {
                { "Prefix", prefix },
                { "RequestTextLength", request.Text.Length },
                { "PromptTokens", completion.PromptTokens },
                { "CompletionTokens", completion.CompletionTokens },
                { "TotalTokens", completion.TotalTokens },
            };

            if (request.AllowLog)
            {
                entity["input"] = request.Text;
                entity["output"] = completion.Text;
            }

            await client.UpsertEntityAsync(entity);
        }
    }

    class RewriteRequest
    {
        public string Text { get; set; }
        public bool AllowLog { get; set; }
    }
}
