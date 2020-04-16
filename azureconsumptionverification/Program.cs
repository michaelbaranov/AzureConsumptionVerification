using System;
using System.Diagnostics;

namespace AzureConsumptionVerification
{
    class Program
    {
        static void Main(string[] args)
        {
            string clientId = args[0];
            string clientSecret = args[1];
            string tenantId = args[2];
            string subscription = args[3];

            var credentials = new CustomCredentials(clientId, clientSecret, tenantId);
            var consumption = new ConsumptionProvider(credentials, subscription);
            var usageDetails = consumption.GetConsumptionAsync().GetAwaiter().GetResult();

            var consumptionAnalyzer = new ConsumptionAnalyzer(new ActivityLogProvider(credentials, subscription));
            var report = consumptionAnalyzer.AnalyzeConsumptionForDeletedResources(usageDetails);

            var reportPath = CsvReporter.WriteReport(report);

            var process = new Process {StartInfo = new ProcessStartInfo(reportPath) {UseShellExecute = true}};
            process.Start();
        }
    }
}
