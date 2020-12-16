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
        ResourceManager vesselResourceManager;

        public ResourceUI(ResourceManager vesselResourceManager)
        {
            this.vesselResourceManager = vesselResourceManager;
        }

        private void CreateList(Dictionary<IKITMod, PerPartResourceInformation> info, List<DialogGUIBase> output)
        {
            foreach (var key in info.Keys)
            {
                var ppri = info[key];
                output.Add(new DialogGUILabel($"{key.KITPartName()} -> amount: {ppri.amount} max: {ppri.maxAmount}"));
            }
        }

        ResourceName[] resources = new ResourceName[] { ResourceName.ElectricCharge, ResourceName.ThermalPower, ResourceName.ChargedParticle, ResourceName.WasteHeat };

        // TODO: we should ensure that we only run after a fixed update, not inbetween.
        public string TextUI()
        {
            List<string> elements = new List<string>(128);
            
            foreach(var resource in resources)
            {
                if(vesselResourceManager.ModProduction.TryGetValue(resource, out var resourceList))
                {
                    elements.Add($"<br><b>{KITResourceSettings.ResourceToName(resource)} Producers</b><br>");
                    var mods = resourceList.Keys;
                    foreach(var mod in mods)
                    {
                        elements.Add($"    {mod.KITPartName()} -> {resourceList[mod].amount} with a max of {resourceList[mod].maxAmount}<br>");
                    }
                }

                if (vesselResourceManager.ModConsumption.TryGetValue(resource, out resourceList))
                {
                    elements.Add($"<br><b>{KITResourceSettings.ResourceToName(resource)} Consumers</b><br>");
                    var mods = resourceList.Keys;
                    foreach (var mod in mods)
                    {
                        elements.Add($"    {mod.KITPartName()} -> {resourceList[mod].amount} with a max of {resourceList[mod].maxAmount}<br>");
                    }
                }

            }

            return string.Join("", elements);
        }

        public static PopupDialog CreateDialog(string vesselName, KITResourceVesselModule vesselResourceManager)
        {
            var resourceUI = new ResourceUI(vesselResourceManager.resourceManager);

            List<DialogGUIBase> layout = new List<DialogGUIBase>();
            layout.Add(new DialogGUILabel(resourceUI.TextUI));
            layout.Add(new DialogGUIButton("Close", () => { }, 140f, 30f, true));

            Rect pos = new Rect(0.5f, 0.5f, 800, 800);
            return PopupDialog.SpawnPopupDialog(//new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new MultiOptionDialog(
                "ThisIsMyName",
                "Quick summary, are we good?",
                $"{vesselName} Resource Manager",
                UISkinManager.defaultSkin,
                pos,
                layout.ToArray()), false, UISkinManager.defaultSkin, false);

        }

    }
    // startup once during flight
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class ResourceManagerFlightUI : MonoBehaviour
    {
        public static bool close_window = false;
        public static bool show_window = false;

        PopupDialog dialog;

        public void Update()
        {
            Vessel vessel = FlightGlobals.ActiveVessel;
            if (vessel == null || vessel.vesselModules == null || (show_window == false && close_window == false)) {
                // is this needed? show_window = close_window = false;
                return;
            }

            if (show_window)
            {
                if(dialog == null)
                {
                    var vrm = FindVesselResourceManager(vessel);
                    dialog = ResourceUI.CreateDialog(vessel.vesselName, vrm);
                    dialog.OnDismiss = dismissDialog;
                }
                show_window = false;
                return;
            }

            // otherwise, close_window is true

            dismissDialog();
            close_window = false;
            return;
        }

        private void dismissDialog()
        {
            if (dialog == null) return;

            dialog.Dismiss();
            dialog = null;
        }

        private KITResourceVesselModule FindVesselResourceManager(Vessel vessel)
        {
            KITResourceVesselModule vesselResourceManager = null;

            for (int i = 0; i < vessel.vesselModules.Count; i++)
            {
                if (vessel.vesselModules[i] == null) continue;
                vesselResourceManager = vessel.vesselModules[i] as KITResourceVesselModule;

                if (vesselResourceManager != null) break;
            }
            return vesselResourceManager == null ? null : vesselResourceManager.resourceManager == null ? null : vesselResourceManager;
        }
    }
}
