using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Management.AppService.Fluent.Models;

namespace AzureConsumptionVerification
{
    internal class ConsumptionAnalysisReport
    {
        private readonly ConcurrentBag<BilledResources> _resources;

        public ConsumptionAnalysisReport()
        {
            _resources = new ConcurrentBag<BilledResources>();
        }

        public int Count => _resources.Count;
        
        public void AddResource(BilledResources resource)
        {
            _resources.Add(resource);
        }

        public IList<BilledResources> GetResources()
        {
            return _resources.ToList();
        }
    }
}