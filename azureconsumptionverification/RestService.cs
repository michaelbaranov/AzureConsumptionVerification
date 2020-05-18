using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace AzureConsumptionVerification
{
    internal class RestService
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public static async Task<string> SendAsync(HttpRequestMessage message)
        {
            var response = await _httpClient.SendAsync(message);
            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();

                var jObject = JObject.Parse(responseContent);
                throw new RestException("Not successful status code", response.StatusCode,
                    jObject["code"].Value<string>(), jObject["message"].Value<string>());
            }

            return await response.Content.ReadAsStringAsync();
        }
    }
}