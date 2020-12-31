using System;

namespace KIT.BeamedPower
{
    public class WaveLengthData
    {
        public Guid PartId { get; set; }
        public bool IsMirror { get; set; }
        public int Count { get; set; }
        public double ApertureSum { get; set; }
        public double Wavelength { get; set; }
        public double MinWavelength { get; set; }
        public double MaxWavelength { get; set; }
        public double AtmosphericAbsorption { get; set; }
        public double NuclearPower { get; set; }
        public double SolarPower { get; set; }
        public double PowerCapacity { get; set; }

        public override int GetHashCode()
        {
            return Wavelength.GetHashCode();
        }
    }
}
