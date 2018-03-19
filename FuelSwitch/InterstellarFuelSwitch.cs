﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using TweakScale;
using KSP.Localization;

namespace InterstellarFuelSwitch
{
    public class IFSresource
    {
        public int ID;
        public string name;
        public double currentSupply;
        public double amount;
        public double maxAmount;
        public double boiloffTemp;
        public double density;
        public double unitCost;
        public double latendHeatVaporation;
        public double specificHeatCapacity;

        public IFSresource(string name)
        {
            ID = name.GetHashCode();
            this.name = name;
            PartResourceDefinition resourceDefinition = PartResourceLibrary.Instance.GetDefinition(name);
            if (resourceDefinition != null)
            {
                this.density = resourceDefinition.density;
                this.unitCost = resourceDefinition.unitCost;
                this.specificHeatCapacity = resourceDefinition.specificHeatCapacity;
            }
        }

        public double FullMass { get { return maxAmount * density; } }
    }

    public class IFSmodularTank
    {
        public string GuiName = String.Empty;
        public string SwitchName = String.Empty;
        public string techReq;
        public bool hasTech;
        public double tankCost;
        public double tankMass;
        public double resourceMassDivider;
        public double resourceMassDividerAddition;

        public List<IFSresource> Resources = new List<IFSresource>();

        public double FullResourceMass { get { return Resources.Sum(m => m.FullMass); } }
    }

	[KSPModule("#LOC_IFS_FuelSwitch_moduleName")]
    public class InterstellarFuelSwitch : PartModule, IRescalable<InterstellarFuelSwitch>, IPartCostModifier, IPartMassModifier
    {
        // Persistants
        [KSPField(isPersistant = true)]
        [UI_ChooseOption(affectSymCounterparts = UI_Scene.None, scene = UI_Scene.All, suppressEditorShipModified = true)]
        public int selectedTankSetup = -1;
        [KSPField(isPersistant = true)]
        public string configuredAmounts = "";
        [KSPField(isPersistant = true)]
        public string configuredFlowStates = "";
        [KSPField(isPersistant = true)]
        public string selectedTankSetupTxt;
        [KSPField(isPersistant = true)]
        public bool configLoaded = false;
        [KSPField(isPersistant = true)]
        public string initialTankSetup;

        // Config properties
        [KSPField]
        public string tankId = string.Empty;
        [KSPField]
        public string resourceGui = string.Empty;
        [KSPField]
        public string tankSwitchNames = string.Empty;
        [KSPField]
        public string bannedResourceNames = string.Empty;
        [KSPField]
        public string switcherDescription = "#LOC_IFS_FuelSwitch_switcherDescription";  // Tank
        [KSPField]
        public string resourceNames = "ElectricCharge;LiquidFuel,Oxidizer;MonoPropellant";
        [KSPField]
        public string resourceAmounts = string.Empty;
        [KSPField]
        public string resourceRatios = string.Empty;
        [KSPField]
        public string initialResourceAmounts = string.Empty;
        [KSPField]
        public bool ignoreInitialCost = false;
        [KSPField(guiActiveEditor = false)]
        public bool adaptiveTankSelection = false;
        [KSPField(guiActiveEditor = false)]
        public float basePartMass = 0;
        [KSPField(guiActiveEditor = false)]
        public float baseResourceMassDivider = 0;
        [KSPField(guiActiveEditor = false)]
        public string tankResourceMassDivider = string.Empty;
        [KSPField(guiActiveEditor = false)]
        public string tankResourceMassDividerAddition = string.Empty;
        [KSPField(guiActiveEditor = false)]
        public bool overrideMassWithTankDividers = false;

        [KSPField]
        public bool orderBySwitchName = false;
        [KSPField]
        public float initialPrefabAmount = 0;
        [KSPField]
        public string tankMass = "";
        [KSPField]
        public string tankTechReq = "";
        [KSPField]
        public string tankCost = "";
        [KSPField]
        public string boilOffTemp = "";
        [KSPField]
        public string latendHeatVaporation = "";
        [KSPField]
        public bool displayCurrentBoilOffTemp = false;
        [KSPField]
        public bool displayTankCost = false;
        [KSPField]
        public bool hasSwitchChooseOption = true;
        [KSPField]
        public bool hasGUI = true;
        [KSPField]
        public bool boiloffActive = false;
        [KSPField(guiActive = false)]
        public bool availableInFlight = false;
        [KSPField]
        public bool availableInEditor = true;
        [KSPField(guiActive = false)]
        public bool returnDryMass = false;
        [KSPField]
        public string inEditorSwitchingTechReq;
        [KSPField]
        public string inFlightSwitchingTechReq;
        [KSPField]
        public bool useTextureSwitchModule = false;
        [KSPField]
        public bool showTankName = true;
        [KSPField]
        public bool showInfo = true; // if false, does not feed info to the part list pop up info menu
        [KSPField]
        public string resourcesFormat = "0.0000";

        //dummyvalues
        [KSPField]
        public float volumeMultiplier;
        [KSPField]
        public float massMultiplier;

