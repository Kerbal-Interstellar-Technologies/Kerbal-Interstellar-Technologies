using KIT.Interfaces;
using KIT.Resources;
using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Smooth.Collections;
using Smooth.Delegates;
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

    public struct ResourceData : IResourceManager
    {
        internal KITResourceVesselModule ResourceManager;

        private readonly ICheatOptions _myCheatOptions;
        internal double FixedDeltaTimeValue;

        internal List<IKITModule> AvailableKITModules;
        internal SortedDictionary<ResourceName, List<IKITVariableSupplier>> VariableSupplierModules;

        internal HashSet<IKITVariableSupplier> TappedOutMods;

        internal List<IKITModule> FixedUpdateCalledMods;
        internal List<IKITModule> ModsCurrentlyRunning;

        internal Dictionary<ResourceName, double> AvailableResources;
        internal Dictionary<ResourceName, double> MaxResources;

        internal ResourceProduction[] ResourceProductionStats;
        internal Dictionary<ResourceName, Dictionary<IKITModule, PerPartResourceInformation>> ModConsumption;
        internal Dictionary<ResourceName, Dictionary<IKITModule, PerPartResourceInformation>> ModProduction;

        public ResourceData(KITResourceVesselModule resourceManager, ICheatOptions cheatOptions)
        {
            ResourceManager = resourceManager;
            _myCheatOptions = cheatOptions;

            AvailableKITModules = new List<IKITModule>(128);
            VariableSupplierModules = new SortedDictionary<ResourceName, List<IKITVariableSupplier>>();

            TappedOutMods = new HashSet<IKITVariableSupplier>(128);

            FixedUpdateCalledMods = new List<IKITModule>(128);
            ModsCurrentlyRunning = new List<IKITModule>(32);

            AvailableResources = new Dictionary<ResourceName, double>();
            MaxResources = new Dictionary<ResourceName, double>();

            FixedDeltaTimeValue = 0;

            ResourceProductionStats = new ResourceProduction[(int)(ResourceName.WasteHeat + 1)];

            ModProduction = new Dictionary<ResourceName, Dictionary<IKITModule, PerPartResourceInformation>>();
            ModConsumption = new Dictionary<ResourceName, Dictionary<IKITModule, PerPartResourceInformation>>();
            for (var i = ResourceName.ElectricCharge; i <= ResourceName.WasteHeat; i++)
            {
                ModProduction[i] = new Dictionary<IKITModule, PerPartResourceInformation>();
                ModConsumption[i] = new Dictionary<IKITModule, PerPartResourceInformation>();
            }
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

        private double CalculateAvailableSpaceCapacity(ResourceKeyValue resource)
        {
            var spareCapacity = SpareCapacity(resource.Resource);
            if (spareCapacity == 0) return 0;

            var value = spareCapacity / resource.Amount;

            return (value <= double.Epsilon) ? 0 : (value >= 1) ? 1 : value;
        }

        private double CalculateAvailableResourceCapacity(ResourceKeyValue resource)
        {
            var availableCapacity = CurrentCapacity(resource.Resource);
            if (availableCapacity == 0) return 0;

            var value = availableCapacity / resource.Amount;

            return (value <= double.Epsilon) ? 0 : (value >= 1) ? 1 : value;
        }

        public double ScaledConsumptionProduction(List<ResourceKeyValue> consumeResources, List<ResourceKeyValue> produceResources, double minimumRatio = 0,
            ConsumptionProductionFlags flags = ConsumptionProductionFlags.Empty)
        {
            double ratio = 1;

            if ((flags & ConsumptionProductionFlags.DoNotOverFill) != 0)
            {
                foreach (var resourceInfo in produceResources)
                {
                    var capacityRatio = CalculateAvailableSpaceCapacity(resourceInfo);
                    ratio = (capacityRatio < ratio) ? capacityRatio : ratio;
                }

                if (ratio == 0) return 0;
            }

            foreach (var resourceInfo in consumeResources)
            {
                // We may get resources on demand, so skip accounting for those at the moment
                if (VariableSupplierModules.ContainsKey(resourceInfo.Resource)) continue;
                
                var capacityRatio = CalculateAvailableResourceCapacity(resourceInfo);
                ratio = (capacityRatio < ratio) ? capacityRatio : ratio;

                if (ratio == 0) return 0;
            }

            consumeResources.ForEach(kv => kv.Amount *= ratio);
            produceResources.ForEach(kv => kv.Amount *= ratio);
            return ResourceManager.ScaledConsumptionProduction(this, consumeResources, produceResources, minimumRatio, flags);
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
            if (ResourceProductionStats == null) return null;

            if (resourceIdentifier >= ResourceName.ElectricCharge && resourceIdentifier <= ResourceName.WasteHeat)
                return ResourceProductionStats[(int) resourceIdentifier];

            return null;
        }
    }

    public partial class KITResourceVesselModule
    {
        private static double fudgeFactor = 0.99999;

        private double _fixedDeltaTime;
        public bool UseThisToHelpWithTesting;

        private readonly OverHeatingResourceManager _overHeatingResourceManager;
        private readonly WasteHeatEquilibriumResourceManager _wasteHeatEquilibriumResourceManager;
        private IResourceManager _topLevelInterface;

        public KITResourceVesselModule()
        {

            _wasteHeatEquilibriumResourceManager = new WasteHeatEquilibriumResourceManager();
            _overHeatingResourceManager = new OverHeatingResourceManager();
        }

        #region IResourceManager implementation

        public double ScaledConsumptionProduction(ResourceData data, List<ResourceKeyValue> consumeResources, List<ResourceKeyValue> produceResources, double minimumRatio = 0,
            ConsumptionProductionFlags flags = ConsumptionProductionFlags.Empty)
        {
            throw new NotImplementedException();
        }

        private bool _inExecuteKITModules;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TrackableResource(ResourceName resource) => resource >= ResourceName.ElectricCharge && resource <= ResourceName.WasteHeat;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool UseOverheatingResourceManager(ResourceData data)
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
        /// <param name="data">Resource Data to consume against</param>
        /// <param name="resource">Resource to consume</param>
        /// <param name="wanted">How much you want</param>
        /// <returns>How much you got</returns>
        public double Consume(ResourceData data, ResourceName resource, double wanted)
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

            var lastMod = data.ModsCurrentlyRunning.Last();

            var trackResourceUsage = TrackableResource(resource) && data.ModConsumption != null && data.ModProduction != null && data.ResourceProductionStats != null;
            if (trackResourceUsage)
            {
                data.ResourceProductionStats[(int)ResourceName.ElectricCharge].InternalCurrentlyRequested += wanted;

                if (!data.ModConsumption[resource].ContainsKey(lastMod))
                {
                    data.ModConsumption[resource][lastMod] = new PerPartResourceInformation();
                }
                tmpPPRI = data.ModConsumption[resource][lastMod];
                tmpPPRI.MaxAmount += wanted;
            }

            if (data.CheatOptions().InfiniteElectricity && resource == ResourceName.ElectricCharge)
            {
                tmpPPRI.Amount += wanted;
                data.ModConsumption[resource][lastMod] = tmpPPRI;
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
                data.ResourceProductionStats[(int)resource].InternalCurrentlySupplied += tmp;

            if (obtainedAmount >= modifiedAmount)
            {
                tmpPPRI.Amount += wanted;
                if (trackResourceUsage) data.ModConsumption[resource][lastMod] = tmpPPRI;

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
            if (trackResourceUsage) data.ModConsumption[resource][lastMod] = tmpPPRI;

            return result;
        }

        /// <summary>
        /// Called by the IKITModule to produce resources on a vessel.It automatically converts the amount by the appropriate value to
        /// give a per-second resource production.
        /// </summary>
        /// <param name="resource">Resource to produce</param>
        /// <param name="amount">Amount you are providing</param>
        /// <param name="max">Maximum amount that could be provided</param>
        public double Produce(ResourceData data, ResourceName resource, double amount, double max = -1)
        {
            KITResourceSettings.ValidateResource(resource);

            if (!_inExecuteKITModules)
            {
                Debug.Log(
                    $"[KITResourceManager.Produce] don't{(_inExecuteKITModules ? " use outside of IKITModules" : "")} {(amount < 0 ? " produce negative amounts" : "")}");
                return 0;
            }

            // Debug.Log($"[Produce] called with resource = {KITResourceSettings.ResourceToName(resource)}, amount = {amount} and max as {max}");

            var trackResourceUsage = TrackableResource(resource) && data.ModConsumption != null && data.ModProduction != null && data.ResourceProductionStats != null;
            if (trackResourceUsage)
            {
                data.ResourceProductionStats[(int)resource].InternalCurrentlySupplied += amount;

                var lastMod = data.ModsCurrentlyRunning.Last();

                if (!data.ModProduction[resource].ContainsKey(lastMod))
                {
                    var tmpPPRI = new PerPartResourceInformation { Amount = amount, MaxAmount = (max == -1) ? 0 : max };
                    data.ModProduction[resource][lastMod] = tmpPPRI;
                }
                else
                {
                    var tmpPPRI = data.ModProduction[resource][lastMod];
                    tmpPPRI.Amount += amount;
                    tmpPPRI.MaxAmount += (max == -1) ? 0 : max;
                    data.ModProduction[resource][lastMod] = tmpPPRI;
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

        private void PickTopLevelInterface(ResourceData resourceData)
        {
            _topLevelInterface = _wasteHeatEquilibriumAchieved ? _wasteHeatEquilibriumResourceManager.Update(resourceData) : (IResourceManager)resourceData;

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
        public void ExecuteKITModules(double deltaTime, ResourceData resourceData)
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

                var currentResourceProduction = resourceData.ResourceProductionStats[i];

                currentResourceProduction.InternalPreviouslyRequested = currentResourceProduction.InternalCurrentlyRequested;
                currentResourceProduction.InternalPreviouslySupplied = currentResourceProduction.InternalCurrentlySupplied;

                currentResourceProduction.InternalCurrentlyRequested = currentResourceProduction.InternalCurrentlySupplied = 0;
            }

            for (var i = ResourceName.ElectricCharge; i <= ResourceName.WasteHeat; i++)
            {
                if (i == ResourceName.WasteHeat && _wasteHeatEquilibriumAchieved) continue;

                resourceData.ModConsumption[i].Clear();
                resourceData.ModProduction[i].Clear();
            }

            resourceData.TappedOutMods.Clear();
            resourceData.FixedUpdateCalledMods.Clear();

            PickTopLevelInterface(resourceData);

            if (resourceData.ModsCurrentlyRunning.Count > 0)
            {
                if (!_complainedToWaiterAboutOrder)
                    Debug.Log("[ResourceManager.ExecuteKITModules] URGENT modsCurrentlyRunning.Count != 0. there may be resource production / consumption issues.");
                else
                    _complainedToWaiterAboutOrder = true;

                resourceData.ModsCurrentlyRunning.Clear();
            }

            if (VesselModified())
            {
                RefreshActiveModules(resourceData);
                if (resourceData.AvailableKITModules.Count == 0)
                {
                    Debug.Log("No KIT Modules found");
                    return;
                }
                Debug.Log($"[resource manager] got {resourceData.AvailableKITModules.Count} modules and also {resourceData.VariableSupplierModules.Count} variable modules");
            }

            if (resourceData.AvailableKITModules.Count >= 1 && resourceData.AvailableKITModules[0] is IDCElectricalSystem dc)
            {
                resourceData.ModConsumption[ResourceName.ElectricCharge][resourceData.AvailableKITModules[0]] = new PerPartResourceInformation { Amount = dc.UnallocatedElectricChargeConsumption() };
                resourceData.AvailableKITModules.Remove(resourceData.AvailableKITModules[0]);
            }

            _inExecuteKITModules = true;

            _fixedDeltaTime = deltaTime;

            while (index != resourceData.AvailableKITModules.Count)
            {
                var mod = resourceData.AvailableKITModules[index];
                index++;

                if (resourceData.ModsCurrentlyRunning.Contains(mod))
                {
                    Debug.Log($"[KITResourceManager.ExecuteKITModules] This module {mod.KITPartName()} should not be marked busy at this stage");
                    continue;
                }
                resourceData.ModsCurrentlyRunning.Add(mod);

                InitializeModuleIfNeeded(resourceData, mod);

                if (VesselModified())
                {
                    index = 0;
                    RefreshActiveModules(resourceData);
                }

                if (resourceData.ModsCurrentlyRunning.Last() != mod)
                {
                    // there is an ordering problem in the above mod.KITFixedUpdate(), and we did not correctly track which module is
                    // currently running.
                    throw new InvalidOperationException("[KITResourceManager.ExecuteKITModules] the currently running mod is not the last running mod");
                }
                resourceData.ModsCurrentlyRunning.Remove(mod);
            }

            _wasteHeatAtEndOfProcessing = resourceData.CurrentCapacity(ResourceName.WasteHeat);

            RechargeBatteries(resourceData);
            OnKITProcessingFinished(resourceData);

            CheckIfEmergencyStopIsNeeded(resourceData);

            _inExecuteKITModules = false;
        }

        private void CheckIfEmergencyStopIsNeeded(ResourceData resourceData)
        {
            var ratio = resourceData.FillFraction(ResourceName.WasteHeat);

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
        private void RechargeBatteries(ResourceData resourceData)
        {
            // Check to see if the variable suppliers can be used to fill any missing EC from the vessel. This will charge
            // any batteries present on the ship.
            double fillBattery = resourceData.SpareCapacity(ResourceName.ElectricCharge);
            if (fillBattery <= 0) return;

            resourceData.AvailableResources[ResourceName.ElectricCharge] += CallVariableSuppliers(resourceData, ResourceName.ElectricCharge, 0, fillBattery);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitializeModuleIfNeeded(ResourceData data, IKITModule ikitModule)
        {
            if (data.FixedUpdateCalledMods.Contains(ikitModule)) return;

            // Hasn't had it's KITFixedUpdate() yet? call that first.
            data.FixedUpdateCalledMods.Add(ikitModule);
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

        private double CallVariableSuppliers(ResourceData resourceData, ResourceName resource, double obtainedAmount, double originalAmount, double resourceFiller = 0)
        {
            if (!resourceData.VariableSupplierModules.ContainsKey(resource)) return 0;

            var reducedObtainedAmount = obtainedAmount * _fixedDeltaTime;
            var reducedOriginalAmount = originalAmount * _fixedDeltaTime;

            IResourceManager secondaryInterface = _wasteHeatEquilibriumAchieved ? _wasteHeatEquilibriumResourceManager : (IResourceManager)resourceData;

            foreach (var mod in resourceData.VariableSupplierModules[resource])
            {
                var kitMod = mod as IKITModule;
                if (kitMod == null) continue;

                if (resourceData.TappedOutMods.Contains(mod)) continue; // it's tapped out for this cycle.
                if (resourceData.ModsCurrentlyRunning.Contains(kitMod)) continue;

                resourceData.ModsCurrentlyRunning.Add(kitMod);

                InitializeModuleIfNeeded(resourceData, kitMod);

                double perSecondAmount = originalAmount * (1 - (reducedObtainedAmount / reducedOriginalAmount));

                try
                {
                    UnityEngine.Profiling.Profiler.BeginSample($"ResourceManager.CallVariableSuppliers.ProvideResource.{kitMod.KITPartName()}");

                    var canContinue = mod.ProvideResource(secondaryInterface, resource, perSecondAmount + resourceFiller);
                    if (!canContinue) resourceData.TappedOutMods.Add(mod);
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

                resourceData.ModsCurrentlyRunning.Remove(kitMod);

                if (reducedObtainedAmount >= reducedOriginalAmount) return originalAmount;
            }

            return originalAmount * (reducedObtainedAmount / reducedOriginalAmount);
        }

        #endregion

    }
}
