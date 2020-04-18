using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Consumption.Models;

namespace AzureConsumptionVerification
{
    internal class ConsumptionAnalyzer
    {
        private const int ProcessingThreads = 20;
        private const int ProcessingRetryCount = 5;
        private readonly ActivityLogProvider _activityLogProvider;

        public ConsumptionAnalyzer(ActivityLogProvider activityLogProvider)
        {
            _activityLogProvider = activityLogProvider;
        }

        public async Task<ConsumptionAnalysisReport> AnalyzeConsumptionForDeletedResources(IList<UsageDetail> usage)
        {
            var report = new ConsumptionAnalysisReport();
            // Fist group by resource, get deletion date, sum numbers after deletion

            // Get resources with non - zero costs
            var processingPool = new ConcurrentQueue<ProcessingTask>(usage
                .Where(u => u.PretaxCost > 0)
                .GroupBy(r => r.InstanceId)
                .Select(u =>
                    new {
                        ResourceId = u.First().InstanceId,
                        Costs = u.Sum(c => c.PretaxCost)
                    })
                .Where(p => p.Costs > 0)
                .Select(t => new ProcessingTask {ResourceId = t.ResourceId}));
            var totalResources = processingPool.Count;

            // Running processing in parallel threads
            var tasks = new List<Task>(ProcessingThreads);
            for (var i = 0; i < ProcessingThreads; i++)
            {
                var task = new Task(() =>
                {
                    ProcessingTask task = null;
                    while (processingPool.TryDequeue(out task))
                        try
                        {
                            if (task.Exceptions.Count > ProcessingRetryCount)
                            {
                                Console.WriteLine($"Failed to process {task.ResourceId}");
                                continue;
                            }

                            var deleteActivity = _activityLogProvider.GetResourceDeletionDate(task.ResourceId);

                            report.AddResource(new BilledResources
                            {
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
                            task.Exceptions.Add(e);
                            processingPool.Enqueue(task);
                        }
                });
                task.Start();
                tasks.Add(task);
            }

            await using var timer = new Timer(data =>
                Console.WriteLine($"Processed ... {report.Count} of {totalResources}"), null, 0, 10000);

            await Task.WhenAll(tasks);

            return report;
        }

        internal class ProcessingTask
        {
            public List<Exception> Exceptions = new List<Exception>();
            public string ResourceId;
        }
    }
}