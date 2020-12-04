using KIT.Interfaces;
using KIT.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KIT.ResourceScheduler
{
    public class ResourceManager : IResourceManager, IResourceScheduler
    {
        private ICheatOptions myCheatOptions = RealCheatOptions.Instance;
        private IVesselResources vesselResources;
        private static double fudgeFactor = 0.99999;
        private Dictionary<ResourceName, double> currentResources;

        private double fixedDeltaTime;

        HashSet<IKITMod> fixedUpdateCalledMods = new HashSet<IKITMod>(128);
        //HashSet<IKITMod> modsCurrentlyRunning = new HashSet<IKITMod>(128);
        List<IKITMod> modsCurrentlyRunning = new List<IKITMod>(128);

        public bool UseThisToHelpWithTesting;


        public ResourceManager(IVesselResources vesselResources, ICheatOptions cheatOptions)
        {
            this.vesselResources = vesselResources;
            this.myCheatOptions = cheatOptions;

            resourceFlow = new List<KeyValuePair<IKITMod, double>>[(int)ResourceName.EndResource];
            for (int i = 0; i < (int) ResourceName.EndResource; i++)
            {
                resourceFlow[i] = new List<KeyValuePair<IKITMod, double>>(64);
            }
        }

        #region IResourceManager implementation
        ICheatOptions IResourceManager.CheatOptions() => myCheatOptions;
        private bool inExecuteKITModules;

        public List<KeyValuePair<IKITMod, double>>[] resourceFlow;

        /// <summary>
        /// Called by the IKITMod to consume resources present on a vessel. It automatically converts the wanted amount by the appropriate value to
        /// give you a per-second resource consumption.
        /// </summary>
        /// <param name="resource">Resource to consume</param>
        /// <param name="wanted">How much you want</param>
        /// <returns>How much you got</returns>
        double IResourceManager.ConsumeResource(ResourceName resource, double wanted)
        {
            KITResourceSettings.ValidateResource(resource);

            if (!inExecuteKITModules)
            {
                Debug.Log("[KITResourceManager.ConsumeResource] don't do this.");
                return 0;
            }
            if (myCheatOptions.InfiniteElectricity && resource == ResourceName.ElectricCharge)
            {
                resourceFlow[(int)ResourceName.ElectricCharge].Add(new KeyValuePair<IKITMod, double>(modsCurrentlyRunning.Last(), -wanted));
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
            if (obtainedAmount >= modifiedAmount)
            {
                resourceFlow[(int)resource].Add(new KeyValuePair<IKITMod, double>(modsCurrentlyRunning.Last(), -wanted));
                return wanted;
            }

            // XXX - todo. At this stage, we might want to try requesting more than we need to refill the resources on hand.
            // Some % of total capacity or something like that? Might reduce some future calls

            // Convert to seconds
            obtainedAmount = wanted * (obtainedAmount / modifiedAmount);
            obtainedAmount = CallVariableSuppliers(resource, obtainedAmount, wanted);

            //return obtainedAmount;

            // is it close enough to being fully requested? (accounting for precision issues)
            var result = (modifiedAmount * fudgeFactor <= obtainedAmount) ? wanted : wanted * (obtainedAmount / modifiedAmount);
            resourceFlow[(int)resource].Add(new KeyValuePair<IKITMod, double>(modsCurrentlyRunning.Last(), -result));
            return result;
        }

        double IResourceManager.FixedDeltaTime() => fixedDeltaTime;

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
        void IResourceManager.ProduceResource(ResourceName resource, double amount)
        {
            KITResourceSettings.ValidateResource(resource);

            //Debug.Log($"ProduceResource {resource} - {amount}");

            if (!inExecuteKITModules)
            {
                Debug.Log("[KITResourceManager.ProduceResource] don't do this.");
                return;
            }

            resourceFlow[(int)resource].Add(new KeyValuePair<IKITMod, double>(modsCurrentlyRunning.Last(), amount));

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

        /// <summary>
        /// ExecuteKITModules() does the heavy work of executing all the IKITMod FixedUpdate() equiv. It needs to be careful to ensure
        /// it is using the most recent list of modules, hence the odd looping code. In the case of no part updates are needed, it's
        /// relatively optimal.
        /// </summary>
        /// <param name="deltaTime">the amount of delta time that each module should use</param>
        /// <param name="resourceAmounts">What resources are available for this call.</param>
        void IResourceScheduler.ExecuteKITModules(double deltaTime, ref Dictionary<ResourceName, double> resourceAmounts, ref Dictionary<ResourceName, double> resourceMaxAmounts)
        {
            int index = 0;

            currentResources = resourceAmounts;

            tappedOutMods.Clear();
            fixedUpdateCalledMods.Clear();

            if (modsCurrentlyRunning.Count > 0 && complainedToWaiterAboutOrder == false)
            {
                Debug.Log($"[ResourceManager.ExecuteKITModules] modsCurrentlyRunning.Count != 0. there may be resource production / consumption issues.");
                complainedToWaiterAboutOrder = true;
                modsCurrentlyRunning.Clear();
            }

            if (vesselResources.VesselModified())
            {
                RefreshActiveModules();
                if (activeKITModules.Count == 0) return;
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

                if (vesselResources.VesselModified())
                {
                    index = 0;
                    RefreshActiveModules();
                }

                if(modsCurrentlyRunning.Last() != mod)
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

        private double CallVariableSuppliers(ResourceName resource, double obtainedAmount, double originalAmount)
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
                    KITMod.KITFixedUpdate(this);
                }

                double perSecondAmount = originalAmount * (1 - (obtainedAmount / originalAmount));

                try
                {
                    var canContinue = mod.ProvideResource(this, resource, perSecondAmount);
                    if (!canContinue) tappedOutMods.Add(mod);
                }
                catch (Exception ex)
                {
                    if (UseThisToHelpWithTesting) throw;
                    Debug.Log($"[KITResourceManager.callVariableSuppliers] calling KITMod {KITMod.KITPartName()} resulted in {ex.ToString()}");
                }

                var tmp = Math.Min(currentResources[resource], perSecondAmount);
                currentResources[resource] -= tmp;
                obtainedAmount += tmp;

                modsCurrentlyRunning.Remove(KITMod);

                if (obtainedAmount >= originalAmount) return originalAmount;
            }

            return obtainedAmount;
        }
        #endregion

    }
}
