using KIT.Interfaces;
using KIT.Resources;
using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace KIT.ResourceScheduler
{
    
    // ReSharper disable once InconsistentNaming
    public class KITDirectCurrentElectricalSystem : IKITModule, IDCElectricalSystem
    {
        public double UnallocatedElectricChargeConsumption;

        public ModuleConfigurationFlags ModuleConfiguration()
        {
            return ModuleConfigurationFlags.First | ModuleConfigurationFlags.SupplierOnly;
        }

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


        double IDCElectricalSystem.UnallocatedElectricChargeConsumption() => UnallocatedElectricChargeConsumption;
    }

    /// <summary>
    /// KITResourceManager implements the Resource Manager code for Kerbal Interstellar Technologies.
    ///
    /// <para>It acts as a scheduler for the KIT Part Modules (denoted by the IKITModule interface), calling them by their self reported priority. This occurs
    /// every FixedUpdate(), and the IKITMods do not implemented their own FixedUpdate() interface.</para>
    ///
    /// <para>It also manages what resources are available each FixedUpdate() tick for the modules, and once all modules have ran, it finalizes the results. This eliminates the need for resource buffering implementations.</para>
    /// </summary>
    public partial class KITResourceVesselModule : VesselModule
    {
        public bool RefreshEventOccurred = true;

        [KSPField] double _lastExecuted;
        [KSPField] bool _catchUpNeeded;

        private bool NeedsRefresh
        {
            get => _catchUpNeeded || RefreshEventOccurred;
            set => RefreshEventOccurred = value;
        }

        Dictionary<string, DecayConfiguration> _resourceDecayConfiguration;

        private KITDirectCurrentElectricalSystem _dcSystem;
        public VesselHeatDissipation VesselHeatDissipation;

        protected override void OnAwake()
        {
            base.OnAwake();

            if (_resourceDecayConfiguration == null)
                _resourceDecayConfiguration = ResourceDecayConfiguration.Instance();

            if (_dcSystem == null)
                _dcSystem = new KITDirectCurrentElectricalSystem();

            if (VesselHeatDissipation == null)
                VesselHeatDissipation = new VesselHeatDissipation(vessel);

            RefreshEventOccurred = true;

            if (EqualityComparer<ResourceData>.Default.Equals(ResourceData, default(ResourceData)))
                ResourceData = new ResourceData(this, RealCheatOptions.Instance);
        }

        // private SortedDictionary<ResourceName, SortedDictionary<int, List<IKITVariableSupplier>>> variableSupplierModules = new SortedDictionary<ResourceName, SortedDictionary<int, List<IKITVariableSupplier>>>();


        private double _trackElectricChargeUsage;

        private bool _useBackgroundProcessing = false;

        public ResourceData ResourceData;

        /// <summary>
        /// FixedUpdate() triggers the ExecuteKITModules() function call above. It implements automatic catch up processing for each module.
        /// </summary>
        public void FixedUpdate()
        {
            if (vessel.vesselType == VesselType.SpaceObject || vessel.isEVA || vessel.vesselType == VesselType.Debris ||
                vessel.vesselType == VesselType.DeployedScienceController || vessel.vesselType == VesselType.DeployedSciencePart)
            {
                return;
            }

            if (!vessel.loaded)
            {
                if (_useBackgroundProcessing)
                {
                    PerformBackgroundProcessing();
                    return;
                }

                _catchUpNeeded = true;
                return;
            }

            if (_lastExecuted == 0) _catchUpNeeded = false;
            double currentTime = Planetarium.GetUniversalTime();
            var deltaTime = _lastExecuted - currentTime;
            _lastExecuted = currentTime;

            GatherResources();

            _dcSystem.UnallocatedElectricChargeConsumption = Math.Max(0, _trackElectricChargeUsage - ResourceData.AvailableResources[ResourceName.ElectricCharge]);

            if (_catchUpNeeded)
            {
                ExecuteKITModules(deltaTime, ResourceData);
                _catchUpNeeded = false;
            }

            ExecuteKITModules(TimeWarp.fixedDeltaTime, ResourceData);
            _trackElectricChargeUsage = ResourceData.AvailableResources[ResourceName.ElectricCharge];

            DisperseResources();
        }

        #region Vessel Interface information

        bool VesselModified()
        {
            bool ret = _catchUpNeeded | RefreshEventOccurred;
            RefreshEventOccurred = false;

            return ret;
        }

        readonly SortedDictionary<int, List<IKITModule>> _sortedModules = new SortedDictionary<int, List<IKITModule>>();
        readonly Dictionary<ResourceName, SortedDictionary<int, List<IKITVariableSupplier>>> _tmpVariableSupplierModules = new Dictionary<ResourceName, SortedDictionary<int, List<IKITVariableSupplier>>>();

        List<PartResource> _decayPartResourceList = new List<PartResource>(64);

        // void VesselKITModules(ref List<IKITModule> moduleList, ref Dictionary<ResourceName, List<IKITVariableSupplier>> variableSupplierModules)

        private Dictionary<IKITModule, LocalResourceData> _localResourceDataInformation =
            new Dictionary<IKITModule, LocalResourceData>(64);
        
        private void RefreshActiveModules(ResourceData resourceData)
        {
            // Clear the inputs

            resourceData.AvailableKITModules.Clear();
            resourceData.AvailableKITModules.Add(_dcSystem);

            resourceData.VariableSupplierModules.Clear();

            // Clear the temporary variables

            _sortedModules.Clear();
            _tmpVariableSupplierModules.Clear();

            foreach (var part in vessel.parts)
            {
                #region Loop over modules
                foreach (var partModule in part.Modules)
                {
                    var mod = partModule as IKITModule;
                    if (mod == null) continue;
                    // Handle the KITFixedUpdate() side of things first.

                    var moduleConfigurationFlags = mod.ModuleConfiguration();

                    var working = (moduleConfigurationFlags & ModuleConfigurationFlags.Disabled) != ModuleConfigurationFlags.Disabled;
                    int priority = (int)(moduleConfigurationFlags & ModuleConfigurationFlags.PriorityMask);
                    bool prepend = (moduleConfigurationFlags & ModuleConfigurationFlags.SupplierOnly) != 0;
                    var local = ((moduleConfigurationFlags & ModuleConfigurationFlags.LocalResources) != 0);

                    if (!working) continue;

                    if (local && !_localResourceDataInformation.ContainsKey(mod))
                        _localResourceDataInformation[mod] = new LocalResourceData(ResourceData, partModule.part);

                    if (!_sortedModules.TryGetValue(priority, out _))
                    {
                        _sortedModules[priority] = new List<IKITModule>(32);
                    }

                    if (prepend)
                    {
                        _sortedModules[priority].Insert(0, mod);
                    }
                    else
                    {
                        _sortedModules[priority].Add(mod);
                    }

                    // Now handle the variable consumption side of things

                    var supplierModule = mod as IKITVariableSupplier;
                    if (supplierModule == null) continue;

                    foreach (ResourceName resource in supplierModule.ResourcesProvided())
                    {
                        if (_tmpVariableSupplierModules.ContainsKey(resource) == false)
                        {
                            _tmpVariableSupplierModules[resource] = new SortedDictionary<int, List<IKITVariableSupplier>>();
                        }
                        var modules = _tmpVariableSupplierModules[resource];

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

            if (!_sortedModules.Any())
            {
                // Nothing found
                NeedsRefresh = false;
                return;
            }

            NeedsRefresh = false;

            foreach (List<IKITModule> list in _sortedModules.Values)
            {
                resourceData.AvailableKITModules.AddRange(list);
            }

            foreach (var resource in _tmpVariableSupplierModules.Keys)
            {
                resourceData.VariableSupplierModules[resource] = new List<IKITVariableSupplier>(16);

                foreach (var list in _tmpVariableSupplierModules[resource].Values)
                {
                    resourceData.VariableSupplierModules[resource].AddRange(list);
                }
            }

            resourceData.AvailableKITModules.Add(VesselHeatDissipation);
        }

        void GatherResources()
        {
            ResourceData.AvailableResources.Clear();
            ResourceData.MaxResources.Clear();

            var ignoreFlow = HighLogic.CurrentGame.Parameters.CustomParams<KITResourceParams>()
                .IgnoreResourceFlowRestrictions;

            foreach (var part in vessel.Parts)
            {
                foreach (var resource in part.Resources.Where(t => t.maxAmount > 0 && (t.flowState || ignoreFlow)))
                {
                    var resourceID = KITResourceSettings.NameToResource(resource.resourceName);
                    if (resourceID == ResourceName.Unknown)
                    {
                        //Debug.Log($"[KITResourceManager.GatherResources] ignoring Unknown resource {resource.resourceName}");
                        continue;
                    }

                    if (ResourceData.AvailableResources.ContainsKey(resourceID) == false)
                    {
                        ResourceData.AvailableResources[resourceID] = ResourceData.MaxResources[resourceID] = 0;
                    }

                    ResourceData.AvailableResources[resourceID] += resource.amount;
                    ResourceData.MaxResources[resourceID] += resource.maxAmount;
                }
            }
        }

        private void HandleSpecialResources()
        {
            // Per FreeThinker, "charged particle  power should technically not be able to be stored. The only reason it existed was to have some consumption
            // balancing when there are more than one consumer.", "charged particles should become Thermal heat when unconsumed and if no Thermal
            // storage available it should become waste heat."

            var hasChargedParticles = ResourceData.AvailableResources.TryGetValue(ResourceName.ChargedParticle, out var chargedParticleAmount);
            var hasThermalPower = ResourceData.AvailableResources.TryGetValue(ResourceName.ThermalPower, out var thermalPowerAmount);

            if (!ResourceData.AvailableResources.ContainsKey(ResourceName.WasteHeat))
            {
                // Seems unlikely to have Charged Particles and/or Thermal Power without WasteHeat, but just in case
                ResourceData.AvailableResources[ResourceName.WasteHeat] = 0;
            }


            if (hasChargedParticles)
            {
                ResourceData.AvailableResources[ResourceName.ChargedParticle] = 0;

                // Store any remaining charged particles in either ThermalPower or WasteHeat
                ResourceData.AvailableResources[hasThermalPower ? ResourceName.ThermalPower : ResourceName.WasteHeat] += chargedParticleAmount;
            }

            if (hasThermalPower)
            {
                // Have we exceeded the storage capacity for Thermal Power?
                if (ResourceData.MaxResources.ContainsKey(ResourceName.ThermalPower))
                {
                    var diff = thermalPowerAmount - ResourceData.MaxResources[ResourceName.ThermalPower];

                    if (diff > 0)
                    {
                        ResourceData.AvailableResources[ResourceName.WasteHeat] += diff;
                        ResourceData.AvailableResources[ResourceName.ThermalPower] = ResourceData.AvailableResources[ResourceName.ThermalPower];
                    }
                }
                else
                {
                    // no Thermal Power storage. Seems unlikely, but just in case

                    ResourceData.AvailableResources.Remove(ResourceName.ThermalPower);
                    ResourceData.AvailableResources[ResourceName.WasteHeat] += thermalPowerAmount;
                }
            }
        }

        void DisperseResources()
        {
            HandleSpecialResources();

            var ignoreFlow = HighLogic.CurrentGame.Parameters.CustomParams<KITResourceParams>()
                .IgnoreResourceFlowRestrictions;

            // Disperse the resources throughout the vessel

            foreach (var part in vessel.Parts)
            {
                foreach (var resource in part.Resources.Where(t => t.maxAmount > 0 && (t.flowState || ignoreFlow)))
                {
                    var resourceID = KITResourceSettings.NameToResource(resource.resourceName);
                    if (resourceID == ResourceName.Unknown)
                    {
                        //Debug.Log($"[KITResourceManager.DisperseResources] ignoring Unknown resource {resource.resourceName}");
                        continue;
                    }

                    if (ResourceData.AvailableResources.ContainsKey(resourceID) == false || ResourceData.MaxResources.ContainsKey(resourceID) == false) return;

                    var tmp = ResourceData.AvailableResources[resourceID] / ResourceData.MaxResources[resourceID];
                    var multiplier = (tmp > 1) ? 1 : (tmp < 0) ? 0 : tmp;
                    resource.amount = resource.maxAmount * multiplier;
                }
            }
        }

        public void OnKITProcessingFinished(IResourceManager resourceManager)
        {
            PerformResourceDecay(resourceManager);
        }

        #endregion

        #region Vessel Wide Decay
        /*
         * Implement decay for across the vessel
         */


        private bool _decayDisabled;
        public static Dictionary<string, double> Configuration { get; } = new Dictionary<string, double>();

        public static void PerformResourceDecayEffect(IResourceManager resourceManager, PartResourceList partResources,
            [NotNull] Dictionary<string, DecayConfiguration> decayConfiguration)
        {
            double fixedDeltaTime = resourceManager.FixedDeltaTime();

            foreach (var resource in partResources)
            {
                if (resource.amount == 0) continue;
                if (decayConfiguration.TryGetValue(resource.resourceName, out var config) == false) continue;

                // Account for decay over time for the resource amount.
                double n0 = resource.amount;
                resource.amount = n0 * Math.Exp(-config.DecayConstant * fixedDeltaTime);

                // As this uses the Produce API, we get time handling done for free.
                var decayAmount = Math.Exp(-config.DecayConstant);
                var nChange = n0 - (n0 * decayAmount);

                resourceManager.Produce(config.DecayProduct, nChange * config.DensityRatio);
            }
        }

        private void PerformResourceDecay(IResourceManager resourceManager)
        {
            if (_decayDisabled || resourceManager.CheatOptions().UnbreakableJoints) return;

            var configuration = ResourceDecayConfiguration.Instance();

            foreach (var part in vessel.parts)
            {
                PerformResourceDecayEffect(resourceManager, part.Resources, configuration);
            }
        }
        #endregion
    }
}
