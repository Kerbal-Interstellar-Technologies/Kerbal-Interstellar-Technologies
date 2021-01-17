using KIT.Resources;
using KIT.ResourceScheduler;
using KSP.Localization;
using System;
using System.Linq;
using LibNoise.Modifiers;
using UnityEngine;

namespace KIT.Reactors
{
    public class KITRadioisotopeGenerator : PartModule, IKITModule
    {
        public const string GroupDisplayName = "Radioisotope Generator";
        public const string GroupName = "KITRadioisotopeGenerator";

        [KSPField(isPersistant = true)] public double peltierEfficiency;
        [KSPField(isPersistant = true)] public double wattsPerGram;
        [KSPField(isPersistant = true)] public double massInKilograms = 1;
        [KSPField(isPersistant = true)] public double halfLifeInSeconds;
        [KSPField(isPersistant = true)] public double halfLifeInKerbinSeconds;

        // how long has this part existed for?
        [KSPField(isPersistant = true)] public double partLifeStarted;

        [KSPField(isPersistant = true, guiName = "#LOC_KIT_RTG_MassRemaining", groupDisplayName = GroupDisplayName, groupName = GroupName, guiActive = true, guiActiveEditor = true, guiUnits = " kg")] public double massRemaining;
        [KSPField(isPersistant = true, guiName = "#LOC_KIT_RTG_RadioactiveIsotope", groupDisplayName = GroupDisplayName, groupName = GroupName, guiActive = true, guiActiveEditor = true)] public string radioisotopeFuel;

        [KSPField(guiActive = true, guiActiveEditor = true, groupDisplayName = GroupDisplayName, groupName = GroupName, guiName = "#LOC_KIT_RTG_Current_Power_Output", guiUnits = " KW", guiFormat = "F4")] public double currentPowerOutput;
        [KSPField(guiActive = true, guiActiveEditor = true, groupDisplayName = GroupDisplayName, groupName = GroupName, guiName = "#LOC_KIT_RTG_WasteHeatOutput", guiUnits = " KW", guiFormat = "F4")] public double wasteHeatOutput;


        /*
         * Per AntaresMC, The typical RTG has a mass ratio of about 1.1 and a peltier of about 10% efficiency.
         * With 80kg that would give about 8kg of Pu238, heat output of about 5kW and power output of about 1/2kW. Seems about right,
         * just lets say they add a bit more plutonium, pr their peltier is a bit better
         * 
         * I would add 3 tech levels, RTG, using the same calculations as the peltier generator on areactor with a core temp
         * proportional yo how full of trash in respect to fuel is it, or just a flat 15%; alphavoltaics (first upgrade), a flat
         * 50% in heavy fuels (actinides, Pu238, Po210 if it gets added, etc); and ßvoltaics, a flat lets say 40% in light
         * fuels (T and FissionProducts when get added) and maybe an improvement to 60% in ghe heavg ones, idk
         * 
         * Ok, so, actinides would produce about a watt per kg forever, alpha emitter
         * Pu238 (that is extracted from actinide waste) about 500W/kg, halving each 90ish years, alpha emitter
         * FissionProducts (what is left after all fuel has been burned and reprocessed over and over again
         * would give about 250W/kg halving each 30ish years, ß emiter T would give about 36kW/kg, ß emitter,
         * halves at about 12y
         * 
         * I didnt find the notes, had to calculate it again :tired_face:
         * As a bonus, but I dont think its worth the extra complexity, Po210 is an alpha emitter that gives about 140kW/kg
         * and halves in about 5 months
         */

        /*
         * To summarize the above,
         *   - Actinides - 1W / kg / forever
         *   - FissionProducts - 250W / kg / 30 years
         *   - Plutonium-238 - 500W / kg / 90 years
         *   - Tritium - 36kW / kg / 12 years
         *   
         * FissionProducts seems worse than Plutonium-238 
         */


        private static void PowerGeneratedPerSecond(double massRemaining, double massInKilograms, double wattsPerGram, double peltierEfficiency, out double electricalCurrentInKW, out double wasteHeatInKW)
        {
            // convert kg to grams, then calculate the heat generated
            var mass = Math.Max(massRemaining * 1000, massInKilograms * 1000 * HighLogic.CurrentGame.Parameters.CustomParams<KITGamePlayParams>().MinimumRtgOutput);
            var heatInWatts = mass * wattsPerGram;
            var powerInWatts = heatInWatts * peltierEfficiency;

            var heatGeneratedInKW = heatInWatts / (1e+3);
            electricalCurrentInKW = powerInWatts / (1e+3);
            wasteHeatInKW = heatGeneratedInKW - electricalCurrentInKW;
        }

        PartResourceDefinition _decayResource;           // source
        PartResourceDefinition _decayProductResource;    // destination
        bool _resourceNotDefined;
        ResourceName _decayProductId;

