using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Azure.Management.Consumption.Models;

namespace AzureConsumptionVerification
{
    class ConsumptionAnalyzer
    {
        private readonly ActivityLogProvider _activityLogProvider;

        public ConsumptionAnalyzer(ActivityLogProvider activityLogProvider)
        {
            _activityLogProvider = activityLogProvider;
        }

        public ConsumptionAnalysisReport AnalyzeConsumptionForDeletedResources(IList<UsageDetail> usage)
        {
            var report = new ConsumptionAnalysisReport();
            foreach (var usageDetail in usage)
            {
                report.Resourceses.Add(new BilledResources()
                {
                    Currency = usageDetail.Currency,
                    Id = usageDetail.Id,
                    InstanceName = usageDetail.InstanceName,
                    PretaxCost = usageDetail.PretaxCost,
                    ConsumedService = usageDetail.ConsumedService,
                    SubscriptionName = usageDetail.SubscriptionName,
                    UsageStart = usageDetail.UsageStart,
                    UsageEnd = usageDetail.UsageEnd,
                    UsaUsageQuantity = usageDetail.UsageQuantity
                });
            }

            return report;
        }
    }
}
