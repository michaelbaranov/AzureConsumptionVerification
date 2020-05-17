using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Monitor.Fluent.Models;
using Newtonsoft.Json.Linq;

namespace AzureConsumptionVerification
{
    internal class ActivityLogProvider
    {
        private readonly CustomCredentials _credentials;
        private readonly string _subscriptionId;

        public ActivityLogProvider(CustomCredentials credentials, string subscriptionId)
        {
            _credentials = credentials;
            _subscriptionId = subscriptionId;
        }

        private async Task<IEnumerable<EventData>> GetEvents(string filter)
        {
            var select =
                "eventName,id,resourceGroupName,resourceProviderName,operationName,status,eventTimestamp,correlationId,submissionTimestamp,level";
            var uri =
                new Uri(
                    $"https://management.azure.com/subscriptions/{_subscriptionId}/providers/microsoft.insights/eventtypes/management/values?api-version=2015-04-01&$filter={filter}&$select={select}");
            var message = new HttpRequestMessage(HttpMethod.Get, uri);
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _credentials.GetToken());

            var response = await RestService.SendAsync(message);
            var jObject = JObject.Parse(response);
            var results = new List<EventData>();
            foreach (var eventRecord in jObject["value"].Children().ToList())
                results.Add(eventRecord.ToObject<EventData>());

            return results;
        }

        private async Task<IEnumerable<EventData>> GetEventsForResource(string resourceId)
        {
            var dateFrom = DateTime.UtcNow.AddDays(-89).ToString("yyyy-MM-ddThh:mm:ssZ");
            var dateTo = DateTime.UtcNow.ToString("yyyy-MM-ddThh:mm:ssZ");
            var filter =
                $"eventTimestamp ge '{dateFrom}' and eventTimestamp le '{dateTo}' and resourceId eq '{resourceId}'";

            return await GetEvents(filter);
        }

        private async Task<IEnumerable<EventData>> GetEventsForResourceGroup(string resourceGroupName)
        {
            var dateFrom = DateTime.UtcNow.AddDays(-89).ToString("yyyy-MM-ddThh:mm:ssZ");
            var dateTo = DateTime.UtcNow.ToString("yyyy-MM-ddThh:mm:ssZ");
            var filter =
                $"eventTimestamp ge '{dateFrom}' and eventTimestamp le '{dateTo}' and resourceGroupName eq '{resourceGroupName}'";

            return await GetEvents(filter);
        }

        public async Task<EventData> GetResourceGroupDeletionDate(string resourceGroupName)
        {
            var events = await GetEventsForResourceGroup(resourceGroupName);
            var deleteEvents = events.Where(e =>
                string.Equals(e.OperationName.Value, "Microsoft.Resources/subscriptions/resourcegroups/delete",
                    StringComparison.OrdinalIgnoreCase)
                && e.Status.Value == "Succeeded").ToList();

            return deleteEvents.FirstOrDefault();
        }

        public async Task<EventData> GetResourceDeletionDate(string resourceId)
        {
            var events = await GetEventsForResource(resourceId);
            var deleteEvents = events.Where(e =>
                e.OperationName.Value.EndsWith("delete") && e.Status.Value == "Succeeded").ToList();

            return deleteEvents.FirstOrDefault();
        }
    }
}