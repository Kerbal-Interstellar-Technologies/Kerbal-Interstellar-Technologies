using System;
using System.Reflection;
using UnityEngine;

namespace KIT.External
{
    public static class Kerbalism
    {
        public static int VersionMajor;
        public static int VersionMinor;
        public static int VersionMajorRevision;
        public static int VersionMinorRevision;
        public static int VersionBuild;
        public static int VersionRevision;

        static readonly Type Sim;
        static readonly MethodInfo VesselTemperature;

        static Kerbalism()
        {
            Debug.Log("[KSPI]: Looking for Kerbalism Assembly");
            Debug.Log("[KSPI]: AssemblyLoader.loadedAssemblies contains " + AssemblyLoader.loadedAssemblies.Count + " assemblies");
            foreach (AssemblyLoader.LoadedAssembly loadedAssembly in AssemblyLoader.loadedAssemblies)
            {
                if (loadedAssembly.name.StartsWith("Kerbalism") && loadedAssembly.name.EndsWith("Bootstrap") == false)
                {
                    Debug.Log("[KSPI]: Found " + loadedAssembly.name + " Assembly");

                    var kerbalismAssembly = loadedAssembly.assembly;

                    AssemblyName assemblyName = kerbalismAssembly.GetName();

                    VersionMajor = assemblyName.Version.Major;
                    VersionMinor = assemblyName.Version.Minor;
                    VersionMajorRevision = assemblyName.Version.MajorRevision;
                    VersionMinorRevision = assemblyName.Version.MinorRevision;
                    VersionBuild = assemblyName.Version.Build;
                    VersionRevision = assemblyName.Version.Revision;

                    var kerbalismversionstr =
                        $"{VersionMajor}.{VersionMinor}.{VersionRevision}.{VersionBuild}.{VersionMajorRevision}.{VersionMinorRevision}";
                    Debug.Log("[KSPI]: Found Kerbalism assemblyName Version " + kerbalismversionstr);

                    try { Sim = kerbalismAssembly.GetType("KERBALISM.Sim"); } catch (Exception e) { Debug.LogException(e); }

                    if (Sim != null)
                    {
                        Debug.Log("[KSPI]: Found KERBALISM.Sim");
                        try
                        {
                            VesselTemperature = Sim.GetMethod("Temperature");
                            if (VesselTemperature != null)
                                Debug.Log("[KSPI]: Found KERBALISM.Sim.Temperature Method");
                            else
                                Debug.LogError("[KSPI]: Failed to find KERBALISM.Sim.Temperature Method");
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }
                    else
                        Debug.LogError("[KSPI]: Failed to find KERBALISM.Sim");

                    return;
                }
            }
            Debug.Log("[KSPI]: KERBALISM was not found");
        }

        public static bool IsLoaded => VersionMajor > 0;

        public static bool HasRadiationFixes => VersionMajor >= 3 && VersionMinor >= 1;

        // return proportion of ionizing radiation not blocked by atmosphere
        public static double GammaTransparency(CelestialBody body, double altitude)
        {
            // deal with underwater & fp precision issues
            altitude = Math.Abs(altitude);

            // get pressure
            double staticPressure = body.GetPressure(altitude);
            if (staticPressure > 0.0)
            {
                // get density
                double density = body.GetDensity(staticPressure, body.GetTemperature(altitude));

                // math, you know
                double Ra = body.Radius + altitude;
                double Ya = body.atmosphereDepth - altitude;
                double path = Math.Sqrt(Ra * Ra + 2.0 * Ra * Ya + Ya * Ya) - Ra;
                double factor = body.GetSolarPowerFactor(density) * Ya / path;

                // poor man atmosphere composition contribution
                if (body.atmosphereContainsOxygen || body.ocean)
                {
                    factor = 1.0 - Math.Pow(1.0 - factor, 0.015);
                }
                return factor;
            }
            return 1.0;
        }
    }
}
