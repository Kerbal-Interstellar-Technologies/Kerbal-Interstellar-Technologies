namespace KIT.BeamedPower
{
    /// <summary>
    /// Storage class required for relay route calculation
    /// </summary>
    public class MicrowaveRoute
    {
        public double Efficiency { get; set; }
        public WaveLengthData WavelengthData { get; }
        public double WaveLength => WavelengthData.Wavelength;
        public double MinimumWaveLength => WavelengthData.MinWavelength;
        public double MaximumWaveLength => WavelengthData.MaxWavelength;
        public double Distance { get; set; }
        public double FacingFactor { get; set; }
        public VesselRelayPersistence PreviousRelay { get; set; }
        public double SpotSize { get; set; }

        public MicrowaveRoute(double efficiency, double distance, double facingFactor, double spotSize, WaveLengthData wavelengthData, VesselRelayPersistence previousRelay = null)
        {
            Efficiency = efficiency;
            Distance = distance;
            FacingFactor = facingFactor;
            PreviousRelay = previousRelay;
            SpotSize = spotSize;
            WavelengthData = wavelengthData;
        }
    }
}
