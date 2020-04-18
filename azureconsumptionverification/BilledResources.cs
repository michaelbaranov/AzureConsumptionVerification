using System;

namespace AzureConsumptionVerification
{
    internal class BilledResources
    {
        public string ConsumedService;
        public string Currency;
        public string Id;
        public string InstanceName;
        public decimal? PretaxCost;
        public decimal? UsaUsageQuantity;
        public string SubscriptionName;
        public DateTime? UsageEnd;
        public DateTime? UsageStart;
        public DateTime? ActivityCreated;
        public DateTime? ActivityDeleted;
        public string DeleteOperationId;
        public decimal? Overage;
    }
}