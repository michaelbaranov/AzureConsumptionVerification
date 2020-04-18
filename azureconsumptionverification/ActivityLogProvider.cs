using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Monitor.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Rest;
using Microsoft.Rest.Azure.OData;

namespace AzureConsumptionVerification
{
    class ActivityLogProvider
    {
        private readonly ServiceClientCredentials _credentials;
        private readonly string _subscription;
        private IEnumerable<IEventData> _eventData;
        private IAzure _azure;

        public ActivityLogProvider(ServiceClientCredentials credentials, string subscription)
        {
            _credentials = credentials;
            _subscription = subscription;
            _azure = Microsoft.Azure.Management.Fluent.Azure
                .Configure()
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .Authenticate(((CustomCredentials)_credentials).ToAzureCredentials())
                .WithSubscription(_subscription);
        }

        private IEnumerable<IEventData> GetEventsForResource(string resourceId)
        {
            return _eventData = _azure.ActivityLogs.DefineQuery()
                .StartingFrom(DateTime.UtcNow.AddDays(-90))
                .EndsBefore(DateTime.UtcNow)
                .WithResponseProperties(EventDataPropertyName.EventTimestamp, EventDataPropertyName.OperationName, EventDataPropertyName.Status, EventDataPropertyName.OperationId)
                .FilterByResource(resourceId)
                .ExecuteAsync()
                .GetAwaiter()
                .GetResult();
        }

        public DeleteOperation GetResourceDeletionDate(string resourceId)
        {
            var events = GetEventsForResource(resourceId);
            var deleteEvents = events.Where(e =>
                e.OperationName.Value.EndsWith("delete") && e.Status.Value == "Succeeded").ToList();

            var eventData = deleteEvents.FirstOrDefault();

            return eventData == null ? null : new DeleteOperation() {OperationId = eventData.OperationId, Date = eventData.EventTimestamp};
        }
    }

    internal class DeleteOperation
    {
        public string OperationId;
        public DateTime? Date;
    }
}
