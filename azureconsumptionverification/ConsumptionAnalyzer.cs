using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Consumption.Models;

namespace AzureConsumptionVerification
{
    internal class ConsumptionAnalyzer
    {
        private const int MaxProcessingThreads = 100;
        private const int ProcessingRetryCount = 5;
        private readonly ActivityLogProvider _activityLogProvider;
        private readonly string _subscriptionId;

        public ConsumptionAnalyzer(ActivityLogProvider activityLogProvider, string subscriptionId)
        {
            _activityLogProvider = activityLogProvider;
            _subscriptionId = subscriptionId;
        }

        public async Task<ConsumptionAnalysisReport> AnalyzeConsumptionForDeletedResources(IList<UsageDetail> usage)
        {
            var report = new ConsumptionAnalysisReport();

            // Get resources with non - zero costs
            var processingPool = new ConcurrentQueue<ProcessingTask>(usage
                .GroupBy(r => r.InstanceId)
                .Select(u =>
                    new {
                        ResourceId = u.First().InstanceId,
                        Costs = u.Sum(c => c.PretaxCost)
                    })
                .Where(p => p.Costs > 0)
                .Select(t => new ProcessingTask {ResourceId = t.ResourceId}));
            var totalResources = processingPool.Count;
            Console.WriteLine($"Subscription ${_subscriptionId}. Retrieved {totalResources} resources with non-zero billing");
            // Running processing in parallel threads
            var processingThreads = processingPool.Count > MaxProcessingThreads
                ? MaxProcessingThreads
                : processingPool.Count;
            var tasks = new List<Task>(processingThreads);
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
                                Console.WriteLine($"Subscription ${_subscriptionId}. Failed to process {task.ResourceId}");
                                continue;
                            }

                            // Delete event is missing for some resources, so use resource group deletion date
                            var deleteActivity = _activityLogProvider.GetResourceDeletionDate(task.ResourceId) ??
                                                 _activityLogProvider.GetResourceGroupDeletionDate(GetResourceGroupName(task.ResourceId));
                            
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
                                ActivityDeleted = deleteActivity?.Date,
                                DeleteOperationId = deleteActivity?.OperationId,
                                Overage = deleteActivity == null
                                    ? 0
                                    : usage.Where(r =>
                                            r.InstanceId == task.ResourceId && r.UsageStart > deleteActivity.Date)
                                        .Sum(o => o.PretaxCost)
                            });
                        }
                        catch (Exception e)
                        {
                            // Activity API sometimes fails with timeouts, need to retry
                            task.Exceptions.Add(e);
                            processingPool.Enqueue(task);
                        }
                });
                tasks.Add(task);
            }

            using var timer = new Timer(data =>
                Console.WriteLine($"Subscription ${_subscriptionId}. Processed ... {report.Count} of {totalResources}"), null, 0, 10000);

            await Task.WhenAll(tasks);

            return report;
        }

        private string GetResourceGroupName(string resourceId)
        {
            return Regex.Match(resourceId, "/resourceGroups/(.*)/providers", RegexOptions.IgnoreCase).Groups[1].Value;
        }

        private class ProcessingTask
        {
            public List<Exception> Exceptions = new List<Exception>();
            public string ResourceId;
        }
    }
}