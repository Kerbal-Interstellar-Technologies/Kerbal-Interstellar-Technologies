using System.Collections.Generic;
using System.Linq;
using KIT.ResourceScheduler;
using KSP.Localization;
using UnityEngine;

namespace KIT.Resources
{
    class BoilOffConfiguration
    {
        /*
        resourceGUIName =	Liquid Helium
        boilOffRate     =	0
        boilOffTemp     =	4.222
        boilOffMultiplier =	1
        boilOffBase	= 	1000
        boilOffAddition =	8.97215e-8
        */

        public ResourceName Name;
        public string ResourceGuiName;
        public double BoilOffRate;
        public double BoilOffTemp;
        public double BoilOffMultiplier;
        public double BoilOffBase;
        public double BoilOffAddition;

        private readonly bool _failed;

        public bool Failed => _failed;

        public BoilOffConfiguration(ConfigNode node)
        {
            Name = KITResourceSettings.NameToResource(node.name);
            _failed = (Name == ResourceName.Unknown);
            _failed |= !node.TryGetValue("resourceGUIName", ref ResourceGuiName);
            _failed |= !node.TryGetValue("boilOffRate", ref BoilOffRate);
            _failed |= !node.TryGetValue("boilOffTemp", ref BoilOffTemp);
            _failed |= !node.TryGetValue("boilOffMultiplier", ref BoilOffMultiplier);
            _failed |= !node.TryGetValue("boilOffBase", ref BoilOffBase);
            _failed |= !node.TryGetValue("boilOffAddition", ref BoilOffAddition);

            if (_failed)
                Debug.Log($"[BoilOffConfiguration] Unable to parse KIT_CRYOSTAT_CONFIG node {node.name}, got {Name}, {ResourceGuiName}, {BoilOffRate}, {BoilOffTemp}, {BoilOffMultiplier}, {BoilOffBase}, and {BoilOffAddition}");
        }
    }

    [KSPModule("Cryostat")]
    class KITCryostat : PartModule
    {
        private const string Group = "KITCryostat";
        private const string GroupTitle = "KIT Cryostat";

        private static readonly Dictionary<string, BoilOffConfiguration> _boilOffConfigurations = new Dictionary<string, BoilOffConfiguration>();
        private static bool _configurationsInitialized;

        public override void OnStart(StartState state)
        {
            if (!_configurationsInitialized)
            {
                _configurationsInitialized = true;

                var root = GameDatabase.Instance.GetConfigNodes("KIT_CRYOSTAT_CONFIG");
                if (root == null || !root.Any())
                {
                    Debug.Log($"[KITCryostat] Unable to find KIT_CRYOSTAT_CONFIG");
                    return;
                }

                foreach (var node in root[0].GetNodes())
                {
                    var config = new BoilOffConfiguration(node);
                    if (config.Failed) continue;

                    _boilOffConfigurations.Add(node.name, config);
                }
            }
        }

        public ResourcePriorityValue ResourceProcessPriority() => ResourcePriorityValue.First;
        private readonly string _partName = Localizer.Format("#LOC_KIT_Vessel_Cryostat_PartName");
        public string KITPartName() => _partName;

        public void KITFixedUpdate(IResourceManager resMan)
        {
            if (resMan.CheatOptions().IgnoreMaxTemperature) return;

            foreach (var resource in part.Resources)
            {
                if (!_boilOffConfigurations.TryGetValue(resource.resourceName, out var configuration)) continue;
                
                
            }
        }
    }
}

