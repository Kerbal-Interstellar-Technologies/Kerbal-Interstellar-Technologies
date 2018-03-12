﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using TweakScale;
using FNPlugin.Extensions;

namespace FNPlugin
{
    enum GenerationType { Mk1 = 0, Mk2 = 1, Mk3 = 2, Mk4 = 3, Mk5 = 4 }

    abstract class EngineECU2 : ResourceSuppliableModule, IRescalable<EngineECU2>
    {
        [KSPField(guiActive = true, guiName = "Max Thrust", guiUnits = " kN", guiFormat = "F4")]
        public double maximumThrust;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Fuel Config")]
        [UI_ChooseOption(affectSymCounterparts = UI_Scene.All, scene = UI_Scene.All, suppressEditorShipModified = true)]
        public int selectedFuel = 0;

        // Persistant
        [KSPField(isPersistant = true)]
        public bool IsEnabled;
        [KSPField(isPersistant = true)]
        bool Launched = false;
        [KSPField(isPersistant = true)]
        public double scale = 1;
        [KSPField(isPersistant = true)]
        public bool hideEmpty = false;
        [KSPField(isPersistant = true)]
        public int selectedTank = 0;
        [KSPField(isPersistant = true)]
        public string selectedTankName = "";

        [KSPField(guiActiveEditor = true, guiName = "upgrade tech 1")]
        public string upgradeTechReq1;
        [KSPField(guiActiveEditor = true, guiName = "upgrade tech 2")]
        public string upgradeTechReq2;
        [KSPField(guiActiveEditor = true, guiName = "upgrade tech 3")]
        public string upgradeTechReq3;
        [KSPField(guiActiveEditor = true, guiName = "upgrade tech 4")]
        public string upgradeTechReq4;

        // None Persistant 
        [KSPField]
        public float minThrottleRatioMk1 = 0.2f;
        [KSPField]
        public float minThrottleRatioMk2 = 0.1f;
        [KSPField]
        public float minThrottleRatioMk3 = 0.05f;
        [KSPField]
        public float minThrottleRatioMk4 = 0.05f;
        [KSPField]
        public float minThrottleRatioMk5 = 0.05f;

        [KSPField]
        public double thrustmultiplier = 1;
        [KSPField]
        public bool isLoaded = false;
        [KSPField]
        public bool resourceSwitching = true;

        [KSPField(guiActiveEditor = true)]
        public float maxThrust = 150;
        [KSPField(guiActiveEditor = true)]
        public float maxThrustUpgraded1 = 300;
        [KSPField(guiActiveEditor = true)]
        public float maxThrustUpgraded2 = 500;
        [KSPField( guiActiveEditor = true)]
        public float maxThrustUpgraded3 = 800;
        [KSPField( guiActiveEditor = true)]
        public float maxThrustUpgraded4 = 1200;

        [KSPField]
        public double efficiency = 0.19;
        [KSPField]
        public double efficiencyUpgraded1 = 0.25;
        [KSPField]
        public double efficiencyUpgraded2 = 0.44;
        [KSPField]
        public double efficiencyUpgraded3 = 0.65;
        [KSPField]
        public double efficiencyUpgraded4 = 0.76;

        // Use for SETI Mode
        [KSPField]
        public float maxTemp = 2500;
        [KSPField]
        public float upgradeCost = 100;


        [KSPField]
        public float throttle;

        public ModuleEngines curEngineT;
        public bool hasMultipleConfigurations = false;

        private IList<FuelConfiguration> _activeConfigurations;
        private FuelConfiguration _currentActiveConfiguration;
        private UIPartActionWindow tweakableUI;
        private StartState CurState;

        private UI_ChooseOption chooseOptionEditor;
        private UI_ChooseOption chooseOptionFlight;
 
        public GenerationType EngineGenerationType { get; private set; }

