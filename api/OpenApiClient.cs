using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace OpenApi
{
    public class OpenApiClient
    {
        public int MaxTokens { get; set; } = 256;
        public Uri Url { get; set; } = new Uri("https://api.openai.com/v1/completions");

        private static readonly HttpClient client = new HttpClient();

        public OpenApiClient(string key) => 
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);

        public Completion GenerateCompletionStub(string prompt) => new Completion(null)
        {
            Id = "stub", 
            Text = prompt
        };

        public async Task<Completion> GenerateCompletion(string prompt)
        {
            var request = new
            {
                model = "text-davinci-003",
#pragma warning disable IDE0037 // Use inferred member name
                prompt = prompt,
#pragma warning restore IDE0037 // Use inferred member name
                max_tokens = MaxTokens,
                temperature = 0.7
            };

            string requestBody = JsonConvert.SerializeObject(request);

            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            var reply = await client.PostAsync(Url, content);

            reply.EnsureSuccessStatusCode();

            var jsonString = await reply.Content.ReadAsStringAsync();
            var response = JsonConvert.DeserializeObject<OpenApiResponse>(jsonString);

            var result = new Completion(response);

            return result;
        }

        internal class OpenApiResponse
        {
            public string Id { get; set; } //"cmpl-uqkvlQyYK7bGYrRHQ0eXlWi7",
            public string Object { get; set; } // "text_completion",
            public ulong Created { get; set; } //  1589478378
            public string Model { get; set; } //"text-davinci-003"
            public OpenApiChoice[] Choices { get; set; }
            public OpenApiUsage Usage { get; set; }
        }

        internal class OpenApiChoice
        {
            public string Text { get; set; } // "\n\nThis is indeed a test",
            public int Index { get; set; } // 0,
            public string Logprobs { get; set; } // null,
            public string Finish_reason { get; set; } // "length"
        }

        internal class OpenApiUsage
        {
            public int Prompt_tokens { get; set; } // 5,
            public int Completion_tokens { get; set; } // 7,
            public int Total_tokens { get; set; } // 12
        }
    }
    public class Completion
    {
        internal Completion(OpenApiClient.OpenApiResponse response)
        {
            Id = response?.Id;
            Text = response?.Choices.FirstOrDefault()?.Text.Trim();
            PromptTokens = response?.Usage.Prompt_tokens ?? 0;
            CompletionTokens = response?.Usage.Completion_tokens ?? 0;
            TotalTokens = response?.Usage.Total_tokens ?? 0;
        }

        public string Id { get; set; } //"cmpl-uqkvlQyYK7bGYrRHQ0eXlWi7"
        public string Text { get; set; } // "\n\nThis is indeed a test"
        public int PromptTokens { get; set; } // 5,
        public int CompletionTokens { get; set; } // 7,
        public int TotalTokens { get; set; } // 12
    }
}
