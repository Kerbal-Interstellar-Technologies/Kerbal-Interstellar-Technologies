
namespace KIT.BeamedPower
{
    public interface IMicrowavePowerTransmitter { };

    public interface IVesselMicrowavePersistence
    {
        double GetAvailablePowerInKW();

        bool IsActive { get; }
    }
    public interface IVesselRelayPersistence
    {
        bool IsActive { get; }
    }
}
