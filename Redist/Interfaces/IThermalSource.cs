using System;

namespace KIT.Interfaces
{
    public enum ElectricGeneratorType { Unknown = 0, Thermal = 1, ChargedParticle = 2 };

    public interface IThermalReceiver
    {
        void AttachThermalReceiver(Guid key, double radius);

        void DetachThermalReceiver(Guid key);

        double GetFractionThermalReceiver(Guid key);

        double ThermalTransportationEfficiency { get; }
    }
}

