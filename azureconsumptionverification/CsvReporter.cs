using System;
using System.IO;
using System.Linq;

namespace AzureConsumptionVerification
{
    internal class CsvReporter
    {
        public static string WriteReport(ConsumptionAnalysisReport report, string subscriptionId)
        {
            var reportFileName = GetTempFileName(subscriptionId);
            using (var writer = File.CreateText(reportFileName))
            {
                var resources = report.GetResources();
                var currency = resources.GroupBy(r => r.Currency).FirstOrDefault().Key;

                writer.WriteLine(
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
                    writer.WriteLine($"{billedResource.InstanceName.Replace(',','_')}," +
                                     $"{billedResource.Id.Replace(',', '_')}," +
                                     $"{billedResource.ConsumedService}," +
                                     $"{billedResource.PretaxCost}," +
                                     $"{billedResource.UsageStart}," +
                                     $"{billedResource.UsageEnd}," +
                                     $"{billedResource.ActivityDeleted}," +
                                     $"{billedResource.DeleteOperationId}," +
                                     $"{billedResource.Overage}");
            }

            return reportFileName;
        }

        private static string GetTempFileName(string subscriptionId)
        {
            return Path.Combine(Path.GetTempPath(), $"consumption_{subscriptionId}_{new Random().Next(1000)}.csv");
        }
    }
}