using KIT.Resources;
using KSP.Localization;
using System;
using System.Linq;
using KIT.Powermanagement.Interfaces;
using UnityEngine;
using KIT.ResourceScheduler;

namespace KIT.Propulsion
{
    class InterstellarMagneticNozzleControllerFX : PartModule, IKITModule, IFnEngineNozzle
    {
        public const string GROUP = "MagneticNozzleController";
        public const string GROUP_TITLE = "#LOC_KSPIE_MagneticNozzleControllerFX_groupName";

        private const bool DebugController = false;

        //Persistent
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = DebugController, guiActiveEditor = true, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_SimulatedThrottle"), UI_FloatRange(stepIncrement = 0.5f, maxValue = 100f, minValue = 0.5f)]//Simulated Throttle
        public float simulatedThrottle = 0.5f;
        [KSPField(isPersistant = true)]
        double powerBufferStore;
        [KSPField(isPersistant = true)]
        public bool exhaustAllowed = true;

        // Non Persistent fields
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiName = "#LOC_KSPIE_ThermalNozzleController_Radius", guiActiveEditor = true, guiFormat = "F2", guiUnits = "m")]
        public double radius = 2.5;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, guiName = "#LOC_KSPIE_FusionEngine_partMass", guiActiveEditor = true, guiFormat = "F3", guiUnits = " t")]
        public float partMass = 1;

        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_ChargedParticleMaximumPercentageUsage", guiFormat = "F3", guiActive = DebugController)]//CP max fraction usage
        private double _chargedParticleMaximumPercentageUsage;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_MaxChargedParticlesPower", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2", guiActive = DebugController)]//Max CP Power
        private double _max_charged_particles_power;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_RequestedParticles", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2", guiActive = DebugController)]//Requested Particles
        private double _charged_particles_requested;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_RecievedParticles", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2", guiActive = DebugController)]//Recieved Particles
        private double _charged_particles_received;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_RequestedElectricity", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2", guiActive = DebugController)]//Requested Electricity
        private double _requestedElectricPower;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_RecievedElectricity", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2", guiActive = DebugController)]//Recieved Electricity
        private double _recievedElectricPower;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_Thrust", guiUnits = " kN", guiActive = DebugController)]//Thrust
        private double _engineMaxThrust;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_Consumption", guiUnits = " kg/s", guiActive = DebugController)]//Consumption
        private double calculatedConsumptionPerSecond;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_ThrotleExponent", guiActive = DebugController)]//Throtle Exponent
        protected double throttleExponent = 1;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_MaximumChargedPower", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F2", guiActive = DebugController)]//Maximum ChargedPower
        protected double maximumChargedPower;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_PowerThrustModifier", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F1", guiActive = DebugController)]//Power Thrust Modifier
        protected double powerThrustModifier;
        [KSPField(groupName = GROUP, guiActiveEditor = true, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_Minimumisp", guiUnits = " s", guiFormat = "F1", guiActive = DebugController)]//Minimum isp
        protected double minimum_isp;
        [KSPField(groupName = GROUP, guiActiveEditor = true, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_Maximumisp", guiUnits = " s", guiFormat = "F1", guiActive = DebugController)]//Maximum isp
        protected double maximum_isp;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_PowerRatio", guiActive = DebugController)]//Power Ratio
        protected double megajoulesRatio;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_EngineIsp", guiActive = DebugController)]//Engine Isp
        protected double engineIsp;
        [KSPField(groupName = GROUP, guiName = "#LOC_KSPIE_MagneticNozzleControllerFX_EngineFuelFlow", guiActive = DebugController)]//Engine Fuel Flow
        protected float engineFuelFlow;
        [KSPField(guiActive = false)]
        protected double chargedParticleRatio;
        [KSPField(guiActive = false)]
        protected double MaxTheoreticalThrust;
        [KSPField(guiActive = false)]
        protected double MaxTheoreticalFuelFlowRate;
        [KSPField(guiActive = false)]
        protected double currentIsp;
        [KSPField(guiActive = false)]
        protected float currentThrust;
        [KSPField(guiActive = false)]
        protected double wasteheatConsumption;

        [KSPField]
        public bool showPartMass = true;
        [KSPField]
        public double powerThrustMultiplier = 1;
        [KSPField]
        public float wasteHeatMultiplier = 1;
        [KSPField]
        public bool maintainsPropellantBuffer = true;
        [KSPField]
        public double minimumPropellantBuffer = 0.01;
        [KSPField]
        public string propellantBufferResourceName = "LqdHydrogen";
        [KSPField]
        public string runningEffectName = String.Empty;
        [KSPField]
        public string powerEffectName = String.Empty;

        //Internal
        private UI_FloatRange simulatedThrottleFloatRange;
        private ModuleEnginesFX _attachedEngine;
        private ModuleEnginesWarp _attachedWarpableEngine;
        private PartResourceDefinition propellantBufferResourceDefinition;
        readonly Guid _id = Guid.NewGuid();

        IFNChargedParticleSource _attachedReactor;

        int _attachedReactorDistance;
        double _exchangerThrustDivisor;
        double _previousChargedParticlesReceived;
        double _maxPowerMultiplier;
        double _powerBufferMax;

        public IFNChargedParticleSource AttachedReactor
        {
            get => _attachedReactor;
            private set
            {
                _attachedReactor = value;
                _attachedReactor?.AttachThermalReceiver(_id, radius);
            }
        }

        public double GetNozzleFlowRate() => _attachedEngine?.maxFuelFlow ?? 0;

        public bool PropellantAbsorbsNeutrons => false;

        public bool RequiresPlasmaHeat => false;

        public bool RequiresThermalHeat => false;

        public float CurrentThrottle => _attachedEngine.currentThrottle > 0 ? (maximum_isp == minimum_isp ? _attachedEngine.currentThrottle : 1) : 0;

        public bool RequiresChargedPower => true;

        public override void OnStart(StartState state)
        {
            if (maintainsPropellantBuffer)
                propellantBufferResourceDefinition = PartResourceLibrary.Instance.GetDefinition(propellantBufferResourceName);

            if (state == StartState.Editor)
            {
                part.OnEditorAttach += OnEditorAttach;
                part.OnEditorDetach += OnEditorDetach;
            }

            _attachedWarpableEngine = part.FindModuleImplementing<ModuleEnginesWarp>();
            _attachedEngine = _attachedWarpableEngine;

            if (_attachedEngine != null)
                _attachedEngine.Fields["finalThrust"].guiFormat = "F5";

            if (_attachedEngine != null && _attachedEngine is ModuleEnginesFX)
            {
                if (!string.IsNullOrEmpty(runningEffectName))
                    part.Effect(runningEffectName, 0, -1);
                if (!string.IsNullOrEmpty(powerEffectName))
                    part.Effect(powerEffectName, 0, -1);
            }

            ConnectToReactor();

            UpdateEngineStats(true);

            _maxPowerMultiplier = Math.Log10(maximum_isp / minimum_isp);

            throttleExponent = Math.Abs(Math.Log10(_attachedReactor.MinimumChargedIspMult / _attachedReactor.MaximumChargedIspMult));

            simulatedThrottleFloatRange = Fields["simulatedThrottle"].uiControlEditor as UI_FloatRange;
            System.Diagnostics.Debug.Assert(simulatedThrottleFloatRange != null, nameof(simulatedThrottleFloatRange) + " != null");

            simulatedThrottleFloatRange.onFieldChanged += UpdateFromGUI;

            if (_attachedReactor == null)
            {
                Debug.LogWarning("[KSPI]: InterstellarMagneticNozzleControllerFX.OnStart no IChargedParticleSource found for MagneticNozzle!");
                return;
            }
            _exchangerThrustDivisor = radius >= _attachedReactor.Radius ? 1 : radius * radius / _attachedReactor.Radius / _attachedReactor.Radius;

            InitializesPropellantBuffer();

            Debug.Log($"[OnStart] Fields[nameof(partMass)] = {Fields[nameof(partMass)]}");

            Fields[nameof(partMass)].guiActiveEditor = showPartMass;
            Fields[nameof(partMass)].guiActive = showPartMass;
        }

        private void InitializesPropellantBuffer()
        {
            if (maintainsPropellantBuffer && string.IsNullOrEmpty(propellantBufferResourceName) == false && part.Resources[propellantBufferResourceName] == null)
            {
                Debug.Log("[KSPI]: Added " + propellantBufferResourceName + " buffer to MagneticNozzle");
                var newResourceNode = new ConfigNode("RESOURCE");
                newResourceNode.AddValue("name", propellantBufferResourceName);
                newResourceNode.AddValue("maxAmount", minimumPropellantBuffer);
                newResourceNode.AddValue("amount", minimumPropellantBuffer);

                part.AddResource(newResourceNode);
            }

            var bufferResource = part.Resources[propellantBufferResourceName];
            if (maintainsPropellantBuffer && bufferResource != null)
                bufferResource.amount = bufferResource.maxAmount;
        }

        /// <summary>
        /// Event handler which is called when part is attached to a new part
        /// </summary>
        private void OnEditorAttach()
        {
            Debug.Log("[KSPI]: Attaching " + part.partInfo.title);

            if (!HighLogic.LoadedSceneIsEditor || _attachedEngine == null) return;

            ConnectToReactor();
            UpdateEngineStats(true);
        }

        /// <summary>
        /// Event handler which is called when part is detached from thermal source
        /// </summary>
        public void OnEditorDetach()
        {
            Debug.Log("[KSPI]: Detaching " + part.partInfo.title);

            _attachedReactor?.DisconnectWithEngine(this);
            _attachedReactor = null;

            UpdateEngineStats(true);
        }

        private void UpdateFromGUI(BaseField field, object oldFieldValueObj)
        {
            UpdateEngineStats(true);
        }

        private void ConnectToReactor()
        {
            // first try to look in part, otherwise try to find the nearest source
            _attachedReactor = part.FindModuleImplementing<IFNChargedParticleSource>() ??
                                BreadthFirstSearchForChargedParticleSource(10, 1);

            _attachedReactor?.ConnectWithEngine(this);
        }

        private IFNChargedParticleSource BreadthFirstSearchForChargedParticleSource(int stackDepth, int parentDepth)
        {
            for (int currentDepth = 0; currentDepth <= stackDepth; currentDepth++)
            {
                IFNChargedParticleSource particleSource = FindChargedParticleSource(part, currentDepth, parentDepth);

                if (particleSource != null)
                {
                    _attachedReactorDistance = currentDepth;
                    return particleSource;
                }
            }
            return null;
        }

        private IFNChargedParticleSource FindChargedParticleSource(Part currentPart, int stackDepth, int parentDepth)
        {
            if (currentPart == null)
                return null;

            if (stackDepth == 0)
                return currentPart.FindModulesImplementing<IFNChargedParticleSource>().FirstOrDefault();

            foreach (var attachNodes in currentPart.attachNodes.Where(atn => atn.attachedPart != null))
            {
                IFNChargedParticleSource particleSource = FindChargedParticleSource(attachNodes.attachedPart, (stackDepth - 1), parentDepth);

                if (particleSource != null)
                    return particleSource;
            }

            if (parentDepth > 0)
            {
                IFNChargedParticleSource particleSource = FindChargedParticleSource(currentPart.parent, (stackDepth - 1), (parentDepth - 1));

                if (particleSource != null)
                    return particleSource;
            }

            return null;
        }

        private void UpdateEngineStats(bool useThrustCurve)
        {
            if (_attachedReactor == null || _attachedEngine == null)
            {
                minimum_isp = 0;
                maximum_isp = 0;
                _engineMaxThrust = 0;
                maximumChargedPower = 0;
                return;
            }

            // set Isp
            var joulesPerAmu = _attachedReactor.CurrentMeVPerChargedProduct * 1e6 * GameConstants.ElectronCharge / GameConstants.DilutionFactor;
            var calculatedIsp = Math.Sqrt(joulesPerAmu * 2 / GameConstants.AtomicMassUnit) / GameConstants.StandardGravity;

            // calculate max and min isp
            minimum_isp = calculatedIsp * _attachedReactor.MinimumChargedIspMult;
            maximum_isp = calculatedIsp * _attachedReactor.MaximumChargedIspMult;

            if (useThrustCurve)
            {
                var currentIsp = Math.Min(maximum_isp, minimum_isp / Math.Pow(simulatedThrottle / 100, throttleExponent));

                FloatCurve newAtmosphereCurve = new FloatCurve();
                newAtmosphereCurve.Add(0, (float)currentIsp);
                newAtmosphereCurve.Add(0.002f, 0);
                _attachedEngine.atmosphereCurve = newAtmosphereCurve;

                // set maximum fuel flow
                var powerThrustModifierLocal = GameConstants.BaseThrustPowerMultiplier * powerThrustMultiplier;
                maximumChargedPower = _attachedReactor.MaximumChargedPower;
                _powerBufferMax = maximumChargedPower / 10000;


                _engineMaxThrust = powerThrustModifierLocal * maximumChargedPower / currentIsp / GameConstants.StandardGravity;
                var maxFuelFlowRate = _engineMaxThrust / currentIsp / GameConstants.StandardGravity;
                _attachedEngine.maxFuelFlow = (float)maxFuelFlowRate;
                _attachedEngine.maxThrust = (float)_engineMaxThrust;

                FloatCurve newThrustCurve = new FloatCurve();
                newThrustCurve.Add(0, (float)_engineMaxThrust);
                newThrustCurve.Add(0.001f, 0);

                _attachedEngine.thrustCurve = newThrustCurve;
                _attachedEngine.useThrustCurve = true;
            }
        }

        public virtual void Update()
        {
            partMass = part.mass;

            UpdateEngineStats(!HighLogic.LoadedSceneIsFlight);
        }

        // FixedUpdate is also called in the Editor
        public void FixedUpdate()
        {

        }


        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

        }

        private void UpdatePowerEffect()
        {
            if (string.IsNullOrEmpty(powerEffectName))
                return;

            var powerEffectRatio = exhaustAllowed && _attachedEngine != null && _attachedEngine.isOperational && _chargedParticleMaximumPercentageUsage > 0 && currentThrust > 0 ? _attachedEngine.currentThrottle : 0;
            part.Effect(powerEffectName, powerEffectRatio);
        }

        private void UpdateRunningEffect()
        {
            if (string.IsNullOrEmpty(runningEffectName))
                return;

            var runningEffectRatio = exhaustAllowed && _attachedEngine != null && _attachedEngine.isOperational && _chargedParticleMaximumPercentageUsage > 0 && currentThrust > 0 ? _attachedEngine.currentThrottle : 0;
            part.Effect(runningEffectName, runningEffectRatio);
        }

        // Note: does not seem to be called while in vab mode
        public override void OnUpdate()
        {
            if (_attachedEngine == null)
                return;

            exhaustAllowed = AllowedExhaust();
        }

        public void UpdatePropellantBuffer(double calculatedConsumptionInTon)
        {
            if (propellantBufferResourceDefinition == null)
                return;

            PartResource propellantPartResource = part.Resources[propellantBufferResourceName];

            if (propellantPartResource == null || propellantBufferResourceDefinition.density == 0)
                return;

            var newMaxAmount = Math.Max(minimumPropellantBuffer, 2 * TimeWarp.fixedDeltaTime * calculatedConsumptionInTon / propellantBufferResourceDefinition.density);

            var storageShortage = Math.Max(0, propellantPartResource.amount - newMaxAmount);

            propellantPartResource.maxAmount = newMaxAmount;
            propellantPartResource.amount = Math.Min(newMaxAmount, propellantPartResource.amount);

            if (storageShortage > 0)
                part.RequestResource(propellantBufferResourceName, -storageShortage);
        }

        public override string GetInfo()
        {
            return "";
        }

        private bool AllowedExhaust()
        {
            if (HighLogic.CurrentGame.Parameters.CustomParams<KITGamePlayParams>().AllowDestructiveEngines) return true;

            var homeworld = FlightGlobals.GetHomeBody();
            var toHomeworld = vessel.CoMD - homeworld.position;
            var distanceToSurfaceHomeworld = toHomeworld.magnitude - homeworld.Radius;
            var cosineAngle = Vector3d.Dot(part.transform.up.normalized, toHomeworld.normalized);
            var currentExhaustAngle = Math.Acos(cosineAngle) * (180 / Math.PI);

            if (double.IsNaN(currentExhaustAngle) || double.IsInfinity(currentExhaustAngle))
                currentExhaustAngle = cosineAngle > 0 ? 180 : 0;

            if (AttachedReactor == null)
                return false;

            if (AttachedReactor.MayExhaustInAtmosphereHomeworld) return true;

            var minAltitude = AttachedReactor.MayExhaustInLowSpaceHomeworld ? homeworld.atmosphereDepth : homeworld.scienceValues.spaceAltitudeThreshold;

            if (distanceToSurfaceHomeworld < minAltitude)
                return false;

            if (AttachedReactor.MayExhaustInLowSpaceHomeworld && distanceToSurfaceHomeworld > 10 * homeworld.Radius)
                return true;

            if (!AttachedReactor.MayExhaustInLowSpaceHomeworld && distanceToSurfaceHomeworld > 20 * homeworld.Radius)
                return true;

            var radiusDividedByAltitude = (homeworld.Radius + minAltitude) / toHomeworld.magnitude;

            var coneAngle = 45 * radiusDividedByAltitude * radiusDividedByAltitude * radiusDividedByAltitude * radiusDividedByAltitude * radiusDividedByAltitude;

            var allowedExhaustAngle = coneAngle + Math.Tanh(radiusDividedByAltitude) * (180 / Math.PI);

            if (allowedExhaustAngle < 3)
                return true;

            return currentExhaustAngle > allowedExhaustAngle;
        }

        public bool ModuleConfiguration(out int priority, out bool supplierOnly, out bool hasLocalResources)
        {
            priority = 3;
            supplierOnly = false;
            hasLocalResources = false;

            return true;
        }

        public void KITFixedUpdate(IResourceManager resMan)
        {
            if (HighLogic.LoadedSceneIsEditor)
                return;

            UpdateRunningEffect();
            UpdatePowerEffect();


            if (_attachedEngine == null)
                return;

            if (_attachedEngine.currentThrottle > 0 && !exhaustAllowed)
            {
                string message = AttachedReactor.MayExhaustInLowSpaceHomeworld
                    ? Localizer.Format("#LOC_KSPIE_MagneticNozzleControllerFX_PostMsg1")//"Engine halted - Radioactive exhaust not allowed towards or inside homeworld atmosphere"
                    : Localizer.Format("#LOC_KSPIE_MagneticNozzleControllerFX_PostMsg2");//"Engine halted - Radioactive exhaust not allowed towards or near homeworld atmosphere"

                ScreenMessages.PostScreenMessage(message, 5, ScreenMessageStyle.UPPER_CENTER);
                vessel.ctrlState.mainThrottle = 0;

                // Return to realtime
                if (vessel.packed)
                    TimeWarp.SetRate(0, true);
            }

            _chargedParticleMaximumPercentageUsage = _attachedReactor?.ChargedParticlePropulsionEfficiency ?? 0;

            if (_chargedParticleMaximumPercentageUsage > 0)
            {
                maximumChargedPower = _attachedReactor.MaximumChargedPower;
                var currentMaximumChargedPower = maximum_isp == minimum_isp ? maximumChargedPower * _attachedEngine.currentThrottle : maximumChargedPower;

                _max_charged_particles_power = currentMaximumChargedPower * _exchangerThrustDivisor * _attachedReactor.ChargedParticlePropulsionEfficiency;
                _charged_particles_requested = exhaustAllowed && _attachedEngine.isOperational && _attachedEngine.currentThrottle > 0 ? _max_charged_particles_power : 0;

                _charged_particles_received = _charged_particles_requested > 0 ?
                    resMan.Consume(ResourceName.ChargedParticle, _charged_particles_requested) : 0;

                // update Isp
                currentIsp = !_attachedEngine.isOperational || _attachedEngine.currentThrottle == 0 ? maximum_isp : Math.Min(maximum_isp, minimum_isp / Math.Pow(_attachedEngine.currentThrottle, throttleExponent));

                var localPowerThrustModifier = GameConstants.BaseThrustPowerMultiplier * powerThrustMultiplier;
                var maxEngineThrustAtMaxIsp = localPowerThrustModifier * _charged_particles_received / maximum_isp / GameConstants.StandardGravity;

                var calculatedConsumptionInTon = maxEngineThrustAtMaxIsp / maximum_isp / GameConstants.StandardGravity;

                UpdatePropellantBuffer(calculatedConsumptionInTon);

                // convert reactor product into propellants when possible and generate addition propellant from reactor fuel consumption
                chargedParticleRatio = currentMaximumChargedPower > 0 ? _charged_particles_received / currentMaximumChargedPower : 0;
                _attachedReactor.UseProductForPropulsion(resMan, chargedParticleRatio, calculatedConsumptionInTon);

                calculatedConsumptionPerSecond = calculatedConsumptionInTon * 1000;

                if (!CheatOptions.IgnoreMaxTemperature)
                {
                    if (_attachedEngine.isOperational && _attachedEngine.currentThrottle > 0)
                    {
                        wasteheatConsumption = _charged_particles_received > _previousChargedParticlesReceived
                            ? _charged_particles_received + (_charged_particles_received - _previousChargedParticlesReceived)
                            : _charged_particles_received - (_previousChargedParticlesReceived - _charged_particles_received);

                        _previousChargedParticlesReceived = _charged_particles_received;
                    }
                    //else if (_previous_charged_particles_received > 0)
                    //{
                    //    wasteheatConsumption = _previous_charged_particles_received;
                    //    _previous_charged_particles_received = 0;
                    //}
                    else
                    {
                        wasteheatConsumption = 0;
                        _charged_particles_received = 0;
                        _previousChargedParticlesReceived = 0;
                    }

                    resMan.Consume(ResourceName.WasteHeat, wasteheatConsumption);
                }

                if (_charged_particles_received == 0)
                {
                    _chargedParticleMaximumPercentageUsage = 0;

                    UpdateRunningEffect();
                    UpdatePowerEffect();
                }

                // calculate power cost
                var ispPowerCostMultiplier = 1 + _maxPowerMultiplier - Math.Log10(currentIsp / minimum_isp);
                var minimumEnginePower = _attachedReactor.MagneticNozzlePowerMult * _charged_particles_received * ispPowerCostMultiplier * 0.005 * Math.Max(_attachedReactorDistance, 1);
                var neededBufferPower = Math.Min(resMan.CurrentCapacity(ResourceName.ElectricCharge), Math.Min(Math.Max(_powerBufferMax - powerBufferStore, 0), minimumEnginePower));
                _requestedElectricPower = minimumEnginePower + neededBufferPower;

                _recievedElectricPower = CheatOptions.InfiniteElectricity || _requestedElectricPower == 0
                    ? _requestedElectricPower
                    : resMan.Consume(ResourceName.ElectricCharge, _requestedElectricPower);

                // adjust power buffer
                var powerSurplus = _recievedElectricPower - minimumEnginePower;
                if (powerSurplus < 0)
                {
                    var powerFromBuffer = Math.Min(-powerSurplus, powerBufferStore);
                    _recievedElectricPower += powerFromBuffer;
                    powerBufferStore -= powerFromBuffer;
                }
                else
                    powerBufferStore += powerSurplus;

                // calculate Power factor
                megajoulesRatio = Math.Min(_recievedElectricPower / minimumEnginePower, 1);
                megajoulesRatio = (double.IsNaN(megajoulesRatio) || double.IsInfinity(megajoulesRatio)) ? 0 : megajoulesRatio;
                var scaledPowerFactor = Math.Pow(megajoulesRatio, 0.5);

                double effectiveThrustRatio = 1;

                _engineMaxThrust = 0;
                if (_max_charged_particles_power > 0)
                {
                    var maxThrust = powerThrustModifier * _charged_particles_received * scaledPowerFactor / currentIsp / GameConstants.StandardGravity;

                    var effectiveThrust = Math.Max(maxThrust - (radius * radius * vessel.atmDensity * 100), 0);

                    effectiveThrustRatio = maxThrust > 0 ? effectiveThrust / maxThrust : 0;

                    _engineMaxThrust = _attachedEngine.currentThrottle > 0
                        ? Math.Max(effectiveThrust, 1e-9)
                        : Math.Max(maxThrust, 1e-9);
                }

                // set isp
                FloatCurve newAtmosphereCurve = new FloatCurve();
                engineIsp = _attachedEngine.currentThrottle > 0 ? (currentIsp * scaledPowerFactor * effectiveThrustRatio) : currentIsp;
                newAtmosphereCurve.Add(0, (float)engineIsp, 0, 0);
                _attachedEngine.atmosphereCurve = newAtmosphereCurve;

                var maxEffectiveFuelFlowRate = !double.IsInfinity(_engineMaxThrust) && !double.IsNaN(_engineMaxThrust) && currentIsp > 0
                    ? _engineMaxThrust / currentIsp / GameConstants.StandardGravity / (_attachedEngine.currentThrottle > 0 ? _attachedEngine.currentThrottle : 1)
                    : 0;

                MaxTheoreticalThrust = powerThrustModifier * maximumChargedPower * _chargedParticleMaximumPercentageUsage / currentIsp / GameConstants.StandardGravity;
                MaxTheoreticalFuelFlowRate = MaxTheoreticalThrust / currentIsp / GameConstants.StandardGravity;

                // set maximum flow
                engineFuelFlow = _attachedEngine.currentThrottle > 0 ? Math.Max((float)maxEffectiveFuelFlowRate, 1e-9f) : (float)MaxTheoreticalFuelFlowRate;

                _attachedEngine.maxFuelFlow = engineFuelFlow;
                _attachedEngine.useThrustCurve = false;

                // This whole thing may be inefficient, but it should clear up some confusion for people.
                if (_attachedEngine.getFlameoutState) return;

                if (_attachedEngine.currentThrottle < 0.01)
                    _attachedEngine.status = Localizer.Format("#LOC_KSPIE_MagneticNozzleControllerFX_statu1");//"offline"
                else if (megajoulesRatio < 0.75 && _requestedElectricPower > 0)
                    _attachedEngine.status = Localizer.Format("#LOC_KSPIE_MagneticNozzleControllerFX_statu2");//"Insufficient Electricity"
                else if (effectiveThrustRatio < 0.01 && vessel.atmDensity > 0)
                    _attachedEngine.status = Localizer.Format("#LOC_KSPIE_MagneticNozzleControllerFX_statu3");//"Too dense atmospherere"
            }
            else
            {
                _chargedParticleMaximumPercentageUsage = 0;
                _attachedEngine.maxFuelFlow = 0.0000000001f;
                _recievedElectricPower = 0;
                _charged_particles_requested = 0;
                _charged_particles_received = 0;
                _engineMaxThrust = 0;
            }

            currentThrust = _attachedEngine.GetCurrentThrust();
        }

        public string KITPartName() => part.partInfo.title;
    }
}
