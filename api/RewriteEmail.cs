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

namespace Editor
{
    public static class RewriteEmail
    {
        [FunctionName("RewriteEmail")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            
            if (data == null)
            {
                return new BadRequestResult();
            }

            string text = data.text;

            string responseText = "";
            if(text != null)
            {
                responseText = string.Join('\n', text.Split('\n').Reverse());
            }

            if (data.allowLog)
            {
                await StoreRewrite(text, responseText);
            }

            return new OkObjectResult(new { Text = responseText });
        }

        private static async Task StoreRewrite(string input, string output)
        {
            var storageConnectionString = Environment.GetEnvironmentVariable("StorageConnectionString");

            var client = new TableClient(storageConnectionString, "rewrites");

            await client.CreateIfNotExistsAsync();

            var entity = new TableEntity("partition", DateTime.UtcNow.ToString("s"))
            {
                { "input", input },
                { "output", output }
            };

            await client.AddEntityAsync(entity);
        }
    }
}
