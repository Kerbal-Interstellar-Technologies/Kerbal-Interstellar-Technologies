using System.Collections.Generic;
using FinePrint;
using KIT.Interfaces;
using KIT.Resources;

namespace KIT.ResourceScheduler
{
    class WasteHeatEquilibriumResourceManager : IResourceManager
    {
        public IResourceManager BaseImpl;

        public IResourceManager Update(IResourceManager baseImpl)
        {
            BaseImpl = baseImpl;
            return this;
        }

        #region proxy implementation

        public double ScaledConsumptionProduction(List<ResourceKeyValue> consumeResources,
            List<ResourceKeyValue> produceResources, double minimumRatio = 0,
            ConsumptionProductionFlags flags = ConsumptionProductionFlags.Empty) =>
            BaseImpl.ScaledConsumptionProduction(consumeResources, produceResources, minimumRatio, flags);

        public double CurrentCapacity(ResourceName resourceIdentifier) => BaseImpl.CurrentCapacity(resourceIdentifier);

        public double FillFraction(ResourceName resourceIdentifier) => BaseImpl.FillFraction(resourceIdentifier);

        public double SpareCapacity(ResourceName resourceIdentifier) => BaseImpl.SpareCapacity(resourceIdentifier);

        public double MaxCapacity(ResourceName resourceIdentifier) => BaseImpl.MaxCapacity(resourceIdentifier);

        ICheatOptions IResourceManager.CheatOptions() => BaseImpl.CheatOptions();

        public bool CapacityInformation(ResourceName resourceIdentifier, out double maxCapacity,
            out double spareCapacity,
            out double currentCapacity, out double fillFraction) => BaseImpl.CapacityInformation(resourceIdentifier,
            out maxCapacity, out spareCapacity, out currentCapacity, out fillFraction);

        double IResourceManager.FixedDeltaTime() => BaseImpl.FixedDeltaTime();
        IResourceProduction IResourceManager.ProductionStats(ResourceName resourceIdentifier) => BaseImpl.ProductionStats(resourceIdentifier);

        #endregion

        double IResourceManager.Consume(ResourceName resource, double wanted)
        {
            if(resource == ResourceName.WasteHeat) return wanted; // don't care.
            return BaseImpl.Consume(resource, wanted);
        }

        double IResourceManager.Produce(ResourceName resource, double amount, double max)
        {
            if (resource == ResourceName.WasteHeat) return amount; // don't care
            return BaseImpl.Produce(resource, amount, max);
        }
    }
}
