using System;
using System.Collections.Generic;
using KIT.Resources;
using KIT.Science;

namespace KIT.Extensions
{
    public static class CelestialBodyExtensions
    {
        static readonly Dictionary<string, BeltData> BeltDataCache = new Dictionary<string, BeltData>();

        class BeltData
        {
            public double Density;
            public double Ampere;
        }

        private const double SqRt2 = 1.4142135624;
        private const double Sqrt2DivPi = 0.79788456080;

        public static double GetBeltAntiparticles(this CelestialBody body, CelestialBody homeworld, double altitude, double lat)
        {
            if (body.flightGlobalsIndex != 0 && altitude <= PluginHelper.GetMaxAtmosphericAltitude(body))
                return 0;
            
            if (!BeltDataCache.TryGetValue(body.name, out BeltData beltData))
            {
                double relrp = body.Radius / homeworld.Radius;
                double relrt = body.rotationPeriod / homeworld.rotationPeriod;

                beltData = new BeltData()
                {
                    Density = body.Mass / homeworld.Mass * relrp / relrt * 50,
                    Ampere = 1.5 * homeworld.Radius * relrp / SqRt2,
                };

                BeltDataCache.Add(body.name, beltData);
            }

            double beltParticles = beltData.Density
                * Sqrt2DivPi
                * Math.Pow(altitude, 2)
                * Math.Exp(-Math.Pow(altitude, 2) / (2 * Math.Pow(beltData.Ampere, 2)))
                / (Math.Pow(beltData.Ampere, 3));

            if (KopernicusHelper.GetLuminocity(body) > 0)
                beltParticles /= 1000;

            if (body.atmosphere)
                beltParticles *= body.atmosphereDepth / 70000;
            else
                beltParticles *= 0.01;

            return beltParticles * Math.Abs(Math.Cos(lat / 180 * Math.PI)) * body.SpecialMagneticFieldScaling();
        }


        public static double GetProtonRadiationLevel(this CelestialBody body, CelestialBody homeworld, double altitude, double lat)
        {
            lat = lat / 180 * Math.PI;

            double atmosphere = FlightGlobals.getStaticPressure(altitude, body) / GameConstants.EarthAtmospherePressureAtSeaLevel;
            double relrp = body.Radius / homeworld.Radius;
            double relrt = body.rotationPeriod / homeworld.rotationPeriod;
            double peakBelt = body.GetPeakProtonBeltAltitude(homeworld);
            double a = peakBelt / SqRt2;
            double beltParticles = Sqrt2DivPi * Math.Pow(altitude, 2) * Math.Exp(-Math.Pow(altitude, 2) / (2 * Math.Pow(a, 2))) / (Math.Pow(a, 3));
            beltParticles = beltParticles * relrp / relrt * 50;

            if (KopernicusHelper.IsStar(body))
                beltParticles /= 1000;

            if (body.atmosphere)
                beltParticles *= body.atmosphereDepth / 70000;
            else
                beltParticles *= 0.01;

            beltParticles = beltParticles * Math.Abs(Math.Cos(lat)) * body.SpecialMagneticFieldScaling() * Math.Exp(-atmosphere);

            return beltParticles;
        }

        public static double GetPeakProtonBeltAltitude(this CelestialBody body, CelestialBody homeworld)
        {
            double relrp = body.Radius / homeworld.Radius;
            return (double)(1.5 * homeworld.Radius * relrp);
        }

