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
using System.Security.Claims;

namespace Editor
{
    public static class RewriteEmail
    {
        [FunctionName("RewriteEmail")]
        public static async Task<IActionResult> RunRewriteEmail(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var principal = StaticWebAppsAuth.Parse(req);
            
            int maxRequestTextLength = int.Parse(Environment.GetEnvironmentVariable("MaxRequestTextLength"));
            var overlength = 100;
            var buffer = new char[maxRequestTextLength + overlength];
            int length = await new StreamReader(req.Body).ReadAsync(buffer, 0, maxRequestTextLength + overlength);
            
            if (length >= maxRequestTextLength + overlength)
            {
                log.LogMetric("RequestTooLong", 1);
                return new OkObjectResult(MakeResultObject("Your request is too long. Try a shorter one."));
            }

            var request = JsonConvert.DeserializeObject<RewriteRequest>(new string(buffer));
            
            if (request == null)
            {
                log.LogError("request is null.");
                return new BadRequestResult();
            }
            
            if (request.Text.Length > maxRequestTextLength)
            {
                log.LogMetric("RequestTooLong", 1);
                return new OkObjectResult(MakeResultObject("Your input is too long. Try a shorter one."));
            }

            request.Text = request.Text.Trim() + "\n";

            var client = new OpenApiClient(Environment.GetEnvironmentVariable("OpenApiKey"), log)
            {
                MaxTokens = int.Parse(Environment.GetEnvironmentVariable("MaxCompletionTokens"))
            };

            string prefix = Environment.GetEnvironmentVariable("Prefix");
            //Rewrite the following into a {Short} {Style} {Target} in {Language} language:\n\n

            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            log.LogDebug($"userId: '{userId}'");

            var personalization = await UserPersonalizationApi.GetUserPersonalization(userId);

            prefix = prefix
                .Replace("{Short}", personalization.Short ? "short" : "")
                .Replace("{Style}", personalization.Style)
                .Replace("{Target}", personalization.Target)
                .Replace("{Language}", personalization.Language);

            bool stub = bool.TryParse(Environment.GetEnvironmentVariable("UseStub"), out stub) ? stub : false;

            var completion = stub
                ? client.GenerateCompletionStub(request.Text)
                : await client.GenerateCompletion(prefix.Replace("\\n", "\n") + request.Text);

            string id = Hash(completion.Id);
            string partition = DateTime.UtcNow.ToString("yyyy-MM-dd");

            await StoreRewriteLog(partition, id, prefix, request, completion);

            return new OkObjectResult(MakeResultObject(partition: partition, id: id, text: completion.Text));
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
            int length = await new StreamReader(req.Body).ReadAsync(buffer, 0, maxRequestLength);
            
            if (length >= maxRequestLength)
            {
                log.LogWarning("Request is too long.");
                return new OkObjectResult(MakeResultObject("Your input is too long. Try a shorter one."));
            }

            var client = await GetTableClient();

            ReportProblemRequest request = null;
            var requestText = new string(buffer);
            
            try
            {
                request = JsonConvert.DeserializeObject<ReportProblemRequest>(requestText);
            }
            catch (Exception x)
            {
                string requestId = Guid.NewGuid().ToString();
                string partition = DateTime.UtcNow.ToString("yyyy-MM-dd");

                var problemEntity = new TableEntity(partition, requestId);
                problemEntity.Add("ProblemInput", requestText);
                problemEntity.Add("Problemoutput", x.Message);

                await client.UpsertEntityAsync(problemEntity);
                return new OkResult();
            }

            if (request == null)
            {
                log.LogWarning("request is null.");
                return new BadRequestResult();
            }

            var entity = new TableEntity(request.Partition, request.Id);
            entity.Add("ProblemInput", request.Text);
            entity.Add("Problemoutput", request.Rewritten);

            await client.UpsertEntityAsync(entity);

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

        private static object MakeResultObject(string text, string partition = null, string id = null) =>
            new { 
                    Partition = partition ?? DateTime.UtcNow.ToString("yyyy-MM-dd"),
                    Id = id ?? Guid.NewGuid().ToString(),
                    Text = text 
                };

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
