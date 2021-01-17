using KIT.Extensions;
using System.Linq;
using UnityEngine;

namespace KIT.Propulsion
{
    class ModuleSolarSail : PartModule
    {
        // Persistent True
        [KSPField(isPersistant = true)]
        public bool IsEnabled;

        // Persistent False
        [KSPField]
        public float reflectedPhotonRatio = 1;
        [KSPField]
        public float surfaceArea = 1; // Surface area of the panel.
        [KSPField]
        public string animName = "";

        // GUI
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_ModuleSolarSail_ForceAcquired")]//Force
        protected string forceAcquired = "";
        [KSPField(guiActive = true, guiName = "#LOC_KSPIE_ModuleSolarSail_SolarAcceleration")]//Acceleration
        protected string solarAcc = "";

        protected Transform surfaceTransform;
        protected Animation solarSailAnim;

        private const double ThrustCoefficient = 9.08e-6;

        protected double solar_force_d;
        protected double solar_acc_d;
        protected long count;

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_ModuleSolarSail_DeploySail", active = true)]//Deploy Sail
        public void DeploySail() {
            if (animName != null && solarSailAnim != null) {
                solarSailAnim[animName].speed = 1;
                solarSailAnim[animName].normalizedTime = 0;
                solarSailAnim.Blend(animName, 2);
            }
            IsEnabled = true;
        }

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_ModuleSolarSail_RetractSail", active = false)]//Retract Sail
        public void RetractSail() {
            if (animName != null && solarSailAnim != null) {
                solarSailAnim[animName].speed = -1;
                solarSailAnim[animName].normalizedTime = 1;
                solarSailAnim.Blend(animName, 2);
            }
            IsEnabled = false;
        }

        public override void OnStart(StartState state)
        {
            if (state != StartState.None && state != StartState.Editor)
            {
                //surfaceTransform = part.FindModelTransform(surfaceTransformName);
                //solarSailAnim = (ModuleAnimateGeneric)part.Modules["ModuleAnimateGeneric"];
                if (animName != null)
                {
                    solarSailAnim = part.FindModelAnimators(animName).FirstOrDefault();
                }
                if (IsEnabled && solarSailAnim != null)
                {
                    solarSailAnim[animName].speed = 1;
                    solarSailAnim[animName].normalizedTime = 0;
                    solarSailAnim.Blend(animName, 0.1f);
                }

                Debug.Log("[KSPI]: ModuleSolarSail on " + part.name + " was Force Activated");
                part.force_activate();
            }
        }

        public override void OnUpdate()
        {
            Events["DeploySail"].active = !IsEnabled;
            Events["RetractSail"].active = IsEnabled;
            Fields["solarAcc"].guiActive = IsEnabled;
            Fields["forceAcquired"].guiActive = IsEnabled;
            forceAcquired = solar_force_d.ToString("E") + " N";
            solarAcc = solar_acc_d.ToString("E") + " m/s";
        }

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

        public override void OnFixedUpdate()
        {
            if (FlightGlobals.fetch != null)
            {
                solar_force_d = 0;
                if (!IsEnabled) return;

                double sunlightFactor = 1;
                // Vector3 sunVector = FlightGlobals.fetch.bodies[0].position - part.orgPos;

                if (!vessel.LineOfSightToSun(LocalStar))
                {
                    sunlightFactor = 0.0;
                }

                Vector3d solarForce = CalculateSolarForce() * sunlightFactor;
                Vector3d solarAcceleration = solarForce / vessel.totalMass / 1000.0 * TimeWarp.fixedDeltaTime;
                if (!vessel.packed)
                {
                    vessel.ChangeWorldVelocity(solarAcceleration);
                }
                else
                {
                    if (sunlightFactor > 0)
                    {
                        double temp1 = solarAcceleration.y;
                        solarAcceleration.y = solarAcceleration.z;
                        solarAcceleration.z = temp1;
                        Vector3d position = vessel.orbit.getRelativePositionAtUT(Planetarium.GetUniversalTime());
                        Orbit orbit2 = new Orbit(vessel.orbit.inclination, vessel.orbit.eccentricity, vessel.orbit.semiMajorAxis, vessel.orbit.LAN, vessel.orbit.argumentOfPeriapsis, vessel.orbit.meanAnomalyAtEpoch, vessel.orbit.epoch, vessel.orbit.referenceBody);
                        orbit2.UpdateFromStateVectors(position, vessel.orbit.vel + solarAcceleration, vessel.orbit.referenceBody, Planetarium.GetUniversalTime());
                        //print(orbit2.timeToAp);
                        if (!double.IsNaN(orbit2.inclination) && !double.IsNaN(orbit2.eccentricity) && !double.IsNaN(orbit2.semiMajorAxis) && orbit2.timeToAp > TimeWarp.fixedDeltaTime)
                        {
                            vessel.orbit.inclination = orbit2.inclination;
                            vessel.orbit.eccentricity = orbit2.eccentricity;
                            vessel.orbit.semiMajorAxis = orbit2.semiMajorAxis;
                            vessel.orbit.LAN = orbit2.LAN;
                            vessel.orbit.argumentOfPeriapsis = orbit2.argumentOfPeriapsis;
                            vessel.orbit.meanAnomalyAtEpoch = orbit2.meanAnomalyAtEpoch;
                            vessel.orbit.epoch = orbit2.epoch;
                            vessel.orbit.referenceBody = orbit2.referenceBody;
                            vessel.orbit.Init();

                            //vessel.orbit.UpdateFromOrbitAtUT(orbit2, Planetarium.GetUniversalTime(), orbit2.referenceBody);
                            vessel.orbit.UpdateFromUT(Planetarium.GetUniversalTime());
                        }

                    }
                }
                solar_force_d = solarForce.magnitude;
                solar_acc_d = solarAcceleration.magnitude / TimeWarp.fixedDeltaTime;
                //print(solarForce.x.ToString() + ", " + solarForce.y.ToString() + ", " + solarForce.z.ToString());
            }
            count++;
        }

        // New Solar Force Function
        private Vector3d CalculateSolarForce()
        {
            if (part != null)
            {
                Vector3d sunPosition = FlightGlobals.fetch.bodies[0].position;
                Vector3d ownPosition = part.transform.position;
                Vector3d ownSunPosition = ownPosition - sunPosition;
                Vector3d normal = part.transform.up;
                if (surfaceTransform != null)
                    normal = surfaceTransform.forward;
                // If normal points away from sun, negate so our force is always away from the sun
                // so that turning the backside towards the sun thrusts correctly
                if (Vector3d.Dot(normal, ownSunPosition) < 0)
                    normal = -normal;
                // Magnitude of force proportional to cosine-squared of angle between sun-line and normal
                double cosConeAngle = Vector3.Dot(ownSunPosition.normalized, normal);
                return normal * cosConeAngle * cosConeAngle * surfaceArea * reflectedPhotonRatio * SolarForceAtDistance();
            }

            return Vector3d.zero;
        }

        private double SolarForceAtDistance()
        {
            double distanceFromSun = Vector3.Distance(LocalStar.position, vessel.CoMD);
            return ThrustCoefficient * GameConstants.KerbinSunDistance * GameConstants.KerbinSunDistance / distanceFromSun / distanceFromSun;
        }

    }
}

