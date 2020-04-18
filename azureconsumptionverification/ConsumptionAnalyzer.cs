﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Azure.Management.Consumption.Models;
using Microsoft.Azure.Management.ContainerRegistry.Fluent.RegistryTask.Definition;
using Timer = System.Threading.Timer;

namespace AzureConsumptionVerification
{
    class ConsumptionAnalyzer
    {
        private readonly ActivityLogProvider _activityLogProvider;
        private const int ProcessingThreads = 20;
        private const int ProcessingRetryCount = 5;

        public ConsumptionAnalyzer(ActivityLogProvider activityLogProvider)
        {
            _activityLogProvider = activityLogProvider;
        }

        public async Task<ConsumptionAnalysisReport> AnalyzeConsumptionForDeletedResources(IList<UsageDetail> usage)
        {
            var report = new ConsumptionAnalysisReport();
            // Fist group by resource, get deletion date, sum numbers after deletion

            var processingPool = new ConcurrentQueue<ProcessingTask>(usage.Where(u => u.PretaxCost > 0)
                .GroupBy(r => r.InstanceId).Select(grp => grp.First().InstanceId)
                .Select(p => new ProcessingTask() { ResourceId = p }));
            int totalResources = processingPool.Count;

            // Running processing in parallel threads
            var tasks = new List<Task>(ProcessingThreads);
            for (int i = 0; i < ProcessingThreads; i++)
            {
                var task = new Task(() =>
                {
                    ProcessingTask task = null;
                    while (processingPool.TryDequeue(out task))
                    {
                        try
                        {
                            if (task.Exceptions.Count > ProcessingRetryCount)
                            {
                                Console.WriteLine($"Failed to process {task.ResourceId}");
                                continue;
                            }
                            var deleteActivity = _activityLogProvider.GetResourceDeletionDate(task.ResourceId);

                            report.AddResource(new BilledResources()
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
                                Overage = deleteActivity == null ? 0 : usage.Where(r => r.InstanceId == task.ResourceId && r.UsageStart > deleteActivity.Date).Sum(o => o.PretaxCost)
                            });
                        }
                        catch (Exception e)
                        {
                            task.Exceptions.Add(e);
                            processingPool.Enqueue(task);
                        }
                    }
                });
                task.Start();
                tasks.Add(task);
            }

            await using var timer = new Timer(new TimerCallback((data) =>
                Console.WriteLine($"Processed ... {report.Count} of {totalResources}")), null, 0, 10000);
            
            await Task.WhenAll(tasks);

            return report;
        }

        private void StatusTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            throw new NotImplementedException();
        }

        internal class ProcessingTask
        {
            public List<Exception> Exceptions = new List<Exception>();
            public string ResourceId;
        }
    }


}