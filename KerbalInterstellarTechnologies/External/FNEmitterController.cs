using System;
using System.Linq;
using KIT.Storage;
using UnityEngine;

namespace KIT.External
{
    public class FNEmitterController:  PartModule
    {
        // Persistent input
        [KSPField(isPersistant = true)]
        public double reactorActivityFraction;
        [KSPField(isPersistant = true, guiName = "#LOC_KSPIE_FNEmitterController_FuelNeutronsFraction")]//Fuel Neutrons Fraction
        public double fuelNeutronsFraction = 0.02;
        [KSPField(isPersistant = true)]
        public double lithiumNeutronAbsorbtionFraction;
        [KSPField(isPersistant = true)]
        public double exhaustActivityFraction;
        [KSPField(isPersistant = true)]
        public double radioactiveFuelLeakFraction;
        [KSPField(isPersistant = true)]
        public bool exhaustProducesNeutronRadiation = false;
        [KSPField(isPersistant = true)]
        public bool exhaustProducesGammaRadiation = false;

        //Setting
        [KSPField(guiActiveEditor = true, guiName = "#LOC_KSPIE_FNEmitterController_MaxGammaRadiation")]//Max Gamma Radiation
        public double maxRadiation = 0.02;
        [KSPField]
        public double neutronsExhaustRadiationMult = 1;
        [KSPField]
        public double gammaRayExhaustRadiationMult = 0.5;
        [KSPField]
        public double neutronScatteringRadiationMult = 20;
        [KSPField]
        public double diameter = 1;
        [KSPField]
        public double height;
        [KSPField]
        public double habitatMassMultiplier = 20;
        [KSPField]
        public double reactorMassMultiplier = 10; 

        // Gui
        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_FNEmitterController_DistanceRadiationModifier", guiFormat = "F5")]//Distance Radiation Modifier
        public double averageDistanceModifier;
        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_FNEmitterController_AverageDistanceToCrew", guiFormat = "F5")]//Average Distance To Crew
        public double averageCrewDistanceToEmitter;
        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_FNEmitterController_AverageCrewMassProtection", guiUnits = " g/cm2", guiFormat = "F5")]//Average Crew Mass Protection
        public double averageCrewMassProtection;
        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_FNEmitterController_ReactorShadowMassProtection")]//Reactor Shadow Mass Protection
        public double reactorShadowShieldMassProtection;
        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_FNEmitterController_ReactorLeadShieldingThickness", guiUnits = " cm", guiFormat = "F5")]//reactor Lead Shielding Thickness
        public double reactorLeadShieldingThickness;
        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_FNEmitterController_AverageHabitatLeadThickness", guiUnits = " cm", guiFormat = "F5")]//Average Habitat Lead Thickness
        public double averageHabitatLeadEquivalantThickness;
        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_FNEmitterController_ReactorShadowShieldLeadThickness", guiUnits = " cm", guiFormat = "F5")]//Reactor Shadow Shield Lead Thickness
        public double reactorShadowShieldLeadThickness;
        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_FNEmitterController_ReactorGammaRaysAttenuation", guiFormat = "F5")]//Reactor GammaRays Attenuation
        public double reactorShieldingGammaAttenuation;
        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_FNEmitterController_ReactorNeutronAttenuation", guiFormat = "F5")]//Reactor Neutron Attenuation
        public double reactorShieldingNeutronAttenuation;
        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_FNEmitterController_AverageGammaRaysAttenuation", guiFormat = "F5")]//Average GammaRays Attenuation
        public double averageHabitatLeadGammaAttenuation;
        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_FNEmitterController_AverageNeutronAttenuation", guiFormat = "F5")]//Average Neutron Attenuation
        public double averageHabitaNeutronAttenuation;
        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_FNEmitterController_EmitterRadiationRate")]//Emitter Radiation Rate
        public double emitterRadiationRate;
        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_FNEmitterController_GammaTransparency")]//Gamma Transparency
        public double gammaTransparency;

        // Output
        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_FNEmitterController_ReactorCoreNeutronRadiation")]//Reactor Core Neutron Radiation
        public double reactorCoreNeutronRadiation;
        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_FNEmitterController_ReactorCoreGammaRadiation")]//Reactor Core Gamma Radiation
        public double reactorCoreGammaRadiation;
        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_FNEmitterController_LostFissionFuelRadiation")]//Lost Fission Fuel Radiation
        public double lostFissionFuelRadiation;
        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_FNEmitterController_FissionExhaustRadiation")]//Fission Exhaust Radiation
        public double fissionExhaustRadiation;
        [KSPField(guiActive = false, guiName = "#LOC_KSPIE_FNEmitterController_FissionFragmentRadiation")]//Fission Fragment Radiation
        public double fissionFragmentRadiation;

        // Privates
        PartModule _emitterModule;
        BaseField _emitterRadiationField;
        PartResource _shieldingPartResource;

        public override void OnStart(StartState state)
        {
            InitializeKerbalismEmitter();
        }

        public virtual void Update()
        {
            UpdateKerbalismEmitter();
        }

        private void InitializeKerbalismEmitter()
        {
            if (Kerbalism.VersionMajor == 0)
            {
                Debug.Log("[KSPI]: Skipped Initialize FNEmitterController");
                return;
            }

            Debug.Log("[KSPI]: FNEmitterController Initialize");

            _shieldingPartResource = part.Resources["Shielding"];
            if (_shieldingPartResource != null)
            {
                var radius = diameter * 0.5;
                if (height == 0)
                    height = diameter;

                var ratio = _shieldingPartResource.amount / _shieldingPartResource.maxAmount;
                _shieldingPartResource.maxAmount = (2 * Math.PI * radius * radius) + (2 * Math.PI * radius * height);    // 2 π r2 + 2 π r h 
                _shieldingPartResource.amount = _shieldingPartResource.maxAmount * ratio;
            }

            if (HighLogic.LoadedSceneIsFlight == false)
                return;

            bool found = false;
            foreach (PartModule module in part.Modules)
            {
                if (module.moduleName == "Emitter")
                {
                    _emitterModule = module;

                    _emitterRadiationField = module.Fields["radiation"];

                    found = true;
                    break;
                }
            }

            if (found)
                Debug.Log("[KSPI]: FNEmitterController Found Emitter");
            else
                Debug.LogWarning("[KSPI]: FNEmitterController failed to find Emitter");
        }

