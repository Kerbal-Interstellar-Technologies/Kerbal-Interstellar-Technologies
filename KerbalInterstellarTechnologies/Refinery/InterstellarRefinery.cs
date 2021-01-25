using KIT.Refinery.Activity;
using KIT.Resources;
using KIT.ResourceScheduler;
using KSP.Localization;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KIT.Refinery
{
    [KSPModule("ISRU Refinery")]
    class InterstellarRefineryController : PartModule, IKITModule
    {
        [KSPField(isPersistant = true, guiActive = false)]
        protected bool refinery_is_enabled;
        [KSPField(isPersistant = true, guiActive = false)]
        protected bool lastOverflowSettings;
        [KSPField(isPersistant = true, guiActive = false)]
        protected double lastPowerRatio;
        [KSPField(isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_Refinery_Current")]//Current
        protected string lastActivityName = "";
        [KSPField(isPersistant = true, guiActive = false)]
        protected string lastClassName = "";

        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "#LOC_KSPIE_Refinery_RefineryType")]//Refinery Type
        public int RefineryType;

        [KSPField(isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_Refinery_PowerControl"), UI_FloatRange(stepIncrement = 0.5f, maxValue = 100f, minValue = 0.5f)]//Power Control
        public float powerPercentage = 100;

        [KSPField(isPersistant = false, guiActive = true, guiName = "#LOC_KSPIE_Refinery_Status")]//Status
        public string StatusStr = string.Empty;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_Refinery_BaseProduction", guiFormat = "F3")]//Base Production
        public double BaseProduction = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_Refinery_ProductionMultiplier", guiFormat = "F3")]//Production Multiplier
        public double ProductionMult = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_Refinery_PowerReqMultiplier", guiFormat = "F3")]//Power Req Multiplier
        public double PowerReqMult = 1;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "#LOC_KSPIE_Refinery_PowerRequirement", guiFormat = "F3", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit")]//Power Requirement
        public double CurrentPowerReq;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "#LOC_KSPIE_Refinery_PowerAvailable", guiUnits = "%", guiFormat = "F3")]//Power Available
        public double UtilisationPercentage;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "#LOC_KSPIE_Refinery_ConsumedPower", guiFormat = "F3", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit")]//Consumed Power
        public double ConsumedPowerMw;

        protected IRefineryActivity CurrentActivity;

        private List<IRefineryActivity> _availableRefineries;
        private Rect _windowPosition = new Rect(50, 50, RefineryActivity.labelWidth * 4, 150);
        private int _windowId;
        private bool _renderWindow;
        private GUIStyle _boldLabel;
        private GUIStyle _valueLabel;
        private GUIStyle _enabledButton;
        private GUIStyle _disabledButton;

        private bool _overflowAllowed;
        
        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_Refinery_ToggleRefineryWindow", active = true)]//Toggle Refinery Window
        public void ToggleWindow()
        {
            _renderWindow = !_renderWindow;

            if (_renderWindow && _availableRefineries.Count == 1)
                CurrentActivity = _availableRefineries.First();
        }

        public override void OnStart(StartState state)
        {
            if (state == StartState.Editor) return;

            // load stored overflow setting
            _overflowAllowed = lastOverflowSettings;

            _windowId = new System.Random(part.GetInstanceID()).Next(int.MinValue, int.MaxValue);

            var refineriesList = part.FindModulesImplementing<IRefineryActivity>().ToList();

            if (RefineryType > 0)
            {
                AddIfMissing(refineriesList, new AluminiumElectrolyzer());
                AddIfMissing(refineriesList, new AmmoniaElectrolyzer());
                AddIfMissing(refineriesList, new AnthraquinoneProcessor());
                AddIfMissing(refineriesList, new AtmosphereProcessor());
                AddIfMissing(refineriesList, new CarbonDioxideElectrolyzer());
                AddIfMissing(refineriesList, new HaberProcess());
                AddIfMissing(refineriesList, new HeavyWaterElectrolyzer());
                AddIfMissing(refineriesList, new PartialMethaneOxidation());
                AddIfMissing(refineriesList, new PeroxideProcess());
                AddIfMissing(refineriesList, new UF4Ammonolysiser());
                AddIfMissing(refineriesList, new RegolithProcessor());
                AddIfMissing(refineriesList, new ReverseWaterGasShift());
                AddIfMissing(refineriesList, new NuclearFuelReprocessor());
                AddIfMissing(refineriesList, new SabatierReactor());
                AddIfMissing(refineriesList, new OceanProcessor());
                AddIfMissing(refineriesList, new SolarWindProcessor());
                AddIfMissing(refineriesList, new WaterElectrolyzer());
                AddIfMissing(refineriesList, new WaterGasShift());

                _availableRefineries = refineriesList
                    .Where(m => ((int) m.RefineryType & RefineryType) == (int) m.RefineryType)
                    .OrderBy(a => a.ActivityName).ToList();
            }
            else
                _availableRefineries = refineriesList.OrderBy(a => a.ActivityName).ToList();

            // initialize refineries
            _availableRefineries.ForEach(m => m.Initialize(part));

            // load same
            if (refinery_is_enabled && !string.IsNullOrEmpty(lastActivityName))
            {
                Debug.Log("[KSPI]: ISRU Refinery looking to restart " + lastActivityName);
                CurrentActivity = _availableRefineries.FirstOrDefault(a => a.ActivityName == lastActivityName);

                if (CurrentActivity == null)
                {
                    Debug.Log("[KSPI]: ISRU Refinery looking to restart " + lastClassName);
                    CurrentActivity = _availableRefineries.FirstOrDefault(a => a.GetType().Name == lastClassName);
                }
            }
        }

        private void AddIfMissing(List<IRefineryActivity> list, IRefineryActivity refinery)
        {
            if (list.All(m => m.ActivityName != refinery.ActivityName))
                list.Add(refinery);
        }

        public override void OnUpdate()
        {
            StatusStr = CurrentActivity == null ? Localizer.Format("#LOC_KSPIE_Refinery_Offline") : CurrentActivity.Status;
        }

        public override string GetInfo()
        {
            return Localizer.Format("#LOC_KSPIE_Refinery_GetInfo");//"Refinery Module capable of advanced ISRU processing."
        }

        private void OnGUI()
        {
            if (vessel != FlightGlobals.ActiveVessel || !_renderWindow) return;

            _windowPosition = GUILayout.Window(_windowId, _windowPosition, Window, Localizer.Format("#LOC_KSPIE_Refinery_WindowTitle"));//"ISRU Refinery Interface"
        }

        private void Window(int window)
        {
            if (_boldLabel == null)
                _boldLabel = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, font = PluginHelper.MainFont};

            if (_valueLabel == null)
                _valueLabel = new GUIStyle(GUI.skin.label) { font = PluginHelper.MainFont };

            if (_enabledButton == null)
                _enabledButton = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold, font = PluginHelper.MainFont };

            if (_disabledButton == null)
                _disabledButton = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Normal, font = PluginHelper.MainFont };

            if (GUI.Button(new Rect(_windowPosition.width - 20, 2, 18, 18), "x"))
                _renderWindow = false;

            GUILayout.BeginVertical();

            if (CurrentActivity == null || !refinery_is_enabled) // if there is no processing going on or the refinery is not enabled
            {
                _availableRefineries.ForEach(act => // per each activity (notice the end brackets are there, 13 lines below)
                {
                    GUILayout.BeginHorizontal();
                    bool hasRequirement = act.HasActivityRequirements(); // if the requirements for the activity are fulfilled
                    GUIStyle guiStyle = hasRequirement ? _enabledButton : _disabledButton; // either draw the enabled, bold button, or the disabled one

                    var buttonText = string.IsNullOrEmpty(act.Formula) ? act.ActivityName : act.ActivityName + " : " + act.Formula;

                    if (GUILayout.Button(buttonText, guiStyle, GUILayout.ExpandWidth(true))) // if user clicks the button and has requirements for the activity
                    {
                        if (hasRequirement)
                        {
                            CurrentActivity = act; // the activity will be treated as the current activity
                            refinery_is_enabled = true; // refinery is now on
                        }
                        else
                            act.PrintMissingResources();

                    }
                    GUILayout.EndHorizontal();
                });
            }
            else
            {
                // unused - var hasRequirement = CurrentActivity.HasActivityRequirements();

                // show button to enable/disable resource overflow
                GUILayout.BeginHorizontal();
                if (_overflowAllowed)
                {
                    if (GUILayout.Button(Localizer.Format("#LOC_KSPIE_Refinery_DisableOverflow"), GUILayout.ExpandWidth(true)))//"Disable Overflow"
                        _overflowAllowed = false;
                }
                else
                {
                    if (GUILayout.Button(Localizer.Format("#LOC_KSPIE_Refinery_EnableOverflow"), GUILayout.ExpandWidth(true)))//"Enable Overflow"
                        _overflowAllowed = true;
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#LOC_KSPIE_Refinery_CurrentActivity"), _boldLabel, GUILayout.Width(RefineryActivity.labelWidth));//"Current Activity"
                GUILayout.Label(CurrentActivity.ActivityName, _valueLabel, GUILayout.Width(RefineryActivity.valueWidth * 2));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label(Localizer.Format("#LOC_KSPIE_Refinery_Status"), _boldLabel, GUILayout.Width(RefineryActivity.labelWidth));//"Status"
                GUILayout.Label(CurrentActivity.Status, _valueLabel, GUILayout.Width(RefineryActivity.valueWidth * 2));
                GUILayout.EndHorizontal();

                // allow current activity to show feedback
                CurrentActivity.UpdateGUI();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(Localizer.Format("#LOC_KSPIE_Refinery_DeactivateProcess"), GUILayout.ExpandWidth(true)))//"Deactivate Process"
                {
                    refinery_is_enabled = false;
                    CurrentActivity = null;
                }
                GUILayout.EndHorizontal();


            }
            GUILayout.EndVertical();
            GUI.DragWindow();

        }

        public ModuleConfigurationFlags ModuleConfiguration() => ModuleConfigurationFlags.Fourth;

        public void KITFixedUpdate(IResourceManager resMan)
        {
            CurrentPowerReq = 0;

            if (!HighLogic.LoadedSceneIsFlight || !refinery_is_enabled || CurrentActivity == null)
            {
                lastActivityName = string.Empty;
                return;
            }

            CurrentPowerReq = PowerReqMult * CurrentActivity.PowerRequirements * BaseProduction;

            var powerRequest = CurrentPowerReq * (powerPercentage / 100);

            ConsumedPowerMw = CheatOptions.InfiniteElectricity
                ? powerRequest
                : resMan.Consume(ResourceName.ElectricCharge, ConsumedPowerMw);

            var powerRatio = CurrentPowerReq > 0 ? ConsumedPowerMw / CurrentPowerReq : 0;

            UtilisationPercentage = powerRatio * 100;

            var productionModifier = ProductionMult * BaseProduction;

            CurrentActivity.UpdateFrame(resMan, powerRatio * productionModifier, powerRatio, productionModifier, _overflowAllowed);

            lastPowerRatio = powerRatio; // save the current power ratio in case the vessel is unloaded
            lastOverflowSettings = _overflowAllowed; // save the current overflow settings in case the vessel is unloaded
            lastActivityName = CurrentActivity.ActivityName; // take the string with the name of the current activity, store it in persistent string
            lastClassName = CurrentActivity.GetType().Name;
        }

        public string KITPartName() => $"{part.partInfo.title}{(CurrentActivity != null ? " (" + lastActivityName + ")" : "")}";
    }
}
