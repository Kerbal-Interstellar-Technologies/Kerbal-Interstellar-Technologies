using KIT.ResourceScheduler;

namespace KIT.Redist
{
    public interface IChargedParticleSource : IPowerSource
    {
        double CurrentMeVPerChargedProduct { get; }

        void UseProductForPropulsion(IResourceManager resMan, double ratio, double propellantMassPerSecond);

        double MaximumChargedIspMult { get; }

        double MinimumChargdIspMult { get; }

        double MagneticNozzlePowerMult { get; }
    }
}
