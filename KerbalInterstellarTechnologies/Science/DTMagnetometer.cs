using System.Linq;
using KIT.Extensions;
using UnityEngine;

namespace KIT.Science
{
    class DTMagnetometer : PartModule
    {
        [KSPField(isPersistant = true)]
        bool IsEnabled;
        [KSPField(isPersistant = false)]
        public string animName = "";
        [KSPField(isPersistant = false, guiActive = true, guiName = "|B|")]
        public string Bmag;
        [KSPField(isPersistant = false, guiActive = true, guiName = "B_r")]
        public string Brad;
        [KSPField(isPersistant = false, guiActive = true, guiName = "B_T")]
        public string Bthe;
        [KSPField(isPersistant = false, guiActive = true, guiName = "#LOC_KSPIE_DTMagnetometer_AntimatterFlux")]//Antimatter Flux
        public string ParticleFlux;

        protected Animation anim;
        protected CelestialBody homeworld;

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_DTMagnetometer_ActivateMagnetometer", active = true)]//Activate Magnetometer
        public void ActivateMagnetometer()
        {
            anim [animName].speed = 1;
            anim [animName].normalizedTime = 0;
            anim.Blend (animName, 2);
            IsEnabled = true;
        }

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_DTMagnetometer_DeactivateMagnetometer", active = false)]//Deactivate Magnetometer
        public void DeactivateMagnetometer()
        {
            anim [animName].speed = -1;
            anim [animName].normalizedTime = 1;
            anim.Blend (animName, 2);
            IsEnabled = false;
        }

        [KSPAction("Activate Magnetometer")]
        public void ActivateMagnetometerAction(KSPActionParam param)
        {
            ActivateMagnetometer();
        }

        [KSPAction("Deactivate Magnetometer")]
        public void DeactivateMagnetometerAction(KSPActionParam param)
        {
            DeactivateMagnetometer();
        }

        [KSPAction("Toggle Magnetometer")]
        public void ToggleMagnetometerAction(KSPActionParam param)
        {
            if (IsEnabled)
                DeactivateMagnetometer();
            else
                ActivateMagnetometer();
        }

        public override void OnStart(StartState state)
        {
            if (state == StartState.Editor) return;

            homeworld = FlightGlobals.fetch.bodies.First(m => m.isHomeWorld);

            Debug.Log("[KSPI]: DTMagnetometer on " + part.name + " was Force Activated");
            part.force_activate();

            anim = part.FindModelAnimators (animName).FirstOrDefault ();

            if (anim == null) return;

            anim [animName].layer = 1;
            if (!IsEnabled)
            {
                anim [animName].normalizedTime = 1;
                anim [animName].speed = -1;
            }
            else
            {
                anim [animName].normalizedTime = 0;
                anim [animName].speed = 1;
            }
            anim.Play ();
        }

        public override void OnUpdate()
        {
            Events[nameof(ActivateMagnetometer)].active = !IsEnabled;
            Events[nameof(DeactivateMagnetometer)].active = IsEnabled;
            Fields[nameof(Bmag)].guiActive = IsEnabled;
            Fields[nameof(Brad)].guiActive = IsEnabled;
            Fields[nameof(Bthe)].guiActive = IsEnabled;
            Fields[nameof(ParticleFlux)].guiActive = IsEnabled;

            var lat = vessel.mainBody.GetLatitude(vessel.GetWorldPos3D());
            var bMag = vessel.mainBody.GetBeltMagneticFieldMagnitude(homeworld, vessel.altitude, lat);
            var bRad = vessel.mainBody.GetBeltMagneticFieldRadial(homeworld, vessel.altitude, lat);
            var bThe = vessel.mainBody.GetBeltMagneticFieldAzimuthal(homeworld, vessel.altitude, lat);
            var flux = vessel.mainBody.GetBeltAntiparticles(homeworld, vessel.altitude, lat);
            Bmag = bMag.ToString("E") + "T";
            Brad = bRad.ToString("E") + "T";
            Bthe = bThe.ToString("E") + "T";
            ParticleFlux = flux.ToString("E");
        }

        public override void OnFixedUpdate() {}
    }
}
