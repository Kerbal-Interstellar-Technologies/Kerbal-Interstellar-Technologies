using KIT.Extensions;
using KIT.PowerManagement;
using KIT.Resources;
using KIT.ResourceScheduler;
using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using TweakScale;
using UnityEngine;
using Waterfall;

namespace KIT.Propulsion
{
    [KSPModule("#LOC_KSPIE_ElectricEngine_partModuleName")]
    class ElectrostaticEngineControllerFX : ElectricEngineControllerFX { }

    [KSPModule("#LOC_KSPIE_ElectricEngine_partModuleName")]
    class ElectricEngineControllerFX : PartModule, IKITModule, IUpgradeableModule, IRescalable<ElectricEngineControllerFX>, IPartMassModifier
    {
        public const string Group = "ElectricEngineControllerFX";
        public const string GroupTitle = "#LOC_KSPIE_ElectricEngine_groupName";

        [KSPField(isPersistant = true)]
        public double storedAbsoluteFactor = 1;

        // Persistent True
        [KSPField(isPersistant = true)]
        public bool isupgraded;
        [KSPField(isPersistant = true)]
        public string propellantName;
        [KSPField(isPersistant = true)]
        public string propellantGUIName;
        [KSPField(isPersistant = true)]
        public bool propellantIsSaved;

        [KSPField(isPersistant = true)]
        public int propellantIndex;

        //Persistent False
        [KSPField]
        public string upgradeTechReq = "";
        [KSPField]
        public string gearsTechReq = "";
        [KSPField]
        public double powerReqMultWithoutReactor;
        [KSPField]
        public double powerReqMult = 1;
        [KSPField]
        public int type;
        [KSPField]
        public int upgradedtype;
        [KSPField]
        public double baseISP = 1000;
        [KSPField]
        public double ispGears = 1;
        [KSPField]
        public double exitArea;
        [KSPField]
        public double powerThrustMultiplier = 1;

        [KSPField]
        public double powerThrustMultiplierWithoutReactors;

        [KSPField]
        public float upgradeCost;
        [KSPField]
        public string originalName = "";
        [KSPField]
        public string upgradedName = "";
        [KSPField]
        public double wasteHeatMultiplier = 1;
        [KSPField]
        public double baseIspIonisationDivider = 3000;
        [KSPField]
        public double minimumIonisationRatio = 0.05;
        [KSPField]
        public double ionisationMultiplier = 0.5;
        [KSPField]
        public double baseEfficiency = 1;
        [KSPField]
        public double variableEfficiency;
        [KSPField]
        public float storedThrottle;
        [KSPField]
        public double particleEffectMult = 1;
        [KSPField]
        public bool ignoreWasteheat;
        [KSPField]
        public double GThreshold = 9;
        [KSPField]
        public double maxEffectPowerRatio = 0.75;

        [KSPField]
        public double Mk1Power = 1;
        [KSPField]
        public double Mk2Power = 1;
        [KSPField]
        public double Mk3Power = 1;
        [KSPField]
        public double Mk4Power = 1;
        [KSPField]
        public double Mk5Power = 1;
        [KSPField]
        public double Mk6Power = 1;
        [KSPField]
        public double Mk7Power = 1;

        [KSPField]
        public string Mk2Tech = "";
        [KSPField]
        public string Mk3Tech = "";
        [KSPField]
        public string Mk4Tech = "";
        [KSPField]
        public string Mk5Tech = "";
        [KSPField]
        public string Mk6Tech = "";
        [KSPField]
        public string Mk7Tech = "";

