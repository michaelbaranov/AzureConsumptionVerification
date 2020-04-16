using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Monitor.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Rest;

namespace AzureConsumptionVerification
{
    class ActivityLogProvider
    {
        private IAzure _azure;

        public ActivityLogProvider(ServiceClientCredentials credentials, string subscription)
        {
            this._azure = Azure
                .Configure()
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .Authenticate(((CustomCredentials)credentials).ToAzureCredentials())
                .WithSubscription(subscription);
        }

        public async Task<IEnumerable<IEventData>> GetActivityLogs(string resourceGroupName)
        {
            return await _azure.ActivityLogs.DefineQuery()
                .StartingFrom(DateTime.UtcNow.AddMonths(-3))
                .EndsBefore(DateTime.UtcNow)
                .WithAllPropertiesInResponse()
                .FilterByResourceGroup(resourceGroupName)
                .ExecuteAsync();
        }
    }
}