        public double MaxThrust {  get { return maxThrust * thrustMult(); } }
        public double MaxThrustUpgraded1 { get { return maxThrustUpgraded1 * thrustMult(); } }
        public double MaxThrustUpgraded2 { get { return maxThrustUpgraded2 * thrustMult(); } }
        public double MaxThrustUpgraded3 { get { return maxThrustUpgraded3 * thrustMult(); } }
        public double MaxThrustUpgraded4 { get { return maxThrustUpgraded4 * thrustMult(); } }

        public void DetermineTechLevel()
        {
            int numberOfUpgradeTechs = 1;
            if (PluginHelper.upgradeAvailable(upgradeTechReq1))
                numberOfUpgradeTechs++;
            if (PluginHelper.upgradeAvailable(upgradeTechReq2))
                numberOfUpgradeTechs++;
            if (PluginHelper.upgradeAvailable(upgradeTechReq3))
                numberOfUpgradeTechs++;
            if (PluginHelper.upgradeAvailable(upgradeTechReq4))
                numberOfUpgradeTechs++;

            if (numberOfUpgradeTechs == 5)
                EngineGenerationType = GenerationType.Mk5;
            else if (numberOfUpgradeTechs == 4)
                EngineGenerationType = GenerationType.Mk4;
            else if (numberOfUpgradeTechs == 3)
                EngineGenerationType = GenerationType.Mk3;
            else if (numberOfUpgradeTechs == 2)
                EngineGenerationType = GenerationType.Mk2;
            else
                EngineGenerationType = GenerationType.Mk1;
        }

        [KSPEvent(active = true, advancedTweakable = true, guiActive = true, guiActiveEditor = false, name = "HideUsableFuelsToggle", guiName = "Hide Unusable Configurations")]
        public void HideFuels()
        {
            hideEmpty = true;
            Events["ShowFuels"].active = hideEmpty; // will activate the event (i.e. show the gui button) if the process is not enabled
            Events["HideFuels"].active = !hideEmpty; // will show the button when the process IS enabled
            //UpdateusefulConfigurations();
            InitializeFuelSelector();
            Debug.Log("[KSPI] - HideFuels calls UpdateFuel");
            UpdateFuel();
        }

        [KSPEvent(active = false, advancedTweakable = true, guiActive = true, guiActiveEditor = false, name = "HideUsableFuelsToggle", guiName = "Show All Configurations")]
        public void ShowFuels()
        {
            FuelConfiguration CurConfig = CurrentActiveConfiguration;
            hideEmpty = false;
            Events["ShowFuels"].active = hideEmpty; // will activate the event (i.e. show the gui button) if the process is not enabled
            Events["HideFuels"].active = !hideEmpty; // will show the button when the process IS enabled
            selectedFuel = ActiveConfigurations.IndexOf(CurConfig);
            InitializeFuelSelector();
            Debug.Log("[KSPI] - ShowFuels calls UpdateFuel");
            UpdateFuel();
        }

        public void InitializeGUI()
        {
            InitializeFuelSelector();
            InitializeHideFuels();
        }

        private void InitializeFuelSelector()
        {
            Debug.Log("[KSPI] - Setup Fuels Configurations for " + part.partInfo.title);

            var chooseField = Fields["selectedFuel"];
            chooseOptionEditor = chooseField.uiControlEditor as UI_ChooseOption;
            chooseOptionFlight = chooseField.uiControlFlight as UI_ChooseOption;

            _activeConfigurations = ActiveConfigurations;

            if (_activeConfigurations.Count <= 1)
            {
                chooseField.guiActive = false;
                chooseField.guiActiveEditor = false;
                selectedFuel = 0;
            }
            else
            {
                chooseField.guiActive = true;
                chooseField.guiActiveEditor = true;
                if (selectedFuel >= _activeConfigurations.Count) selectedFuel = _activeConfigurations.Count - 1;
                _currentActiveConfiguration = _activeConfigurations[selectedFuel];
            }

            Debug.Log("[KSPI] - Selected Fuel # " + selectedFuel);

            var names = _activeConfigurations.Select(m => m.fuelConfigurationName).ToArray();

            chooseOptionEditor.options = names;
            chooseOptionFlight.options = names;

            // connect on change event
            if (chooseField.guiActive) 
                chooseOptionFlight.onFieldChanged = UpdateFlightGUI;
            if (chooseField.guiActiveEditor) 
                chooseOptionEditor.onFieldChanged = UpdateEditorGUI;
            _currentActiveConfiguration = _activeConfigurations[selectedFuel];
        }


