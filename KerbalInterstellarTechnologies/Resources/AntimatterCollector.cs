using KIT.Extensions;
using KIT.ResourceScheduler;
using System;
using System.Linq;

namespace KIT.Resources
{
    class AntimatterCollector : PartModule, IKITModule
    {
        public const string Group = "AntimatterCollector";
        public const string GroupTitle = "#LOC_KSPIE_AntimatterCollector_groupName";

        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_AntimatterCollector_Collecting"), UI_Toggle(disabledText = "#LOC_KSPIE_AntimatterCollector_Collecting_Off", enabledText = "#LOC_KSPIE_AntimatterCollector_Collecting_On")]//Collecting--Off--On
        public bool active = true;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = false, guiActive = true, guiName = "#LOC_KSPIE_AntimatterCollector_ParticleFlux")]//Antimatter Flux
        public string ParticleFlux;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = false, guiActive = true, guiName = "#LOC_KSPIE_AntimatterCollector_CollectionRate", guiFormat = "F4", guiUnits = " mg/hour")]//Rate
        public double collectionRate;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_AntimatterCollector_CollectionMultiplier")]//Collection Multiplier
        public double collectionMultiplier = 1;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = false, guiActive = true, guiName = "#LOC_KSPIE_AntimatterCollector_CelestrialBodyFieldStrengthMod", guiFormat = "F2")]//Field Strength Multiplier
        public double celestrialBodyFieldStrengthMod = 1;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_AntimatterCollector_CanCollect")]//Can collect
        public bool canCollect = true;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_AntimatterCollector_PowerReqKW", guiUnits = " KW")]//Power Usage
        public double powerReqKW;
        [KSPField(isPersistant = true, guiActive = false)]
        public double flux;

        private PartResourceDefinition _antimatterDef;
        private ModuleAnimateGeneric _moduleAnimateGeneric;
        private CelestialBody _homeworld;

        private double _effectiveFlux;

        public override void OnStart(StartState state)
        {
            _antimatterDef = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.AntiProtium);

            _moduleAnimateGeneric = part.FindModuleImplementing<ModuleAnimateGeneric>();

            powerReqKW = Math.Pow(collectionMultiplier, 0.9);

            if (state == StartState.Editor) return;

            _homeworld = FlightGlobals.fetch.bodies.First(m => m.isHomeWorld);

            if (!(vessel.orbit.eccentricity < 1) || !active || !canCollect) return;

            var vesselAvgAlt = (vessel.orbit.ApA + vessel.orbit.PeA) / 2;
            flux = collectionMultiplier * 0.5 * (vessel.mainBody.GetBeltAntiparticles(_homeworld, vesselAvgAlt, vessel.orbit.inclination) + vessel.mainBody.GetBeltAntiparticles(_homeworld, vesselAvgAlt, 0.0));
        }

        public override void OnUpdate()
        {
            var lat = vessel.mainBody.GetLatitude(vessel.GetWorldPos3D());
            celestrialBodyFieldStrengthMod = MagneticFieldDefinitionsHandler.GetMagneticFieldDefinitionForBody(vessel.mainBody).StrengthMult;
            flux = collectionMultiplier * vessel.mainBody.GetBeltAntiparticles(_homeworld, vessel.altitude, lat);
            ParticleFlux = flux.ToString("E");
            collectionRate = _effectiveFlux * PluginSettings.Config.SecondsInHour;
            canCollect = _moduleAnimateGeneric == null ? true :  _moduleAnimateGeneric.GetScalar == 1;
        }

        public bool ModuleConfiguration(out int priority, out bool supplierOnly, out bool hasLocalResources)
        {
            priority = 4;
            supplierOnly = false;
            hasLocalResources = false;

            return true;
        }

        public void KITFixedUpdate(IResourceManager resMan)
        {
            if (!HighLogic.LoadedSceneIsFlight) return;

            if (!active || !canCollect)
            {
                _effectiveFlux = 0;
                return;
            }

            double receivedPowerKW = resMan.Consume(ResourceName.ElectricCharge, powerReqKW);
            double powerRatio = powerReqKW > 0.0 ? receivedPowerKW / powerReqKW : 0.0;

            _effectiveFlux = powerRatio * flux;
            resMan.Produce(ResourceName.AntiProtium, _effectiveFlux);
        }

        public string KITPartName() => part.partInfo.title;
    }
}
