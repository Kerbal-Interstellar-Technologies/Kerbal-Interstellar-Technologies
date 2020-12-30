using KIT.Interfaces;
using KIT.Resources;
using System;
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
        private IResourceManager _baseImpl;
        public double ConsumptionReduction = 1;

        public IResourceManager SetBaseResourceManager(IResourceManager root)
        {
            _baseImpl = root;

            return this;
        }

        #region Proxy implementation functions
        public ICheatOptions CheatOptions() => _baseImpl.CheatOptions();
        public double FixedDeltaTime() => _baseImpl.FixedDeltaTime();
        public double ProduceResource(ResourceName resource, double amount, double max = -1) => _baseImpl.ProduceResource(resource, amount, max);
        public double ResourceCurrentCapacity(ResourceName resourceIdentifier) => _baseImpl.ResourceCurrentCapacity(resourceIdentifier);
        public double ResourceFillFraction(ResourceName resourceIdentifier) => _baseImpl.ResourceFillFraction(resourceIdentifier);
        public IResourceProduction ResourceProductionStats(ResourceName resourceIdentifier) => _baseImpl.ResourceProductionStats(resourceIdentifier);
        public double ResourceSpareCapacity(ResourceName resourceIdentifier) => _baseImpl.ResourceSpareCapacity(resourceIdentifier);
        #endregion

        public double ConsumeResource(ResourceName resource, double wanted)
        {
            if(ConsumptionReduction < 0 || ConsumptionReduction > 1)
            {
                Debug.Log($"[OverHeatingResourceManager] Invalid consumptionReduction, got {ConsumptionReduction}, wanted between 0 and 1");
                return 0;
            }

            if (resource != ResourceName.WasteHeat)
            {
                wanted = Math.Max(0, wanted * ConsumptionReduction);
            }

            return _baseImpl.ConsumeResource(resource, wanted);
        }
    }

}