        private void InitializeHideFuels()
        {
            BaseEvent[] EventList = { Events["HideFuels"], Events["ShowFuels"] };
            foreach (BaseEvent akEvent in EventList)
            {
                if (FuelConfigurations.Count <= 1)
                    akEvent.guiActive = false;
                else
                    akEvent.guiActive = true;
            }
        }

        public FuelConfiguration CurrentActiveConfiguration
        {
            get
            {
                if (_currentActiveConfiguration == null) 
                    _currentActiveConfiguration = ActiveConfigurations[selectedFuel];
                return _currentActiveConfiguration;
            }
        }

        private IList<FuelConfiguration> fuelConfigurations;

        public IList<FuelConfiguration> FuelConfigurations
        {
            get
            {
                if (fuelConfigurations == null)
                    fuelConfigurations = part.FindModulesImplementing<FuelConfiguration>().Where(c => c.requiredTechLevel <= (int)EngineGenerationType).ToList();
                return fuelConfigurations;
            }
        }

        private double thrustMult()
        {
            return FuelConfigurations.Count > 0 ? CurrentActiveConfiguration.thrustMult : 1;
        }

        private void UpdateEditorGUI(BaseField field, object oldFieldValueObj)
        {
            Debug.Log("[KSPI] - Editor Gui Updated");
            UpdateFromGUI(field, oldFieldValueObj);
            selectedTank = selectedFuel;
            selectedTankName = FuelConfigurations[selectedFuel].ConfigName;
            UpdateResources();           
        }

        private void UpdateFlightGUI(BaseField field, object oldFieldValueObj)
        {
            UpdateFromGUI(field, oldFieldValueObj);
            Debug.Log("[KSPI] - UpdateFlightGUI calls UpdateFuel");
            UpdateFuel();
        }

        public virtual void UpdateFuel(bool isEditor = false)
        {
            Debug.Log("[KSPI] - Update Fuel with " + CurrentActiveConfiguration.fuelConfigurationName);

            ConfigNode akPropellants = new ConfigNode();

            int I = 0;
            int N = 0;
            while (I < CurrentActiveConfiguration.Fuels.Length)
            {
                if (CurrentActiveConfiguration.Ratios[I] > 0)
                {
                    Debug.Log("[KSPI] - Load propellant " + CurrentActiveConfiguration.Fuels[I]);
                    akPropellants.AddNode(LoadPropellant(CurrentActiveConfiguration.Fuels[I], CurrentActiveConfiguration.Ratios[I]));
                }
                else
                    N++;
                I++;
            }
            //if (N + 1 >= akConfig.Fuels.Length) 
            //    Fields["selectedFuel"].guiActive = false;

            akPropellants.AddValue("maxThrust", 1);
            akPropellants.AddValue("maxFuelFlow", 1);

            curEngineT.Load(akPropellants);
            curEngineT.atmosphereCurve = CurrentActiveConfiguration.atmosphereCurve;
            if (!isEditor)
            {
                vessel.ClearStaging();
                vessel.ResumeStaging();
            }
        }

