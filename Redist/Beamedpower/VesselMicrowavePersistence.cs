using System;
using System.Collections.Generic;
using KIT.Extensions;

namespace KIT.BeamedPower
{
    public class VesselMicrowavePersistence : IVesselMicrowavePersistence
    {
        private double _aperture;
        private double _powerCapacity;

        private CelestialBody _localStar;
        public CelestialBody LocalStar
        {
            get
            {
                if (_localStar == null)
                {
                    _localStar = Vessel.GetLocalStar();
                }
                return _localStar;
            }
        }

        public VesselMicrowavePersistence(Vessel vessel)
        {
            this.Vessel = vessel;
            SupportedTransmitWavelengths = new List<WaveLengthData>();
        }

        public bool HasPower => NuclearPower > 0 || SolarPower > 0;

        public double GetAvailablePowerInKW()
        {
            double power = 0;
            if (SolarPower > 0.001 && Vessel.LineOfSightToSun(LocalStar))
            {
                var distanceBetweenVesselAndSun = Vector3d.Distance(Vessel.GetVesselPos(), LocalStar.position);
                double invSquareMult = Math.Pow(distanceBetweenVesselAndSun, 2) / Math.Pow(GameConstants.KerbinSunDistance, 2);
                power = SolarPower / invSquareMult;
            }

            power += NuclearPower;

            return (double)Math.Min(1000 * _powerCapacity, power);
        }

        public double GetAvailablePowerInMW()
        {
            return GetAvailablePowerInKW()/1000;
        }

        public Vessel Vessel { get; }

        public bool IsActive { get; set; }

        public double SolarPower { get; set; }

        public double NuclearPower { get; set; }

        public double Aperture
        {
            get => _aperture != 0 ? _aperture : 5;
            set => _aperture = value;
        }

        public double PowerCapacity
        {
            get => _powerCapacity != 0 ? _powerCapacity : 2;
            set => _powerCapacity = value;
        }

        public List<WaveLengthData> SupportedTransmitWavelengths { get; }
    }
}
