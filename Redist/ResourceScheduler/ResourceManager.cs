using KIT.Interfaces;
using KIT.Resources;
using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Smooth.Collections;
using UnityEngine;

namespace KIT.ResourceScheduler
{
    public struct ResourceProduction : IResourceProduction
    {
        internal double InternalCurrentlyRequested;
        internal double InternalCurrentlySupplied;
        internal double InternalPreviouslyRequested;
        internal double InternalPreviouslySupplied;

        public double CurrentlyRequested() => InternalCurrentlyRequested;
        public double CurrentSupplied() => InternalCurrentlySupplied;

        public double PreviousUnmetDemand() => Math.Max(0, InternalPreviouslyRequested - InternalPreviouslySupplied);
        public bool PreviousDemandMet() => InternalPreviouslySupplied >= InternalPreviouslyRequested;

        public double PreviouslyRequested() => InternalPreviouslyRequested;
        public double PreviouslySupplied() => InternalPreviouslySupplied;

        public double PreviousSurplus() => Math.Max(0, InternalPreviouslySupplied - InternalPreviouslyRequested);

        public bool PreviousDataSupplied() => InternalPreviouslySupplied != 0 && InternalPreviouslyRequested != 0;
    }

    public struct PerPartResourceInformation
    {
        public double Amount, MaxAmount;
    }

    public struct ResourceManagerData : IResourceManager
    {
        internal ResourceManager ResourceManager;

        private readonly ICheatOptions _myCheatOptions;
        internal double FixedDeltaTimeValue;

        internal List<IKITModule> FixedUpdateCalledMods;
        internal List<IKITModule> ModsCurrentlyRunning;

        internal Dictionary<ResourceName, double> AvailableResources;
        internal Dictionary<ResourceName, double> MaxResources;

        public ResourceManagerData(ResourceManager resourceManager, ICheatOptions cheatOptions)
        {
            ResourceManager = resourceManager;
            _myCheatOptions = cheatOptions;

            FixedUpdateCalledMods = new List<IKITModule>(128);
            ModsCurrentlyRunning = new List<IKITModule>(32);

            AvailableResources = new Dictionary<ResourceName, double>();
            MaxResources = new Dictionary<ResourceName, double>();

            FixedDeltaTimeValue = 0;
        }

        public void Update(double fixedDeltaTime, Dictionary<ResourceName, double> availableResources, Dictionary<ResourceName, double> maxResources, List<IKITModule> modsCurrentlyRunning, List<IKITModule> fixedUpdateCalledMods)
        {
            FixedDeltaTimeValue = fixedDeltaTime;

            AvailableResources.Clear();
            AvailableResources.AddAll(availableResources);

            MaxResources.Clear();
            MaxResources.AddAll(maxResources);

            ModsCurrentlyRunning.Clear();
            ModsCurrentlyRunning.AddRange(modsCurrentlyRunning);

            FixedUpdateCalledMods.Clear();
            FixedUpdateCalledMods.AddRange(fixedUpdateCalledMods);
        }

        public double Consume(ResourceName resource, double wanted)
        {
            return ResourceManager.Consume(this, resource, wanted);
        }

        public double Produce(ResourceName resource, double amount, double max = -1)
        {
            return ResourceManager.Produce(this, resource, amount, max);
        }

        public bool CapacityInformation(
            ResourceName resourceIdentifier, out double maxCapacity,
            out double spareCapacity, out double currentCapacity, out double fillFraction)
        {

            var ret = AvailableResources.TryGetValue(resourceIdentifier, out currentCapacity);
            if (ret == false)
            {
                maxCapacity = spareCapacity = fillFraction = 0;
                return false;
            }

            MaxResources.TryGetValue(resourceIdentifier, out maxCapacity);

            if (resourceIdentifier == ResourceName.WasteHeat && _myCheatOptions.IgnoreMaxTemperature)
            {
                fillFraction = currentCapacity = 0;
                spareCapacity = maxCapacity;
                return true;
            }

            if (maxCapacity > currentCapacity)
            {
                spareCapacity = maxCapacity - currentCapacity;
            }
            else
            {
                spareCapacity = 0;
            }


            if (maxCapacity > 0)
            {
                fillFraction = currentCapacity / maxCapacity;
            }
            else
            {
                fillFraction = 1;
            }

            return true;
        }


