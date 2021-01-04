using KIT.ResourceScheduler;
using KSP.Localization;

namespace KIT.Resources
{
    /*
    
    From the KSPIE resource management code, ran at the end of processing

    Continuing to have the built in passive dissipation and convection would be a good idea.

        


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


    */

    public class VesselHeatDissipation : IKITModule
    {
        public Vessel Vessel;

        public VesselHeatDissipation(Vessel v)
        {
            Vessel = v;
        }

        private static double GetMaxAtmosphericAltitude(CelestialBody body)
        {
            if (!body.atmosphere) return 0;
            return body.atmosphereDepth;
        }

        // I have no idea where this value came from, or how it was calculated.
        private const double PassiveTempP4 = 2947.295521;

        private double AdjustSupplyComplete(double powerToExtract)
        {
            // passive dissipation of waste heat - a little bit of this
            double vesselMass = Vessel.totalMass;
            powerToExtract += 2.0 * PassiveTempP4 * GameConstants.StefanConst * vesselMass;

            if (Vessel.mainBody.atmosphere && Vessel.altitude <= Vessel.mainBody.atmosphereDepth)
            {
                // passive convection - a lot of this
                double pressure = FlightGlobals.getStaticPressure(Vessel.transform.position) / GameConstants.EarthAtmospherePressureAtSeaLevel;
                powerToExtract += 40.0e-6 * GameConstants.RadConstH * pressure * vesselMass;
            }

            return powerToExtract;
        }


        public void KITFixedUpdate(IResourceManager resMan)
        {
            /*
            // substract available resource amount to get delta resource change
            double supply = current.Supply - Math.Max(availableAmount, 0);
            double missingAmount = maxAmount - availableAmount;
            double powerToExtract = AdjustSupplyComplete(timeWarpDT, -supply * timeWarpDT);

            // Update storage
            if (powerToExtract > 0.0)
                powerToExtract = Math.Min(powerToExtract, availableAmount);
            else
                powerToExtract = Math.Max(powerToExtract, -missingAmount);

            if (!powerToExtract.IsInfinityOrNaN())
            {
                availableAmount += part.RequestResource(resourceDefinition.id, powerToExtract);
            }
            */

            // Eh, I am not sure this is working correctly at the moment. TBD.

            /*
            var stats = resMan.ResourceProductionStats(ResourceName.WasteHeat);

            double availableAmount = resMan.ResourceCurrentCapacity(ResourceName.WasteHeat);

            double supply = stats.CurrentSupplied() - stats.CurrentlyRequested();
            double missingAmount = resMan.ResourceSpareCapacity(ResourceName.WasteHeat);

            double powerToExtract = AdjustSupplyComplete(-supply);

            if (powerToExtract <= 0) return;

            Debug.Log($"[VesselHeatDissipation] powerToExtract is {powerToExtract}, missingAmount is {missingAmount}, and availableAmount is {availableAmount}");

            resMan.ConsumeResource(ResourceName.WasteHeat, Math.Min(availableAmount, powerToExtract));
            */
        }


        private string _KITPartName;
        public string KITPartName()
        {
            if (string.IsNullOrEmpty(_KITPartName)) _KITPartName = Localizer.Format("#LOC_KIT_Vessel_Heat_Dissipation");
            return _KITPartName;
        }

        public bool ModuleConfiguration(out int priority, out bool supplierOnly, out bool hasLocalResources)
        {
            priority = 5;
            supplierOnly = false;
            hasLocalResources = false;
            return true;
        }

    }

}