        private void UpdateResources()
        {
            if (!resourceSwitching)
                return;

            Debug.Log("[KSPI] - Update Resources");

            ConfigNode akResources = new ConfigNode();
            FuelConfiguration akConfig = new FuelConfiguration();

            if (selectedTankName == "")
                selectedTankName = FuelConfigurations[selectedTank].ConfigName;
            else if (FuelConfigurations[selectedTank].ConfigName == selectedTankName)
                akConfig = FuelConfigurations[selectedTank];
            else
            {
                selectedTank = FuelConfigurations.IndexOf(FuelConfigurations.FirstOrDefault(g => g.ConfigName == selectedTankName));
                akConfig = FuelConfigurations[selectedTank];
            }

            int I = 0;
            int N = 0;

            while (I < part.Resources.Count)
            {
                part.Resources.Remove(part.Resources[I]);
                I++;
            }

            part.Resources.Clear();

            I = 0;
            N = 0;
            while (I < akConfig.Fuels.Length)
            {
                Debug.Log("[KSPI] - Resource: " + akConfig.Fuels[I] + " has a " + akConfig.MaxAmount[I] + " tank.");
                if (akConfig.MaxAmount[I] > 0)
                {
                    Debug.Log("[KSPI] - Loaded Resource: " + akConfig.Fuels[I]);
                    part.AddResource(LoadResource(akConfig.Fuels[I], akConfig.Amount[I], akConfig.MaxAmount[I]));
                }
                else N++;
                I++;
            }

            if (N + 1 >= akConfig.Fuels.Length) 
                Fields["selectedFuel"].guiActive = false;

            Debug.Log("[KSPI] - New Fuels: " + akConfig.Fuels.Length);
            if (tweakableUI == null)
                tweakableUI = part.FindActionWindow();
            if (tweakableUI != null)
                tweakableUI.displayDirty = true;

            //     curEngineT.Save(akResources);
            Debug.Log("[KSPI] - Resources Updated");
        }    

        private ConfigNode LoadPropellant(string akName, float akRatio)
        {
            Debug.Log("[KSPI] - Name: " + akName);
            //    Debug.Log("Ratio: "+ akRatio);

            ConfigNode PropellantNode = new ConfigNode().AddNode("PROPELLANT");
            PropellantNode.AddValue("name", akName);
            PropellantNode.AddValue("ratio", akRatio);
            PropellantNode.AddValue("DrawGauge", true);

            return PropellantNode;
        }

        private ConfigNode LoadResource(string akName, float akAmount, float akMax)
        {
           // Debug.Log("Resource: "+akName + " Added");
            ConfigNode ResourceNode = new ConfigNode().AddNode("RESOURCE");
            ResourceNode.AddValue("name", akName);
            ResourceNode.AddValue("amount", akAmount);
            ResourceNode.AddValue("maxAmount", akMax);
            return ResourceNode;
        }

        private void UpdateFromGUI(BaseField field, object oldFieldValueObj)
        {
            Debug.Log("[KSPI] - UpdateFromGUI is called with " + selectedFuel);

            if (!_activeConfigurations.Any())
            {
                Debug.Log("[KSPI] - UpdateFromGUI no FuelConfigurations found");
                return;
            }

            if (selectedFuel < _activeConfigurations.Count)
            {
                Debug.Log("[KSPI] - UpdateFromGUI " + selectedFuel + " < orderedFuelGenerators.Count");
                _currentActiveConfiguration = _activeConfigurations[selectedFuel];
            }
            else
            {
                Debug.Log("[KSPI] - UpdateFromGUI " + selectedFuel + " >= orderedFuelGenerators.Count");
                selectedFuel = _activeConfigurations.Count - 1;
                _currentActiveConfiguration = _activeConfigurations.Last();
            }

            if (_currentActiveConfiguration == null)
            {
                Debug.Log("[KSPI] - UpdateFromGUI no activeConfiguration found");
                return;
            }
        }

        private void UpdateActiveConfiguration()
        {
            if (_currentActiveConfiguration == null)
                return;

            string previousFuelConfigurationName = _currentActiveConfiguration.fuelConfigurationName;

            _activeConfigurations = ActiveConfigurations;

            if (!_activeConfigurations.Any())
                return;

            chooseOptionFlight.options = _activeConfigurations.Select(m => m.fuelConfigurationName).ToArray();

            var index = chooseOptionFlight.options.IndexOf(previousFuelConfigurationName);

            if (index >= 0)
                selectedFuel = index;

            if (selectedFuel < _activeConfigurations.Count)
                _currentActiveConfiguration = _activeConfigurations[selectedFuel];
            else
            {
                selectedFuel = _activeConfigurations.Count - 1;
                _currentActiveConfiguration = _activeConfigurations.Last();
            }

            if (_currentActiveConfiguration == null)
                return;

            if (previousFuelConfigurationName != _currentActiveConfiguration.fuelConfigurationName)
            {
                Debug.Log("[KSPI] - UpdateActiveConfiguration calls UpdateFuel");
                UpdateFuel();
            }
        }

