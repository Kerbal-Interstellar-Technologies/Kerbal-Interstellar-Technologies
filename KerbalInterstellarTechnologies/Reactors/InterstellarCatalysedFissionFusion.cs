using KIT.PowerManagement.Interfaces;

namespace KIT.Reactors
{
    [KSPModule("Antimatter Initiated Reactor")]
    class InterstellarCatalysedFissionFusion : InterstellarReactor, IFNChargedParticleSource
    {
		[KSPField]
		public double magneticNozzlePowerMult = 1;

        public override bool IsFuelNeutronRich => CurrentFuelMode != null && !CurrentFuelMode.Aneutronic;

        public double MaximumChargedIspMult => 1;

        public double MinimumChargedIspMult => 100;

        public override double MagneticNozzlePowerMult => magneticNozzlePowerMult;
    }
}
