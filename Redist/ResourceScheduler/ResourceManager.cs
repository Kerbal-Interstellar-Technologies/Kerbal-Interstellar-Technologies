using KIT.Interfaces;
using KIT.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KIT.ResourceScheduler
{
    public struct ResourceProduction : IResourceProduction
    {
        internal double _currentlyRequested;
        internal double _currentlySupplied;
        internal double _previouslyRequested;
        internal double _previouslySupplied;

        public double CurrentlyRequested() => _currentlyRequested;
        public double CurrentSupplied() => _currentlySupplied;

        public double PreviousUnmetDemand() => Math.Max(0, _previouslyRequested - _previouslySupplied);
        public bool PreviousDemandMet() => _previouslySupplied >= _previouslyRequested;

        public double PreviouslyRequested() => _previouslyRequested;
        public double PreviouslySupplied() => _previouslySupplied;

        public double PreviousSurplus() => Math.Max(0, _previouslySupplied - _previouslyRequested);

        public bool PreviousDataSupplied() => _previouslySupplied != 0 && _previouslyRequested != 0;
    }

    public struct PerPartResourceInformation
    {
        public double amount, maxAmount;
    }

    public class ResourceManager : IResourceManager, IResourceScheduler
    {
        private ICheatOptions myCheatOptions = RealCheatOptions.Instance;
        private IVesselResources vesselResources;
        private static double fudgeFactor = 0.99999;
        private Dictionary<ResourceName, double> currentResources;
        private Dictionary<ResourceName, double> currentMaxResources;

        private double fixedDeltaTime;

        HashSet<IKITMod> fixedUpdateCalledMods = new HashSet<IKITMod>(128);
        List<IKITMod> modsCurrentlyRunning = new List<IKITMod>(128);

        public bool UseThisToHelpWithTesting;

        public Dictionary<ResourceName, Dictionary<IKITMod, PerPartResourceInformation>> ModConsumption;
        public Dictionary<ResourceName, Dictionary<IKITMod, PerPartResourceInformation>> ModProduction;

        public ResourceManager(IVesselResources vesselResources, ICheatOptions cheatOptions)
        {
            this.vesselResources = vesselResources;
            this.myCheatOptions = cheatOptions;

            resourceProductionStats = new ResourceProduction[(int)(ResourceName.WasteHeat - ResourceName.ElectricCharge) + 1];

            ModProduction = new Dictionary<ResourceName, Dictionary<IKITMod, PerPartResourceInformation>>();
            ModConsumption = new Dictionary<ResourceName, Dictionary<IKITMod, PerPartResourceInformation>>();
            for (var i = ResourceName.ElectricCharge; i <= ResourceName.WasteHeat; i++)
            {
                ModProduction[i] = new Dictionary<IKITMod, PerPartResourceInformation>();
                ModConsumption[i] = new Dictionary<IKITMod, PerPartResourceInformation>();
            }
        }

        #region IResourceManager implementation
        ICheatOptions IResourceManager.CheatOptions() => myCheatOptions;
        private bool inExecuteKITModules;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TrackableResource(ResourceName resource) => resource >= ResourceName.ElectricCharge && resource <= ResourceName.WasteHeat;

        /// <summary>
        /// Called by the IKITMod to consume resources present on a vessel. It automatically converts the wanted amount by the appropriate value to
        /// give you a per-second resource consumption.
        /// </summary>
        /// <param name="resource">Resource to consume</param>
        /// <param name="wanted">How much you want</param>
        /// <returns>How much you got</returns>
        public double ConsumeResource(ResourceName resource, double wanted)
        {
            KITResourceSettings.ValidateResource(resource);

            PerPartResourceInformation tmpPPRI;
            if (!inExecuteKITModules)
            {
                Debug.Log("[KITResourceManager.ConsumeResource] don't do this.");
                return 0;
            }
            tmpPPRI.amount = tmpPPRI.maxAmount = 0;

            var lastMod = modsCurrentlyRunning.Last();

            var trackResourceUsage = TrackableResource(resource);
            if (trackResourceUsage)
            {
                resourceProductionStats[resource - ResourceName.ElectricCharge]._currentlyRequested += wanted;

                if (!ModConsumption[resource].ContainsKey(lastMod))
                {
                    ModConsumption[resource][lastMod] = new PerPartResourceInformation();
                }
                tmpPPRI = ModConsumption[resource][lastMod];
                tmpPPRI.maxAmount += wanted;
            }

            if (myCheatOptions.InfiniteElectricity && resource == ResourceName.ElectricCharge)
            {
                tmpPPRI.amount += wanted;
                ModConsumption[resource][lastMod] = tmpPPRI;
                return wanted;
            }

            if (currentResources.ContainsKey(resource) == false)
            {
                currentResources[resource] = 0;
            }

            double obtainedAmount = 0;
            double modifiedAmount = wanted * fixedDeltaTime;

            var tmp = Math.Min(currentResources[resource], modifiedAmount);
            obtainedAmount += tmp;
            currentResources[resource] -= tmp;

            if (trackResourceUsage && tmp > 0)
                resourceProductionStats[resource - ResourceName.ElectricCharge]._currentlySupplied += tmp;

            if (obtainedAmount >= modifiedAmount)
            {
                tmpPPRI.amount += wanted;
                if(trackResourceUsage) ModConsumption[resource][lastMod] = tmpPPRI;

                return wanted;
            }

            // Convert to seconds
            obtainedAmount = wanted * (obtainedAmount / modifiedAmount);
            obtainedAmount = CallVariableSuppliers(resource, obtainedAmount, wanted, currentMaxResources[resource]); ;

            // We do not need to account for _currentlySupplied here, as the modules called above will call
            // ProduceResource which credits the _currentlySupplied field here.

            // is it close enough to being fully requested? (accounting for precision issues)
            var result = (obtainedAmount < (wanted * fudgeFactor)) ? wanted * (obtainedAmount / wanted) : wanted;

            tmpPPRI.amount += result;
            if (trackResourceUsage) ModConsumption[resource][lastMod] = tmpPPRI;

            return result;
        }

        public double FixedDeltaTime() => fixedDeltaTime;

        void RefreshActiveModules()
        {
            vesselResources.VesselKITModules(ref activeKITModules, ref variableSupplierModules);
        }

        /// <summary>
        /// Called by the IKITMod to produce resources on a vessel.It automatically converts the amount by the appropriate value to
        /// give a per-second resource production.
        /// </summary>
        /// <param name="resource">Resource to produce</param>
        /// <param name="amount">Amount you are providing</param>
        public double ProduceResource(ResourceName resource, double amount, double max = -1)
        {
            KITResourceSettings.ValidateResource(resource);

            if (!inExecuteKITModules)
            {
                Debug.Log("[KITResourceManager.ProduceResource] don't do this.");
                return 0;
            }

            if (TrackableResource(resource))
            {
                resourceProductionStats[resource - ResourceName.ElectricCharge]._currentlySupplied += amount;

                var lastMod = modsCurrentlyRunning.Last();

                if (ModProduction[resource].ContainsKey(lastMod) == false)
                {
                    var tmpPPRI = new PerPartResourceInformation();
                    tmpPPRI.amount = amount;
                    tmpPPRI.maxAmount = (max == -1) ? 0 : max;
                    ModProduction[resource][lastMod] = tmpPPRI;
                }
                else
                {
                    var tmpPPRI = ModProduction[resource][lastMod];
                    tmpPPRI.amount += amount;
                    tmpPPRI.maxAmount += (max == -1) ? 0 : max;
                    ModProduction[resource][lastMod] = tmpPPRI;
                }
            }


            if (resource == ResourceName.WasteHeat && myCheatOptions.IgnoreMaxTemperature) return 0;

            if (currentResources.ContainsKey(resource) == false)
            {
                currentResources[resource] = 0;
            }
            currentResources[resource] += amount * fixedDeltaTime;

            return amount;
        }

        #endregion
        #region IResourceScheduler implementation

        private List<IKITMod> activeKITModules = new List<IKITMod>(128);
        private Dictionary<ResourceName, List<IKITVariableSupplier>> variableSupplierModules = new Dictionary<ResourceName, List<IKITVariableSupplier>>();

        private bool complainedToWaiterAboutOrder;

        public ulong KITSteps;

        /// <summary>
        /// ExecuteKITModules() does the heavy work of executing all the IKITMod FixedUpdate() equiv. It needs to be careful to ensure
        /// it is using the most recent list of modules, hence the odd looping code. In the case of no part updates are needed, it's
        /// relatively optimal.
        /// </summary>
        /// <param name="deltaTime">the amount of delta time that each module should use</param>
        /// <param name="resourceAmounts">What resources are available for this call.</param>
        public void ExecuteKITModules(double deltaTime, ref Dictionary<ResourceName, double> resourceAmounts, ref Dictionary<ResourceName, double> resourceMaxAmounts)
        {
            var index = 0;

            KITSteps++;

            currentResources = resourceAmounts;
            currentMaxResources = resourceMaxAmounts;

            // Cycle the resource tracking data over.
            for (var i = 0; i < ResourceName.WasteHeat - ResourceName.ElectricCharge; i++)
            {
                var currentResourceProduction = resourceProductionStats[i];

                currentResourceProduction._previouslyRequested = currentResourceProduction._currentlyRequested;
                currentResourceProduction._previouslySupplied = currentResourceProduction._currentlySupplied;

                currentResourceProduction._currentlyRequested = currentResourceProduction._currentlySupplied = 0;
            }

            for (var i = ResourceName.ElectricCharge; i <= ResourceName.WasteHeat; i++)
            {
                ModConsumption[i].Clear();
                ModProduction[i].Clear();
            }

            tappedOutMods.Clear();
            fixedUpdateCalledMods.Clear();

            if (modsCurrentlyRunning.Count > 0)
            {
                if (complainedToWaiterAboutOrder == false)
                    Debug.Log($"[ResourceManager.ExecuteKITModules] URGENT modsCurrentlyRunning.Count != 0. there may be resource production / consumption issues.");
                else
                    complainedToWaiterAboutOrder = true;

                modsCurrentlyRunning.Clear();
            }

            if (vesselResources.VesselModified())
            {
                RefreshActiveModules();
                if (activeKITModules.Count == 0)
                {
                    Debug.Log($"No KIT Modules found");
                    return;
                }
                Debug.Log("[resource manager] got {activeKITModules.Count} modules and also {variableSupplierModules.Count} variable modules");
            }

            if (activeKITModules.Count >= 1)
            {
                var dc = activeKITModules[0] as IDCElectricalSystem;
                if (dc != null)
                {
                    var ppri = new PerPartResourceInformation();
                    ppri.amount = dc.unallocatedElectricChargeConsumption();
                    ModConsumption[ResourceName.ElectricCharge][activeKITModules[0]] = ppri;
                }
            }

            inExecuteKITModules = true;

            fixedDeltaTime = deltaTime;

            while (index != activeKITModules.Count)
            {
                var mod = activeKITModules[index];
                index++;

                if (modsCurrentlyRunning.Contains(mod))
                {
                    Debug.Log($"[KITResourceManager.ExecuteKITModules] This module {mod.KITPartName()} should not be marked busy at this stage");
                    continue;
                }
                modsCurrentlyRunning.Add(mod);

                InitializeModuleIfNeeded(mod);

                if (vesselResources.VesselModified())
                {
                    index = 0;
                    RefreshActiveModules();
                }

                if (modsCurrentlyRunning.Last() != mod)
                {
                    // there is an ordering problem in the above mod.KITFixedUpdate(), and we did not correctly track which module is
                    // currently running.
                    throw new InvalidOperationException("[KITResourceManager.ExecuteKITModules] the currently running mod is not the last running mod");
                }
                modsCurrentlyRunning.Remove(mod);
            }

            RechargeBatteries(ref resourceAmounts, ref resourceMaxAmounts);
            vesselResources.OnKITProcessingFinished(this);

            currentResources = null;
            inExecuteKITModules = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RechargeBatteries(ref Dictionary<ResourceName, double> resourceAmounts, ref Dictionary<ResourceName, double> resourceMaxAmounts)
        {
            // TODO: should we do this every loop, or should we wait a until a certain % has dropped?

            // Check to see if the variable suppliers can be used to fill any missing EC from the vessel. This will charge
            // any batteries present on the ship.
            if (resourceMaxAmounts.ContainsKey(ResourceName.ElectricCharge) && resourceAmounts.ContainsKey(ResourceName.ElectricCharge))
            {
                double fillBattery = resourceMaxAmounts[ResourceName.ElectricCharge] - resourceAmounts[ResourceName.ElectricCharge];
                if (fillBattery > 0)
                    resourceAmounts[ResourceName.ElectricCharge] += CallVariableSuppliers(ResourceName.ElectricCharge, 0, fillBattery);
            }
        }

        HashSet<IKITVariableSupplier> tappedOutMods = new HashSet<IKITVariableSupplier>(128);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitializeModuleIfNeeded(IKITMod KITMod)
        {
            if (fixedUpdateCalledMods.Contains(KITMod) == false)
            {
                // Hasn't had it's KITFixedUpdate() yet? call that first.
                fixedUpdateCalledMods.Add(KITMod);
                UnityEngine.Profiling.Profiler.BeginSample($"ResourceManager.InitializeModuleIfNeeded.Init.{KITMod.KITPartName()}");

                try
                {
                    KITMod.KITFixedUpdate(this);
                }
                catch (Exception ex)
                {
                    Debug.Log($"[KITResourceManager.InitializeModuleIfNeeded] Exception when processing [{KITMod.KITPartName()}, {(KITMod as PartModule).ClassName}]: {ex.ToString()}");
                }
                finally
                {
                    UnityEngine.Profiling.Profiler.EndSample();
                }
            }
        }

        private double CallVariableSuppliers(ResourceName resource, double obtainedAmount, double originalAmount, double resourceFiller = 0)
        {
            if (variableSupplierModules.ContainsKey(resource) == false) return 0;

            var reducedObtainedAmount = obtainedAmount * fixedDeltaTime;
            var reducedOriginalAmount = originalAmount * fixedDeltaTime;

            foreach (var mod in variableSupplierModules[resource])
            {
                var KITMod = mod as IKITMod;

                if (tappedOutMods.Contains(mod)) continue; // it's tapped out for this cycle.
                if (modsCurrentlyRunning.Contains(KITMod)) continue;

                modsCurrentlyRunning.Add(KITMod);

                InitializeModuleIfNeeded(KITMod);

                double perSecondAmount = originalAmount * (1 - (reducedObtainedAmount / reducedOriginalAmount));

                try
                {
                    UnityEngine.Profiling.Profiler.BeginSample($"ResourceManager.CallVariableSuppliers.ProvideResource.{KITMod.KITPartName()}");

                    var canContinue = mod.ProvideResource(this, resource, perSecondAmount + resourceFiller);
                    if (!canContinue) tappedOutMods.Add(mod);
                }
                catch (Exception ex)
                {
                    if (UseThisToHelpWithTesting) throw;
                    Debug.Log($"[KITResourceManager.callVariableSuppliers] calling KITMod {KITMod.KITPartName()} resulted in {ex.ToString()}");
                }
                finally
                {
                    UnityEngine.Profiling.Profiler.EndSample();
                }

                var tmp = Math.Min(currentResources[resource], reducedOriginalAmount - reducedObtainedAmount);
                currentResources[resource] -= tmp;
                reducedObtainedAmount += tmp;

                modsCurrentlyRunning.Remove(KITMod);

                if (reducedObtainedAmount >= reducedOriginalAmount) return originalAmount;
            }

            return obtainedAmount * (reducedObtainedAmount / reducedOriginalAmount);
        }

        public double ResourceSpareCapacity(ResourceName resourceIdentifier)
        {
            if (currentMaxResources.TryGetValue(resourceIdentifier, out var maxResourceAmount) && currentResources.TryGetValue(resourceIdentifier, out var currentResourceAmount))
                return maxResourceAmount - currentResourceAmount;

            return 0;
        }

        public double ResourceCurrentCapacity(ResourceName resourceIdentifier)
        {
            if (currentResources.TryGetValue(resourceIdentifier, out var currentResourceAmount))
                return currentResourceAmount;

            return 0;
        }

        public double ResourceFillFraction(ResourceName resourceIdentifier)
        {
            if (currentMaxResources.TryGetValue(resourceIdentifier, out var maxResourceAmount) && currentResources.TryGetValue(resourceIdentifier, out var currentResourceAmount))
                return currentResourceAmount / maxResourceAmount;

            return 0;
        }

        private ResourceProduction[] resourceProductionStats;

        public IResourceProduction ResourceProductionStats(ResourceName resourceIdentifier)
        {
            if (resourceIdentifier >= ResourceName.ElectricCharge && resourceIdentifier <= ResourceName.WasteHeat)
                return resourceProductionStats[resourceIdentifier - ResourceName.ElectricCharge];

            return null;
        }
        #endregion

    }
}