        public override void OnUpdate()
        {
            UpdateActiveConfiguration();

            base.OnUpdate();
        }

        private void LoadInitialConfiguration()
        {
            isLoaded = true;
            // find maxIsp closes to target maxIsp
            _currentActiveConfiguration = FuelConfigurations.FirstOrDefault();
            selectedFuel = 0;

            if (FuelConfigurations.Count > 1)
                hasMultipleConfigurations = true;
        }

        public override void OnStart(StartState state)
        {
            try
            {
                Debug.Log("[KSPI] - Start State: " + state.ToString());
                Debug.Log("[KSPI] - Already Launched: " + Launched);
                CurState = state;
                curEngineT = this.part.FindModuleImplementing<ModuleEngines>();

                InitializeGUI();

                if (state.ToString().Contains(StartState.Editor.ToString()))
                {
                    Debug.Log("[KSPI] - Editor");
                    hideEmpty = false;
                    selectedTank = selectedFuel;
                    selectedTankName = FuelConfigurations[selectedFuel].ConfigName;
                    UpdateResources();
                    Debug.Log("[KSPI] - OnStart calls UpdateFuel");
                    UpdateFuel(true);
                }
                else
                {
                    hideEmpty = true;
                    if (state.ToString().Contains(StartState.PreLaunch.ToString())) // startstate normally == prelaunch,landed
                    {
                        Debug.Log("[KSPI] - PreLaunch");
                        hideEmpty = true;
                        UpdateResources();
                        //UpdateusefulConfigurations();
                        InitializeFuelSelector();
                        Debug.Log("[KSPI] - OnStart calls UpdateFuel");
                        UpdateFuel();
                    }
                    else
                    {
                        Debug.Log("[KSPI] - No PreLaunch");
                    }
                }
                Events["ShowFuels"].active = hideEmpty;
                Events["HideFuels"].active = !hideEmpty;
            }
            catch (Exception e)
            {
                Debug.LogError("EngineECU2 OnStart eception: " + e.Message);
            }
            
            base.OnStart(state);
        }

        public virtual void OnRescale(TweakScale.ScalingFactor akFactor)
        {
            scale = akFactor.absolute.linear;
        }

        public override void OnInitialize()
        {
            base.OnInitialize();
        }
        
        private IList<FuelConfiguration> usefulConfigurations;
        public IList<FuelConfiguration> UsefulConfigurations
        {
            get
            {
                //if (usefulConfigurations == null)
                usefulConfigurations = GetUsableConfigurations(FuelConfigurations);
                if (usefulConfigurations == null)
                {
                    Debug.Log("[KSPI] - UsefulConfigurations Broke!");
                    return FuelConfigurations;
                }

                return usefulConfigurations;
            }
        }

        
        public IList<FuelConfiguration> ActiveConfigurations
        {
            get
            {
                return hideEmpty ? UsefulConfigurations : FuelConfigurations;
            }
        }

        public IList<FuelConfiguration> GetUsableConfigurations(IList<FuelConfiguration> akConfigs)
        {
            IList<FuelConfiguration> nwConfigs = new List<FuelConfiguration>();
            int I = 0;

            while (I < akConfigs.Count)
            {
                var currentConfig = akConfigs[I];

                if ((_currentActiveConfiguration != null && currentConfig.fuelConfigurationName == _currentActiveConfiguration.fuelConfigurationName) 
                    || ConfigurationHasFuel(currentConfig))
                {
                    nwConfigs.Add(currentConfig);
                    //Debug.Log("[KSPI] - Added fuel configuration: " + akConfigs[I].fuelConfigurationName);
                }
                else 
                    if (I < selectedFuel && I > 0) 
                        selectedFuel--;
                I++;
            }

            return nwConfigs;
        }

