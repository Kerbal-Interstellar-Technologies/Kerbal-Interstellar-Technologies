using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FNPlugin 
{
    [KSPModule("Radiator")]
    class StackFNRadiator : FNRadiator { }

    [KSPModule("Radiator")]
    class FlatFNRadiator : FNRadiator { }

    [KSPModule("Radiator")]
	class FNRadiator : FNResourceSuppliableModule	
    {
        // persitant
		[KSPField(isPersistant = true)]
		public bool radiatorIsEnabled;
        [KSPField(isPersistant = true)]
        public bool isupgraded;
        [KSPField(isPersistant = true)]
        public bool radiatorInit;
        [KSPField(isPersistant = true)]
        public bool isAutomated = true;

        [KSPField(isPersistant = false)]
        public bool showColorHeat = true;
        [KSPField(isPersistant = true)]
        public bool showRetractButton = false;

        [KSPField(isPersistant = false)]
        public float radiatorTemperatureMk1 = 1850;
        [KSPField(isPersistant = false)]
        public float radiatorTemperatureMk2 = 2200;
        [KSPField(isPersistant = false)]
        public float radiatorTemperatureMk3 = 2616;
        [KSPField(isPersistant = false)]
        public float radiatorTemperatureMk4 = 3111;
        [KSPField(isPersistant = false)]
        public float radiatorTemperatureMk5 = 3700;

        [KSPField(isPersistant = false)]
        public string radiatorTypeMk1 = "NaK Loop Radiator";
        [KSPField(isPersistant = false)]
        public string radiatorTypeMk2 = "Mo Li Heat Pipe Mk1";
        [KSPField(isPersistant = false)]
        public string radiatorTypeMk3 = "Mo Li Heat Pipe Mk2";
        [KSPField(isPersistant = false)]
        public string radiatorTypeMk4 = "Graphene Radiator Mk1";
        [KSPField(isPersistant = false)]
        public string radiatorTypeMk5 = "Graphene Radiator Mk2";

        [KSPField(isPersistant = false, guiActive = false)]
        public string upgradeTechReqMk2 = null;
        [KSPField(isPersistant = false, guiActive = false)]
        public string upgradeTechReqMk3 = null;
        [KSPField(isPersistant = false, guiActive = false)]
        public string upgradeTechReqMk4 = null;
        [KSPField(isPersistant = false, guiActive = false)]
        public string upgradeTechReqMk5 = null;

        [KSPField(isPersistant = false, guiActive = false)]
        public string surfaceAreaUpgradeTechReq = null;
        [KSPField(isPersistant = false, guiActive = false)]
        public float surfaceAreaUpgradeMult = 1.6f;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Color Ratio")]
        public float colorRatio;

        // non persistant
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Mass", guiUnits = " t")]
        public float partMass;
		[KSPField(isPersistant = false)]
		public bool isDeployable = false;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiName = "Converction Bonus")]
		public float convectiveBonus = 1.0f;
		[KSPField(isPersistant = false)]
		public string animName;
        [KSPField(isPersistant = false)]
        public string thermalAnim;
		[KSPField(isPersistant = false)]
		public string originalName;
		[KSPField(isPersistant = false)]
		public float upgradeCost = 100;
        [KSPField(isPersistant = false)]
        public float temperatureColorDivider = 1;
        [KSPField(isPersistant = false)]
        public float emissiveColorPower = 6;
        [KSPField(isPersistant = false)]
		public string upgradedName;
        [KSPField(isPersistant = false)]
        public float wasteHeatMultiplier = 1;
        [KSPField(isPersistant = false)]
        public string colorHeat = "_EmissiveColor";
        [KSPField(isPersistant = false, guiActive = false, guiName = "Pressure Load", guiFormat= "F2", guiUnits = "%")]
        public float pressureLoad;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Type")]
		public string radiatorType;
		[KSPField(isPersistant = false, guiActive = true, guiName = "Rad Temp")]
		public string radiatorTempStr;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Part Temp")]
        public string partTempStr;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Surface Area", guiFormat = "F2", guiUnits = " m2")]
        public double radiatorArea = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false)]
        public float areaMultiplier = 4;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false)]
        public float radiativeAreaFraction = 1;

        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Effective Area", guiFormat = "F2", guiUnits = " m2")]
        public double effectiveRadiatorArea;
		[KSPField(isPersistant = false, guiActive = true, guiName = "Power Radiated")]
		public string thermalPowerDissipStr;
		[KSPField(isPersistant = false, guiActive = true, guiName = "Power Convected")]
		public string thermalPowerConvStr;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Rad Upgrade Cost")]
		public string upgradeCostStr;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Radiator Start Temp")]
        public float radiator_temperature_temp_val;
        [KSPField(isPersistant = false, guiActive = false)]
        public float instantaneous_rad_temp;
        [KSPField(isPersistant = false, guiActive = false, guiName = "WasteHeat Ratio")]
        public float wasteheatRatio;

        //[KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Called")]
        //public int callCounter;


        //[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Core Cooling"), UI_Toggle(disabledText = "Off", enabledText = "On")]
        //public bool isCoreRadiator = false;
        //[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Global Cooling"), UI_Toggle(disabledText = "Off", enabledText = "On")]
        //public bool globalCooling = true;

        const float rad_const_h = 1000;
        const String kspShader = "KSP/Emissive/Bumped Specular";

        //[KSPField(isPersistant = false, guiActive = true, guiName = "Overcooling")]
        //public float overcooling = 0.25f;
        //[KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Part cooling", guiFormat = "F6")]
        //public double currentCooling;
        //[KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Radiator heating", guiFormat = "F6")]
        //public double currentHeating;
        //[KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Total cooling", guiFormat = "F6")]
        //public double totalCooling = 0;

        //[KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Skin Exposed Area", guiFormat = "F6")]
        //public double skinExposedArea;
        //[KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Radiative Area", guiFormat = "F6")]
        //public double radiativeArea;

        //[KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Cooling Target")]
        //public string hottestPartName;
        //[KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Surface temperature", guiFormat = "F6")]
        //public double hottestPartSkinTemperature;
        //[KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Internal temperature", guiFormat = "F6")]
        //public double hottestPartInternalTemperature;

        //private List<Part> attachedParts;
        //private Part hottestPart;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Max Energy Transfer", guiFormat = "F1")]
        private double _maxEnergyTransfer;

		protected Animation deployAnim;
		protected float radiatedThermalPower;
		protected float convectedThermalPower;
		protected float current_rad_temp;
		protected float directionrotate = 1;
		protected Vector3 original_eulers;
		protected Transform pivot;
		protected long last_draw_update = 0;
        protected long update_count = 0;
		protected bool hasrequiredupgrade;
		protected int explode_counter = 0;
        protected int nrAvailableUpgradeTechs;
        
        private Renderer[] array;
        private AnimationState[] heatStates;
        private ModuleDeployableRadiator _moduleDeployableRadiator;
        private ModuleActiveRadiator _moduleActiveRadiator;

        private bool hasSurfaceAreaUpgradeTechReq;
        private List<IThermalSource> list_of_thermal_sources;

        private static List<FNRadiator> list_of_all_radiators = new List<FNRadiator>();

        public GenerationType CurrentGenerationType { get; private set; }

        public float RadiatorTemperature
        {
            get
            {
                if (CurrentGenerationType == GenerationType.Mk5)
                    return radiatorTemperatureMk5;
                else if (CurrentGenerationType == GenerationType.Mk4)
                    return radiatorTemperatureMk4;
                else if (CurrentGenerationType == GenerationType.Mk3)
                    return radiatorTemperatureMk3;
                else if (CurrentGenerationType == GenerationType.Mk2)
                    return radiatorTemperatureMk2;
                else
                    return radiatorTemperatureMk1;
            }
        }

        public double EffectiveRadiatorArea
        {
            get 
            {
                if (HighLogic.LoadedSceneIsFlight)
                    radiatorArea = part.radiativeArea * radiativeAreaFraction;

                var baseRadiatorArea = radiatorArea * areaMultiplier;
                return hasSurfaceAreaUpgradeTechReq ? baseRadiatorArea * surfaceAreaUpgradeMult : baseRadiatorArea; 
            }
        }

        private void DetermineGenerationType()
        {
            // check if we have SurfaceAreaUpgradeTechReq 
            hasSurfaceAreaUpgradeTechReq = PluginHelper.upgradeAvailable(surfaceAreaUpgradeTechReq);

            // determine number of upgrade techs
            nrAvailableUpgradeTechs = 1;
            if (PluginHelper.upgradeAvailable(upgradeTechReqMk5))
                nrAvailableUpgradeTechs++;
            if (PluginHelper.upgradeAvailable(upgradeTechReqMk4))
                nrAvailableUpgradeTechs++;
            if (PluginHelper.upgradeAvailable(upgradeTechReqMk3))
                nrAvailableUpgradeTechs++;
            if (PluginHelper.upgradeAvailable(upgradeTechReqMk2))
                nrAvailableUpgradeTechs++;

            // determine fusion tech levels
            if (nrAvailableUpgradeTechs == 5)
                CurrentGenerationType = GenerationType.Mk5;
            else if (nrAvailableUpgradeTechs == 4)
                CurrentGenerationType = GenerationType.Mk4;
            else if (nrAvailableUpgradeTechs == 3)
                CurrentGenerationType = GenerationType.Mk3;
            else if (nrAvailableUpgradeTechs == 2)
                CurrentGenerationType = GenerationType.Mk2;
            else
                CurrentGenerationType = GenerationType.Mk1;
        }

        private string RadiatorType
        {
            get
            {
                if (CurrentGenerationType == GenerationType.Mk5)
                    return radiatorTypeMk5;
                else if (CurrentGenerationType == GenerationType.Mk4)
                    return radiatorTypeMk4;
                else if (CurrentGenerationType == GenerationType.Mk3)
                    return radiatorTypeMk3;
                else if (CurrentGenerationType == GenerationType.Mk2)
                    return radiatorTypeMk2;
                else
                    return radiatorTypeMk1;
            }
        }

		public static List<FNRadiator> getRadiatorsForVessel(Vessel vess) 
        {
			list_of_all_radiators.RemoveAll(item => item == null);

            List<FNRadiator> list_of_radiators_for_vessel = new List<FNRadiator>();
			foreach (FNRadiator radiator in list_of_all_radiators) 
            {
				if (radiator.vessel == vess) 
					list_of_radiators_for_vessel.Add (radiator);
			}
			return list_of_radiators_for_vessel;
		}

		public static bool hasRadiatorsForVessel(Vessel vess) 
        {
			list_of_all_radiators.RemoveAll(item => item == null);

			bool has_radiators = false;
			foreach (FNRadiator radiator in list_of_all_radiators) 
            {
				if (radiator.vessel == vess) 
					has_radiators = true;
			}
			return has_radiators;
		}

		public static float getAverageRadiatorTemperatureForVessel(Vessel vess) 
        {
			list_of_all_radiators.RemoveAll(item => item == null);
			float average_temp = 0;
			float n_radiators = 0;
			foreach (FNRadiator radiator in list_of_all_radiators) 
            {
				if (radiator.vessel == vess) 
                {
					average_temp += radiator.getRadiatorTemperature ();
					n_radiators+=1.0f;
				}
			}

			if (n_radiators > 0) 
				average_temp = average_temp / n_radiators;
			else 
				average_temp = 0;

			return average_temp;
		}

        public static float getAverageMaximumRadiatorTemperatureForVessel(Vessel vess) 
        {
            list_of_all_radiators.RemoveAll(item => item == null);
            float average_temp = 0;
            float n_radiators = 0;

            foreach (FNRadiator radiator in list_of_all_radiators) 
            {
                if (radiator.vessel == vess) 
                {
                    average_temp += radiator.RadiatorTemperature;
                    n_radiators += 1.0f;
                }
            }

            if (n_radiators > 0) 
                average_temp = average_temp / n_radiators;
            else 
                average_temp = 0;

            return average_temp;
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Enable Automation", active = true)]
        public void SwitchAutomation()
        {
            isAutomated = !isAutomated;

            UpdateEnableAutomation();
        }


		[KSPEvent(guiActive = true, guiActiveEditor=true, guiName = "Deploy Radiator", active = true)]
		public void DeployRadiator() 
        {
            isAutomated = false;

            Deploy();
		}

        private void Deploy()
        {
            if (!isDeployable) return;

            if (_moduleDeployableRadiator != null)
                _moduleDeployableRadiator.Extend();

            if (_moduleActiveRadiator != null)
                _moduleActiveRadiator.Activate();

            radiatorIsEnabled = true;

            if (deployAnim == null) return;

            deployAnim[animName].enabled = true;
            deployAnim[animName].speed = 0.5f;
            deployAnim[animName].normalizedTime = 0f;
            deployAnim.Blend(animName, 2f);
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Retract Radiator", active = false)]
		public void RetractRadiator() 
        {
            if (!isDeployable) return;

            isAutomated = false;

            Retract();
		}

        private void Retract()
        {
            if (!isDeployable) return;

            if (_moduleDeployableRadiator != null)
                _moduleDeployableRadiator.Retract();

            if (_moduleActiveRadiator != null)
                _moduleActiveRadiator.Shutdown();

            radiatorIsEnabled = false;

            if (deployAnim == null) return;

            deployAnim[animName].enabled = true;
            deployAnim[animName].speed = -0.5f;
            deployAnim[animName].normalizedTime = 1f;
            deployAnim.Blend(animName, 2f);
        }

		[KSPEvent(guiActive = true, guiName = "Retrofit", active = true)]
		public void RetrofitRadiator() 
        {
			if (ResearchAndDevelopment.Instance == null) { return;} 
			if (isupgraded || ResearchAndDevelopment.Instance.Science < upgradeCost) { return; }

			isupgraded = true;
			//radiatorType = upgradedName;
            radiatorTempStr = RadiatorTemperature + "K";

            ResearchAndDevelopment.Instance.AddScience(-upgradeCost, TransactionReasons.RnDPartPurchase);
		}

        [KSPAction("Switch Automation")]
        public void SwitchAutomationAction(KSPActionParam param)
        {
            SwitchAutomation();
        }

		[KSPAction("Deploy Radiator")]
		public void DeployRadiatorAction(KSPActionParam param) 
        {
            DeployRadiator();
		}

		[KSPAction("Retract Radiator")]
		public void RetractRadiatorAction(KSPActionParam param) 
        {
			RetractRadiator();
		}

		[KSPAction("Toggle Radiator")]
		public void ToggleRadiatorAction(KSPActionParam param) 
        {
			if (radiatorIsEnabled) 
				RetractRadiator();
			else 
				DeployRadiator();
		}

        public override void OnStart(StartState state)
        {
            radiatedThermalPower = 0;
            convectedThermalPower = 0;
            current_rad_temp = 0;
            directionrotate = 1;
            last_draw_update = 0;
            update_count = 0;
            hasrequiredupgrade = false;
            explode_counter = 0;

            DetermineGenerationType();

            if (hasSurfaceAreaUpgradeTechReq)
                part.emissiveConstant = 1.6;

            radiatorType = RadiatorType;

            UpdateEnableAutomation();

            effectiveRadiatorArea = EffectiveRadiatorArea;

            //attachedParts = part.attachNodes.Where(a => a.attachedPart != null).Select(m => m.attachedPart).ToList();

            Actions["DeployRadiatorAction"].guiName = Events["DeployRadiator"].guiName = "Deploy Radiator";
            Actions["ToggleRadiatorAction"].guiName = String.Format("Toggle Radiator");

            Actions["RetractRadiatorAction"].guiName = "Retract Radiator";
            Events["RetractRadiator"].guiName = "Retract Radiator";

            var wasteheatPowerResource = part.Resources.list.FirstOrDefault(r => r.resourceName == FNResourceManager.FNRESOURCE_WASTEHEAT);
            // calculate WasteHeat Capacity
            if (wasteheatPowerResource != null)
            {
                var ratio = Math.Min(1, Math.Max(0, wasteheatPowerResource.amount / wasteheatPowerResource.maxAmount));
                wasteheatPowerResource.maxAmount = part.mass * 1.0e+5 * wasteHeatMultiplier;
                wasteheatPowerResource.amount = wasteheatPowerResource.maxAmount * ratio;
            }

            var myAttachedEngine = this.part.FindModuleImplementing<ModuleEngines>();
            if (myAttachedEngine == null)
            {
                Fields["partMass"].guiActiveEditor = true;
                Fields["convectiveBonus"].guiActiveEditor = true;
            }

            if (!String.IsNullOrEmpty(thermalAnim))
                heatStates = SetUpAnimation(thermalAnim, this.part);
            SetHeatAnimationRatio(0);

            deployAnim = part.FindModelAnimators(animName).FirstOrDefault();
            if (deployAnim != null)
            {
                deployAnim[animName].layer = 1;
                deployAnim[animName].speed = 0;

                if (radiatorIsEnabled)
                    deployAnim[animName].normalizedTime = 1;
                else
                    deployAnim[animName].normalizedTime = 0;
            }

            _moduleDeployableRadiator = part.FindModuleImplementing<ModuleDeployableRadiator>();
            _moduleActiveRadiator = part.FindModuleImplementing<ModuleActiveRadiator>();

            _maxEnergyTransfer = radiatorArea * 1000 * (1 + ((int)CurrentGenerationType * 2));

            if (state == StartState.Editor)
            {
                if (hasTechsRequiredToUpgrade())
                {
                    isupgraded = true;
                    hasrequiredupgrade = true;
                }
                return;
            }

            // find all thermal sources
            list_of_thermal_sources = vessel.FindPartModulesImplementing<IThermalSource>().Where(tc => tc.IsThermalSource).ToList();

            if (ResearchAndDevelopment.Instance != null)
                upgradeCostStr = ResearchAndDevelopment.Instance.Science + "/" + upgradeCost.ToString("0") + " Science";

            if (state == PartModule.StartState.Docked)
            {
                base.OnStart(state);
                return;
            }

            // add to static list of all radiators
            FNRadiator.list_of_all_radiators.Add(this);



            array = part.FindModelComponents<Renderer>();

            if (isDeployable)
            {
                UnityEngine.Debug.Log("[KSPI] - OnStart.Start isDeployable");

                pivot = part.FindModelTransform("suntransform");

                if (pivot != null)
                    original_eulers = pivot.transform.localEulerAngles;
            }
            else
                radiatorIsEnabled = true;

            UnityEngine.Debug.Log("[KSPI] - OnStart.Start I");

            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {
                //if(PluginHelper.hasTech(upgradeTechReq)) 
                //	hasrequiredupgrade = true;
            }
            else
                hasrequiredupgrade = true;

            if (radiatorInit == false)
                radiatorInit = true;

            part.maxTemp = RadiatorTemperature;

            radiatorTempStr = RadiatorTemperature + "K";

            UnityEngine.Debug.Log("[KSPI] - OnStart.Start J");
        }

        public static AnimationState[] SetUpAnimation(string animationName, Part part)
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

        private void UpdateEnableAutomation()
        {
            Events["SwitchAutomation"].active = isDeployable;
            Events["SwitchAutomation"].guiName = isAutomated ? "Disable Automation" : "Enable Automation";
        }

        public void Update()
        {
            ////if (HighLogic.LoadedSceneIsEditor)
            ////{
            ////    //radiativeArea = part.radiativeArea;
            ////    //skinExposedArea = part.skinExposedArea;
            ////    effectiveRadiatorArea = EffectiveRadiatorArea;
            ////    callCounter++;
            ////}

            //Events["DeployRadiator"].active = Events["DeployRadiator"].guiActiveEditor =  !radiatorIsEnabled && isDeployable && _moduleDeployableRadiator == null;
            //Events["RetractRadiator"].active = Events["RetractRadiator"].guiActiveEditor = radiatorIsEnabled && isDeployable && _moduleDeployableRadiator == null;
        }

        public override void OnUpdate() // is called while in flight
        {
            if (update_count - last_draw_update > 8)
            {
                UpdateEnableAutomation();

                if (ResearchAndDevelopment.Instance != null)
                    Events["RetrofitRadiator"].active = !isupgraded && ResearchAndDevelopment.Instance.Science >= upgradeCost && hasrequiredupgrade;
                else
                    Events["RetrofitRadiator"].active = false;

                Fields["thermalPowerConvStr"].guiActive = convectedThermalPower > 0;

                if (_moduleDeployableRadiator != null)
                    Events["RetractRadiator"].active = showRetractButton && _moduleDeployableRadiator.panelState == ModuleDeployableRadiator.panelStates.EXTENDED;
                

                if ((_moduleDeployableRadiator != null && _moduleDeployableRadiator.panelState == ModuleDeployableRadiator.panelStates.EXTENDED) || _moduleDeployableRadiator == null)
                {
                    thermalPowerDissipStr = radiatedThermalPower.ToString("0.000") + "MW";
                    thermalPowerConvStr = convectedThermalPower.ToString("0.000") + "MW";
                }
                else
                {
                    thermalPowerDissipStr = "disabled";
                    thermalPowerConvStr = "disabled";
                }

                

                radiatorTempStr = current_rad_temp.ToString("0.0") + "K / " + RadiatorTemperature.ToString("0.0") + "K";

                partTempStr = part.temperature.ToString("0.0") + "K / " + part.maxTemp.ToString("0.0") + "K";

                last_draw_update = update_count;

                if (showColorHeat)
                    ColorHeat();
            }

            update_count++;
        }


        public void FixedUpdate() // FixedUpdate is also called when not activated
        {
            try
            {
                if (!HighLogic.LoadedSceneIsFlight)
                    return;

                effectiveRadiatorArea = EffectiveRadiatorArea;

                //ProcessStockThermalCooling();

                _maxEnergyTransfer = radiatorArea * 500 * Math.Pow(1 + ((int)CurrentGenerationType), 1.5);

                if (_moduleActiveRadiator != null)
                {
                    //ModuleResourceHarvester
                    //_moduleActiveRadiator.isCoreRadiator = isCoreRadiator;
                    //_moduleActiveRadiator.parentCoolingOnly = !globalCooling;

                    _moduleActiveRadiator.maxEnergyTransfer = _maxEnergyTransfer;
                }

                if (vessel.altitude <= PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody))
                {
                    float pressure = ((float)FlightGlobals.getStaticPressure(vessel.transform.position) / 100f);
                    float dynamic_pressure = (float)(0.5f * pressure * 1.2041f * vessel.srf_velocity.sqrMagnitude / 101325.0f);
                    pressure += dynamic_pressure;
                    float low_temp = (float)FlightGlobals.getExternalTemperature(vessel.transform.position);

                    float delta_temp = Mathf.Max(0, (float)current_rad_temp - low_temp);
                    double conv_power_dissip = pressure * delta_temp * radiatorArea * rad_const_h / 1e6f * TimeWarp.fixedDeltaTime * convectiveBonus;
                    if (!radiatorIsEnabled)
                        conv_power_dissip = conv_power_dissip / 2.0f;

                    convectedThermalPower = (float)consumeWasteHeat(conv_power_dissip);

                    if (isDeployable)
                        DeployMentControl(dynamic_pressure);
                }
                else
                {
                    pressureLoad = 0;
                    if (!radiatorIsEnabled && isAutomated)
                        Deploy();
                }

                wasteheatRatio = (float)getResourceBarRatio(FNResourceManager.FNRESOURCE_WASTEHEAT);

                radiator_temperature_temp_val = RadiatorTemperature * Mathf.Pow(wasteheatRatio, 0.25f);

                var activeThermalSources = GetActiveThermalSources();
                if (activeThermalSources.Any())
                    radiator_temperature_temp_val = Math.Min(Mathf.Min(GetAverageTemperatureofOfThermalSource(activeThermalSources)) / 1.01f, radiator_temperature_temp_val);

                if (radiatorIsEnabled)
                {
                    if (wasteheatRatio >= 1 && current_rad_temp >= RadiatorTemperature)
                    {
                        explode_counter++;
                        if (explode_counter > 25)
                            part.explode();
                    }
                    else
                        explode_counter = 0;

                    double fixed_thermal_power_dissip = Mathf.Pow(radiator_temperature_temp_val, 4) * GameConstants.stefan_const * effectiveRadiatorArea / 1e6f * TimeWarp.fixedDeltaTime;

                    if (Double.IsNaN(fixed_thermal_power_dissip))
                        Debug.LogWarning("FNRadiator: OnFixedUpdate Single.IsNaN detected in fixed_thermal_power_dissip");

                    radiatedThermalPower = (float)consumeWasteHeat(fixed_thermal_power_dissip);

                    if (Single.IsNaN(radiatedThermalPower))
                        Debug.LogError("FNRadiator: OnFixedUpdate Single.IsNaN detected in radiatedThermalPower after call consumeWasteHeat (" + fixed_thermal_power_dissip + ")");

                    instantaneous_rad_temp = Mathf.Min(radiator_temperature_temp_val * 1.014f, RadiatorTemperature);
                    instantaneous_rad_temp = Mathf.Max(instantaneous_rad_temp, Mathf.Max((float)FlightGlobals.getExternalTemperature(vessel.altitude, vessel.mainBody), 2.7f));

                    if (Single.IsNaN(instantaneous_rad_temp))
                        Debug.LogError("FNRadiator: OnFixedUpdate Single.IsNaN detected in instantaneous_rad_temp after reading external temperature");

                    current_rad_temp = instantaneous_rad_temp;

                    if (isDeployable && pivot != null)
                    {
                        pivot.Rotate(Vector3.up * 5f * TimeWarp.fixedDeltaTime * directionrotate);

                        Vector3 sunpos = FlightGlobals.Bodies[0].transform.position;
                        Vector3 flatVectorToTarget = sunpos - transform.position;

                        flatVectorToTarget = flatVectorToTarget.normalized;
                        float dot = Mathf.Asin(Vector3.Dot(pivot.transform.right, flatVectorToTarget)) / Mathf.PI * 180.0f;

                        float anglediff = -dot;
                        directionrotate = anglediff / 5 / TimeWarp.fixedDeltaTime;
                        directionrotate = Mathf.Min(3, directionrotate);
                        directionrotate = Mathf.Max(-3, directionrotate);

                        part.maximum_drag = 0.8f;
                        part.minimum_drag = 0.8f;
                    }
                }
                else
                {
                    if (isDeployable && pivot != null)
                        pivot.transform.localEulerAngles = original_eulers;

                    double fixed_thermal_power_dissip = Mathf.Pow(radiator_temperature_temp_val, 4) * GameConstants.stefan_const * effectiveRadiatorArea / 0.5e7f * TimeWarp.fixedDeltaTime;

                    radiatedThermalPower = (float)consumeWasteHeat(fixed_thermal_power_dissip);

                    instantaneous_rad_temp = Mathf.Min(radiator_temperature_temp_val * 1.014f, RadiatorTemperature);
                    instantaneous_rad_temp = Mathf.Max(instantaneous_rad_temp, Mathf.Max((float)FlightGlobals.getExternalTemperature((float)vessel.altitude, vessel.mainBody), 2.7f));

                    current_rad_temp = instantaneous_rad_temp;

                    part.maximum_drag = 0.2f;
                    part.minimum_drag = 0.2f;
                }

            }
            catch (Exception e)
            {
                Debug.LogError("FNReactor.FixedUpdate" + e.Message);
            }


        }

        //private void ProcessStockThermalCooling()
        //{
        //    if (!globalCooling)
        //    {
        //        // Find hottest part
        //        if (isCoreRadiator)
        //            this.hottestPart = attachedParts.OrderByDescending(p => p.temperature).FirstOrDefault();
        //        else
        //            this.hottestPart = attachedParts.OrderByDescending(p => p.skinTemperature).FirstOrDefault();

        //        hottestPartName = hottestPart.name;
        //        hottestPartSkinTemperature = hottestPart.skinTemperature;
        //        hottestPartInternalTemperature = hottestPart.temperature;

        //        var hottestPartTemperature = isCoreRadiator ? hottestPartInternalTemperature : hottestPartSkinTemperature;

        //        var minimumEffectiveTemperature = part.temperature * (1 - overcooling);

        //        var effectiveTransferRatio = hottestPartTemperature > 0 ? minimumEffectiveTemperature / hottestPartTemperature : 0;

        //        if (minimumEffectiveTemperature < hottestPartTemperature)
        //        {
        //            var effectiveness = (part.temperature <= hottestPartTemperature) ? 1 : (hottestPartInternalTemperature - minimumEffectiveTemperature) / (part.temperature - minimumEffectiveTemperature);

        //            currentCooling = Math.Max(100 * _maxEnergyTransfer * effectiveness / this.hottestPart.thermalMass, 0);
        //            currentHeating = Math.Max(100 * _maxEnergyTransfer * effectiveness / this.part.thermalMass, 0);

        //            var thermalMassRatio = this.part.thermalMass / this.hottestPart.thermalMass;

        //            var currentCoolingFixed = currentCooling * TimeWarp.fixedDeltaTime;
        //            totalCooling += currentCoolingFixed;
        //            var maxReducedTemperature = hottestPartTemperature - currentCoolingFixed;
        //            //var newTemperature = Math.Max(maxReducedTemperature, minimumEffectiveTemperature);
        //            //var effectiveDifference = newTemperature - maxReducedTemperature;
        //            //var maxDifference = hottestPartTemperature - maxReducedTemperature;
        //            //var effectiveRatio = maxDifference > 0 ? effectiveDifference / maxDifference : 0;

        //            //if (isCoreRadiator)
        //            //    hottestPart.temperature = newTemperature;
        //            //else
        //            //    hottestPart.skinTemperature = newTemperature;

        //            if (isCoreRadiator)
        //                hottestPart.temperature = maxReducedTemperature;
        //            else
        //                hottestPart.skinTemperature = maxReducedTemperature;

        //            //part.temperature += (currentCooling * effectiveTransferRatio * thermalMassRatio * TimeWarp.fixedDeltaTime);
        //            part.temperature += (currentHeating * TimeWarp.fixedDeltaTime);
        //        }
        //        else
        //        {
        //            currentCooling = 0;
        //            currentHeating = 0;
        //        }
        //    }
        //}

        private void DeployMentControl(float dynamic_pressure)
        {
            if (dynamic_pressure <= 0) return;

            pressureLoad = (dynamic_pressure / 1.4854428818159e-3f) * 100;
            if (pressureLoad > 100)
            {
                if (radiatorIsEnabled)
                {
                    if (isAutomated)
                        Retract();
                    else
                    {
                        part.deactivate();
                        part.decouple(1);
                    }
                }
            }
            else if (!radiatorIsEnabled && isAutomated)
            {
                Deploy();
            }
        }

        public float GetAverageTemperatureofOfThermalSource(List<IThermalSource> active_thermal_sources)
        {
            //return active_thermal_sources.Any() 
            //    ? active_thermal_sources.Sum(r => r.HotBathTemperature) / active_thermal_sources.Count
            //    : RadiatorTemperature;
            return RadiatorTemperature;
        }

        public List<IThermalSource> GetActiveThermalSources()
        {
            if (list_of_thermal_sources == null)
                Debug.LogError("list_of_thermal_sources == null");

            return list_of_thermal_sources.Where(ts => ts.IsActive).ToList();
        }

        private double consumeWasteHeat(double wasteheatToConsume)
        {
            if ((_moduleDeployableRadiator != null && _moduleDeployableRadiator.panelState == ModuleDeployableRadiator.panelStates.EXTENDED) || _moduleDeployableRadiator == null)
            {
                var consumedWasteheat = consumeFNResource(wasteheatToConsume, FNResourceManager.FNRESOURCE_WASTEHEAT);

                if (Double.IsNaN(consumedWasteheat))
                    return 0;
                    
                return consumedWasteheat / TimeWarp.fixedDeltaTime;
            }

            return 0;
        }

        public bool hasTechsRequiredToUpgrade()
        {
            if (HighLogic.CurrentGame == null) return false;

            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER) return true;

            return false;
        }

		public float getRadiatorTemperature() 
        {
			return current_rad_temp;
		}

		public override string GetInfo() 
        {
            DetermineGenerationType();

            effectiveRadiatorArea = EffectiveRadiatorArea;

            var sb = new StringBuilder();

            sb.Append(String.Format("Maximum Waste Heat Radiated\nMk1: {0} MW\n\n", GameConstants.stefan_const * effectiveRadiatorArea * Mathf.Pow(radiatorTemperatureMk1, 4) / 1e6f));

            if (!String.IsNullOrEmpty(upgradeTechReqMk2))
                sb.Append(String.Format("Mk2: {0} MW\n", GameConstants.stefan_const * effectiveRadiatorArea * Mathf.Pow(radiatorTemperatureMk2, 4) / 1e6f));
            if (!String.IsNullOrEmpty(upgradeTechReqMk3))
                sb.Append(String.Format("Mk3: {0} MW\n", GameConstants.stefan_const * effectiveRadiatorArea * Mathf.Pow(radiatorTemperatureMk3, 4) / 1e6f));
            if (!String.IsNullOrEmpty(upgradeTechReqMk4))
                sb.Append(String.Format("Mk4: {0} MW\n", GameConstants.stefan_const * effectiveRadiatorArea * Mathf.Pow(radiatorTemperatureMk4, 4) / 1e6f));
            if (!String.IsNullOrEmpty(upgradeTechReqMk5))
                sb.Append(String.Format("Mk5: {0} MW\n", GameConstants.stefan_const * effectiveRadiatorArea * Mathf.Pow(radiatorTemperatureMk5, 4) / 1e6f));

            return sb.ToString();
        }

        public override int getPowerPriority() 
        {
            return 3;
        }

        private void SetHeatAnimationRatio (float colorRatio )
        {
            if (heatStates != null)
            {
                foreach (AnimationState anim in heatStates)
                {
                    anim.normalizedTime = colorRatio;
                }
                return;
            }
        }

        private void ColorHeat()
        {
            float currentTemperature = getRadiatorTemperature();

            float partTempRatio = Mathf.Min((float)(part.temperature / (part.maxTemp * 0.95)), 1);

            float radiatorTempRatio = Mathf.Min(currentTemperature / RadiatorTemperature, 1);

            colorRatio = Mathf.Pow(Math.Max(partTempRatio, radiatorTempRatio) / temperatureColorDivider, emissiveColorPower);

            SetHeatAnimationRatio(colorRatio);

            var emissiveColor = new Color(colorRatio, 0.0f, 0.0f, 0.5f);

            foreach (Renderer renderer in array)
            {
                if (renderer.material.shader.name != kspShader)
                    renderer.material.shader = Shader.Find(kspShader);

                if (part.name.StartsWith("circradiator"))
                {
                    if (renderer.material.GetTexture("_Emissive") == null)
                        renderer.material.SetTexture("_Emissive", GameDatabase.Instance.GetTexture("WarpPlugin/Parts/Electrical/circradiatorKT/texture1_e", false));

                    if (renderer.material.GetTexture("_BumpMap") == null)
                        renderer.material.SetTexture("_BumpMap", GameDatabase.Instance.GetTexture("WarpPlugin/Parts/Electrical/circradiatorKT/texture1_n", false));
                }
                else if (part.name.StartsWith("RadialRadiator"))
                {
                    if (renderer.material.GetTexture("_Emissive") == null)
                        renderer.material.SetTexture("_Emissive", GameDatabase.Instance.GetTexture("WarpPlugin/Parts/Electrical/RadialHeatRadiator/d_glow", false));
                }
                else if (part.name.StartsWith("LargeFlatRadiator"))
                {

                    if (renderer.material.shader.name != kspShader)
                        renderer.material.shader = Shader.Find(kspShader);

                    if (renderer.material.GetTexture("_Emissive") == null)
                        renderer.material.SetTexture("_Emissive", GameDatabase.Instance.GetTexture("WarpPlugin/Parts/Electrical/LargeFlatRadiator/glow", false));

                    if (renderer.material.GetTexture("_BumpMap") == null)
                        renderer.material.SetTexture("_BumpMap", GameDatabase.Instance.GetTexture("WarpPlugin/Parts/Electrical/LargeFlatRadiator/radtex_n", false));
                }

                if (heatStates != null)
                    return;

                if (string.IsNullOrEmpty(colorHeat))
                    return;

                renderer.material.SetColor(colorHeat, emissiveColor);
            }
        }
	}
}