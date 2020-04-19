using System;
using System.Diagnostics;
using Mono.Options;

namespace AzureConsumptionVerification
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            string clientId = string.Empty;
            string clientSecret = string.Empty;
            string tenantId = string.Empty;
            string subscription = string.Empty;
            bool showHelp = false;

            var optionSet = new OptionSet()
                .Add("clientId=", o => clientId = o)
                .Add("clientSecret=", o => clientSecret = o)
                .Add("tenantId=", o => tenantId = o)
                .Add("subscription=", o => subscription = o)
                .Add("h|?|help", o => showHelp = o != null); 

            optionSet.Parse(args);

            if (showHelp)
            {
                ShowHelp();
                return;
            }

            if (string.IsNullOrEmpty(subscription))
            {
                Console.WriteLine("Mandatory parameter -subscriptionId is missing");
            }

            var credentials = new CustomCredentials(clientId, clientSecret, tenantId);
            var consumption = new ConsumptionProvider(credentials, subscription);
            var usageDetails = consumption.GetConsumptionAsync(1).GetAwaiter().GetResult();

            var consumptionAnalyzer = new ConsumptionAnalyzer(new ActivityLogProvider(credentials, subscription));
            var report = consumptionAnalyzer.AnalyzeConsumptionForDeletedResources(usageDetails).GetAwaiter()
                .GetResult();

            var reportPath = CsvReporter.WriteReport(report);

            var process = new Process {StartInfo = new ProcessStartInfo(reportPath) {UseShellExecute = true}};
            process.Start();
        }

        private static void ShowHelp()
        {
            Console.WriteLine("Expected parameters:");
            Console.WriteLine("- For headless authentication");
            Console.WriteLine(" -clientId");
            Console.WriteLine(" -clientSecret");
            Console.WriteLine(" -tenantId");
            Console.WriteLine(" -subscription");
            Console.WriteLine("Example:");
            Console.WriteLine("AzureConsumptionVerification -cilentId=124d8317-dd0a-47f8-b630-c4839eb1602d " +
                              "-clientSecret=ObTY9A53gEB3_TgUFICK=gqX_NedhlE- " +
                              "-tenantId=91700184-c314-4dc9-bb7e-a411df456a1e " +
                              "-subscription=38cadfad-6513-4396-af97-8606962edfa1");
        }
    }
}