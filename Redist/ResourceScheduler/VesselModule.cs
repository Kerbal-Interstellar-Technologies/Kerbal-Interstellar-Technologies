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

    /// <summary>
    /// KITResourceManager implements the Resource Manager code for Kerbal Interstellar Technologies.
    /// 
    /// <para>It acts as a scheduler for the KIT Part Modules (denoted by the IKITMod interface), calling them by their self reported priority. This occurs 
    /// every FixedUpdate(), and the IKITMods do not implemented their own FixedUpdate() interface.</para>
    /// 
    /// <para>It also manages what resources are available each FixedUpdate() tick for the modules, and once all modules have ran, it finalizes the reults. This eliminates the need for resource buffering implementations.</para>
    /// </summary>
    public class KITResourceVesselModule : VesselModule, IVesselResources
    {
        public bool refreshEventOccurred = true;

        [KSPField] double lastExecuted;
        [KSPField] bool catchUpNeeded;
        ResourceManager resourceManager;
        IResourceScheduler resourceScheduler;

        private bool needsRefresh
        {
            get { return catchUpNeeded || refreshEventOccurred; }
            set { refreshEventOccurred = value; }
        }

        Dictionary<string, DecayConfiguration> resourceDecayConfiguration;

        protected override void OnAwake()
        {
            base.OnAwake();

            if (resourceManager == null)
            {
                resourceManager = new ResourceManager(this, RealCheatOptions.Instance);
                resourceScheduler = resourceManager;
            }

            if (resourceDecayConfiguration == null)
                resourceDecayConfiguration = ResourceDecayConfiguration.Instance();

        }

        private SortedDictionary<ResourceName, SortedDictionary<ResourcePriorityValue, List<IKITVariableSupplier>>> variableSupplierModules = new SortedDictionary<ResourceName, SortedDictionary<ResourcePriorityValue, List<IKITVariableSupplier>>>();

        Dictionary<ResourceName, double> resourceAmounts = new Dictionary<ResourceName, double>(32);
        Dictionary<ResourceName, double> resourceMaxAmounts = new Dictionary<ResourceName, double>(32);

        /// <summary>
        /// FixedUpdate() triggers the ExecuteKITModules() function call above. It implements automatic catch up processing for each module.
        /// </summary>
        public void FixedUpdate()
        {
            if (!vessel.loaded)
            {
                catchUpNeeded = true;
                return;
            }

            if (!HighLogic.LoadedSceneIsFlight || vessel.vesselType == VesselType.SpaceObject ||
                vessel.isEVA || vessel.vesselType == VesselType.Debris) return;

            if (lastExecuted == 0) catchUpNeeded = false;
            double currentTime = Planetarium.GetUniversalTime();
            var deltaTime = lastExecuted - currentTime;
            lastExecuted = currentTime;

            GatherResources(ref resourceAmounts, ref resourceMaxAmounts);
            VesselWideResourceBuffering(resourceAmounts[ResourceName.ElectricCharge]);

            // handle vessel wide EC buffering, discount the buffer used internally so we don't
            // generate EC that will be voided later on. Will be 0 if none present.
            if (resourceMaxAmounts.ContainsKey(ResourceName.ElectricCharge))
                resourceMaxAmounts[ResourceName.ElectricCharge] -= extraECPresent;

            if (catchUpNeeded)
            {
                resourceScheduler.ExecuteKITModules(deltaTime, ref resourceAmounts, ref resourceMaxAmounts);
                catchUpNeeded = false;
            }

            resourceScheduler.ExecuteKITModules(TimeWarp.fixedDeltaTime, ref resourceAmounts, ref resourceMaxAmounts);
            DisperseResources(ref resourceAmounts);
        }

        bool IVesselResources.VesselModified()
        {
            bool ret = catchUpNeeded | refreshEventOccurred;
            refreshEventOccurred = false;

            return ret;
        }

        SortedDictionary<ResourcePriorityValue, List<IKITMod>> sortedModules = new SortedDictionary<ResourcePriorityValue, List<IKITMod>>();
        Dictionary<ResourceName, SortedDictionary<ResourcePriorityValue, List<IKITVariableSupplier>>> tmpVariableSupplierModules = new Dictionary<ResourceName, SortedDictionary<ResourcePriorityValue, List<IKITVariableSupplier>>>();

        List<PartResource> decayPartResourceList = new List<PartResource>(64);

        void IVesselResources.VesselKITModules(ref List<IKITMod> moduleList, ref Dictionary<ResourceName, List<IKITVariableSupplier>> variableSupplierModules)
        {
            // Clear the inputs

            moduleList.Clear();
            variableSupplierModules.Clear();

            // Clear the temporary variables

            sortedModules.Clear();
            tmpVariableSupplierModules.Clear();

            List<IKITMod> KITMods;

            bool hasKITModules;

            foreach (var part in vessel.parts)
            {
                #region Loop over modules
                foreach (var _mod in part.Modules)
                {
                    var mod = _mod as IKITMod;
                    if (mod == null) continue;
                    // Handle the KITFixedUpdate() side of things first.

                    var priority = mod.ResourceProcessPriority();
                    var prepend = (priority & ResourcePriorityValue.SupplierOnlyFlag) == ResourcePriorityValue.SupplierOnlyFlag;
                    priority &= ~ResourcePriorityValue.SupplierOnlyFlag;

                    if (sortedModules.TryGetValue(priority, out KITMods) == false)
                    {
                        sortedModules[priority] = new List<IKITMod>(32);
                    }

                    if (prepend)
                    {
                        sortedModules[priority].Prepend(mod);
                    }
                    else
                    {
                        sortedModules[priority].Append(mod);
                    }

                    // Now handle the variable consumption side of things

                    var supmod = mod as IKITVariableSupplier;
                    if (supmod == null) continue;

                    foreach (ResourceName resource in supmod.ResourcesProvided())
                    {
                        if (tmpVariableSupplierModules.ContainsKey(resource) == false)
                        {
                            tmpVariableSupplierModules[resource] = new SortedDictionary<ResourcePriorityValue, List<IKITVariableSupplier>>();
                        }
                        var modules = tmpVariableSupplierModules[resource];

                        if (modules.ContainsKey(priority) == false)
                        {
                            modules[priority] = new List<IKITVariableSupplier>(16);
                        }

                        if (prepend)
                        {
                            modules[priority].Prepend(supmod);
                        }
                        else
                        {
                            modules[priority].Append(supmod);
                        }

                    }
                }
                #endregion
                #region Loop over resources, looking for parts to decay
                /*
                decayPartResourceList.Clear();

                foreach (var res in part.Resources)
                {
                    if (resourceDecayConfiguration.ContainsKey(res.resourceName) == false) continue;

                }
                */
                // Use an KITDecayResource() module, and look for those above. then add any resources to the decay list.
                #endregion
            }



            if (sortedModules.Count() == 0)
            {
                // Nothing found
                hasKITModules = needsRefresh = false;
                return;
            }

            hasKITModules = true;
            needsRefresh = false;

            foreach (List<IKITMod> list in sortedModules.Values)
            {
                moduleList.AddRange(list);
            }

            foreach (var resource in tmpVariableSupplierModules.Keys)
            {
                variableSupplierModules[resource] = new List<IKITVariableSupplier>(16);

                foreach (var list in tmpVariableSupplierModules[resource].Values)
                {
                    variableSupplierModules[resource].AddRange(list);
                }
            }
        }

        void GatherResources(ref Dictionary<ResourceName, double> amounts, ref Dictionary<ResourceName, double> maxAmounts)
        {
            foreach (var part in vessel.Parts)
            {
                foreach (var resource in part.Resources)
                {
                    if (resource.maxAmount == 0) continue;

                    var resourceID = KITResourceSettings.NameToResource(resource.resourceName);
                    if (resourceID == ResourceName.Unknown)
                    {
                        Debug.Log($"[KITResourceManager.GatherResources] ignoring unknown resource {resource.resourceName}");
                        continue;
                    }

                    if (amounts.ContainsKey(resourceID) == false)
                    {
                        amounts[resourceID] = maxAmounts[resourceID] = 0;
                    }

                    amounts[resourceID] += resource.amount;
                    maxAmounts[resourceID] += resource.maxAmount;
                }
            }
        }

        void DisperseResources(ref Dictionary<ResourceName, double> available)
        {
            foreach (var part in vessel.Parts)
            {
                foreach (var resource in part.Resources)
                {
                    var resourceID = KITResourceSettings.NameToResource(resource.resourceName);
                    if (resourceID == ResourceName.Unknown)
                    {
                        Debug.Log($"[KITResourceManager.DisperseResources] ignoring unknown resource {resource.resourceName}");
                        continue;
                    }

                    if (available.ContainsKey(resourceID) == false) return; // Shouldn't happen
                    if (available[resourceID] == 0) return;

                    var tmp = Math.Min(available[resourceID], resource.maxAmount);
                    available[resourceID] -= tmp;
                    resource.amount = tmp;
                }
            }
        }

        public void OnKITProcessingFinished(IResourceManager resourceManager)
        {
            PerformResourceDecay(resourceManager);
        }

        #region Vessel Wide Decay
        /*
         * Implement decay for across the vessel
         */


        private bool decayDisabled = false;
        private static Dictionary<string, double> decayConfiguration = new Dictionary<string, double>();

        public static void PerformResourceDecayEffect(IResourceManager resourceManager, PartResourceList partResources, Dictionary<String, DecayConfiguration> decayConfiguration)
        {
            DecayConfiguration config;
            double fixedDeltaTime = resourceManager.FixedDeltaTime();

            foreach (var resource in partResources)
            {
                if (resource.amount == 0) continue;
                if (decayConfiguration.TryGetValue(resource.resourceName, out config) == false) continue;

                // Account for decay over time for the resource amount.
                double n_0 = resource.amount;
                resource.amount = n_0 * Math.Exp(-config.decayConstant * fixedDeltaTime);

                // As this uses the ProduceResource API, we get time handling done for free.
                var decayAmount = Math.Exp(-config.decayConstant);
                var n_change = n_0 - (n_0 * decayAmount);

                resourceManager.ProduceResource(config.decayProduct, n_change * config.densityRatio);
            }
        }

        private void PerformResourceDecay(IResourceManager resourceManager)
        {
            if (decayDisabled || resourceManager.CheatOptions().UnbreakableJoints) return;

            var configuration = ResourceDecayConfiguration.Instance();

            foreach (var part in vessel.parts)
            {
                PerformResourceDecayEffect(resourceManager, part.Resources, configuration);
            }
        }
        #endregion

        #region Vessel Wide Resource Buffering

        public double extraECPresent = 0;

        private PartResource electricChargeBufferPartResource;
        private bool inHighWarp;
        double originalMaxAmount;
        public const double timeWarpCutoff = 50;
        public const double initialECBufferSize = 50000;


        public int warpECCountdown;
        public double instrumentECChangeStart;
        public double electricChargeAmountAtStart;

        public double chargeRatio;
        public double previousTimewarp;

        private void VesselWideResourceBuffering(double currentEC)
        {
            // warp -> non warp transition
            if (inHighWarp && Time.fixedDeltaTime < timeWarpCutoff)
            {
                Debug.Log($"[VesselResourceManager.VesselWideResourceBuffering] transitioning from high warp -> low warp");

                inHighWarp = false;
                if (electricChargeBufferPartResource != null)
                {
                    electricChargeBufferPartResource.maxAmount = originalMaxAmount;
                    electricChargeBufferPartResource = null;
                }
                instrumentECChangeStart = 0;
                extraECPresent = 0;

                return;
            }

            // non-warp -> warp transition
            if (!inHighWarp && Time.fixedDeltaTime >= timeWarpCutoff)
            {
                Debug.Log($"[VesselResourceManager.VesselWideResourceBuffering] transitioning from low warp -> high warp");
                warpECCountdown = 4;
                inHighWarp = true;
                // Find a part to increase for buffer.
                foreach (var part in vessel.parts)
                {
                    electricChargeBufferPartResource = part.Resources.Get("ElectricCharge");
                    if (electricChargeBufferPartResource != null) break;
                }
                if (electricChargeBufferPartResource == null)
                {
                    Debug.Log($"[VesselResourceManager.VesselWideResourceBuffering] low warp -> high warp transition - can't find part to buffer in");
                    return;
                }

                originalMaxAmount = electricChargeBufferPartResource.maxAmount;
                electricChargeBufferPartResource.maxAmount += initialECBufferSize;
                extraECPresent = initialECBufferSize;

                instrumentECChangeStart = Planetarium.GetUniversalTime();
                electricChargeAmountAtStart = currentEC;
                previousTimewarp = 0;
                return;
            }

            // warp -> warp transition, after some FixedUpdate cycles
            if (inHighWarp && Time.fixedDeltaTime >= timeWarpCutoff && --warpECCountdown > 0)
            {
                Debug.Log($"[VesselResourceManager.VesselWideResourceBuffering] transitioning from low warp -> high warp");
                var deltaTime = Planetarium.GetUniversalTime() - instrumentECChangeStart;
                var deltaEC = currentEC - electricChargeAmountAtStart;

                // warp -> warp transition, negative EC.
                if (currentEC < electricChargeAmountAtStart)
                {
                    Debug.Log($"[VesselResourceManager.VesselWideResourceBuffering] EC will deplete, regardless of buffering");
                    // EC will deplete. XXX: How do we stop time warp for them / alert them.
                    electricChargeBufferPartResource.maxAmount = originalMaxAmount;
                    return;
                }

                // warp -> warp transition, positive / equal EC
                chargeRatio = Math.Max(20, deltaEC) / deltaTime;
                previousTimewarp = 0;
                // deliberate fall through
            }

            // warp -> warp transition, after counting down several ticks.
            if (inHighWarp && warpECCountdown == 0 && previousTimewarp != Time.fixedDeltaTime)
            {
                Debug.Log($"[VesselResourceManager.VesselWideResourceBuffering] recalculating buffer size");
                previousTimewarp = Time.fixedDeltaTime;
                extraECPresent = (initialECBufferSize * chargeRatio) * Time.fixedDeltaTime;
                electricChargeBufferPartResource.maxAmount = originalMaxAmount + extraECPresent;
                return;
            }

            // nothing worth writing home about.
        }
        #endregion
    }
}
