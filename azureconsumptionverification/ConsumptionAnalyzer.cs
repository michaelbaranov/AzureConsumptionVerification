using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Consumption.Models;
using Serilog;

namespace AzureConsumptionVerification
{
    internal class ConsumptionAnalyzer
    {
        private const int MaxProcessingThreads = 100;
        private const int ProcessingRetryCount = 5;
        private readonly ActivityLogProvider _activityLogProvider;
        private readonly ResourceProvider _resourceProvider;
        private readonly string _subscriptionId;

        public ConsumptionAnalyzer(ActivityLogProvider activityLogProvider, ResourceProvider resourceProvider,
            string subscriptionId)
        {
            _activityLogProvider = activityLogProvider;
            _resourceProvider = resourceProvider;
            _subscriptionId = subscriptionId;
        }

        public async Task<ConsumptionAnalysisReport> AnalyzeConsumptionForDeletedResources(IList<UsageDetail> usage,
            bool onlyWithOverages)
        {
            var report = new ConsumptionAnalysisReport();

            // Get resources with non - zero costs
            var processingPool = new ConcurrentQueue<ProcessingTask>(usage
                .GroupBy(r => r.InstanceId.ToLowerInvariant())
                .Select(u =>
                    new
                    {
                        ResourceId = u.First().InstanceId,
                        Costs = u.Sum(c => c.PretaxCost)
                    })
                .Where(p => p.Costs > 0)
                .Select(t => new ProcessingTask {ResourceId = t.ResourceId}));
            var totalResources = processingPool.Count;
            Log.Information(
                $"Subscription ${_subscriptionId}. Retrieved {totalResources} resources with non-zero billing");
            // Running processing in parallel threads
            var processingThreads = processingPool.Count > MaxProcessingThreads
                ? MaxProcessingThreads
                : processingPool.Count;
            var tasks = new List<Task>(processingThreads);
            var processedCount = 0;
            for (var i = 0; i < processingThreads; i++)
            {
                var task = Task.Run(() =>
                {
                    ProcessingTask task = null;
                    while (processingPool.TryDequeue(out task))
                        try
                        {
                            if (task.Exceptions.Count > ProcessingRetryCount)
                            {
                                Log.Warning($"Subscription ${_subscriptionId}. Failed to process {task.ResourceId}");
                                continue;
                            }

                            // Skip existing resources
                            var resourceExists = _resourceProvider.IsResourceExists(task.ResourceId).Result;
                            if (resourceExists == null)
                            {
                                Log.Information(
                                    $"Item skipped. Billing Item {task.ResourceId} is not actually a resource");
                                continue;
                            }

                            if (resourceExists.Value)
                            {
                                Log.Information($"Item skipped. Item {task.ResourceId} exists at the moment");
                                continue;
                            }

                            // Delete event is missing for some resources, so use resource group deletion date
                            var deleteActivity = _activityLogProvider.GetResourceDeletionDate(task.ResourceId).Result ??
                                                 _activityLogProvider
                                                     .GetResourceGroupDeletionDate(
                                                         GetResourceGroupName(task.ResourceId)).Result;

                            if (deleteActivity == null || onlyWithOverages &&
                                !usage.Any(r =>
                                    r.InstanceId == task.ResourceId && r.UsageStart > deleteActivity?.EventTimestamp))
                            {
                                processedCount++;
                                continue;
                            }

                            // Calculate overage as sum of billing records for dates past deletion
                            report.AddResource(new BilledResources
                            {
                                SubscriptionId = usage.First(r => r.InstanceId == task.ResourceId).SubscriptionGuid,
                                Currency = usage.First(r => r.InstanceId == task.ResourceId).Currency,
                                Id = task.ResourceId,
                                InstanceName = usage.First(r => r.InstanceId == task.ResourceId).InstanceName,
                                PretaxCost = usage.Where(r => r.InstanceId == task.ResourceId).Sum(u => u.PretaxCost),
                                ConsumedService = usage.First(r => r.InstanceId == task.ResourceId).ConsumedService,
                                SubscriptionName = usage.First(r => r.InstanceId == task.ResourceId).SubscriptionName,
                                UsageStart = usage.Where(r => r.InstanceId == task.ResourceId).Min(u => u.UsageStart),
                                UsageEnd = usage.Where(r => r.InstanceId == task.ResourceId).Max(u => u.UsageEnd),
                                ActivityDeleted = deleteActivity?.EventTimestamp,
                                DeleteOperationId = deleteActivity?.OperationId,
                                Overage = deleteActivity == null
                                    ? 0
                                    : usage.Where(r =>
                                            r.InstanceId == task.ResourceId &&
                                            r.UsageStart > deleteActivity.EventTimestamp)
                                        .Sum(o => o.PretaxCost)
                            });
                            processedCount++;
                        }
                        catch (Exception exception)
                        {
                            // Activity API sometimes fails with timeouts, need to retry
                            Log.Error(exception, $"Exception while processing resource {task.ResourceId}, retrying");
                            task.Exceptions.Add(exception);
                            processingPool.Enqueue(task);
                        }
                }).ContinueWith(t =>
                {
                    if (!t.IsCompletedSuccessfully)
                        Log.Error(t.Exception?.GetBaseException(), "Analysis thread crashed");
                });
                tasks.Add(task);
            }

            using var timer = new Timer(data =>
                    Log.Information(
                        $"Subscription {_subscriptionId}. Processed ... {processedCount} of {totalResources}"),
                null, 0, 10000);

            await Task.WhenAll(tasks);

            return report;
        }

        private string GetResourceGroupName(string resourceId)
        {
            return Regex.Match(resourceId, "/resourceGroups/(.*)/providers", RegexOptions.IgnoreCase).Groups[1].Value;
        }

        private class ProcessingTask
        {
            public readonly List<Exception> Exceptions = new List<Exception>();
            public string ResourceId;
        }
    }
}