        public bool ConfigurationHasFuel(FuelConfiguration akConfig)
        {
            bool result = true;
            int I = 0;
            while (I < akConfig.Fuels.Length)
            {
                if (akConfig.Ratios[I] > 0)
                {
                    double akAmount = 0;
                    double akMaxAmount = 0;

                    var akResource = PartResourceLibrary.Instance.GetDefinition(akConfig.Fuels[I]);

                    if (akResource != null)
                    {
                        part.GetConnectedResourceTotals(akResource.id, out akAmount, out akMaxAmount);
                        //Debug.Log("[KSPI] - Resource: " + akConfig.Fuels[I] + " has " + akAmount);

                        if (akAmount == 0)
                        {
                            if (akMaxAmount > 0)
                            {
                                //Debug.Log("[KSPI] - Resource: " + akConfig.Fuels[I] + " is empty, but that is ok");
                                result = false;
                                I = akConfig.Fuels.Length;
                            }
                            else
                            {
                                //Debug.Log("[KSPI] - Resource: " + akConfig.Fuels[I] + " is missing, it will be removed from the list");
                                result = false;
                                I = akConfig.Fuels.Length;
                            }
                        }
                    }
                    else
                    {
                        //Debug.Log("[KSPI] - Resource: " + akConfig.Fuels[I] + " is not defined");
                        result = false;
                        I = akConfig.Fuels.Length;
                    }
                }
                I++;
            }
            return result;
        }
    }

    class FuelConfiguration : PartModule, IRescalable<FuelConfiguration>
    {
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Fuel Configuration")]
        public string fuelConfigurationName = "";
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Required Tech Level")]
        public int requiredTechLevel = 0;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Fuels")]
        public string fuels = "";
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Ratios")]
        public string ratios = "";
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Amount")]
        public string amount = "";
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Max Amount")]
        public string maxAmount = "";
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Thrust Mult")]
        public float thrustMult = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Power Mult")]
        public float powerMult = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Wasteheat Mult")]
        public float wasteheatMult = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Has Isp Throttling")]
        public bool hasIspThrottling = true;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Atmopheric Curve")]
        public FloatCurve atmosphereCurve = new FloatCurve();
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Ignore ISP")]
        public string ignoreForIsp = "";
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Ignore Thrust")]
        public string ignoreForThrustCurve = "";

        [KSPField(isPersistant = true)]
        public float Scale = 1;
        [KSPField(isPersistant = true)]
        private string akConfigName = "";
        [KSPField(isPersistant = true)]
        private string strAmount="";
        [KSPField(isPersistant = true)]
        private string strMaxAmount="";

        private float[] akAmount = new float[0];
        private float[] akMaxAmount = new float[0];
        private string[] akFuels = new string[0];
        private bool[] akIgnoreIsp = new bool[0];
        private bool[] akIgnoreThrust = new bool[0];
        private float[] akRatio = new float[0];

        public string ConfigName
        {
            get
            {
                if (akConfigName == "") akConfigName = fuelConfigurationName;
                return akConfigName;
            }
        }

        public string[] Fuels
        {
            get
            {
                if (akFuels.Length == 0) 
                    akFuels = Regex.Replace(fuels, " ", "").Split(',');
                return akFuels;
            }
        }

        public float[] Ratios
        {
            get
            {
                if (akRatio.Length == 0) akRatio = StringToFloatArray(ratios);

                return akRatio;
            }
        }

        public float[] Amount
        {
            get
            {
                if (akAmount.Length == 0) 
                    akAmount = StringToFloatArray(StrAmount);
                return VolumeTweaked(akAmount);
            }
        }

        public float[] MaxAmount
        {
            get
            {
                if (akMaxAmount.Length == 0) 
                    akMaxAmount = StringToFloatArray(StrMaxAmount);
                return VolumeTweaked(akMaxAmount);
            }

        }