        // Gui
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "#LOC_IFS_FuelSwitch_tankGuiName")] // Tank name
        public string tankGuiName = "";
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_IFS_FuelSwitch_maxWetDryMass")] // Dry/Wet Mass
        public string maxWetDryMass = "";
        [KSPField(guiActive = false, guiActiveEditor = true, guiName = "#LOC_IFS_FuelSwitch_massRatioStr")] // Mass Ratio
        public string massRatioStr = "";

        // Debug
        [KSPField]
        public double dryMass;
        [KSPField]
        public double wetMass;
        [KSPField]
        public double initialMass;
        [KSPField]
        public double moduleMassDelta;
        [KSPField]
        public float defaultMass;

        [KSPField(isPersistant = true, guiActiveEditor = false)]
        public float storedFactorMultiplier = 1;
        [KSPField(isPersistant = true)]
        public float storedVolumeMultiplier = 1;
        [KSPField(isPersistant = true)]
        public float storedMassMultiplier = 1;

        [KSPField]
        public string resourceAmountStr0 = "";
        [KSPField]
        public string resourceAmountStr1 = "";
        [KSPField]
        public string resourceAmountStr2 = "";
        [KSPField]
        public float volumeExponent = 3;
        [KSPField]
        public float massExponent = 3;

        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_IFS_FuelSwitch_totalMass", guiUnits = " t", guiFormat = "F4")] // Total mass
        public double totalMass;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "#LOC_IFS_FuelSwitch_maxResourceCost", guiFormat = "F3", guiUnits = " Ѵ")]  // Max Wet cost
        public double maxResourceCost = 0;
        [KSPField(guiActive = false, guiActiveEditor = true, guiName = "#LOC_IFS_FuelSwitch_dryCost", guiFormat = "F3", guiUnits = " Ѵ")]           // Dry Tank cost
        public double dryCost = 0;
        [KSPField(guiActive = false, guiActiveEditor = true, guiName = "#LOC_IFS_FuelSwitch_resourceCost", guiFormat = "F3", guiUnits = " Ѵ")]      // Resource cost
        public double resourceCost = 0;
        [KSPField(guiActive = false, guiActiveEditor = true, guiName = "#LOC_IFS_FuelSwitch_totalCost", guiFormat = "F3", guiUnits = " Ѵ")]         // Total Tank cost
        public double totalCost = 0;

        InterstellarTextureSwitch2 textureSwitch;
        List<string> currentResources;
        List<IFSmodularTank> _modularTankList = new List<IFSmodularTank>();
        IFSmodularTank selectedTank;
        UIPartActionWindow tweakableUI;
        HashSet<string> activeResourceList = new HashSet<string>();

        bool initialized = false;
        int numberOfAvailableTanks = 0;

        double _maxResourceMassAmount0;
        double _maxResourceMassAmount1;
        double _maxResourceMassAmount2;

        double _partResourceMaxAmountFraction0;
        double _partResourceMaxAmountFraction1;
        double _partResourceMaxAmountFraction2;

        PartResource _partResource0;
        PartResource _partResource1;
        PartResource _partResource2;

        PartResourceDefinition _partRresourceDefinition0;
        PartResourceDefinition _partRresourceDefinition1;
        PartResourceDefinition _partRresourceDefinition2;

        BaseField _field0;
        BaseField _field1;
        BaseField _field2;

        BaseField _tankGuiNameField;
        BaseField _chooseField;

        BaseEvent _nextTankSetupEvent;
        BaseEvent _previousTankSetupEvent;

        static HashSet<string> researchedTechs;

        public virtual void OnRescale(TweakScale.ScalingFactor factor)
        {
            try
            {
                storedFactorMultiplier = factor.absolute.linear;
                storedVolumeMultiplier = Mathf.Pow(factor.absolute.linear, volumeExponent);
                storedMassMultiplier = Mathf.Pow(factor.absolute.linear, massExponent);

                initialMass = part.prefabMass * storedMassMultiplier;
            }
            catch (Exception e)
            {
                Debug.LogError("[IFS] - OnRescale Error: " + e.Message);
                throw;
            }
        }

        public override void OnStart(PartModule.StartState state)
        {
            try
            {
                initialMass = part.prefabMass * storedMassMultiplier;

                if (initialMass == 0)
                    initialMass = part.prefabMass;

                InitializeData();

                if (adaptiveTankSelection || selectedTankSetup == -1)
                {
                    if (selectedTankSetup == -1)
                    {
                        initialTankSetup = String.Join(";", part.Resources.Select(m => m.resourceName).ToArray());
                    }

                    for (int i = 0; i < _modularTankList.Count; i++)
                    {
                        var modularTank = _modularTankList[i];

                        bool isSimilar = true;
                        foreach (var resource in modularTank.Resources)
                        {
                            if (!part.Resources.Contains(resource.name))
                            {
                                isSimilar = false;
                                break;
                            }
                            else if (adaptiveTankSelection && selectedTankSetup != -1)
                            {
                                if (part.Resources[resource.name].maxAmount != resource.maxAmount)
                                {
                                    isSimilar = false;
                                    break;
                                }
                            }
                        }
                        if (isSimilar)
                        {
                            selectedTankSetup = i;
                            if (adaptiveTankSelection)
                                selectedTankSetupTxt = _modularTankList[selectedTankSetup].GuiName;
                            break;
                        }

                        if (selectedTankSetup == -1)
                        {
                            selectedTankSetup = 0;
                        }
                    }
                }

                this.enabled = true;

                AssignResourcesToPart(false);

                _chooseField = Fields["selectedTankSetup"];

                var chooseField = Fields["selectedObject"];
                _chooseField.guiName = Localizer.Format(switcherDescription);
                _chooseField.guiActiveEditor = hasSwitchChooseOption && availableInEditor && _modularTankList.Count > 1;
                _chooseField.guiActive = hasSwitchChooseOption && availableInFlight && _modularTankList.Count > 1;

                var chooseOptionEditor = _chooseField.uiControlEditor as UI_ChooseOption;
                if (chooseOptionEditor != null)
                {
                    chooseOptionEditor.options = _modularTankList.Select(s => s.SwitchName).ToArray();
                    chooseOptionEditor.onFieldChanged = UpdateFromGUI;
                }

                var chooseOptionFlight = _chooseField.uiControlFlight as UI_ChooseOption;
                if (chooseOptionFlight != null)
                {
                    chooseOptionFlight.options = _modularTankList.Select(s => s.SwitchName).ToArray();
                    chooseOptionFlight.onFieldChanged = UpdateFromGUI;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[IFS] - OnStart Error: " + e.Message);
                throw;
            }
        }

        private void UpdateFromGUI(BaseField field, object oldFieldValueObj)
        {
            if (!_modularTankList[selectedTankSetup].hasTech)
            {
                if ((int)oldFieldValueObj < selectedTankSetup || ((int)oldFieldValueObj == _modularTankList.Count - 1 && selectedTankSetup == 0))
                    nextTankSetupEvent();
                else
                    previousTankSetupEvent();
            }
            else
                AssignResourcesToPart(true, true);
        }

        // Called by external classes
        public int SelectTankSetup(int newTankIndex, bool calledByPlayer)
        {
            try
            {
                InitializeData();

                if (selectedTankSetup == newTankIndex)
                    return newTankIndex;

                var oldSelectedTankSetup = selectedTankSetup;
                selectedTankSetup = newTankIndex;

                if (!_modularTankList[selectedTankSetup].hasTech)
                {
                    if (oldSelectedTankSetup < selectedTankSetup || (oldSelectedTankSetup == _modularTankList.Count - 1 && selectedTankSetup == 0))
                        nextTankSetupEvent();
                    else
                        previousTankSetupEvent();
                }
                else
                    AssignResourcesToPart(calledByPlayer, true);

                return selectedTankSetup;
            }
            catch (Exception e)
            {
                Debug.LogError("[IFS] - SelectTankSetup Error: " + e.Message);
                throw;
            }
        }

        public override void OnAwake()
        {
            try
            {
                if (configLoaded)
                    InitializeData();
            }
            catch (Exception e)
            {
                Debug.LogError("[IFS] - OnAwake Error: " + e.Message);
                throw;
            }
        }

        public override void OnLoad(ConfigNode partNode)
        {
            base.OnLoad(partNode);

            if (!configLoaded)
                InitializeData();

            configLoaded = true;
        }

        private void InitializeData()
        {
            try
            {
                // Prevent execution to once per Scene switch
                if (initialized)
                    return;

                _field0 = Fields["resourceAmountStr0"];
                _field1 = Fields["resourceAmountStr1"];
                _field2 = Fields["resourceAmountStr2"];
                _tankGuiNameField = Fields["tankGuiName"];

                availableInEditor = String.IsNullOrEmpty(inEditorSwitchingTechReq) ? availableInEditor : hasTech(inEditorSwitchingTechReq);
                availableInFlight = String.IsNullOrEmpty(inFlightSwitchingTechReq) ? availableInFlight : hasTech(inFlightSwitchingTechReq);

                SetupTankList(false);

                if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
                {
                    Debug.Log("[IFS] - InsterstellarFuelSwitch Verify Tank Tech Requirements ");
                    foreach (var modularTank in _modularTankList)
                    {
                        modularTank.hasTech = hasTech(modularTank.techReq);
                    }
                    numberOfAvailableTanks = _modularTankList.Where(m => m.hasTech).Count();
                }

                _nextTankSetupEvent = Events["nextTankSetupEvent"];
                _nextTankSetupEvent.guiActive = hasGUI && availableInFlight;

                _previousTankSetupEvent = Events["previousTankSetupEvent"];
                _previousTankSetupEvent.guiActive = hasGUI && availableInFlight;

                Fields["dryCost"].guiActiveEditor = displayTankCost && HighLogic.LoadedSceneIsEditor;
                Fields["resourceCost"].guiActiveEditor = displayTankCost && HighLogic.LoadedSceneIsEditor;
                Fields["maxResourceCost"].guiActiveEditor = displayTankCost && HighLogic.LoadedSceneIsEditor;
                Fields["totalCost"].guiActiveEditor = displayTankCost && HighLogic.LoadedSceneIsEditor;

                if (useTextureSwitchModule)
                {
                    textureSwitch = part.GetComponent<InterstellarTextureSwitch2>(); // only looking for first, not supporting multiple fuel switchers
                    if (textureSwitch == null)
                    {
                        useTextureSwitchModule = false;
                        Debug.Log("[IFS] - no InterstellarTextureSwitch2 module found, despite useTextureSwitchModule being true");
                    }
                }

                initialized = true;
            }
            catch (Exception e)
            {
                Debug.LogError("[IFS] - InitializeData Error: " + e.Message);
                throw;
            }
        }

        [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "#LOC_IFS_FuelSwitch_nextTankSetupText")]
        public void nextTankSetupEvent()
        {
            try
            {
                selectedTankSetup++;

                if (selectedTankSetup >= _modularTankList.Count)
                    selectedTankSetup = 0;

                if (!_modularTankList[selectedTankSetup].hasTech)
                    nextTankSetupEvent();

                AssignResourcesToPart(true, true);
            }
            catch (Exception e)
            {
                Debug.LogError("[IFS] - nextTankSetupEvent Error: " + e.Message);
                throw;
            }
        }

        [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "#LOC_IFS_FuelSwitch_previousTankSetupText")]
        public void previousTankSetupEvent()
        {
            try
            {
                selectedTankSetup--;
                if (selectedTankSetup < 0)
                    selectedTankSetup = _modularTankList.Count - 1;

                if (!_modularTankList[selectedTankSetup].hasTech)
                    previousTankSetupEvent();

                AssignResourcesToPart(true, true);
            }
            catch (Exception e)
            {
                Debug.LogError("[IFS] - previousTankSetupEvent Error: " + e.Message);
                throw;
            }
        }

        private void AssignResourcesToPart(bool calledByPlayer = false, bool affectSymCounterparts = false)
        {
            try
            {
                // destroying a resource messes up the gui in editor, but not in flight.
                currentResources = SetupTankInPart(part, calledByPlayer);

                // update GUI part
                ConfigureResourceMassGui(currentResources);
                UpdateTankName();
                UpdateTexture(calledByPlayer);

                // update Dry Mass
                dryMass = 0;
                UpdateDryMass();
                UpdateGuiResourceMass();
                UpdateCost();

                if (HighLogic.LoadedSceneIsEditor && affectSymCounterparts)
                {
                    foreach (Part symPart in part.symmetryCounterparts)
                    {
                        InterstellarFuelSwitch symSwitch = String.IsNullOrEmpty(tankId) 
                            ? symPart.FindModulesImplementing<InterstellarFuelSwitch>().FirstOrDefault()
                            : symPart.FindModulesImplementing<InterstellarFuelSwitch>().FirstOrDefault(m => m.tankId == tankId);

                        if (symSwitch != null)
                        {
                            symSwitch.selectedTankSetup = selectedTankSetup;
                            symSwitch.selectedTankSetupTxt = selectedTankSetupTxt;
                            symSwitch.AssignResourcesToPart(calledByPlayer, false);
                        }
                    }
                }

                if (tweakableUI == null)
                    tweakableUI = part.FindActionWindow();

                if (tweakableUI != null)
                    tweakableUI.displayDirty = true;
            }
            catch (Exception e)
            {
                Debug.LogError("[IFS] - AssignResourcesToPart Error " + e.Message);
                throw;
            }
        }

        public void UpdateTexture(bool calledByPlayer)
        {
            if (textureSwitch != null)
                textureSwitch.SelectTankSetup(selectedTankSetup, calledByPlayer);
        }

        public void UpdateTankName()
        {
            tankGuiName = _modularTankList[selectedTankSetup].GuiName;

            bool tankGuiNameIsNotEmpty = !String.IsNullOrEmpty(tankGuiName);
            _tankGuiNameField.guiActive = showTankName && tankGuiNameIsNotEmpty;
            _tankGuiNameField.guiActiveEditor = showTankName && tankGuiNameIsNotEmpty;
        }

        private List<string> SetupTankInPart(Part currentPart, bool calledByPlayer)
        {
            try
            {
                // find selected tank
                selectedTank = null;

                if (!calledByPlayer && !String.IsNullOrEmpty(selectedTankSetupTxt))
                {
                    // first find based on gui name
                    selectedTank = _modularTankList.FirstOrDefault(t => t.GuiName == selectedTankSetupTxt);

                    // otherwise find based on switch name
                    if (selectedTank == null)
                        selectedTank = _modularTankList.FirstOrDefault(t => t.SwitchName == selectedTankSetupTxt);
 
                    // otherwise find basided on similarity with switch name
                    if (selectedTank == null)
                        selectedTank = _modularTankList.FirstOrDefault(t => selectedTankSetupTxt.Contains(t.SwitchName));
                }

                // if still no tank selected, do it based on index or pick the first one if invalid
                if (selectedTank == null)
                    selectedTank = selectedTankSetup < _modularTankList.Count
                        ? _modularTankList[selectedTankSetup]
                        : _modularTankList[0];

                // update txt and index for future
                selectedTankSetupTxt = selectedTank.GuiName;
                selectedTankSetup = _modularTankList.IndexOf(selectedTank);

                // create new ResourceNode
                var newResources = new List<string>();
                var newResourceNodes = new List<ConfigNode>();
                var parsedConfigAmount = new List<float>();
                var parsedConfigFlowStates = new List<bool>();

                // parse configured amounts
                if (configuredAmounts.Length > 0)
                {
                    // empty configuration if switched by user
                    if (calledByPlayer)
                        configuredAmounts = String.Empty;

                    string[] configAmounts = configuredAmounts.Split(',');
                    foreach (string item in configAmounts)
                    {
                        float value;
                        if (float.TryParse(item, out value))
                            parsedConfigAmount.Add(value);
                    }

                    // empty configuration if in flight
                    if (!HighLogic.LoadedSceneIsEditor)
                        configuredAmounts = String.Empty;
                }

                if (configuredFlowStates.Length > 0)
                {
                    // empty configuration if switched by user
                    if (calledByPlayer)
                        configuredFlowStates = String.Empty;

                    string[] configFlowStates = configuredFlowStates.Split(',');
                    foreach (string item in configFlowStates)
                    {
                        bool value;
                        if (bool.TryParse(item, out value))
                            parsedConfigFlowStates.Add(value);
                    }

                    // empty configuration if in flight
                    if (!HighLogic.LoadedSceneIsEditor)
                        configuredFlowStates = String.Empty;
                }

                for (int resourceId = 0; resourceId < selectedTank.Resources.Count; resourceId++)
                {
                    var selectedTankResource = selectedTank.Resources[resourceId];

                    if (selectedTankResource.name == "Structural")
                        continue;

                    newResources.Add(selectedTankResource.name);

                    ConfigNode newResourceNode = new ConfigNode("RESOURCE");
                    double maxAmount = selectedTankResource.maxAmount * storedVolumeMultiplier;

                    newResourceNode.AddValue("name", selectedTankResource.name);
                    newResourceNode.AddValue("maxAmount", maxAmount);
                   
                    PartResource existingResource = null;
                    if (!HighLogic.LoadedSceneIsEditor || (HighLogic.LoadedSceneIsEditor && !calledByPlayer))
                    {
                        foreach (var partResource in currentPart.Resources)
                        {
                            if (partResource.resourceName.Equals(selectedTankResource.name))
                            {
                                existingResource = partResource;
                                break;
                            }
                        }
                    }

                    double resourceNodeAmount;
                   
                    if (existingResource != null)
                        resourceNodeAmount =  Math.Min((existingResource.amount / existingResource.maxAmount) * maxAmount, maxAmount);
                    else if (!HighLogic.LoadedSceneIsEditor && resourceId < parsedConfigAmount.Count)
                        resourceNodeAmount = parsedConfigAmount[resourceId];
                    else if (!HighLogic.LoadedSceneIsEditor && calledByPlayer)
                        resourceNodeAmount = 0.0;
                    else
                        resourceNodeAmount = selectedTank.Resources[resourceId].amount * storedVolumeMultiplier;

                    newResourceNode.AddValue("amount", resourceNodeAmount);

                    if (existingResource != null)
                        newResourceNode.AddValue("flowState", existingResource.flowState);
                    else if (resourceId < parsedConfigFlowStates.Count)
                        newResourceNode.AddValue("flowState", parsedConfigFlowStates[resourceId]);

                    newResourceNodes.Add(newResourceNode);
                }

                var finalResourceNodes = new List<ConfigNode>();
                if (newResourceNodes.Count > 0)
                {
                    finalResourceNodes.AddRange(newResourceNodes);
                    newResourceNodes.Clear();
                }

                foreach (var resource in currentPart.Resources)
                {
                    if (!activeResourceList.Contains(resource.resourceName))
                    {
                        ConfigNode newResourceNode = new ConfigNode("RESOURCE");
                        newResourceNode.AddValue("name", resource.resourceName);
                        newResourceNode.AddValue("maxAmount", resource.maxAmount);
                        newResourceNode.AddValue("amount", resource.amount);
                        newResourceNode.AddValue("flowState", resource.flowState);

                        finalResourceNodes.Add(newResourceNode);
                    }
                }

                // add any remaining new nodes
                if (newResourceNodes.Count > 0)
                {
                    finalResourceNodes.AddRange(newResourceNodes);
                    newResourceNodes.Clear();
                }

                // remove all resources
                currentPart.Resources.Clear();

                // add new or exisitng resources
                if (finalResourceNodes.Count > 0)
                {
                    Debug.Log("[IFS] - InsterstellarFuelSwitch SetupTankInPart adding resources: " + ParseTools.Print(newResources));
                    foreach (var resourceNode in finalResourceNodes)
                    {
                        currentPart.AddResource(resourceNode);
                    }
                }

                UpdateCost();

                return newResources;
            }
            catch (Exception e)
            {
                Debug.LogError("[IFS] - SetupTankInPart Error: " + e.Message);
                throw;
            }
        }

        // only called after tank switching
        public void ConfigureResourceMassGui(List<string> newResources)
        {
            _partRresourceDefinition0 = newResources.Count > 0 ? PartResourceLibrary.Instance.GetDefinition(newResources[0]) : null;
            _partRresourceDefinition1 = newResources.Count > 1 ? PartResourceLibrary.Instance.GetDefinition(newResources[1]) : null;
            _partRresourceDefinition2 = newResources.Count > 2 ? PartResourceLibrary.Instance.GetDefinition(newResources[2]) : null; 

            _field0.guiName = _partRresourceDefinition0 != null ? _partRresourceDefinition0.name : ":";
            _field1.guiName = _partRresourceDefinition1 != null ? _partRresourceDefinition1.name : ":";
            _field2.guiName = _partRresourceDefinition2 != null ? _partRresourceDefinition2.name : ":";

            _field0.guiActive = _partRresourceDefinition0 != null;
            _field1.guiActive = _partRresourceDefinition1 != null;
            _field2.guiActive = _partRresourceDefinition2 != null;

            _field0.guiActiveEditor = _partRresourceDefinition0 != null;
            _field1.guiActiveEditor = _partRresourceDefinition1 != null;
            _field2.guiActiveEditor = _partRresourceDefinition2 != null;

            _partResource0 = _partRresourceDefinition0 == null ? null : part.Resources[newResources[0]];
            _partResource1 = _partRresourceDefinition1 == null ? null : part.Resources[newResources[1]];
            _partResource2 = _partRresourceDefinition2 == null ? null : part.Resources[newResources[2]];

            _partResourceMaxAmountFraction0 = _partResource0 == null ? 0 : _partResource0.maxAmount / 1000;
            _partResourceMaxAmountFraction1 = _partResource1 == null ? 0 : _partResource1.maxAmount / 1000;
            _partResourceMaxAmountFraction2 = _partResource2 == null ? 0 : _partResource2.maxAmount / 1000;
        }

        private double UpdateCost(float defaultCost = 0)
        {
            dryCost = part.partInfo.cost * storedMassMultiplier;

            if (selectedTankSetup >= 0 && selectedTankSetup < _modularTankList.Count)
                dryCost += _modularTankList[selectedTankSetup].tankCost * storedMassMultiplier;

            resourceCost = 0;
            maxResourceCost = 0;

            if (_partRresourceDefinition0 == null || _partResource0 == null)
            {
                totalCost = dryCost;
                return 0;
            }

            bool preserveInitialCost = false;
            if (!ignoreInitialCost && !String.IsNullOrEmpty(initialTankSetup))
            {
                preserveInitialCost = true;
                string[] initialTankSetupArray = initialTankSetup.Split(';');

                foreach (var resourcename in initialTankSetupArray)
                {
                    if (!part.Resources.Contains(resourcename))
                    {
                        preserveInitialCost = false;
                        break;
                    }
                }
            }

            var isSmaller = storedFactorMultiplier < 0.999;
            var isLarger = storedFactorMultiplier > 1.001;

            bool unaltered = !isSmaller && !isLarger;

            resourceCost += _partRresourceDefinition0.unitCost * _partResource0.amount;
            maxResourceCost += _partRresourceDefinition0.unitCost * _partResource0.maxAmount; 

            if (_partRresourceDefinition1 == null || _partResource1 == null)
            {
                if (preserveInitialCost)
                {
                    totalCost = dryCost - maxResourceCost + resourceCost;
                    return 0;
                }
                else
                {
                    totalCost = dryCost + resourceCost;
                    return unaltered ? maxResourceCost : (isSmaller ? -dryCost * storedFactorMultiplier : dryCost * storedFactorMultiplier * 0.125);
                }
            }

            resourceCost += _partRresourceDefinition1.unitCost * _partResource1.amount;
            maxResourceCost += _partRresourceDefinition1.unitCost * _partResource1.maxAmount;

            if (_partRresourceDefinition2 == null || _partResource2 == null)
            {
                if (preserveInitialCost)
                {
                    totalCost = dryCost - maxResourceCost + resourceCost;
                    return 0;
                }
                else
                {
                    totalCost = dryCost + resourceCost;
                    return unaltered ? maxResourceCost : (isSmaller ? -dryCost * storedFactorMultiplier : dryCost * storedFactorMultiplier * 0.125);
                }
            }

            resourceCost += _partRresourceDefinition2.unitCost * _partResource2.amount;
            maxResourceCost = _partRresourceDefinition2.unitCost * _partResource2.maxAmount;

            if (preserveInitialCost)
            {
                totalCost = dryCost - maxResourceCost + resourceCost;
                return 0;
            }
            else
            {
                totalCost = dryCost + resourceCost;
                return unaltered ? maxResourceCost : (isSmaller ? -dryCost * storedFactorMultiplier : dryCost * storedFactorMultiplier * 0.125);
            }
        }

        private void UpdateDryMass()
        {
            if (dryMass == 0 || HighLogic.LoadedSceneIsEditor)
            {
                // update Dry Mass
                dryMass = CalculateDryMass();

                UpdateMassRatio();
            }
        }

        private double CalculateDryMass()
        {
            if (selectedTank == null && selectedTankSetup >= 0 && selectedTankSetup < _modularTankList.Count)
            {
                selectedTank = _modularTankList[selectedTankSetup];
            }

            double mass = basePartMass;
            if (selectedTank != null)
            {
                var totalTankResourceMassDivider = selectedTank.resourceMassDivider + selectedTank.resourceMassDividerAddition;

                if (overrideMassWithTankDividers && totalTankResourceMassDivider > 0)
                    mass = selectedTank.FullResourceMass / totalTankResourceMassDivider;
                else
                {
                    mass += selectedTank.tankMass;

                    // use baseResourceMassDivider if specified
                    if (baseResourceMassDivider > 0)
                        mass += selectedTank.FullResourceMass / baseResourceMassDivider;

                    // use resourceMassDivider if specified
                    if (totalTankResourceMassDivider > 0)
                        mass += selectedTank.FullResourceMass / totalTankResourceMassDivider;
                }
            }

            // prevent 0 mass
            if (mass == 0)
               mass = part.prefabMass;

            return mass * storedMassMultiplier;
        }

        private string formatMassStr(double amount)
        {
            if (amount >= 1)
                return (amount).ToString(resourcesFormat) + " t";
            if (amount >= 1e-3)
                return (amount * 1e3).ToString(resourcesFormat) + " kg";
            if (amount >= 1e-6)
                return (amount * 1e6).ToString(resourcesFormat) + " g";

            return (amount * 1e9).ToString(resourcesFormat) + " mg";
        }

        private void UpdateGuiResourceMass()
        {
            var missing0 = _partRresourceDefinition0 == null || _partResource0 == null;
            var missing1 = _partRresourceDefinition1 == null || _partResource1 == null;
            var missing2 = _partRresourceDefinition2 == null || _partResource2 == null;

            var currentResourceMassAmount0 = missing0 ? 0 : _partRresourceDefinition0.density * _partResource0.amount;
            var currentResourceMassAmount1 = missing1 ? 0 : _partRresourceDefinition1.density * _partResource1.amount;
            var currentResourceMassAmount2 = missing2 ? 0 : _partRresourceDefinition2.density * _partResource2.amount;

            totalMass = dryMass + currentResourceMassAmount0 + currentResourceMassAmount1 + currentResourceMassAmount2;

            resourceAmountStr0 = missing0 ? String.Empty : formatMassStr(currentResourceMassAmount0);
            resourceAmountStr1 = missing1 ? String.Empty : formatMassStr(currentResourceMassAmount1);
            resourceAmountStr2 = missing2 ? String.Empty : formatMassStr(currentResourceMassAmount2);
        }

        private void UpdateMassRatio()
        {
            _maxResourceMassAmount0 = _partRresourceDefinition0 == null || _partResource0 == null ? 0 : _partRresourceDefinition0.density * _partResource0.maxAmount;
            _maxResourceMassAmount1 = _partRresourceDefinition1 == null || _partResource1 == null ? 0 : _partRresourceDefinition1.density * _partResource1.maxAmount;
            _maxResourceMassAmount2 = _partRresourceDefinition2 == null || _partResource2 == null ? 0 : _partRresourceDefinition2.density * _partResource1.maxAmount;

            wetMass = _maxResourceMassAmount0 + _maxResourceMassAmount1 + _maxResourceMassAmount2;

            if (wetMass > 0 && dryMass > 0)
                massRatioStr = ToRoundedString(1 / (dryMass / wetMass));

            maxWetDryMass = string.Format("{0} t / {1} t", ToStringWithFixedDigits(dryMass), ToStringWithFixedDigits(wetMass));
        }

        private string ToRoundedString(double value)
        {
            var massRatioRounded = Math.Round(value, 0);
            var differenceWithRounded = Math.Abs(value - massRatioRounded);

            if (differenceWithRounded > 0.05)
                return "1 : " + value.ToString("0.0");
            else if (differenceWithRounded > 0.005)
                return "1 : " + value.ToString("0.00");
            else if (differenceWithRounded > 0.0005)
                return "1 : " + value.ToString("0.000");
            else
                return "1 : " + value.ToString("0");
        }

        private string ToStringWithFixedDigits(double value)
        {
            if (value >= 1000000)
                return value.ToString("0");
            else if (value >= 100000)
                return value.ToString("0.0");
            else if (value >= 10000)
                return value.ToString("0.00");
            else if (value >= 1000)
                return value.ToString("0.000");
            else if (value >= 100)
                return value.ToString("0.0000");
            else if (value >= 10)
                return value.ToString("0.00000");
            else
                return value.ToString("0.000000");
        }

        // Note: do note remove, it is called by KSP
        public void Update()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                UpdateGuiResourceMass();

                // show if any tank resources below 1/1000
                var showSwitchButtons = availableInFlight && numberOfAvailableTanks > 1 
                    && (_partResource0 == null || _partResource0.amount < _partResourceMaxAmountFraction0)
                    && (_partResource1 == null || _partResource1.amount < _partResourceMaxAmountFraction1) 
                    && (_partResource2 == null || _partResource2.amount < _partResourceMaxAmountFraction2);

                _nextTankSetupEvent.guiActive = showSwitchButtons;
                _previousTankSetupEvent.guiActive = showSwitchButtons;

                return;
            }

            // update Dry Mass
            UpdateDryMass();
            UpdateGuiResourceMass();
            UpdateCost();

            configuredAmounts = String.Empty;;
            configuredFlowStates = String.Empty;

            foreach (var resoure in part.Resources)
            {
                configuredAmounts += resoure.amount + ",";
                configuredFlowStates += resoure.flowState.ToString() + ",";
            }
        }

        private void SetupTankList(bool calledByPlayer)
        {
            try
            {
                var weightList = ParseTools.ParseDoubles(tankMass, () => tankMass);
                var tankCostList = ParseTools.ParseDoubles(tankCost, () => tankCost);
                var tankResourceMassDividerList = ParseTools.ParseDoubles(tankResourceMassDivider, () => tankResourceMassDivider);
                var tankResourceMassDividerAdditionList = ParseTools.ParseDoubles(tankResourceMassDividerAddition, () => tankResourceMassDividerAddition);

                // First find the amounts each tank type is filled with
                List<List<double>> resourceList = new List<List<double>>();
                List<List<double>> initialResourceList = new List<List<double>>();
                List<List<double>> boilOffTempList = new List<List<double>>();
                List<List<double>> latendHeatVaporationList = new List<List<double>>();

                string[] resourceTankAbsoluteAmountArray = resourceAmounts.Split(';');
                string[] resourceTankRatioAmountArray = resourceRatios.Split(';');
                string[] initialResourceTankArray = initialResourceAmounts.Split(';');
                string[] boilOffTempTankArray = boilOffTemp.Split(';');
                string[] latendHeatVaporationArray = latendHeatVaporation.Split(';');
                string[] tankNameArray = resourceNames.Split(';');
                string[] tankTechReqArray = tankTechReq.Split(';');
                string[] tankGuiNameArray = resourceGui.Split(';');
                string[] tankSwitcherNameArray = tankSwitchNames.Split(';');

                // if initial resource ammount is missing or not complete, use full amount
                if (initialResourceAmounts.Equals(String.Empty) ||
                    initialResourceTankArray.Length != resourceTankAbsoluteAmountArray.Length)
                    initialResourceTankArray = resourceTankAbsoluteAmountArray;

                var maxLengthTankArray = Math.Max(resourceTankAbsoluteAmountArray.Length, resourceTankRatioAmountArray.Length);

                for (int tankCounter = 0; tankCounter < maxLengthTankArray; tankCounter++)
                {
                    resourceList.Add(new List<double>());
                    initialResourceList.Add(new List<double>());
                    boilOffTempList.Add(new List<double>());
                    latendHeatVaporationList.Add(new List<double>());

                    string[] resourceAmountArray = resourceTankAbsoluteAmountArray[tankCounter].Trim().Split(',');
                    string[] initialResourceAmountArray = initialResourceTankArray[tankCounter].Trim().Split(',');
                    string[] boilOffTempAmountArray = boilOffTempTankArray.Count() > tankCounter ? boilOffTempTankArray[tankCounter].Trim().Split(',') : new string[0];
                    string[] latendHeatVaporationAmountArray = latendHeatVaporationArray.Count() > tankCounter ? latendHeatVaporationArray[tankCounter].Trim().Split(',') : new string[0];

                    // if missing or not complete, use full amount
                    if (initialResourceAmounts.Equals(String.Empty) ||
                        initialResourceAmountArray.Length != resourceAmountArray.Length)
                        initialResourceAmountArray = resourceAmountArray;

                    for (var amountCounter = 0; amountCounter < resourceAmountArray.Length; amountCounter++)
                    {
                        try
                        {
                            if (tankCounter >= resourceList.Count || amountCounter >= resourceAmountArray.Count()) continue;

                            resourceList[tankCounter].Add(double.Parse(resourceAmountArray[amountCounter].Trim()));
                        }
                        catch (Exception exception)
                        {
                            Debug.LogWarning("[IFS] - " + part.name + " error parsing resourceTankAmountArray amount " + tankCounter + "/" + amountCounter +
                                      ": '" + resourceTankAbsoluteAmountArray[tankCounter] + "': '" + resourceAmountArray[amountCounter].Trim() + "' with error: " + exception.Message);
                        }

                        try
                        {
                            if (tankCounter < initialResourceList.Count && amountCounter < initialResourceAmountArray.Count())
                                initialResourceList[tankCounter].Add(ParseTools.ParseDouble(initialResourceAmountArray[amountCounter]));
                        }
                        catch (Exception exception)
                        {
                            Debug.LogWarning("[IFS] - " + part.name +  " error parsing initialResourceList amount " + tankCounter + "/" + amountCounter +
                                      ": '" + initialResourceList[tankCounter] + "': '" + initialResourceAmountArray[amountCounter].Trim() + "' with error: " + exception.Message);
                        }

                        try
                        {
                            if (tankCounter < boilOffTempList.Count && amountCounter < boilOffTempAmountArray.Length)
                                boilOffTempList[tankCounter].Add(ParseTools.ParseDouble(boilOffTempAmountArray[amountCounter]));
                        }
                        catch (Exception exception)
                        {
                            Debug.LogWarning("[IFS] - " + part.name + " error parsing boilOffTempList amount " + tankCounter + "/" + amountCounter +
                                      ": '" + boilOffTempList[tankCounter] + "': '" + boilOffTempAmountArray[amountCounter].Trim() + "' with error: " + exception.Message);
                        }
                    }
                }

                // Then find the kinds of resources each tank holds, and fill them with the amounts found previously, or the amount hey held last (values kept in save persistence/craft)
                for (int currentResourceCounter = 0; currentResourceCounter < tankNameArray.Length; currentResourceCounter++)
                {
                    // create a new modularTank
                    var modularTank = new IFSmodularTank();
                    _modularTankList.Add(modularTank);

                    // initialiseSwitchName
                    if (currentResourceCounter < tankSwitcherNameArray.Length)
                        modularTank.SwitchName = tankSwitcherNameArray[currentResourceCounter];

                    // initialize Gui name if possible
                    if (currentResourceCounter < tankGuiNameArray.Length)
                        modularTank.GuiName = tankGuiNameArray[currentResourceCounter];

                    // initialise tech requirement but ignore first
                    if (currentResourceCounter != 0 && currentResourceCounter < tankTechReqArray.Length)
                        modularTank.techReq = tankTechReqArray[currentResourceCounter].Trim(' ');

                    // initialise tank mass
                    if (currentResourceCounter < weightList.Count)
                        modularTank.tankMass = weightList[currentResourceCounter];

                    if (currentResourceCounter < tankResourceMassDividerList.Count)
                        modularTank.resourceMassDivider = tankResourceMassDividerList[currentResourceCounter];

                    if (currentResourceCounter < tankResourceMassDividerAdditionList.Count)
                        modularTank.resourceMassDividerAddition = tankResourceMassDividerAdditionList[currentResourceCounter];

                    // initialise tank cost
                    if (currentResourceCounter < tankCostList.Count)
                        modularTank.tankCost = tankCostList[currentResourceCounter];

                    string[] resourceNameArray = tankNameArray[currentResourceCounter].Split(',');
                    for (var nameCounter = 0; nameCounter < resourceNameArray.Length; nameCounter++)
                    {
                        var resourceName = resourceNameArray[nameCounter].Trim(' ');
                        var newResource = new IFSresource(resourceName);

                        if (!activeResourceList.Contains(resourceName))
                            activeResourceList.Add(resourceName);

                        if (resourceList[currentResourceCounter] != null && nameCounter < resourceList[currentResourceCounter].Count)
                        {
                            newResource.maxAmount = resourceList[currentResourceCounter][nameCounter];
                            newResource.amount = initialResourceList[currentResourceCounter][nameCounter];
                        }

                        // add boiloff data
                        if (currentResourceCounter < boilOffTempList.Count && boilOffTempList[currentResourceCounter] != null && boilOffTempList[currentResourceCounter].Count > nameCounter)
                            newResource.boiloffTemp = boilOffTempList[currentResourceCounter][nameCounter];

                        if (currentResourceCounter < latendHeatVaporationList.Count && latendHeatVaporationList[currentResourceCounter] != null && latendHeatVaporationList[currentResourceCounter].Count > nameCounter)
                            newResource.latendHeatVaporation = latendHeatVaporationList[currentResourceCounter][nameCounter];

                        modularTank.Resources.Add(newResource);
                    }

                    var extraActiveResourceList = bannedResourceNames.Split(';');
                    foreach( string resourceName in extraActiveResourceList)
                    {
                        if (!activeResourceList.Contains(resourceName))
                            activeResourceList.Add(resourceName);
                    }

                    // ensure there is always a gui name
                    if (string.IsNullOrEmpty(modularTank.GuiName))
                    {
                        var names = modularTank.Resources.Select(m => m.name);
                        modularTank.GuiName = String.Empty;
                        foreach (var name in names)
                        {
                            if (!String.IsNullOrEmpty(modularTank.GuiName))
                                modularTank.GuiName += "+";
                            modularTank.GuiName += name;
                        }
                    }

                    // use guiTankName is switchName is missing
                    if (string.IsNullOrEmpty(modularTank.SwitchName))
                        modularTank.SwitchName = modularTank.GuiName;
                }

                if (orderBySwitchName)
                    _modularTankList = _modularTankList.OrderBy(m => m.SwitchName).ToList();
            }
            catch (Exception e)
            {
                Debug.LogError("[IFS] - SetupTankList Error: " + e.Message);
                throw;
            }
        }

        public float GetModuleCost(float defaultCost, ModifierStagingSituation sit)
        {
            return (float)UpdateCost(defaultCost);
        }

        public ModifierChangeWhen GetModuleCostChangeWhen()
        {
            return ModifierChangeWhen.STAGED;
        }

        public ModifierChangeWhen GetModuleMassChangeWhen()
        {
            return ModifierChangeWhen.STAGED;
        }

        public float GetModuleMass(float defaultMass, ModifierStagingSituation sit)
        {
            this.defaultMass = defaultMass;

            if (returnDryMass)
            {
                return (float)dryMass;
            }
            else
            {
                moduleMassDelta = dryMass - initialMass;

                return (float)moduleMassDelta;
            }
        }

        public override string GetInfo()
        {
            if (!showInfo) return string.Empty;

            var info = new StringBuilder();

            info.AppendLine(Localizer.Format("#LOC_IFS_FuelSwitch_GetInfo") + ":");
            info.AppendLine();

            foreach (var module in _modularTankList)
            {
                int count = 0;
                info.Append("* ");

                foreach (var resource in module.Resources)
                {
                    if (count > 0)
                        info.Append(" + ");
                    if (resource.maxAmount > 0)
                    {
                        info.Append(resource.maxAmount);
                        info.Append(" ");
                    }
                    info.Append(resource.name);

                    count++;
                }

                info.AppendLine();
            }
            return info.ToString();
        }

        private bool hasTech(string techid)
        {
            if (String.IsNullOrEmpty(techid))
                return true;

            if (!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight)
                return true;

            if ((HighLogic.CurrentGame.Mode != Game.Modes.CAREER && HighLogic.CurrentGame.Mode != Game.Modes.SCIENCE_SANDBOX))
                return true;

            if (ResearchAndDevelopment.Instance == null)
            {
                if (researchedTechs == null)
                    LoadSaveFile();

                bool found = researchedTechs.Contains(techid);
                return found;
            }

            var techstate = ResearchAndDevelopment.Instance.GetTechState(techid);
            if (techstate != null)
            {
                var available = techstate.state == RDTech.State.Available;
                return available;
            }
            else
                return false;
        }

        private void LoadSaveFile()
        {
            researchedTechs = new HashSet<string>();

            string persistentfile = KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/persistent.sfs";
            ConfigNode config = ConfigNode.Load(persistentfile);
            ConfigNode gameconf = config.GetNode("GAME");
            ConfigNode[] scenarios = gameconf.GetNodes("SCENARIO");

            foreach (ConfigNode scenario in scenarios)
            {
                if (scenario.GetValue("name") == "ResearchAndDevelopment")
                {
                    ConfigNode[] techs = scenario.GetNodes("Tech");
                    foreach (ConfigNode technode in techs)
                    {
                        var technodename = technode.GetValue("id");
                        researchedTechs.Add(technodename);
                    }
                }
            }
        }
    }
}
