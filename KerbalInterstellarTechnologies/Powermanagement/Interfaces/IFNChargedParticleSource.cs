using KIT.Interfaces;

namespace KIT.Powermanagement.Interfaces
{
    public interface IFNChargedParticleSource : IChargedParticleSource
    {
        bool MayExhaustInAtmosphereHomeworld { get; }
        bool MayExhaustInLowSpaceHomeworld { get; }
    }
}
