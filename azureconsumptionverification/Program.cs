using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Mono.Options;
using Serilog;

namespace AzureConsumptionVerification
{
    internal class Program
    {
        private static readonly int MaxNumberOfSubscriptionsToAnalyzeInParallel = 20;

        private static async Task<int> Main(string[] args)
        {
            var clientId = string.Empty;
            var clientSecret = string.Empty;
            var tenantId = string.Empty;
            var showHelp = false;
            var numberOfMonthsToAnalyze = 1;
            var outputFolder = Path.GetTempPath();
            var openReport = false;
            var subscription = string.Empty;
            var onlyWithOverages = false;

            var optionSet = new OptionSet()
                .Add("clientId=", o => clientId = o)
                .Add("clientSecret=", o => clientSecret = o)
                .Add("tenantId=", o => tenantId = o)
                .Add("subscription=", o => subscription = o)
                .Add("numberOfMonths=", o => numberOfMonthsToAnalyze = int.Parse(o))
                .Add("outputFolder=", o => outputFolder = o)
                .Add("openReport", o => openReport = o != null)
                .Add("onlyWithOverages", o => onlyWithOverages = true)
                .Add("h|?|help", o => showHelp = o != null);

            optionSet.Parse(args);

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File($"log-{DateTime.UtcNow:yyyy_MM_dd_hh_mm}.txt")
                .CreateLogger();

            if (showHelp)
            {
                ShowHelp();
                return 0;
            }

            if (string.IsNullOrEmpty(subscription))
            {
                Console.WriteLine("Mandatory parameter -subscriptionId is missing");
                ShowHelp();
                return -1;
            }

            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }
            else if (Directory.GetFiles(outputFolder).Length > 0)
            {
                Console.WriteLine("Output directory is not empty");
                return -1;
            }

            var credentials = new CustomCredentials(clientId, clientSecret, tenantId);

            var subscriptions = await GetSubscriptions(subscription, credentials);

            await Process(credentials, subscriptions, numberOfMonthsToAnalyze, outputFolder, onlyWithOverages);

            var reportFile = CsvReporter.MergeReports(outputFolder);
            // Open report
            if (openReport)
            {
                var process = new Process {StartInfo = new ProcessStartInfo(reportFile) {UseShellExecute = true}};
                process.Start();
            }

            return 0;
        }

        private static async Task<ConcurrentQueue<string>> GetSubscriptions(string subscription,
            CustomCredentials credentials)
        {
            ConcurrentQueue<string> subscriptions = null;

            if (subscription == "all")
                await ResourceManager.Configure()
                    .Authenticate(credentials.ToAzureCredentials())
                    .Subscriptions
                    .ListAsync()
                    .ContinueWith(c =>
                        subscriptions =
                            new ConcurrentQueue<string>(c.Result.Select(s => s.SubscriptionId)));
            else
                subscriptions = new ConcurrentQueue<string>(subscription.Split(','));

            return subscriptions;
        }

