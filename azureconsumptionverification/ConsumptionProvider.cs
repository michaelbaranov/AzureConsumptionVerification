using System.Collections.Generic;
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

        public async Task<IList<UsageDetail>> GetConsumptionAsync()
        {
            var response = _client.UsageDetails.ListByBillingPeriodAsync("20200301");

            var usageDetails = new List<UsageDetail>();

            do
            {
                var result = await response;
                usageDetails.AddRange(result.ToList());
                if (string.IsNullOrEmpty(result.NextPageLink)) return usageDetails;
                response = _client.UsageDetails.ListNextAsync(result.NextPageLink);
            } while (true);
        }
    }
}