        public double ScaledConsumptionProduction(List<KeyValuePair<ResourceName, double>> consumeResources, List<KeyValuePair<ResourceName, double>> produceResources, double minimumRatio = 0,
            ConsumptionProductionFlags flags = ConsumptionProductionFlags.Empty)
        {
            return ResourceManager.ScaledConsumptionProduction(this, consumeResources, produceResources, minimumRatio,
                flags);
        }

        public double CurrentCapacity(ResourceName resourceIdentifier)
        {
            CapacityInformation(resourceIdentifier, out _, out _, out var currentCapacity, out _);
            return currentCapacity;
        }

        public double FillFraction(ResourceName resourceIdentifier)
        {
            CapacityInformation(resourceIdentifier, out _, out _, out _, out var fillFraction);
            return fillFraction;
        }

        public double SpareCapacity(ResourceName resourceIdentifier)
        {
            CapacityInformation(resourceIdentifier, out _, out var spareCapacity, out _, out _);
            return spareCapacity;
        }

        public double MaxCapacity(ResourceName resourceIdentifier)
        {
            CapacityInformation(resourceIdentifier, out var maxCapacity, out _, out _, out _);
            return maxCapacity;
        }

        public ICheatOptions CheatOptions() => _myCheatOptions;
        public double FixedDeltaTime() => FixedDeltaTimeValue;

        public IResourceProduction ProductionStats(ResourceName resourceIdentifier)
        {
            throw new NotImplementedException();
        }
    }

    public class ResourceManager : IResourceScheduler
    {
        private readonly IVesselResources _vesselResources;
        private static double fudgeFactor = 0.99999;

        private double _fixedDeltaTime;

        private readonly HashSet<IKITModule> _fixedUpdateCalledMods = new HashSet<IKITModule>(128);
        private readonly List<IKITModule> _modsCurrentlyRunning = new List<IKITModule>(128);

        public bool UseThisToHelpWithTesting;

        public Dictionary<ResourceName, Dictionary<IKITModule, PerPartResourceInformation>> ModConsumption;
        public Dictionary<ResourceName, Dictionary<IKITModule, PerPartResourceInformation>> ModProduction;

        private readonly OverHeatingResourceManager _overHeatingResourceManager;
        private readonly WasteHeatEquilibriumResourceManager _wasteHeatEquilibriumResourceManager;
        private IResourceManager _topLevelInterface;

        public ResourceManager(IVesselResources vesselResources)
        {
            _vesselResources = vesselResources;

            _resourceProductionStats = new ResourceProduction[ResourceName.WasteHeat - ResourceName.ElectricCharge + 1];

            ModProduction = new Dictionary<ResourceName, Dictionary<IKITModule, PerPartResourceInformation>>();
            ModConsumption = new Dictionary<ResourceName, Dictionary<IKITModule, PerPartResourceInformation>>();
            for (var i = ResourceName.ElectricCharge; i <= ResourceName.WasteHeat; i++)
            {
                ModProduction[i] = new Dictionary<IKITModule, PerPartResourceInformation>();
                ModConsumption[i] = new Dictionary<IKITModule, PerPartResourceInformation>();
            }

            _wasteHeatEquilibriumResourceManager = new WasteHeatEquilibriumResourceManager();
            _overHeatingResourceManager = new OverHeatingResourceManager();
        }

        #region IResourceManager implementation

        public double ScaledConsumptionProduction(ResourceManagerData data, List<KeyValuePair<ResourceName, double>> consumeResources, List<KeyValuePair<ResourceName, double>> produceResources, double minimumRatio = 0,
            ConsumptionProductionFlags flags = ConsumptionProductionFlags.Empty)
        {
            throw new NotImplementedException();
        }

