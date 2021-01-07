using KIT.Interfaces;
using KIT.Resources;
using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KIT.ResourceScheduler
{

    // ReSharper disable once InconsistentNaming
    public class KITDCElectricalSystem : IKITModule, IDCElectricalSystem
    {
        public double UnallocatedElectricChargeConsumption;

        public bool ModuleConfiguration(out int priority, out bool supplierOnly, out bool hasLocalResources)
        {
            priority = 0;
            supplierOnly = false;
            hasLocalResources = false;

            return true;
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
                ResourceManager = new ResourceManager(this);
                _resourceScheduler = ResourceManager;
            }

            if (_resourceDecayConfiguration == null)
                _resourceDecayConfiguration = ResourceDecayConfiguration.Instance();

            if (_dcSystem == null)
                _dcSystem = new KITDCElectricalSystem();

            if (VesselHeatDissipation == null)
                VesselHeatDissipation = new VesselHeatDissipation(vessel);

            RefreshEventOccurred = true;

            if(EqualityComparer<ResourceManagerData>.Default.Equals(_resourceData, default(ResourceManagerData)))
                _resourceData = new ResourceManagerData(ResourceManager, RealCheatOptions.Instance);
        }

        private SortedDictionary<ResourceName, SortedDictionary<int, List<IKITVariableSupplier>>> variableSupplierModules = new SortedDictionary<ResourceName, SortedDictionary<int, List<IKITVariableSupplier>>>();


        private double _trackElectricChargeUsage;

        private ResourceManagerData _resourceData;
        
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

            GatherResources();

            _dcSystem.UnallocatedElectricChargeConsumption = Math.Max(0, _trackElectricChargeUsage - _resourceData.AvailableResources[ResourceName.ElectricCharge]);

            if (_catchUpNeeded)
            {
                _resourceScheduler.ExecuteKITModules(deltaTime, _resourceData);
                _catchUpNeeded = false;
            }

            _resourceScheduler.ExecuteKITModules(TimeWarp.fixedDeltaTime, _resourceData);
            _trackElectricChargeUsage = _resourceData.AvailableResources[ResourceName.ElectricCharge];
            
            DisperseResources();
        }

        bool IVesselResources.VesselModified()
        {
            bool ret = _catchUpNeeded | RefreshEventOccurred;
            RefreshEventOccurred = false;

            return ret;
        }

        readonly SortedDictionary<int, List<IKITModule>> sortedModules = new SortedDictionary<int, List<IKITModule>>();
        readonly Dictionary<ResourceName, SortedDictionary<int, List<IKITVariableSupplier>>> tmpVariableSupplierModules = new Dictionary<ResourceName, SortedDictionary<int, List<IKITVariableSupplier>>>();

        List<PartResource> _decayPartResourceList = new List<PartResource>(64);

        void IVesselResources.VesselKITModules(ref List<IKITModule> moduleList, ref Dictionary<ResourceName, List<IKITVariableSupplier>> variableSupplierModules)
        {
            // Clear the inputs

            moduleList.Clear();
            moduleList.Add(_dcSystem);

            variableSupplierModules.Clear();

            // Clear the temporary variables

            sortedModules.Clear();
            tmpVariableSupplierModules.Clear();

            foreach (var part in vessel.parts)
            {
                #region Loop over modules
                foreach (var partModule in part.Modules)
                {
                    var mod = partModule as IKITModule;
                    if (mod == null) continue;
                    // Handle the KITFixedUpdate() side of things first.

                    var working = mod.ModuleConfiguration(out var priority, out var prepend, out var hasLocalResources);
                    if (!working) continue;
                    
                    if (!sortedModules.TryGetValue(priority, out _))
                    {
                        sortedModules[priority] = new List<IKITModule>(32);
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
                            tmpVariableSupplierModules[resource] = new SortedDictionary<int, List<IKITVariableSupplier>>();
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

            foreach (List<IKITModule> list in sortedModules.Values)
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

        void GatherResources()
        {
            _resourceData.AvailableResources.Clear();
            _resourceData.MaxResources.Clear();

            var ignoreFlow = HighLogic.CurrentGame.Parameters.CustomParams<KITResourceParams>()
                .IgnoreResourceFlowRestrictions;

            foreach (var part in vessel.Parts)
            {
                foreach (var resource in part.Resources.Where( t => t.maxAmount > 0 && (t.flowState || ignoreFlow)))
                {
                    var resourceID = KITResourceSettings.NameToResource(resource.resourceName);
                    if (resourceID == ResourceName.Unknown)
                    {
                        //Debug.Log($"[KITResourceManager.GatherResources] ignoring Unknown resource {resource.resourceName}");
                        continue;
                    }

                    if (_resourceData.AvailableResources.ContainsKey(resourceID) == false)
                    {
                        _resourceData.AvailableResources[resourceID] = _resourceData.MaxResources[resourceID] = 0;
                    }

                    _resourceData.AvailableResources[resourceID] += resource.amount;
                    _resourceData.MaxResources[resourceID] += resource.maxAmount;
                }
            }
        }

        private void HandleSpecialResources()
        {
            // Per FreeThinker, "charged particle  power should technically not be able to be stored. The only reason it existed was to have some consumption
            // balancing when there are more than one consumer.", "charged particles should become Thermal heat when unconsumed and if no Thermal
            // storage available it should become waste heat."

            var hasChargedParticles = _resourceData.AvailableResources.TryGetValue(ResourceName.ChargedParticle, out var chargedParticleAmount);
            var hasThermalPower = _resourceData.AvailableResources.TryGetValue(ResourceName.ThermalPower, out var thermalPowerAmount);

            if (!_resourceData.AvailableResources.ContainsKey(ResourceName.WasteHeat))
            {
                // Seems unlikely to have Charged Particles and/or Thermal Power without WasteHeat, but just in case
                _resourceData.AvailableResources[ResourceName.WasteHeat] = 0;
            }


            if (hasChargedParticles)
            {
                _resourceData.AvailableResources[ResourceName.ChargedParticle] = 0;

                // Store any remaining charged particles in either ThermalPower or WasteHeat
                _resourceData.AvailableResources[hasThermalPower ? ResourceName.ThermalPower : ResourceName.WasteHeat] += chargedParticleAmount;
            }

            if (hasThermalPower)
            {
                // Have we exceeded the storage capacity for Thermal Power?
                if (_resourceData.MaxResources.ContainsKey(ResourceName.ThermalPower))
                {
                    var diff = thermalPowerAmount - _resourceData.MaxResources[ResourceName.ThermalPower];

                    if (diff > 0)
                    {
                        _resourceData.AvailableResources[ResourceName.WasteHeat] += diff;
                        _resourceData.AvailableResources[ResourceName.ThermalPower] = _resourceData.AvailableResources[ResourceName.ThermalPower];
                    }
                }
                else
                {
                    // no Thermal Power storage. Seems unlikely, but just in case

                    _resourceData.AvailableResources.Remove(ResourceName.ThermalPower);
                    _resourceData.AvailableResources[ResourceName.WasteHeat] += thermalPowerAmount;
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

                    if (_resourceData.AvailableResources.ContainsKey(resourceID) == false || _resourceData.MaxResources.ContainsKey(resourceID) == false) return;

                    var tmp = _resourceData.AvailableResources[resourceID] / _resourceData.MaxResources[resourceID];
                    var multiplier = (tmp > 1) ? 1 : (tmp < 0) ? 0 : tmp;
                    resource.amount = resource.maxAmount * multiplier;
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
        private static Dictionary<string, double> _decayConfiguration = new Dictionary<string, double>();

        public static void PerformResourceDecayEffect(IResourceManager resourceManager, PartResourceList partResources, Dictionary<String, DecayConfiguration> decayConfiguration)
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
