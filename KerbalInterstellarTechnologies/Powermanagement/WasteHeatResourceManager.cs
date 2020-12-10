using KIT.Constants;
using System;
using KIT.Resources;
using UnityEngine;

namespace KIT.Powermanagement
{
    internal class WasteHeatResourceManager : ResourceManager
    {
        private const double PASSIVE_TEMP_P4 = 2947.295521;

        public double TemperatureRatio { get; private set; }

        public double RadiatorEfficiency { get; private set; }

        public WasteHeatResourceManager(Guid overmanagerId, PartModule pm) : base(overmanagerId, pm, KITResourceSettings.WasteHeat, FNRESOURCE_FLOWTYPE_EVEN)
        {
            WindowPosition = new Rect(600, 600, LABEL_WIDTH + VALUE_WIDTH + PRIORITY_WIDTH, 50);
            TemperatureRatio = 0.0;
            RadiatorEfficiency = 0.0;
        }

        protected override double AdjustSupplyComplete(double timeWarpDeltaTime, double powerToExtract)
        {
            // passive dissip of waste heat - a little bit of this
            double vesselMass = Vessel.totalMass;
            powerToExtract += 2.0 * PASSIVE_TEMP_P4 * GameConstants.stefan_const * vesselMass * timeWarpDeltaTime;

            if (Vessel.altitude <= PluginHelper.GetMaxAtmosphericAltitude(Vessel.mainBody))
            {
                // passive convection - a lot of this
                double pressure = FlightGlobals.getStaticPressure(Vessel.transform.position) / GameConstants.EarthAtmospherePressureAtSeaLevel;
                powerToExtract += 40.0e-6 * GameConstants.rad_const_h * pressure * vesselMass * timeWarpDeltaTime;
            }
            return powerToExtract;
        }

        public override void update(long counter)
        {
            base.update(counter);
            TemperatureRatio = Math.Pow(ResourceFillFraction, 0.75);
            RadiatorEfficiency = 1.0 - Math.Pow(1.0 - ResourceFillFraction, 400.0);
        }
    }
}