        private bool _inExecuteKITModules;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TrackableResource(ResourceName resource) => resource >= ResourceName.ElectricCharge && resource <= ResourceName.WasteHeat;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool UseOverheatingResourceManager(ResourceManagerData data)
        {
            // If players don't want the requests scaled back, so be it
            if (HighLogic.CurrentGame.Parameters.CustomParams<KITResourceParams>().DisableResourceConsumptionRateLimit) return false;

            var found = data.CapacityInformation(ResourceName.WasteHeat, out _, out _, out _, out var wasteHeatRatio);
            
            var ratio = wasteHeatRatio;
            if (ratio < 0.90) return false;

            var reduction = (ratio - 0.90) * 10;

            Debug.Log($"[UseOverheatingResourceManager] reducing consumption of resources by {Math.Round(reduction * 100, 2)}%");

            _overHeatingResourceManager.ConsumptionReduction = reduction;
            return true;

        }

        /// <summary>
        /// Called by the IKITModule to consume resources present on a vessel. It automatically converts the wanted amount by the appropriate value to
        /// give you a per-second resource consumption.
        /// </summary>
        /// <param name="resource">Resource to consume</param>
        /// <param name="wanted">How much you want</param>
        /// <returns>How much you got</returns>
        public double Consume(ResourceManagerData data, ResourceName resource, double wanted)
        {
            KITResourceSettings.ValidateResource(resource);

            PerPartResourceInformation tmpPPRI;
            if (!_inExecuteKITModules || wanted < 0)
            {
                Debug.Log(
                    $"[KITResourceManager.Consume] don't{(_inExecuteKITModules ? " use outside of IKITModules" : "")} {(wanted < 0 ? " consume negative amounts" : "")}");
                return 0;
            }

            tmpPPRI.Amount = tmpPPRI.MaxAmount = 0;

            var lastMod = _modsCurrentlyRunning.Last();

            var trackResourceUsage = TrackableResource(resource);
            if (trackResourceUsage)
            {
                _resourceProductionStats[resource - ResourceName.ElectricCharge].InternalCurrentlyRequested += wanted;

                if (!ModConsumption[resource].ContainsKey(lastMod))
                {
                    ModConsumption[resource][lastMod] = new PerPartResourceInformation();
                }
                tmpPPRI = ModConsumption[resource][lastMod];
                tmpPPRI.MaxAmount += wanted;
            }

            if (data.CheatOptions().InfiniteElectricity && resource == ResourceName.ElectricCharge)
            {
                tmpPPRI.Amount += wanted;
                ModConsumption[resource][lastMod] = tmpPPRI;
                return wanted;
            }

            if (!data.AvailableResources.ContainsKey(resource))
            {
                data.AvailableResources[resource] = 0;
            }

            double obtainedAmount = 0;
            double modifiedAmount = wanted * _fixedDeltaTime;

            var tmp = Math.Min(data.AvailableResources[resource], modifiedAmount);
            obtainedAmount += tmp;
            data.AvailableResources[resource] -= tmp;

            if (trackResourceUsage && tmp > 0)
                _resourceProductionStats[resource - ResourceName.ElectricCharge].InternalCurrentlySupplied += tmp;

            if (obtainedAmount >= modifiedAmount)
            {
                tmpPPRI.Amount += wanted;
                if (trackResourceUsage) ModConsumption[resource][lastMod] = tmpPPRI;

                return wanted;
            }

            double surplusWanted = 0;
            if (resource != ResourceName.ChargedParticle) data.MaxResources.TryGetValue(resource, out surplusWanted);

            // Convert to seconds
            obtainedAmount = wanted * (obtainedAmount / modifiedAmount);
            // Debug.Log($"[Consume] calling variable suppliers for {KITResourceSettings.ResourceToName(resource)}, already have {obtainedAmount}, want a total of {wanted}, with a surplus of {surplusWanted}");
            obtainedAmount = CallVariableSuppliers(data, resource, obtainedAmount, wanted, surplusWanted);

            // We do not need to account for InternalCurrentlySupplied here, as the modules called above will call
            // Produce which credits the InternalCurrentlySupplied field here.

            // is it close enough to being fully requested? (accounting for precision issues)
            var result = (obtainedAmount < (wanted * fudgeFactor)) ? wanted * (obtainedAmount / wanted) : wanted;

            // Debug.Log($"[Consume] after calling variable suppliers, obtainedAmount is {obtainedAmount}, fudged value is {wanted * fudgeFactor}, and result is {result}");

            tmpPPRI.Amount += result;
            if (trackResourceUsage) ModConsumption[resource][lastMod] = tmpPPRI;

            return result;
        }

