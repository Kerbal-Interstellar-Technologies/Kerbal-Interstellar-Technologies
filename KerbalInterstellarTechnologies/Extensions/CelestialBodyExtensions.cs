using System;
using System.Collections.Generic;
using KIT.Resources;

namespace KIT.Extensions
{
    public static class CelestialBodyExtensions
    {
        static Dictionary<string, BeltData> BeltDataCache = new Dictionary<string, BeltData>();

        class BeltData
        {
            public double Density;
            public double Ampere;
        }

        const double sqrt2 = 1.4142135624;
        const double sqrt2divPi = 0.79788456080;

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
                    Ampere = 1.5 * homeworld.Radius * relrp / sqrt2,
                };

                BeltDataCache.Add(body.name, beltData);
            }

            double beltParticles = beltData.Density
                * sqrt2divPi
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
            double peakBelt = body.GetPeakProtonBeltAltitude(homeworld, altitude, lat);
            double altituded = altitude;
            double a = peakBelt / sqrt2;
            double beltParticles = sqrt2divPi * Math.Pow(altituded, 2) * Math.Exp(-Math.Pow(altituded, 2) / (2 * Math.Pow(a, 2))) / (Math.Pow(a, 3));
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

        public static double GetPeakProtonBeltAltitude(this CelestialBody body, CelestialBody homeworld, double altitude, double lat)
        {
            lat = lat / 180 * Math.PI;
            double relrp = body.Radius / homeworld.Radius;
            double peakBelt = 1.5 * homeworld.Radius * relrp;
            return peakBelt;
        }

        public static double GetElectronRadiationLevel(this CelestialBody body, CelestialBody homeworld, double altitude, double lat)
        {
            lat = lat / 180 * Math.PI;
            double atmosphere = FlightGlobals.getStaticPressure(altitude, body) / GameConstants.EarthAtmospherePressureAtSeaLevel;
            double atmosphereHeight = PluginHelper.GetMaxAtmosphericAltitude(body);
            double atmosphereScaling = Math.Exp(-atmosphere);

            double relrp = body.Radius / homeworld.Radius;
            double relrt = body.rotationPeriod / homeworld.rotationPeriod;

            double peakBelt2 = body.GetPeakElectronBeltAltitude(homeworld, altitude, lat);
            double altituded = altitude;
            double b = peakBelt2 / sqrt2;
            double beltParticles = 0.9 * sqrt2divPi * Math.Pow(altituded, 2) * Math.Exp(-Math.Pow(altituded, 2) / (2 * Math.Pow(b, 2))) / (Math.Pow(b, 3));
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

        public static double GetPeakElectronBeltAltitude(this CelestialBody body, CelestialBody homeworld, double altitude, double lat)
        {
            lat = lat / 180 * Math.PI;
            double relrp = body.Radius / homeworld.Radius;
            double peakBelt = 6.0 * homeworld.Radius * relrp;
            return peakBelt;
        }

        public static double SpecialMagneticFieldScaling(this CelestialBody body)
        {
            return MagneticFieldDefinitionsHandler.GetMagneticFieldDefinitionForBody(body.name).StrengthMult;
        }

        public static double GetBeltMagneticFieldMagnitude(this CelestialBody body, CelestialBody homeworld, double altitude, double lat)
        {
            double mlat = lat / 180 * Math.PI + Math.PI / 2;

            double relmp = body.Mass / homeworld.Mass;
            double relrp = body.Radius / homeworld.Radius;
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
            double relrp = body.Radius / homeworld.Radius;
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
            double relrp = body.Radius / homeworld.Radius;
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
