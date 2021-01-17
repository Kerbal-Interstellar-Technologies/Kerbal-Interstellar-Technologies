using System;
using System.Collections.Generic;
using KIT.Resources;
using KIT.ResourceScheduler;
using UnityEngine;

namespace KIT.Toolbar
{

    public class ResourceUI
    {
        readonly KITResourceVesselModule _kitResourceVesselModule;

        public ResourceUI(KITResourceVesselModule kitResourceVesselModule)
        {
            this._kitResourceVesselModule = kitResourceVesselModule;
        }

        readonly ResourceName[] _resources = new[] { ResourceName.ElectricCharge, ResourceName.ThermalPower, ResourceName.ChargedParticle, ResourceName.WasteHeat };

        public string TextUI()
        {
            List<string> elements = new List<string>(128);

            foreach(var resource in _resources)
            {
                if(_kitResourceVesselModule.ResourceData.ModProduction.TryGetValue(resource, out var resourceList))
                {
                    elements.Add($"<br><b>{KITResourceSettings.ResourceToName(resource)} Producers</b><br>");
                    var mods = resourceList.Keys;
                    foreach(var mod in mods)
                    {
                        elements.Add($"    {mod.KITPartName()} -> {Math.Round(resourceList[mod].Amount, 8)} with a max of {Math.Round(resourceList[mod].MaxAmount, 8)}<br>");
                    }
                }

                if (_kitResourceVesselModule.ResourceData.ModConsumption.TryGetValue(resource, out resourceList))
                {
                    elements.Add($"<br><b>{KITResourceSettings.ResourceToName(resource)} Consumers</b><br>");
                    var mods = resourceList.Keys;
                    foreach (var mod in mods)
                    {
                        elements.Add($"    {mod.KITPartName()} -> {Math.Round(resourceList[mod].Amount, 8)} with a max of {Math.Round(resourceList[mod].MaxAmount, 8)}<br>");
                    }
                }

            }

            return string.Join("", elements);
        }

        public static PopupDialog CreateDialog(string vesselName, KITResourceVesselModule kitResourceVesselModule)
        {
            var resourceUI = new ResourceUI(kitResourceVesselModule);

            List<DialogGUIBase> layout = new List<DialogGUIBase>
            {
                new DialogGUILabel(resourceUI.TextUI), new DialogGUIButton("Close", () => { }, 140f, 30f, true)
            };

            Rect pos = new Rect(0.5f, 0.5f, 800, 800);
            return PopupDialog.SpawnPopupDialog(//new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new MultiOptionDialog(
                "ThisIsMyName",
                "",
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
        public static bool CloseWindow;
        public static bool ShowWindow;

        PopupDialog _dialog;

        public void Update()
        {
            var vessel = FlightGlobals.ActiveVessel;
            if (vessel == null || vessel.vesselModules == null || (ShowWindow == false && CloseWindow == false)) {
                // is this needed? show_window = close_window = false;
                return;
            }

            if (ShowWindow)
            {
                if(_dialog == null)
                {
                    var vrm = vessel.FindVesselModuleImplementing<KITResourceVesselModule>();
                    _dialog = ResourceUI.CreateDialog(vessel.vesselName, vrm);
                    _dialog.OnDismiss = DismissDialog;
                }
                ShowWindow = false;
                return;
            }

            // otherwise, close_window is true

            DismissDialog();
            CloseWindow = false;
        }

        private void DismissDialog()
        {
            if (_dialog == null) return;

            _dialog.Dismiss();
            _dialog = null;
        }
    }
}
