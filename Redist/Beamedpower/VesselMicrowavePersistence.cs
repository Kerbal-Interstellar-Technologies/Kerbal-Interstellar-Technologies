using System;
using System.Collections.Generic;
using KIT.Extensions;

namespace KIT.BeamedPower
{
    public class VesselMicrowavePersistence : IVesselMicrowavePersistence
    {
        readonly Vessel vessel;

        private double _aperture;
        private double _nuclearPower;
        private double _solarPower;
        private double _powerCapacity;

        private CelestialBody _localStar;
        public CelestialBody LocalStar
        {
            get
            {
                if (_localStar == null)
                {
                    _localStar = vessel.GetLocalStar();
                }
                return _localStar;
            }
        }

        public VesselMicrowavePersistence(Vessel vessel)
        {
            this.vessel = vessel;
            SupportedTransmitWavelengths = new List<WaveLengthData>();
        }

        public bool HasPower => _nuclearPower > 0 || _solarPower > 0;

        public double GetAvailablePowerInKW()
        {
            double power = 0;
            if (_solarPower > 0.001 && vessel.LineOfSightToSun(LocalStar))
            {
                var distanceBetweenVesselAndSun = Vector3d.Distance(vessel.GetVesselPos(), LocalStar.position);
                double invSquareMult = Math.Pow(distanceBetweenVesselAndSun, 2) / Math.Pow(GameConstants.KerbinSunDistance, 2);
                power = _solarPower / invSquareMult;
            }

            power += _nuclearPower;

            var finalPower = Math.Min(1000 * _powerCapacity, power);

            return finalPower;
        }

        public double GetAvailablePowerInMW()
        {
            return GetAvailablePowerInKW()/1000;
        }

        public Vessel Vessel => vessel;

        public bool IsActive { get; set; }

        public double SolarPower
        {
            get => _solarPower;
            set => _solarPower = value;
        }

        public double NuclearPower
        {
            get => _nuclearPower;
            set => _nuclearPower = value;
        }

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
