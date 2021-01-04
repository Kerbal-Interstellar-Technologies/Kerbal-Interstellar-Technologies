using KIT.Interfaces;
using KIT.Resources;
using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

    public class ResourceManager : IResourceManager, IResourceScheduler
    {
        private readonly ICheatOptions _myCheatOptions;
        private readonly IVesselResources _vesselResources;
        private static double fudgeFactor = 0.99999;
        private Dictionary<ResourceName, double> _currentResources;
        private Dictionary<ResourceName, double> _currentMaxResources;

        private double _fixedDeltaTime;

        private readonly HashSet<IKITModule> _fixedUpdateCalledMods = new HashSet<IKITModule>(128);
        private readonly List<IKITModule> _modsCurrentlyRunning = new List<IKITModule>(128);

        public bool UseThisToHelpWithTesting;

        public Dictionary<ResourceName, Dictionary<IKITModule, PerPartResourceInformation>> ModConsumption;
        public Dictionary<ResourceName, Dictionary<IKITModule, PerPartResourceInformation>> ModProduction;

        private readonly OverHeatingResourceManager _overHeatingResourceManager;
        private readonly WasteHeatEquilibriumResourceManager _wasteHeatEquilibriumResourceManager;
        private IResourceManager _topLevelInterface;

        public ResourceManager(IVesselResources vesselResources, ICheatOptions cheatOptions)
        {
            _vesselResources = vesselResources;
            _myCheatOptions = cheatOptions;

            _resourceProductionStats = new ResourceProduction[ResourceName.WasteHeat - ResourceName.ElectricCharge + 1];

            ModProduction = new Dictionary<ResourceName, Dictionary<IKITModule, PerPartResourceInformation>>();
            ModConsumption = new Dictionary<ResourceName, Dictionary<IKITModule, PerPartResourceInformation>>();
            for (var i = ResourceName.ElectricCharge; i <= ResourceName.WasteHeat; i++)
            {
                ModProduction[i] = new Dictionary<IKITModule, PerPartResourceInformation>();
                ModConsumption[i] = new Dictionary<IKITModule, PerPartResourceInformation>();
            }

            _wasteHeatEquilibriumResourceManager = new WasteHeatEquilibriumResourceManager(this);
            _overHeatingResourceManager = new OverHeatingResourceManager();
        }

        #region IResourceManager implementation
        ICheatOptions IResourceManager.CheatOptions() => _myCheatOptions;
        private bool _inExecuteKITModules;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TrackableResource(ResourceName resource) => resource >= ResourceName.ElectricCharge && resource <= ResourceName.WasteHeat;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double WasteHeatRatio()
        {
            if (!_currentResources.TryGetValue(ResourceName.WasteHeat, out var currentHeat) || !_currentMaxResources.TryGetValue(ResourceName.WasteHeat, out var maxCurrentHeat)) return 0;

            maxCurrentHeat = Math.Max(maxCurrentHeat, 1);

            return currentHeat / maxCurrentHeat;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool UseOverheatingResourceManager()
        {
            // If players don't want the requests scaled back, so be it
            if (HighLogic.CurrentGame.Parameters.CustomParams<KITResourceParams>().DisableResourceConsumptionRateLimit) return false;

            var ratio = WasteHeatRatio();
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
        public double ConsumeResource(ResourceName resource, double wanted)
        {
            KITResourceSettings.ValidateResource(resource);

            PerPartResourceInformation tmpPPRI;
            if (!_inExecuteKITModules || wanted < 0)
            {
                Debug.Log(
                    $"[KITResourceManager.ConsumeResource] don't{(_inExecuteKITModules ? " use outside of IKITModules" : "")} {(wanted < 0 ? " consume negative amounts" : "")}");
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

            if (_myCheatOptions.InfiniteElectricity && resource == ResourceName.ElectricCharge)
            {
                tmpPPRI.Amount += wanted;
                ModConsumption[resource][lastMod] = tmpPPRI;
                return wanted;
            }

            if (!_currentResources.ContainsKey(resource))
            {
                _currentResources[resource] = 0;
            }

            double obtainedAmount = 0;
            double modifiedAmount = wanted * _fixedDeltaTime;

            var tmp = Math.Min(_currentResources[resource], modifiedAmount);
            obtainedAmount += tmp;
            _currentResources[resource] -= tmp;

            if (trackResourceUsage && tmp > 0)
                _resourceProductionStats[resource - ResourceName.ElectricCharge].InternalCurrentlySupplied += tmp;

            if (obtainedAmount >= modifiedAmount)
            {
                tmpPPRI.Amount += wanted;
                if (trackResourceUsage) ModConsumption[resource][lastMod] = tmpPPRI;

                return wanted;
            }

            var surplusWanted = _currentMaxResources[resource];
            if (resource == ResourceName.ChargedParticle) surplusWanted = 0;

            // Convert to seconds
            obtainedAmount = wanted * (obtainedAmount / modifiedAmount);
            obtainedAmount = CallVariableSuppliers(resource, obtainedAmount, wanted, surplusWanted);

            // We do not need to account for InternalCurrentlySupplied here, as the modules called above will call
            // ProduceResource which credits the InternalCurrentlySupplied field here.

            // is it close enough to being fully requested? (accounting for precision issues)
            var result = (obtainedAmount < (wanted * fudgeFactor)) ? wanted * (obtainedAmount / wanted) : wanted;

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
        public double ProduceResource(ResourceName resource, double amount, double max = -1)
        {
            KITResourceSettings.ValidateResource(resource);

            if (!_inExecuteKITModules)
            {
                Debug.Log(
                    $"[KITResourceManager.ProduceResource] don't{(_inExecuteKITModules ? " use outside of IKITModules" : "")} {(amount < 0 ? " produce negative amounts" : "")}");
                return 0;
            }

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


            if (resource == ResourceName.WasteHeat && _myCheatOptions.IgnoreMaxTemperature) return 0;

            if (!_currentResources.ContainsKey(resource))
            {
                _currentResources[resource] = 0;
            }
            _currentResources[resource] += amount * _fixedDeltaTime;

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
        private const double EquilibriumDifference = 0.0001;

        private void PickTopLevelInterface()
        {
            _topLevelInterface = _wasteHeatEquilibriumAchieved ? _wasteHeatEquilibriumResourceManager : (IResourceManager)this;

            if (UseOverheatingResourceManager())
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
        /// <param name="resourceAmounts">What resources are available for this call.</param>
        /// <param name="resourceMaxAmounts">Maximum resources available for this call</param>
        public void ExecuteKITModules(double deltaTime, ref Dictionary<ResourceName, double> resourceAmounts, ref Dictionary<ResourceName, double> resourceMaxAmounts)
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

            _currentResources = resourceAmounts;
            _currentMaxResources = resourceMaxAmounts;

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

            PickTopLevelInterface();

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

            RechargeBatteries(ref resourceAmounts, ref resourceMaxAmounts);
            _vesselResources.OnKITProcessingFinished(this);

            if (!_wasteHeatEquilibriumAchieved)
            {
                resourceAmounts.TryGetValue(ResourceName.WasteHeat, out _wasteHeatAtEndOfProcessing);
            }
            else
            {
                resourceAmounts[ResourceName.WasteHeat] = _wasteHeatAtEndOfProcessing;
            }

            CheckIfEmergencyStopIsNeeded(ref resourceAmounts, ref resourceMaxAmounts);

            _currentResources = null;
            _inExecuteKITModules = false;
        }

        private void CheckIfEmergencyStopIsNeeded(ref Dictionary<ResourceName, double> resourceAmounts, ref Dictionary<ResourceName, double> resourceMaxAmounts)
        {

            if (resourceAmounts.TryGetValue(ResourceName.WasteHeat, out var wasteHeatAmount) && resourceMaxAmounts.TryGetValue(ResourceName.WasteHeat, out var wasteHeatMaxAmount))
            {
                var ratio = wasteHeatAmount / wasteHeatMaxAmount;
                if (ratio > HighLogic.CurrentGame.Parameters.CustomParams<KITResourceParams>().EmergencyShutdownTemperaturePercentage)
                {
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
                else
                {
                    _overHeatingCounter = 0;
                }
            }

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

        private double CallVariableSuppliers(ResourceName resource, double obtainedAmount, double originalAmount, double resourceFiller = 0)
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

                var tmp = Math.Min(_currentResources[resource], reducedOriginalAmount - reducedObtainedAmount);
                _currentResources[resource] -= tmp;
                reducedObtainedAmount += tmp;

                _modsCurrentlyRunning.Remove(kitMod);

                if (reducedObtainedAmount >= reducedOriginalAmount) return originalAmount;
            }

            return obtainedAmount * (reducedObtainedAmount / reducedOriginalAmount);
        }

        public double ResourceSpareCapacity(ResourceName resourceIdentifier)
        {
            if (_currentMaxResources.TryGetValue(resourceIdentifier, out var maxResourceAmount) && _currentResources.TryGetValue(resourceIdentifier, out var currentResourceAmount))
                return maxResourceAmount - currentResourceAmount;

            return 0;
        }

        public double ResourceCurrentCapacity(ResourceName resourceIdentifier)
        {
            if (_currentResources.TryGetValue(resourceIdentifier, out var currentResourceAmount))
                return currentResourceAmount;

            return 0;
        }

        public double ResourceFillFraction(ResourceName resourceIdentifier)
        {
            if (_myCheatOptions.IgnoreMaxTemperature && resourceIdentifier == ResourceName.WasteHeat) return 0;

            if (_currentMaxResources.TryGetValue(resourceIdentifier, out var maxResourceAmount) &&
                _currentResources.TryGetValue(resourceIdentifier, out var currentResourceAmount))
            {
                if (maxResourceAmount > 0) return currentResourceAmount / maxResourceAmount;
            }


            return 0;
        }

        private readonly ResourceProduction[] _resourceProductionStats;

        public IResourceProduction ResourceProductionStats(ResourceName resourceIdentifier)
        {
            if (resourceIdentifier >= ResourceName.ElectricCharge && resourceIdentifier <= ResourceName.WasteHeat)
                return _resourceProductionStats[resourceIdentifier - ResourceName.ElectricCharge];

            return null;
        }
        #endregion

    }
}