/*
 * using KSP.Localization;
using System;
using KIT.Resources;
using UnityEngine;
using KIT.ResourceScheduler;

namespace KIT
{
    [KSPModule("Cryostat")]
    class ModuleStorageCryostat: FNModuleCryostat {}

    [KSPModule("Cryostat")]
    class FNModuleCryostat : PartModule, IKITMod
    {
        public const string GROUP = "FNModuleCryostat";
        public const string GROUP_TITLE = "Interstellar Cryostat";

        // Persistant
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = true, guiActive = true, guiName = "#LOC_IFS_Cryostat_Cooling"), UI_Toggle(disabledText = "#LOC_IFS_Cryostat_On", enabledText = "#LOC_IFS_Cryostat_Off")]//Cooling--On--Off
        public bool isDisabled = false;

        [KSPField(isPersistant = true)]
        public double storedTemp = 0;

        // Confiration
        [KSPField] public string resourceName = "";
        [KSPField] public string resourceGUIName = "";
        [KSPField] public double boilOffRate = 0;
        [KSPField] public double powerReqKW = 0;
        [KSPField] public double powerReqMult = 1;
        [KSPField] public double boilOffMultiplier = 0;
        [KSPField] public double boilOffBase = 10000;
        [KSPField] public double boilOffAddition = 0;
        [KSPField] public double boilOffTemp = 20.271;
        [KSPField] public double convectionMod = 1;
        [KSPField] public bool showPower = true;
        [KSPField] public bool showBoiloff = true;
        [KSPField] public bool showTemp = true;
        [KSPField] public bool warningShown;
        [KSPField] public int initializationCountdown = 10;

        //GUI
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = false, guiActive = false, guiName = "#LOC_KSPIE_ModuleCryostat_Power")]//Power
        public string powerStatusStr = string.Empty;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = false, guiActive = false, guiName = "#LOC_KSPIE_ModuleCryostat_Boiloff")]//Boiloff
        public string boiloffStr;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = false, guiActive = false, guiName = "#LOC_KSPIE_ModuleCryostat_Temperature", guiFormat = "F2", guiUnits = " K")]//Temperature
        public double externalTemperature;
        [KSPField(groupName = GROUP, groupDisplayName = GROUP_TITLE, isPersistant = false, guiActive = false, guiName = "#LOC_KSPIE_ModuleCryostat_Internalboiloff")]//internal boiloff
        public double boiloff;

        private BaseField isDisabledField;
        private BaseField boiloffStrField;
        private BaseField powerStatusStrField;
        private BaseField externalTemperatureField;

        private double environmentBoiloff;
        private double environmentFactor;
        private double receivedPowerKW;
        private double previousReceivedPowerKW;
        private double currentPowerReq;
        private double previousPowerReq;

        private bool requiresPower;

        public override void OnStart(PartModule.StartState state)
        {
            enabled = true;

            // compensate for stock solar initialization heating issues
            part.temperature = storedTemp;
            requiresPower = powerReqKW > 0;

            isDisabledField = Fields[nameof(isDisabled)];
            boiloffStrField = Fields[nameof(boiloffStr)];
            powerStatusStrField = Fields[nameof(powerStatusStr)];
            externalTemperatureField = Fields[nameof(externalTemperature)];

            if (state == StartState.Editor) return;

            part.temperature = storedTemp;
            part.skinTemperature = storedTemp;

            // if electricCharge buffer is missing, add it.
            if (!part.Resources.Contains(KITResourceSettings.ElectricCharge))
            {
                Debug.Log($"{part.ClassName} with {this.moduleName} is missing an Electric Charge Resource. Please fix.");
            }
        }

        public void Update()
        {
            storedTemp = part.temperature;
            if (initializationCountdown > 0)
                initializationCountdown--;

            var cryostatResource = part.Resources[resourceName];

            if (cryostatResource != null)
            {
                if (HighLogic.LoadedSceneIsEditor)
                {
                    isDisabledField.guiActiveEditor = true;
                    return;
                }

                isDisabledField.guiActive = requiresPower;

                bool coolingIsRelevant = cryostatResource.amount > 0.0000001 && (boilOffRate > 0 || requiresPower);

                powerStatusStrField.guiActive = showPower && requiresPower && coolingIsRelevant;
                boiloffStrField.guiActive = showBoiloff && boiloff > 0.00001;
                externalTemperatureField.guiActive = showTemp && coolingIsRelevant;

                if (!coolingIsRelevant)
                {
                    currentPowerReq = 0;
                    return;
                }

                double atmosphereModifier = convectionMod == -1 ? 0 : convectionMod + part.atmDensity / (convectionMod + 1);

                externalTemperature = part.temperature;
                if (Double.IsNaN(externalTemperature) || Double.IsInfinity(externalTemperature))
                {
                    part.temperature = part.skinTemperature;
                    externalTemperature = part.skinTemperature;
                }

                var temperatureModifier = Math.Max(0, externalTemperature - boilOffTemp) / 300;

                environmentFactor = atmosphereModifier * temperatureModifier;

                if (powerReqKW > 0)
                {
                    currentPowerReq = powerReqKW * 0.2 * environmentFactor * powerReqMult;
                    UpdatePowerStatusString();
                }
                else
                    currentPowerReq = 0;

                environmentBoiloff = environmentFactor * boilOffMultiplier * boilOffBase;
            }
            else
            {
                boiloffStrField.guiActive = false;
                powerStatusStrField.guiActive = false;

                if (HighLogic.LoadedSceneIsEditor)
                {
                    isDisabledField.guiActiveEditor = false;
                }
                else
                {
                    isDisabledField.guiActive = false;
                }
            }
        }

        private void UpdatePowerStatusString()
        {
            powerStatusStr = PluginHelper.GetFormattedPowerString(receivedPowerKW) +
                " / " + PluginHelper.GetFormattedPowerString(currentPowerReq);
        }

        public override string GetInfo()
        {
            double envMod = ((convectionMod <= -1.0) ? 0.0 : convectionMod + 1.0 /
                (convectionMod + 1.0)) * Math.Max(0.0, 300.0 - boilOffTemp) / 300.0;
            return
                $"{resourceName} @ {boilOffTemp:F1} K\nPower Requirements: {powerReqKW * 0.2 * powerReqMult * envMod:F1} KW";
        }

        public ResourcePriorityValue ResourceProcessPriority() => ResourcePriorityValue.Second;

        public void KITFixedUpdate(IResourceManager resMan)
        {
            var cryostat_resource = part.Resources[resourceName];
            if (cryostat_resource == null || double.IsPositiveInfinity(currentPowerReq))
            {
                boiloff = 0;
                return;
            }

            if (!isDisabled && currentPowerReq > 0.0)
            {
                receivedPowerKW = resMan.ConsumeResource(ResourceName.ElectricCharge, currentPowerReq);
            }
            else
                receivedPowerKW = 0;

            bool hasExtraBoiloff = initializationCountdown == 0 && powerReqKW > 0 && currentPowerReq > 0 && receivedPowerKW < currentPowerReq && previousReceivedPowerKW < previousPowerReq;

            var boiloffReduction = !hasExtraBoiloff ? boilOffRate : boilOffRate + (boilOffAddition * (1 - receivedPowerKW / currentPowerReq));

            boiloff = CheatOptions.IgnoreMaxTemperature || boiloffReduction <= 0 ? 0 : boiloffReduction * environmentBoiloff;

            if (boiloff > 1e-10)
            {
                var boilOffAmount = boiloff;

                cryostat_resource.amount = Math.Max(0, cryostat_resource.amount - boilOffAmount);

                boiloffStr = boiloff.ToString("0.0000000") + " L/s " + cryostat_resource.resourceName;

                if (hasExtraBoiloff && part.vessel.isActiveVessel && !warningShown)
                {
                    warningShown = true;
                    var message = Localizer.Format("#LOC_KSPIE_ModuleCryostat_Postmsg", boiloffStr);//"Warning: " +  + " Boiloff"
                    Debug.LogWarning("[KSPI]: FNModuleCryostat: " + message);
                    ScreenMessages.PostScreenMessage(message, 5, ScreenMessageStyle.UPPER_CENTER);
                }
            }
            else
            {
                warningShown = false;
                boiloffStr = "0.0000000 L/s " + cryostat_resource.resourceName;
            }

            previousPowerReq = currentPowerReq;
            previousReceivedPowerKW = receivedPowerKW;
        }

        public string KITPartName() => $"{resourceGUIName} {Localizer.Format("#LOC_KSPIE_ModuleCryostat_Cryostat")}"; //Cryostat
    }
}

*/
