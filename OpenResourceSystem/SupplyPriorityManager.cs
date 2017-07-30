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

        public Vessel Vessel {get; private set;}
        public PartModule ProcessingPart { get; private set; }

        public void UpdatePartModule(PartModule partmodule)
        {
            Vessel = partmodule.vessel;
            ProcessingPart = partmodule;
        }

        public SupplyPriorityManager(Vessel vessel)
        {
            this.Vessel = vessel;
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

        public long Counter { get; private set; }

        public void UpdateResourceSuppliables(long  updateCounter, float fixedDeltaTime)
        {
            try
            {
                Counter = updateCounter;

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