        private static async Task Process(CustomCredentials credentials, ConcurrentQueue<string> subscriptions,
            int numberOfMonthsToAnalyze, string outputFolder,
            bool onlyWithOverages)
        {
            var processingThreads = subscriptions.Count > MaxNumberOfSubscriptionsToAnalyzeInParallel
                ? MaxNumberOfSubscriptionsToAnalyzeInParallel
                : subscriptions.Count;
            var threads = new List<Task>(processingThreads);
            var subscriptionsTotal = subscriptions.Count;
            var processed = 0;
            for (var i = 0; i < processingThreads; i++)
                threads.Add(Task.Run(() =>
                {
                    while (subscriptions.TryDequeue(out var subscriptionId))
                        try
                        {
                            var consumption = new ConsumptionProvider(credentials, subscriptionId);
                            var usageDetails = consumption.GetConsumptionAsync(numberOfMonthsToAnalyze).GetAwaiter()
                                .GetResult();
                            if (usageDetails.Count == 0)
                            {
                                Log.Warning($"No billing information found for subscription {subscriptionId}");
                                continue;
                            }

                            var consumptionAnalyzer =
                                new ConsumptionAnalyzer(new ActivityLogProvider(credentials, subscriptionId),
                                    subscriptionId);
                            var report = consumptionAnalyzer
                                .AnalyzeConsumptionForDeletedResources(usageDetails, onlyWithOverages).GetAwaiter()
                                .GetResult();

                            var reportFile = Path.Combine(outputFolder, $"consumption_{subscriptionId}_{DateTime.UtcNow:yyyy_MM_dd_hh_mm}.csv");

                            CsvReporter.WriteReport(report, reportFile);
                            processed++;
                        }
                        catch (Exception exception)
                        {
                            Log.Error(exception, $"Exception while processing {subscriptionId}");
                        }
                }).ContinueWith(t =>
                {
                    if (!t.IsCompletedSuccessfully)
                        Log.Error(t.Exception?.GetBaseException(), "Processing thread crashed");
                }));

            using var timer = new Timer(data =>
                Log.Information($"Processed ... {processed} of {subscriptionsTotal} subscriptions"), null, 0, 10000);

            await Task.WhenAll(threads);
        }

        private static void ShowHelp()
        {
            Console.WriteLine("Expected parameters:");
            Console.WriteLine(" -clientId");
            Console.WriteLine(" -clientSecret");
            Console.WriteLine(" -tenantId");
            Console.WriteLine(" -subscription [mandatory] supported values - <all>, <array of subscription ids>");
            Console.WriteLine(
                " -numberOfMonths [optional, default 1] number of months to analyze, due to Activity log API limitation in 90 days max value is 4");
            Console.WriteLine(
                " -outputFolder [optional, default %TEMP%] folder to save report");
            Console.WriteLine(
                " -openReport [optional, default <empty>] switch if enabled opens generated report");
            Console.WriteLine(
                " -onlyWithOverages [optional, default <empty>] switch if enabled show only resources with overages");
            Console.WriteLine("Examples:");
            Console.WriteLine(
                "Run analysis for all subscriptions available for service principal, show only those with overages");
            Console.WriteLine("AzureConsumptionVerification -clientId=124d8317-dd0a-47f8-b630-c4839eb1602d " +
                              "-clientSecret=ObTY9A53gEB3_TgUFICK=gqX_NedhlE- " +
                              "-tenantId=91700184-c314-4dc9-bb7e-a411df456a1e " +
                              "-subscription=all " +
                              "-outputFolder=\"c:\\reports\" " +
                              "-openReport " +
                              "-onlyWithOverages");
            Console.WriteLine("------------------------------------------------------------------");
            Console.WriteLine("Run analysis for single subscription");
            Console.WriteLine("AzureConsumptionVerification -clientId=124d8317-dd0a-47f8-b630-c4839eb1602d " +
                              "-clientSecret=ObTY9A53gEB3_TgUFICK=gqX_NedhlE- " +
                              "-tenantId=91700184-c314-4dc9-bb7e-a411df456a1e " +
                              "-subscription=22d1e318-4c86-4f8e-9cef-b04f36ba31c0 " +
                              "-outputFolder=\"c:\\reports\" " +
                              "-openReport ");
            Console.WriteLine("------------------------------------------------------------------");
            Console.WriteLine("Run analysis for two subscriptions");
            Console.WriteLine("AzureConsumptionVerification -clientId=124d8317-dd0a-47f8-b630-c4839eb1602d " +
                              "-clientSecret=ObTY9A53gEB3_TgUFICK=gqX_NedhlE- " +
                              "-tenantId=91700184-c314-4dc9-bb7e-a411df456a1e " +
                              "-subscription=22d1e318-4c86-4f8e-9cef-b04f36ba31c0,1e31822d-5c86-4a8e-9cef-f36ba31c0b04 " +
                              "-outputFolder=\"c:\\reports\" " +
                              "-openReport ");
        }
    }
}