        private void DecayFuel(IResourceManager resMan)
        {
            if (HighLogic.CurrentGame.Parameters.CustomParams<KITGamePlayParams>().PreventRadioactiveDecay) return;

            double halfLife = GameSettings.KERBIN_TIME ? halfLifeInKerbinSeconds : halfLifeInSeconds;
            double originalMassRemaining = massRemaining;

            var currentPartLifetime = Planetarium.GetUniversalTime() - partLifeStarted;

            massRemaining = massInKilograms * Math.Pow(2, (-currentPartLifetime) / halfLife);

            if (_resourceNotDefined) return;

            var productsToGenerateInKG = originalMassRemaining - massRemaining;
            if (_decayResource == null || _decayProductResource == null)
            {
                var config = GameDatabase.Instance.GetConfigNodes("KIT_Radioactive_Decay");
                if (config == null || !config.Any())
                {
                    _resourceNotDefined = true;
                    Debug.Log($"[KITRadioisotopeGenerator] can't find KIT_Radioactive_Decay configuration");
                    return;
                }

                var node = config[0].GetNode(radioisotopeFuel);
                if (node == null)
                {
                    _resourceNotDefined = true;
                    Debug.Log($"[KITRadioisotopeGenerator] {radioisotopeFuel} has no decay products defined");
                    return;
                }

                string decayProduct = "";
                if (node.TryGetValue("product", ref decayProduct) == false)
                {
                    _resourceNotDefined = true;
                    Debug.Log($"[KITRadioisotopeGenerator] {radioisotopeFuel} configuration has no product to decay into defined");
                    return;
                }

                _decayResource = PartResourceLibrary.Instance.GetDefinition(radioisotopeFuel);
                _decayProductResource = PartResourceLibrary.Instance.GetDefinition(decayProduct);

                if (_decayResource == null || _decayProductResource == null)
                {
                    _resourceNotDefined = true;
                    Debug.Log($"[KITRadioisotopeGenerator] could not get definitions for {(_decayResource == null ? radioisotopeFuel + " " : "")}{(_decayProductResource == null ? decayProduct : "")}");
                    return;
                }

                _decayProductId = KITResourceSettings.NameToResource(decayProduct);
                if (_decayProductId == ResourceName.Unknown)
                {
                    _resourceNotDefined = true;
                    Debug.Log($"[KITRadioisotopeGenerator] could not get KIT definition for {decayProduct}");
                    return;
                }
            }

            var densityRatio = _decayResource.density / _decayProductResource.density;
            resMan.Produce(_decayProductId, productsToGenerateInKG * densityRatio);

            // decay products being generated.
        }

        public override void OnStart(StartState state)
        {
            if (partLifeStarted == 0)
            {
                if (state != StartState.Editor) partLifeStarted = Planetarium.GetUniversalTime();
                massRemaining = massInKilograms;
                halfLifeInKerbinSeconds = (halfLifeInSeconds / 60 / 60 / 24 / 365) * 426 * 6 * 60 * 60;
            }

        }

        public void Update()
        {
            PowerGeneratedPerSecond(massRemaining, massInKilograms, wattsPerGram, peltierEfficiency, out currentPowerOutput, out wasteHeatOutput);
        }

        public ModuleConfigurationFlags ModuleConfiguration() => ModuleConfigurationFlags.LocalResources |
                                                                 ModuleConfigurationFlags.First |
                                                                 ModuleConfigurationFlags.SupplierOnly;

        public void KITFixedUpdate(IResourceManager resMan)
        {
            if (HighLogic.LoadedSceneIsEditor) return;

            PowerGeneratedPerSecond(massRemaining, massInKilograms, wattsPerGram, peltierEfficiency, out var energyExtractedInKW, out var wasteHeatInKW);

            resMan.Produce(ResourceName.ElectricCharge, energyExtractedInKW);
            resMan.Produce(ResourceName.WasteHeat, wasteHeatInKW);

            DecayFuel(resMan);
        }

        private static readonly string _kitPartName = Localizer.Format("#LOC_KIT_RTG_PartName");
        public string KITPartName() => _kitPartName;

        public override string GetInfo()
        {
            return Localizer.Format("#LOC_KIT_RTG_GetInfo");
        }

        public static string KITPartName(ProtoPartSnapshot protoPartSnapshot,
            ProtoPartModuleSnapshot protoPartModuleSnapshot) => _kitPartName;

        public static void KITBackgroundUpdate(IResourceManager resMan, Vessel vessel, ProtoPartSnapshot protoPartSnapshot,
            ProtoPartModuleSnapshot protoPartModuleSnapshot, Part part)
        {
            var massRemaining = Lib.GetDouble(protoPartModuleSnapshot, nameof(KITRadioisotopeGenerator.massRemaining));
            var massInKilograms =
                Lib.GetDouble(protoPartModuleSnapshot, nameof(KITRadioisotopeGenerator.massInKilograms));
            var wattsPerGram = Lib.GetDouble(protoPartModuleSnapshot, nameof(KITRadioisotopeGenerator.wattsPerGram));
            var peltierEfficiency =
                Lib.GetDouble(protoPartModuleSnapshot, nameof(KITRadioisotopeGenerator.peltierEfficiency));
            
            
            PowerGeneratedPerSecond(massRemaining, massInKilograms, wattsPerGram, peltierEfficiency, out var electricalCurrent, out var wasteHeat);
            resMan.Produce(ResourceName.ElectricCharge, electricalCurrent);
            resMan.Produce(ResourceName.WasteHeat, wasteHeat);
        }

        public static ModuleConfigurationFlags BackgroundModuleConfiguration(
            ProtoPartModuleSnapshot protoPartModuleSnapshot) => ModuleConfigurationFlags.LocalResources |
                                                                ModuleConfigurationFlags.First |
                                                                ModuleConfigurationFlags.SupplierOnly;
    }
}
