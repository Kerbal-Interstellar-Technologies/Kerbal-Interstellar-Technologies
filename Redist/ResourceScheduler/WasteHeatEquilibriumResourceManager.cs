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
        double IResourceManager.ResourceCurrentCapacity(ResourceName resourceIdentifier) => BaseImpl.ResourceCurrentCapacity(resourceIdentifier);
        double IResourceManager.FixedDeltaTime() => BaseImpl.FixedDeltaTime();
        double IResourceManager.ResourceFillFraction(ResourceName resourceIdentifier) => BaseImpl.ResourceFillFraction(resourceIdentifier);
        IResourceProduction IResourceManager.ResourceProductionStats(ResourceName resourceIdentifier) => BaseImpl.ResourceProductionStats(resourceIdentifier);
        double IResourceManager.ResourceSpareCapacity(ResourceName resourceIdentifier) => BaseImpl.ResourceSpareCapacity(resourceIdentifier);

        double IResourceManager.ConsumeResource(ResourceName resource, double wanted)
        {
            if(resource == ResourceName.WasteHeat) return wanted; // don't care.
            return BaseImpl.ConsumeResource(resource, wanted);
        }

        double IResourceManager.ProduceResource(ResourceName resource, double amount, double max)
        {
            if (resource == ResourceName.WasteHeat) return amount; // don't care
            return BaseImpl.ProduceResource(resource, amount, max);
        }
    }
}
