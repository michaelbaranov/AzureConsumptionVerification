using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AzureConsumptionVerification
{
    internal class ResourceProvider
    {
        private readonly CustomCredentials _credentials;
        private readonly string _subscriptionId;
        private readonly object _lock = new object();
        private List<AzureProvider> _resourceProviders;

        public ResourceProvider(CustomCredentials credentials, string subscriptionId)
        {
            _credentials = credentials;
            _subscriptionId = subscriptionId;
        }

        private async Task<List<AzureProvider>> GetResourceProviders(string subscriptionId)
        {
            var uri = $"https://management.azure.com/subscriptions/{subscriptionId}/providers?api-version=2015-01-01";
            var message = new HttpRequestMessage(HttpMethod.Get, uri);
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _credentials.GetToken());
            var response = await RestService.SendAsync(message);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            var jObject = JObject.Parse(responseContent);
            var azureProviders = new List<AzureProvider>();
            foreach (var resourceNamespace in jObject["value"].Children())
            {
                var provider = resourceNamespace.ToObject<AzureProvider>();
                azureProviders.Add(provider);
            }

            return azureProviders;
        }

        public string GetApiVersionForResourceProvider(string providerNamespace, string resourceType)
        {
            if (_resourceProviders == null)
                lock (_lock)
                {
                    _resourceProviders = GetResourceProviders(_subscriptionId).Result;
                }

            return _resourceProviders.First(p =>
                    string.Compare(p.Namespace, providerNamespace, StringComparison.OrdinalIgnoreCase) == 0)
                .ResourceTypes.First(t =>
                    string.Compare(t.ResourceType, resourceType, StringComparison.InvariantCultureIgnoreCase) == 0)
                .ApiVersions.Max();
        }

        public async Task<bool?> IsResourceExists(string resourceId)
        {
            var match = new Regex(@".*/providers/([A-Za-z\.]*)/([A-Za-z\.]*)").Match(resourceId);
            var providerNamespace = match.Groups[1].Value;
            var type = match.Groups[2].Value;
            var apiVersion = GetApiVersionForResourceProvider(providerNamespace, type);

            var uri = $"https://management.azure.com/{resourceId}?api-version={apiVersion}";
            var message = new HttpRequestMessage(HttpMethod.Get, uri);
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _credentials.GetToken());
            var response = await RestService.SendAsync(message);

            switch (response.StatusCode)
            {
                case HttpStatusCode.NotFound:

                {
                    return false;
                }
                case HttpStatusCode.OK:
                {
                    return true;
                }
                default:
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var jObject = JObject.Parse(responseContent);
                    throw new RestException(
                        !response.IsSuccessStatusCode
                            ? "Resource Management API error"
                            : "Resource Management API returned not expected status code",
                        response.StatusCode, jObject["error"]["code"].Value<string>(),
                        jObject["error"]["message"].Value<string>());
                }
            }
        }
    }

    internal class AzureProvider
    {
        [JsonProperty("namespace")] public string Namespace;

        [JsonProperty("resourceTypes")] public IEnumerable<AzureProviderResourceType> ResourceTypes;
    }

    internal class AzureProviderResourceType
    {
        [JsonProperty("apiVersions")] public IEnumerable<string> ApiVersions;

        [JsonProperty("resourceType")] public string ResourceType;
    }
}