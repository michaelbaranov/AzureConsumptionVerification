using System.IO;
using System.Linq;

namespace AzureConsumptionVerification
{
    internal class CsvReporter
    {
        public static void WriteReport(ConsumptionAnalysisReport report, string outputFile)
        {
            using var writer = File.CreateText(outputFile);
            var resources = report.GetResources();
            var currency = resources.GroupBy(r => r?.Currency).FirstOrDefault()?.Key;

            writer.WriteLine(
                $"SubscriptionId," +
                $"Resource name," +
                $"Resource ID," +
                $"Service Type," +
                $"Cost {currency}," +
                $"Billing Start," +
                $"Billing End," +
                $"Actual resource deletion date," +
                $"DeleteOperationId," +
                $"Overage {currency}");
            foreach (var billedResource in resources)
                writer.WriteLine($"{billedResource.SubscriptionId}," + 
                                 $"{billedResource.InstanceName.Replace(',','_')}," +
                                 $"{billedResource.Id.Replace(',', '_')}," +
                                 $"{billedResource.ConsumedService}," +
                                 $"{billedResource.PretaxCost}," +
                                 $"{billedResource.UsageStart}," +
                                 $"{billedResource.UsageEnd}," +
                                 $"{billedResource.ActivityDeleted}," +
                                 $"{billedResource.DeleteOperationId}," +
                                 $"{billedResource.Overage}");
        }
    }
}