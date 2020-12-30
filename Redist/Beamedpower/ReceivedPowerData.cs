using System.Collections.Generic;

namespace KIT.BeamedPower
{
    public class ReceivedPowerData
    {
        public IBeamedPowerReceiver Receiver { get; set; }
        public double CurrentReceivedPower { get; set; }
        public double MaximumReceivedPower { get; set; }
        public double AvailablePower { get; set; }
        public double ConsumedPower { get; set; }
        public bool IsAlive { get; set; }
        public double NetworkPower { get; set; }
        public double NetworkCapacity { get; set; }
        public double TransmitPower { get; set; }
        public double ReceiverEfficiency { get; set; }
        public double PowerUsageOthers { get; set; }
        public double RemainingPower { get; set; }
        public string Wavelengths { get; set; }
        public double Distance { get; set; }
        public long UpdateCounter { get; set; }

        public MicrowaveRoute Route { get; set; }
        public IList<VesselRelayPersistence> Relays { get; set; }
        public VesselMicrowavePersistence Transmitter { get; set; }
    }
}
