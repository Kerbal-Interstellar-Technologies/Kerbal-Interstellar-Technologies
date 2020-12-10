using KIT.ResourceScheduler;
using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using KSP.UI.Screens;
using UnityEngine;
using KIT.Resources;

namespace KIT
{

    public class ResourceUI
    {
        protected const int LABEL_WIDTH = 240;
        protected const int VALUE_WIDTH = 55;
        protected const int PRIORITY_WIDTH = 30;
        protected const int OVERVIEW_WIDTH = 65;

        protected const int MAX_PRIORITY = 6;
        private const int POWER_HISTORY_LEN = 10;

        protected readonly string resourceName;

        internal bool renderWindow;
        protected GUIStyle left_bold_label;
        protected GUIStyle right_bold_label;
        protected GUIStyle green_label;
        protected GUIStyle red_label;
        protected GUIStyle left_aligned_label;
        protected GUIStyle right_aligned_label;
        private readonly int windowID;

        private string windowTitle;

        public Rect WindowPosition { get; protected set; }

        private static Font mainFont;
        public static Font MainFont()
        {
            if (mainFont == null)
                mainFont = Font.CreateDynamicFontFromOSFont("Arial", 11);

            return mainFont;
        }

        public ResourceUI(string resource_name, int windowID, float x, float y)
        {
            resourceName = resource_name;
            renderWindow = false;

            windowTitle = resourceName + " " + Localizer.Format("#LOC_KSPIE_ResourceManager_title");//Management Display
            WindowPosition = new Rect(x, y, LABEL_WIDTH + VALUE_WIDTH + PRIORITY_WIDTH, 50);
            this.windowID = windowID;

            left_bold_label = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                font = MainFont()
            };

            right_bold_label = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                font = MainFont(),
                alignment = TextAnchor.MiddleRight
            };

