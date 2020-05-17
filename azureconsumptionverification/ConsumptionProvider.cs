using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Consumption;
using Microsoft.Azure.Management.Consumption.Models;
using Microsoft.Rest;
using Serilog;

namespace AzureConsumptionVerification
{
    public class ConsumptionProvider
    {
        private readonly ConsumptionManagementClient _client;
        private readonly string _subscriptionId;

        public ConsumptionProvider(ServiceClientCredentials credentials, string subscriptionId)
        {
            _subscriptionId = subscriptionId;
            _client = new ConsumptionManagementClient(credentials) {SubscriptionId = _subscriptionId};
        }

        public async Task<IList<UsageDetail>> GetConsumptionAsync(int numberOfMonths)
        {
            Log.Information($"Subscription {_subscriptionId}. Obtaining Billing information");
            var usageDetails = new List<UsageDetail>();

            for (var i = 0; i < numberOfMonths; i++)
            {
                var billingPeriodName = DateTime.UtcNow.AddMonths(-i).ToString("yyyyMM01");
                Log.Information($"Subscription {_subscriptionId}. Getting data for billing period {billingPeriodName}");
                var response = _client.UsageDetails.ListByBillingPeriodAsync(billingPeriodName);

                do
                {
                    var result = await response;
                    usageDetails.AddRange(result.ToList());
                    Log.Information(
                        $"Subscription {_subscriptionId}. Obtaining Billing information, done {result.Count()} records received, total {usageDetails.Count}");

                    if (string.IsNullOrEmpty(result.NextPageLink)) break;

                    response = _client.UsageDetails.ListNextAsync(result.NextPageLink);
                } while (true);
            }

            return usageDetails;
        }
    }
}