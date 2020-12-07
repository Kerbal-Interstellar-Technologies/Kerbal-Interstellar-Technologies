using KIT.Constants;
using KIT.Extensions;
using KIT.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KIT.Powermanagement
{
    /*
    abstract class ResourceSuppliableModule : PartModule, IResourceSuppliable, IResourceSupplier
    {
        [KSPField(isPersistant = false, guiActive = false, guiName = "#LOC_KSPIE_ResourceManager_UpdateCounter")]//Update Counter
        public long updateCounter;

        protected readonly Dictionary<Guid, double> connectedReceivers = new Dictionary<Guid, double>();
        protected readonly Dictionary<Guid, double> connectedReceiversFraction = new Dictionary<Guid, double>();

        protected List<Part> similarParts;
        protected string[] resources_to_supply;

        public Guid Id { get; private set;}

        protected int partNrInList;

        private readonly Dictionary<string, double> fnresource_supplied = new Dictionary<string, double>();

        public virtual void AttachThermalReciever(Guid key, double radius)
        {
            if (!connectedReceivers.ContainsKey(key))
                connectedReceivers.Add(key, radius);
        }

        public virtual void DetachThermalReciever(Guid key)
        {
            if (connectedReceivers.ContainsKey(key))
                connectedReceivers.Remove(key);
        }

        public virtual double GetFractionThermalReciever(Guid key)
        {
            if (connectedReceiversFraction.TryGetValue(key, out var result))
                return result;
            else
                return 0;
        }

        public double getTotalPowerSupplied(string resourcename)
        {
            if (string.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: getTotalPowerSupplied resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.TotalPowerSupplied;
        }

        public double getStableResourceSupply(string resourcename)
        {
            if (string.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: getCurrentResourceDemand resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.StableResourceSupply;
        }

        public double getCurrentHighPriorityResourceDemand(string resourcename)
        {
            if (string.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: getCurrentHighPriorityResourceDemand resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.ResourceDemandHighPriority;
        }

        public double getAvailableStableSupply(string resourcename)
        {
            if (string.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: getAvailableResourceSupply resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return Math.Max(manager.StableResourceSupply - manager.ResourceDemandHighPriority, 0);
        }

        public double getAvailablePrioritisedStableSupply(string resourcename)
        {
            if (string.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: getAvailablePrioritisedStableSupply resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return Math.Max(manager.StableResourceSupply - manager.GetStablePriorityResourceSupply(getPowerPriority()), 0);
        }

        public double getAvailablePrioritisedCurrentSupply(string resourcename)
        {
            if (string.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: getAvailablePrioritisedCurrentSupply resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return Math.Max(manager.ResourceSupply - manager.GetCurrentPriorityResourceSupply(getPowerPriority()), 0);
        }

        public double GetCurrentPriorityResourceSupply(string resourcename)
        {
            if (string.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: getAvailablePrioritisedCurrentSupply resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.GetCurrentPriorityResourceSupply(getPowerPriority());
        }

        public double getStablePriorityResourceSupply(string resourcename, int priority)
        {
            if (string.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: getAvailableResourceSupply resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.GetStablePriorityResourceSupply(priority);
        }

        public double getPriorityResourceSupply(string resourcename)
        {
            if (string.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: getAvailableResourceSupply resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.GetStablePriorityResourceSupply(getPowerPriority());
        }

        public double getAvailableResourceSupply(string resourcename)
        {
            if (string.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: getAvailableResourceSupply resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
            {
                //Debug.LogError("[KSPI]: failed to find resource Manager For Current Vessel");
                return 0;
            }

            return Math.Max(manager.ResourceSupply - manager.ResourceDemandHighPriority, 0);
        }

        public double getCurrentResourceSupply(string resourcename)
        {
            if (string.IsNullOrEmpty(resourcename)) {
                Debug.LogError("[KSPI]: getResourceSupply resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
            {
                //Debug.LogError("[KSPI]: failed to find resource Manager For Current Vessel");
                return 0;
            }

            return manager.ResourceSupply;
        }

        public double GetCurrentSurplus(string resourcename)
        {
            if (string.IsNullOrEmpty(resourcename)) {
                Debug.LogError("[KSPI]: GetCurrentSurplus resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
            {
                //Debug.LogError("[KSPI]: failed to find resource Manager For Current Vessel");
                return 0;
            }

            return manager.CurrentSurplus;
        }

        public double getDemandStableSupply(string resourcename)
        {
            if (string.IsNullOrEmpty(resourcename)) {
                Debug.LogError("[KSPI]: getDemandStableSupply resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.DemandStableSupply;
        }

        public double getResourceDemand(string resourcename)
        {
            if (string.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: getResourceDemand resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
            {
                //Debug.LogError("[KSPI]: failed to find resource Manager For Current Vessel");
                return 0;
            }

            return manager.ResourceDemand;
        }

        public double GetRequiredResourceDemand(string resourcename)
        {
            if (string.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: GetRequiredResourceDemand resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.RequiredResourceDemand;
        }

        public double GetCurrentUnfilledResourceDemand(string resourcename)
        {
            if (string.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: GetRequiredResourceDemand resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.CurrentUnfilledResourceDemand;
        }

        public double GetPowerSupply(string resourceName)
        {
            if (string.IsNullOrEmpty(resourceName))
            {
                Debug.LogError("[KSPI]: GetPowerSupply resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourceName);
            if (manager == null)
                return 0;

            return manager.CurrentResourceSupply;
        }

        public double getResourceBarRatio(string resourcename)
        {
            if (string.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: getResourceBarRatio resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return (double)manager.ResourceFillFraction;
        }

        public double getResourceBarFraction(string resourcename)
        {
            if (string.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: getResourceBarFraction resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.ResourceFillFraction;
        }

        public double getSpareResourceCapacity(string resourcename)
        {
            if (string.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: getSpareResourceCapacity resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.GetSpareResourceCapacity();
        }

        public double getResourceAvailability(string resourcename)
        {
            if (string.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: getResourceAvailability resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.GetResourceAvailability();
        }

        public double getTotalResourceCapacity(string resourcename)
        {
            if (string.IsNullOrEmpty(resourcename))
            {
                Debug.LogError("[KSPI]: getTotalResourceCapacity resourceName is null or empty");
                return 0;
            }

            ResourceManager manager = getManagerForVessel(resourcename);
            if (manager == null)
                return 0;

            return manager.GetTotalResourceCapacity();
        }

        public override void OnStart(PartModule.StartState state)
        {
            Id = Guid.NewGuid();

            if (state == StartState.Editor || resources_to_supply == null) return;

            part.OnJustAboutToBeDestroyed -= OnJustAboutToBeDestroyed;
            part.OnJustAboutToBeDestroyed += OnJustAboutToBeDestroyed;

            foreach (string resourcename in resources_to_supply)
            {
                ResourceManager manager = getOvermanagerForResource(resourcename).getManagerForVessel(vessel);

                if (manager == null)
                {
                    similarParts = null;
                    manager = CreateResourceManagerForResource(resourcename);

                    Debug.Log("[KSPI]: ResourceSuppliableModule.OnStart created Resource Manager for Vessel " + vessel.GetName() + " for " + resourcename + " with manager Id " + manager.Id + " and overmanager id " + manager.OverManagerId);
                }
            }

            var priorityManager = getSupplyPriorityManager(this.vessel);
            if (priorityManager != null)
                priorityManager.Register(this);
        }

        private void OnJustAboutToBeDestroyed()
        {
            if (!HighLogic.LoadedSceneIsFlight) return;

            Debug.LogWarning("[KSPI]: detecting supplyable part " + part.partInfo.title + " is being destroyed");

            var priority_manager = getSupplyPriorityManager(this.vessel);
            if (priority_manager != null)
                priority_manager.Unregister(this);
        }

        public override void XXXOnFixedUpdate()
        {
            double timeWarpFixedDeltaTime = TimeWarp.fixedDeltaTime;

            updateCounter++;

            if (resources_to_supply == null) return;

            foreach (string resourcename in resources_to_supply)
            {
                var overmanager = getOvermanagerForResource(resourcename);

                ResourceManager resource_manager = null;;

                if (overmanager != null)
                    resource_manager = overmanager.getManagerForVessel(vessel);

                if (resource_manager == null)
                {
                    similarParts = null;
                    resource_manager = CreateResourceManagerForResource(resourcename);

                    Debug.Log("[KSPI]: ResourceSuppliableModule.OnFixedUpdate created Resourcemanager for Vessel " + vessel.GetName() + " for " + resourcename + " with ResourceManagerId " + resource_manager.Id + " and OvermanagerId" + resource_manager.Id);
                }

                if (resource_manager != null)
                {
                    if (resource_manager.PartModule == null || resource_manager.PartModule.vessel != this.vessel || resource_manager.Counter < updateCounter)
                        resource_manager.UpdatePartModule(this);

                    if (resource_manager.PartModule == this)
                        resource_manager.update(updateCounter);
                }
            }

            var priority_manager = getSupplyPriorityManager(this.vessel);
            if (priority_manager != null)
            {
                priority_manager.Register(this);

                if (priority_manager.ProcessingPart == null || priority_manager.ProcessingPart.vessel != this.vessel || priority_manager.Counter < updateCounter)
                    priority_manager.UpdatePartModule(this);

                if (priority_manager.ProcessingPart == this)
                    priority_manager.UpdateResourceSuppliables(updateCounter, timeWarpFixedDeltaTime);
            }
        }

        public void RemoveItselfAsManager()
        {
            foreach (string resourcename in resources_to_supply)
            {
                var overmanager = getOvermanagerForResource(resourcename);

                if (overmanager == null)
                    continue;

                ResourceManager resource_manager = overmanager.getManagerForVessel(vessel);

                if (resource_manager != null && resource_manager.PartModule == this)
                    resource_manager.UpdatePartModule(null);
            }
        }

        public virtual string getResourceManagerDisplayName()
        {
            string displayName = part.partInfo.title;

            if (similarParts == null)
            {
                similarParts = vessel.parts.Where(m => m.partInfo.title == this.part.partInfo.title).ToList();
                partNrInList = 1 + similarParts.IndexOf(this.part);
            }

            if (similarParts.Count > 1)
                displayName += " " + partNrInList;

            return displayName;
        }

        // default priority
        public virtual int getPowerPriority()
        {
            return 2;
        }

        public virtual int getSupplyPriority()
        {
            return getPowerPriority();
        }

        private ResourceManager CreateResourceManagerForResource(string resourcename)
        {
            return getOvermanagerForResource(resourcename).CreateManagerForVessel(this);
        }

        private ResourceOvermanager getOvermanagerForResource(string resourcename)
        {
            return ResourceOvermanager.getResourceOvermanagerForResource(resourcename);
        }

        protected ResourceManager getManagerForVessel(string resourcename)
        {
            return getManagerForVessel(resourcename, vessel);
        }

        private ResourceManager getManagerForVessel(string resourcename, Vessel vessel)
        {
            var overmanager = getOvermanagerForResource(resourcename);
            if (overmanager == null)
            {
                Debug.LogError("[KSPI]: ResourceSuppliableModule failed to find " + resourcename + " Overmanager for " + vessel.name);
                return null;
            }
            return overmanager.getManagerForVessel(vessel);
        }

        private SupplyPriorityManager getSupplyPriorityManager(Vessel vessel)
        {
            return SupplyPriorityManager.GetSupplyPriorityManagerForVessel(vessel);
        }

        public virtual void OnFixedUpdateResourceSuppliable(double fixedDeltaTime)
        {

        }

        public virtual void OnPostResourceSuppliable(double fixedDeltaTime)
        {

        }
    }
    */
}
