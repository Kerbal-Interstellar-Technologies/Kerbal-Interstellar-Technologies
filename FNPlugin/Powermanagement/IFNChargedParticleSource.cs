using KIT.Redist;

namespace KIT.Powermanagement
{
    public interface IFNChargedParticleSource : IChargedParticleSource
    {
        bool MayExhaustInAtmosphereHomeworld { get; }
        bool MayExhaustInLowSpaceHomeworld { get; }
    }
}