        // GUI
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_ElectricEngine_engineType")]
        public string engineTypeStr = "";
        [KSPField(groupName = Group, guiActive = true, guiName = "#autoLOC_6001377", guiUnits = "#autoLOC_7001408", guiFormat = "F3")]
        public double thrust_d;
        [KSPField(groupName = Group, guiActive = false, guiName = "#LOC_KSPIE_ElectricEngineController_CalculatedThrust", guiFormat = "F3", guiUnits = "kN")]//Calculated Thrust
        public double calculated_thrust;
        [KSPField(groupName = Group, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_ElectricEngine_warpIsp", guiFormat = "F1", guiUnits = "s")]
        public double engineIsp;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_ElectricEngine_maxPowerInput", guiFormat = "F1", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit")]
        public double scaledMaxPower;
        [KSPField(groupName = Group, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_ElectricEngine_activePropellantName")]
        public string propNameStr = "";
        [KSPField(groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_ElectricEngine_powerShare")]
        public string electricalPowerShareStr = "";
        [KSPField(groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_ElectricEngineController_MaximumPowerRequest", guiFormat = "F1", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit")]//Maximum Power Request
        public double maximum_power_request;
        [KSPField(groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_ElectricEngine_powerRequested", guiFormat = "F1", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit")] // Current Power Request
        public double current_power_request;
        [KSPField(groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_ElectricEngine_propellantEfficiency")]
        public string efficiencyStr = "";
        [KSPField(groupName = Group, guiActive = false, guiName = "#LOC_KSPIE_ElectricEngine_overheatEfficiency")]
        public string thermalEfficiency = "";
        [KSPField(groupName = Group, guiActive = false, guiName = "#LOC_KSPIE_ElectricEngine_heatProduction")]
        public string heatProductionStr = "";
        [KSPField(groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_ElectricEngine_upgradeCost")]
        public string upgradeCostStr = "";
        [KSPField(groupName = Group, guiActive = false, guiName = "#LOC_KSPIE_ElectricEngine_maxEffectivePower", guiFormat = "F1", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit")]
        public double maxEffectivePower;
        [KSPField(groupName = Group, guiActive = false, guiName = "#LOC_KSPIE_ElectricEngine_maxThrottlePower", guiFormat = "F1", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit")]
        public double modifiedMaxThrottlePower;
        [KSPField(groupName = Group, guiActive = false, guiName = "#LOC_KSPIE_FusionEngine_lightSpeedRatio", guiFormat = "F9", guiUnits = "c")]
        public double lightSpeedRatio;
        [KSPField(groupName = Group, guiActive = false, guiName = "#LOC_KSPIE_FusionEngine_timeDilation", guiFormat = "F10")]
        public double timeDilation = 1;
        [KSPField(groupName = Group, guiActive = false, guiName = "#LOC_KSPIE_ElectricEngineController_CapacityModifier")]//Capacity Modifier
        protected double powerCapacityModifier = 1;
        [KSPField(groupName = Group, guiActive = false, guiName = "#LOC_KSPIE_ElectricEngineController_AtmTrustEfficiency")]//Atm Trust Efficiency
        protected double _atmosphereThrustEfficiency;
        [KSPField(groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_ElectricEngineController_AtmTrustEfficiency", guiFormat = "F2", guiUnits = "%")]//Atm Trust Efficiency
        protected double _atmosphereThrustEfficiencyPercentage;
        [KSPField(groupName = Group, guiActive = false, guiName = "#LOC_KSPIE_ElectricEngineController_MaxFuelFlowRate")]//Max Fuel Flow Rate
        protected float _maxFuelFlowRate;
        [KSPField(groupName = Group, guiActive = false, guiName = "#LOC_KSPIE_ElectricEngineController_CurrentSpaceFuelFlowRate")]//Current Space Fuel Flow Rate
        protected double _currentSpaceFuelFlowRate;
        [KSPField(groupName = Group, guiActive = false, guiName = "#LOC_KSPIE_ElectricEngineController_PotentialSpaceFuelFlowRate")]//Potential Space Fuel Flow Rate
        protected double _simulatedSpaceFuelFlowRate;
        [KSPField(groupName = Group, guiActive = false, guiName = "#LOC_KSPIE_ElectricEngineController_FuelFlowModifier")]//Fuel Flow Modifier
        protected double _fuelFlowModifier;
        [KSPField(groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_ElectricEngineController_CurrentThrustinSpace", guiFormat = "F3", guiUnits = " kN")]//Current Thrust in Space
        protected double currentThrustInSpace;
        [KSPField(groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_ElectricEngineController_MaxThrustinSpace", guiFormat = "F3", guiUnits = " kN")]//Max Thrust in Space
        protected double simulatedThrustInSpace;
        [KSPField(guiActive = false)]
        public double simulated_max_thrust;

        [KSPField(guiActive = false)]
        public double currentPropellantEfficiency;
        [KSPField(groupName = Group, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_ElectricEngine_engineMass", guiFormat = "F3", guiUnits = " t")]
        public float partMass;

        [KSPField]
        public double prefabMass;
        [KSPField]
        public double expectedMass;
        [KSPField]
        public double desiredMass;

        [KSPField(guiActive = false)]
        protected double modifiedMaximumPowerForEngine;
        [KSPField(guiActive = false)]
        protected double modifiedCurrentPowerForEngine;

        [KSPField(guiActive = false)]
        protected double effectiveMaximumPower;
        [KSPField(guiActive = false)]
        protected double effectiveReceivedPower;
        [KSPField(guiActive = false)]
        protected double modifiedThrottle;
        [KSPField(guiActive = false)]
        protected double effectivePowerThrustModifier;
        [KSPField(guiActive = false)]
        public double actualPowerReceived;

        [KSPField]
        protected double maximumAvailablePowerForEngine;
        [KSPField]
        protected double currentAvailablePowerForEngine;
        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_ElectricEngineController_TotalPowerSupplied")]//Total Power Supplied
        protected double totalPowerSupplied;
        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_ElectricEngineController_MaximumAvailablePower")]//Maximum Available Power
        protected double availableMaximumPower;
        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_ElectricEngineController_CurrentAvailablePower")]//Current Available Power
        protected double availableCurrentPower;

        [KSPField]
        protected double maximumThrustFromPower = 0.001;
        [KSPField]
        protected double currentThrustFromPower = 0.001;


        [KSPField]
        protected double effectPower;
        [KSPField]
        public string EffectName = string.Empty;

        [KSPField]
        public double massTweakscaleExponent = 3;
        [KSPField]
        public double powerExponent = 3;
        [KSPField]
        public double massExponent = 3;
        [KSPField]
        public double maxPower = 1000;
        [KSPField]
        public double ratioHeadingVersusRequest;

        private int _initializationCountdown;
        private int _vesselChangedSioCountdown;
        private int _numberOfAvailableUpgradeTechs;

        private bool _hasRequiredUpgrade;
        private bool _hasGearTechnology;
        private bool _warpToReal;
        private bool _isFullyStarted;

        private double _maximumThrustInSpace;
        private double _effectiveSpeedOfLight;
        private double _modifiedEngineBaseIsp;
        private double _electricalShareF;
        private double _electricalConsumptionF;
        private double _heatProductionF;
        private double _modifiedCurrentPropellantIspMultiplier;
        private double _maxIsp;
        private double _effectiveIsp;
        private double _ispPersistent;

        private FloatCurve _ispFloatCurve;
        private ModuleEngines _attachedEngine;

        private List<ElectricEnginePropellant> _vesselPropellants;
        private List<string> _allPropellantsFx;

        private ConfigNode _effectNameToWaterfallValuesConfigNode;
        private float _waterfallFxControllerValue;
        private ModuleWaterfallFX _waterfallFx;
        private bool _updateWaterfallModule;
        private static readonly string WaterfallFxControllerName = "propellantFuel";

        // Properties
        public string UpgradeTechnology => upgradeTechReq;
        public double MaxPower => scaledMaxPower * powerReqMult * powerCapacityModifier;
        public double MaxEffectivePower => ignoreWasteheat ? MaxPower : MaxPower * CurrentPropellantEfficiency * ThermalEfficiency;
        public bool IsOperational => _attachedEngine != null && _attachedEngine.isOperational;

        public double PowerCapacityModifier
        {
            get
            {
                switch (_numberOfAvailableUpgradeTechs)
                {
                    case 0:
                        return Mk1Power;
                    case 1:
                        return Mk2Power;
                    case 2:
                        return Mk3Power;
                    case 3:
                        return Mk4Power;
                    case 4:
                        return Mk5Power;
                    case 5:
                        return Mk6Power;
                    case 6:
                        return Mk7Power;
                    default:
                        return 1;
                }
            }
        }

        private void DetermineTechLevel()
        {
            _numberOfAvailableUpgradeTechs = 0;
            if (PluginHelper.UpgradeAvailable(Mk2Tech))
                _numberOfAvailableUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(Mk3Tech))
                _numberOfAvailableUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(Mk4Tech))
                _numberOfAvailableUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(Mk5Tech))
                _numberOfAvailableUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(Mk6Tech))
                _numberOfAvailableUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(Mk7Tech))
                _numberOfAvailableUpgradeTechs++;
        }

        private ElectricEnginePropellant _currentPropellant;
        public ElectricEnginePropellant CurrentPropellant
        {
            get => _currentPropellant;
            set
            {
                if (value == null)
                    return;

                _currentPropellant = value;
                propellantIsSaved = true;
                propellantIndex = _vesselPropellants.IndexOf(_currentPropellant);
                propellantName = _currentPropellant.PropellantName;
                propellantGUIName = _currentPropellant.PropellantGUIName;
                _modifiedCurrentPropellantIspMultiplier = CurrentIspMultiplier;
            }
        }

        public double CurrentIspMultiplier =>
            type == (int)ElectricEngineType.VASIMR || type == (int)ElectricEngineType.ARCJET
                ? CurrentPropellant.DecomposedIspMult
                : CurrentPropellant.IspMultiplier;

        private double _mostRecentWasteHeatRatio;
        public double ThermalEfficiency
        {
            get
            {
                if (HighLogic.LoadedSceneIsFlight || CheatOptions.IgnoreMaxTemperature || ignoreWasteheat)
                    return 1;

                return 1 - _mostRecentWasteHeatRatio * _mostRecentWasteHeatRatio * _mostRecentWasteHeatRatio;
            }
        }

        public double CurrentPropellantThrustMultiplier => type == (int)ElectricEngineType.ARCJET ? CurrentPropellant.ThrustMultiplier : 1;

        public double CurrentPropellantEfficiency
        {
            get
            {
                double efficiency;

                if (type == (int)ElectricEngineType.ARCJET)
                {
                    // achieves higher efficiencies due to wasteheat preheating
                    efficiency = (ionisationMultiplier * CurrentPropellant.Efficiency) + ((1 - ionisationMultiplier) * baseEfficiency);
                }
                else if (type == (int)ElectricEngineType.VASIMR)
                {
                    var ionizationEnergyRatio = _attachedEngine.currentThrottle > 0
                        ? minimumIonisationRatio + (_attachedEngine.currentThrottle * ionisationMultiplier)
                        : minimumIonisationRatio;

                    ionizationEnergyRatio = Math.Min(1, ionizationEnergyRatio);

                    efficiency = (ionizationEnergyRatio * CurrentPropellant.Efficiency) + ((1 - ionizationEnergyRatio) * (baseEfficiency + ((1 - _attachedEngine.currentThrottle) * variableEfficiency)));
                }
                else if (type == (int)ElectricEngineType.VACUUMTHRUSTER)
                {
                    // achieves higher efficiencies due to wasteheat preheating
                    efficiency = CurrentPropellant.Efficiency;
                }
                else
                {
                    var ionizationEnergyRatio = Math.Min(1, 1 / (baseISP / baseIspIonisationDivider));

                    // achieve higher efficiencies at higher base isp
                    efficiency = (ionizationEnergyRatio * CurrentPropellant.Efficiency) + ((1 - ionizationEnergyRatio) * baseEfficiency);
                }

                return efficiency;
            }
        }

        public void VesselChangedSoi()
        {
            _vesselChangedSioCountdown = 10;
        }

        // Events
        [KSPEvent(groupName = Group, guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_ElectricEngine_nextPropellant", active = true)]
        public void ToggleNextPropellantEvent()
        {
            ToggleNextPropellant();
        }

        [KSPEvent(groupName = Group, guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_ElectricEngine_previous Propellant", active = true)]
        public void TogglePreviousPropellantEvent()
        {
            TogglePreviousPropellant();
        }

        public void OnRescale(ScalingFactor factor)
        {
            storedAbsoluteFactor = (double)(decimal)factor.absolute.linear;

            ScaleParameters();
        }

        private void ScaleParameters()
        {
            prefabMass = part.prefabMass;
            expectedMass = prefabMass * Math.Pow(storedAbsoluteFactor, massTweakscaleExponent);
            desiredMass = prefabMass * Math.Pow(storedAbsoluteFactor, massExponent);
            scaledMaxPower = maxPower * Math.Pow(storedAbsoluteFactor, powerExponent);
        }

        public float GetModuleMass(float defaultMass, ModifierStagingSituation sit)
        {
            return 0.0f;
        }

        public ModifierChangeWhen GetModuleMassChangeWhen()
        {
            return ModifierChangeWhen.STAGED;
        }

        [KSPEvent(groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_ElectricEngine_retrofit", active = true)]
        public void RetrofitEngine()
        {
            if (ResearchAndDevelopment.Instance == null) return;
            if (isupgraded || ResearchAndDevelopment.Instance.Science < upgradeCost) return;

            upgradePartModule();
            ResearchAndDevelopment.Instance.AddScience(-upgradeCost, TransactionReasons.RnDPartPurchase);
        }

        // Actions
        [KSPAction("#LOC_KSPIE_ElectricEngine_nextPropellant")]
        public void ToggleNextPropellantAction(KSPActionParam param)
        {
            ToggleNextPropellantEvent();
        }

        [KSPAction("#LOC_KSPIE_ElectricEngine_previous Propellant")]
        public void TogglePreviousPropellantAction(KSPActionParam param)
        {
            TogglePreviousPropellantEvent();
        }

        // Methods
        private void UpdateEngineTypeString()
        {
            engineTypeStr = isupgraded ? upgradedName : originalName;
        }

        public override void OnLoad(ConfigNode node)
        {
            if (isupgraded)
                upgradePartModule();
            UpdateEngineTypeString();
        }

        public override void OnStart(StartState state)
        {
            if (state != StartState.Editor)
            {
                if (!vessel.FindPartModulesImplementing<FNGenerator>().Any(m => m.isHighPower))
                {
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (powerThrustMultiplier == 1 && powerThrustMultiplierWithoutReactors > 0)
                        powerThrustMultiplier = powerThrustMultiplierWithoutReactors;

                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (powerReqMult == 1 && powerReqMultWithoutReactor > 0)
                        powerReqMult = powerReqMultWithoutReactor;
                }
            }

            ScaleParameters();

            // initialise resources
            //resources_to_supply = new [] { KITResourceSettings.WasteHeatInMegawatt };
            base.OnStart(state);

            AttachToEngine();
            DetermineTechLevel();
            powerCapacityModifier = PowerCapacityModifier;

            _initializationCountdown = 10;
            _ispFloatCurve = new FloatCurve();
            _ispFloatCurve.Add(0, (float)baseISP);
            _effectiveSpeedOfLight = PluginSettings.Config.SpeedOfLight;
            _hasGearTechnology = string.IsNullOrEmpty(gearsTechReq) || PluginHelper.UpgradeAvailable(gearsTechReq);
            _modifiedEngineBaseIsp = baseISP * PluginSettings.Config.ElectricEngineIspMult;
            _hasRequiredUpgrade = this.HasTechsRequiredToUpgrade();

            if (_hasRequiredUpgrade && (isupgraded || state == StartState.Editor))
                upgradePartModule();

            UpdateEngineTypeString();
            InitializePropellantMode();

            ConfigurePropellant(true);

            _attachedEngine.maxThrust = (float)maximumThrustFromPower;

            _effectNameToWaterfallValuesConfigNode =
                GameDatabase.Instance.GetConfigNodes("ELECTRIC_PROPELLANT_TO_WATERFALL_INDEX")?[0];
            if (_effectNameToWaterfallValuesConfigNode == null)
            {
                Debug.Log("[ElectricEngineControllerFX] Unable to find ELECTRIC_PROPELLANT_TO_WATERFALL_INDEX");
            }

            _waterfallFx = part.FindModuleImplementing<ModuleWaterfallFX>();
        }

        private void InitializePropellantMode()
        {
            // initialize propellant
            _allPropellantsFx = GetAllPropellants().Select(m => m.ParticleFXName).Distinct().ToList();
            _vesselPropellants = ElectricEnginePropellant.GetPropellantsEngineForType(type);

            if (propellantIsSaved || HighLogic.LoadedSceneIsEditor)
            {
                if (!string.IsNullOrEmpty(propellantName))
                {
                    CurrentPropellant = _vesselPropellants.FirstOrDefault(m => m.PropellantName == propellantName) ??
                                        _vesselPropellants.FirstOrDefault(m => m.PropellantGUIName == propellantName);
                }

                if (CurrentPropellant == null && !string.IsNullOrEmpty(propellantGUIName))
                {
                    CurrentPropellant = _vesselPropellants.FirstOrDefault(m => m.PropellantName == propellantGUIName) ??
                                        _vesselPropellants.FirstOrDefault(m => m.PropellantGUIName == propellantGUIName);
                }
            }

            if (_vesselPropellants == null)
            {
                Debug.LogWarning("[KSPI]: SetupPropellants _vesselPropellants is still null");
            }
            else if (CurrentPropellant == null)
            {
                CurrentPropellant = propellantIndex < _vesselPropellants.Count ? _vesselPropellants[propellantIndex] : _vesselPropellants.First();
            }
        }

        private void AttachToEngine()
        {
            _attachedEngine = part.FindModuleImplementing<ModuleEngines>();
            if (_attachedEngine == null) return;

            var finalTrustField = _attachedEngine.Fields[nameof(_attachedEngine.finalThrust)];
            finalTrustField.guiActive = false;

            var realIspField = _attachedEngine.Fields[nameof(_attachedEngine.realIsp)];
            realIspField.guiActive = false;
        }

        private void ConfigurePropellant(bool moveNext)
        {
            var index = propellantIndex;

            // Debug.Log($"[ConfigurePropellant] propellants count {_vesselPropellants.Count}");

            if (index >= _vesselPropellants.Count)
            {
                // Debug.Log($"[ConfigurePropellant] should not have reached this state (propellants count {_vesselPropellants.Count}");
                propellantIndex = 0;
            }

            var vesselResources = part.vessel.parts.SelectMany(p => p.Resources).Select(m => m.resourceName).Distinct()
                .ToArray();

            var found = false;

            for (index = propellantIndex + (moveNext ? 1 : -1);
                index != propellantIndex;
                index += (moveNext ? 1 : -1))
            {
                if (moveNext)
                {
                    if (index >= _vesselPropellants.Count) index = 0;
                }
                else if (index < 0) index = _vesselPropellants.Count - 1;

                // Debug.Log($"[ConfigurePropellant] index is {index} - {_vesselPropellants[index].PropellantName}");

                // During flight, it must be on the ship
                if (HighLogic.LoadedSceneIsFlight && !vesselResources.Contains(_vesselPropellants[index].Propellant.name)) continue;

                // Propellant requires a different engine? Skip
                if ((_vesselPropellants[index].SupportedEngines & type) != type) continue;

                // Are all required fuels defined?
                if (PartResourceLibrary.Instance.GetDefinition(_vesselPropellants[index].Propellant.name) ==
                    null) continue;

                found = true;
                break;
            }

            if (!found) return;

            propellantIndex = index;
            CurrentPropellant = _vesselPropellants[index];
            var prop = CurrentPropellant.Propellant;

            // Debug.Log($"at end of processing, changing propellant. creating new node with name = {prop.name}, ratio = {prop.ratio} and DrawGauge = {prop.drawStackGauge}");

            //Get the Ignition state, i.e. is the engine shutdown or activated
            var engineState = _attachedEngine.getIgnitionState;

            _attachedEngine.Shutdown();

            var newPropNode = new ConfigNode();
            var propellantConfigNode = newPropNode.AddNode("PROPELLANT");
            propellantConfigNode.AddValue("name", prop.name);
            propellantConfigNode.AddValue("ratio", prop.ratio);
            propellantConfigNode.AddValue("DrawGauge", prop.drawStackGauge);

            _attachedEngine.Load(newPropNode);

            UpdateIsp();

            if (engineState)
                _attachedEngine.Activate();

            _effectNameToWaterfallValuesConfigNode?.TryGetValue(CurrentPropellant.ParticleFXName,
                ref _waterfallFxControllerValue);
            _updateWaterfallModule = _waterfallFx?.GetControllerNames().Contains(WaterfallFxControllerName) ?? false;
        }

        public override void OnUpdate()
        {
            // Base class update
            base.OnUpdate();

            // stop engines and drop out of timewarp when X pressed
            if (vessel.packed && storedThrottle > 0 && Input.GetKeyDown(KeyCode.X))
            {
                // Return to realtime
                TimeWarp.SetRate(0, true);

                storedThrottle = 0;
                vessel.ctrlState.mainThrottle = storedThrottle;
            }

            // When transitioning from timewarp to real update throttle
            if (_warpToReal)
            {
                vessel.ctrlState.mainThrottle = storedThrottle;
                _warpToReal = false;
            }

            if (ResearchAndDevelopment.Instance != null)
            {
                Events[nameof(RetrofitEngine)].active = !isupgraded && ResearchAndDevelopment.Instance.Science >= upgradeCost && _hasRequiredUpgrade;
                Fields[nameof(upgradeCostStr)].guiActive = !isupgraded && _hasRequiredUpgrade;
                upgradeCostStr = ResearchAndDevelopment.Instance.Science + "/" + upgradeCost.ToString("0") + " " + Localizer.Format("#LOC_KSPIE_ElectricEngine_science");
            }
            else
            {
                Events[nameof(RetrofitEngine)].active = false;
                Fields[nameof(upgradeCostStr)].guiActive = false;
            }

            var isInfinite = _currentPropellant.IsInfinite;

            Fields[nameof(engineIsp)].guiActive = !isInfinite;
            Fields[nameof(propNameStr)].guiActive = !isInfinite;
            Fields[nameof(efficiencyStr)].guiActive = !isInfinite;
            Fields[nameof(thermalEfficiency)].guiActive = !ignoreWasteheat;

            if (IsOperational)
            {
                Fields[nameof(electricalPowerShareStr)].guiActive = true;
                Fields[nameof(heatProductionStr)].guiActive = true;
                Fields[nameof(efficiencyStr)].guiActive = true;
                electricalPowerShareStr = _electricalShareF.ToString("P2");
                heatProductionStr = PluginHelper.GetFormattedPowerString(_heatProductionF);

                if (CurrentPropellant == null)
                    efficiencyStr = "";
                else
                {
                    efficiencyStr = CurrentPropellantEfficiency.ToString("P2");
                    thermalEfficiency = ThermalEfficiency.ToString("P2");
                }
            }
            else
            {
                Fields[nameof(electricalPowerShareStr)].guiActive = false;
                Fields[nameof(heatProductionStr)].guiActive = false;
                Fields[nameof(efficiencyStr)].guiActive = false;
            }

            if (_updateWaterfallModule && _waterfallFx != null) _waterfallFx.SetControllerValue(WaterfallFxControllerName, _waterfallFxControllerValue);
        }

        // ReSharper disable once UnusedMember.Global
        public void Update()
        {
            partMass = part.mass;
            propNameStr = CurrentPropellant != null ? CurrentPropellant.PropellantGUIName : "";
        }

        private double IspGears => _hasGearTechnology ? ispGears : 1;

        private double ModifiedThrottle =>
            CurrentPropellant.SupportedEngines == 8
                ? _attachedEngine.currentThrottle
                : Math.Min((double)(decimal)_attachedEngine.currentThrottle * IspGears, 1);

        private double ThrottleModifiedIsp()
        {
            if (double.IsNaN(_attachedEngine.currentThrottle))
            {
                // Debug.Log("[ElectricEngineControllerFX] ThrottleModifiedISP is NaN - returning 0");
                return 0;
            }

            var currentThrottle = (double)(decimal)_attachedEngine.currentThrottle;

            return CurrentPropellant.SupportedEngines == 8
                ? 1
                : currentThrottle < (1d / IspGears)
                    ? IspGears
                    : IspGears - ((currentThrottle - (1d / IspGears)) * IspGears);
        }

        // ReSharper disable once UnusedMember.Global
        public void FixedUpdate()
        {

        }

        private void IdleEngine()
        {
            thrust_d = 0;

            if (double.IsNaN(_maxFuelFlowRate))
            {
                // Debug.Log($"[IdleEngine] _maxFuelFlowRate is NaN");
                return;
            }

            if (IsValidPositiveNumber(simulated_max_thrust) && IsValidPositiveNumber(simulatedThrustInSpace))
            {
                UpdateIsp(Math.Max(0, simulated_max_thrust / simulatedThrustInSpace));
                _maxFuelFlowRate = (float)Math.Max(_simulatedSpaceFuelFlowRate, 0);
                _attachedEngine.maxFuelFlow = _maxFuelFlowRate;
            }
            else
            {
                UpdateIsp();
                _maxFuelFlowRate = 0;
                _attachedEngine.maxFuelFlow = _maxFuelFlowRate;
            }

            if (_attachedEngine is ModuleEnginesFX && particleEffectMult > 0)
                part.Effect(CurrentPropellant.ParticleFXName, 0, -1);
        }

        public void CalculateTimeDilation()
        {
            var worldSpaceVelocity = vessel.orbit.GetFrameVel().magnitude;

            lightSpeedRatio = Math.Min(_effectiveSpeedOfLight == 0.0 ? 1.0 : worldSpaceVelocity / _effectiveSpeedOfLight, 0.9999999999);

            timeDilation = Math.Sqrt(1 - (lightSpeedRatio * lightSpeedRatio));
        }

        private static bool IsValidPositiveNumber(double value)
        {
            if (double.IsNaN(value))
                return false;

            if (double.IsInfinity(value))
                return false;

            return value > 0;
        }

        private void PersistentThrust(double fixedDeltaTime, double universalTime, Vector3d thrustDirection, double vesselMass, double thrust, double isp)
        {
            var deltaVv = CalculateDeltaVV(thrustDirection, vesselMass, fixedDeltaTime, thrust, isp, out var demandMass);
            string message;

            var persistentThrustDot = Vector3d.Dot(thrustDirection, vessel.obt_velocity);
            if (persistentThrustDot < 0 && (vessel.obt_velocity.magnitude <= deltaVv.magnitude * 2))
            {
                message = Localizer.Format("#LOC_KSPIE_ElectricEngineController_PostMsg1");
                ScreenMessages.PostScreenMessage(message, 5, ScreenMessageStyle.UPPER_CENTER);//"Thrust warp stopped - orbital speed too low"
                Debug.Log("[KSPI]: " + message);
                TimeWarp.SetRate(0, true);
                return;
            }

            double fuelRatio = 0;

            // determine fuel availability
            if (!CurrentPropellant.IsInfinite && !CheatOptions.InfinitePropellant && CurrentPropellant.ResourceDefinition.density > 0)
            {
                var requestedAmount = demandMass / (double)(decimal)CurrentPropellant.ResourceDefinition.density;
                if (IsValidPositiveNumber(requestedAmount))
                    fuelRatio = part.RequestResource(CurrentPropellant.Propellant.name, requestedAmount) / requestedAmount;
            }
            else
                fuelRatio = 1;

            if (!double.IsNaN(fuelRatio) && !double.IsInfinity(fuelRatio) && fuelRatio > 0)
            {
                vessel.orbit.Perturb(deltaVv * fuelRatio, universalTime);
            }

            if (thrust > 0.0000005 && fuelRatio < 0.999999 && _isFullyStarted)
            {
                message = Localizer.Format("#LOC_KSPIE_ElectricEngineController_PostMsg2", fuelRatio, thrust);// "Thrust warp stopped - " + + " propellant depleted thrust: " +
                ScreenMessages.PostScreenMessage(message, 5, ScreenMessageStyle.UPPER_CENTER);
                Debug.Log("[KSPI]: " + message);
                TimeWarp.SetRate(0, true);
            }
        }

        public void upgradePartModule()
        {
            isupgraded = true;
            type = upgradedtype;
            _vesselPropellants = ElectricEnginePropellant.GetPropellantsEngineForType(type);
            engineTypeStr = upgradedName;

            if (type == (int)ElectricEngineType.VACUUMTHRUSTER && !part.Resources.Contains(KITResourceSettings.VacuumPlasma))
            {
                var node = new ConfigNode("RESOURCE");
                node.AddValue("name", KITResourceSettings.VacuumPlasma);
                node.AddValue("maxAmount", scaledMaxPower * 0.0000001);
                node.AddValue("amount", scaledMaxPower * 0.0000001);
                part.AddResource(node);
            }
        }

        public override string GetInfo()
        {
            return Localizer.Format("#LOC_KSPIE_ElectricEngine_maxPowerConsumption") + ": " + PluginHelper.GetFormattedPowerString(maxPower * powerReqMult);
        }

        private void TogglePropellant(bool next)
        {
            if (next)
                ToggleNextPropellant();
            else
                TogglePreviousPropellant();
        }

        private void ToggleNextPropellant()
        {
            Debug.Log("[KSPI]: ElectricEngineControllerFX toggleNextPropellant");
            if (_vesselPropellants.Count == 0) return;

            ConfigurePropellant(true);
        }

        private void TogglePreviousPropellant()
        {
            Debug.Log("[KSPI]: ElectricEngineControllerFX togglePreviousPropellant");
            if (_vesselPropellants.Count == 0) return;

            ConfigurePropellant(false);
        }

        private double EvaluateMaxThrust(double powerSupply)
        {
            if (CurrentPropellant == null) return 0;

            if (_modifiedCurrentPropellantIspMultiplier <= 0) return 0;

            return CurrentPropellantEfficiency * GetPowerThrustModifier() * powerSupply / (_modifiedEngineBaseIsp * _modifiedCurrentPropellantIspMultiplier * PhysicsGlobals.GravitationalAcceleration);
        }

        private void UpdateIsp(double ispEfficiency = 1)
        {
            // Debug.Log("before calling ThrottleModifiedIsp()");

            var throttleModifiedIsp = ThrottleModifiedIsp();

            // Debug.Log($"[UpdateIsp] timeDilation is {timeDilation}, ispEfficiency is {ispEfficiency}, _modifiedEngineBaseIsp is {_modifiedEngineBaseIsp}, _modifiedCurrentPropellantIspMultiplier is {_modifiedCurrentPropellantIspMultiplier}, CurrentPropellantThrustMultiplier is {CurrentPropellantThrustMultiplier} and throttleModifiedIsp is {throttleModifiedIsp}");

            engineIsp = timeDilation * ispEfficiency * _modifiedEngineBaseIsp * _modifiedCurrentPropellantIspMultiplier * CurrentPropellantThrustMultiplier * throttleModifiedIsp;

            if (Double.IsNaN(engineIsp))
            {
                // Debug.Log($"[ElectricEngineControllerFX] refusing to set a NaN engineIsp");
                return;
            }

            _ispFloatCurve.Curve.RemoveKey(0);
            _ispFloatCurve.Add(0, (float)engineIsp);
            _attachedEngine.atmosphereCurve = _ispFloatCurve;

            _changeFlipFlop = true;
        }

        private double GetPowerThrustModifier()
        {
            return GameConstants.BaseThrustPowerMultiplier * PluginSettings.Config.GlobalElectricEnginePowerMaxThrustMult * powerThrustMultiplier;
        }

        private double GetAtmosphericDensityModifier()
        {
            return Math.Max(1.0 - (part.vessel.atmDensity * PluginSettings.Config.ElectricEngineAtmosphericDensityThrustLimiter), 0.0);
        }

        private static List<ElectricEnginePropellant> GetAllPropellants()
        {
            var configNodes = GameDatabase.Instance.GetConfigNodes("ELECTRIC_PROPELLANT");

            List<ElectricEnginePropellant> propellantList;
            if (configNodes.Length == 0)
            {
                PluginHelper.ShowInstallationErrorMessage();
                propellantList = new List<ElectricEnginePropellant>();
            }
            else
                propellantList = configNodes.Select(prop => new ElectricEnginePropellant(prop)).ToList();


            return propellantList;
        }

        public static Vector3d CalculateDeltaVV(Vector3d thrustDirection, double totalMass, double deltaTime, double thrust, double isp, out double demandMass)
        {
            // Mass flow rate
            var massFlowRate = thrust / (isp * PhysicsGlobals.GravitationalAcceleration);
            // Change in mass over time interval dT
            var dm = massFlowRate * deltaTime;
            // Resource demand from propellants with mass
            demandMass = dm;
            // Mass at end of time interval dT
            var finalMass = totalMass - dm;
            // deltaV amount
            var deltaV = finalMass > 0 && totalMass > 0
                ? isp * PhysicsGlobals.GravitationalAcceleration * Math.Log(totalMass / finalMass)
                : 0;

            // Return deltaV vector
            return deltaV * thrustDirection;
        }

        private bool _changeFlipFlop;

        public ModuleConfigurationFlags ModuleConfiguration() => ModuleConfigurationFlags.Third;

        public void KITFixedUpdate(IResourceManager resMan)
        {
            if (_attachedEngine == null || !HighLogic.LoadedSceneIsFlight) return;

            if (_changeFlipFlop)
            {
                _changeFlipFlop = false;
                return;
            }
            
            _mostRecentWasteHeatRatio = resMan.FillFraction(ResourceName.WasteHeat);

            // disable exhaust effects
            if (!string.IsNullOrEmpty(EffectName))
                part.Effect(EffectName, (float)effectPower);

            if (_allPropellantsFx != null)
            {
                // set all FX to zero
                foreach (var propName in _allPropellantsFx)
                {
                    var currentEffectPower = CurrentPropellant.ParticleFXName == propName ? effectPower : 0;
                    part.Effect(propName, (float)currentEffectPower);
                }
            }

            if (!isEnabled) return;

            if (_initializationCountdown > 0)
                _initializationCountdown--;

            if (_vesselChangedSioCountdown > 0)
                _vesselChangedSioCountdown--;

            CalculateTimeDilation();

            if (CurrentPropellant == null) return;

            if (!vessel.packed && !_warpToReal)
                storedThrottle = vessel.ctrlState.mainThrottle;

            // XXX 
            storedThrottle = vessel.ctrlState.mainThrottle;

            maxEffectivePower = MaxEffectivePower;
            currentPropellantEfficiency = CurrentPropellantEfficiency;

            var sumOfAllEffectivePower = vessel.FindPartModulesImplementing<ElectricEngineControllerFX>().Where(ee => ee.IsOperational).Sum(ee => ee.MaxEffectivePower);
            _electricalShareF = 1; // sumOfAllEffectivePower > 0 ? maxEffectivePower / sumOfAllEffectivePower : 1;

            if (Double.IsNaN(_attachedEngine.currentThrottle))
            {
                Debug.Log($"[TEST] _attachedEngine.currentThrottle is {_attachedEngine.currentThrottle}");
                _attachedEngine.currentThrottle = 0;

                return;
            }

            modifiedThrottle = ModifiedThrottle;
            modifiedMaxThrottlePower = maxEffectivePower * modifiedThrottle;

            if (!_attachedEngine.getIgnitionState)
            {
                totalPowerSupplied = resMan.Consume(ResourceName.ElectricCharge, maxPower);
            }
            else
            {
                totalPowerSupplied = maxPower;
            }


            var stats = resMan.ProductionStats(ResourceName.ElectricCharge);
            availableMaximumPower = totalPowerSupplied; // stats.PreviousDataSupplied() ? stats.PreviouslySupplied() : stats.CurrentSupplied();
            availableCurrentPower = totalPowerSupplied;

            maximumAvailablePowerForEngine = availableMaximumPower * _electricalShareF;
            currentAvailablePowerForEngine = availableCurrentPower * _electricalShareF;

            maximumThrustFromPower = EvaluateMaxThrust(maximumAvailablePowerForEngine);
            currentThrustFromPower = EvaluateMaxThrust(currentAvailablePowerForEngine);

            modifiedMaximumPowerForEngine = maximumAvailablePowerForEngine * modifiedThrottle;
            modifiedCurrentPowerForEngine = currentAvailablePowerForEngine * modifiedThrottle;

            maximum_power_request = availableMaximumPower;
            current_power_request = totalPowerSupplied;

            // request electric power
            actualPowerReceived = totalPowerSupplied;

            // produce waste heat
            var heatModifier = (1 - currentPropellantEfficiency) * CurrentPropellant.WasteHeatMultiplier;
            _heatProductionF = actualPowerReceived * heatModifier;
            var maxHeatToProduce = maximumAvailablePowerForEngine * heatModifier;

            resMan.Produce(ResourceName.WasteHeat, _heatProductionF);

            // update GUI Values
            _electricalConsumptionF = actualPowerReceived;

            _effectiveIsp = _modifiedEngineBaseIsp * _modifiedCurrentPropellantIspMultiplier * ThrottleModifiedIsp();
            _maxIsp = _effectiveIsp * CurrentPropellantThrustMultiplier;

            var throttleModifier = ispGears == 1 ? 1 : ModifiedThrottle;

            effectivePowerThrustModifier = timeDilation * currentPropellantEfficiency * CurrentPropellantThrustMultiplier * GetPowerThrustModifier();

            effectiveMaximumPower = effectivePowerThrustModifier * modifiedMaxThrottlePower * throttleModifier;
            effectiveReceivedPower = effectivePowerThrustModifier * actualPowerReceived * throttleModifier;

            _maximumThrustInSpace = effectiveMaximumPower / _effectiveIsp / PhysicsGlobals.GravitationalAcceleration;
            currentThrustInSpace = _effectiveIsp <= 0 ? 0 : effectiveReceivedPower / _effectiveIsp / PhysicsGlobals.GravitationalAcceleration;
            simulatedThrustInSpace = _effectiveIsp <= 0 ? 0 : effectiveReceivedPower / _effectiveIsp / PhysicsGlobals.GravitationalAcceleration;

            // Debug.Log($"maxThrust is {simulatedThrustInSpace}, and _maxIsp is {_maxIsp}, effectiveReceivedPower is {effectiveReceivedPower}, _effectiveIsp is {_effectiveIsp}, and PhysicsGlobals.GravitationalAcceleration is {PhysicsGlobals.GravitationalAcceleration}, and _effectiveIsp is {_effectiveIsp}, and effectivePowerThrustModifier is {effectivePowerThrustModifier}, and  {effectiveMaximumPower} is {effectiveMaximumPower}, and CurrentPropellantThrustMultiplier is {CurrentPropellantThrustMultiplier}, and _modifiedEngineBaseIsp is {_modifiedEngineBaseIsp}, and _modifiedCurrentPropellantIspMultiplier is {_modifiedCurrentPropellantIspMultiplier}");
            
            _attachedEngine.maxThrust = (float)Math.Max(simulatedThrustInSpace, 0.001);

            _currentSpaceFuelFlowRate = _maxIsp <= 0 ? 0 : currentThrustInSpace / _maxIsp / PhysicsGlobals.GravitationalAcceleration;
            _simulatedSpaceFuelFlowRate = _maxIsp <= 0 ? 0 : simulatedThrustInSpace / _maxIsp / PhysicsGlobals.GravitationalAcceleration;

            var maxThrustWithCurrentThrottle = currentThrustInSpace * throttleModifier;

            calculated_thrust = CurrentPropellant.SupportedEngines == 8
                ? maxThrustWithCurrentThrottle
                : Math.Max(maxThrustWithCurrentThrottle - (exitArea * vessel.staticPressurekPa), 0);

            simulated_max_thrust = CurrentPropellant.SupportedEngines == 8
                ? simulatedThrustInSpace
                : Math.Max(simulatedThrustInSpace - (exitArea * vessel.staticPressurekPa), 0);

            var throttle = _attachedEngine.getIgnitionState && _attachedEngine.currentThrottle > 0 ? Math.Max(_attachedEngine.currentThrottle, 0.01) : 0;

            if (throttle > 0)
            {
                if (IsValidPositiveNumber(calculated_thrust) && IsValidPositiveNumber(maxThrustWithCurrentThrottle))
                {
                    _atmosphereThrustEfficiency = Math.Min(1, calculated_thrust / maxThrustWithCurrentThrottle);

                    _atmosphereThrustEfficiencyPercentage = _atmosphereThrustEfficiency * 100;

                    UpdateIsp(_atmosphereThrustEfficiency);

                    _fuelFlowModifier = ispGears == 1
                        ? 1 / throttle
                        : ModifiedThrottle / throttle;

                    _maxFuelFlowRate = (float)Math.Max(_atmosphereThrustEfficiency * _currentSpaceFuelFlowRate * _fuelFlowModifier, 0);
                    // Debug.Log($"[ElectricEngineControllerFX] _maxFuelFlowRate is {_maxFuelFlowRate}");
                    _attachedEngine.maxFuelFlow = _maxFuelFlowRate;
                }
                else
                {
                    UpdateIsp();
                    _atmosphereThrustEfficiency = 0;
                    _maxFuelFlowRate = 0;
                    _attachedEngine.maxFuelFlow = _maxFuelFlowRate;
                }

                if (vessel.packed == false)
                {
                    // allow throttle to be used up to Geeforce threshold
                    TimeWarp.GThreshold = GThreshold;

                    _isFullyStarted = true;
                    _ispPersistent = _attachedEngine.realIsp;

                    thrust_d = _attachedEngine.requestedMassFlow * PhysicsGlobals.GravitationalAcceleration * _ispPersistent;

                    ratioHeadingVersusRequest = 0;
                }
                else if (vessel.packed && _attachedEngine.isEnabled && FlightGlobals.ActiveVessel == vessel && _initializationCountdown == 0)
                {
                    _warpToReal = true; // Set to true for transition to realtime

                    thrust_d = calculated_thrust;

                    ratioHeadingVersusRequest = vessel.PersistHeading(_vesselChangedSioCountdown > 0, ratioHeadingVersusRequest == 1);

                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (ratioHeadingVersusRequest == 1)
                        PersistentThrust((double)(decimal)TimeWarp.fixedDeltaTime, Planetarium.GetUniversalTime(), part.transform.up, vessel.totalMass, thrust_d, _ispPersistent);
                }
                else
                    IdleEngine();
            }
            else
                IdleEngine();

            if (_attachedEngine is ModuleEnginesFX && particleEffectMult > 0)
            {
                var engineFuelFlow = _currentSpaceFuelFlowRate * _attachedEngine.currentThrottle;
                var currentMaxFuelFlowRate = _attachedEngine.maxThrust / _attachedEngine.realIsp / PhysicsGlobals.GravitationalAcceleration;
                var engineMaxFuelFlowRat = _maximumThrustInSpace / _attachedEngine.realIsp / PhysicsGlobals.GravitationalAcceleration;

                var currentEffectPower = Math.Min(1, particleEffectMult * (engineFuelFlow / currentMaxFuelFlowRate));
                var maximumEffectPower = Math.Min(1, particleEffectMult * (engineFuelFlow / engineMaxFuelFlowRat));

                effectPower = currentEffectPower * (1 - maxEffectPowerRatio) + maximumEffectPower * maxEffectPowerRatio;
            }

            var vacuumPlasmaResource = part.Resources[KITResourceSettings.VacuumPlasma];
            if (isupgraded && vacuumPlasmaResource != null)
            {
                var calculatedConsumptionInTon = vessel.packed ? 0 : simulatedThrustInSpace / engineIsp / PhysicsGlobals.GravitationalAcceleration;
                var vacuumPlasmaResourceAmount = calculatedConsumptionInTon * 2000 * TimeWarp.fixedDeltaTime;
                vacuumPlasmaResource.maxAmount = vacuumPlasmaResourceAmount;
                part.RequestResource(KITResourceSettings.VacuumPlasma, -vacuumPlasmaResource.maxAmount);
            }

            // Debug.Log($"[testing] currentThrustInSpace is {currentThrustInSpace}, simulatedThrustInSpace is {simulatedThrustInSpace}, effectiveMaximumPower is {effectiveMaximumPower}, ModifiedThrottle is {ModifiedThrottle}, calculated_thrust is {calculated_thrust}, and simulated_max_thrust is {simulated_max_thrust}");

        }

        public string KITPartName() => $"{part.partInfo.title}{(CurrentPropellant != null ? " (" + CurrentPropellant.PropellantGUIName + ")" : "")}";
    }
}