        public double FixedDeltaTime() => _fixedDeltaTime;

        void RefreshActiveModules()
        {
            _vesselResources.VesselKITModules(ref _activeKITModules, ref _variableSupplierModules);
        }

        /// <summary>
        /// Called by the IKITModule to produce resources on a vessel.It automatically converts the amount by the appropriate value to
        /// give a per-second resource production.
        /// </summary>
        /// <param name="resource">Resource to produce</param>
        /// <param name="amount">Amount you are providing</param>
        /// <param name="max">Maximum amount that could be provided</param>
        public double Produce(ResourceManagerData data, ResourceName resource, double amount, double max = -1)
        {
            KITResourceSettings.ValidateResource(resource);

            if (!_inExecuteKITModules)
            {
                Debug.Log(
                    $"[KITResourceManager.Produce] don't{(_inExecuteKITModules ? " use outside of IKITModules" : "")} {(amount < 0 ? " produce negative amounts" : "")}");
                return 0;
            }

            // Debug.Log($"[Produce] called with resource = {KITResourceSettings.ResourceToName(resource)}, amount = {amount} and max as {max}");

            if (TrackableResource(resource))
            {
                _resourceProductionStats[resource - ResourceName.ElectricCharge].InternalCurrentlySupplied += amount;

                var lastMod = _modsCurrentlyRunning.Last();

                if (!ModProduction[resource].ContainsKey(lastMod))
                {
                    var tmpPPRI = new PerPartResourceInformation { Amount = amount, MaxAmount = (max == -1) ? 0 : max };
                    ModProduction[resource][lastMod] = tmpPPRI;
                }
                else
                {
                    var tmpPPRI = ModProduction[resource][lastMod];
                    tmpPPRI.Amount += amount;
                    tmpPPRI.MaxAmount += (max == -1) ? 0 : max;
                    ModProduction[resource][lastMod] = tmpPPRI;
                }
            }


            if (resource == ResourceName.WasteHeat && data.CheatOptions().IgnoreMaxTemperature) return 0;

            if (!data.AvailableResources.ContainsKey(resource))
            {
                data.AvailableResources[resource] = 0;
            }
            data.AvailableResources[resource] += amount * _fixedDeltaTime;

            return amount;
        }

        #endregion
        #region IResourceScheduler implementation

        private List<IKITModule> _activeKITModules = new List<IKITModule>(128);
        private Dictionary<ResourceName, List<IKITVariableSupplier>> _variableSupplierModules = new Dictionary<ResourceName, List<IKITVariableSupplier>>();

        private bool _complainedToWaiterAboutOrder;

        public ulong KITSteps;

        private int _overHeatingCounter;

        private bool _wasteHeatEquilibriumAchieved;

        private double _wasteHeatAtEndOfProcessing;
        private double _wasteHeatAtBeginningOfProcessing;

        // TODO - how constant are these across game versions / situations / mods / etc? Can we calc at run time / find out?
        private const int TimeWarp100X = 4;
        private const double DeltaTimeAt100X = 2;

        bool _informWhenHighTimeWarpPossible;
        private const double EquilibriumDifference = 0.00001;

        private void PickTopLevelInterface(ResourceManagerData resourceData)
        {
            _topLevelInterface = _wasteHeatEquilibriumAchieved ? _wasteHeatEquilibriumResourceManager : (IResourceManager)resourceData;

            if (UseOverheatingResourceManager(resourceData))
                _topLevelInterface = _overHeatingResourceManager.SetBaseResourceManager(_topLevelInterface);
        }

        private void CheckForWasteHeatEquilibrium()
        {
            double larger, smaller;

            if (_wasteHeatAtEndOfProcessing > _wasteHeatAtBeginningOfProcessing)
            {
                larger = _wasteHeatAtEndOfProcessing;
                smaller = _wasteHeatAtBeginningOfProcessing;
            }
            else
            {
                larger = _wasteHeatAtBeginningOfProcessing;
                smaller = _wasteHeatAtEndOfProcessing;
            }

            if (smaller == 0 || larger == 0)
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                _wasteHeatEquilibriumAchieved = smaller == larger;
                return;
            }

