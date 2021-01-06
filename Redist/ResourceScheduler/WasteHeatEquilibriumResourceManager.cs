using KIT.Interfaces;
using KIT.Resources;

namespace KIT.ResourceScheduler
{
    class WasteHeatEquilibriumResourceManager : IResourceManager
    {
        public IResourceManager BaseImpl;

        public WasteHeatEquilibriumResourceManager(IResourceManager parent)
        {
            BaseImpl = parent;
        }

        ICheatOptions IResourceManager.CheatOptions() => BaseImpl.CheatOptions();
        double IResourceManager.CurrentCapacity(ResourceName resourceIdentifier) => BaseImpl.CurrentCapacity(resourceIdentifier);
        double IResourceManager.FixedDeltaTime() => BaseImpl.FixedDeltaTime();
        double IResourceManager.FillFraction(ResourceName resourceIdentifier) => BaseImpl.FillFraction(resourceIdentifier);
        IResourceProduction IResourceManager.ProductionStats(ResourceName resourceIdentifier) => BaseImpl.ProductionStats(resourceIdentifier);
        double IResourceManager.SpareCapacity(ResourceName resourceIdentifier) => BaseImpl.SpareCapacity(resourceIdentifier);

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
