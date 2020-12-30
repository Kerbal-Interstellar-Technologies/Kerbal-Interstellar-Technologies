using KIT.ResourceScheduler;

namespace KIT.Interfaces
{
    public interface IChargedParticleSource : IPowerSource
    {
        double CurrentMeVPerChargedProduct { get; }

        void UseProductForPropulsion(IResourceManager resMan, double ratio, double propellantMassPerSecond);

        double MaximumChargedIspMult { get; }

        double MinimumChargedIspMult { get; }

        double MagneticNozzlePowerMult { get; }
    }
}
