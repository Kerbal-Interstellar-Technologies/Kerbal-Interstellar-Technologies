using KIT.Resources;
using KIT.ResourceScheduler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIT.Reactors
{
    public class KITRadioisotopeGenerator : PartModule, IKITMod
    {
        [KSPField(isPersistant = true)] public double upgradeMultiplier = 1;
        [KSPField(isPersistant = true)] public double powerMultiplier = 0.006; // (8 / 0.75); // 8kg of Pu238 gives 0.75W output
        [KSPField(isPersistant = true)]
        public double halfLife = (
            GameSettings.KERBIN_TIME ?
                87.7 * 426 * 6 * 60 * 60 :
                87.7 * 365 * 24 * 60 * 60
        );

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

        public void KITFixedUpdate(IResourceManager resMan)
        {
            var power = part.Resources[0].amount * powerMultiplier * upgradeMultiplier;
            resMan.ProduceResource(ResourceName.ElectricCharge, power);
        }

        public string KITPartName() => "Radioisotope Generator";

        public override string GetInfo()
        {
            return "";
        }

        public ResourcePriorityValue ResourceProcessPriority() => ResourcePriorityValue.First | ResourcePriorityValue.SupplierOnlyFlag;
    }
}
