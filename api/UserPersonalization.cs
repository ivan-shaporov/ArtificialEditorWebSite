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
    public static class UserPersonalizationApi
    {
        [FunctionName("GetUserPersonalization")]
        public static async Task<IActionResult> RunGetUserPersonalization(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            var table = await GetTableClient();
            //table.GetEntityAsync()
            return new OkObjectResult(new { Short = false, Style = "Friendly", Language = "English" });
        }

        [FunctionName("SaveUserPersonalization")]
        public static async Task<IActionResult> RunSaveUserPersonalization(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var maxLength = 100;
            var buffer = new char[maxLength];
            int length = await new StreamReader(req.Body).ReadAsync(buffer, 0, maxLength);
            
            if (length >= maxLength)
            {
                return new BadRequestResult();
            }

            var request = JsonConvert.DeserializeObject<UserPersonalization>(new string(buffer));

            var table = await GetTableClient();
            //table.GetEntityAsync()
            return new OkObjectResult(new { Short = false, Style = "Friendly", Language = "English" });
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
