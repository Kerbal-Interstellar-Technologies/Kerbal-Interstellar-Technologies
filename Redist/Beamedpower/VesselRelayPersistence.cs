using System.Collections.Generic;

namespace KIT.BeamedPower
{
    public class VesselRelayPersistence : IVesselRelayPersistence
    {
        double _diameter;
        double _aperture;
        double _powerCapacity;
        double _minimumRelayWavelength;
        double _maximumRelayWavelength;

        public VesselRelayPersistence(Vessel vessel)
        {
            Vessel = vessel;
            SupportedTransmitWavelengths = new List<WaveLengthData>();
        }

        public List<WaveLengthData> SupportedTransmitWavelengths { get; }

        public Vessel Vessel { get; }

        public bool IsActive { get; set; }

        public double Diameter
        {
            get => _diameter != 0 ? _diameter : Aperture; // fall back to aperture when diameter is not available
            set => _diameter = value;
        }

        public double Aperture
        {
            get => _aperture != 0 ? _aperture : 1;
            set => _aperture = value;
        }

        public double PowerCapacity
        {
            get => _powerCapacity != 0 ? _powerCapacity : 1000;
            set => _powerCapacity = value;
        }

        public double MinimumRelayWavelength
        {
            get => _minimumRelayWavelength != 0 ? _minimumRelayWavelength : 0.003189281;
            set => _minimumRelayWavelength = value;
        }

        public double MaximumRelayWavelength
        {
            get => _maximumRelayWavelength != 0 ? _maximumRelayWavelength: 0.008565499;
            set => _maximumRelayWavelength = value;
        }
    }
}
