using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;

namespace AzureConsumptionVerification
{
    public class CustomCredentials : ServiceClientCredentials
    {
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _tenantId;
        private string _authenticationToken;

        public CustomCredentials(string clientId, string clientSecret, string tenantId)
        {
            _clientId = clientId;
            _clientSecret = clientSecret;
            _tenantId = tenantId;
        }

        public override async Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authenticationToken);
            await base.ProcessHttpRequestAsync(request, cancellationToken);
        }

        public override void InitializeServiceClient<T>(ServiceClient<T> client)
        {
            var authenticationContext =
                new AuthenticationContext("https://login.windows.net/" + _tenantId);
            var credential = new ClientCredential(_clientId, _clientSecret);

            var result = authenticationContext.AcquireTokenAsync("https://management.azure.com/", credential).Result;

            _authenticationToken = result.AccessToken;
        }

        public AzureCredentials ToAzureCredentials()
        {
            return SdkContext.AzureCredentialsFactory.FromServicePrincipal(_clientId, _clientSecret, _tenantId,
                AzureEnvironment.AzureGlobalCloud);
        }
    }
}
