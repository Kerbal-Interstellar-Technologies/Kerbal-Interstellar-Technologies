using KIT.Interfaces;
using KIT.Resources;
using System;
using System.Collections.Generic;
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

        public double MaxCapacity(ResourceName resourceIdentifier)
        {
            throw new NotImplementedException();
        }

        public ICheatOptions CheatOptions() => _baseImpl.CheatOptions();

        public bool CapacityInformation(ResourceName resourceIdentifier, out double maxCapacity,
            out double spareCapacity,
            out double currentCapacity, out double fillFraction) => _baseImpl.CapacityInformation(resourceIdentifier,
            out maxCapacity, out spareCapacity, out currentCapacity, out fillFraction);
        
        public double FixedDeltaTime() => _baseImpl.FixedDeltaTime();
        public double Produce(ResourceName resource, double amount, double max = -1) => _baseImpl.Produce(resource, amount, max);
        public IResourceProduction ProductionStats(ResourceName resourceIdentifier) => _baseImpl.ProductionStats(resourceIdentifier);


        public double CurrentCapacity(ResourceName resourceIdentifier) => _baseImpl.CurrentCapacity(resourceIdentifier);
        public double FillFraction(ResourceName resourceIdentifier) => _baseImpl.FillFraction(resourceIdentifier);
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

        public double ScaledConsumptionProduction(List<KeyValuePair<ResourceName, double>> consumeResources,
            List<KeyValuePair<ResourceName, double>> produceResources, double minimumRatio = 0,
            ConsumptionProductionFlags flags = ConsumptionProductionFlags.Empty)
        {
            // TODO implement consumption reduction across the board
            return _baseImpl.ScaledConsumptionProduction(consumeResources, produceResources, minimumRatio, flags);
        }
    }

}
