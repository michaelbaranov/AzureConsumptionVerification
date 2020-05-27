using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Rest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AzureConsumptionVerification
{
    public class CustomCredentials : ServiceClientCredentials
    {
        private const string AuthenticationContext = "https://login.windows.net/";
        private const string GrantType = "client_credentials";
        private const string Scope = "user_impersonation";
        private const string Resource = "https://management.azure.com/";
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly object _lock = new object();
        private readonly string _tenantId;
        private string _authenticationToken;
        private Token token;

        public CustomCredentials(string clientId, string clientSecret, string tenantId)
        {
            _clientId = clientId;
            _clientSecret = clientSecret;
            _tenantId = tenantId;
        }

        public override async Task ProcessHttpRequestAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GetToken());
            await base.ProcessHttpRequestAsync(request, cancellationToken);
        }

        public override void InitializeServiceClient<T>(ServiceClient<T> client)
        {
            _authenticationToken = GetToken();
        }

        public AzureCredentials ToAzureCredentials()
        {
            return SdkContext.AzureCredentialsFactory.FromServicePrincipal(_clientId, _clientSecret, _tenantId,
                AzureEnvironment.AzureGlobalCloud);
        }

        public string GetToken()
        {
            if (token == null || token.IsExpired)
                lock (_lock)
                {
                    var formData = new Dictionary<string, string>
                    {
                        {"grant_type", GrantType},
                        {"client_id", _clientId},
                        {"client_secret", _clientSecret},
                        {"resource", Resource},
                        {"scope", Scope}
                    };

                    var uri = new Uri($"https://login.microsoftonline.com/{_tenantId}/oauth2/token");
                    var message = new HttpRequestMessage(HttpMethod.Post, uri)
                    {
                        Content = new FormUrlEncodedContent(formData)
                    };
                    var response = RestService.SendAsync(message).GetAwaiter().GetResult();
                    var responseContent = response.Content.ReadAsStringAsync().Result;
                    var jObject = JObject.Parse(responseContent);
                    if (!response.IsSuccessStatusCode)
                        throw new RestException("Not successful status code", response.StatusCode,
                            jObject["code"].Value<string>(), jObject["message"].Value<string>());
                    token = JsonConvert.DeserializeObject<Token>(responseContent);
                }

            return token.AccessToken;
        }

        internal class Token
        {
            private readonly DateTime _obtainedAt = DateTime.UtcNow;

            [JsonProperty("access_token")] public string AccessToken { get; set; }

            [JsonProperty("token_type")] public string TokenType { get; set; }

            [JsonProperty("expires_in")] public int ExpiresIn { get; set; }

            [JsonProperty("refresh_token")] public string RefreshToken { get; set; }

            public bool IsExpired => DateTime.UtcNow >= _obtainedAt.AddSeconds(ExpiresIn);
        }
    }
}