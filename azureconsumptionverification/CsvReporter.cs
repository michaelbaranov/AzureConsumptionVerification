﻿using System;
using System.IO;

namespace AzureConsumptionVerification
{
    internal class CsvReporter
    {
        public static void WriteReport(ConsumptionAnalysisReport report, string outputFile)
        {
            using var writer = File.CreateText(outputFile);
            var resources = report.GetResources();

            writer.WriteLine(
                "SubscriptionId," +
                "Resource name," +
                "Resource ID," +
                "Service Type," +
                "Cost," +
                "Currency," +
                "Billing Start," +
                "Billing End," +
                "Actual resource deletion date," +
                "DeleteOperationId," +
                "Overage");
            foreach (var billedResource in resources)
                writer.WriteLine($"{billedResource.SubscriptionId}," +
                                 $"{billedResource.InstanceName.Replace(',', '_')}," +
                                 $"{billedResource.Id.Replace(',', '_')}," +
                                 $"{billedResource.ConsumedService}," +
                                 $"{billedResource.PretaxCost}," +
                                 $"{billedResource.Currency}," +
                                 $"{billedResource.UsageStart}," +
                                 $"{billedResource.UsageEnd}," +
                                 $"{billedResource.ActivityDeleted}," +
                                 $"{billedResource.DeleteOperationId}," +
                                 $"{billedResource.Overage}");
        }

        public static string MergeReports(string path)
        {
            var files = Directory.GetFiles(path);
            if (files.Length == 0) return string.Empty;
            var finalReportFileName = Path.Combine(path, $"analysis_result_{DateTime.UtcNow:yyyy_MM_dd_hh_mm}.csv");

            // Write header
            using var anyReport = File.OpenText(files[0]);
            var header = anyReport.ReadLine();

            using var finalReport = File.CreateText(finalReportFileName);
            finalReport.WriteLine(header);
            foreach (var file in files)
            {
                var subscriptionReport = File.OpenText(file);
                // Skip first line
                subscriptionReport.ReadLine();
                finalReport.Write(subscriptionReport.ReadToEnd());
                subscriptionReport.Close();
            }

            return finalReportFileName;
        }
    }
}