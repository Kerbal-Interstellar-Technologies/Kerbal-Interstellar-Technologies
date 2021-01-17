using System;
using KIT.Resources;
using KIT.ResourceScheduler;
using KSP.Localization;
using UnityEngine;

namespace KIT.Storage
{
    [KSPModule("Cryostat")]
    class ModuleStorageCryostat: FNModuleCryostat {}

    [KSPModule("Cryostat")]
    class FNModuleCryostat : PartModule, IKITModule
    {
        public const string Group = "FNModuleCryostat";
        public const string GroupTitle = "Interstellar Cryostat";

        // Persistant
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActive = true, guiName = "#LOC_IFS_Cryostat_Cooling"), UI_Toggle(disabledText = "#LOC_IFS_Cryostat_On", enabledText = "#LOC_IFS_Cryostat_Off")]//Cooling--On--Off
        public bool isDisabled;

        [KSPField(isPersistant = true)]
        public double storedTemp;

        // Confiration
        [KSPField] public string resourceName = "";
        [KSPField] public string resourceGUIName = "";
        [KSPField] public double boilOffRate;
        [KSPField] public double powerReqKW;
        [KSPField] public double powerReqMult = 1;
        [KSPField] public double boilOffMultiplier;
        [KSPField] public double boilOffBase = 10000;
        [KSPField] public double boilOffAddition;
        [KSPField] public double boilOffTemp = 20.271;
        [KSPField] public double convectionMod = 1;
        [KSPField] public bool showPower = true;
        [KSPField] public bool showBoiloff = true;
        [KSPField] public bool showTemp = true;
        [KSPField] public bool warningShown;
        [KSPField] public int initializationCountdown = 10;

        //GUI
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = false, guiActive = false, guiName = "#LOC_KSPIE_ModuleCryostat_Power")]//Power
        public string powerStatusStr = string.Empty;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = false, guiActive = false, guiName = "#LOC_KSPIE_ModuleCryostat_Boiloff")]//Boiloff
        public string boiloffStr;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = false, guiActive = false, guiName = "#LOC_KSPIE_ModuleCryostat_Temperature", guiFormat = "F2", guiUnits = " K")]//Temperature
        public double externalTemperature;
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = false, guiActive = false, guiName = "#LOC_KSPIE_ModuleCryostat_Internalboiloff")]//internal boiloff
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

        public override void OnStart(StartState state)
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
                Debug.Log($"{part.ClassName} with {moduleName} is missing an Electric Charge Resource. Please fix.");
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

        public ModuleConfigurationFlags ModuleConfiguration() => ModuleConfigurationFlags.Second;

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
                receivedPowerKW = resMan.Consume(ResourceName.ElectricCharge, currentPowerReq);
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