        private string StrMaxAmount
        {
            get
            {
                if (strMaxAmount == "") strMaxAmount = maxAmount;
                return strMaxAmount;
            }
        }

        private string StrAmount
        {
            get
            {
                if (strAmount == "") strAmount = amount;
                return strAmount;
            }
        }

        public bool[] IgnoreForIsp
        {
            get
            {
                if (ignoreForIsp == "") akIgnoreIsp = falseBoolArray();
                else if (akIgnoreIsp.Length == 0) akIgnoreIsp = StringToBoolArray(ignoreForIsp);
                return akIgnoreIsp;
            }
        }

        public bool[] IgnoreForThrust
        {
            get
            {
                if (ignoreForThrustCurve == "") akIgnoreThrust = falseBoolArray();
                else if (akIgnoreThrust.Length == 0) akIgnoreIsp = StringToBoolArray(ignoreForIsp);
                return akIgnoreThrust;
            }
        }

        private float[] VolumeTweaked(float[] akFloat)
        {
            float[] akTweaked = new float[akFloat.Length];

            if (Scale != 1 && Scale > 0)
            {
                int I = 0;
                while (I < akFloat.Length)
                {
                    akTweaked[I] = (float)(akFloat[I] * Math.Pow(Scale, 3));
                    I++;
                }
                akFloat = akTweaked.ToArray();
            }
            return akFloat;
        }

        private bool[] falseBoolArray()
        {
            List<bool> akBoolList = new List<bool>();
            int I = 0;
            while (I < akFuels.Length)
            {
                akBoolList.Add(false);
                I++;
            }
            return akBoolList.ToArray();
        }

        private float[] StringToFloatArray(string akString)
        {
            List<float> akFloat = new List<float>();
            string[] arString = Regex.Replace(akString, " ", "").Split(',');
            int I = 0;
            while (I < arString.Length)
            {
                akFloat.Add((float)Convert.ToDouble(arString[I]));
                I++;
            }
            return akFloat.ToArray();
        }

        private string FloatArrayToString(float[] akFloat)
        {
            string akstring = "";
            int I = 1;
            akstring += akFloat[0];
            while (I < akFloat.Length)
            {
                akstring = akstring + ", " + akFloat[I];
                I++;
            }
            maxAmount = akstring;
            return akstring;
        }

        private bool[] StringToBoolArray(string akString)
        {
            List<bool> akBool = new List<bool>();
            string[] arString = Regex.Replace(akString, " ", "").Split(',');
            int I = 0;
            while (I < arString.Length)
            {
                akBool.Add(Convert.ToBoolean(arString[I]));
                I++;
            }
            return akBool.ToArray();
        }
        private void Refresh()
        {
            akConfigName = "";
            strMaxAmount = maxAmount;
            akMaxAmount = new float[0];
           int i = 0;
            while (i < Amount.Length)
            {
                if (Amount[i] > MaxAmount[i]) Amount[i] = MaxAmount[i];
                i++;
            }
        }

        private void SaveAmount(ShipConstruct Ship)
        {
            try
            {
                int i = 0;
                if (part.Resources.Count == Amount.Length)
                    while (i < Amount.Length)
                    {
                       // Debug.Log("Saving " + part.Resources[i].resourceName + " Amount " + part.Resources[i].amount);
                        akAmount[i] = (float)part.Resources[i].amount;
                        i++;
                    }

                strAmount = FloatArrayToString(Amount);
            }
            catch (Exception e)
            {
                Debug.LogError("Save Amount Error: " + e);
            }
        }       

        public virtual void OnRescale(ScalingFactor factor)
        {
            Scale = factor.absolute.linear;
        }

        public override void OnStart(StartState state)
        {
            if (fuelConfigurationName != akConfigName || StrMaxAmount != maxAmount) 
                Refresh();

            GameEvents.onEditorShipModified.Add(SaveAmount);
            base.OnStart(state);
        }

    }
}
