using System;
using KIT.Powermanagement.Interfaces;

namespace KIT.Reactors
{
    [KSPModule("Antimatter Reactor")]
    class FNAntimatterReactor : InterstellarReactor, IFNChargedParticleSource
    {
        [KSPField]
        public double magneticNozzlePowerMult = 0.1;
        [KSPField]
        public double maximumChargedIspMult = 100;
        [KSPField]
        public double minimumChargedIspMult = 10;
        [KSPField]
        public double chargedProductMult = 1;
        [KSPField]
        public double chargedProductExp = 0;

        public override string TypeName => (isupgraded ? upgradedName != "" ? upgradedName : originalName : originalName) + " Antimatter Reactor";

        public override double CurrentMeVPerChargedProduct => CurrentFuelMode != null ? CurrentFuelMode.MeVPerChargedProduct * chargedProductMult * Math.Pow(massDifference, chargedProductExp) : 0;

        public double MaximumChargedIspMult => maximumChargedIspMult;

        public double MinimumChargedIspMult => minimumChargedIspMult;

        public override double MagneticNozzlePowerMult => magneticNozzlePowerMult;
    }
}
