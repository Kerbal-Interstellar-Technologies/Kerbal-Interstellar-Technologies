using KIT.Resources;
using KIT.ResourceScheduler;
using KSP.Localization;
using System;

namespace KIT.Reactors
{
    [KSPModule("Particle Accelerator")]
    class FNParticleAccelerator : InterstellarInertialConfinementReactor { }

    [KSPModule("Quantum Singularity Reactor")]
    class QuantumSingularityReactor : InterstellarInertialConfinementReactor { }

    [KSPModule("Confinement Fusion Reactor")]
    class IntegratedInertialConfinementReactor : InterstellarInertialConfinementReactor {}

    [KSPModule("Confinement Fusion Engine")]
    class IntegratedInertialConfinementEngine : InterstellarInertialConfinementReactor { }

    [KSPModule("Confinement Fusion Reactor")]
    class InertialConfinementReactor : InterstellarInertialConfinementReactor { }

    [KSPModule("Inertial Confinement Fusion Reactor")]
    class InterstellarInertialConfinementReactor : InterstellarFusionReactor
    {
        // Configs
        [KSPField] public bool CanJumpStart = true;
        [KSPField] public bool canChargeJumpStart = true;
        [KSPField] public float startupPowerMultiplier = 1;
        [KSPField] public float startupCostGravityMultiplier;
        [KSPField] public float startupCostGravityExponent = 1;
        [KSPField] public float startupMaximumGeforce = 10000;
        [KSPField] public float startupMinimumChargePercentage;
        [KSPField] public double geeForceMaintenancePowerMultiplier;
        [KSPField] public bool showSecondaryPowerUsage;
        [KSPField] public double gravityDivider;

        // Persistent
        [KSPField(isPersistant = true)]
        public double accumulatedElectricChargeInMW;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_InertialConfinementReactor_MaxSecondaryPowerUsage"), UI_FloatRange(stepIncrement = 1f / 3f, maxValue = 100, minValue = 1)]//Max Secondary Power Usage
        public float maxSecondaryPowerUsage = 90;
        [KSPField(groupName = Group, guiName = "#LOC_KSPIE_InertialConfinementReactor_PowerAffectsMaintenance")]//Power Affects Maintenance
        public bool powerControlAffectsMaintenance = true;

