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
                writer.WriteLine("Resource name,Resource ID,Service Type,Cost,Currency,Billing Start,Billing End,Deleted,DeleteOperationId,Overage");
                foreach (var billedResource in report.GetResources())
                {
                    writer.WriteLine($"{billedResource.InstanceName}," +
                                     $"{billedResource.Id}," +
                                     $"{billedResource.ConsumedService}," +
                                     $"{billedResource.PretaxCost?.ToString("0.00")}," +
                                     $"{billedResource.Currency}," +
                                     $"{billedResource.UsageStart}," +
                                     $"{billedResource.UsageEnd}," +
                                     $"{billedResource.ActivityDeleted}," +
                                     $"{billedResource.DeleteOperationId}," +
                                     $"{billedResource.Overage}");
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
