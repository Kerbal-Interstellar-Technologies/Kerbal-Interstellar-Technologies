using KIT.Resources;
using KIT.ResourceScheduler;
using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KIT.Reactors
{
    public class KITRadioisotopeGenerator : PartModule, IKITMod
    {
        public const string GROUP_DISPLAY_NAME = "Radioisotope Generator";
        public const string GROUP_NAME = "KITRadioisotopeGenerator";

        [KSPField(isPersistant = true)] public double peltierEfficency;
        [KSPField(isPersistant = true)] public double wattsPerGram;
        [KSPField(isPersistant = true)] public double massInKilograms = 1;
        [KSPField(isPersistant = true)] public double halfLifeInSeconds;
        [KSPField(isPersistant = true)] public double halfLifeInKerbinSeconds;

        // how long has this part existed for?
        [KSPField(isPersistant = true)] public double partLifeTimeInSeconds;

        [KSPField(isPersistant = true, guiName = "#LOC_KIT_RTG_MassRemaining", groupDisplayName = GROUP_DISPLAY_NAME, groupName = GROUP_NAME, guiActive = true, guiActiveEditor = true, guiUnits = " kg")] public double massRemaining;
        [KSPField(isPersistant = true, guiName = "#LOC_KIT_RTG_RadioactiveIsotope", groupDisplayName = GROUP_DISPLAY_NAME, groupName = GROUP_NAME, guiActive = true, guiActiveEditor = true)] public string radioisotopeFuel;

        [KSPField(guiActive = true, guiActiveEditor = true, groupDisplayName = GROUP_DISPLAY_NAME, groupName = GROUP_NAME, guiName = "#LOC_KIT_RTG_Current_Power_Output", guiUnits = " KW", guiFormat = "F4")] public double currentPowerOutput;
        [KSPField(guiActive = true, guiActiveEditor = true, groupDisplayName = GROUP_DISPLAY_NAME, groupName = GROUP_NAME, guiName = "#LOC_KIT_RTG_WasteHeatOutput", guiUnits = " KW", guiFormat = "F4")] public double wasteHeatOutput;


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


        private void PowerGeneratedPerSecond(out double electricalCurrentInKW, out double wasteHeatInKW)
        {
            // convert kg to grams, then calculate the heat generated
            var heatInWatts = (massRemaining * 1000) * wattsPerGram;
            var powerInWatts = heatInWatts * peltierEfficency;

            var heatGeneratedInKW = heatInWatts / (1e+3);
            electricalCurrentInKW = powerInWatts / (1e+3);
            wasteHeatInKW = heatGeneratedInKW - electricalCurrentInKW;
        }

        private void DecayFuel(IResourceManager resMan)
        {
            double halfLife = GameSettings.KERBIN_TIME ? halfLifeInKerbinSeconds : halfLifeInSeconds;

            double perSecondDecayConstant = 1 / halfLife;
            double originalMassRemaining = massRemaining;
            
            massRemaining = massInKilograms * Math.Pow(2, (-partLifeTimeInSeconds) / halfLife);

            var productsToGenerateInKG = originalMassRemaining - massRemaining;

            partLifeTimeInSeconds += resMan.FixedDeltaTime();
            // decay products being generated.
        }

        public override void OnStart(StartState state)
        {
            if (partLifeTimeInSeconds == 0)
            {
                massRemaining = massInKilograms;
                halfLifeInKerbinSeconds = (halfLifeInSeconds / 60 / 60 / 24 / 365) * 426 * 6 * 60 * 60;
            }

        }

        public void Update()
        {
            PowerGeneratedPerSecond(out currentPowerOutput, out wasteHeatOutput);
        }

        public void KITFixedUpdate(IResourceManager resMan)
        {
            if (!HighLogic.LoadedSceneIsFlight) return;

            double energyExtractedInKW, wasteHeatInKW;

            PowerGeneratedPerSecond(out energyExtractedInKW, out wasteHeatInKW);
            resMan.ProduceResource(ResourceName.ElectricCharge, energyExtractedInKW);
            resMan.ProduceResource(ResourceName.WasteHeat, wasteHeatInKW);
            
            DecayFuel(resMan);
        }

        private string _KITPartName = Localizer.Format("#LOC_KIT_RTG_PartName");
        public string KITPartName() => _KITPartName;

        public override string GetInfo()
        {
            return Localizer.Format("#LOC_KIT_RTG_GetInfo");
        }

        public ResourcePriorityValue ResourceProcessPriority() => ResourcePriorityValue.First | ResourcePriorityValue.SupplierOnlyFlag;
    }
}
