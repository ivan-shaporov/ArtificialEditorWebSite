using System;
using System.Linq;
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
        public static async Task<IActionResult> RunRewriteEmail(
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

            //var completion = await client.GenerateCompletion(prefix.Replace("\\n", "\n") + request.Text);
            var completion = client.GenerateCompletionStub(request.Text);

            string id = Hash(completion.Id);
            string partition = DateTime.UtcNow.ToString("yyyy-MM-dd");

            await StoreRewriteLog(partition, id, prefix, request, completion);

            log.LogInformation("C# HTTP trigger function processed a request.");

            return new OkObjectResult(new {Partition = partition, Id = id, Text = completion.Text });
        }

        [FunctionName("Ping")]
        public static IActionResult RunPing(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            return new OkResult();
        }

        [FunctionName("ReportProblem")]
        public static async Task<IActionResult> RunReportProblem(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            int maxRequestTextLength = int.Parse(Environment.GetEnvironmentVariable("MaxRequestTextLength"));
            var overlength = 100;
            var maxRequestLength = maxRequestTextLength * 11 + overlength;
            var buffer = new char[maxRequestLength];
            int length = await new StreamReader(req.Body).ReadAsync(buffer, 0, maxRequestTextLength + overlength);
            
            if (length >= maxRequestLength)
            {
                return new BadRequestResult();
            }

            var request = JsonConvert.DeserializeObject<ReportProblemRequest>(new string(buffer));
            
            if (request == null)
            {
                log.LogWarning("request is null.");
                return new BadRequestResult();
            }
            
            if (request.Text.Length > maxRequestTextLength ||
                request.Rewritten.Length > maxRequestTextLength * 10)
            {
                return new BadRequestResult();
            }

            var client = await GetTableClient();
            var entity = new TableEntity(request.Partition, request.Id);
            entity.Add("ProblemInput", request.Text);
            entity.Add("Problemoutput", request.Rewritten);

            await client.UpdateEntityAsync(entity, Azure.ETag.All);

            log.LogInformation("C# HTTP trigger function processed a request.");

            return new OkResult();
        }

        private static async Task<TableClient> GetTableClient()
        {
            var storageConnectionString = Environment.GetEnvironmentVariable("StorageConnectionString");
            var client = new TableClient(storageConnectionString, "rewrites");
            await client.CreateIfNotExistsAsync();
            return client;
        }

        private static async Task StoreRewriteLog(string partitionKey, string rowKey, string prefix, RewriteRequest request, Completion completion)
        {
            var client = await GetTableClient();

            var entity = new TableEntity(partitionKey, rowKey)
            {
                { "Prefix", prefix },
                { "CompletionId", completion.Id },
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

        private static string Hash(string v) => $"{v.GetHashCode()}{new string(v.Reverse().ToArray()).GetHashCode()}";
    }

    class RewriteRequest
    {
        public string Text { get; set; }
        public bool AllowLog { get; set; }
    }

    class ReportProblemRequest
    {
        public string Partition { get; set; }
        public string Id { get; set; }
        public string Text { get; set; }
        public string Rewritten { get; set; }
    }
}
