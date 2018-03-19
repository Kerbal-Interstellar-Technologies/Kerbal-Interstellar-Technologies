﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FNPlugin.Extensions;

namespace FNPlugin 
{
    class ElectricRCSController : ResourceSuppliableModule 
    {
        [KSPField(isPersistant = true)]
        bool isInitialised = false;
        [KSPField(isPersistant = true)]
        public int fuel_mode;
        [KSPField(isPersistant = true)]
        public string fuel_mode_name;
        [KSPField(isPersistant = false)]
        public string AnimationName = "";
        [KSPField(isPersistant = false)]
        public double efficiency = 0.8;
        [KSPField(isPersistant = false)]
        public int type = 16;
        [KSPField(isPersistant = false)]
        public float maxThrust = 1;
        [KSPField(isPersistant = false)]
        public float maxIsp = 2000;
        [KSPField(isPersistant = false)]
        public float minIsp = 250;
        [KSPField(isPersistant = false)]
        public string displayName = "";
        [KSPField(isPersistant = false)]
        public bool showConsumption = true;
        [KSPField(isPersistant = false)]
        public double powerMult = 1;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Full Thrust", advancedTweakable = true), UI_Toggle(disabledText = "Off", enabledText = "On", affectSymCounterparts = UI_Scene.All)]
        public bool fullThrustEnabled;
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "FT Threshold", guiUnits = "%", advancedTweakable = true), UI_FloatRange(stepIncrement = 1f, maxValue = 100, minValue = 0, affectSymCounterparts = UI_Scene.All)]
        public float fullThrustMinLimiter;
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Use Throttle"), UI_Toggle(disabledText = "Off", enabledText = "On", affectSymCounterparts = UI_Scene.All)]
        public bool useThrotleEnabled;
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Use Lever", advancedTweakable = true), UI_Toggle(disabledText = "Off", enabledText = "On", affectSymCounterparts = UI_Scene.All)]
        public bool useLeverEnabled;
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Precision", advancedTweakable = true), UI_FloatRange(stepIncrement = 1f, maxValue = 100, minValue = 5, affectSymCounterparts = UI_Scene.All)]
        public float precisionFactorLimiter;
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Power", advancedTweakable = true), UI_Toggle(disabledText = "Off", enabledText = "On", affectSymCounterparts = UI_Scene.All)]
        public bool powerEnabled = true;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "Propellant Name")]
        public string propNameStr = "";
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = false, guiName = "Propellant Maximum Isp")]
        public float maxPropellantIsp;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = false, guiName = "Propellant Thrust Multiplier")]
        public double currentThrustMultiplier = 1;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Thrust / ISP Mult")]
        public string thrustIspMultiplier = "";
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Thrust Limiter", advancedTweakable = true), UI_FloatRange(stepIncrement = 0.05f, maxValue = 100, minValue = 5, affectSymCounterparts = UI_Scene.All)]
        public float thrustLimiter = 100;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Base Thrust", guiUnits = " kN")]
        public float baseThrust = 0;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "Max Thrust", guiUnits = " kN")]
        public float thrustStr;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = true, guiName = "Forces")]
        public string thrustForcesStr;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = true, guiName = "Current Total Thrust", guiUnits = " kN")]
        public float currentThrust;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = false, guiName = "Mass", guiUnits = " t")]
        public float partMass = 0;

        //Config settings settings
        protected double g0 = PluginHelper.GravityConstant;

        // GUI
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = true, guiName = "Is Powered")]
        public bool hasSufficientPower = true;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Consumption")]
        public string electricalPowerConsumptionStr = "";
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "Efficiency")]
        public string efficiencyStr = "";

        // internal
        private AnimationState[] rcsStates;
        private bool rcsIsOn;
        private bool rcsPartActive;

        private PartResourceDefinition definitionMegajoule;

        private double power_ratio = 1;
        private double power_requested_f = 0;
        private double power_recieved_f = 1;

        private double heat_production_f = 0;
        private List<ElectricEnginePropellant> _propellants;
        private ModuleRCS attachedRCS;
        private FNModuleRCSFX attachedModuleRCSFX;
        private float oldThrustLimiter;
        private bool oldPowerEnabled;
        private int insufficientPowerTimout = 2;
        private bool delayedVerificationPropellant;

        public ElectricEnginePropellant Current_propellant { get; set; }

        [KSPAction("Next Propellant")]
        public void ToggleNextPropellantAction(KSPActionParam param)
        {
            ToggleNextPropellantEvent();
        }

        [KSPAction("Previous Propellant")]
        public void TogglePreviousPropellantAction(KSPActionParam param)
        {
            TogglePreviousPropellantEvent();
        }

        [KSPEvent(guiActiveEditor = true, guiActive = true, guiName = "Next Propellant", active = true)]
        public void ToggleNextPropellantEvent()
        {
            SwitchToNextPropellant(_propellants.Count);
        }

        [KSPEvent(guiActiveEditor = true, guiActive = true, guiName = "Previous Propellant", active = true)]
        public void TogglePreviousPropellantEvent()
        {
            SwitchToPreviousPropellant(_propellants.Count);
        }

        protected void SwitchPropellant(bool next, int maxSwitching)
        {
            if (next)
                SwitchToNextPropellant(maxSwitching);
            else
                SwitchToPreviousPropellant(maxSwitching);
        }

        protected void SwitchToNextPropellant(int maxSwitching)
        {
            fuel_mode++;
            if (fuel_mode >= _propellants.Count)
                fuel_mode = 0;

            SetupPropellants(true, maxSwitching);
        }

        protected void SwitchToPreviousPropellant(int maxSwitching)
        {
            fuel_mode--;
            if (fuel_mode < 0)
                fuel_mode = _propellants.Count - 1;

            SetupPropellants(false, maxSwitching);
        }

        private void SetupPropellants(bool moveNext = true, int maxSwitching = 0)
        {
            Current_propellant = fuel_mode < _propellants.Count ? _propellants[fuel_mode] : _propellants.FirstOrDefault();
            fuel_mode_name = Current_propellant.PropellantName;

            if ((Current_propellant.SupportedEngines & type) != type)
            {
                SwitchPropellant(moveNext, --maxSwitching);
                return;
            }

            Propellant new_propellant = Current_propellant.Propellant;
            if (HighLogic.LoadedSceneIsFlight)
            {
                // you can have any fuel you want in the editor but not in flight
                var totalpartresources = part.GetConnectedResources(new_propellant.name).ToList();
                if (!totalpartresources.Any() && maxSwitching > 0)
                {
                    SwitchPropellant(moveNext, --maxSwitching);
                    return;
                }
            }

            if (PartResourceLibrary.Instance.GetDefinition(new_propellant.name) != null)
            {
                var effectiveIspMultiplier = type == 2 ? Current_propellant.DecomposedIspMult : Current_propellant.IspMultiplier;

                //var effectiveThrust = (thrustLimiter / 100) * Current_propellant.ThrustMultiplier * baseThrust / effectiveIspMultiplier;

                var moduleConfig = new ConfigNode("MODULE");
                moduleConfig.AddValue("name", "FNModuleRCSFX");
                moduleConfig.AddValue("thrusterPower", attachedRCS.thrusterPower.ToString("0.000"));
                moduleConfig.AddValue("resourceName", new_propellant.name);
                moduleConfig.AddValue("resourceFlowMode", "STAGE_PRIORITY_FLOW");

                currentThrustMultiplier = hasSufficientPower ? Current_propellant.ThrustMultiplier : Current_propellant.ThrustMultiplierCold;

                //var effectiveThrustModifier = currentThrustMultiplier * (currentThrustMultiplier / Current_propellant.ThrustMultiplier);

                var effectiveBaseIsp = hasSufficientPower ? maxIsp : minIsp;

                maxPropellantIsp = (float)(effectiveBaseIsp * effectiveIspMultiplier * currentThrustMultiplier);

                var atmosphereCurve = new ConfigNode("atmosphereCurve");
                atmosphereCurve.AddValue("key", "0 " + (maxPropellantIsp).ToString("0.000"));
                atmosphereCurve.AddValue("key", "1 " + (maxPropellantIsp * 0.5).ToString("0.000"));
                atmosphereCurve.AddValue("key", "4 " + (maxPropellantIsp * 0.00001).ToString("0.000"));
                moduleConfig.AddNode(atmosphereCurve);

                attachedRCS.Load(moduleConfig);
            }
            else if (maxSwitching > 0)
            {
                Debug.Log("ElectricRCSController SetupPropellants switching mode because no definition found for " + new_propellant.name);
                SwitchPropellant(moveNext, --maxSwitching);
                return;
            }
        }

        [KSPAction("Toggle Yaw")]
        public void ToggleYawAction(KSPActionParam param)
        {
            if (attachedModuleRCSFX != null)
                attachedModuleRCSFX.enableYaw = !attachedModuleRCSFX.enableYaw;
        }

        [KSPAction("Toggle Pitch")]
        public void TogglePitchAction(KSPActionParam param)
        {
            if (attachedModuleRCSFX != null)
                attachedModuleRCSFX.enablePitch = !attachedModuleRCSFX.enablePitch;
        }

        [KSPAction("Toggle Roll")]
        public void ToggleRollAction(KSPActionParam param)
        {
            if (attachedModuleRCSFX != null)
                attachedModuleRCSFX.enableRoll = !attachedModuleRCSFX.enableRoll;
        }

        [KSPAction("Toggle Enable X")]
        public void ToggleEnableXAction(KSPActionParam param)
        {
            if (attachedModuleRCSFX != null)
                attachedModuleRCSFX.enableX = !attachedModuleRCSFX.enableX;
        }

        [KSPAction("Toggle Enable Y")]
        public void ToggleEnableYAction(KSPActionParam param)
        {
            if (attachedModuleRCSFX != null)
                attachedModuleRCSFX.enableY = !attachedModuleRCSFX.enableY;
        }

        [KSPAction("Toggle Enable Z")]
        public void ToggleEnableZAction(KSPActionParam param)
        {
            if (attachedModuleRCSFX != null)
                attachedModuleRCSFX.enableZ = !attachedModuleRCSFX.enableZ;
        }

        [KSPAction("Toggle Full Thrust")]
        public void ToggleFullThrustAction(KSPActionParam param)
        {
            fullThrustEnabled = !fullThrustEnabled;
            if (attachedModuleRCSFX != null)
                attachedModuleRCSFX.fullThrust = fullThrustEnabled;
        }

        [KSPAction("Toggle Use Throtle")]
        public void ToggleUseThrotleEnabledAction(KSPActionParam param)
        {
            useThrotleEnabled = !useThrotleEnabled;
            if (attachedModuleRCSFX != null)
                attachedModuleRCSFX.useThrottle = useThrotleEnabled;
        }

        [KSPAction("Toggle Use Lever")]
        public void ToggleUseLeverAction(KSPActionParam param)
        {
            useLeverEnabled = !useLeverEnabled;
            if (attachedModuleRCSFX != null)
                attachedModuleRCSFX.useLever = useLeverEnabled;
        }

        [KSPAction("Toggle Power")]
        public void TogglePowerAction(KSPActionParam param)
        {
            powerEnabled = !powerEnabled;

            power_recieved_f = powerEnabled ? CheatOptions.InfiniteElectricity ? 1 : consumeFNResourcePerSecond(0.1, ResourceManager.FNRESOURCE_MEGAJOULES) : 0;
            hasSufficientPower = power_recieved_f >= 0.09;
            SetupPropellants();
            currentThrustMultiplier = hasSufficientPower ? Current_propellant.ThrustMultiplier : Current_propellant.ThrustMultiplierCold;
        }

        public override void OnStart(PartModule.StartState state) 
        {
            definitionMegajoule = PartResourceLibrary.Instance.GetDefinition(ResourceManager.FNRESOURCE_MEGAJOULES);

            try
            {
                attachedRCS = this.part.FindModuleImplementing<ModuleRCS>();
                attachedModuleRCSFX = attachedRCS as FNModuleRCSFX;

                if (!isInitialised)
                {
                    precisionFactorLimiter = attachedRCS.precisionFactor * 100;
                    fullThrustMinLimiter = attachedRCS.fullThrustMin * 100;
                    useThrotleEnabled = attachedRCS.useThrottle;
                    fullThrustEnabled = attachedRCS.fullThrust;
                     useLeverEnabled = attachedRCS.useLever;
                }

                if (attachedModuleRCSFX != null)
                {
                    attachedModuleRCSFX.Fields["RCS"].guiActive = true;
                    attachedModuleRCSFX.Fields["enableYaw"].guiActive = true;
                    attachedModuleRCSFX.Fields["enablePitch"].guiActive = true;
                    attachedModuleRCSFX.Fields["enableRoll"].guiActive = true;
                    attachedModuleRCSFX.Fields["enableX"].guiActive = true;
                    attachedModuleRCSFX.Fields["enableY"].guiActive = true;
                    attachedModuleRCSFX.Fields["enableZ"].guiActive = true;
                }

                attachedRCS.precisionFactor = precisionFactorLimiter / 100;
                attachedRCS.fullThrustMin = fullThrustMinLimiter / 100;
                attachedRCS.useThrottle = useThrotleEnabled;
                attachedRCS.fullThrust = fullThrustEnabled;                
                attachedRCS.useLever = useLeverEnabled;

                // old legacy stuff
                if (baseThrust == 0 && maxThrust > 0)
                    baseThrust = maxThrust;

                if (partMass == 0)
                    partMass = part.mass;

                if (String.IsNullOrEmpty(displayName))
                    displayName = part.partInfo.title;

                String[] resources_to_supply = { ResourceManager.FNRESOURCE_WASTEHEAT };
                this.resources_to_supply = resources_to_supply;

                oldThrustLimiter = thrustLimiter;
                oldPowerEnabled = powerEnabled;
                //efficiencyModifier = g0 * 0.5 / 1000 / efficiency;
                efficiencyStr = (efficiency * 100).ToString() + "%";

                if (!String.IsNullOrEmpty(AnimationName))
                    rcsStates = SetUpAnimation(AnimationName, this.part);

                // initialize propellant
                _propellants = ElectricEnginePropellant.GetPropellantsEngineForType(type);

                delayedVerificationPropellant = true;
                // find correct fuel mode index
                if (!String.IsNullOrEmpty(fuel_mode_name))
                {
                    Debug.Log("[KSPI] - ElectricRCSController OnStart loaded fuelmode " + fuel_mode_name);
                    Current_propellant = _propellants.FirstOrDefault(p => p.PropellantName == fuel_mode_name);
                }
                if (Current_propellant != null && _propellants.Contains(Current_propellant))
                {
                    fuel_mode = _propellants.IndexOf(Current_propellant);
                    Debug.Log("[KSPI] - ElectricRCSController OnStart index of fuelmode " + Current_propellant.PropellantGUIName + " = " + fuel_mode);
                }

                base.OnStart(state);

                Fields["electricalPowerConsumptionStr"].guiActive = showConsumption;
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI] - ElectricRCSController OnStart Error: " + e.Message);
                throw;
            }
         }

        public void Update()
        {
            if (Current_propellant == null) return;

            if (oldThrustLimiter != thrustLimiter)
            {
                SetupPropellants(true, 0);
                oldThrustLimiter = thrustLimiter;
            }

            if (oldPowerEnabled != powerEnabled)
            {
                hasSufficientPower = powerEnabled;
                SetupPropellants(true, 0);
                oldPowerEnabled = powerEnabled;
            }

            attachedRCS.precisionFactor = precisionFactorLimiter / 100;
            attachedRCS.fullThrustMin = fullThrustMinLimiter / 100;
            attachedRCS.useThrottle = useThrotleEnabled;
            attachedRCS.fullThrust = fullThrustEnabled;
            attachedRCS.useLever = useLeverEnabled;

            propNameStr = Current_propellant.PropellantGUIName;

            thrustStr = attachedRCS.thrusterPower;

            thrustIspMultiplier = maxPropellantIsp + "s / " + currentThrustMultiplier;
        }

        public override void OnUpdate() 
        {
            if (delayedVerificationPropellant)
            {
                // test is we got any megajoules
                power_recieved_f = CheatOptions.InfiniteElectricity ? 1 : consumeFNResourcePerSecond(powerMult, ResourceManager.FNRESOURCE_MEGAJOULES);
                hasSufficientPower = power_recieved_f > powerMult * 0.99;

                // return test power
                if (!CheatOptions.InfiniteElectricity && power_recieved_f > 0)
                    part.RequestResource(definitionMegajoule.id, -power_recieved_f);

                delayedVerificationPropellant = false;
                SetupPropellants(true, _propellants.Count);
                currentThrustMultiplier = hasSufficientPower ? Current_propellant.ThrustMultiplier : Current_propellant.ThrustMultiplierCold;
            }

            if (attachedRCS != null && vessel.ActionGroups[KSPActionGroup.RCS]) 
            {
                Fields["electricalPowerConsumptionStr"].guiActive = true;
                electricalPowerConsumptionStr = power_recieved_f.ToString("0.00") + " MW / " + power_requested_f.ToString("0.00") + " MW";
            } 
            else 
                Fields["electricalPowerConsumptionStr"].guiActive = false;

            if (rcsStates == null) return;

            rcsIsOn = this.vessel.ActionGroups.groups[3];
            foreach (ModuleRCS rcs in part.FindModulesImplementing<ModuleRCS>())
            {
                rcsPartActive = rcs.isEnabled;
            }

            foreach (AnimationState anim in rcsStates)
            {
                if (attachedRCS.rcsEnabled && rcsIsOn && rcsPartActive && anim.normalizedTime < 1) { anim.speed = 1; }
                if (attachedRCS.rcsEnabled && rcsIsOn && rcsPartActive && anim.normalizedTime >= 1)
                {
                    anim.speed = 0;
                    anim.normalizedTime = 1;
                }
                if ((!attachedRCS.rcsEnabled || !rcsIsOn || !rcsPartActive) && anim.normalizedTime > 0) { anim.speed = -1; }
                if ((!attachedRCS.rcsEnabled || !rcsIsOn || !rcsPartActive) && anim.normalizedTime <= 0)
                {
                    anim.speed = 0;
                    anim.normalizedTime = 0;
                }
            }
        }


        /// <summary>
        /// FixedUpdate is also called when not activated
        /// </summary>
        public void FixedUpdate()
        {
            if (attachedRCS == null) return;

            if (!HighLogic.LoadedSceneIsFlight) return;

            currentThrust = 0;

            currentThrust = attachedModuleRCSFX != null ? attachedModuleRCSFX.curThrust : attachedRCS.thrustForces.Sum(frc => frc);

            thrustForcesStr = String.Empty;

            if (!vessel.ActionGroups[KSPActionGroup.RCS]) return;

            foreach (var force in attachedRCS.thrustForces)
            {
                thrustForcesStr += force.ToString("0.00") + "kN ";
            }           

            if (powerEnabled && currentThrust > 0)
            {
                power_requested_f = 0.5 * powerMult * currentThrust * maxIsp * 9.81 / efficiency / 1000 / Current_propellant.ThrustMultiplier;

                if (CheatOptions.InfiniteElectricity)
                    power_recieved_f = power_requested_f;
                else
                {
                    var avaialablePower = getAvailableResourceSupply(ResourceManager.FNRESOURCE_MEGAJOULES);
                    power_recieved_f = avaialablePower >= power_requested_f 
                        ? consumeFNResourcePerSecond(power_requested_f, ResourceManager.FNRESOURCE_MEGAJOULES) 
                        : 0;
                }

                double heat_to_produce = power_recieved_f * (1 - efficiency);

                heat_production_f = CheatOptions.IgnoreMaxTemperature 
                    ? heat_to_produce
                    : supplyFNResourcePerSecond(heat_to_produce, ResourceManager.FNRESOURCE_WASTEHEAT);

                power_ratio = power_requested_f > 0 ? Math.Min(power_recieved_f / power_requested_f, 1.0) : 1;
            }
            else
            {
                power_recieved_f = 0;
                power_ratio = 0;
                insufficientPowerTimout = 0;
            }

            if (hasSufficientPower && power_ratio <= 0.9 && power_recieved_f <= 0.01 )
            {
                if (insufficientPowerTimout < 1)
                {
                    hasSufficientPower = false;
                    SetupPropellants();
                }
                else
                    insufficientPowerTimout--;
            }
            else if (!hasSufficientPower && power_ratio > 0.9 && power_recieved_f > 0.01)
            {
                insufficientPowerTimout = 2;
                hasSufficientPower = true;
                SetupPropellants();
            }

            // return any unused power
            if (!hasSufficientPower && power_recieved_f > 0)
                part.RequestResource(definitionMegajoule.id, -power_recieved_f * TimeWarp.fixedDeltaTime);
        }

        public static AnimationState[] SetUpAnimation(string animationName, Part part)  //Thanks Majiir!
        {
            var states = new List<AnimationState>();
            foreach (var animation in part.FindModelAnimators(animationName))
            {
                var animationState = animation[animationName];
                animationState.speed = 0;
                animationState.enabled = true;
                animationState.wrapMode = WrapMode.ClampForever;
                animation.Blend(animationName);
                states.Add(animationState);
            }
            return states.ToArray();
        }

        public override string getResourceManagerDisplayName() 
        {
            return part.partInfo.title + " (" + propNameStr + ")";
        }
        public override int getPowerPriority()
        {
            return 3;
        }
    }
}
