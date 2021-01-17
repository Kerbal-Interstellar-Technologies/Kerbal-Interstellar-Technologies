using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIT.ResourceScheduler
{
    public class VesselInfo
    {
        public Vessel Vessel { get; }
        public string[] SolarPowerModules { get; }

        public VesselInfo(Vessel vessel, string[] solarPowerModules)
        {
            Vessel = vessel;
            SolarPowerModules = solarPowerModules;
        }
    }
}
