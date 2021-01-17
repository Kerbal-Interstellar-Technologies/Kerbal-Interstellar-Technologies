using System;
using System.Collections.Generic;
using System.Linq;
using KIT.Extensions;
using KIT.Resources;
using KIT.ResourceScheduler;
using KIT.Wasteheat;
using KSP.Localization;
using UnityEngine;

namespace KIT.BeamedPower
{
    class PhasedArrayTransmitter : BeamedPowerTransmitter { }

    class MicrowavePowerTransmitter : BeamedPowerTransmitter { }

    class BeamedPowerLaserTransmitter : BeamedPowerTransmitter { }

    class BeamedPowerTransmitter : PartModule, IKITModule, IMicrowavePowerTransmitter, IScalarModule
    {
        public const string Group = "BeamedPowerTransmitter";
        public const string GroupTitle = "#LOC_KSPIE_MicrowavePowerTransmitter_groupName";

        //Persistent
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_TransmitPower"), UI_FloatRange(stepIncrement = 0.5f, maxValue = 100, minValue = 0)]//Transmission Strength
        public float transmitPower = 100;
        [KSPField(isPersistant = true)]
        public string partId;
        [KSPField(isPersistant = true)]
        public bool IsEnabled;
        [KSPField(isPersistant = true)]
        public bool relay;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_CanRelay")]//Can Relay
        public bool canRelay;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_IsMirror")]//Is Mirror
        public bool isMirror;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_Canmergebeams")]//Can merge beams
        public bool isBeamMerger;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_MergingBeams")]//Merging Beams
        public bool mergingBeams;
        [KSPField(isPersistant = true)]
        public double nuclear_power;
        [KSPField(isPersistant = true)]
        public double solar_power;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_PowerCapacity", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2")]//Power Capacity
        public double powerCapacity;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_TransmitWaveLengthm", guiFormat = "F8", guiUnits = " m")]//Transmit WaveLength m
        public double wavelength;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_TransmitWaveLengthSI")]//Transmit WaveLength SI
        public string wavelengthText;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_TransmitWaveLengthWLName")]//Transmit WaveLength WL Name
        public string wavelengthName;
        [KSPField(isPersistant = true)]
        public double atmosphericAbsorption = 0.1;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_MinRelayWaveLength", guiFormat = "F8", guiUnits = " m")]//Min Relay WaveLength
        public double minimumRelayWaveLength;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_MaxRelayWaveLength", guiFormat = "F8", guiUnits = " m")]//Max Relay WaveLength
        public double maximumRelayWaveLength;
        [KSPField(isPersistant = true)]
        public double aperture = 1;
        [KSPField(isPersistant = true)]
        public double diameter;
        [KSPField(isPersistant = true)]
        public bool forceActivateAtStartup;
        [KSPField(isPersistant = true)]
        public bool hasLinkedReceivers;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_NativeWavelength", guiFormat = "F8", guiUnits = " m")]
        public double nativeWaveLength = 0.003189281;
        [KSPField(isPersistant = true, guiActiveEditor = false)]
        public double nativeAtmosphericAbsorptionPercentage = 10;

        //Non Persistent
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_AtmosphericAbsorptionPercentage", guiFormat = "F2", guiUnits = "%")]//Air Absorption Percentage
        public double atmosphericAbsorptionPercentage;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_WaterAbsorptionPercentage", guiFormat = "F2", guiUnits = "%")]//Water Absorption Percentage
        public double waterAbsorptionPercentage;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActiveEditor = false, guiActive = true, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_TotalAbsorptionPercentage", guiFormat = "F2", guiUnits = "%")]//Absorption Percentage
        public double totalAbsorptionPercentage;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_Bodyname")]//Body
        public string body_name;
        public string biome_desc;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_MoistureModifier", guiFormat = "F3")]//Moisture Modifier
        public double moistureModifier;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActiveEditor = false, guiActive = false)]
        public bool canFunctionOnSurface = true;

        [KSPField]
        public bool canPivot = true;        // determines if effective aperture is affected on surface
        [KSPField]
        public double maximumPower = 10000;
        [KSPField]
        public float atmosphereToleranceModifier = 1;
        [KSPField]
        public string animName = "";
        [KSPField]
        public bool canBeActive;
        [KSPField]
        protected int nearbyPartsCount;

        //GUI
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActiveEditor = true, guiActive = false, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_CanTransmit")]//Can Transmit
        public bool canTransmit;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActiveEditor = true, guiActive = false, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_BuildinRelay")]//Build in Relay
        public bool buildInRelay;
        [KSPField(groupName = Group)]
        public int compatibleBeamTypes = 1;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActiveEditor = true, guiActive = false, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_ApertureDiameter", guiFormat = "F2", guiUnits = " m")]//Aperture Diameter
        public double apertureDiameter;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = true, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_Status")]//Status
        public string statusStr;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_TransmissionEfficiency", guiFormat = "F1", guiUnits = "%")]//Transmission Efficiency
        public double transmissionEfficiencyPercentage;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = true, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_BeamedPower")]//Wall to Beam Power
        public string beamedpower;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = true, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_AvailablePower", guiFormat = "F2", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", advancedTweakable = true)]//Available Power
        public double availablePower;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, guiActive = true, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_RequestedPower", guiFormat = "F2", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", advancedTweakable = true)]//Requested Power
        public double requestedPower;

        [KSPField(isPersistant = true)] public double ActiveBeamGeneratorEfficiency;
        
        // Near Future Compatibility properties
        [KSPField]
        public double powerMult = 1;
        [KSPField]
        public double powerHeatMultiplier = 1;

        protected string scalarModuleID = Guid.NewGuid().ToString();
        protected EventData<float, float> onMoving;
        protected EventData<float> onStop;

        //Internal
        public Animation anim;
        public List<ISolarPower> SolarCells;
        public BeamedPowerReceiver PartReceiver;
        public List<BeamedPowerReceiver> VesselReceivers;
        public BeamGenerator ActiveBeamGenerator;
        public List<BeamGenerator> BeamGenerators;
        public ModuleAnimateGeneric GenericAnimation;

        private BaseEvent _activateTransmitterEvent;
        private BaseEvent _deactivateTransmitterEvent;
        private BaseEvent _activateRelayEvent;
        private BaseEvent _deactivateRelayEvent;

        private BaseField _apertureDiameterField;
        private BaseField _beamedPowerField;
        private BaseField _transmitPowerField;
        private BaseField _totalAbsorptionPercentageField;
        private BaseField _wavelengthField;
        private BaseField _wavelengthNameField;

        public bool CanMove => true;

        public float GetScalar => 1;

        public EventData<float, float> OnMoving => onMoving;

        public EventData<float> OnStop => onStop;

        public string ScalarModuleID => scalarModuleID;

        public bool IsMoving()
        {
            return anim != null && anim.isPlaying;
        }

        public void SetUIRead(bool state)
        {
            // ignore
        }
        public void SetUIWrite(bool state)
        {
            // ignore
        }

        [KSPEvent(groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_ActivateTransmitter", active = false)]//Activate Transmitter
        public void ActivateTransmitter()
        {
            if (relay) return;


            Debug.Log("[KSPI]: BeamedPowerTransmitter on " + part.name + " was Force Activated");
            part.force_activate();
            forceActivateAtStartup = true;

            if (GenericAnimation != null && GenericAnimation.GetScalar < 1)
            {
                GenericAnimation.Toggle();
            }

            IsEnabled = true;

            // update wavelength
            wavelength = Wavelength;
            minimumRelayWaveLength = wavelength * 0.99;
            maximumRelayWaveLength = wavelength * 1.01;

            wavelengthText = WavelengthToText(wavelength);
            wavelengthName = WavelengthName;
            atmosphericAbsorption = CombinedAtmosphericAbsorption;
        }

        private string WavelengthToText(double waveLength)
        {
            if (waveLength > 1.0e-3)
                return (waveLength * 1.0e+3) + " mm";
            else if (waveLength > 7.5e-7)
                return (waveLength * 1.0e+6) + " µm";
            else if (waveLength > 1.0e-9)
                return (waveLength * 1.0e+9) + " nm";
            else
                return (waveLength * 1.0e+12) + " pm";
        }

        [KSPEvent(groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_DeactivateTransmitter", active = false)]//Deactivate Transmitter
        public void DeactivateTransmitter()
        {
            if (relay) return;

            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_MicrowavePowerTransmitter_DeactivateTransmitter_Msg"), 4.0f, ScreenMessageStyle.UPPER_CENTER);//"Transmitter deactivated"

            if (GenericAnimation != null && GenericAnimation.GetScalar > 0)
            {
                GenericAnimation.Toggle();
            }

            IsEnabled = false;
        }

        [KSPEvent(groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_ActivateRelay", active = false)]//Activate Relay
        public void ActivateRelay()
        {
            if (IsEnabled || relay) return;

            if (GenericAnimation != null && GenericAnimation.GetScalar < 1)
            {
                GenericAnimation.Toggle();
            }

            VesselReceivers = vessel.FindPartModulesImplementing<BeamedPowerReceiver>().Where(m => m.part != part).ToList();

            UpdateRelayWavelength();

            relay = true;
        }

        [KSPEvent(groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_DeactivateRelay", active = false)]//Deactivate Relay
        public void DeactivateRelay()
        {
            if (!relay) return;

            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_MicrowavePowerTransmitter_DeactivateRelay_Msg"), 4, ScreenMessageStyle.UPPER_CENTER);//"Relay deactivated"

            if (GenericAnimation != null && GenericAnimation.GetScalar > 0)
            {
                GenericAnimation.Toggle();
            }

            relay = false;
        }

        private void UpdateRelayWavelength()
        {
            // update stored variables
            wavelength = Wavelength;
            wavelengthText = WavelengthToText(wavelength);
            wavelengthName = WavelengthName;
            atmosphericAbsorption = CombinedAtmosphericAbsorption;

            if (isMirror)
            {
                hasLinkedReceivers = true;
                return;
            }

            // collected all receivers relevant for relay
            var receiversConfiguredForRelay = VesselReceivers.Where(m => m.linkedForRelay).ToList();

            // add build in relay if it can be used for relay
            if (PartReceiver != null && buildInRelay)
                receiversConfiguredForRelay.Add(PartReceiver);

            // determine if we can activate relay
            hasLinkedReceivers = receiversConfiguredForRelay.Count > 0;

            // use all available receivers
            if (hasLinkedReceivers)
            {
                minimumRelayWaveLength = receiversConfiguredForRelay.Min(m => m.minimumWavelength);
                maximumRelayWaveLength = receiversConfiguredForRelay.Max(m => m.maximumWavelength);

                diameter = receiversConfiguredForRelay.Max(m => m.diameter);
            }
        }

        [KSPAction("Activate Transmitter")]
        public void ActivateTransmitterAction(KSPActionParam param)
        {
            ActivateTransmitter();
        }

        [KSPAction("Deactivate Transmitter")]
        public void DeactivateTransmitterAction(KSPActionParam param)
        {
            DeactivateTransmitter();
        }

        [KSPAction("Activate Relay")]
        public void ActivateRelayAction(KSPActionParam param)
        {
            ActivateRelay();
        }

        [KSPAction("Deactivate Relay")]
        public void DeactivateRelayAction(KSPActionParam param)
        {
            DeactivateRelay();
        }

        public override void OnStart(StartState state)
        {
            onMoving = new EventData<float, float>("transmitterMoving");
            onStop = new EventData<float>("transmitterStop");

            powerCapacity = maximumPower * powerMult;

            if (String.IsNullOrEmpty(partId))
                partId = Guid.NewGuid().ToString();

            // store  aperture and diameter
            aperture = apertureDiameter;
            diameter = apertureDiameter;

            PartReceiver = part.FindModulesImplementing<BeamedPowerReceiver>().FirstOrDefault();
            GenericAnimation = part.FindModulesImplementing<ModuleAnimateGeneric>().FirstOrDefault(m => m.animationName == animName);

            ConnectToBeamGenerator();

            _activateRelayEvent = Events[nameof(ActivateRelay)];
            _deactivateRelayEvent = Events[nameof(DeactivateRelay)];
            _activateTransmitterEvent = Events[nameof(ActivateTransmitter)];
            _deactivateTransmitterEvent = Events[nameof(DeactivateTransmitter)];

            _wavelengthField = Fields[nameof(wavelengthText)];
            _beamedPowerField = Fields[nameof(beamedpower)];
            _transmitPowerField = Fields[nameof(transmitPower)];
            _wavelengthNameField = Fields[nameof(wavelengthName)];
            _apertureDiameterField = Fields[nameof(apertureDiameter)];
            _totalAbsorptionPercentageField = Fields[nameof(totalAbsorptionPercentage)];

            if (state == StartState.Editor)
            {
                part.OnEditorAttach += OnEditorAttach;
                return;
            }

            SolarCells = vessel.FindPartModulesImplementing<ISolarPower>();
            VesselReceivers = vessel.FindPartModulesImplementing<BeamedPowerReceiver>().Where(m => m.part != part).ToList();

            UpdateRelayWavelength();

            if (forceActivateAtStartup)
            {
                Debug.Log("[KSPI]: BeamedPowerTransmitter on " + part.name + " was Force Activated");
                part.force_activate();
            }
        }

        /// <summary>
        /// Event handler which is called when part is attached to a new part
        /// </summary>
        private void OnEditorAttach()
        {
            ConnectToBeamGenerator();
        }

        private void ConnectToBeamGenerator()
        {
            // connect with beam generators
            BeamGenerators = part.FindModulesImplementing<BeamGenerator>().Where(m => (m.beamType & compatibleBeamTypes) == m.beamType).ToList();

            if (BeamGenerators.Count == 0 && part.parent != null)
            {
                BeamGenerators.AddRange(part.parent.FindModulesImplementing<BeamGenerator>().Where(m => (m.beamType & compatibleBeamTypes) == m.beamType));
            }

            if (BeamGenerators.Count == 0)
            {
                var attachedParts = part.attachNodes.Where(m => m.attachedPart != null).Select(m => m.attachedPart).ToList();

                var parentParts = attachedParts.Where(m => m.parent != null && m.parent != part).Select(m => m.parent).ToList();
                var indirectParts = attachedParts.SelectMany(m => m.attachNodes.Where(l => l.attachedPart != null && l.attachedPart != part).Select(l => l.attachedPart)).ToList();

                attachedParts.AddRange(indirectParts);
                attachedParts.AddRange(parentParts);

                var nearbyParts = attachedParts.Distinct().ToList();
                nearbyPartsCount = nearbyParts.Count;

                var nearbyGenerators = nearbyParts.Select(m => m.FindModuleImplementing<BeamGenerator>()).Where(l => l != null);
                var availableGenerators = nearbyGenerators.SelectMany(m => m.FindBeamGenerators(m.part)).Where(m => (m.beamType & compatibleBeamTypes) == m.beamType).Distinct();

                BeamGenerators.AddRange(availableGenerators);
            }

            ActiveBeamGenerator = BeamGenerators.FirstOrDefault();

            if (ActiveBeamGenerator != null)
            {
                ActiveBeamGenerator.Connect(this);

                if (ActiveBeamGenerator.part != part)
                    ActiveBeamGenerator.UpdateMass(maximumPower);
            }
        }

        public bool CanBeActive
        {
            get
            {
                if (anim == null)
                    return true;

                var pressure = part.atmDensity;
                var dynamicPressure = 0.5 * pressure * 1.2041 * vessel.srf_velocity.sqrMagnitude / 101325.0;

                if (dynamicPressure <= 0) return true;

                var pressureLoad = (dynamicPressure / 1.4854428818159e-3) * 100;
                if (pressureLoad > 100 * atmosphereToleranceModifier)
                    return false;
                else
                    return true;
            }
        }

        public void Update()
        {
            if (!HighLogic.LoadedSceneIsFlight)
            {
                powerCapacity = maximumPower * powerMult;
                return;
            }

            UpdateRelayWavelength();

            totalAbsorptionPercentage = atmosphericAbsorptionPercentage + waterAbsorptionPercentage;
            atmosphericAbsorption = totalAbsorptionPercentage / 100;

            var vesselInSpace = (vessel.situation == Vessel.Situations.ORBITING || vessel.situation == Vessel.Situations.ESCAPING || vessel.situation == Vessel.Situations.SUB_ORBITAL);
            var receiverOn = PartReceiver != null && PartReceiver.isActive();
            canBeActive = CanBeActive;

            if (anim != null && !canBeActive && IsEnabled && part.vessel.isActiveVessel && !CheatOptions.UnbreakableJoints)
            {
                if (relay)
                {
                    var message = Localizer.Format("#LOC_KSPIE_MicrowavePowerTransmitter_Disabledrelay_Msg");//"Disabled relay because of static pressure atmosphere"
                    ScreenMessages.PostScreenMessage(message, 5f, ScreenMessageStyle.UPPER_CENTER);
                    Debug.Log("KSPI - " + message);
                    DeactivateRelay();
                }
                else
                {
                    var message = Localizer.Format("#LOC_KSPIE_MicrowavePowerTransmitter_Disabledtransmitter_Msg");//"Disabled transmitter because of static pressure atmosphere"
                    ScreenMessages.PostScreenMessage(message, 5f, ScreenMessageStyle.UPPER_CENTER);
                    Debug.Log("KSPI - " + message);
                    DeactivateTransmitter();
                }
            }

            var canOperateInCurrentEnvironment = canFunctionOnSurface || vesselInSpace;
            var vesselCanTransmit = canTransmit && canOperateInCurrentEnvironment;

            _activateTransmitterEvent.active = ActiveBeamGenerator != null && vesselCanTransmit && !IsEnabled && !relay && !receiverOn && canBeActive;
            _deactivateTransmitterEvent.active = IsEnabled;

            canRelay = hasLinkedReceivers && canOperateInCurrentEnvironment;

            _activateRelayEvent.active = canRelay && !IsEnabled && !relay && !receiverOn && canBeActive;
            _deactivateRelayEvent.active = relay;

            mergingBeams = IsEnabled && canRelay && isBeamMerger;

            bool isTransmitting = IsEnabled && !relay;

            _apertureDiameterField.guiActive = isTransmitting;
            _beamedPowerField.guiActive = isTransmitting && canBeActive;
            _transmitPowerField.guiActive = PartReceiver == null || !PartReceiver.isActive();

            bool isLinkedForRelay = PartReceiver != null && PartReceiver.linkedForRelay;
            bool receiverNotInUse = !isLinkedForRelay && !receiverOn && !IsRelay;

            _totalAbsorptionPercentageField.guiActive = receiverNotInUse;
            _wavelengthField.guiActive = receiverNotInUse;
            _wavelengthNameField.guiActive = receiverNotInUse;

            if (IsEnabled)
                statusStr = Localizer.Format("#LOC_KSPIE_MicrowavePowerTransmitter_Statu1");//"Transmitter Active"
            else if (relay)
                statusStr = Localizer.Format("#LOC_KSPIE_MicrowavePowerTransmitter_Statu2");//"Relay Active"
            else
            {
                if (isLinkedForRelay)
                    statusStr = Localizer.Format("#LOC_KSPIE_MicrowavePowerTransmitter_Statu3");//"Is Linked For Relay"
                else if (receiverOn)
                    statusStr = Localizer.Format("#LOC_KSPIE_MicrowavePowerTransmitter_Statu4");//"Receiver active"
                else if (canRelay)
                    statusStr = Localizer.Format("#LOC_KSPIE_MicrowavePowerTransmitter_Statu5");//"Is ready for relay"
                else if (BeamGenerators.Count == 0)
                    statusStr = Localizer.Format("#LOC_KSPIE_MicrowavePowerTransmitter_Statu6");//"No beam generator found"
                else
                    statusStr = Localizer.Format("#LOC_KSPIE_MicrowavePowerTransmitter_Statu7");//"Inactive."
            }

            if (ActiveBeamGenerator == null)
            {
                var wavelengthField = Fields[nameof(wavelength)];
                wavelengthField.guiActive = false;
                wavelengthField.guiActiveEditor = false;

                var atmosphericAbsorptionPercentageField = Fields[nameof(atmosphericAbsorptionPercentage)];
                atmosphericAbsorptionPercentageField.guiActive = false;
                atmosphericAbsorptionPercentageField.guiActiveEditor = false;

                var waterAbsorptionPercentageField = Fields[nameof(waterAbsorptionPercentage)];
                waterAbsorptionPercentageField.guiActive = false;
                waterAbsorptionPercentageField.guiActiveEditor = false;

                return;
            }

            wavelength = ActiveBeamGenerator.wavelength;
            wavelengthText = WavelengthToText(wavelength);
            atmosphericAbsorptionPercentage = ActiveBeamGenerator.atmosphericAbsorptionPercentage;
            waterAbsorptionPercentage = ActiveBeamGenerator.waterAbsorptionPercentage * moistureModifier;

            beamedpower = PluginHelper.GetFormattedPowerString(nuclear_power + solar_power);
            SolarCells = vessel.FindPartModulesImplementing<ISolarPower>();
            ActiveBeamGeneratorEfficiency = ActiveBeamGenerator.efficiencyPercentage;
        }

        public void FixedUpdate()
        {

        }

        private static void CollectBiomeData(Vessel vessel, out double moistureModifier, out string biomeDesc, out string bodyName)
        {
            moistureModifier = 0;
            biomeDesc = bodyName = string.Empty;

            if (vessel == null) return;

            double lat = vessel.latitude * Math.PI / 180d;
            double lon = vessel.longitude * Math.PI / 180d;

            if (vessel.mainBody == null) return;

            bodyName = vessel.mainBody.name;

            if (vessel.mainBody.BiomeMap == null) return;

            var attribute = vessel.mainBody.BiomeMap.GetAtt(lat, lon);

            if (attribute == null) return;

            biomeDesc = attribute.name;

            double cloudVariance;
            if (bodyName == "Kerbin" || bodyName == "Earth")
            {
                if (biomeDesc == "Desert" || biomeDesc == "Ice Caps" || biomeDesc == "BadLands")
                    moistureModifier = 0.4;
                else if (biomeDesc == "Water")
                    moistureModifier = 1;
                else
                    moistureModifier = 0.8;

                cloudVariance = 0.5d + (Planetarium.GetUniversalTime() % 3600 / 7200d);
            }
            else
                cloudVariance = 1;

            double latitudeVariance = (180d - lat) / 180d;

            moistureModifier = 2 * moistureModifier * latitudeVariance * cloudVariance;
        }

        public double PowerCapacity => powerCapacity;

        public double Wavelength => ActiveBeamGenerator != null ? ActiveBeamGenerator.wavelength : nativeWaveLength;

        public string WavelengthName => ActiveBeamGenerator != null ? ActiveBeamGenerator.beamWaveName : "";

        public double CombinedAtmosphericAbsorption =>
            ActiveBeamGenerator != null
                ? (atmosphericAbsorptionPercentage + waterAbsorptionPercentage) / 100d
                : nativeAtmosphericAbsorptionPercentage / 100d;

        public double GetNuclearPower()
        {
            return nuclear_power;
        }

        public double GetSolarPower()
        {
            return solar_power;
        }

        public bool IsRelay => relay;

        private bool isActive()
        {
            return IsEnabled;
        }

        public static IVesselRelayPersistence GetVesselRelayPersistenceForVessel(Vessel vessel)
        {
            // find all active transmitters configured for relay
            var relays = vessel.FindPartModulesImplementing<BeamedPowerTransmitter>().Where(m => m.IsRelay || m.mergingBeams).ToList();
            if (relays.Count == 0)
                return null;

            var relayPersistence = new VesselRelayPersistence(vessel) { IsActive = true };

            // TODO(https://discord.com/channels/586489099632902178/596008513776386070/793441544363180053)
            // if (relayPersistence.IsActive)
            //    return relayPersistence;
            // can probably be deleted

            foreach (var relay in relays)
            {
                var transmitData = relayPersistence.SupportedTransmitWavelengths.FirstOrDefault(m => m.Wavelength == relay.wavelength);
                if (transmitData == null)
                {
                    // Add guid if missing
                    relay.partId = string.IsNullOrEmpty(relay.partId)
                        ? Guid.NewGuid().ToString()
                        : relay.partId;

                    relayPersistence.SupportedTransmitWavelengths.Add(new WaveLengthData()
                    {
                        PartId = new Guid(relay.partId),
                        Count = 1,
                        ApertureSum = relay.aperture,
                        PowerCapacity = relay.powerCapacity,
                        Wavelength = relay.Wavelength,
                        MinWavelength = relay.minimumRelayWaveLength,
                        MaxWavelength = relay.maximumRelayWaveLength,
                        IsMirror = relay.isMirror,
                        AtmosphericAbsorption = relay.CombinedAtmosphericAbsorption
                    });
                }
                else
                {
                    transmitData.Count++;
                    transmitData.ApertureSum += relay.aperture;
                    transmitData.PowerCapacity += relay.powerCapacity;
                }
            }

            relayPersistence.Aperture = relays.Average(m => m.aperture) * Approximate.Sqrt(relays.Count);
            relayPersistence.Diameter = relays.Average(m => m.diameter);
            relayPersistence.PowerCapacity = relays.Sum(m => m.PowerCapacity);
            relayPersistence.MinimumRelayWavelength = relays.Min(m => m.minimumRelayWaveLength);
            relayPersistence.MaximumRelayWavelength = relays.Max(m => m.maximumRelayWaveLength);

            return relayPersistence;
        }

        public static IVesselMicrowavePersistence GetVesselMicrowavePersistenceForVessel(Vessel vessel)
        {
            var transmitters = vessel.FindPartModulesImplementing<BeamedPowerTransmitter>().Where(m => m.IsEnabled).ToList();
            if (transmitters.Count == 0)
                return null;

            var vesselTransmitters = new VesselMicrowavePersistence(vessel) { IsActive = true };

            foreach (var transmitter in transmitters)
            {
                // Add guid if missing
                transmitter.partId = string.IsNullOrEmpty(transmitter.partId)
                    ? Guid.NewGuid().ToString()
                    : transmitter.partId;

                var transmitData = vesselTransmitters.SupportedTransmitWavelengths.FirstOrDefault(m => m.Wavelength == transmitter.wavelength);
                if (transmitData == null)
                {
                    vesselTransmitters.SupportedTransmitWavelengths.Add(new WaveLengthData()
                    {
                        PartId = new Guid(transmitter.partId),
                        Count = 1,
                        ApertureSum = transmitter.aperture,
                        Wavelength = transmitter.Wavelength,
                        MinWavelength = transmitter.Wavelength * 0.99,
                        MaxWavelength = transmitter.Wavelength * 1.01,
                        NuclearPower = transmitter.nuclear_power,
                        SolarPower = transmitter.solar_power,
                        PowerCapacity = transmitter.powerCapacity,
                        IsMirror = transmitter.isMirror,
                        AtmosphericAbsorption = transmitter.CombinedAtmosphericAbsorption
                    });
                }
                else
                {
                    transmitData.Count++;
                    transmitData.ApertureSum += transmitter.aperture;
                    transmitData.NuclearPower += transmitter.nuclear_power;
                    transmitData.SolarPower += transmitter.solar_power;
                    transmitData.PowerCapacity += transmitter.powerCapacity;
                }
            }

            vesselTransmitters.Aperture = transmitters.Average(m => m.aperture) * transmitters.Count.Sqrt();
            vesselTransmitters.NuclearPower = transmitters.Sum(m => m.GetNuclearPower());
            vesselTransmitters.SolarPower = transmitters.Sum(m => m.GetSolarPower());
            vesselTransmitters.PowerCapacity = transmitters.Sum(m => m.PowerCapacity);

            return vesselTransmitters;
        }

        /// <summary>
        /// Collect anything that can act like a transmitter, including relays
        /// </summary>
        /// <param name="vessel"></param>
        /// <returns></returns>
        public static IVesselMicrowavePersistence GetVesselMicrowavePersistenceForProtoVessel(Vessel vessel)
        {
            var transmitter = new VesselMicrowavePersistence(vessel);
            int totalCount = 0;

            double totalAperture = 0.0;
            double totalNuclearPower = 0.0;
            double totalSolarPower = 0.0;
            double totalPowerCapacity = 0.0;

            foreach (var protoPart in vessel.protoVessel.protoPartSnapshots)
            {
                foreach (var protoModule in protoPart.modules)
                {
                    if (protoModule.moduleName != "MicrowavePowerTransmitter" && protoModule.moduleName != "PhasedArrayTransmitter" && protoModule.moduleName != "BeamedPowerLaserTransmitter")
                        continue;

                    // filter on active transmitters
                    bool transmitterIsEnabled = bool.Parse(protoModule.moduleValues.GetValue("IsEnabled"));
                    if (!transmitterIsEnabled)
                        continue;

                    var aperture = double.Parse(protoModule.moduleValues.GetValue("aperture"));
                    var nuclearPower = double.Parse(protoModule.moduleValues.GetValue("nuclear_power"));
                    var solarPower = double.Parse(protoModule.moduleValues.GetValue("solar_power"));
                    var powerCapacity = double.Parse(protoModule.moduleValues.GetValue("powerCapacity"));
                    var wavelength = double.Parse(protoModule.moduleValues.GetValue("wavelength"));

                    totalCount++;
                    totalAperture += aperture;
                    totalNuclearPower += nuclearPower;
                    totalSolarPower += solarPower;
                    totalPowerCapacity += powerCapacity;

                    var transmitData = transmitter.SupportedTransmitWavelengths.FirstOrDefault(m => m.Wavelength == wavelength);
                    if (transmitData == null)
                    {
                        bool isMirror = bool.Parse(protoModule.moduleValues.GetValue("isMirror"));
                        string partId = protoModule.moduleValues.GetValue("partId");

                        transmitter.SupportedTransmitWavelengths.Add(new WaveLengthData()
                        {
                            PartId = new Guid(partId),
                            Count = 1,
                            ApertureSum = aperture,
                            Wavelength = wavelength,
                            MinWavelength = wavelength * 0.99,
                            MaxWavelength = wavelength * 1.01,
                            IsMirror = isMirror,
                            NuclearPower = nuclearPower,
                            SolarPower = solarPower,
                            PowerCapacity = powerCapacity,
                            AtmosphericAbsorption = double.Parse(protoModule.moduleValues.GetValue("atmosphericAbsorption"))
                        });
                    }
                    else
                    {
                        transmitData.Count++;
                        transmitData.ApertureSum += aperture;
                        transmitData.NuclearPower += nuclearPower;
                        transmitData.SolarPower += solarPower;
                        transmitData.PowerCapacity += powerCapacity;
                    }
                }
            }

            transmitter.Aperture = totalAperture;
            transmitter.NuclearPower = totalNuclearPower;
            transmitter.SolarPower = totalSolarPower;
            transmitter.PowerCapacity = totalPowerCapacity;
            transmitter.IsActive = totalCount > 0;

            return transmitter;
        }

        public static IVesselRelayPersistence GetVesselRelayPersistenceForProtoVessel(Vessel vessel)
        {
            var relayVessel = new VesselRelayPersistence(vessel);
            int totalCount = 0;

            double totalDiameter = 0;
            double totalAperture = 0;
            double totalPowerCapacity = 0;
            double minimumRelayWavelength = 1;
            double maximumRelayWavelength = 0;

            foreach (var protoPart in vessel.protoVessel.protoPartSnapshots)
            {
                foreach (var protoModule in protoPart.modules)
                {
                    if (protoModule.moduleName != "MicrowavePowerTransmitter" && protoModule.moduleName != "PhasedArrayTransmitter" && protoModule.moduleName != "BeamedPowerLaserTransmitter")
                        continue;

                    bool inRelayMode = bool.Parse(protoModule.moduleValues.GetValue("relay"));

                    bool isMergingBeams = false;
                    if (protoModule.moduleValues.HasValue("mergingBeams"))
                        isMergingBeams = bool.Parse(protoModule.moduleValues.GetValue("mergingBeams"));

                    // filter on transmitters
                    if (inRelayMode || isMergingBeams)
                    {
                        var wavelength = double.Parse(protoModule.moduleValues.GetValue("wavelength"));
                        var isMirror = bool.Parse(protoModule.moduleValues.GetValue("isMirror"));
                        var aperture = double.Parse(protoModule.moduleValues.GetValue("aperture"));
                        var powerCapacity = double.Parse(protoModule.moduleValues.GetValue("powerCapacity"));

                        var diameter = protoModule.moduleValues.HasValue("diameter") ? double.Parse(protoModule.moduleValues.GetValue("diameter")) : aperture;

                        totalCount++;
                        totalAperture += aperture;
                        totalDiameter += diameter;
                        totalPowerCapacity += powerCapacity;

                        var relayWavelengthMin = double.Parse(protoModule.moduleValues.GetValue("minimumRelayWaveLength"));
                        if (relayWavelengthMin < minimumRelayWavelength)
                            minimumRelayWavelength = relayWavelengthMin;

                        var relayWavelengthMax = double.Parse(protoModule.moduleValues.GetValue("maximumRelayWaveLength"));
                        if (relayWavelengthMax > maximumRelayWavelength)
                            maximumRelayWavelength = relayWavelengthMax;

                        var relayData = relayVessel.SupportedTransmitWavelengths.FirstOrDefault(m => m.Wavelength == wavelength);
                        if (relayData == null)
                        {
                            string partId = protoModule.moduleValues.GetValue("partId");

                            relayVessel.SupportedTransmitWavelengths.Add(new WaveLengthData()
                            {
                                PartId = new Guid(partId),
                                Count = 1,
                                ApertureSum = aperture,
                                PowerCapacity = powerCapacity,
                                Wavelength = wavelength,
                                MinWavelength = relayWavelengthMin,
                                MaxWavelength = relayWavelengthMax,
                                IsMirror = isMirror,
                                AtmosphericAbsorption = double.Parse(protoModule.moduleValues.GetValue("atmosphericAbsorption"))
                            });
                        }
                        else
                        {
                            relayData.Count++;
                            relayData.ApertureSum += aperture;
                            relayData.PowerCapacity += powerCapacity;
                        }
                    }
                }
            }

            relayVessel.Aperture = (totalAperture / totalCount) * Approximate.Sqrt(totalCount);
            relayVessel.Diameter = totalDiameter / totalCount;
            relayVessel.PowerCapacity = totalPowerCapacity;
            relayVessel.IsActive = totalCount > 0;
            relayVessel.MinimumRelayWavelength = minimumRelayWavelength;
            relayVessel.MaximumRelayWavelength = maximumRelayWavelength;

            return relayVessel;
        }

        public override string GetInfo()
        {
            var info = StringBuilderCache.Acquire();

            info.Append(Localizer.Format("#LOC_KSPIE_MicrowavePowerTransmitter_info1"));//Aperture Diameter
            info.Append(": ").Append(apertureDiameter.ToString("F1")).AppendLine(" m");
            info.Append(Localizer.Format("#LOC_KSPIE_MicrowavePowerTransmitter_info2"));//Can Mirror power
            info.Append(": ").AppendLine(RUIutils.GetYesNoUIString(isMirror));
            info.Append(Localizer.Format("#LOC_KSPIE_MicrowavePowerTransmitter_info3"));//Can Transmit power
            info.Append(": ").AppendLine(RUIutils.GetYesNoUIString(canTransmit));
            info.Append(Localizer.Format("#LOC_KSPIE_MicrowavePowerTransmitter_info4"));//Can Relay independently
            info.Append(": ").AppendLine(RUIutils.GetYesNoUIString(buildInRelay));

            return info.ToStringAndRelease();
        }

        public ModuleConfigurationFlags ModuleConfiguration() => ModuleConfigurationFlags.Fifth;

        public static void StaticFixedUpdate(IResourceManager resMan, Vessel vessel, double activeBeamGeneratorEfficiency,
            bool isEnabled, bool relay, double transmitPower, double powerCapacity, double solarPowerSupplied,
            out double transmissionEfficiencyPercentage, out double moistureModifier,
            out double nuclearPower, out double solarPower, out double availablePower, out double requestedPower, out string bodyName, out string biomeDesc)
        {
            nuclearPower = 0;
            solarPower = 0;
            availablePower = 0;
            requestedPower = 0;
            moistureModifier = 0;
            bodyName = biomeDesc = "";

            transmissionEfficiencyPercentage = activeBeamGeneratorEfficiency;

            if (!HighLogic.LoadedSceneIsFlight) return;

            CollectBiomeData(vessel, out moistureModifier, out biomeDesc, out bodyName);

            if (activeBeamGeneratorEfficiency == 0 || !isEnabled || relay) return;

            double powerTransmissionRatio = (double)(decimal)transmitPower / 100d;
            double transmissionWasteRatio = (100 - activeBeamGeneratorEfficiency) / 100d;
            double transmissionEfficiencyRatio = activeBeamGeneratorEfficiency / 100d;

            var megajoulesRatio = resMan.FillFraction(ResourceName.ElectricCharge);
            var wasteHeatRatio = resMan.FillFraction(ResourceName.WasteHeat);

            var effectiveResourceThrottling = Math.Min(megajoulesRatio > 0.5 ? 1 : megajoulesRatio * 2, wasteHeatRatio < 0.9 ? 1 : (1 - wasteHeatRatio) * 10);

            requestedPower = Math.Min(powerCapacity * powerTransmissionRatio, effectiveResourceThrottling * /*availablePower*/ powerCapacity);
            availablePower = resMan.Consume(ResourceName.ElectricCharge, requestedPower);

            nuclearPower += transmissionEfficiencyRatio * availablePower;
            solarPower += transmissionEfficiencyRatio * solarPowerSupplied;

            // generate waste heat for converting electric power to beamed power
            resMan.Produce(ResourceName.WasteHeat, availablePower * transmissionWasteRatio);

            // extract solar power from stable power
            nuclearPower -= solarPower;

            if (double.IsInfinity(nuclearPower) || double.IsNaN(nuclearPower) || nuclearPower < 0)
                nuclearPower = 0;

            if (double.IsInfinity(solarPower) || double.IsNaN(solarPower) || solarPower < 0)
                solarPower = 0;
        }

        public void KITFixedUpdate(IResourceManager resMan)
        {
            if (!HighLogic.LoadedSceneIsFlight) return;

            var solarPowerSupplied = SolarCells.Sum(m => m.SolarPower);

            StaticFixedUpdate(resMan, vessel, ActiveBeamGeneratorEfficiency, IsEnabled, relay, transmitPower, powerCapacity, solarPowerSupplied,
                out transmissionEfficiencyPercentage, out moistureModifier, out nuclear_power, out solar_power,
                out availablePower, out requestedPower, out body_name, out biome_desc);
        }

        public string KITPartName() => part.partInfo.title;

        public void SetScalar(float t)
        {
            ((IScalarModule)GenericAnimation).SetScalar(t);
        }

        public static string KITPartName(ProtoPartSnapshot protoPartSnapshot, ProtoPartModuleSnapshot protoPartModuleSnapshot) =>
            protoPartSnapshot.partInfo.title;

        public static void KITBackgroundUpdate(IResourceManager resMan, VesselInfo vesselInfo, ProtoPartSnapshot protoPartSnapshot,
            ProtoPartModuleSnapshot protoPartModuleSnapshot, Part part)
        {
            var solarPowerSupply = vesselInfo.Vessel.protoVessel.protoPartSnapshots.SelectMany(partSnapshot => protoPartSnapshot.modules)
                .Where(partModuleSnapshot => vesselInfo.SolarPowerModules.Contains(protoPartModuleSnapshot.moduleName))
                .Sum(partModuleSnapshot => Lib.GetDouble(protoPartModuleSnapshot, "solar_supply"));

            StaticFixedUpdate(resMan, vesselInfo.Vessel, Lib.GetDouble(protoPartModuleSnapshot, nameof(ActiveBeamGeneratorEfficiency)), 
                Lib.GetBool(protoPartModuleSnapshot, nameof(IsEnabled)), Lib.GetBool(protoPartModuleSnapshot, nameof(relay)), 
                Lib.GetDouble(protoPartModuleSnapshot, nameof(transmitPower)), Lib.GetDouble(protoPartModuleSnapshot, nameof(powerCapacity)), solarPowerSupply, out _, out _, out var nuclearPower, out var solarPower, out _, out _, out _, out _);

            protoPartModuleSnapshot.moduleValues.SetValue(nameof(nuclearPower), nuclearPower, true);
            protoPartModuleSnapshot.moduleValues.SetValue(nameof(solarPower), solarPower, true);
        }

        public static ModuleConfigurationFlags
            BackgroundModuleConfiguration(ProtoPartModuleSnapshot protoPartModuleSnapshot) =>
            ModuleConfigurationFlags.Fifth;
    }
}
