﻿using KIT.Extensions;
using KIT.External;
using KIT.Resources;
using KIT.ResourceScheduler;
using KSP.Localization;
using System;
using System.Collections.Generic;
using TweakScale;
using UnityEngine;

namespace KIT.Propulsion
{
    [KSPModule("Fission Engine")]
    class FissionEngineController : DaedalusEngineController { }

    [KSPModule("Confinement Fusion Engine")]
    class FusionEngineController : DaedalusEngineController { }

    [KSPModule("Confinement Fusion Engine")]
    class DaedalusEngineController : PartModule, IKITModule, IUpgradeableModule , IRescalable<DaedalusEngineController>
    {
        const string LightBlue = "<color=#7fdfffff>";
        const string Group = "FusionEngine";
        const string GroupTitle = "#LOC_KSPIE_FusionEngine_groupName";

        // Persistent
        [KSPField(isPersistant = true)] public double thrustMultiplier = 1;
        [KSPField(isPersistant = true)] public double ispMultiplier = 1;
        [KSPField(isPersistant = true)] public bool IsEnabled;
        [KSPField(isPersistant = true)] public bool radiationSafetyFeatures = true;

        [KSPField] public double massThrustExp;
        [KSPField] public double massIspExp;
        [KSPField] public double higherScaleThrustExponent = 3;
        [KSPField] public double lowerScaleThrustExponent = 4;
        [KSPField] public double higherScaleIspExponent = 0.25;
        [KSPField] public double lowerScaleIspExponent = 1;
        [KSPField] public double GThreshold = 15;

        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_FusionEngine_speedLimit", guiUnits = "c"), UI_FloatRange(stepIncrement = 0.005f, maxValue = 1, minValue = 0.005f)]
        public float speedLimit = 1;
        [KSPField(groupName = Group, isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_FusionEngine_fuelLimit", guiUnits = "%"), UI_FloatRange(stepIncrement = 0.5f, maxValue = 100, minValue = 0.5f)]
        public float fuelLimit = 100;
        [KSPField(groupName = Group, isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_maximizeThrust"), UI_Toggle(disabledText = "Off", enabledText = "On")]
        public bool maximizeThrust = true;
        [KSPField(groupName = Group, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_FusionEngine_powerUsage")]
        public string powerUsage;
        [KSPField(groupName = Group, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_FusionEngine_wasteHeat", guiFormat = "F2", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit")]
        public double wasteHeat;

        [KSPField] public double finalRequestedPower;
        [KSPField] public string fusionFuel1 = string.Empty;
        [KSPField] public string fusionFuel2 = string.Empty;
        [KSPField] public string fusionFuel3 = string.Empty;

        [KSPField] public string fuelName1 = "FusionPellets";
        [KSPField] public string fuelName2 = string.Empty;
        [KSPField] public string fuelName3 = string.Empty;

        [KSPField] public double fuelRatio1 = 1;
        [KSPField] public double fuelRatio2;
        [KSPField] public double fuelRatio3;
        [KSPField] public string effectName = string.Empty;

        [KSPField(groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_temperatureStr")]
        public string temperatureStr = "";
        [KSPField(groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_speedOfLight", guiUnits = " m/s")]
        public double engineSpeedOfLight;
        [KSPField(groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_lightSpeedRatio", guiFormat = "F9", guiUnits = "c")]
        public double lightSpeedRatio;
        [KSPField(groupName = Group, guiActive = false, guiName = "#LOC_KSPIE_FusionEngine_relativity", guiFormat = "F10")]
        public double relativity;
        [KSPField(groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_timeDilation", guiFormat = "F10")]
        public double timeDilation;
        [KSPField(groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_radhazardstr")]
        public string radiationHazardString = "";
        [KSPField(groupName = Group, guiActiveEditor = true, guiName = "#LOC_KSPIE_FusionEngine_partMass", guiFormat = "F3", guiUnits = " t")]
        public float partMass = 1;
        [KSPField(groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_fusionRatio", guiFormat = "F3")]
        public double fusionRatio;
        [KSPField(groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_fuelAmountsCurrent", guiFormat = "F3")]
        public double fuelAmounts;
        [KSPField(groupName = Group, guiActive = false, guiName = "#LOC_KSPIE_FusionEngine_fuelAmountsMax", guiFormat = "F3")]
        public double fuelAmountsMax;
        [KSPField(groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_fuelAmountsRatio")]
        public string fuelAmountsRatio;
        [KSPField(groupName = Group, guiActive = false, guiName = "#LOC_KSPIE_FusionEngine_thrustPowerInTeraWatt", guiFormat = "F2", guiUnits = " TW")]
        public double thrustPowerInTeraWatt;
        [KSPField(groupName = Group, guiActive = false, guiName = "#LOC_KSPIE_FusionEngine_calculatedFuelflow", guiFormat = "F6", guiUnits = " U")]
        public double calculatedFuelflow;
        [KSPField(groupName = Group, guiActive = false, guiName = "#LOC_KSPIE_FusionEngine_massFlowRateKgPerSecond", guiFormat = "F6", guiUnits = " kg/s")]
        public double massFlowRateKgPerSecond;
        [KSPField(groupName = Group, guiActive = false, guiName = "#LOC_KSPIE_FusionEngine_massFlowRateTonPerHour", guiFormat = "F6", guiUnits = " t/h")]
        public double massFlowRateTonPerHour;
        [KSPField(groupName = Group, guiActive = false, guiName = "#LOC_KSPIE_FusionEngine_storedThrotle")]
        public float storedThrotle;
        [KSPField(groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_effectiveMaxThrustInKiloNewton", guiFormat = "F2", guiUnits = " kN")]
        public double effectiveMaxThrustInKiloNewton;
        [KSPField(groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_effectiveIsp", guiFormat = "F1", guiUnits = "s")]
        public double effectiveIsp;
        [KSPField(groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_FusionEngine_worldSpaceVelocity", guiFormat = "F2", guiUnits = " m/s")]
        public double worldSpaceVelocity;

        [KSPField(groupName = Group, guiName = "#LOC_KSPIE_DaedalusEngineController_UpgradeTech")]public string translatedTechMk1;
        [KSPField(groupName = Group, guiName = "#LOC_KSPIE_DaedalusEngineController_UpgradeTech")]public string translatedTechMk2;
        [KSPField(groupName = Group, guiName = "#LOC_KSPIE_DaedalusEngineController_UpgradeTech")]public string translatedTechMk3;
        [KSPField(groupName = Group, guiName = "#LOC_KSPIE_DaedalusEngineController_UpgradeTech")]public string translatedTechMk4;
        [KSPField(groupName = Group, guiName = "#LOC_KSPIE_DaedalusEngineController_UpgradeTech")]public string translatedTechMk5;
        [KSPField(groupName = Group, guiName = "#LOC_KSPIE_DaedalusEngineController_UpgradeTech")]public string translatedTechMk6;
        [KSPField(groupName = Group, guiName = "#LOC_KSPIE_DaedalusEngineController_UpgradeTech")]public string translatedTechMk7;
        [KSPField(groupName = Group, guiName = "#LOC_KSPIE_DaedalusEngineController_UpgradeTech")]public string translatedTechMk8;

        [KSPField] public float maxThrustMk1 = 300;
        [KSPField] public float maxThrustMk2 = 500;
        [KSPField] public float maxThrustMk3 = 800;
        [KSPField] public float maxThrustMk4 = 1200;
        [KSPField] public float maxThrustMk5 = 1500;
        [KSPField] public float maxThrustMk6 = 2000;
        [KSPField] public float maxThrustMk7 = 2500;
        [KSPField] public float maxThrustMk8 = 3000;
        [KSPField] public float maxThrustMk9 = 3500;

        [KSPField] public float wasteheatMk1;
        [KSPField] public float wasteheatMk2;
        [KSPField] public float wasteheatMk3;
        [KSPField] public float wasteheatMk4;
        [KSPField] public float wasteheatMk5;
        [KSPField] public float wasteheatMk6;
        [KSPField] public float wasteheatMk7;
        [KSPField] public float wasteheatMk8;
        [KSPField] public float wasteheatMk9;

        [KSPField] public double powerRequirementMk1;
        [KSPField] public double powerRequirementMk2;
        [KSPField] public double powerRequirementMk3;
        [KSPField] public double powerRequirementMk4;
        [KSPField] public double powerRequirementMk5;
        [KSPField] public double powerRequirementMk6;
        [KSPField] public double powerRequirementMk7;
        [KSPField] public double powerRequirementMk8;
        [KSPField] public double powerRequirementMk9;

        [KSPField] public double powerProductionMk1;
        [KSPField] public double powerProductionMk2;
        [KSPField] public double powerProductionMk3;
        [KSPField] public double powerProductionMk4;
        [KSPField] public double powerProductionMk5;
        [KSPField] public double powerProductionMk6;
        [KSPField] public double powerProductionMk7;
        [KSPField] public double powerProductionMk8;
        [KSPField] public double powerProductionMk9;

        [KSPField] public double thrustIspMk1 = 83886;
        [KSPField] public double thrustIspMk2 = 104857;
        [KSPField] public double thrustIspMk3 = 131072;
        [KSPField] public double thrustIspMk4 = 163840;
        [KSPField] public double thrustIspMk5 = 204800;
        [KSPField] public double thrustIspMk6 = 256000;
        [KSPField] public double thrustIspMk7 = 320000;
        [KSPField] public double thrustIspMk8 = 400000;
        [KSPField] public double thrustIspMk9 = 500000;

        [KSPField] public int powerPriority = 4;
        [KSPField] public int numberOfAvailableUpgradeTechs;

        [KSPField] public float throttle;
        [KSPField] public float maxAtmosphereDensity;
        [KSPField] public float lethalDistance = 2000;
        [KSPField] public float killDivider = 50;
        [KSPField] public float wasteHeatMultiplier = 1;
        [KSPField] public float powerRequirementMultiplier = 1;
        [KSPField] public float maxTemp = 3200;

        [KSPField] public double demandMass;
        [KSPField] public double fuelRatio;
        [KSPField] public double averageDensity;
        [KSPField] public double ispThrottleExponent = 0.5;
        [KSPField] public double fuelNeutronsFraction = 0.005;
        [KSPField] public double ratioHeadingVersusRequest;

        [KSPField] public string originalName = Localizer.Format("#LOC_KSPIE_DaedalusEngineController_originalName");//"Prototype Daedalus IC Fusion Engine"
        [KSPField] public string upgradedName = Localizer.Format("#LOC_KSPIE_DaedalusEngineController_upgradedName");//"Daedalus IC Fusion Engine"

        [KSPField] public string upgradeTechReq1;
        [KSPField] public string upgradeTechReq2;
        [KSPField] public string upgradeTechReq3;
        [KSPField] public string upgradeTechReq4;
        [KSPField] public string upgradeTechReq5;
        [KSPField] public string upgradeTechReq6;
        [KSPField] public string upgradeTechReq7;
        [KSPField] public string upgradeTechReq8;

        [KSPField] public double fuelFactor1;
        [KSPField] public double fuelFactor2;
        [KSPField] public double fuelFactor3;

        [KSPField] public double fusionFuelRequestAmount1;
        [KSPField] public double fusionFuelRequestAmount2;
        [KSPField] public double fusionFuelRequestAmount3;

        FNEmitterController _emitterController;
        ModuleEngines _curEngineT;
        BaseEvent _deactivateRadSafetyEvent;
        BaseEvent _activateRadSafetyEvent;
        BaseField _radHazardStrField;

        PartResourceDefinition _fuelResourceDefinition1;
        PartResourceDefinition _fuelResourceDefinition2;
        PartResourceDefinition _fuelResourceDefinition3;

        ResourceName _fuelResourceID1;
        ResourceName _fuelResourceID2;
        ResourceName _fuelResourceID3;

        private bool _radHazard;
        private bool _warpToReal;
        private double _engineIsp;
        private double _universalTime;
        private double _percentageFuelRemaining;
        private int _vesselChangedSioCountdown;

        private int _engineGenerationType;

        public GenerationType EngineGenerationType
        {
            get => (GenerationType) _engineGenerationType;
            private set => _engineGenerationType = (int) value;
        }

        [KSPEvent(groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_DaedalusEngineController_DeactivateRadSafety", active = true)]//Disable Radiation Safety
        public void DeactivateRadSafety()
        {
            radiationSafetyFeatures = false;
        }

        [KSPEvent(groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_DaedalusEngineController_ActivateRadSafety", active = false)]//Activate Radiation Safety
        public void ActivateRadSafety()
        {
            radiationSafetyFeatures = true;
        }

        public void VesselChangedSOI()
        {
            _vesselChangedSioCountdown = 10;
        }

        #region IUpgradeableModule

        public string UpgradeTechnology => upgradeTechReq1;

        private float RawMaximumThrust
        {
            get
            {
                switch (_engineGenerationType)
                {
                    case (int)GenerationType.Mk1: return maxThrustMk1;
                    case (int)GenerationType.Mk2: return maxThrustMk2;
                    case (int)GenerationType.Mk3: return maxThrustMk3;
                    case (int)GenerationType.Mk4: return maxThrustMk4;
                    case (int)GenerationType.Mk5: return maxThrustMk5;
                    case (int)GenerationType.Mk6: return maxThrustMk6;
                    case (int)GenerationType.Mk7: return maxThrustMk7;
                    case (int)GenerationType.Mk8: return maxThrustMk8;
                    default:
                        return maxThrustMk9;
                }
            }
        }

        private double MaximumThrust => RawMaximumThrust * thrustMultiplier * Math.Pow(part.mass / partMass, massThrustExp);

        private float FusionWasteHeat
        {
            get
            {
                switch (_engineGenerationType)
                {
                    case (int)GenerationType.Mk1: return wasteheatMk1;
                    case (int)GenerationType.Mk2: return wasteheatMk2;
                    case (int)GenerationType.Mk3: return wasteheatMk3;
                    case (int)GenerationType.Mk4: return wasteheatMk4;
                    case (int)GenerationType.Mk5: return wasteheatMk5;
                    case (int)GenerationType.Mk6: return wasteheatMk6;
                    case (int)GenerationType.Mk7: return wasteheatMk7;
                    case (int)GenerationType.Mk8: return wasteheatMk8;
                    default:
                        return maxThrustMk9;
                }
            }
        }

        public double PowerRequirement
        {
            get
            {
                switch (_engineGenerationType)
                {
                    case (int)GenerationType.Mk1: return powerRequirementMk1;
                    case (int)GenerationType.Mk2: return powerRequirementMk2;
                    case (int)GenerationType.Mk3: return powerRequirementMk3;
                    case (int)GenerationType.Mk4: return powerRequirementMk4;
                    case (int)GenerationType.Mk5: return powerRequirementMk5;
                    case (int)GenerationType.Mk6: return powerRequirementMk6;
                    case (int)GenerationType.Mk7: return powerRequirementMk7;
                    case (int)GenerationType.Mk8: return powerRequirementMk8;
                    default:
                        return powerRequirementMk9;
                }
            }
        }

        public double PowerProduction
        {
            get
            {
                switch (_engineGenerationType)
                {
                    case (int)GenerationType.Mk1: return powerProductionMk1;
                    case (int)GenerationType.Mk2: return powerProductionMk2;
                    case (int)GenerationType.Mk3: return powerProductionMk3;
                    case (int)GenerationType.Mk4: return powerProductionMk4;
                    case (int)GenerationType.Mk5: return powerProductionMk5;
                    case (int)GenerationType.Mk6: return powerProductionMk6;
                    case (int)GenerationType.Mk7: return powerProductionMk7;
                    case (int)GenerationType.Mk8: return powerProductionMk8;
                    default:
                        return powerProductionMk9;
                }
            }
        }

        public double RawEngineIsp
        {
            get
            {
                switch (_engineGenerationType)
                {
                    case (int)GenerationType.Mk1: return thrustIspMk1;
                    case (int)GenerationType.Mk2: return thrustIspMk2;
                    case (int)GenerationType.Mk3: return thrustIspMk3;
                    case (int)GenerationType.Mk4: return thrustIspMk4;
                    case (int)GenerationType.Mk5: return thrustIspMk5;
                    case (int)GenerationType.Mk6: return thrustIspMk6;
                    case (int)GenerationType.Mk7: return thrustIspMk7;
                    case (int)GenerationType.Mk8: return thrustIspMk8;
                    default:
                        return thrustIspMk9;
                }
            }
        }

        public double EngineIsp => RawEngineIsp * ispMultiplier * Math.Pow(part.mass / partMass, massIspExp);

        private double EffectiveMaxPowerRequirement => PowerRequirement * powerRequirementMultiplier;

        private double EffectiveMaxPowerProduction => PowerProduction * powerRequirementMultiplier;

        private double EffectiveMaxFusionWasteHeat => FusionWasteHeat * wasteHeatMultiplier;


        public void upgradePartModule()
        {
            //isUpgraded = true;
        }

        #endregion

        public override void OnStart(StartState state)
        {
            // string[] resources_to_supply = { ResourceSettings.Config.WasteHeatInMegawatt, ResourceSettings.Config.ElectricPowerInMegawatt };
            //this.resources_to_supply = resources_to_supply;
            base.OnStart(state);

            engineSpeedOfLight = PluginSettings.Config.SpeedOfLight;

            UpdateFuelFactors();

            part.maxTemp = maxTemp;
            part.thermalMass = 1;
            part.thermalMassModifier = 1;

            _curEngineT = part.FindModuleImplementing<ModuleEngines>();

            if (_curEngineT == null) return;

            DetermineTechLevel();

            _engineIsp = EngineIsp;

            // bind with fields and events
            _deactivateRadSafetyEvent = Events[nameof(DeactivateRadSafety)];
            _activateRadSafetyEvent = Events[nameof(ActivateRadSafety)];
            _radHazardStrField = Fields[nameof(radiationHazardString)];

            translatedTechMk1 = PluginHelper.DisplayTech(upgradeTechReq1);
            translatedTechMk2 = PluginHelper.DisplayTech(upgradeTechReq2);
            translatedTechMk3 = PluginHelper.DisplayTech(upgradeTechReq3);
            translatedTechMk4 = PluginHelper.DisplayTech(upgradeTechReq4);
            translatedTechMk5 = PluginHelper.DisplayTech(upgradeTechReq5);
            translatedTechMk6 = PluginHelper.DisplayTech(upgradeTechReq6);
            translatedTechMk7 = PluginHelper.DisplayTech(upgradeTechReq7);
            translatedTechMk8 = PluginHelper.DisplayTech(upgradeTechReq8);

            InitializeKerbalismEmitter();
        }

        private void InitializeKerbalismEmitter()
        {
            if (!Kerbalism.IsLoaded)
                return;

            _emitterController = part.FindModuleImplementing<FNEmitterController>();

            if (_emitterController == null)
                Debug.LogWarning("[KSPI]: No Emitter Found om " + part.partInfo.title);
        }

        private void UpdateKerbalismEmitter()
        {
            if (_emitterController == null)
                return;

            _emitterController.reactorActivityFraction = fusionRatio;
            _emitterController.exhaustActivityFraction = fusionRatio;
            _emitterController.fuelNeutronsFraction = fuelNeutronsFraction;
        }

        private void UpdateFuelFactors()
        {
            _fuelResourceID1 = _fuelResourceID2 = _fuelResourceID3 = 0;

            if (!string.IsNullOrEmpty(fuelName1))
                _fuelResourceDefinition1 = PartResourceLibrary.Instance.GetDefinition(fuelName1);
            else if (!string.IsNullOrEmpty(fusionFuel1))
                _fuelResourceDefinition1 = PartResourceLibrary.Instance.GetDefinition(fusionFuel1);

            if (!string.IsNullOrEmpty(fuelName2))
                _fuelResourceDefinition2 = PartResourceLibrary.Instance.GetDefinition(fuelName2);
            else if (!string.IsNullOrEmpty(fusionFuel2))
                _fuelResourceDefinition2 = PartResourceLibrary.Instance.GetDefinition(fusionFuel2);

            if (!string.IsNullOrEmpty(fuelName3))
                _fuelResourceDefinition3 = PartResourceLibrary.Instance.GetDefinition(fuelName3);
            else if (!string.IsNullOrEmpty(fusionFuel3))
                _fuelResourceDefinition3 = PartResourceLibrary.Instance.GetDefinition(fusionFuel3);

            var ratioSum = 0.0;
            var densitySum = 0.0;

            if (_fuelResourceDefinition1 != null)
            {
                _fuelResourceID1 = KITResourceSettings.NameToResource(_fuelResourceDefinition1.name);
                ratioSum += fuelRatio1;
                densitySum += _fuelResourceDefinition1.density * fuelRatio1;
            }
            if (_fuelResourceDefinition2 != null)
            {
                _fuelResourceID2 = KITResourceSettings.NameToResource(_fuelResourceDefinition2.name);
                ratioSum += fuelRatio2;
                densitySum += _fuelResourceDefinition2.density * fuelRatio2;
            }
            if (_fuelResourceDefinition3 != null)
            {
                _fuelResourceID3 = KITResourceSettings.NameToResource(_fuelResourceDefinition3.name);
                ratioSum += fuelRatio3;
                densitySum += _fuelResourceDefinition3.density * fuelRatio3;
            }

            averageDensity = densitySum / ratioSum;

            fuelFactor1 = _fuelResourceDefinition1 != null ? fuelRatio1/ratioSum : 0;
            fuelFactor2 = _fuelResourceDefinition2 != null ? fuelRatio2/ratioSum : 0;
            fuelFactor3 = _fuelResourceDefinition3 != null ? fuelRatio3/ratioSum : 0;
        }

        private void DetermineTechLevel()
        {
            numberOfAvailableUpgradeTechs = 0;
            if (PluginHelper.UpgradeAvailable(upgradeTechReq1))
                numberOfAvailableUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(upgradeTechReq2))
                numberOfAvailableUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(upgradeTechReq3))
                numberOfAvailableUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(upgradeTechReq4))
                numberOfAvailableUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(upgradeTechReq5))
                numberOfAvailableUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(upgradeTechReq6))
                numberOfAvailableUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(upgradeTechReq7))
                numberOfAvailableUpgradeTechs++;
            if (PluginHelper.UpgradeAvailable(upgradeTechReq8))
                numberOfAvailableUpgradeTechs++;

            EngineGenerationType = (GenerationType) numberOfAvailableUpgradeTechs;
        }

        public void Update()
        {
            var wasteheatPartResource = part.Resources[KITResourceSettings.WasteHeat];
            if (wasteheatPartResource != null)
            {
                var localWasteheatRatio = wasteheatPartResource.amount / wasteheatPartResource.maxAmount;
                wasteheatPartResource.maxAmount = 1000 * partMass * wasteHeatMultiplier;
                wasteheatPartResource.amount = wasteheatPartResource.maxAmount * localWasteheatRatio;
            }

            if (HighLogic.LoadedSceneIsEditor)
            {
                // configure engine for Kerbal Engineer support
                UpdateAtmosphericCurve(EngineIsp);
                effectiveMaxThrustInKiloNewton = MaximumThrust;
                calculatedFuelflow = effectiveMaxThrustInKiloNewton / EngineIsp / PhysicsGlobals.GravitationalAcceleration;
                _curEngineT.maxFuelFlow = (float)calculatedFuelflow;
                _curEngineT.maxThrust = (float)effectiveMaxThrustInKiloNewton;
                powerUsage = EffectiveMaxPowerRequirement.ToString("0.00") + Localizer.Format("#LOC_KSPIE_Reactor_megawattUnit");
                wasteHeat = EffectiveMaxFusionWasteHeat;
            }
            else
            {
                part.GetConnectedResourceTotals(_fuelResourceDefinition1.id, out fuelAmounts, out fuelAmountsMax);

                _percentageFuelRemaining = fuelAmountsMax > 0 ? fuelAmounts / fuelAmountsMax * 100 : 0;
                fuelAmountsRatio = _percentageFuelRemaining.ToString("0.000") + "% ";
            }
        }

        private string FormatThrustStatistics(double value, double isp, string color = null, string format = "F0")
        {
            var result = value.ToString(format) + " kN @ " + isp.ToString(format) + "s";

            if (string.IsNullOrEmpty(color))
                return result;

            return "<color=" + color + ">" + result + "</color>";
        }

        private string FormatPowerStatistics(double powerRequirement, double wasteheat, string color = null, string format = "F0")
        {
            var result = (powerRequirement * powerRequirementMultiplier).ToString(format) + " MWe / " + wasteheat.ToString(format) + " MJ";

            if (string.IsNullOrEmpty(color))
                return result;

            return "<color=" + color + ">" + result + "</color>";
        }

        // Note: we assume OnRescale is called at load and after any time tweak scale changes the size of an part
        public void OnRescale(ScalingFactor factor)
        {
            Debug.Log("[KSPI]: DaedalusEngineController OnRescale was called with factor " + factor.absolute.linear);

            var storedAbsoluteFactor = (double)(decimal)factor.absolute.linear;

            thrustMultiplier = storedAbsoluteFactor >= 1 ? Math.Pow(storedAbsoluteFactor, higherScaleThrustExponent) : Math.Pow(storedAbsoluteFactor, lowerScaleThrustExponent);
            ispMultiplier = storedAbsoluteFactor >= 1 ? Math.Pow(storedAbsoluteFactor, higherScaleIspExponent) : Math.Pow(storedAbsoluteFactor, lowerScaleIspExponent);
        }

        public override void OnUpdate()
        {
            // stop engines and drop out of timewarp when X pressed
            if (vessel.packed && storedThrotle > 0 && Input.GetKeyDown(KeyCode.X))
            {
                // Return to realtime
                TimeWarp.SetRate(0, true);

                storedThrotle = 0;
                vessel.ctrlState.mainThrottle = storedThrotle;
            }

            if (_curEngineT == null) return;

            // When transitioning from timewarp to real update radiationRatio
            if (_warpToReal)
            {
                vessel.ctrlState.mainThrottle = storedThrotle;
                _warpToReal = false;
            }

            _deactivateRadSafetyEvent.active = radiationSafetyFeatures;
            _activateRadSafetyEvent.active = !radiationSafetyFeatures;

            if (_curEngineT.isOperational && !IsEnabled)
            {
                IsEnabled = true;
                Debug.Log("[KSPI]: DaedalusEngineController on " + part.name + " was Force Activated");
                part.force_activate();
            }

            _radHazard = false;

            if (!HighLogic.CurrentGame.Parameters.CustomParams<KITGamePlayParams>().AllowDestructiveEngines)
            {
                var kerbalHazardCount = 0;
                foreach (var currentVessel in FlightGlobals.Vessels)
                {
                    var distance = Vector3d.Distance(vessel.transform.position, currentVessel.transform.position);
                    if (distance < lethalDistance && currentVessel != vessel)
                        kerbalHazardCount += currentVessel.GetCrewCount();
                }

                if (kerbalHazardCount > 0)
                {
                    _radHazard = true;
                    radiationHazardString = Localizer.Format(kerbalHazardCount > 1
                        ? "#LOC_KSPIE_DaedalusEngineController_radhazardstr2"
                        : "#LOC_KSPIE_DaedalusEngineController_radhazardstr1", kerbalHazardCount);

                    _radHazardStrField.guiActive = true;
                }
            }

            if (_radHazard == false)
            {
                _radHazardStrField.guiActive = false;
                _radHazard = false;
                radiationHazardString = Localizer.Format("#LOC_KSPIE_DaedalusEngineController_radhazardstr3");//"None."
            }

            Fields[nameof(powerUsage)].guiActive = EffectiveMaxPowerRequirement > 0;
            Fields[nameof(wasteHeat)].guiActive = EffectiveMaxFusionWasteHeat > 0;
        }

        private void ShutDown(string reason)
        {
            _curEngineT.Events[nameof(ModuleEnginesFX.Shutdown)].Invoke();
            _curEngineT.currentThrottle = 0;
            _curEngineT.requestedThrottle = 0;

            ScreenMessages.PostScreenMessage(reason, 5.0f, ScreenMessageStyle.UPPER_CENTER);
            foreach (var fxGroup in part.fxGroups)
            {
                fxGroup.setActive(false);
            }
        }

        private void CalculateTimeDilation()
        {
            worldSpaceVelocity = vessel.orbit.GetFrameVel().magnitude;

            lightSpeedRatio = Math.Min(worldSpaceVelocity / engineSpeedOfLight, 0.9999999999);

            timeDilation = Math.Sqrt(1 - (lightSpeedRatio * lightSpeedRatio));

            relativity = 1 / timeDilation;
        }

        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsEditor)
                return;

            temperatureStr = part.temperature.ToString("0.0") + "K / " + part.maxTemp.ToString("0.0") + "K";

            if (IsEnabled) return;

            if (!string.IsNullOrEmpty(effectName))
                part.Effect(effectName, 0, -1);

            UpdateTime();
        }

        private void UpdateTime()
        {
            _universalTime = Planetarium.GetUniversalTime();
            CalculateTimeDilation();
        }

        private void UpdateAtmosphericCurve(double isp)
        {
            var newAtmosphereCurve = new FloatCurve();
            newAtmosphereCurve.Add(0, (float)isp);
            newAtmosphereCurve.Add(maxAtmosphereDensity, 0);
            _curEngineT.atmosphereCurve = newAtmosphereCurve;
        }

        private void PersistentThrust(IResourceManager resMan, double modifiedFixedDeltaTime, double modifiedUniversalTime, Vector3d thrustVector, double vesselMass)
        {
            ratioHeadingVersusRequest = vessel.PersistHeading(_vesselChangedSioCountdown > 0, ratioHeadingVersusRequest == 1);
            if (ratioHeadingVersusRequest != 1)
            {
                Debug.Log("[KSPI]: " + "quit persistent heading: " + ratioHeadingVersusRequest);
                return;
            }

            var timeDilationMaximumThrust = timeDilation * timeDilation * MaximumThrust * (maximizeThrust ? 1 : storedThrotle);

            var deltaVv = PluginHelper.CalculateDeltaVV(thrustVector, vesselMass, modifiedFixedDeltaTime, timeDilationMaximumThrust * fusionRatio, timeDilation * _engineIsp, out demandMass);

            double persistentThrustDot = Vector3d.Dot(part.transform.up, vessel.obt_velocity);
            if (persistentThrustDot < 0 && (vessel.obt_velocity.magnitude <= deltaVv.magnitude * 2))
            {
                var message = Localizer.Format("#LOC_KSPIE_DaedalusEngineController_PostMsg4");//"Thrust warp stopped - orbital speed too low"
                ScreenMessages.PostScreenMessage(message, 5, ScreenMessageStyle.UPPER_CENTER);
                Debug.Log("[KSPI]: " + message);
                TimeWarp.SetRate(0, true);
                return;
            }

            fuelRatio = CollectFuel(resMan, demandMass);

            effectiveMaxThrustInKiloNewton = timeDilationMaximumThrust * fuelRatio;

            if (fuelRatio <= 0)
                return;

            vessel.orbit.Perturb(deltaVv * fuelRatio, modifiedUniversalTime);
        }

        private double CollectFuel(IResourceManager resMan, double mass)
        {
            if (CheatOptions.InfinitePropellant || mass <= 0)
                return 1;

            fusionFuelRequestAmount1 = 0.0;
            fusionFuelRequestAmount2 = 0.0;
            fusionFuelRequestAmount3 = 0.0;

            var totalAmount = mass / averageDensity;

            double availableRatio = 1;
            if (fuelFactor1 > 0)
            {
                fusionFuelRequestAmount1 = fuelFactor1 * totalAmount;
                availableRatio = Math.Min(resMan.CurrentCapacity(_fuelResourceID1) / fusionFuelRequestAmount1, availableRatio);
            }
            if (fuelFactor2 > 0)
            {
                fusionFuelRequestAmount2 = fuelFactor2 * totalAmount;
                availableRatio = Math.Min(resMan.CurrentCapacity(_fuelResourceID2) / fusionFuelRequestAmount2, availableRatio);
            }
            if (fuelFactor3 > 0)
            {
                fusionFuelRequestAmount3 = fuelFactor3 * totalAmount;
                availableRatio = Math.Min(resMan.CurrentCapacity(_fuelResourceID3) / fusionFuelRequestAmount3, availableRatio);
            }

            if (availableRatio <= float.Epsilon)
                return 0;

            double receivedRatio = 1;
            if (fuelFactor1 > 0)
            {
                var receivedFusionFuel = resMan.Consume(_fuelResourceID1, fusionFuelRequestAmount1 * availableRatio);
                receivedRatio = Math.Min(receivedRatio, fusionFuelRequestAmount1 > 0 ? receivedFusionFuel / fusionFuelRequestAmount1 : 0);
            }
            if (fuelFactor2 > 0)
            {
                var receivedFusionFuel = resMan.Consume(_fuelResourceID2, fusionFuelRequestAmount2 * availableRatio);
                receivedRatio = Math.Min(receivedRatio, fusionFuelRequestAmount2 > 0 ? receivedFusionFuel / fusionFuelRequestAmount2 : 0);
            }
            if (fuelFactor3 > 0)
            {
                var receivedFusionFuel = resMan.Consume(_fuelResourceID3, fusionFuelRequestAmount3 * availableRatio);
                receivedRatio = Math.Min(receivedRatio, fusionFuelRequestAmount3 > 0 ? receivedFusionFuel / fusionFuelRequestAmount3 : 0);
            }
            return receivedRatio;
        }

        private double ProcessPowerAndWasteHeat(IResourceManager resMan, float requestedThrottle)
        {
            // Calculate Fusion Ratio
            var effectiveMaxPowerRequirement = EffectiveMaxPowerRequirement;
            var effectiveMaxPowerProduction = EffectiveMaxPowerProduction;
            var effectiveMaxFusionWasteHeat = EffectiveMaxFusionWasteHeat;

            var wasteheatRatio = resMan.FillFraction(ResourceName.WasteHeat);

            var wasteheatModifier = CheatOptions.IgnoreMaxTemperature || wasteheatRatio < 0.9 ? 1 : (1  - wasteheatRatio) * 10;

            var requestedPower = requestedThrottle * effectiveMaxPowerRequirement * wasteheatModifier;

            finalRequestedPower = requestedPower * wasteheatModifier;

            var receivedPower = resMan.Consume(ResourceName.ElectricCharge, finalRequestedPower);

            var plasmaRatio = !requestedPower.IsInfinityOrNaNorZero() && !receivedPower.IsInfinityOrNaNorZero() ? Math.Min(1, receivedPower / requestedPower) : 0;

            powerUsage = receivedPower.ToString("F2") + Localizer.Format("#LOC_KSPIE_Reactor_megawattUnit") + " / " + requestedPower.ToString("F2") + Localizer.Format("#LOC_KSPIE_Reactor_megawattUnit");

            // The Absorbed wasteheat from Fusion production and reaction
            wasteHeat = requestedThrottle * plasmaRatio * effectiveMaxFusionWasteHeat;
            if (effectiveMaxFusionWasteHeat > 0)
                resMan.Produce(ResourceName.WasteHeat, wasteHeat);

            if (effectiveMaxPowerProduction > 0)
                resMan.Produce(ResourceName.ElectricCharge, requestedThrottle * plasmaRatio * effectiveMaxPowerProduction);

            return plasmaRatio;
        }

        private void KillKerbalsWithRadiation(float radiationRatio)
        {
            if (!_radHazard || radiationRatio <= 0 || radiationSafetyFeatures) return;

            var vesselsToRemove = new List<Vessel>();
            var crewToRemove = new List<ProtoCrewMember>();

            foreach (var currentVessel in FlightGlobals.Vessels)
            {
                var distance = Vector3d.Distance(vessel.transform.position, currentVessel.transform.position);

                if (distance >= lethalDistance || currentVessel == vessel || currentVessel.GetCrewCount() <= 0) continue;

                var invSqDist = distance / killDivider;
                var invSqMult = 1 / invSqDist / invSqDist;

                foreach (var crewMember in currentVessel.GetVesselCrew())
                {
                    if (UnityEngine.Random.value < (1 - TimeWarp.fixedDeltaTime * invSqMult)) continue;

                    if (!currentVessel.isEVA)
                    {
                        ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_DaedalusEngineController_PostMsg5", crewMember.name), 5f, ScreenMessageStyle.UPPER_CENTER);// + " was killed by Radiation!"
                        crewToRemove.Add(crewMember);
                    }
                    else
                    {
                        ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_DaedalusEngineController_PostMsg5", crewMember.name), 5f, ScreenMessageStyle.UPPER_CENTER);// + " was killed by Radiation!"
                        vesselsToRemove.Add(currentVessel);
                    }
                }
            }

            foreach (var currentVessel in vesselsToRemove)
            {
                currentVessel.rootPart.Die();
            }

            foreach (var crewMember in crewToRemove)
            {
                var currentVessel = FlightGlobals.Vessels.Find(p => p.GetVesselCrew().Contains(crewMember));
                var partWithCrewMember = currentVessel.Parts.Find(p => p.protoModuleCrew.Contains(crewMember));
                partWithCrewMember.RemoveCrewmember(crewMember);
                crewMember.Die();
            }
        }

        public override string GetInfo()
        {
            var sb = StringBuilderCache.Acquire();
            DetermineTechLevel();

            if (!string.IsNullOrEmpty(upgradeTechReq1))
            {
                sb.Append(LightBlue).Append(Localizer.Format("#LOC_KSPIE_Generic_upgradeTechnologies")).AppendLine(":</color><size=10>");
                sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(upgradeTechReq1)));
                if (!string.IsNullOrEmpty(upgradeTechReq2))
                    sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(upgradeTechReq2)));
                if (!string.IsNullOrEmpty(upgradeTechReq3))
                    sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(upgradeTechReq3)));
                if (!string.IsNullOrEmpty(upgradeTechReq4))
                    sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(upgradeTechReq4)));
                if (!string.IsNullOrEmpty(upgradeTechReq5))
                    sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(upgradeTechReq5)));
                if (!string.IsNullOrEmpty(upgradeTechReq6))
                    sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(upgradeTechReq6)));
                if (!string.IsNullOrEmpty(upgradeTechReq7))
                    sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(upgradeTechReq7)));
                if (!string.IsNullOrEmpty(upgradeTechReq8))
                    sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(upgradeTechReq8)));
                sb.AppendLine("</size>");
            }

            sb.Append(LightBlue).Append(Localizer.Format("#LOC_KSPIE_Generic_EnginePerformance")).AppendLine(":</color><size=10>");
            sb.AppendLine(FormatThrustStatistics(maxThrustMk1, thrustIspMk1));
            if (!string.IsNullOrEmpty(upgradeTechReq1))
                sb.AppendLine(FormatThrustStatistics(maxThrustMk2, thrustIspMk2));
            if (!string.IsNullOrEmpty(upgradeTechReq2))
                sb.AppendLine(FormatThrustStatistics(maxThrustMk3, thrustIspMk3));
            if (!string.IsNullOrEmpty(upgradeTechReq3))
                sb.AppendLine(FormatThrustStatistics(maxThrustMk4, thrustIspMk4));
            if (!string.IsNullOrEmpty(upgradeTechReq4))
                sb.AppendLine(FormatThrustStatistics(maxThrustMk5, thrustIspMk5));
            if (!string.IsNullOrEmpty(upgradeTechReq5))
                sb.AppendLine(FormatThrustStatistics(maxThrustMk6, thrustIspMk6));
            if (!string.IsNullOrEmpty(upgradeTechReq6))
                sb.AppendLine(FormatThrustStatistics(maxThrustMk7, thrustIspMk7));
            if (!string.IsNullOrEmpty(upgradeTechReq7))
                sb.AppendLine(FormatThrustStatistics(maxThrustMk8, thrustIspMk8));
            if (!string.IsNullOrEmpty(upgradeTechReq8))
                sb.AppendLine(FormatThrustStatistics(maxThrustMk9, thrustIspMk9));
            sb.AppendLine("</size>");

            sb.Append(LightBlue).Append(Localizer.Format("#LOC_KSPIE_Generic_PowerRequirementAndWasteheat")).AppendLine(":</color><size=10>");
            sb.AppendLine(FormatPowerStatistics(powerRequirementMk1, wasteheatMk1));
            if (!string.IsNullOrEmpty(upgradeTechReq1))
                sb.AppendLine(FormatPowerStatistics(powerRequirementMk2, wasteheatMk2));
            if (!string.IsNullOrEmpty(upgradeTechReq2))
                sb.AppendLine(FormatPowerStatistics(powerRequirementMk3, wasteheatMk3));
            if (!string.IsNullOrEmpty(upgradeTechReq3))
                sb.AppendLine(FormatPowerStatistics(powerRequirementMk4, wasteheatMk4));
            if (!string.IsNullOrEmpty(upgradeTechReq4))
                sb.AppendLine(FormatPowerStatistics(powerRequirementMk5, wasteheatMk5));
            if (!string.IsNullOrEmpty(upgradeTechReq5))
                sb.AppendLine(FormatPowerStatistics(powerRequirementMk6, wasteheatMk6));
            if (!string.IsNullOrEmpty(upgradeTechReq6))
                sb.AppendLine(FormatPowerStatistics(powerRequirementMk7, wasteheatMk7));
            if (!string.IsNullOrEmpty(upgradeTechReq7))
                sb.AppendLine(FormatPowerStatistics(powerRequirementMk8, wasteheatMk8));
            if (!string.IsNullOrEmpty(upgradeTechReq8))
                sb.AppendLine(FormatPowerStatistics(powerRequirementMk9, wasteheatMk9));
            sb.Append("</size>");

            return sb.ToStringAndRelease();
        }

        public ModuleConfigurationFlags ModuleConfiguration() => ModuleConfigurationFlags.Fifth;
        
        public void KITFixedUpdate(IResourceManager resMan)
        {
            if (_curEngineT == null) return;

            if (_vesselChangedSioCountdown > 0)
                _vesselChangedSioCountdown--;

            UpdateTime();

            throttle = !_curEngineT.getFlameoutState && _curEngineT.currentThrottle > 0 ? Mathf.Max(_curEngineT.currentThrottle, 0.01f) : 0;

            if (throttle > 0)
            {
                if (vessel.atmDensity > maxAtmosphereDensity)
                    ShutDown(Localizer.Format("#LOC_KSPIE_DaedalusEngineController_Shutdownreason1"));//"Inertial Fusion cannot operate in atmosphere!"

                if (_radHazard && radiationSafetyFeatures)
                    ShutDown(Localizer.Format("#LOC_KSPIE_DaedalusEngineController_Shutdownreason2"));//"Engines throttled down as they presently pose a radiation hazard"
            }

            KillKerbalsWithRadiation(throttle);

            if (!vessel.packed && !_warpToReal)
                storedThrotle = vessel.ctrlState.mainThrottle;

            // Update ISP
            effectiveIsp = timeDilation * _engineIsp;

            UpdateAtmosphericCurve(effectiveIsp);

            if (throttle > 0 && !vessel.packed)
            {
                TimeWarp.GThreshold = GThreshold;

                var thrustRatio = Math.Max(_curEngineT.thrustPercentage * 0.01, 0.01);
                var scaledThrottle = Math.Pow(thrustRatio * throttle, ispThrottleExponent);
                effectiveIsp = timeDilation * _engineIsp * scaledThrottle;

                UpdateAtmosphericCurve(effectiveIsp);

                fusionRatio = ProcessPowerAndWasteHeat(resMan, throttle);

                if (!string.IsNullOrEmpty(effectName))
                    part.Effect(effectName, (float)(throttle * fusionRatio));

                // Update FuelFlow
                effectiveMaxThrustInKiloNewton = timeDilation * timeDilation * MaximumThrust;
                calculatedFuelflow = fusionRatio * effectiveMaxThrustInKiloNewton / effectiveIsp / PhysicsGlobals.GravitationalAcceleration;
                massFlowRateKgPerSecond = thrustRatio * _curEngineT.currentThrottle * calculatedFuelflow * 0.001;

                if (!_curEngineT.getFlameoutState && fusionRatio < 0.01)
                {
                    _curEngineT.status = Localizer.Format("#LOC_KSPIE_DaedalusEngineController_curEngineTstatus1");//"Insufficient Electricity"
                }

                ratioHeadingVersusRequest = 0;
            }
            else if (vessel.packed && _curEngineT.currentThrottle > 0 && _curEngineT.getIgnitionState && _curEngineT.enabled && FlightGlobals.ActiveVessel == vessel && throttle > 0 && _percentageFuelRemaining > (100 - fuelLimit) && lightSpeedRatio < speedLimit)
            {
                _warpToReal = true; // Set to true for transition to realtime

                fusionRatio = CheatOptions.InfiniteElectricity
                    ? 1
                    : maximizeThrust
                        ? ProcessPowerAndWasteHeat(resMan, 1)
                        : ProcessPowerAndWasteHeat(resMan, storedThrotle);

                if (fusionRatio <= 0.01)
                {
                    var message = Localizer.Format("#LOC_KSPIE_DaedalusEngineController_PostMsg1");//"Thrust warp stopped - insufficient power"
                    Debug.Log("[KSPI]: " + message);
                    ScreenMessages.PostScreenMessage(message, 5, ScreenMessageStyle.UPPER_CENTER);
                    // Return to realtime
                    TimeWarp.SetRate(0, true);
                }

                effectiveMaxThrustInKiloNewton = timeDilation * timeDilation * MaximumThrust;
                calculatedFuelflow = effectiveIsp > 0 ? fusionRatio * effectiveMaxThrustInKiloNewton / effectiveIsp / PhysicsGlobals.GravitationalAcceleration : 0;
                massFlowRateKgPerSecond = calculatedFuelflow * 0.001;

                var fixedDeltaTime = resMan.FixedDeltaTime();

                if (fixedDeltaTime > 20)
                {
                    var deltaCalculations = Math.Ceiling(fixedDeltaTime * 0.05);
                    var deltaTimeStep = fixedDeltaTime / deltaCalculations;

                    for (var step = 0; step < deltaCalculations; step++)
                    {
                        PersistentThrust(resMan, deltaTimeStep, _universalTime + (step * deltaTimeStep), part.transform.up, vessel.totalMass);
                        CalculateTimeDilation();
                    }
                }
                else
                    PersistentThrust(resMan, fixedDeltaTime, _universalTime, part.transform.up, vessel.totalMass);

                if (fuelRatio < 0.999)
                {
                    var message = (fuelRatio <= 0) ? Localizer.Format("#LOC_KSPIE_DaedalusEngineController_PostMsg2") : Localizer.Format("#LOC_KSPIE_DaedalusEngineController_PostMsg3");//"Thrust warp stopped - propellant depleted" : "Thrust warp stopped - running out of propellant"
                    Debug.Log("[KSPI]: " + message);
                    ScreenMessages.PostScreenMessage(message, 5, ScreenMessageStyle.UPPER_CENTER);
                    // Return to realtime
                    TimeWarp.SetRate(0, true);
                }

                if (!string.IsNullOrEmpty(effectName))
                    part.Effect(effectName, (float)(throttle * fusionRatio));
            }
            else
            {
                ratioHeadingVersusRequest = vessel.PersistHeading(_vesselChangedSioCountdown > 0, ratioHeadingVersusRequest == 1);

                if (!string.IsNullOrEmpty(effectName))
                    part.Effect(effectName, 0, -1);

                powerUsage = "0.00" + Localizer.Format("#LOC_KSPIE_Reactor_megawattUnit") + " / " + EffectiveMaxPowerRequirement.ToString("F2") + Localizer.Format("#LOC_KSPIE_Reactor_megawattUnit");

                if (!(_percentageFuelRemaining > (100 - fuelLimit) || lightSpeedRatio > speedLimit))
                {
                    _warpToReal = false;
                    vessel.ctrlState.mainThrottle = 0;
                }

                effectiveMaxThrustInKiloNewton = timeDilation * timeDilation * MaximumThrust;
                calculatedFuelflow = effectiveMaxThrustInKiloNewton / effectiveIsp / PhysicsGlobals.GravitationalAcceleration;
                massFlowRateKgPerSecond = 0;
                fusionRatio = 0;
            }

            _curEngineT.maxFuelFlow = Mathf.Max((float)calculatedFuelflow, 1e-10f);
            _curEngineT.maxThrust = Mathf.Max((float)effectiveMaxThrustInKiloNewton, 0.0001f);

            massFlowRateTonPerHour = massFlowRateKgPerSecond * 3.6;
            thrustPowerInTeraWatt = effectiveMaxThrustInKiloNewton * 500 * effectiveIsp * PhysicsGlobals.GravitationalAcceleration * 1e-12;

            UpdateKerbalismEmitter();
        }

        public string KITPartName() => part.partInfo.title;
    }
}