            green_label = new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = resourceName == KITResourceSettings.WasteHeat ? Color.red : Color.green },
                font = MainFont(),
                alignment = TextAnchor.MiddleRight
            };

            red_label = new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = resourceName == KITResourceSettings.WasteHeat ? Color.green : Color.red },
                font = MainFont(),
                alignment = TextAnchor.MiddleRight
            };

            left_aligned_label = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Normal,
                font = MainFont()
            };

            right_aligned_label = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Normal,
                font = MainFont(),
                alignment = TextAnchor.MiddleRight,
            };
        }

        public static string GetFormattedPowerString(double power)
        {
            var absPower = Math.Abs(power);
            string suffix;

            if (absPower >= 1e6)
            {
                suffix = " TW";
                absPower *= 1e-6;
                power *= 1e-6;
            }
            else if (absPower >= 1000)
            {
                suffix = " GW";
                absPower *= 1e-3;
                power *= 1e-3;
            }
            else if (absPower >= 1)
            {
                suffix = " MW";
            }
            else if (absPower >= 0.001)
            {
                suffix = " KW";
                absPower *= 1e3;
                power *= 1e3;
            }
            else
                return (power * 1e6).ToString("0") + " W";
            if (absPower > 100.0)
                return power.ToString("0") + suffix;
            else if (absPower > 10.0)
                return power.ToString("0.0") + suffix;
            else
                return power.ToString("0.00") + suffix;
        }

        public void HideWindow()
        {
            renderWindow = false;
        }
        public void ShowWindow()
        {
            renderWindow = true;
        }

        protected void DoWindow()
        {
         
            List <DialogGUIBase> producers = new List<DialogGUIBase>();
            foreach(var x in resourceProducers)
            {
                producers.Add(new DialogGUILabel(x));
            }

            List<DialogGUIBase> consumers = new List<DialogGUIBase>();
            foreach (var x in resourceConsumers)
            {
                consumers.Add(new DialogGUILabel(x));
            }

            List<DialogGUIBase> dialog = new List<DialogGUIBase>();
            dialog.Add(new DialogGUILabel($"{resourceName} Producers", true, true));
            dialog.Add(new DialogGUIHorizontalLayout(producers.ToArray()));
            dialog.Add(new DialogGUILabel($"{resourceName} Consumers", true, true));
            dialog.Add(new DialogGUIHorizontalLayout(consumers.ToArray()));
            dialog.Add(new DialogGUIButton("Close", () => { }, 140f, 30f, true));

            Rect pos = new Rect(0.5f, 0.5f, 50, 50);
            PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new MultiOptionDialog(
                "ThisIsMyName",
                "Quick summary, are we good?",
                $"{resourceName} Resource Manager",
                UISkinManager.defaultSkin,
                pos,
                dialog.ToArray()), false, UISkinManager.defaultSkin);

            // if (renderWindow && GUI.Button(new Rect(WindowPosition.width - 20, 2, 18, 18), "x"))
            //    renderWindow = false;

        }

        double supplyInTheory;
        double currentSupply;
        double currentDemand;
        string[] resourceProducers;
        string[] resourceConsumers;

        ulong previousStepCount;

        public void DisplayData(
            ulong stepCount,
            Dictionary<IKITMod, PerPartResourceInformation> modConsumption,
            Dictionary<IKITMod, PerPartResourceInformation> modProduction,
            IResourceProduction resourceProduction
        )
        {

            if (stepCount == previousStepCount) return;

            currentSupply = resourceProduction.CurrentSupplied();
            currentDemand = resourceProduction.CurrentlyRequested();
            resourceProducers = new string[modProduction.Count];
            resourceConsumers = new string[modConsumption.Count];

            foreach (var key in modProduction.Keys)
            {
                var data = modProduction[key];
                resourceProducers.Append($"{key.KITPartName()} amount {data.amount}, maxAmount {data.maxAmount}");
            }

            foreach (var key in modConsumption.Keys)
            {
                var data = modConsumption[key];
                resourceConsumers.Append($"{key.KITPartName()} amount {data.amount}, maxAmount {data.maxAmount}");
            }

            previousStepCount = stepCount;

            Debug.Log($"{resourceName}, current Supply: {currentSupply}, current Demand: {currentDemand}\nProducers:\n{String.Join("\n", resourceProducers)}\nConsumers:\n{String.Join("\n", resourceConsumers)}\n");
        }

    }
    // startup once during flight
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class ResourceManagerFlightUI : MonoBehaviour
    {
        public static bool hide_button = false;
        public static bool show_window = false;

        ResourceUI electricChargeUI, thermalPowerUI, chargedParticleUI, wasteHeatUI;

        bool hasSetup;
        void Setup()
        {
            // the resources we display should be configurable..
            var rng = new System.Random();

            electricChargeUI = new ResourceUI(KITResourceSettings.ElectricCharge, rng.Next(), 50, 50);
            chargedParticleUI = new ResourceUI(KITResourceSettings.ChargedParticle, rng.Next(), 50, 600);
            thermalPowerUI = new ResourceUI(KITResourceSettings.ThermalPower, rng.Next(), 600, 50);
            wasteHeatUI = new ResourceUI(KITResourceSettings.WasteHeat, rng.Next(), 600, 600);

            Debug.Log($"[ResourceManagerFlightUI] performed.");

            hasSetup = true;
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2))
            {
                hide_button = !hide_button;
            }
        }

        protected void OnGUI()
        {
            Vessel vessel = FlightGlobals.ActiveVessel;
            KITResourceVesselModule vesselResourceManager = null;

            if (!hasSetup) Setup();

            // Debug.Log($"[ResourceManagerFlightUI] OnGUI after setup.");

            if (vessel == null || vessel.vesselModules == null) return;
            if (hide_button) return;

            for (int i = 0; i < vessel.vesselModules.Count; i++)
            {
                if (vessel.vesselModules[i] == null) continue;
                vesselResourceManager = vessel.vesselModules[i] as KITResourceVesselModule;

                if (vesselResourceManager != null) break;
            }
            if (vesselResourceManager == null || vesselResourceManager.resourceManager == null) return;

            bool tmp = hide_button;
            if (show_window)
                electricChargeUI.ShowWindow();

            /*
             * Are there dragons lurking here, waiting to race?
             */

            electricChargeUI.DisplayData(
                vesselResourceManager.resourceManager.KITSteps,
                vesselResourceManager.resourceManager.ModConsumption[ResourceName.ElectricCharge], vesselResourceManager.resourceManager.ModProduction[ResourceName.ElectricCharge],
                vesselResourceManager.resourceManager.ResourceProductionStats(ResourceName.ElectricCharge)
            );

            // throw new System.Exception("fix this");
            /*

            var megajoules_overmanager = ResourceOvermanager.getResourceOvermanagerForResource(ResourceSettings.Config.ElectricPowerInMegawatt);
            if (megajoules_overmanager.hasManagerForVessel(vessel) && !hide_button)
            {
                ResourceManager mega_manager = megajoules_overmanager.getManagerForVessel(vessel);
                if (mega_manager != null && mega_manager.PartModule != null)
                {
                    // activate rendering
                    if (show_window)
                        mega_manager.ShowWindow();

                    // show window
                    mega_manager.OnGUI();
                }
            }

            var thermalpower_overmanager = ResourceOvermanager.getResourceOvermanagerForResource(ResourceSettings.Config.ThermalPowerInMegawatt);
            if (thermalpower_overmanager.hasManagerForVessel(vessel) && !hide_button)
            {
                ResourceManager thermal_manager = thermalpower_overmanager.getManagerForVessel(vessel);
                if (thermal_manager != null && thermal_manager.PartModule != null)
                {
                    // activate rendering
                    if (show_window)
                        thermal_manager.ShowWindow();

                    // show window
                    thermal_manager.OnGUI();
                }
            }

            var charged_overmanager = ResourceOvermanager.getResourceOvermanagerForResource(ResourceSettings.Config.ChargedParticleInMegawatt);
            if (charged_overmanager.hasManagerForVessel(vessel) && !hide_button)
            {
                ResourceManager charged_manager = charged_overmanager.getManagerForVessel(vessel);
                if (charged_manager != null && charged_manager.PartModule != null)
                {
                    // activate rendering
                    if (show_window)
                        charged_manager.ShowWindow();

                    // show window
                    charged_manager.OnGUI();
                }
            }

            var wasteheat_overmanager = ResourceOvermanager.getResourceOvermanagerForResource(ResourceSettings.Config.WasteHeatInMegawatt);
            if (wasteheat_overmanager.hasManagerForVessel(vessel) && !hide_button)
            {
                ResourceManager waste_manager = wasteheat_overmanager.getManagerForVessel(vessel);
                if (waste_manager != null && waste_manager.PartModule != null)
                {
                    // activate rendering
                    if (show_window)
                        waste_manager.ShowWindow();

                    // show window
                    waste_manager.OnGUI();
                }
            }

            show_window = false;

            */
        }
    }
}
