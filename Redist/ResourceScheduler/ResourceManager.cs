﻿using KIT.Interfaces;
using KIT.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
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
        //HashSet<IKITMod> modsCurrentlyRunning = new HashSet<IKITMod>(128);
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

        // public List<KeyValuePair<IKITMod, double>>[] resourceFlow;

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

            bool trackResourceUsage = resource >= ResourceName.ElectricCharge && resource <= ResourceName.WasteHeat;

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

            if (resource >= ResourceName.ElectricCharge && resource <= ResourceName.WasteHeat && tmp > 0)
                resourceProductionStats[resource - ResourceName.ElectricCharge]._currentlySupplied += tmp;

            if (obtainedAmount >= modifiedAmount)
            {
                // resourceFlow[(int)resource].Add(new KeyValuePair<IKITMod, double>(modsCurrentlyRunning.Last(), -wanted));

                tmpPPRI.amount += wanted;
                if(trackResourceUsage) ModConsumption[resource][lastMod] = tmpPPRI;

                return wanted;
            }

            // XXX - todo. At this stage, we might want to try requesting more than we need to refill the resources on hand.
            // Some % of total capacity or something like that? Might reduce some future calls

            // Convert to seconds
            obtainedAmount = wanted * (obtainedAmount / modifiedAmount);
            obtainedAmount = CallVariableSuppliers(resource, obtainedAmount, wanted, currentMaxResources[resource]); ;

            // We do not need to account for _currentlySupplied here, as the modules called above will call
            // ProduceResource which credits the _currentlySupplied field here.

            // is it close enough to being fully requested? (accounting for precision issues)
            var result = (modifiedAmount * fudgeFactor <= obtainedAmount) ? wanted : wanted * (obtainedAmount / modifiedAmount);

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
        public void ProduceResource(ResourceName resource, double amount, double max = -1)
        {
            KITResourceSettings.ValidateResource(resource);

            if (!inExecuteKITModules)
            {
                Debug.Log("[KITResourceManager.ProduceResource] don't do this.");
                return;
            }

            if (resource >= ResourceName.ElectricCharge && resource <= ResourceName.WasteHeat)
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


            if (resource == ResourceName.WasteHeat && myCheatOptions.IgnoreMaxTemperature) return;

            if (currentResources.ContainsKey(resource) == false)
            {
                currentResources[resource] = 0;
            }
            currentResources[resource] += amount * fixedDeltaTime;
        }

        #endregion
        #region IResourceScheduler implementation

        // private SortedDictionary<ResourcePriorityValue, List<IKITMod>> sortedModules = new SortedDictionary<ResourcePriorityValue, List<IKITMod>>();
        private List<IKITMod> activeKITModules = new List<IKITMod>(128);

        private Dictionary<ResourceName, List<IKITVariableSupplier>> variableSupplierModules = new Dictionary<ResourceName, List<IKITVariableSupplier>>();

        private bool complainedToWaiterAboutOrder;
        private bool firstRun = true;

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
            int index = 0;

            KITSteps++;

            currentResources = resourceAmounts;
            currentMaxResources = resourceMaxAmounts;

            // Cycle the resource tracking data over.
            for (int i = 0; i < (int)(ResourceName.WasteHeat - ResourceName.ElectricCharge); i++)
            {
                resourceProductionStats[i]._previouslyRequested = resourceProductionStats[i]._currentlyRequested;
                resourceProductionStats[i]._previouslySupplied = resourceProductionStats[i]._currentlySupplied;

                resourceProductionStats[i]._currentlyRequested = resourceProductionStats[i]._currentlySupplied =
                    resourceProductionStats[i]._previouslyRequested = resourceProductionStats[i]._previouslySupplied =
                    0;
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

            inExecuteKITModules = true;

            fixedDeltaTime = deltaTime;

            while (index != activeKITModules.Count)
            {
                var mod = activeKITModules[index];
                index++;

                if (fixedUpdateCalledMods.Contains(mod)) continue;
                fixedUpdateCalledMods.Add(mod);

                if (modsCurrentlyRunning.Contains(mod))
                {
                    Debug.Log($"[KITResourceManager.ExecuteKITModules] This module {mod.KITPartName()} should not be marked busy at this stage");
                    continue;
                }

                modsCurrentlyRunning.Add(mod);

                try
                {
                    UnityEngine.Profiling.Profiler.BeginSample($"ResourceManager.ExecuteKITModules.{mod.KITPartName()}");
                    mod.KITFixedUpdate(this);
                }
                catch (Exception ex)
                {
                    if (UseThisToHelpWithTesting) throw;
                    else
                    {
                        // XXX - part names and all that.
                        Debug.Log($"[KITResourceManager.ExecuteKITModules] Exception when processing [{mod.KITPartName()}, {(mod as PartModule).ClassName}]: {ex.ToString()}");
                    }
                }
                finally
                {
                    UnityEngine.Profiling.Profiler.EndSample();
                }

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

            // Check to see if the variable suppliers can be used to fill any missing EC from the vessel. This will charge
            // any batteries present on the ship.
            if (resourceMaxAmounts.ContainsKey(ResourceName.ElectricCharge) && resourceAmounts.ContainsKey(ResourceName.ElectricCharge))
            {
                double fillBattery = resourceMaxAmounts[ResourceName.ElectricCharge] - resourceAmounts[ResourceName.ElectricCharge];
                if (fillBattery > 0)
                    resourceAmounts[ResourceName.ElectricCharge] += CallVariableSuppliers(ResourceName.ElectricCharge, 0, fillBattery);
            }

            vesselResources.OnKITProcessingFinished(this);

            currentResources = null;
            inExecuteKITModules = false;
        }

        HashSet<IKITVariableSupplier> tappedOutMods = new HashSet<IKITVariableSupplier>(128);

        private double CallVariableSuppliers(ResourceName resource, double obtainedAmount, double originalAmount, double resourceFiller = 0)
        {
            if (variableSupplierModules.ContainsKey(resource) == false) return 0;

            foreach (var mod in variableSupplierModules[resource])
            {
                var KITMod = mod as IKITMod;

                if (tappedOutMods.Contains(mod)) continue; // it's tapped out for this cycle.
                if (modsCurrentlyRunning.Contains(KITMod)) continue;

                modsCurrentlyRunning.Add(KITMod);

                if (fixedUpdateCalledMods.Contains(KITMod) == false)
                {
                    // Hasn't had it's KITFixedUpdate() yet? call that first.
                    fixedUpdateCalledMods.Add(KITMod);
                    UnityEngine.Profiling.Profiler.BeginSample($"ResourceManager.CallVariableSuppliers.Init.{KITMod.KITPartName()}");

                    try
                    {
                        KITMod.KITFixedUpdate(this);
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"[KITResourceManager.CallVariableSuppliers] Exception when processing [{KITMod.KITPartName()}, {(mod as PartModule).ClassName}]: {ex.ToString()}");
                    }
                    finally
                    {
                        UnityEngine.Profiling.Profiler.EndSample();
                    }

                }

                double perSecondAmount = originalAmount * (1 - (obtainedAmount / originalAmount));

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

                var tmp = Math.Min(currentResources[resource], perSecondAmount);
                currentResources[resource] -= tmp;
                obtainedAmount += tmp;

                modsCurrentlyRunning.Remove(KITMod);

                if (obtainedAmount >= originalAmount) return originalAmount;
            }

            return obtainedAmount;
        }

        public double ResourceSpareCapacity(ResourceName resourceIdentifier)
        {
            return currentMaxResources[resourceIdentifier] - currentResources[resourceIdentifier];
        }

        public double ResourceCurrentCapacity(ResourceName resourceIdentifier)
        {
            return currentResources[resourceIdentifier];
        }

        public double ResourceFillFraction(ResourceName resourceIdentifier)
        {
            return currentResources[resourceIdentifier] / currentMaxResources[resourceIdentifier];
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
