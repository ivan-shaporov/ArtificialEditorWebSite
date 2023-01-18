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
using System.Security.Claims;

namespace Editor
{
    public static class UserPersonalizationApi
    {
        [FunctionName("GetUserPersonalization")]
        public static async Task<IActionResult> RunGetUserPersonalization(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            var principal = StaticWebAppsAuth.Parse(req);

            if (!principal.IsInRole("authenticated"))
            {
                return new UnauthorizedResult();
            }

            var table = await GetTableClient();
            
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier).Value;
            var record = await table.GetEntityIfExistsAsync<TableEntity>(userId, "Personalization");

            if (!record.HasValue)
            {
                return new OkObjectResult(new { Short = false, Style = "Friendly", Language = "English" });
            }

            return new OkObjectResult(new 
            { 
                Short = record.Value.GetBoolean("Short"),
                Style = record.Value.GetString("Style"),
                Language = record.Value.GetString("Language")
            });
        }

        [FunctionName("PingUserPersonalization")]
        public static IActionResult RunPingUserPersonalization(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            return new OkObjectResult("OK");
        }

        [FunctionName("SaveUserPersonalization")]
        public static async Task<IActionResult> RunSaveUserPersonalization(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var principal = StaticWebAppsAuth.Parse(req);

            if (!principal.IsInRole("authenticated"))
            {
                return new UnauthorizedResult();
            }

            var maxLength = 100;
            var buffer = new char[maxLength];
            int length = await new StreamReader(req.Body).ReadAsync(buffer, 0, maxLength);
            
            if (length >= maxLength)
            {
                return new BadRequestResult();
            }

            var request = JsonConvert.DeserializeObject<UserPersonalization>(new string(buffer));

            var table = await GetTableClient();

            var userId = principal.FindFirst(ClaimTypes.NameIdentifier).Value;
            var entity = new TableEntity(userId, "Personalization")
            {
                { "Language", request.Language},
                { "Style", request.Style},
                { "Short", request.Short},
            };

            await table.UpsertEntityAsync(entity);
            return new OkResult();
        }

        private static async Task<TableClient> GetTableClient()
        {
            var storageConnectionString = Environment.GetEnvironmentVariable("StorageConnectionString");
            var client = new TableClient(storageConnectionString, "userpersonalization");
            await client.CreateIfNotExistsAsync();
            return client;
        }

        class UserPersonalization
        {
            public string Language { get; set; }
            public string Style { get; set; }
            public bool Short { get; set; }
        }
    }
}