        // UI Display
        [KSPField(groupName = Group, guiActive = false, guiUnits = "%", guiName = "#LOC_KSPIE_InertialConfinementReactor_MinimumThrotle", guiFormat = "F2")]//Minimum Throttle
        public double minimumThrottlePercentage;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = true, guiName = "#LOC_KSPIE_InertialConfinementReactor_Charge")]//Charge
        public string accumulatedChargeStr = string.Empty;
        [KSPField(groupName = Group, guiActive = false, guiName = "#LOC_KSPIE_InertialConfinementReactor_FusionPowerRequirement", guiFormat = "F2")]//Fusion Power Requirement
        public double currentLaserPowerRequirements;
        [KSPField(groupName = Group, isPersistant = true, guiName = "#LOC_KSPIE_InertialConfinementReactor_Startup"), UI_Toggle(disabledText = "#LOC_KSPIE_InertialConfinementReactor_Startup_Off", enabledText = "#LOC_KSPIE_InertialConfinementReactor_Startup_Charging")]//Startup--Off--Charging
        public bool isChargingForJumpStart;

        private double _powerConsumed;
        private int jumpStartPowerTime;
        private double _framesPlasmaRatioIsGood;

        private BaseField isChargingField;
        private BaseField accumulatedChargeStrField;
        private PartResourceDefinition primaryInputResourceDefinition;
        private PartResourceDefinition secondaryInputResourceDefinition;

        public override double PlasmaModifier => plasma_ratio;
        public double GravityDivider => startupCostGravityMultiplier * Math.Pow(FlightGlobals.getGeeForceAtPosition(vessel.GetWorldPos3D()).magnitude, startupCostGravityExponent);

        public override void OnStart(StartState state)
        {
            isChargingField = Fields[nameof(isChargingForJumpStart)];
            accumulatedChargeStrField = Fields[nameof(accumulatedChargeStr)];

            Fields[nameof(maxSecondaryPowerUsage)].guiActive = showSecondaryPowerUsage;
            Fields[nameof(maxSecondaryPowerUsage)].guiActiveEditor = showSecondaryPowerUsage;

            isChargingField.guiActiveEditor = false;

            base.OnStart(state);

            if (state != StartState.Editor && allowJumpStart)
            {
                if (StartDisabled)
                {
                    allowJumpStart = false;
                    IsEnabled = false;
                }
                else
                {
                    jumpStartPowerTime = 50;
                    IsEnabled = true;
                    reactor_power_ratio = 1;
                }

                UnityEngine.Debug.LogWarning("[KSPI]: InterstellarInertialConfinementReactor.OnStart allowJumpStart");
            }
        }

        public override void StartReactor()
        {
            // instead of starting the reactor right away, we always first have to charge it
            isChargingForJumpStart = true;
        }

        public override double MinimumThrottle
        {
            get
            {
                var currentMinimumThrottle = (powerPercentage > 0 && base.MinimumThrottle > 0)
                    ? Math.Min(base.MinimumThrottle / PowerRatio, 1)
                    : base.MinimumThrottle;

                minimumThrottlePercentage = currentMinimumThrottle * 100;

                return currentMinimumThrottle;
            }
        }

        public double LaserPowerRequirements
        {
            get
            {
                currentLaserPowerRequirements =
                    CurrentFuelMode == null
                    ? PowerRequirement
                    : powerControlAffectsMaintenance
                        ? PowerRatio * NormalizedPowerRequirement
                        : NormalizedPowerRequirement;

                if (geeForceMaintenancePowerMultiplier > 0)
                    currentLaserPowerRequirements += Math.Abs(currentLaserPowerRequirements * geeForceMaintenancePowerMultiplier * part.vessel.geeForce);

                return currentLaserPowerRequirements;
            }
        }

        public double StartupPower
        {
            get
            {
                var startupPower = startupPowerMultiplier * LaserPowerRequirements;

                if (startupCostGravityMultiplier <= 0) return startupPower;

                gravityDivider = GravityDivider;
                startupPower = gravityDivider > 0 ? startupPower / gravityDivider : startupPower;

                return startupPower;
            }
        }

        public override bool ShouldScaleDownJetISP()
        {
            return !isupgraded;
        }

        public override void Update()
        {
            base.Update();

            isChargingField.guiActive = !IsEnabled && HighLogic.LoadedSceneIsFlight && canChargeJumpStart && part.vessel.geeForce < startupMaximumGeforce;
            isChargingField.guiActiveEditor = false;
        }

        public override void OnUpdate()
        {
            if (isChargingField.guiActive)
                accumulatedChargeStr = PluginHelper.GetFormattedPowerString(accumulatedElectricChargeInMW) + " / " + PluginHelper.GetFormattedPowerString(StartupPower);
            else if (part.vessel.geeForce > startupMaximumGeforce)
                accumulatedChargeStr = part.vessel.geeForce.ToString("F2") + "g > " + startupMaximumGeforce.ToString("F2") + "g";
            else
                accumulatedChargeStr = string.Empty;

            accumulatedChargeStrField.guiActive = plasma_ratio < 1;

            electricPowerMaintenance = PluginHelper.GetFormattedPowerString(_powerConsumed) + " / " + PluginHelper.GetFormattedPowerString(LaserPowerRequirements);

            if (StartupAnimation != null && !Initialized)
            {
                if (IsEnabled)
                {
                    if (AnimationStarted == 0)
                    {
                        StartupAnimation.ToggleAction(new KSPActionParam(KSPActionGroup.Custom01, KSPActionType.Activate));
                        AnimationStarted = Planetarium.GetUniversalTime();
                    }
                    else if (!StartupAnimation.IsMoving())
                    {
                        StartupAnimation.ToggleAction(new KSPActionParam(KSPActionGroup.Custom01, KSPActionType.Deactivate));
                        AnimationStarted = 0;
                        Initialized = true;
                        isDeployed = true;
                    }
                }
                else // Not Enabled
                {
                    // continuously start
                    StartupAnimation.ToggleAction(new KSPActionParam(KSPActionGroup.Custom01, KSPActionType.Activate));
                    StartupAnimation.ToggleAction(new KSPActionParam(KSPActionGroup.Custom01, KSPActionType.Deactivate));
                }
            }
            else if (StartupAnimation == null)
            {
                isDeployed = true;
            }

            // call base class
            base.OnUpdate();
        }

        public new void KITFixedUpdate(IResourceManager resMan)
        {
            base.KITFixedUpdate(resMan);

            UpdateLoopingAnimation(ongoing_consumption_rate * powerPercentage / 100);

            if (!IsEnabled && !isChargingForJumpStart)
            {
                plasma_ratio = 0;
                _powerConsumed = 0;
                allowJumpStart = false;
                if (accumulatedElectricChargeInMW > 0)
                    accumulatedElectricChargeInMW -= 0.01 * accumulatedElectricChargeInMW;
                return;
            }

            ProcessCharging(resMan);

            // quit if no fuel available
            if (stored_fuel_ratio <= 0.01)
            {
                plasma_ratio = 0;
                return;
            }

            var powerRequested = LaserPowerRequirements * required_reactor_ratio;

            double primaryPowerReceived = 0;
            if (powerRequested > 0)
            {
                primaryPowerReceived = resMan.Consume(ResourceName.ElectricCharge, powerRequested);

                if (maintenancePowerWasteheatRatio > 0)
                    resMan.Produce(ResourceName.WasteHeat, maintenancePowerWasteheatRatio * primaryPowerReceived);
            }

            // calculate effective primary power ratio
            var powerReceived = primaryPowerReceived;
            var powerRequirementMetRatio = powerRequested > 0 ? powerReceived / powerRequested : 1;

            // adjust power to optimal power
            _powerConsumed = LaserPowerRequirements * powerRequirementMetRatio;

            // verify if we need startup with accumulated power
            if (CanJumpStart && accumulatedElectricChargeInMW > 0 && _powerConsumed < StartupPower && (accumulatedElectricChargeInMW + _powerConsumed) >= StartupPower)
            {
                var shortage = StartupPower - _powerConsumed;
                if (shortage <= accumulatedElectricChargeInMW)
                {
                    //ScreenMessages.PostScreenMessage("Attempting to Jump start", 5.0f, ScreenMessageStyle.LOWER_CENTER);
                    _powerConsumed += accumulatedElectricChargeInMW;
                    accumulatedElectricChargeInMW -= shortage;
                    jumpStartPowerTime = 50;
                }
            }

            if (isSwappingFuelMode)
            {
                plasma_ratio = 1;
                isSwappingFuelMode = false;
            }
            else if (jumpStartPowerTime > 0)
            {
                plasma_ratio = 1;
                jumpStartPowerTime--;
            }
            else if (_framesPlasmaRatioIsGood > 0) // maintain reactor
            {
                plasma_ratio = Math.Round(LaserPowerRequirements > 0 ? _powerConsumed / LaserPowerRequirements : 1, 4);
                allowJumpStart = plasma_ratio >= 1;
            }
            else  // starting reactor
            {
                plasma_ratio = Math.Round(StartupPower > 0 ? _powerConsumed / StartupPower : 1, 4);
                allowJumpStart = plasma_ratio >= 1;
            }

            if (plasma_ratio > 0.999)
            {
                plasma_ratio = 1;
                isChargingForJumpStart = false;
                IsEnabled = true;
                if (_framesPlasmaRatioIsGood < 100)
                    _framesPlasmaRatioIsGood += 1;
                if (_framesPlasmaRatioIsGood > 10)
                    accumulatedElectricChargeInMW = 0;
            }
            else
            {
                var threshold = 10 * (1 - plasma_ratio);
                if (_framesPlasmaRatioIsGood >= threshold)
                {
                    _framesPlasmaRatioIsGood -= threshold;
                    plasma_ratio = 1;
                }
            }
        }

        private void UpdateLoopingAnimation(double ratio)
        {
            if (LoopingAnimation == null)
                return;

            if (!isDeployed)
                return;

            if (!IsEnabled)
            {
                if (!Initialized || ShutdownAnimation == null || LoopingAnimation.IsMoving()) return;

                if (AnimationStarted < 0)
                {
                    AnimationStarted = Planetarium.GetUniversalTime();
                    ShutdownAnimation.ToggleAction(new KSPActionParam(KSPActionGroup.Custom01, KSPActionType.Activate));
                }
                else if (!ShutdownAnimation.IsMoving())
                {
                    ShutdownAnimation.ToggleAction(new KSPActionParam(KSPActionGroup.Custom01, KSPActionType.Deactivate));
                    Initialized = false;
                    isDeployed = true;
                }
                return;
            }

            if (!LoopingAnimation.IsMoving())
                LoopingAnimation.Toggle();
        }

        private void ProcessCharging(IResourceManager resMan)
        {
            if (!CanJumpStart || !isChargingForJumpStart || (part.vessel.geeForce >= startupMaximumGeforce)) return;

            var neededPower = Math.Max(StartupPower - accumulatedElectricChargeInMW, 0);

            if (neededPower <= 0)
                return;

            var minimumChargingPower = startupMinimumChargePercentage * RawPowerOutput;
            if (startupCostGravityMultiplier > 0)
            {
                gravityDivider = GravityDivider;
                minimumChargingPower = gravityDivider > 0 ? minimumChargingPower / gravityDivider : minimumChargingPower;
            }

            var availableStablePower = resMan.Consume(ResourceName.ElectricCharge, neededPower);
            resMan.Produce(ResourceName.WasteHeat, 0.05 * availableStablePower);
            accumulatedElectricChargeInMW += availableStablePower;

            if (availableStablePower < minimumChargingPower)
            {
                if (startupCostGravityMultiplier > 0)
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_InertialConfinementReactor_PostMsg1", minimumChargingPower.ToString("F0")), 1f, ScreenMessageStyle.UPPER_CENTER);//"Curent you need at least " +  + " MW to charge the reactor. Move closer to gravity well to reduce amount needed"
                else
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_InertialConfinementReactor_PostMsg2", minimumChargingPower.ToString("F0")), 5f, ScreenMessageStyle.UPPER_CENTER);//"You need at least " +  + " MW to charge the reactor"
            }
        }
    }
}
