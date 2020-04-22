using System;
using Microsoft.Azure.Management.ContainerRegistry.Fluent.Models;

namespace AzureConsumptionVerification
{
    internal class BilledResources
    {
        public string SubscriptionId;
        public DateTime? ActivityDeleted;
        public string ConsumedService;
        public string Currency;
        public string DeleteOperationId;
        public string Id;
        public string InstanceName;
        public decimal? Overage;
        public decimal? PretaxCost;
        public string SubscriptionName;
        public DateTime? UsageEnd;
        public DateTime? UsageStart;
    }
}