            var percentDifference = (larger - smaller) / larger;

            _wasteHeatEquilibriumAchieved = percentDifference <= EquilibriumDifference;
            if (_wasteHeatEquilibriumAchieved)
            {
                Debug.Log($"[CheckForWasteHeatEquilibrium] WasteHeatEquilibriumAchieved - percentDifference is {percentDifference}, larger is {larger}, and smaller is {smaller}");
            }
        }

        /// <summary>
        /// ExecuteKITModules() does the heavy work of executing all the IKITModule FixedUpdate() equiv. It needs to be careful to ensure
        /// it is using the most recent list of modules, hence the odd looping code. In the case of no part updates are needed, it's
        /// relatively optimal.
        /// </summary>
        /// <param name="deltaTime">the amount of delta time that each module should use</param>
        /// <param name="resourceData">What resources are available for this call.</param>
        public void ExecuteKITModules(double deltaTime, ResourceManagerData resourceData)
        {
            var index = 0;

            KITSteps++;

            if (deltaTime < DeltaTimeAt100X)
            {
                // is writing to memory cheaper than reading to it? (page faults, etc)
                _wasteHeatEquilibriumAchieved = false;
                _informWhenHighTimeWarpPossible = false;
            }
            else if (deltaTime == DeltaTimeAt100X && !_wasteHeatEquilibriumAchieved)
            {
                Debug.Log($"[ExecuteKITModules] CurrentRateIndex is {TimeWarp.CurrentRateIndex} and CurrentRate is {TimeWarp.CurrentRate}");
                CheckForWasteHeatEquilibrium();

                if (_informWhenHighTimeWarpPossible)
                {
                    ScreenMessages.PostScreenMessage(
                        Localizer.Format("#LOC_KIT_WasteHeatAtEquilibrium"),
                        5.0f, ScreenMessageStyle.UPPER_CENTER
                    );
                }
            }
            else if (deltaTime > DeltaTimeAt100X && !_wasteHeatEquilibriumAchieved)
            {
                ScreenMessages.PostScreenMessage(
                    Localizer.Format("#LOC_KIT_WasteHeatNotAtEquilibrium"),
                    5.0f, ScreenMessageStyle.UPPER_CENTER
                );

                TimeWarp.SetRate(TimeWarp100X, true);

                _informWhenHighTimeWarpPossible = true;
            }

            _wasteHeatAtBeginningOfProcessing = _wasteHeatAtEndOfProcessing;

            // Cycle the resource tracking data over.
            for (var i = 0; i < (ResourceName.WasteHeat - ResourceName.ElectricCharge) + 1; i++)
            {
                if (i == ResourceName.WasteHeat - ResourceName.ElectricCharge && _wasteHeatEquilibriumAchieved) continue;

                var currentResourceProduction = _resourceProductionStats[i];

                currentResourceProduction.InternalPreviouslyRequested = currentResourceProduction.InternalCurrentlyRequested;
                currentResourceProduction.InternalPreviouslySupplied = currentResourceProduction.InternalCurrentlySupplied;

                currentResourceProduction.InternalCurrentlyRequested = currentResourceProduction.InternalCurrentlySupplied = 0;
            }

            for (var i = ResourceName.ElectricCharge; i <= ResourceName.WasteHeat; i++)
            {
                if (i == ResourceName.WasteHeat && _wasteHeatEquilibriumAchieved) continue;

                ModConsumption[i].Clear();
                ModProduction[i].Clear();
            }

            tappedOutMods.Clear();
            _fixedUpdateCalledMods.Clear();

            PickTopLevelInterface(resourceData);

            if (_modsCurrentlyRunning.Count > 0)
            {
                if (!_complainedToWaiterAboutOrder)
                    Debug.Log("[ResourceManager.ExecuteKITModules] URGENT modsCurrentlyRunning.Count != 0. there may be resource production / consumption issues.");
                else
                    _complainedToWaiterAboutOrder = true;

                _modsCurrentlyRunning.Clear();
            }

            if (_vesselResources.VesselModified())
            {
                RefreshActiveModules();
                if (_activeKITModules.Count == 0)
                {
                    Debug.Log("No KIT Modules found");
                    return;
                }
                Debug.Log($"[resource manager] got {_activeKITModules.Count} modules and also {_variableSupplierModules.Count} variable modules");
            }

            if (_activeKITModules.Count >= 1 && _activeKITModules[0] is IDCElectricalSystem dc)
            {
                ModConsumption[ResourceName.ElectricCharge][_activeKITModules[0]] = new PerPartResourceInformation { Amount = dc.UnallocatedElectricChargeConsumption() };
                _activeKITModules.Remove(_activeKITModules[0]);
            }

            _inExecuteKITModules = true;

            _fixedDeltaTime = deltaTime;

            while (index != _activeKITModules.Count)
            {
                var mod = _activeKITModules[index];
                index++;

                if (_modsCurrentlyRunning.Contains(mod))
                {
                    Debug.Log($"[KITResourceManager.ExecuteKITModules] This module {mod.KITPartName()} should not be marked busy at this stage");
                    continue;
                }
                _modsCurrentlyRunning.Add(mod);

                InitializeModuleIfNeeded(mod);

                if (_vesselResources.VesselModified())
                {
                    index = 0;
                    RefreshActiveModules();
                }

                if (_modsCurrentlyRunning.Last() != mod)
                {
                    // there is an ordering problem in the above mod.KITFixedUpdate(), and we did not correctly track which module is
                    // currently running.
                    throw new InvalidOperationException("[KITResourceManager.ExecuteKITModules] the currently running mod is not the last running mod");
                }
                _modsCurrentlyRunning.Remove(mod);
            }

            RechargeBatteries(resourceData);
            _vesselResources.OnKITProcessingFinished(resourceData);

            /* if (!_wasteHeatEquilibriumAchieved)
            {
                resourceAmounts.TryGetValue(ResourceName.WasteHeat, out _wasteHeatAtEndOfProcessing);
            }
            else
            {
                resourceAmounts[ResourceName.WasteHeat] = _wasteHeatAtEndOfProcessing;
            }
            */

            CheckIfEmergencyStopIsNeeded(resourceData);

            _inExecuteKITModules = false;
        }

