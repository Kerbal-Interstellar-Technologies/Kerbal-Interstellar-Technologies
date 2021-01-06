using KIT.Resources;
using KIT.ResourceScheduler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KIT.Extensions;
using UnityEngine;

namespace KIT.BeamedPower
{
    public class KITVesselTracker : PartModule, IKITModule
    {
        internal const string GROUP = "KITVesselTracker";
        internal const string GROUP_TITLE = "#LOC_KIT_VesselTracker_GroupName";

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KIT_VesselTracker_VesselName")]
        public string vesselName;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = true, guiActiveEditor = true), UI_Toggle(disabledText = "#LOC_KIT_VesselTracker_Disabled", enabledText = "#LOC_KIT_VesselTracker_Enabled")]
        public bool trackerActive;

        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = true, guiName = "#LOC_KIT_VesselTracker_StatusString")]
        public string statusString;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true)]
        public string pivotAnimationName;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true)]
        public string rotationAnimationName;


        private IBeamedPowerReceiver _beamedPowerReceiver;
        private ModuleAnimateGeneric _rotationAnimation;
        private ModuleAnimateGeneric _pivotAnimation;

        private Transform _pivotTransform;
        private Transform _rotationTransform;

        private bool _configured;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            _configured = false;

            _beamedPowerReceiver = part.FindModuleImplementing<IBeamedPowerReceiver>();
            if (_beamedPowerReceiver == null)
            {
                Debug.Log("[KITVesselTracker] no IBeamedPowerReceiver");
                return;
            }

            if (!_knowsHowToHandle.Contains(_beamedPowerReceiver.ReceiverType))
            {
                Debug.Log("[KITVesselTracker] do not know how to handle a ReceiverType of {_beamedPowerReceiver.ReceiverType}");
                return;
            }

            _rotationAnimation = part.FindModulesImplementing<ModuleAnimateGeneric>().FirstOrDefault(mag => mag.animationName == rotationAnimationName);
            _pivotAnimation = part.FindModulesImplementing<ModuleAnimateGeneric>().FirstOrDefault(mag => mag.animationName == pivotAnimationName);

            if (_rotationAnimation == null || _pivotAnimation == null)
            {
                Debug.Log($"[KITVesselTracker] could not find an animation for {(_rotationAnimation == null ? "rotation" : "")} {(_pivotAnimation == null ? "pivot" : "")}");
                return;
            }

            _rotationTransform = part.FindModelTransform(rotationAnimationName);
            _pivotTransform = part.FindModelTransform(pivotAnimationName);

            if (_rotationTransform == null || _pivotTransform == null)
            {
                Debug.Log("[KITVesselTracker] unable to find part transforms with animation names");
                _rotationTransform = part.FindModelTransform("Model00_Base");
                _pivotTransform = part.FindModelTransform("Model01_Platform");
                Debug.Log("[KITVesselTracker] and now we've got {_rotationTransform} and {_pivotTransform}");
                if (_rotationTransform == null || _pivotTransform == null) return;
            }

            _configured = true;
            Debug.Log("[KITVesselTracker] ready to track vessels");

            trackerActive = false;
        }

        private bool _shownVesselNameWarning;
        private readonly int[] _knowsHowToHandle = new[] { 2 };

        public override void OnUpdate()
        {
            base.OnUpdate();

            if (HighLogic.LoadedSceneIsEditor || string.IsNullOrEmpty(vesselName) || !_configured || !trackerActive) return;

            var vesselToTrack = FlightGlobals.Vessels.Find(t => t.vesselName == vesselName);
            if (vesselToTrack == null)
            {
                if (_shownVesselNameWarning) return;
                _shownVesselNameWarning = true;

                // TODO - track vessel name changes
                Debug.Log($"[KITVesselTracker] could not find {vesselName}");
                return;
            }

            //Vector3d test = vesselToTrack.GetVesselPos().normalized - vesselToTrack.GetVesselPos().normalized;

            //_rotationTransform.localRotation = Quaternion.RotateTowards(_rotationTransform.localRotation, Quaternion.Euler(0,(float)(test.y), 0 ), 10);
            //_pivotTransform.localRotation = Quaternion.RotateTowards(_pivotTransform.localRotation, Quaternion.Euler((float)test.x, 0, 0), 10);


            //if (!vessel.HasLineOfSightWith(vesselToTrack))
            //{
            // Debug.Log("[KITVesselTracker] we do not have line of sight to the vessel we wish to track");
            return;
            //}



            //var vesselToTrackPosition = vesselToTrack.GetVesselPos();

            //Debug.DrawLine(vessel.CoMD.normalized, vesselToTrackPosition.normalized, Color.blue, 4);

            //var facingDirection = CalculateDirectionToFace(this.part.transform, vesselToTrack.GetVesselPos());
        }

        private int _rateLimit;

        private double CalculateDirectionToFace(Transform thisVesselAntenna, Vector3d targetVesselLocation)
        {
            // var vesselPosition = vessel.GetVesselPos();
            // Vector3d directionVector = part.transform.up.normalized - targetVesselLocation.normalized;

            //var angleToTargetVessel = Vector3d.Angle(part.transform.up, directionVector);

            //Debug.DrawRay(this.part.transform.up.normalized, directionVector, Color.green, 2);

            //Vector3d outwardsFacing = transform.TransformDirection(part.transform.up);
            //Vector3d toOtherVessel = targetVesselLocation.normalized - part.transform.up.normalized;

            //var dotFacing = Vector3d.Dot(outwardsFacing, toOtherVessel);

            // var rotatingUp = Vector3d.RotateTowards(part.transform.up, directionVector, (1 * Time.deltaTime));
            // var rotatingSide = Vector3d.RotateTowards(part.transform.right, directionVector, (1 * Time.deltaTime));

            //if (_rateLimit++ % 100 == 0)
            //{
            //Debug.Log($"[CalculateDirectionToFace] directionVector is {directionVector}, and angleToTargetVessel is {angleToTargetVessel}, dotFacing is {dotFacing}, abs is {Math.Abs(dotFacing)}, factor is {1 - Math.Abs(dotFacing)}");
            //Debug.Log($"[CalculateDirectionToFace] rotatingUp is {rotatingUp} and rotatingSide is {rotatingSide}");
            //}
            /*
                        if (dotFacing < 0)
                        {
                            Debug.Log("[CalculateDirectionToFace] target is behind me");
                            return 0;
                        }

                        if(! _rotationAnimation.IsMoving()) _rotationAnimation.SetScalar((float)(angleToTargetVessel / 180));
                        if(! _pivotAnimation.IsMoving()) _pivotAnimation.SetScalar((float)(1 - dotFacing));
            */



            return 0;
        }

        public bool ModuleConfiguration(out int priority, out bool supplierOnly, out bool hasLocalResources)
        {
            priority = 5;
            supplierOnly = hasLocalResources = false;

            return true;
        }

        public void KITFixedUpdate(IResourceManager resMan)
        {
            // deliberately empty
        }

        public string KITPartName() => part.partInfo.title;

    }
