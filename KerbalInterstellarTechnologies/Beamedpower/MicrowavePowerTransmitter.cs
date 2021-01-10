﻿using System;
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
        public double power_capacity;
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
        public List<ISolarPower> solarCells;
        public BeamedPowerReceiver part_receiver;
        public List<BeamedPowerReceiver> vessel_recievers;
        public BeamGenerator activeBeamGenerator;
        public List<BeamGenerator> beamGenerators;
        public ModuleAnimateGeneric genericAnimation;

        private BaseEvent activateTransmittervEvent;
        private BaseEvent deactivateTransmitterEvent;
        private BaseEvent activateRelayEvent;
        private BaseEvent deactivateRelayEvent;

        private BaseField apertureDiameterField;
        private BaseField beamedpowerField;
        private BaseField transmitPowerField;
        private BaseField totalAbsorptionPercentageField;
        private BaseField wavelengthField;
        private BaseField wavelengthNameField;

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

            if (genericAnimation != null && genericAnimation.GetScalar < 1)
            {
                genericAnimation.Toggle();
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

        private string WavelengthToText( double waveLength)
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

            if (genericAnimation != null && genericAnimation.GetScalar > 0)
            {
                genericAnimation.Toggle();
            }

            IsEnabled = false;
        }

        [KSPEvent(groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_ActivateRelay", active = false)]//Activate Relay
        public void ActivateRelay()
        {
            if (IsEnabled || relay) return;

            if (genericAnimation != null && genericAnimation.GetScalar < 1)
            {
                genericAnimation.Toggle();
            }

            vessel_recievers = vessel.FindPartModulesImplementing<BeamedPowerReceiver>().Where(m => m.part != part).ToList();

            UpdateRelayWavelength();

            relay = true;
        }

        [KSPEvent(groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_MicrowavePowerTransmitter_DeactivateRelay", active = false)]//Deactivate Relay
        public void DeactivateRelay()
        {
            if (!relay) return;

            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_MicrowavePowerTransmitter_DeactivateRelay_Msg"), 4, ScreenMessageStyle.UPPER_CENTER);//"Relay deactivated"

            if (genericAnimation != null && genericAnimation.GetScalar > 0)
            {
                genericAnimation.Toggle();
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
            var receiversConfiguredForRelay = vessel_recievers.Where(m => m.linkedForRelay).ToList();

            // add build in relay if it can be used for relay
            if (part_receiver != null && buildInRelay)
                receiversConfiguredForRelay.Add(part_receiver);

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

            power_capacity = maximumPower * powerMult;

            if (String.IsNullOrEmpty(partId))
                partId = Guid.NewGuid().ToString();

            // store  aperture and diameter
            aperture = apertureDiameter;
            diameter = apertureDiameter;

            part_receiver = part.FindModulesImplementing<BeamedPowerReceiver>().FirstOrDefault();
            genericAnimation = part.FindModulesImplementing<ModuleAnimateGeneric>().FirstOrDefault(m => m.animationName == animName);

            ConnectToBeamGenerator();

            activateRelayEvent = Events[nameof(ActivateRelay)];
            deactivateRelayEvent = Events[nameof(DeactivateRelay)];
            activateTransmittervEvent = Events[nameof(ActivateTransmitter)];
            deactivateTransmitterEvent = Events[nameof(DeactivateTransmitter)];

            wavelengthField = Fields[nameof(wavelengthText)];
            beamedpowerField = Fields[nameof(beamedpower)];
            transmitPowerField = Fields[nameof(transmitPower)];
            wavelengthNameField = Fields[nameof(wavelengthName)];
            apertureDiameterField = Fields[nameof(apertureDiameter)];
            totalAbsorptionPercentageField = Fields[nameof(totalAbsorptionPercentage)];

            if (state == StartState.Editor)
            {
                part.OnEditorAttach += OnEditorAttach;
                return;
            }

            solarCells = vessel.FindPartModulesImplementing<ISolarPower>();
            vessel_recievers = vessel.FindPartModulesImplementing<BeamedPowerReceiver>().Where(m => m.part != part).ToList();

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
            beamGenerators = part.FindModulesImplementing<BeamGenerator>().Where(m => (m.beamType & compatibleBeamTypes) == m.beamType).ToList();

            if (beamGenerators.Count == 0 && part.parent != null)
            {
                beamGenerators.AddRange(part.parent.FindModulesImplementing<BeamGenerator>().Where(m => (m.beamType & compatibleBeamTypes) == m.beamType));
            }

            if (beamGenerators.Count == 0)
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

                beamGenerators.AddRange(availableGenerators);
            }

            activeBeamGenerator = beamGenerators.FirstOrDefault();

            if (activeBeamGenerator != null)
            {
                activeBeamGenerator.Connect(this);

                if (activeBeamGenerator.part != part)
                    activeBeamGenerator.UpdateMass(maximumPower);
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
                power_capacity = maximumPower * powerMult;
                return;
            }

            UpdateRelayWavelength();

            totalAbsorptionPercentage = atmosphericAbsorptionPercentage + waterAbsorptionPercentage;
            atmosphericAbsorption = totalAbsorptionPercentage / 100;

            bool vesselInSpace = (vessel.situation == Vessel.Situations.ORBITING || vessel.situation == Vessel.Situations.ESCAPING || vessel.situation == Vessel.Situations.SUB_ORBITAL);
            bool receiverOn = part_receiver != null && part_receiver.isActive();
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

            activateTransmittervEvent.active = activeBeamGenerator != null && vesselCanTransmit && !IsEnabled && !relay && !receiverOn && canBeActive;
            deactivateTransmitterEvent.active = IsEnabled;

            canRelay = hasLinkedReceivers && canOperateInCurrentEnvironment;

            activateRelayEvent.active = canRelay && !IsEnabled && !relay && !receiverOn && canBeActive;
            deactivateRelayEvent.active = relay;

            mergingBeams = IsEnabled && canRelay && isBeamMerger;

            bool isTransmitting = IsEnabled && !relay;

            apertureDiameterField.guiActive = isTransmitting;
            beamedpowerField.guiActive = isTransmitting && canBeActive;
            transmitPowerField.guiActive = part_receiver == null || !part_receiver.isActive();

            bool isLinkedForRelay = part_receiver != null && part_receiver.linkedForRelay;
            bool receiverNotInUse = !isLinkedForRelay && !receiverOn && !IsRelay;

            totalAbsorptionPercentageField.guiActive = receiverNotInUse;
            wavelengthField.guiActive = receiverNotInUse;
            wavelengthNameField.guiActive = receiverNotInUse;

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
                else if (beamGenerators.Count == 0)
                    statusStr = Localizer.Format("#LOC_KSPIE_MicrowavePowerTransmitter_Statu6");//"No beam generator found"
                else
                    statusStr = Localizer.Format("#LOC_KSPIE_MicrowavePowerTransmitter_Statu7");//"Inactive."
            }

            if (activeBeamGenerator == null)
            {
                var wavelegthField = Fields[nameof(wavelength)];
                wavelegthField.guiActive = false;
                wavelegthField.guiActiveEditor = false;

                var atmosphericAbsorptionPercentageField = Fields[nameof(atmosphericAbsorptionPercentage)];
                atmosphericAbsorptionPercentageField.guiActive = false;
                atmosphericAbsorptionPercentageField.guiActiveEditor = false;

                var waterAbsorptionPercentageField = Fields[nameof(waterAbsorptionPercentage)];
                waterAbsorptionPercentageField.guiActive = false;
                waterAbsorptionPercentageField.guiActiveEditor = false;

                return;
            }

            wavelength = activeBeamGenerator.wavelength;
            wavelengthText = WavelengthToText(wavelength);
            atmosphericAbsorptionPercentage = activeBeamGenerator.atmosphericAbsorptionPercentage;
            waterAbsorptionPercentage = activeBeamGenerator.waterAbsorptionPercentage * moistureModifier;

            beamedpower = PluginHelper.GetFormattedPowerString(nuclear_power + solar_power);
            solarCells = vessel.FindPartModulesImplementing<ISolarPower>();
        }

        public override void OnFixedUpdate()
        {
            if (!part.enabled)
                base.OnFixedUpdate();
        }

        public void FixedUpdate()
        {

        }

        private void CollectBiomeData()
        {
            moistureModifier = 0;
            biome_desc = string.Empty;

            if (part.vessel == null) return;

            double lat = vessel.latitude * Math.PI / 180d;
            double lon = vessel.longitude * Math.PI / 180d;

            if (part.vessel.mainBody == null) return;

            body_name = part.vessel.mainBody.name;

            if (part.vessel.mainBody.BiomeMap == null) return;

            var attribute = part.vessel.mainBody.BiomeMap.GetAtt(lat, lon);

            if (attribute == null) return;

            biome_desc = attribute.name;

            double cloudVariance;
            if (body_name == "Kerbin" || body_name == "Earth")
            {
                if (biome_desc == "Desert" || biome_desc == "Ice Caps" || biome_desc == "BadLands")
                    moistureModifier = 0.4;
                else if (biome_desc == "Water")
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

        public double PowerCapacity =>  power_capacity;

        public double Wavelength => activeBeamGenerator != null ? activeBeamGenerator.wavelength : nativeWaveLength;

        public string WavelengthName => activeBeamGenerator != null ? activeBeamGenerator.beamWaveName : "";

        public double CombinedAtmosphericAbsorption =>
            activeBeamGenerator != null
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

            var relayPersistence = new VesselRelayPersistence(vessel) {IsActive = true};

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
                        PowerCapacity = relay.power_capacity,
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
                    transmitData.PowerCapacity += relay.power_capacity;
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

            var vesselTransmitters = new VesselMicrowavePersistence(vessel) {IsActive = true};

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
                        PowerCapacity = transmitter.power_capacity,
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
                    transmitData.PowerCapacity += transmitter.power_capacity;
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
                    var powerCapacity = double.Parse(protoModule.moduleValues.GetValue("power_capacity"));
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
                        var powerCapacity = double.Parse(protoModule.moduleValues.GetValue("power_capacity"));

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

        public bool ModuleConfiguration(out int priority, out bool supplierOnly, out bool hasLocalResources)
        {
            priority = 5;
            supplierOnly = false;
            hasLocalResources = false;

            return true;
        }

        public void KITFixedUpdate(IResourceManager resMan)
        {
            if (activeBeamGenerator != null)
                transmissionEfficiencyPercentage = activeBeamGenerator.efficiencyPercentage;

            if (!HighLogic.LoadedSceneIsFlight) return;

            nuclear_power = 0;
            solar_power = 0;
            availablePower = 0;
            requestedPower = 0;

            CollectBiomeData();

            base.OnFixedUpdate();

            if (activeBeamGenerator != null && IsEnabled && !relay)
            {
                double powerTransmissionRatio = (double)(decimal)transmitPower / 100d;
                double transmissionWasteRatio = (100 - activeBeamGenerator.efficiencyPercentage) / 100d;
                double transmissionEfficiencyRatio = activeBeamGenerator.efficiencyPercentage / 100d;

                var megajoulesRatio = resMan.FillFraction(ResourceName.ElectricCharge);
                var wasteheatRatio = resMan.FillFraction(ResourceName.WasteHeat);

                var effectiveResourceThrottling = Math.Min(megajoulesRatio > 0.5 ? 1 : megajoulesRatio * 2, wasteheatRatio < 0.9 ? 1 : (1 - wasteheatRatio) * 10);

                requestedPower = Math.Min(power_capacity * powerTransmissionRatio, effectiveResourceThrottling * /*availablePower*/ power_capacity);
                availablePower = resMan.Consume(ResourceName.ElectricCharge, requestedPower);

                nuclear_power += transmissionEfficiencyRatio * availablePower;
                solar_power += transmissionEfficiencyRatio * solarCells.Sum(m => m.SolarPower);

                // generate wasteheat for converting electric power to beamed power
                resMan.Produce(ResourceName.WasteHeat, availablePower * transmissionWasteRatio);
            }

            // extract solar power from stable power
            nuclear_power -= solar_power;

            if (double.IsInfinity(nuclear_power) || double.IsNaN(nuclear_power) || nuclear_power < 0)
                nuclear_power = 0;

            if (double.IsInfinity(solar_power) || double.IsNaN(solar_power) || solar_power < 0)
                solar_power = 0;
        }

        public string KITPartName() => part.partInfo.title;

        public void SetScalar(float t)
        {
            ((IScalarModule)genericAnimation).SetScalar(t);
        }
    }
}
