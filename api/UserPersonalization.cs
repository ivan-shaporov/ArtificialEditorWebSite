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
using System.Net.Http;
using System.Net.Http.Headers;

namespace Editor
{
    public static class UserPersonalizationApi
    {
        private static readonly HttpClient httpClient = new HttpClient();

        [FunctionName("UserPersonalizationPing")]
        public static IActionResult RunUserPersonalizationPing(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            return new OkObjectResult("OK");
        }

        [FunctionName("UserPersonalization")]
        public static async Task<IActionResult> RunUserPersonalization(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "delete", Route = null)] HttpRequest req,
            ILogger log)
        {
            var principal = StaticWebAppsAuth.Parse(req);

            if (!principal.IsInRole("authenticated"))
            {
                return new UnauthorizedResult();
            }
            
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier).Value;

            var table = await GetTableClient();

            string method = req.Method.ToLowerInvariant();

            if (method == "get")
            {
                return await GetUserPersonalization(table, userId);
            }
            else if (method == "post")
            {
                return await SaveUserPersonalization(req, table, userId);
            }
            else if (method == "delete")
            {
                var oktaDomain = "virtueditor.com";
                log.LogTrace($"https://${oktaDomain}/api/v1/users/{userId}?sendEmail=true");
                return await DeleteUserPersonalization(table, userId);
            }
            else
            {
                return new BadRequestObjectResult($"Method '{req.Method}' not supported");
            }
        }

        private static async Task<OkObjectResult> GetUserPersonalization(TableClient table, string userId)
        {
            var record = await table.GetEntityIfExistsAsync<TableEntity>(userId, "Personalization");

            var result = record.HasValue ? (UserPersonalization)record.Value: UserPersonalization.Default;

            return new OkObjectResult(result);
        }

        public static async Task<IActionResult> SaveUserPersonalization(HttpRequest req, TableClient table, string userId)
        {
            var maxLength = 100;
            var buffer = new char[maxLength];
            int length = await new StreamReader(req.Body).ReadAsync(buffer, 0, maxLength);
            
            if (length >= maxLength)
            {
                return new BadRequestObjectResult("Request too long");
            }

            var request = JsonConvert.DeserializeObject<UserPersonalization>(new string(buffer));

            var entity = request.MakeTableEntity(userId);

            await table.UpsertEntityAsync(entity);

            return new OkResult();
        }

        public static async Task<IActionResult> DeleteUserPersonalization(TableClient table, string userId)
        {
            await table.DeleteEntityAsync(userId, "Personalization");

            var oktaDomain = "virtueditor.com";

            var oktaToken = Environment.GetEnvironmentVariable("OKTA_API_TOKEN");

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("SSWS", oktaToken);

            await httpClient.DeleteAsync($"https://${oktaDomain}/api/v1/users/{userId}?sendEmail=true");

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

            public static UserPersonalization Default => new UserPersonalization { Short = true, Style = "Friendly", Language = "English" };
            public TableEntity MakeTableEntity(string userId) => 
                new TableEntity(userId, "Personalization") 
                { 
                    { "Language", Language},
                    { "Style", Style},
                    { "Short", Short},
                };

            public static explicit operator UserPersonalization(TableEntity record) => new UserPersonalization 
            { 
                Short = record.GetBoolean("Short") ?? false,
                Style = record.GetString("Style"),
                Language = record.GetString("Language")
            };
        }
    }
}
