using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace AzureConsumptionVerification
{
    internal class RestService
    {
        private static readonly HttpClient _httpClient;

        static RestService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(5);
        }

        public static async Task<HttpResponseMessage> SendAsync(HttpRequestMessage message)
        {
            return await _httpClient.SendAsync(message);
        }
    }
}