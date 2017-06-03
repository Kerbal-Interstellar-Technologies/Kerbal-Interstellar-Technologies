﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OpenResourceSystem
{
    public class SupplyPriorityManager
    {
        protected static Dictionary<Vessel, SupplyPriorityManager> supply_priority_managers = new Dictionary<Vessel,SupplyPriorityManager>();

        public static SupplyPriorityManager GetSupplyPriorityManagerForVessel(Vessel vessel) 
        {
            SupplyPriorityManager manager;

            if (!supply_priority_managers.TryGetValue(vessel, out manager))
            {
                manager = new SupplyPriorityManager(vessel);
                supply_priority_managers.Add(vessel, manager);
            }

            return manager;
        }

        protected List<ORSResourceSuppliableModule> suppliable_modules = new List<ORSResourceSuppliableModule>();
        
        protected Vessel vessel;
        public PartModule processingPart;

        public SupplyPriorityManager(Vessel vessel)
        {
            this.vessel = vessel;
        }

        public void Register(ORSResourceSuppliableModule suppliable)
        {
            if (!suppliable_modules.Contains(suppliable))
            {
                suppliable_modules.Add(suppliable);
            }
        }

        public void Unregister(ORSResourceSuppliableModule suppliable)
        {
            if (!suppliable_modules.Contains(suppliable))
            {
                suppliable_modules.Remove(suppliable);
            }
        }

        public void UpdateResourceSuppliables(float fixedDeltaTime)
        {
            try
            {
                var suppliable_modules_priotised = suppliable_modules.Where(m => m != null).OrderBy(m => m.getPowerPriority()).ToList();

                suppliable_modules_priotised.ForEach(s => s.OnFixedUpdateResourceSuppliable(fixedDeltaTime));
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - Exception in SupplyPriorityManager.UpdateResourceSuppliables " + e.Message);
                throw;
            }
        }
       
    }

}