        private void CheckIfEmergencyStopIsNeeded(ResourceManagerData resourceData)
        {
            var found = resourceData.CapacityInformation(ResourceName.WasteHeat,
                maxCapacity: out var maxCapacity,
                spareCapacity: out _,
                currentCapacity: out var currentCapacity,
                fillFraction: out _);

            if (!found) return;

            var ratio = currentCapacity / maxCapacity;
            if (ratio < HighLogic.CurrentGame.Parameters.CustomParams<KITResourceParams>()
                .EmergencyShutdownTemperaturePercentage)
            {
                _overHeatingCounter = 0;
                return;
            }

            _overHeatingCounter++;

            ScreenMessages.PostScreenMessage(
                Localizer.Format("#LOC_KIT_EmergencyShutdownWasteHeat"),
                5.0f, ScreenMessageStyle.UPPER_CENTER
            );

            TimeWarp.SetRate(0, true);

            /*
            for (int i = 0; i < activeKITModules.Count; i++)
            {
                try
                {
                    // activeKITModules[i].EmergencyStop(overHeatingCounter);
                }
                catch (Exception e)
                {
                    Debug.Log($"[CheckIfEmergencyStopIsNeeded] {activeKITModules[i].KITPartName()} threw an exception: {e.Message}");
                }
            }
            */
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RechargeBatteries(ResourceManagerData resourceData)
        {
            var found = resourceData.CapacityInformation(
                resourceIdentifier: ResourceName.ElectricCharge,
                maxCapacity: out var maxCapacity,
                spareCapacity: out _,
                currentCapacity: out var currentCapacity,
                fillFraction: out _);

            if (!found) return;

            // Check to see if the variable suppliers can be used to fill any missing EC from the vessel. This will charge
            // any batteries present on the ship.
            double fillBattery = maxCapacity - currentCapacity;
            if (fillBattery <= 0) return;

            resourceData.AvailableResources[ResourceName.ElectricCharge] += CallVariableSuppliers(resourceData, ResourceName.ElectricCharge, 0, fillBattery);
        }

        readonly HashSet<IKITVariableSupplier> tappedOutMods = new HashSet<IKITVariableSupplier>(128);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitializeModuleIfNeeded(IKITModule ikitModule)
        {
            if (!_fixedUpdateCalledMods.Contains(ikitModule))
            {
                // Hasn't had it's KITFixedUpdate() yet? call that first.
                _fixedUpdateCalledMods.Add(ikitModule);
                UnityEngine.Profiling.Profiler.BeginSample($"ResourceManager.InitializeModuleIfNeeded.Init.{ikitModule.KITPartName()}");

                try
                {
                    ikitModule.KITFixedUpdate(_topLevelInterface);
                }
                catch (Exception ex)
                {
                    Debug.Log($"[KITResourceManager.InitializeModuleIfNeeded] Exception when processing [{ikitModule.KITPartName()}, {(ikitModule as PartModule)?.ClassName}]: {ex}");
                }
                finally
                {
                    UnityEngine.Profiling.Profiler.EndSample();
                }
            }
        }

        private double CallVariableSuppliers(ResourceManagerData resourceData, ResourceName resource, double obtainedAmount, double originalAmount, double resourceFiller = 0)
        {
            if (!_variableSupplierModules.ContainsKey(resource)) return 0;

            var reducedObtainedAmount = obtainedAmount * _fixedDeltaTime;
            var reducedOriginalAmount = originalAmount * _fixedDeltaTime;

            IResourceManager secondaryInterface = _wasteHeatEquilibriumAchieved ? _wasteHeatEquilibriumResourceManager : (IResourceManager)this;

            foreach (var mod in _variableSupplierModules[resource])
            {
                var kitMod = mod as IKITModule;
                if (kitMod == null) continue;

                if (tappedOutMods.Contains(mod)) continue; // it's tapped out for this cycle.
                if (_modsCurrentlyRunning.Contains(kitMod)) continue;

                _modsCurrentlyRunning.Add(kitMod);

                InitializeModuleIfNeeded(kitMod);

                double perSecondAmount = originalAmount * (1 - (reducedObtainedAmount / reducedOriginalAmount));

                try
                {
                    UnityEngine.Profiling.Profiler.BeginSample($"ResourceManager.CallVariableSuppliers.ProvideResource.{kitMod.KITPartName()}");

                    var canContinue = mod.ProvideResource(secondaryInterface, resource, perSecondAmount + resourceFiller);
                    if (!canContinue) tappedOutMods.Add(mod);
                }
                catch (Exception ex)
                {
                    if (UseThisToHelpWithTesting) throw;
                    Debug.Log($"[KITResourceManager.callVariableSuppliers] calling ikitModule {kitMod.KITPartName()} resulted in {ex}");
                }
                finally
                {
                    UnityEngine.Profiling.Profiler.EndSample();
                }

                var tmp = Math.Min(resourceData.AvailableResources[resource], reducedOriginalAmount - reducedObtainedAmount);
                resourceData.AvailableResources[resource] -= tmp;
                reducedObtainedAmount += tmp;

                // Debug.Log($"[CallVariableSuppliers] _currentResources[resource] is {_currentResources[resource]}, reducedOriginalAmount - reducedObtainedAmount is {reducedOriginalAmount - reducedObtainedAmount} and tmp is {tmp}, reducedObtainedAmount is now {reducedObtainedAmount}");

                _modsCurrentlyRunning.Remove(kitMod);

                if (reducedObtainedAmount >= reducedOriginalAmount) return originalAmount;
            }

            return originalAmount * (reducedObtainedAmount / reducedOriginalAmount);
        }

        private readonly ResourceProduction[] _resourceProductionStats;

        public IResourceProduction ProductionStats(ResourceName resourceIdentifier)
        {
            if (resourceIdentifier >= ResourceName.ElectricCharge && resourceIdentifier <= ResourceName.WasteHeat)
                return _resourceProductionStats[resourceIdentifier - ResourceName.ElectricCharge];

            return null;
        }
        #endregion

    }
}
