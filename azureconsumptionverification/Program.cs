using System.Diagnostics;

namespace AzureConsumptionVerification
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var clientId = args[0];
            var clientSecret = args[1];
            var tenantId = args[2];
            var subscription = args[3];

            var credentials = new CustomCredentials(clientId, clientSecret, tenantId);
            var consumption = new ConsumptionProvider(credentials, subscription);
            var usageDetails = consumption.GetConsumptionAsync(4).GetAwaiter().GetResult();

            var consumptionAnalyzer = new ConsumptionAnalyzer(new ActivityLogProvider(credentials, subscription));
            var report = consumptionAnalyzer.AnalyzeConsumptionForDeletedResources(usageDetails).GetAwaiter()
                .GetResult();

            var reportPath = CsvReporter.WriteReport(report);

            var process = new Process {StartInfo = new ProcessStartInfo(reportPath) {UseShellExecute = true}};
            process.Start();
        }
    }
}