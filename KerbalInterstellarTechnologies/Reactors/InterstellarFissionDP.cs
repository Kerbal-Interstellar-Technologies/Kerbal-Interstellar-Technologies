using KIT.Powermanagement.Interfaces;

namespace KIT.Reactors
{
    [KSPModule("Fission Fragment Reactor")]
    class InterstellarFissionDP : InterstellarFissionPB, IFNChargedParticleSource
    {
        [KSPField]
        public double magneticNozzlePowerMult = 1;

        public double MaximumChargedIspMult => (float)maximumChargedIspMult;

        public double MinimumChargedIspMult => (float)minimumChargdIspMult;

        public override double MagneticNozzlePowerMult => magneticNozzlePowerMult;
    }
}
