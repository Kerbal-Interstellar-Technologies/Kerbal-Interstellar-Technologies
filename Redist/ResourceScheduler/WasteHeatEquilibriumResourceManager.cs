using KIT.Interfaces;
using KIT.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIT.ResourceScheduler
{
    class WasteHeatEquilibriumResourceManager : IResourceManager
    {
        public IResourceManager baseImpl = null;

        public WasteHeatEquilibriumResourceManager(IResourceManager parent)
        {
            baseImpl = parent;
        }

        ICheatOptions IResourceManager.CheatOptions() => baseImpl.CheatOptions();
        double IResourceManager.ResourceCurrentCapacity(ResourceName resourceIdentifier) => baseImpl.ResourceCurrentCapacity(resourceIdentifier);
        double IResourceManager.FixedDeltaTime() => baseImpl.FixedDeltaTime();
        double IResourceManager.ResourceFillFraction(ResourceName resourceIdentifier) => baseImpl.ResourceFillFraction(resourceIdentifier);
        IResourceProduction IResourceManager.ResourceProductionStats(ResourceName resourceIdentifier) => baseImpl.ResourceProductionStats(resourceIdentifier);
        double IResourceManager.ResourceSpareCapacity(ResourceName resourceIdentifier) => baseImpl.ResourceSpareCapacity(resourceIdentifier);

        double IResourceManager.ConsumeResource(ResourceName resource, double wanted)
        {
            if(resource == ResourceName.WasteHeat) return wanted; // don't care.
            return baseImpl.ConsumeResource(resource, wanted);
        }

        double IResourceManager.ProduceResource(ResourceName resource, double amount, double max)
        {
            if (resource == ResourceName.WasteHeat) return amount; // don't care
            return baseImpl.ProduceResource(resource, amount, max);
        }
    }
}