        public static double GetElectronRadiationLevel(this CelestialBody body, CelestialBody homeworld, double altitude, double lat)
        {
            lat = lat / 180 * Math.PI;
            double atmosphere = FlightGlobals.getStaticPressure(altitude, body) / GameConstants.EarthAtmospherePressureAtSeaLevel;
            double atmosphereHeight = PluginHelper.GetMaxAtmosphericAltitude(body);
            double atmosphereScaling = Math.Exp(-atmosphere);

            double relrp = body.Radius / homeworld.Radius;
            double relrt = body.rotationPeriod / homeworld.rotationPeriod;

            double peakBelt2 = body.GetPeakElectronBeltAltitude(homeworld);
            double b = peakBelt2 / SqRt2;
            double beltParticles = 0.9 * Sqrt2DivPi * Math.Pow(altitude, 2) * Math.Exp(-Math.Pow(altitude, 2) / (2 * Math.Pow(b, 2))) / (Math.Pow(b, 3));
            beltParticles = beltParticles * relrp / relrt * 50;

            if (KopernicusHelper.IsStar(body))
                beltParticles /= 1000;

            if (body.atmosphere)
                beltParticles *= body.atmosphereDepth / 70000;
            else
                beltParticles *= 0.01;

            beltParticles = beltParticles * Math.Abs(Math.Cos(lat)) * body.SpecialMagneticFieldScaling() * atmosphereScaling;

            return beltParticles;
        }

        public static double GetPeakElectronBeltAltitude(this CelestialBody body, CelestialBody homeworld)
        {
            double relrp = body.Radius / homeworld.Radius;
            return 6.0 * homeworld.Radius * relrp;
        }

        public static double SpecialMagneticFieldScaling(this CelestialBody body)
        {
            return MagneticFieldDefinitionsHandler.GetMagneticFieldDefinitionForBody(body.name).StrengthMult;
        }

        public static double GetBeltMagneticFieldMagnitude(this CelestialBody body, CelestialBody homeworld, double altitude, double lat)
        {
            double mlat = lat / 180 * Math.PI + Math.PI / 2;

            double relmp = body.Mass / homeworld.Mass;
            double relrt = body.rotationPeriod / homeworld.rotationPeriod;

            double altituded = altitude + body.Radius;
            double Bmag = VanAllen.B0 / relrt * relmp * Math.Pow((body.Radius / altituded), 3) * Math.Sqrt(1 + 3 * Math.Pow(Math.Cos(mlat), 2)) * body.SpecialMagneticFieldScaling();

            if (KopernicusHelper.IsStar(body))
                Bmag /= 1000;

            if (body.atmosphere)
                Bmag *= body.atmosphereDepth / 70000;
            else
                Bmag *= 0.01;

            return Bmag;
        }

        public static double GetBeltMagneticFieldRadial(this CelestialBody body, CelestialBody homeworld, double altitude, double lat)
        {
            double mlat = lat / 180 * Math.PI + Math.PI / 2;

            double relmp = body.Mass / homeworld.Mass;
            double relrt = body.rotationPeriod / homeworld.rotationPeriod;

            double altituded = altitude + body.Radius;
            double Bmag = -2 / relrt * relmp * VanAllen.B0 * Math.Pow((body.Radius / altituded), 3) * Math.Cos(mlat) * body.SpecialMagneticFieldScaling();

            if (KopernicusHelper.GetLuminocity(body) > 0)
                Bmag /= 1000;

            if (body.atmosphere)
                Bmag *= body.atmosphereDepth / 70000;
            else
                Bmag *= 0.01;

            return Bmag;
        }

        public static double GetBeltMagneticFieldAzimuthal(this CelestialBody body, CelestialBody homeworld, double altitude, double lat)
        {
            double mlat = lat / 180 * Math.PI + Math.PI / 2;

            double relmp = body.Mass / homeworld.Mass;
            double relrt = body.rotationPeriod / homeworld.rotationPeriod;

            double altituded = altitude + body.Radius;
            double Bmag = -relmp * VanAllen.B0 / relrt * Math.Pow((body.Radius / altituded), 3) * Math.Sin(mlat) * body.SpecialMagneticFieldScaling();

            if (KopernicusHelper.IsStar(body))
                Bmag /= 1000;

            if (body.atmosphere)
                Bmag *= body.atmosphereDepth / 70000;
            else
                Bmag *= 0.01;

            return Bmag;
        }
    }
}
