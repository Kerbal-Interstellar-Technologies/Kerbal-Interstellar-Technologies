using KIT.Constants;
using KIT.Extensions;
using KIT.Reactors;
using KIT.Redist;
using KIT.Resources;
using KIT.ResourceScheduler;
using KIT.Wasteheat;
using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using TweakScale;
using UnityEngine;

namespace KIT.Powermanagement
{
    enum PowerStates { PowerOnline, PowerOffline };

    [KSPModule("Thermal Electric Effect Generator")]
    class ThermalElectricEffectGenerator : FNGenerator { }

    [KSPModule("Integrated Thermal Electric Power Generator")]
    class IntegratedThermalElectricPowerGenerator : FNGenerator { }

    [KSPModule("Thermal Electric Power Generator")]
    class ThermalElectricPowerGenerator : FNGenerator { }

    [KSPModule("Integrated Charged Particles Power Generator")]
    class IntegratedChargedParticlesPowerGenerator : FNGenerator { }

    [KSPModule("Charged Particles Power Generator")]
    class ChargedParticlesPowerGenerator : FNGenerator { }

    [KSPModule(" Generator")]
    class FNGenerator : PartModule, IKITMod, IKITVariableSupplier, IUpgradeableModule, IElectricPowerGeneratorSource, IPartMassModifier, IRescalable<FNGenerator>
    {
        public const string GROUP = "FNGenerator";
        public const string GROUP_TITLE = "#LOC_KSPIE_Generator_groupName";

        // Persistent
        [KSPField(isPersistant = true)] public bool IsEnabled = true;
        [KSPField(isPersistant = true)] public bool generatorInit;
        [KSPField(isPersistant = true)] public bool isupgraded;
        [KSPField(isPersistant = true)] public bool chargedParticleMode = false;
        [KSPField(isPersistant = true)] public double storedMassMultiplier;
        [KSPField(isPersistant = true)] public double maximumElectricPower;

        // Settings
        [KSPField] public float powerCapacityMaxValue = 100;
        [KSPField] public float powerCapacityMinValue = 0.5f;
        [KSPField] public float powerCapacityStepIncrement = 0.5f;
        [KSPField] public bool isHighPower = false;
        [KSPField] public bool isMHD = false;
        [KSPField] public bool isLimitedByMinThrottle = false;
        [KSPField] public double powerOutputMultiplier = 1;
        [KSPField] public double hotColdBathRatio;
        [KSPField] public bool calculatedMass = false;

        [KSPField] public double efficiencyMk1;
        [KSPField] public double efficiencyMk2;
        [KSPField] public double efficiencyMk3;
        [KSPField] public double efficiencyMk4;
        [KSPField] public double efficiencyMk5;
        [KSPField] public double efficiencyMk6;
        [KSPField] public double efficiencyMk7;
        [KSPField] public double efficiencyMk8;
        [KSPField] public double efficiencyMk9;

        [KSPField] public string Mk2TechReq = "";
        [KSPField] public string Mk3TechReq = "";
        [KSPField] public string Mk4TechReq = "";
        [KSPField] public string Mk5TechReq = "";
        [KSPField] public string Mk6TechReq = "";
        [KSPField] public string Mk7TechReq = "";
        [KSPField] public string Mk8TechReq = "";
        [KSPField] public string Mk9TechReq = "";

