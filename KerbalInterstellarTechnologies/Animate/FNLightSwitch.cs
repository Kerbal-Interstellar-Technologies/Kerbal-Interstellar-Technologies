﻿using KSP.Localization;
using System.Collections.Generic;
using UnityEngine;

namespace KIT.Animate
{
    public class FNLightSwitch : PartModule
    {
        [KSPField]
        public string lightName = Localizer.Format("#LOC_KSPIE_LightSwitch_lightName");//"Light"

        [KSPField]
        public string Emissive = "Emissive";

        [KSPField(isPersistant = true)]
        public bool LightsAreOn = true;

        private Light _myLight = null;

        public void SetLightState(bool toggle)
        {
            if (!_myLight)
                _myLight = part.FindModelComponent<Light>();

            if (toggle)
                LightsAreOn = !LightsAreOn;
            _myLight.enabled = LightsAreOn;

            List<MeshRenderer> pRenderers = part.FindModelComponents<MeshRenderer>();
            foreach (var mr in pRenderers)
            {
                if (mr.gameObject.name == Emissive)
                {
                    float lightAlpha = 1f;
                    if (!LightsAreOn)
                        lightAlpha = 0f;

                    Color oldColor = mr.material.GetColor("_EmissiveColor");
                    Color newColor = new Color(oldColor.r, oldColor.g, oldColor.b, lightAlpha);
                    mr.material.SetColor("_EmissiveColor", newColor);
                }
            }
        }


        public override void OnStart(StartState state)
        {
            SetLightState(false);
        }

        [KSPEvent(guiName = "Toggle Lights", guiActive = true, guiActiveEditor = true, active = true)]
        public void ToggleLights()
        {
            SetLightState(true);
        }

        [KSPAction("Lights On")]
        public void LightsOnAction(KSPActionParam ap)
        {
            if (!LightsAreOn)
                SetLightState(true);
        }

        [KSPAction("Lights Off")]
        public void LightsOffAction(KSPActionParam ap)
        {
            if (LightsAreOn)
                SetLightState(true);
        }

        [KSPAction("Toggle Lights")]
        public void ToggleLightsAction(KSPActionParam ap)
        {
            SetLightState(true);
        }

    }
}
