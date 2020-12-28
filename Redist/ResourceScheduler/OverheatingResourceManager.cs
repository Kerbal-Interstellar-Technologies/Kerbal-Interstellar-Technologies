using KIT.Interfaces;
using KIT.Resources;
using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KIT.ResourceScheduler
{
    /// <summary>
    /// OverHeatingResourceManager is used temporarily when there is overheating occurring
    /// (where WH ratio % is >= 90%), and is used to reduce non-WasteHeat consumption, with
    /// the aim of reducing what other parts do on the ship to reduce WasteHeat generation.
    /// </summary>
    public class OverHeatingResourceManager : IResourceManager
    {
        private IResourceManager baseImpl;
        public double consumptionReduction = 1;

        public IResourceManager SetBaseResourceManager(IResourceManager root)
        {
            baseImpl = root;

            return this;
        }

        #region Proxy implementation functions
        public ICheatOptions CheatOptions() => baseImpl.CheatOptions();
        public double FixedDeltaTime() => baseImpl.FixedDeltaTime();
        public double ProduceResource(ResourceName resource, double amount, double max = -1) => baseImpl.ProduceResource(resource, amount, max);
        public double ResourceCurrentCapacity(ResourceName resourceIdentifier) => baseImpl.ResourceCurrentCapacity(resourceIdentifier);
        public double ResourceFillFraction(ResourceName resourceIdentifier) => baseImpl.ResourceFillFraction(resourceIdentifier);
        public IResourceProduction ResourceProductionStats(ResourceName resourceIdentifier) => baseImpl.ResourceProductionStats(resourceIdentifier);
        public double ResourceSpareCapacity(ResourceName resourceIdentifier) => baseImpl.ResourceSpareCapacity(resourceIdentifier);
        #endregion

        public double ConsumeResource(ResourceName resource, double wanted)
        {
            if(consumptionReduction < 0 || consumptionReduction > 1)
            {
                Debug.Log($"[OverHeatingResourceManager] Invalid consumptionReduction, got {consumptionReduction}, wanted between 0 and 1");
                return 0;
            }

            if (resource != ResourceName.WasteHeat)
            {
                wanted = Math.Max(0, wanted * consumptionReduction);
            }

            return baseImpl.ConsumeResource(resource, wanted);
        }
    }

}
