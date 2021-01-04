using System.Collections.Generic;
using System.Linq;
using KIT.Interfaces;

namespace KIT.Extensions
{
    public static class VesselExtensions
    {
        /// <summary>Tests whether two vessels have line of sight to each other</summary>
        /// <returns><c>true</c> if a straight line from a to b is not blocked by any celestial body; 
        /// otherwise, <c>false</c>.</returns>
        public static bool HasLineOfSightWith(this Vessel vesselA, Vessel vesselB, double freeDistance = 2500, double minHeight = 5)
        {
            var vesselAVector = vesselA.transform.position;
            var vesselBVector = vesselB.transform.position;

            if (freeDistance > 0 && Vector3d.Distance(vesselAVector, vesselBVector) < freeDistance)           // if both vessels are within active view
                return true;

            foreach (var referenceBody in FlightGlobals.Bodies)
            {
                var bodyFromA = referenceBody.position - vesselAVector;
                var bFromA = vesselBVector - vesselAVector;

                // Is body at least roughly between satA and satB?
                if (Vector3d.Dot(bodyFromA, bFromA) <= 0) continue;

                Vector3d bFromANorm = bFromA.normalized;

                if (Vector3d.Dot(bodyFromA, bFromANorm) >= bFromA.magnitude) continue;

                // Above conditions guarantee that Vector3d.Dot(bodyFromA, bFromANorm) * bFromANorm 
                // lies between the origin and bFromA
                var lateralOffset = bodyFromA - (Vector3d.Dot(bodyFromA, bFromANorm) * bFromANorm);

                if (lateralOffset.magnitude < referenceBody.Radius - minHeight) return false;
            }
            return true;
        }

        public static bool LineOfSightToSun(this Vessel vessel, CelestialBody star)
        {
            Vector3d vesselPosition = vessel.GetVesselPos();
            Vector3d startPosition = star.position;

            return vesselPosition.LineOfSightToSun(startPosition);
        }

        /*
         * This function should allow this module to work in solar systems other than the vanilla KSP one as well. Credit to Freethinker's MicrowavePowerReceiver code.
         * It checks current reference body's temperature at 0 altitude. If it is less than 2k K, it checks this body's reference body next and so on.
         */
        public static CelestialBody GetLocalStar(this Vessel vessel)
        {
            var depth = 0;
            var star = vessel.mainBody;

            while ((depth < 10) && (star.GetTemperature(0) < 2000))
            {
                star = star.referenceBody;
                depth++;
            }
            if ((star.GetTemperature(0) < 2000) || (star.name == "Galactic Core"))
                star = null;

            return star;
        }

        public static Vector3d GetVesselPos(this Vessel v)
        {
            return (v.state == Vessel.State.ACTIVE)
                ? v.CoMD
                : v.GetWorldPos3D();
        }

        public static List<T> GetVesselAndModuleMass<T>(this Vessel vessel, out double totalMass, out double moduleMass) where T : class
        {
            totalMass = 0;
            moduleMass = 0;

            var moduleList = new List<T>();

            List<Part> parts = (HighLogic.LoadedSceneIsEditor ? EditorLogic.fetch.ship.parts : vessel.parts);
            foreach (var currentPart in parts)
            {
                totalMass += currentPart.mass;
                var module = currentPart.FindModuleImplementing<T>();
                if (module == null) continue;

                moduleMass += currentPart.mass;
                moduleList.Add(module);
            }
            return moduleList;
        }

        public static bool HasAnyModulesImplementing<T>(this Vessel vessel) where T : class
        {
            return vessel.FindPartModulesImplementing<T>().Any();
        }

        public static bool IsInAtmosphere(this Vessel vessel)
        {
            return vessel.altitude <= vessel.mainBody.atmosphereDepth;
        }

        public static double GetTemperatureOfColdestThermalSource(this Vessel vessel)
        {
            var activeReactors = vessel.FindPartModulesImplementing<IPowerSource>().Where(ts => ts.IsActive && ts.IsThermalSource).ToList();
            return activeReactors.Any() ? activeReactors.Min(ts => ts.CoreTemperature) : double.MaxValue;
        }

        public static double GetAverageTemperatureOfThermalSource(this Vessel vessel)
        {
            List<IPowerSource> activeReactors = vessel.FindPartModulesImplementing<IPowerSource>().Where(ts => ts.IsActive && ts.IsThermalSource).ToList();
            return activeReactors.Any() ? activeReactors.Sum(r => r.HotBathTemperature) / activeReactors.Count : 0;
        }

        public static bool HasAnyActiveThermalSources(this Vessel vessel)
        {
            return vessel.FindPartModulesImplementing<IPowerSource>().Any(ts => ts.IsActive);
        }
    }
}
