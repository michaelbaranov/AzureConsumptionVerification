using System;
using System.Diagnostics;
using Mono.Options;

namespace AzureConsumptionVerification
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var clientId = string.Empty;
            var clientSecret = string.Empty;
            var tenantId = string.Empty;
            var subscriptionId = string.Empty;
            var showHelp = false;
            var numberOfMonthsToAnalyze = 1;

            var optionSet = new OptionSet()
                .Add("clientId=", o => clientId = o)
                .Add("clientSecret=", o => clientSecret = o)
                .Add("tenantId=", o => tenantId = o)
                .Add("subscriptionId=", o => subscriptionId = o)
                .Add("numberOfMonths=", o => numberOfMonthsToAnalyze = int.Parse(o))
                .Add("h|?|help", o => showHelp = o != null);

            optionSet.Parse(args);

            if (showHelp)
            {
                ShowHelp();
                return;
            }

            if (string.IsNullOrEmpty(subscriptionId))
            {
                Console.WriteLine("Mandatory parameter -subscriptionId is missing");
                ShowHelp();
                return;
            }

            var credentials = new CustomCredentials(clientId, clientSecret, tenantId);
            var consumption = new ConsumptionProvider(credentials, subscriptionId);
            var usageDetails = consumption.GetConsumptionAsync(numberOfMonthsToAnalyze).GetAwaiter().GetResult();

            var consumptionAnalyzer = new ConsumptionAnalyzer(new ActivityLogProvider(credentials, subscriptionId));
            var report = consumptionAnalyzer.AnalyzeConsumptionForDeletedResources(usageDetails).GetAwaiter()
                .GetResult();

            var reportPath = CsvReporter.WriteReport(report, subscriptionId);

            // Open report
            var process = new Process {StartInfo = new ProcessStartInfo(reportPath) {UseShellExecute = true}};
            process.Start();
        }

        private static void ShowHelp()
        {
            Console.WriteLine("Expected parameters:");
            Console.WriteLine(" -clientId");
            Console.WriteLine(" -clientSecret");
            Console.WriteLine(" -tenantId");
            Console.WriteLine(" -subscriptionId");
            Console.WriteLine(
                " -numberOfMonths [optional, default 1] number of months to analyze, due to Activity log API limitation in 90 days max value is 4");
            Console.WriteLine("Example:");
            Console.WriteLine("AzureConsumptionVerification -cilentId=124d8317-dd0a-47f8-b630-c4839eb1602d " +
                              "-clientSecret=ObTY9A53gEB3_TgUFICK=gqX_NedhlE- " +
                              "-tenantId=91700184-c314-4dc9-bb7e-a411df456a1e " +
                              "-subscriptionId=38cadfad-6513-4396-af97-8606962edfa1");
        }
    }
}