        [KSPField] public string animName = "";
        [KSPField] public string upgradeTechReq = "";
        [KSPField] public float upgradeCost = 1;
        [KSPField] public double wasteHeatMultiplier = 1;
        [KSPField] public bool fullPowerBuffer = false;
        [KSPField] public bool showSpecialisedUI = true;
        [KSPField] public bool showDetailedInfo = true;
        [KSPField] public bool controlWasteHeatBuffer = true;
        [KSPField] public double rawPowerToMassDivider = 1000;
        [KSPField] public double massModifier = 1;
        [KSPField] public double rawMaximumPower;
        [KSPField] public double coreTemperateHotBathExponent = 0.7;
        [KSPField] public double capacityToMassExponent = 0.7;
        [KSPField] public double targetMass;
        [KSPField] public double initialMass;
        [KSPField] public double megajouleBarRatio;
        [KSPField] public double rawThermalPower;
        [KSPField] public double rawChargedPower;
        [KSPField] public double rawReactorPower;
        [KSPField] public double maxThermalPower;
        [KSPField] public double powerRatio;
        [KSPField] public double effectiveMaximumThermalPower;
        [KSPField] public double maxChargedPowerForThermalGenerator;
        [KSPField] public double maxChargedPowerForChargedGenerator;
        [KSPField] public double maxAllowedChargedPower;
        [KSPField] public double maxReactorPower;
        [KSPField] public double potentialThermalPower;
        [KSPField] public double attachedPowerSourceRatio;
        [KSPField] public string upgradeCostStr = "";
        [KSPField] public double coldBathTemp = 500;

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_Generator_powerCapacity"), UI_FloatRange(stepIncrement = 0.5f, maxValue = 100f, minValue = 0.5f)]
        public float powerCapacity = 100;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_Generator_powerControl"), UI_FloatRange(stepIncrement = 0.5f, maxValue = 100f, minValue = 0.5f)]
        public float powerPercentage = 100;
        [KSPField(groupName = GROUP, guiActiveEditor = true, guiName = "#LOC_KSPIE_Generator_maxGeneratorEfficiency", guiFormat = "P1")]
        public double maxEfficiency;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = true, guiFormat = "F2", guiName = "#LOC_KSPIE_Generator_radius", guiUnits = " m")]
        public double radius = 2.5;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_Generator_maxUsageRatio")]
        public double powerUsageEfficiency;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_Generator_maxTheoreticalPower", guiFormat = "F2")]
        public string maximumTheoreticalPower;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_Generator_partMass", guiUnits = " t", guiFormat = "F3")]
        public float partMass;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActive = false, guiName = "#LOC_KSPIE_Generator_currentElectricPower", guiUnits = " MW_e", guiFormat = "F2")]
        public string OutputPower;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActive = false, guiName = "#LOC_KSPIE_Generator_MaximumElectricPower")]//Maximum Electric Power
        public string MaxPowerStr;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActive = true, guiName = "#LOC_KSPIE_Generator_ElectricEfficiency")]//Electric Efficiency
        public string overallEfficiencyStr;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActive = false, guiName = "#LOC_KSPIE_Generator_coldBathTemp", guiUnits = " K", guiFormat = "F0")]
        public double coldBathTempDisplay = 500;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActive = false, guiName = "#LOC_KSPIE_Generator_hotBathTemp", guiUnits = " K", guiFormat = "F0")]
        public double hotBathTemp = 300;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActive = false, guiName = "#LOC_KSPIE_Generator_electricPowerNeeded", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")]
        public double electrical_power_currently_needed;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiActiveEditor = true, guiActive = false, guiName = "#LOC_KSPIE_Generator_InitialGeneratorPowerEC", guiUnits = " kW")]//Offscreen Power Generation
        public double initialGeneratorPowerEC;

        // Debug
        [KSPField] public double stableMaximumReactorPower;
        [KSPField] public double heat_exchanger_thrust_divisor;
        [KSPField] public double requested_power_per_second;
        [KSPField] public double received_power_per_second;
        [KSPField] public double post_received_power_per_second;
        [KSPField] public double spareResourceCapacity;
        [KSPField] public double possibleSpareResourceCapacityFilling;
        [KSPField] public double currentUnfilledResourceDemand;
        [KSPField] public double effectiveInputPowerPerSecond;
        [KSPField] public double postEffectiveInputPowerPerSecond;
        [KSPField] public double powerBufferBonus;
        [KSPField] public double stablePowerForBuffer;
        [KSPField] public double maxStableMegaWattPower;
        [KSPField] public bool applies_balance;
        [KSPField] public double effectiveThermalPowerNeededForElectricity;
        [KSPField] public double thermalPowerRequested;
        [KSPField] public double reactorPowerRequested;
        [KSPField] public double attachedPowerSourceMaximumThermalPowerUsageRatio;
        [KSPField] public double initialThermalPowerReceived;
        [KSPField] public double initialChargedPowerReceived;
        [KSPField] public double thermalPowerReceived;
        [KSPField] public double chargedPowerReceived;
        [KSPField] public double totalPowerReceived;
        [KSPField] public double overheatingModifier;
        [KSPField] public double requestedChargedPower;
        [KSPField] public double requestedThermalPower;
        [KSPField] public double finalRequest;
        [KSPField] public double requestedPostChargedPower;
        [KSPField] public double requestedPostThermalPower;
        [KSPField] public double requestedPostReactorPower;
        [KSPField] public double postThermalPowerReceived;
        [KSPField] public double postChargedPowerReceived;
        [KSPField] public double thermalPowerRequestRatio;
        [KSPField] public double effectiveMaxThermalPowerRatio;
        [KSPField] public double electricdtps;
        [KSPField] public double maxElectricdtps;
        [KSPField] public bool shouldUseChargedPower;
        [KSPField] public double _totalEff;
        [KSPField] public double capacityRatio;
        [KSPField] public double maximumGeneratorPowerMJ;
        [KSPField] public double currentPowerForGeneratorMJ;
        [KSPField] public double maximumGeneratorPowerEC;

        // Internal
        protected double outputPower;
        protected double powerDownFraction;

        protected bool play_down = true;
        protected bool play_up = true;
        protected bool hasrequiredupgrade;

        protected int partDistance;
        protected int shutdown_counter = 0;
        protected int startcount = 0;

        private PowerStates _powerState;
        private IFNPowerSource attachedPowerSource;

        private Animation anim;
        private Queue<double> averageRadiatorTemperatureQueue = new Queue<double>();

        public String UpgradeTechnology { get { return upgradeTechReq; } }

        [KSPEvent(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_Generator_activateGenerator", active = true)]
        public void ActivateGenerator()
        {
            IsEnabled = true;
        }

        [KSPEvent(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_Generator_deactivateGenerator", active = false)]
        public void DeactivateGenerator()
        {
            IsEnabled = false;
        }

        [KSPEvent(groupName = GROUP, guiActive = true, guiName = "#LOC_KSPIE_Generator_retrofitGenerator", active = true)]
        public void RetrofitGenerator()
        {
            if (ResearchAndDevelopment.Instance == null) return;

            if (isupgraded || ResearchAndDevelopment.Instance.Science < upgradeCost) return;

            upgradePartModule();
            ResearchAndDevelopment.Instance.AddScience(-upgradeCost, TransactionReasons.RnDPartPurchase);
        }

        [KSPAction("#LOC_KSPIE_Generator_activateGenerator")]
        public void ActivateGeneratorAction(KSPActionParam param)
        {
            ActivateGenerator();
        }

        [KSPAction("#LOC_KSPIE_Generator_deactivateGenerator")]
        public void DeactivateGeneratorAction(KSPActionParam param)
        {
            DeactivateGenerator();
        }

        [KSPAction("#LOC_KSPIE_Generator_toggleGenerator")]
        public void ToggleGeneratorAction(KSPActionParam param)
        {
            IsEnabled = !IsEnabled;
        }

        public virtual void OnRescale(TweakScale.ScalingFactor factor)
        {
            try
            {
                Debug.Log("[KSPI]: FNGenerator.OnRescale called with " + factor.absolute.linear);
                storedMassMultiplier = Math.Pow((double)(decimal)factor.absolute.linear, 3);
                initialMass = (double)(decimal)part.prefabMass * storedMassMultiplier;
                UpdateModuleGeneratorOutput();
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: FNGenerator.OnRescale " + e.Message);
            }
        }

        public void Refresh()
        {
            Debug.Log("[KSPI]: FNGenerator Refreshed");
            UpdateTargetMass();
        }

        public ModifierChangeWhen GetModuleMassChangeWhen()
        {
            return ModifierChangeWhen.STAGED;
        }

        public float GetModuleMass(float defaultMass, ModifierStagingSituation sit)
        {
            if (!calculatedMass)
                return 0;

            var moduleMassDelta = (float)(targetMass - initialMass);

            return moduleMassDelta;
        }

        public void upgradePartModule()
        {
            isupgraded = true;
        }

        /// <summary>
        /// Event handler which is called when part is attached to a new part
        /// </summary>
        private void OnEditorAttach()
        {
            try
            {
                Debug.Log("[KSPI]: attach " + part.partInfo.title);
                FindAndAttachToPowerSource();
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: FNGenerator.OnEditorAttach " + e.Message);
            }
        }

        /// <summary>
        /// Event handler which is called when part is deptach to a exiting part
        /// </summary>
        private void OnEditorDetach()
        {
            try
            {
                if (attachedPowerSource == null)
                    return;

                Debug.Log("[KSPI]: detach " + part.partInfo.title);
                if (chargedParticleMode && attachedPowerSource.ConnectedChargedParticleElectricGenerator != null)
                    attachedPowerSource.ConnectedChargedParticleElectricGenerator = null;
                if (!chargedParticleMode && attachedPowerSource.ConnectedThermalElectricGenerator != null)
                    attachedPowerSource.ConnectedThermalElectricGenerator = null;
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: FNGenerator.OnEditorDetach " + e.Message);
            }
        }

        private void OnDestroyed()
        {
            try
            {
                OnEditorDetach();

                // RemoveItselfAsManager();
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: Exception on " + part.name + " durring FNGenerator.OnDestroyed with message " + e.Message);
            }
        }

        public override void OnStart(PartModule.StartState state)
        {
            // String[] resources_to_supply = { ResourceSettings.Config.ElectricPowerInMegawatt, ResourceSettings.Config.WasteHeatInMegawatt, ResourceSettings.Config.ThermalPowerInMegawatt, ResourceSettings.Config.ChargedParticleInMegawatt };
            // this.resources_to_supply = resources_to_supply;

            base.OnStart(state);

            var prefabMass = (double)(decimal)part.prefabMass;
            targetMass = prefabMass * storedMassMultiplier;
            initialMass = prefabMass * storedMassMultiplier;

            if (initialMass == 0)
                initialMass = prefabMass;
            if (targetMass == 0)
                targetMass = prefabMass;

            InitializeEfficiency();

            var powerCapacityField = Fields["powerCapacity"];
            powerCapacityField.guiActiveEditor = !isLimitedByMinThrottle;

            var powerCapacityFloatRange = powerCapacityField.uiControlEditor as UI_FloatRange;
            powerCapacityFloatRange.maxValue = powerCapacityMaxValue;
            powerCapacityFloatRange.minValue = powerCapacityMinValue;
            powerCapacityFloatRange.stepIncrement = powerCapacityStepIncrement;

            if (state == StartState.Editor)
            {
                powerCapacity = Math.Max(powerCapacityMinValue, powerCapacity);
                powerCapacity = Math.Min(powerCapacityMaxValue, powerCapacity);
            }

            Fields["partMass"].guiActive = Fields["partMass"].guiActiveEditor = calculatedMass;
            Fields["powerPercentage"].guiActive = Fields["powerPercentage"].guiActiveEditor = showSpecialisedUI;
            Fields["radius"].guiActiveEditor = showSpecialisedUI;

            if (state == StartState.Editor)
            {
                if (this.HasTechsRequiredToUpgrade())
                {
                    isupgraded = true;
                    hasrequiredupgrade = true;
                    upgradePartModule();
                }
                part.OnEditorAttach += OnEditorAttach;
                part.OnEditorDetach += OnEditorDetach;
                part.OnEditorDestroy += OnEditorDetach;

                part.OnJustAboutToBeDestroyed += OnDestroyed;
                part.OnJustAboutToDie += OnDestroyed;

                FindAndAttachToPowerSource();
                return;
            }

            if (this.HasTechsRequiredToUpgrade())
                hasrequiredupgrade = true;

            // only force activate if no certain partmodules are not present and not limited by minimum throtle
            if (!isLimitedByMinThrottle && part.FindModuleImplementing<BeamedPowerReceiver>() == null && part.FindModuleImplementing<InterstellarReactor>() == null)
            {
                Debug.Log("[KSPI]: Generator on " + part.name + " was Force Activated");
                part.force_activate();
            }

            anim = part.FindModelAnimators(animName).FirstOrDefault();
            if (anim != null)
            {
                anim[animName].layer = 1;
                if (!IsEnabled)
                {
                    anim[animName].normalizedTime = 1;
                    anim[animName].speed = -1;
                }
                else
                {
                    anim[animName].normalizedTime = 0;
                    anim[animName].speed = 1;
                }
                anim.Play();
            }

            if (generatorInit == false)
            {
                IsEnabled = true;
            }

            if (isupgraded)
                upgradePartModule();

            FindAndAttachToPowerSource();

            UpdateHeatExchangedThrustDivisor();
        }

        private void InitializeEfficiency()
        {
            if (efficiencyMk1 == 0)
                efficiencyMk1 = 0.1;
            if (efficiencyMk2 == 0)
                efficiencyMk2 = efficiencyMk1;
            if (efficiencyMk3 == 0)
                efficiencyMk3 = efficiencyMk2;
            if (efficiencyMk4 == 0)
                efficiencyMk4 = efficiencyMk3;
            if (efficiencyMk5 == 0)
                efficiencyMk5 = efficiencyMk4;
            if (efficiencyMk6 == 0)
                efficiencyMk6 = efficiencyMk5;
            if (efficiencyMk7 == 0)
                efficiencyMk7 = efficiencyMk6;
            if (efficiencyMk8 == 0)
                efficiencyMk8 = efficiencyMk7;
            if (efficiencyMk9 == 0)
                efficiencyMk9 = efficiencyMk8;

            if (string.IsNullOrEmpty(Mk2TechReq))
                Mk2TechReq = upgradeTechReq;

            int techLevel = 1;
            if (PluginHelper.UpgradeAvailable(Mk9TechReq))
                techLevel++;
            if (PluginHelper.UpgradeAvailable(Mk8TechReq))
                techLevel++;
            if (PluginHelper.UpgradeAvailable(Mk7TechReq))
                techLevel++;
            if (PluginHelper.UpgradeAvailable(Mk6TechReq))
                techLevel++;
            if (PluginHelper.UpgradeAvailable(Mk5TechReq))
                techLevel++;
            if (PluginHelper.UpgradeAvailable(Mk4TechReq))
                techLevel++;
            if (PluginHelper.UpgradeAvailable(Mk3TechReq))
                techLevel++;
            if (PluginHelper.UpgradeAvailable(Mk2TechReq))
                techLevel++;

            if (techLevel >= 9)
                maxEfficiency = efficiencyMk9;
            else if (techLevel == 8)
                maxEfficiency = efficiencyMk8;
            else if (techLevel >= 7)
                maxEfficiency = efficiencyMk7;
            else if (techLevel == 6)
                maxEfficiency = efficiencyMk6;
            else if (techLevel == 5)
                maxEfficiency = efficiencyMk5;
            else if (techLevel == 4)
                maxEfficiency = efficiencyMk4;
            else if (techLevel == 3)
                maxEfficiency = efficiencyMk3;
            else if (techLevel == 2)
                maxEfficiency = efficiencyMk2;
            else
                maxEfficiency = efficiencyMk1;
        }

        /// <summary>
        /// Finds the nearest available thermal source and update effective part mass
        /// </summary>
        public void FindAndAttachToPowerSource()
        {
            partDistance = 0;

            // disconnect
            if (attachedPowerSource != null)
            {
                if (chargedParticleMode)
                    attachedPowerSource.ConnectedChargedParticleElectricGenerator = null;
                else
                    attachedPowerSource.ConnectedThermalElectricGenerator = null;
            }

            // first look if part contains an thermal source
            attachedPowerSource = part.FindModulesImplementing<IFNPowerSource>().FirstOrDefault();
            if (attachedPowerSource != null)
            {
                ConnectToPowerSource();
                Debug.Log("[KSPI]: Found power source localy");
                return;
            }

            if (!part.attachNodes.Any() || part.attachNodes.All(m => m.attachedPart == null))
            {
                Debug.Log("[KSPI]: not connected to any parts yet");
                UpdateTargetMass();
                return;
            }

            Debug.Log("[KSPI]: generator is currently connected to " + part.attachNodes.Count + " parts");
            // otherwise look for other non self contained thermal sources that is not already connected

            var searchResult = chargedParticleMode
                ? FindChargedParticleSource()
                : isMHD
                    ? FindPlasmaPowerSource()
                    : FindThermalPowerSource();

            // quit if we failed to find anything
            if (searchResult == null)
            {
                Debug.LogWarning("[KSPI]: Failed to find power source");
                return;
            }

            // verify cost is not higher than 1
            partDistance = (int)Math.Max(Math.Ceiling(searchResult.Cost), 0);
            if (partDistance > 1)
            {
                Debug.LogWarning("[KSPI]: Found power source but at too high cost");
                return;
            }

            // update attached thermal source
            attachedPowerSource = searchResult.Source;

            Debug.Log("[KSPI]: succesfully connected to " + attachedPowerSource.Part.partInfo.title);

            ConnectToPowerSource();
        }

        private void ConnectToPowerSource()
        {
            //connect with source
            if (chargedParticleMode)
                attachedPowerSource.ConnectedChargedParticleElectricGenerator = this;
            else
                attachedPowerSource.ConnectedThermalElectricGenerator = this;

            UpdateTargetMass();

            UpdateModuleGeneratorOutput();
        }

        private void UpdateModuleGeneratorOutput()
        {
            if (attachedPowerSource == null)
                return;

            var maximumPower = isLimitedByMinThrottle ? attachedPowerSource.MinimumPower : attachedPowerSource.MaximumPower;

            if (chargedParticleMode)
                maximumGeneratorPowerMJ = maximumPower * maxEfficiency;
            else
                maximumGeneratorPowerMJ = maximumPower * maxEfficiency * 0.6;

        }

        private PowerSourceSearchResult FindThermalPowerSource()
        {
            var searchResult =
                PowerSourceSearchResult.BreadthFirstSearchForThermalSource(part,
                    p => p.IsThermalSource && p.ConnectedThermalElectricGenerator == null && p.ThermalEnergyEfficiency > 0,
                    3, 3, 3, true);
            return searchResult;
        }

        private PowerSourceSearchResult FindPlasmaPowerSource()
        {
            var searchResult =
                PowerSourceSearchResult.BreadthFirstSearchForThermalSource(part,
                     p => p.IsThermalSource && p.ConnectedChargedParticleElectricGenerator == null && p.PlasmaEnergyEfficiency > 0,
                     3, 3, 3, true);
            return searchResult;
        }

        private PowerSourceSearchResult FindChargedParticleSource()
        {
            var searchResult =
                PowerSourceSearchResult.BreadthFirstSearchForThermalSource(part,
                     p => p.IsThermalSource && p.ConnectedChargedParticleElectricGenerator == null && p.ChargedParticleEnergyEfficiency > 0,
                     3, 3, 3, true);
            return searchResult;
        }

        private void UpdateTargetMass()
        {
            try
            {
                if (attachedPowerSource == null)
                {
                    targetMass = initialMass;
                    return;
                }

                if (chargedParticleMode && attachedPowerSource.ChargedParticleEnergyEfficiency > 0)
                    powerUsageEfficiency = attachedPowerSource.ChargedParticleEnergyEfficiency;
                else if (isMHD && attachedPowerSource.PlasmaEnergyEfficiency > 0)
                    powerUsageEfficiency = attachedPowerSource.PlasmaEnergyEfficiency;
                else if (attachedPowerSource.ThermalEnergyEfficiency > 0)
                    powerUsageEfficiency = attachedPowerSource.ThermalEnergyEfficiency;
                else
                    powerUsageEfficiency = 1;

                rawMaximumPower = attachedPowerSource.RawMaximumPowerForPowerGeneration * powerUsageEfficiency;
                maximumTheoreticalPower = PluginHelper.getFormattedPowerString(rawMaximumPower * CapacityRatio * maxEfficiency);

                // verify if mass calculation is active
                if (!calculatedMass)
                {
                    targetMass = initialMass;
                    return;
                }

                // update part mass
                if (rawMaximumPower > 0 && rawPowerToMassDivider > 0)
                    targetMass = (massModifier * attachedPowerSource.ThermalProcessingModifier * rawMaximumPower * Math.Pow(CapacityRatio, capacityToMassExponent)) / rawPowerToMassDivider;
                else
                    targetMass = initialMass;
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: FNGenerator.UpdateTargetMass " + e.Message);
            }
        }

        public double CapacityRatio
        {
            get
            {
                capacityRatio = (double)(decimal)powerCapacity / 100;
                return capacityRatio;
            }
        }

        public double PowerRatio
        {
            get
            {
                powerRatio = (double)(decimal)powerPercentage / 100;
                return powerRatio;
            }
        }

        /// <summary>
        /// Is called by KSP while the part is active
        /// </summary>
        public override void OnUpdate()
        {
            Events[nameof(ActivateGenerator)].active = !IsEnabled && showSpecialisedUI;
            Events[nameof(DeactivateGenerator)].active = IsEnabled && showSpecialisedUI;
            Fields[nameof(overallEfficiencyStr)].guiActive = showDetailedInfo && IsEnabled;
            Fields[nameof(MaxPowerStr)].guiActive = showDetailedInfo && IsEnabled;
            Fields[nameof(coldBathTempDisplay)].guiActive = showDetailedInfo && !chargedParticleMode;
            Fields[nameof(hotBathTemp)].guiActive = showDetailedInfo && !chargedParticleMode;

            if (ResearchAndDevelopment.Instance != null)
            {
                Events[nameof(RetrofitGenerator)].active = !isupgraded && ResearchAndDevelopment.Instance.Science >= upgradeCost && hasrequiredupgrade;
                upgradeCostStr = ResearchAndDevelopment.Instance.Science.ToString("0") + " / " + upgradeCost;
            }
            else
                Events[nameof(RetrofitGenerator)].active = false;

            if (IsEnabled)
            {
                if (play_up && anim != null)
                {
                    play_down = true;
                    play_up = false;
                    anim[animName].speed = 1;
                    anim[animName].normalizedTime = 0;
                    anim.Blend(animName, 2);
                }
            }
            else
            {
                if (play_down && anim != null)
                {
                    play_down = false;
                    play_up = true;
                    anim[animName].speed = -1;
                    anim[animName].normalizedTime = 1;
                    anim.Blend(animName, 2);
                }
            }

            if (IsEnabled)
            {
                var percentOutputPower = _totalEff * 100.0;
                var outputPowerReport = -outputPower;

                OutputPower = PluginHelper.getFormattedPowerString(outputPowerReport);
                overallEfficiencyStr = percentOutputPower.ToString("0.00") + "%";

                maximumElectricPower = (_totalEff >= 0)
                    ? !chargedParticleMode
                        ? PowerRatio * _totalEff * maxThermalPower
                        : PowerRatio * _totalEff * maxChargedPowerForChargedGenerator
                    : 0;

                MaxPowerStr = PluginHelper.getFormattedPowerString(maximumElectricPower);
            }
            else
                OutputPower = Localizer.Format("#LOC_KSPIE_Generator_Offline");//"Generator Offline"
        }

        public double MaxStableMegaWattPower
        {
            get
            {
                if (attachedPowerSource == null || !IsEnabled)
                    return 0;

                var maxPowerUsageRatio =
                    chargedParticleMode
                        ? attachedPowerSource.ChargedParticleEnergyEfficiency
                        : isMHD
                            ? attachedPowerSource.PlasmaEnergyEfficiency
                            : attachedPowerSource.ThermalEnergyEfficiency;

                stableMaximumReactorPower = isLimitedByMinThrottle
                    ? attachedPowerSource.MinimumPower
                    : HighLogic.LoadedSceneIsEditor
                        ? attachedPowerSource.MaximumPower
                        : attachedPowerSource.StableMaximumReactorPower;

                return stableMaximumReactorPower * attachedPowerSource.PowerRatio * maxPowerUsageRatio * maxEfficiency * CapacityRatio;
            }
        }

        private void UpdateHeatExchangedThrustDivisor()
        {
            if (attachedPowerSource == null) return;

            if (attachedPowerSource.Radius <= 0 || radius <= 0)
            {
                heat_exchanger_thrust_divisor = 1;
                return;
            }

            heat_exchanger_thrust_divisor = radius > attachedPowerSource.Radius
                ? attachedPowerSource.Radius * attachedPowerSource.Radius / radius / radius
                : radius * radius / attachedPowerSource.Radius / attachedPowerSource.Radius;
        }

        private void UpdateGeneratorPower()
        {
            if (attachedPowerSource == null) return;

            if (!chargedParticleMode) // thermal or plasma mode
            {
                var chargedPowerModifier = attachedPowerSource.ChargedPowerRatio * attachedPowerSource.ChargedPowerRatio;

                var plasmaTemperature = attachedPowerSource.CoreTemperature <= attachedPowerSource.HotBathTemperature
                    ? attachedPowerSource.CoreTemperature
                    : attachedPowerSource.HotBathTemperature + Math.Pow(attachedPowerSource.CoreTemperature - attachedPowerSource.HotBathTemperature, coreTemperateHotBathExponent);

                hotBathTemp = applies_balance || !isMHD
                    ? attachedPowerSource.HotBathTemperature
                    : attachedPowerSource.SupportMHD
                        ? plasmaTemperature
                        : plasmaTemperature * chargedPowerModifier + (1 - chargedPowerModifier) * attachedPowerSource.HotBathTemperature;	// for fusion reactors connected to MHD

                averageRadiatorTemperatureQueue.Enqueue(FNRadiator.GetAverageRadiatorTemperatureForVessel(vessel));

                while (averageRadiatorTemperatureQueue.Count > 10)
                    averageRadiatorTemperatureQueue.Dequeue();

                coldBathTempDisplay = averageRadiatorTemperatureQueue.Average();
                coldBathTemp = coldBathTempDisplay * 0.75;
            }

            if (HighLogic.LoadedSceneIsEditor)
                UpdateHeatExchangedThrustDivisor();

            attachedPowerSourceRatio = attachedPowerSource.PowerRatio;
            effectiveMaximumThermalPower = attachedPowerSource.MaximumThermalPower * PowerRatio * CapacityRatio;

            rawThermalPower = isLimitedByMinThrottle ? attachedPowerSource.MinimumPower : effectiveMaximumThermalPower;
            rawChargedPower = attachedPowerSource.MaximumChargedPower * PowerRatio * CapacityRatio;
            rawReactorPower = rawThermalPower + rawChargedPower;

            if (!(attachedPowerSourceRatio > 0))
            {
                maxChargedPowerForThermalGenerator = rawChargedPower;
                maxThermalPower = rawThermalPower;
                maxReactorPower = rawReactorPower;
                return;
            }

            attachedPowerSourceMaximumThermalPowerUsageRatio = isMHD
                ? attachedPowerSource.PlasmaEnergyEfficiency
                : attachedPowerSource.ThermalEnergyEfficiency;

            potentialThermalPower = ((applies_balance ? rawThermalPower : rawReactorPower) / attachedPowerSourceRatio);
            maxAllowedChargedPower = rawChargedPower * (chargedParticleMode ? attachedPowerSource.ChargedParticleEnergyEfficiency : 1);

            maxThermalPower = attachedPowerSourceMaximumThermalPowerUsageRatio * Math.Min(rawReactorPower, potentialThermalPower);
            maxChargedPowerForThermalGenerator = attachedPowerSourceMaximumThermalPowerUsageRatio * Math.Min(rawChargedPower, (1 / attachedPowerSourceRatio) * maxAllowedChargedPower);
            maxChargedPowerForChargedGenerator = attachedPowerSource.ChargedParticleEnergyEfficiency * Math.Min(rawChargedPower, (1 / attachedPowerSourceRatio) * maxAllowedChargedPower);
            maxReactorPower = chargedParticleMode ? maxChargedPowerForChargedGenerator : maxThermalPower;
        }

        // Update is called in the editor
        // ReSharper disable once UnusedMember.Global
        public void Update()
        {
            partMass = part.mass;

            if (HighLogic.LoadedSceneIsFlight) return;

            UpdateTargetMass();

            Fields[nameof(targetMass)].guiActive = attachedPowerSource != null && attachedPowerSource.Part != this.part;
        }

        private void PowerDown()
        {
            if (_powerState != PowerStates.PowerOffline)
            {
                if (powerDownFraction > 0)
                    powerDownFraction -= 0.01;

                if (powerDownFraction <= 0)
                    _powerState = PowerStates.PowerOffline;
            }
        }

        public override string GetInfo()
        {
            var sb = StringBuilderCache.Acquire();
            sb.Append("<color=#7fdfffff>").Append(Localizer.Format("#LOC_KSPIE_Generator_upgradeTechnologies")).AppendLine("</color>");
            sb.Append("<size=10>");

            if (!string.IsNullOrEmpty(Mk2TechReq))
                sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(Mk2TechReq)));
            if (!string.IsNullOrEmpty(Mk3TechReq))
                sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(Mk3TechReq)));
            if (!string.IsNullOrEmpty(Mk4TechReq))
                sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(Mk4TechReq)));
            if (!string.IsNullOrEmpty(Mk5TechReq))
                sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(Mk5TechReq)));
            if (!string.IsNullOrEmpty(Mk6TechReq))
                sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(Mk6TechReq)));
            if (!string.IsNullOrEmpty(Mk7TechReq))
                sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(Mk7TechReq)));
            if (!string.IsNullOrEmpty(Mk8TechReq))
                sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(Mk8TechReq)));
            if (!string.IsNullOrEmpty(Mk9TechReq))
                sb.Append("- ").AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(Mk9TechReq)));

            sb.Append("</size><color=#7fdfffff>").Append(Localizer.Format("#LOC_KSPIE_Generator_conversionEfficiency")).AppendLine("</color>");
            sb.Append("<size=10>Mk1: ").AppendLine(efficiencyMk1.ToString("P0"));
            if (!string.IsNullOrEmpty(Mk2TechReq))
                sb.Append("Mk2: ").AppendLine(efficiencyMk2.ToString("P0"));
            if (!string.IsNullOrEmpty(Mk3TechReq))
                sb.Append("Mk3: ").AppendLine(efficiencyMk3.ToString("P0"));
            if (!string.IsNullOrEmpty(Mk4TechReq))
                sb.Append("Mk4: ").AppendLine(efficiencyMk4.ToString("P0"));
            if (!string.IsNullOrEmpty(Mk5TechReq))
                sb.Append("Mk5: ").AppendLine(efficiencyMk5.ToString("P0"));
            if (!string.IsNullOrEmpty(Mk6TechReq))
                sb.Append("Mk6: ").AppendLine(efficiencyMk6.ToString("P0"));
            if (!string.IsNullOrEmpty(Mk7TechReq))
                sb.Append("Mk7: ").AppendLine(efficiencyMk7.ToString("P0"));
            if (!string.IsNullOrEmpty(Mk8TechReq))
                sb.Append("Mk8: ").AppendLine(efficiencyMk8.ToString("P0"));
            if (!string.IsNullOrEmpty(Mk9TechReq))
                sb.Append("Mk9: ").AppendLine(efficiencyMk9.ToString("P0"));
            sb.Append("</size>");

            return sb.ToStringAndRelease();
        }


        public ResourcePriorityValue ResourceProcessPriority()
        {
            if (isLimitedByMinThrottle)
                return (ResourcePriorityValue)1;

            if (attachedPowerSource == null)
                return ResourcePriorityValue.Third;

            return (ResourcePriorityValue)attachedPowerSource.ProviderPowerPriority;
        }

        double thermalPowerRatio, chargedPowerRatio;

        public void KITFixedUpdate(IResourceManager resMan)
        {
            if (!IsEnabled || attachedPowerSource == null || !FNRadiator.HasRadiatorsForVessel(vessel))
            {
                electricdtps = 0;
                maxElectricdtps = 0;
                generatorInit = true;
                resMan.ProduceResource(ResourceName.ElectricCharge, 0);

                if (IsEnabled && !vessel.packed)
                {
                    if (attachedPowerSource == null)
                    {
                        IsEnabled = false;
                        var message = Localizer.Format("#LOC_KSPIE_Generator_Msg1");//"Generator Shutdown: No Attached Power Source"
                        Debug.Log("[KSPI]: " + message);
                        ScreenMessages.PostScreenMessage(message, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        PowerDown();
                    }
                    else if (!FNRadiator.HasRadiatorsForVessel(vessel))
                    {
                        IsEnabled = false;
                        var message = Localizer.Format("#LOC_KSPIE_Generator_Msg2");//"Generator Shutdown: No radiators available!"
                        Debug.Log("[KSPI]: " + message);
                        ScreenMessages.PostScreenMessage(message, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        PowerDown();
                    }
                }
                else
                {
                    PowerDown();
                }

                return;
            }

            applies_balance = attachedPowerSource.ShouldApplyBalance(chargedParticleMode ? ElectricGeneratorType.charged_particle : ElectricGeneratorType.thermal);

            UpdateGeneratorPower();

            // check if MaxStableMegaWattPower is changed
            maxStableMegaWattPower = MaxStableMegaWattPower;

            generatorInit = true;

            // don't produce any power when our reactor has stopped
            if (maxStableMegaWattPower <= 0)
            {
                electricdtps = 0;
                maxElectricdtps = 0;
                PowerDown();

                return;
            }

            powerDownFraction = 1;

            var wasteheatRatio = resMan.ResourceFillFraction(ResourceName.WasteHeat);
            overheatingModifier = wasteheatRatio < 0.9 ? 1 : (1 - wasteheatRatio) * 10;

            thermalPowerRatio = 1 - attachedPowerSource.ChargedPowerRatio;
            chargedPowerRatio = attachedPowerSource.ChargedPowerRatio;

            if (!chargedParticleMode) // thermal mode
            {
                hotColdBathRatio = Math.Max(Math.Min(1 - coldBathTemp / hotBathTemp, 1), 0);

                _totalEff = Math.Min(maxEfficiency, hotColdBathRatio * maxEfficiency);

                if (hotColdBathRatio <= 0.01 || coldBathTemp <= 0 || hotBathTemp <= 0 || maxThermalPower <= 0)
                {
                    requested_power_per_second = 0;
                    return;
                }
            }
            else
            {
                hotColdBathRatio = 1;
                _totalEff = maxEfficiency;
            }

        }

        public string KITPartName()
        {
            if (isLimitedByMinThrottle)
                return part.partInfo.title;

            var displayName = part.partInfo.title + " " + Localizer.Format("#LOC_KSPIE_Generator_partdisplay");//(generator)

            /* 
            if (similarParts == null)
            {
                similarParts = vessel.parts.Where(m => m.partInfo.title == this.part.partInfo.title).ToList();
                partNrInList = 1 + similarParts.IndexOf(this.part);
            }

            if (similarParts.Count > 1)
                displayName += " " + partNrInList;
            */

            return displayName;
        }

        private ResourceName[] resourcesProvided = new ResourceName[] { ResourceName.ElectricCharge };
        public ResourceName[] ResourcesProvided() => resourcesProvided;

        // todo - max power output limit
        public bool ProvideResource(IResourceManager resMan, ResourceName resource, double requestedAmount)
        {
            if (!chargedParticleMode)
            {
                electrical_power_currently_needed = requestedAmount;

                effectiveThermalPowerNeededForElectricity = electrical_power_currently_needed / _totalEff;

                reactorPowerRequested = Math.Max(0, Math.Min(maxReactorPower, effectiveThermalPowerNeededForElectricity));
                requestedPostReactorPower = Math.Max(0, attachedPowerSource.MinimumPower - reactorPowerRequested);

                thermalPowerRequested = Math.Max(0, Math.Min(maxThermalPower, effectiveThermalPowerNeededForElectricity));
                thermalPowerRequested *= applies_balance && chargedPowerRatio != 1 ? thermalPowerRatio : 1;
                requestedPostThermalPower = Math.Max(0, (attachedPowerSource.MinimumPower * thermalPowerRatio) - thermalPowerRequested);

                requested_power_per_second = thermalPowerRequested;

                if (chargedPowerRatio != 1)
                {
                    requestedThermalPower = Math.Min(thermalPowerRequested, effectiveMaximumThermalPower);

                    if (isMHD)
                    {
                        initialThermalPowerReceived = resMan.ConsumeResource(ResourceName.ThermalPower, requestedThermalPower * thermalPowerRatio);
                        initialChargedPowerReceived = resMan.ConsumeResource(ResourceName.ChargedParticle, requestedThermalPower * chargedPowerRatio);
                    }
                    else
                        initialThermalPowerReceived = resMan.ConsumeResource(ResourceName.ThermalPower, requestedThermalPower);

                    thermalPowerRequestRatio = Math.Min(1, effectiveMaximumThermalPower > 0 ? requestedThermalPower / attachedPowerSource.MaximumThermalPower : 0);
                    attachedPowerSource.NotifyActiveThermalEnergyGenerator(_totalEff, thermalPowerRequestRatio, isMHD, isLimitedByMinThrottle ? part.mass * 0.05 : part.mass);
                }
                else
                    initialThermalPowerReceived = 0;

                thermalPowerReceived = initialThermalPowerReceived + initialChargedPowerReceived;
                totalPowerReceived = thermalPowerReceived;

                shouldUseChargedPower = chargedPowerRatio > 0;

                // Collect charged power when needed
                if (chargedPowerRatio == 1)
                {
                    requestedChargedPower = reactorPowerRequested;

                    chargedPowerReceived = resMan.ConsumeResource(ResourceName.ChargedParticle, requestedChargedPower);

                    var maximumChargedPower = attachedPowerSource.MaximumChargedPower * powerUsageEfficiency * CapacityRatio;
                    var chargedPowerRequestRatio = Math.Min(1, maximumChargedPower > 0 ? thermalPowerRequested / maximumChargedPower : 0);

                    attachedPowerSource.NotifyActiveThermalEnergyGenerator(_totalEff, chargedPowerRequestRatio, isMHD, isLimitedByMinThrottle ? part.mass * 0.05 : part.mass);
                }
                else if (shouldUseChargedPower && thermalPowerReceived < reactorPowerRequested)
                {
                    requestedChargedPower = Math.Min(Math.Min(reactorPowerRequested - thermalPowerReceived, maxChargedPowerForThermalGenerator), Math.Max(0, maxReactorPower - thermalPowerReceived));
                    //chargedPowerReceived = consumeFNResourcePerSecond(requestedChargedPower, ResourceSettings.Config.ChargedParticleInMegawatt);
                    chargedPowerReceived = resMan.ConsumeResource(ResourceName.ChargedParticle, requestedChargedPower);
                }
                else
                {
                    //consumeFNResourcePerSecond(0, ResourceSettings.Config.ChargedParticleInMegawatt);
                    resMan.ConsumeResource(ResourceName.ChargedParticle, 0);
                    chargedPowerReceived = 0;
                    requestedChargedPower = 0;
                }

                totalPowerReceived += chargedPowerReceived;

                // any shortage should be consumed again from remaining thermalpower
                if (shouldUseChargedPower && chargedPowerRatio != 1 && totalPowerReceived < reactorPowerRequested)
                {
                    finalRequest = Math.Max(0, reactorPowerRequested - totalPowerReceived);
                    thermalPowerReceived += resMan.ConsumeResource(ResourceName.ThermalPower, finalRequest);
                }
            }
            else
            {
                electrical_power_currently_needed = requestedAmount;

                requestedChargedPower = overheatingModifier * Math.Max(0, Math.Min(maxAllowedChargedPower, electrical_power_currently_needed / _totalEff));
                requestedPostChargedPower = overheatingModifier * Math.Max(0, (attachedPowerSource.MinimumPower * chargedPowerRatio) - requestedChargedPower);

                requested_power_per_second = requestedChargedPower;

                var maximumChargedPower = attachedPowerSource.MaximumChargedPower * attachedPowerSource.ChargedParticleEnergyEfficiency;
                var chargedPowerRequestRatio = Math.Min(1, maximumChargedPower > 0 ? requestedChargedPower / maximumChargedPower : 0);
                attachedPowerSource.NotifyActiveChargedEnergyGenerator(_totalEff, chargedPowerRequestRatio, part.mass);

                chargedPowerReceived = resMan.ConsumeResource(ResourceName.ChargedParticle, requestedChargedPower);
            }

            received_power_per_second = thermalPowerReceived + chargedPowerReceived;
            effectiveInputPowerPerSecond = received_power_per_second * _totalEff;

            resMan.ProduceResource(ResourceName.WasteHeat, effectiveInputPowerPerSecond);

            if (!chargedParticleMode)
            {
                electricdtps = Math.Max(effectiveInputPowerPerSecond * powerOutputMultiplier, 0);
                effectiveMaxThermalPowerRatio = applies_balance ? thermalPowerRatio : 1;
                maxElectricdtps = effectiveMaxThermalPowerRatio * attachedPowerSource.StableMaximumReactorPower * attachedPowerSource.PowerRatio * powerUsageEfficiency * _totalEff * CapacityRatio;
            }
            else
            {
                electricdtps = Math.Max(effectiveInputPowerPerSecond * powerOutputMultiplier, 0);
                maxElectricdtps = overheatingModifier * maxChargedPowerForChargedGenerator * _totalEff;
            }

            resMan.ProduceResource(ResourceName.ElectricCharge, electricdtps, maxElectricdtps);

            return true;
        }
    }
}


