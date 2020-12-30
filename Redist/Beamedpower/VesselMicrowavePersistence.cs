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
                double invSquareMult = Math.Pow(distanceBetweenVesselAndSun, 2) / Math.Pow(GameConstants.kerbin_sun_distance, 2);
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

        public Vessel Vessel => this.vessel;

        public bool IsActive { get; set; }

        public double SolarPower
        {
            get => this._solarPower;
            set => this._solarPower = value;
        }

        public double NuclearPower
        {
            get => this._nuclearPower;
            set => this._nuclearPower = value;
        }

        public double Aperture
        {
            get => _aperture != 0 ? this._aperture : 5;
            set => this._aperture = value;
        }

        public double PowerCapacity
        {
            get => _powerCapacity != 0 ? this._powerCapacity : 2;
            set => this._powerCapacity = value;
        }

        public List<WaveLengthData> SupportedTransmitWavelengths { get; }
    }
}
