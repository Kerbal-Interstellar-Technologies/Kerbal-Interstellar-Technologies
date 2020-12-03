using KIT.Powermanagement;

namespace KIT.Reactors
{
    [KSPModule("Fission Fragment Reactor")]
    class InterstellarFissionDP : InterstellarFissionPB, IFNChargedParticleSource
    {
        [KSPField]
        public double magneticNozzlePowerMult = 1;

        public double MaximumChargedIspMult { get { return (float)maximumChargedIspMult; } }

        public double MinimumChargdIspMult { get { return (float)minimumChargdIspMult; } }

        public override double MagneticNozzlePowerMult { get { return magneticNozzlePowerMult; } }
    }
}