        private void UpdateKerbalismEmitter()
        {
            if (_emitterModule == null)
                return;

            if (_emitterRadiationField == null)
                return;

            if (maxRadiation == 0)
                return;

            int totalCrew = vessel.GetCrewCount();
            if (totalCrew == 0)
                return;

            double totalDistancePart = 0;
            double totalCrewMassShielding = 0;

            Vector3 reactorPosition = part.transform.position;

            foreach (Part partWithCrew in vessel.parts.Where(m => m.protoModuleCrew.Count > 0))
            {
                int partCrewCount = partWithCrew.protoModuleCrew.Count;

                double distanceToPart = (reactorPosition - partWithCrew.transform.position).magnitude;

                totalDistancePart += distanceToPart * partCrewCount / diameter;

                var habitat = partWithCrew.FindModuleImplementing<KerbalismHabitatController>();
                if (habitat != null)
                {
                    var habitatSurface = habitat.Surface;
                    if (habitatSurface > 0)
                        totalCrewMassShielding = (partWithCrew.resourceMass / habitatSurface) * partCrewCount;
                }
            }

            averageCrewMassProtection = Math.Max(0, totalCrewMassShielding / totalCrew);
            averageCrewDistanceToEmitter = Math.Max(1, totalDistancePart / totalCrew);

            reactorLeadShieldingThickness = _shieldingPartResource != null ? (_shieldingPartResource.info.density / 0.2268) * 20 * _shieldingPartResource.amount / _shieldingPartResource.maxAmount : 0;
            averageHabitatLeadEquivalantThickness = habitatMassMultiplier * averageCrewMassProtection / 0.2268;
            reactorShadowShieldLeadThickness = reactorMassMultiplier * reactorShadowShieldMassProtection;

            reactorShieldingGammaAttenuation = Math.Pow(1 - 0.9, reactorLeadShieldingThickness / 5);
            reactorShieldingNeutronAttenuation = Math.Pow(1 - 0.5, reactorLeadShieldingThickness / 6.8);

            averageHabitatLeadGammaAttenuation = Math.Pow(1 - 0.9, (averageHabitatLeadEquivalantThickness + reactorShadowShieldLeadThickness) / 5);
            averageHabitaNeutronAttenuation = Math.Pow(1 - 0.5, averageHabitatLeadEquivalantThickness / 6.8);

            gammaTransparency = Kerbalism.HasRadiationFixes ? 1 : Kerbalism.GammaTransparency(vessel.mainBody, vessel.altitude);

            averageDistanceModifier = 1 / (averageCrewDistanceToEmitter * averageCrewDistanceToEmitter);

            var averageDistanceGammaRayShieldingAttenuation = averageDistanceModifier * averageHabitatLeadGammaAttenuation;
            var averageDistanceNeutronShieldingAttenuation = averageDistanceModifier * averageHabitaNeutronAttenuation;

            var maxCoreRadiation = maxRadiation * reactorActivityFraction;

            reactorCoreGammaRadiation = maxCoreRadiation * averageDistanceGammaRayShieldingAttenuation * reactorShieldingGammaAttenuation;
            reactorCoreNeutronRadiation = maxCoreRadiation * averageDistanceNeutronShieldingAttenuation * reactorShieldingNeutronAttenuation * part.atmDensity * fuelNeutronsFraction * neutronScatteringRadiationMult * Math.Max(0, 1 - lithiumNeutronAbsorbtionFraction);

            var maxEngineRadiation = maxRadiation * exhaustActivityFraction;

            lostFissionFuelRadiation = maxEngineRadiation * averageDistanceNeutronShieldingAttenuation * (1 + part.atmDensity) * radioactiveFuelLeakFraction * neutronsExhaustRadiationMult;
            fissionExhaustRadiation = maxEngineRadiation * averageDistanceNeutronShieldingAttenuation * (1 + part.atmDensity) * (exhaustProducesNeutronRadiation ? neutronsExhaustRadiationMult : 0);
            fissionFragmentRadiation = maxEngineRadiation * averageDistanceGammaRayShieldingAttenuation * (exhaustProducesGammaRadiation ? gammaRayExhaustRadiationMult : 0);

            emitterRadiationRate = 0;
            emitterRadiationRate += reactorCoreGammaRadiation;
            emitterRadiationRate += reactorCoreNeutronRadiation;
            emitterRadiationRate += lostFissionFuelRadiation;
            emitterRadiationRate += fissionExhaustRadiation;
            emitterRadiationRate += fissionFragmentRadiation;

            emitterRadiationRate = gammaTransparency > 0 ? emitterRadiationRate / gammaTransparency : 0;

            SetRadiation(emitterRadiationRate);
        }

        public void SetRadiation(double radiation)
        {
            if (_emitterRadiationField == null || _emitterModule == null)
                return;

            if (double.IsInfinity(radiation) || double.IsNaN(radiation))
            {
                Debug.LogError("[KSPI]: InterstellarReactor emitterRadiationRate = " + radiation);
                return;
            }

            _emitterRadiationField.SetValue(radiation, _emitterModule);
        }
    }
}
