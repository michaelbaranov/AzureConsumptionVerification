using System.Collections.Generic;

namespace AzureConsumptionVerification
{
    internal class ConsumptionAnalysisReport
    {
        public ConsumptionAnalysisReport()
        {
            Resourceses = new List<BilledResources>();
        }

        public IList<BilledResources> Resourceses { get; }
    }
}