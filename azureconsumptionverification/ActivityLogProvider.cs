using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Monitor.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Rest;

namespace AzureConsumptionVerification
{
    internal class ActivityLogProvider
    {
        private readonly ServiceClientCredentials _credentials;
        private readonly string _subscription;

        public ActivityLogProvider(ServiceClientCredentials credentials, string subscription)
        {
            _credentials = credentials;
            _subscription = subscription;
        }

        private IEnumerable<IEventData> GetEventsForResource(string resourceId)
        {
            return Azure
                .Configure()
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .Authenticate(((CustomCredentials)_credentials).ToAzureCredentials())
                .WithSubscription(_subscription)
                .ActivityLogs.DefineQuery()
                .StartingFrom(DateTime.UtcNow.AddDays(-90))
                .EndsBefore(DateTime.UtcNow)
                .WithResponseProperties(EventDataPropertyName.EventTimestamp, EventDataPropertyName.OperationName,
                    EventDataPropertyName.Status, EventDataPropertyName.OperationId)
                .FilterByResource(resourceId)
                .ExecuteAsync()
                .GetAwaiter()
                .GetResult();
        }

        private IEnumerable<IEventData> GetEventsForResourceGroup(string resourceGroupName)
        {
            return Azure
                .Configure()
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .Authenticate(((CustomCredentials)_credentials).ToAzureCredentials())
                .WithSubscription(_subscription)
                .ActivityLogs.DefineQuery()
                .StartingFrom(DateTime.UtcNow.AddDays(-90))
                .EndsBefore(DateTime.UtcNow)
                .WithResponseProperties(EventDataPropertyName.EventTimestamp, EventDataPropertyName.OperationName,
                    EventDataPropertyName.Status, EventDataPropertyName.OperationId)
                .FilterByResourceGroup(resourceGroupName)
                .ExecuteAsync()
                .GetAwaiter()
                .GetResult();
        }

        public DeleteOperation GetResourceGroupDeletionDate(string resourceGroupName)
        {
            var events = GetEventsForResourceGroup(resourceGroupName);
            var deleteEvents = events.Where(e =>
                string.Equals(e.OperationName.Value, "Microsoft.Resources/subscriptions/resourcegroups/delete", StringComparison.OrdinalIgnoreCase)
                && e.Status.Value == "Succeeded").ToList();

            var eventData = deleteEvents.FirstOrDefault();

            return eventData == null
                ? null
                : new DeleteOperation { OperationId = eventData.OperationId, Date = eventData.EventTimestamp };
        }

        public DeleteOperation GetResourceDeletionDate(string resourceId)
        {
            var events = GetEventsForResource(resourceId);
            var deleteEvents = events.Where(e =>
                e.OperationName.Value.EndsWith("delete") && e.Status.Value == "Succeeded").ToList();

            var eventData = deleteEvents.FirstOrDefault();

            return eventData == null
                ? null
                : new DeleteOperation {OperationId = eventData.OperationId, Date = eventData.EventTimestamp};
        }
    }

    internal class DeleteOperation
    {
        public DateTime? Date;
        public string OperationId;
    }
}