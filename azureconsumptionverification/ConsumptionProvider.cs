using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Consumption;
using Microsoft.Azure.Management.Consumption.Models;
using Microsoft.Rest;

namespace AzureConsumptionVerification
{
    public class ConsumptionProvider
    {
        private readonly ConsumptionManagementClient _client;

        public ConsumptionProvider(ServiceClientCredentials credentials, string subscription)
        {
            _client = new ConsumptionManagementClient(credentials) {SubscriptionId = subscription};
        }

        public async Task<IList<UsageDetail>> GetConsumptionAsync(int numberOfMonths)
        {
            Console.WriteLine("Obtaining Billing information, this might take a while");
            var usageDetails = new List<UsageDetail>();

            for (var i = 0; i < numberOfMonths; i++)
            {
                var billingPeriodName = DateTime.UtcNow.AddMonths(-i).ToString("yyyyMM01");
                Console.WriteLine($"Getting data for billing period {billingPeriodName}");
                var response = _client.UsageDetails.ListByBillingPeriodAsync(billingPeriodName);

                do
                {
                    var result = await response;
                    usageDetails.AddRange(result.ToList());
                    Console.WriteLine($"Obtaining Billing information, done {usageDetails.Count} records received");

                    if (string.IsNullOrEmpty(result.NextPageLink)) break;

                    response = _client.UsageDetails.ListNextAsync(result.NextPageLink);
                } while (true);
            }

            return usageDetails;
        }
    }
}