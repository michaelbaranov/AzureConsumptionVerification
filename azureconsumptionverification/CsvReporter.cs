using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using File = System.IO.File;

namespace AzureConsumptionVerification
{
    class CsvReporter
    {
        public static string WriteReport(ConsumptionAnalysisReport report)
        {
            var reportFileName = GetTempFileName();
            using (var writer = File.CreateText(reportFileName))
            {
                writer.WriteLine("Resource name,Resource ID,Service Type,Cost,Billing Start,Billing End,Created,Deleted,PretaxCost,UsageQuantity");
                foreach (var billedResource in report.Resourceses)
                {
                    writer.WriteLine($"{billedResource.InstanceName},{billedResource.Id},{billedResource.ConsumedService},{billedResource.PretaxCost?.ToString("0.00")} {billedResource.Currency},{billedResource.UsageStart},{billedResource.UsageEnd},{billedResource.ActivityCreated}, {billedResource.ActivityDeleted}, {billedResource.PretaxCost}, {billedResource.UsaUsageQuantity}");
                }
            }

            return reportFileName;
        }

        private static string GetTempFileName()
        {
            return Path.Combine(Path.GetTempPath(), $"consumption_{new Random().Next(1000)}.csv");
        }
    }
}
