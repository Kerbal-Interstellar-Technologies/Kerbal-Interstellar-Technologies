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
        public double Produce(ResourceName resource, double amount, double max = -1) => _baseImpl.Produce(resource, amount, max);
        public double CurrentCapacity(ResourceName resourceIdentifier) => _baseImpl.CurrentCapacity(resourceIdentifier);
        public double FillFraction(ResourceName resourceIdentifier) => _baseImpl.FillFraction(resourceIdentifier);
        public IResourceProduction ProductionStats(ResourceName resourceIdentifier) => _baseImpl.ProductionStats(resourceIdentifier);
        public double SpareCapacity(ResourceName resourceIdentifier) => _baseImpl.SpareCapacity(resourceIdentifier);
        #endregion

        public double Consume(ResourceName resource, double wanted)
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

            return _baseImpl.Consume(resource, wanted);
        }
    }

}
