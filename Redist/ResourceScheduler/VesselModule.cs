using KIT.Interfaces;
using KIT.Resources;
using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KIT.ResourceScheduler
{

    // ReSharper disable once InconsistentNaming
    public class KITDCElectricalSystem : IKITMod, IDCElectricalSystem
    {
        public double UnallocatedElectricChargeConsumption;

        public void KITFixedUpdate(IResourceManager resMan)
        {
        }

        private string _KITPartName;
        public string KITPartName()
        {
            if (string.IsNullOrEmpty(_KITPartName))
            {
                _KITPartName = Localizer.Format("#LOC_KIT_DC_Electrical_System");
            }

            return _KITPartName;
        }

        public ResourcePriorityValue ResourceProcessPriority() => ResourcePriorityValue.Fifth;

        double IDCElectricalSystem.UnallocatedElectricChargeConsumption() => UnallocatedElectricChargeConsumption;
    }

    /// <summary>
    /// KITResourceManager implements the Resource Manager code for Kerbal Interstellar Technologies.
    /// 
    /// <para>It acts as a scheduler for the KIT Part Modules (denoted by the IKITMod interface), calling them by their self reported priority. This occurs 
    /// every FixedUpdate(), and the IKITMods do not implemented their own FixedUpdate() interface.</para>
    /// 
    /// <para>It also manages what resources are available each FixedUpdate() tick for the modules, and once all modules have ran, it finalizes the results. This eliminates the need for resource buffering implementations.</para>
    /// </summary>
    public class KITResourceVesselModule : VesselModule, IVesselResources
    {
        public bool RefreshEventOccurred = true;

        [KSPField] double _lastExecuted;
        [KSPField] bool _catchUpNeeded;
        public ResourceManager ResourceManager;
        IResourceScheduler _resourceScheduler;

        private bool NeedsRefresh
        {
            get => _catchUpNeeded || RefreshEventOccurred;
            set => RefreshEventOccurred = value;
        }

        Dictionary<string, DecayConfiguration> _resourceDecayConfiguration;

        private KITDCElectricalSystem _dcSystem;
        public VesselHeatDissipation VesselHeatDissipation;

        protected override void OnAwake()
        {
            base.OnAwake();

            if (ResourceManager == null)
            {
                ResourceManager = new ResourceManager(this, RealCheatOptions.Instance);
                _resourceScheduler = ResourceManager;
            }

            if (_resourceDecayConfiguration == null)
                _resourceDecayConfiguration = ResourceDecayConfiguration.Instance();

            if (_dcSystem == null)
                _dcSystem = new KITDCElectricalSystem();

            if (VesselHeatDissipation == null)
                VesselHeatDissipation = new VesselHeatDissipation(vessel);

            RefreshEventOccurred = true;
        }

        private SortedDictionary<ResourceName, SortedDictionary<ResourcePriorityValue, List<IKITVariableSupplier>>> variableSupplierModules = new SortedDictionary<ResourceName, SortedDictionary<ResourcePriorityValue, List<IKITVariableSupplier>>>();

        Dictionary<ResourceName, double> _resourceAmounts = new Dictionary<ResourceName, double>(32);
        Dictionary<ResourceName, double> _resourceMaxAmounts = new Dictionary<ResourceName, double>(32);

        private double _trackElectricChargeUsage;

        /// <summary>
        /// FixedUpdate() triggers the ExecuteKITModules() function call above. It implements automatic catch up processing for each module.
        /// </summary>
        public void FixedUpdate()
        {
            if (!vessel.loaded)
            {
                _catchUpNeeded = true;
                return;
            }

            if (vessel.vesselType == VesselType.SpaceObject || vessel.isEVA || vessel.vesselType == VesselType.Debris) return;

            if (_lastExecuted == 0) _catchUpNeeded = false;
            double currentTime = Planetarium.GetUniversalTime();
            var deltaTime = _lastExecuted - currentTime;
            _lastExecuted = currentTime;

            GatherResources(ref _resourceAmounts, ref _resourceMaxAmounts);

            _dcSystem.UnallocatedElectricChargeConsumption = Math.Max(0, _trackElectricChargeUsage - _resourceAmounts[ResourceName.ElectricCharge]);

            if (_catchUpNeeded)
            {
                _resourceScheduler.ExecuteKITModules(deltaTime, ref _resourceAmounts, ref _resourceMaxAmounts);
                _catchUpNeeded = false;
            }

            _resourceScheduler.ExecuteKITModules(TimeWarp.fixedDeltaTime, ref _resourceAmounts, ref _resourceMaxAmounts);

            _trackElectricChargeUsage = _resourceAmounts[ResourceName.ElectricCharge];
            DisperseResources(ref _resourceAmounts, ref _resourceMaxAmounts);
        }

        bool IVesselResources.VesselModified()
        {
            bool ret = _catchUpNeeded | RefreshEventOccurred;
            RefreshEventOccurred = false;

            return ret;
        }

        readonly SortedDictionary<ResourcePriorityValue, List<IKITMod>> sortedModules = new SortedDictionary<ResourcePriorityValue, List<IKITMod>>();
        readonly Dictionary<ResourceName, SortedDictionary<ResourcePriorityValue, List<IKITVariableSupplier>>> tmpVariableSupplierModules = new Dictionary<ResourceName, SortedDictionary<ResourcePriorityValue, List<IKITVariableSupplier>>>();

        List<PartResource> _decayPartResourceList = new List<PartResource>(64);

        void IVesselResources.VesselKITModules(ref List<IKITMod> moduleList, ref Dictionary<ResourceName, List<IKITVariableSupplier>> variableSupplierModules)
        {
            // Clear the inputs

            moduleList.Clear();
            moduleList.Add(_dcSystem);

            variableSupplierModules.Clear();

            // Clear the temporary variables

            sortedModules.Clear();
            tmpVariableSupplierModules.Clear();

            List<IKITMod> KITMods;

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
                        sortedModules[priority].Insert(0, mod);
                    }
                    else
                    {
                        sortedModules[priority].Add(mod);
                    }

                    // Now handle the variable consumption side of things

                    var supplierModule = mod as IKITVariableSupplier;
                    if (supplierModule == null) continue;

                    foreach (ResourceName resource in supplierModule.ResourcesProvided())
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
                            modules[priority].Insert(0, supplierModule);
                        }
                        else
                        {
                            modules[priority].Add(supplierModule);
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

            if (!sortedModules.Any())
            {
                // Nothing found
                NeedsRefresh = false;
                return;
            }

            NeedsRefresh = false;

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

            moduleList.Add(VesselHeatDissipation);
        }

        void GatherResources(ref Dictionary<ResourceName, double> amounts, ref Dictionary<ResourceName, double> maxAmounts)
        {
            _resourceAmounts.Clear();
            _resourceMaxAmounts.Clear();

            foreach (var part in vessel.Parts)
            {
                foreach (var resource in part.Resources)
                {
                    if (resource.maxAmount == 0) continue;

                    var resourceID = KITResourceSettings.NameToResource(resource.resourceName);
                    if (resourceID == ResourceName.Unknown)
                    {
                        //Debug.Log($"[KITResourceManager.GatherResources] ignoring Unknown resource {resource.resourceName}");
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

        private void HandleChargedParticles(ref Dictionary<ResourceName, double> available, ref Dictionary<ResourceName, double> maxAmounts)
        {
            // Per FreeThinker, "charged particle  power should technically not be able to be stored. The only reason it existed was to have some consumption
            // balancing when there are more than one consumer.", "charged particles should become Thermal heat when unconsumed and if no Thermal
            // storage available it should become waste heat."

            if (available.TryGetValue(ResourceName.ChargedParticle, out var chargedParticleAmount))
            {
                _resourceMaxAmounts.Remove(ResourceName.ChargedParticle);
                available.Remove(ResourceName.ChargedParticle);

                // Store any charged particles in either WasteHeat or ThermalPower
                if (available.ContainsKey(ResourceName.ThermalPower) == false)
                {
                    available[ResourceName.WasteHeat] += chargedParticleAmount;
                }
                else
                {
                    available[ResourceName.ThermalPower] += chargedParticleAmount;
                }
            }

            if (available.TryGetValue(ResourceName.ThermalPower, out var thermalPowerAmount))
            {
                // Have we exceeded the storage capacity for Thermal Power?
                if (_resourceMaxAmounts.ContainsKey(ResourceName.ThermalPower))
                {
                    var diff = thermalPowerAmount - _resourceMaxAmounts[ResourceName.ThermalPower];

                    if (diff > 0)
                    {
                        available[ResourceName.WasteHeat] += diff;
                        available[ResourceName.ThermalPower] = _resourceMaxAmounts[ResourceName.ThermalPower];
                    }
                }
                else
                {
                    available.Remove(ResourceName.ThermalPower);
                    available[ResourceName.WasteHeat] += thermalPowerAmount;
                }
            }
        }

        void DisperseResources(ref Dictionary<ResourceName, double> available, ref Dictionary<ResourceName, double> maxAmounts)
        {
            HandleChargedParticles(ref available, ref maxAmounts);

            // Disperse the resources throughout the vessel

            foreach (var part in vessel.Parts)
            {
                foreach (var resource in part.Resources)
                {
                    var resourceID = KITResourceSettings.NameToResource(resource.resourceName);
                    if (resourceID == ResourceName.Unknown)
                    {
                        //Debug.Log($"[KITResourceManager.DisperseResources] ignoring Unknown resource {resource.resourceName}");
                        continue;
                    }

                    if (available.ContainsKey(resourceID) == false || maxAmounts.ContainsKey(resourceID) == false) return;

                    var mult = Math.Min(1, (available[resourceID] / maxAmounts[resourceID]));
                    resource.amount = resource.maxAmount * mult;
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
            
            double fixedDeltaTime = resourceManager.FixedDeltaTime();

            foreach (var resource in partResources)
            {
                DecayConfiguration config;
                
                if (resource.amount == 0) continue;
                if (decayConfiguration.TryGetValue(resource.resourceName, out config) == false) continue;

                // Account for decay over time for the resource amount.
                double n_0 = resource.amount;
                resource.amount = n_0 * Math.Exp(-config.DecayConstant * fixedDeltaTime);

                // As this uses the ProduceResource API, we get time handling done for free.
                var decayAmount = Math.Exp(-config.DecayConstant);
                var n_change = n_0 - (n_0 * decayAmount);

                resourceManager.ProduceResource(config.DecayProduct, n_change * config.DensityRatio);
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


        private void VesselWideResourceBuffering(double fixedDeltaTime)
        {
            // Removed for now.
        }
        #endregion

    }
}
