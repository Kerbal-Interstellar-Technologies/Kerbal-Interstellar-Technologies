﻿using System;
using KIT.Constants;
using KIT.Resources;
using KIT.ResourceScheduler;
using KSP.Localization;
using UnityEngine;

namespace KIT.Powermanagement
{

    /*
    
    With no Megajoules present, this should no longer be needed.

    [KSPModule("Generator Adapter")]
    class FNGeneratorAdapterVariable : PartModule, IKITMod
    {
        [KSPField(groupName = FNGenerator.GROUP, groupDisplayName = FNGenerator.GROUP_TITLE, isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "#LOC_KSPIE_FNGeneratorAdapter_Powerinput", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F4")]//Power input
        public double powerGeneratorPowerInput;
        [KSPField(groupName = FNGenerator.GROUP, groupDisplayName = FNGenerator.GROUP_TITLE, isPersistant = true, guiActiveEditor = false, guiActive = true, guiName = "#LOC_KSPIE_FNGeneratorAdapter_Poweroutput", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F4")]//Power output
        public double powerGeneratorPowerOutput;

        [KSPField(isPersistant = true)]
        private double initialInputAmount;
        [KSPField(isPersistant = true)]
        private double initialOutputAmount;

        [KSPField]
        public bool showDisplayStatus = true;
        [KSPField]
        public bool showEfficiency = true;
        [KSPField]
        public bool showPowerInput = true;
        [KSPField]
        public bool showPowerOuput = true;
        [KSPField]
        public bool offlineProcessing = false;
        [KSPField]
        public int index = 0;
        [KSPField]
        public double maximumPowerGeneration = 0;
        [KSPField]
        public double currentMegajoulesDemand;
        [KSPField]
        public double currentMegajoulesSupply;
        [KSPField]
        public double inputDivider = 0;
        [KSPField]
        public double inputRate = 0;
        [KSPField]
        public double outputRate = 0;
        [KSPField]
        public double inputAmount;
        [KSPField]
        public double inputMaxAmount;

        [KSPField]
        private double generatorOutputRateInElectricCharge;

        private ModuleGenerator moduleGenerator;

        private ModuleResource mockInputResource;
        private ModuleResource moduleInputResource;
        private ModuleResource moduleOutputResource;

        private BaseField efficiencyField;
        private BaseField displayStatusField;
        private BaseField powerGeneratorPowerInputField;
        private BaseField powerGeneratorPowerOutputField;
        private BaseField moduleGeneratorEfficienctBaseField;

        private ResourceType outputType = 0;
        private ResourceType inputType = 0;

        private double generatorInputRate;

        private bool active;

        public override void OnStart(StartState state)
        {
            try
            {
                if (state == StartState.Editor) return;

                //InitializePartModule();

                var modules = part.FindModulesImplementing<ModuleGenerator>();

                moduleGenerator = modules.Count > index ? modules[index] : null;

                if (moduleGenerator == null) return;

                string[] resourcesToSupply = { ResourceSettings.Config.ElectricPowerInMegawatt };
                // this.resources_to_supply = resourcesToSupply;
                base.OnStart(state);

                outputType = ResourceType.other;
                inputType = ResourceType.other;

                foreach (ModuleResource moduleResource in moduleGenerator.resHandler.inputResources)
                {
                    if (moduleResource.name == ResourceSettings.Config.ElectricPowerInMegawatt)
                        inputType = ResourceType.megajoule;
                    else if (moduleResource.name == ResourceSettings.Config.ElectricPowerInKilowatt)
                        inputType = ResourceType.electricCharge;

                    if (inputType != ResourceType.other)
                    {
                        moduleInputResource = moduleResource;

                        if (inputRate != 0)
                            moduleInputResource.rate = inputRate;

                        initialInputAmount = moduleInputResource.rate;

                        break;
                    }
                }

                foreach (ModuleResource moduleResource in moduleGenerator.resHandler.outputResources)
                {
                    // assuming only one of those two is present
                    if (moduleResource.name == ResourceSettings.Config.ElectricPowerInMegawatt)
                        outputType = ResourceType.megajoule;
                    else if (moduleResource.name == ResourceSettings.Config.ElectricPowerInKilowatt)
                        outputType = ResourceType.electricCharge;

                    if (outputType != ResourceType.other)
                    {
                        mockInputResource = new ModuleResource();
                        mockInputResource.name = moduleResource.name;
                        mockInputResource.id = moduleResource.name.GetHashCode();
                        moduleGenerator.resHandler.inputResources.Add(mockInputResource);

                        moduleOutputResource = moduleResource;

                        if (outputRate != 0)
                            moduleOutputResource.rate = outputRate;

                        initialOutputAmount = moduleOutputResource.rate;

                        moduleGeneratorEfficienctBaseField = moduleGenerator.Fields["efficiency"];
                        if (moduleGeneratorEfficienctBaseField != null)
                        {
                            moduleGeneratorEfficienctBaseField.guiActive = false;
                            moduleGeneratorEfficienctBaseField.guiActiveEditor = false;
                        }

                        break;
                    }
                }
                efficiencyField = moduleGenerator.Fields["efficiency"];
                displayStatusField = moduleGenerator.Fields["displayStatus"];

                efficiencyField.guiActive = showEfficiency;
                displayStatusField.guiActive = showDisplayStatus;

                powerGeneratorPowerInputField = Fields["powerGeneratorPowerInput"];
                powerGeneratorPowerOutputField = Fields["powerGeneratorPowerOutput"];

                if (index > 0)
                {
                    powerGeneratorPowerInputField.guiName = powerGeneratorPowerInputField.guiName + " " + (index + 1);
                    powerGeneratorPowerOutputField.guiName = powerGeneratorPowerOutputField.guiName + " " + (index + 1);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: Exception in FNGeneratorAdapter.OnStart " + e.ToString());
                throw;
            }
        }

        /// <summary>
        /// Is called by KSP while the part is active
        /// </summary>
        public override void OnUpdate()
        {
            if (moduleGenerator == null)
                return;

            powerGeneratorPowerInputField.guiActive = moduleInputResource != null && showPowerInput;
            powerGeneratorPowerOutputField.guiActive = moduleOutputResource != null && showPowerOuput;
        }

        // TODO I think these next two functions are basically wrong / out of date, but I'm not sure at the moment. Will check later.
        public override void OnFixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight) return;
            if (moduleGenerator == null) return;

            active = true;
            base.OnFixedUpdate();
        }


        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight) return;

            if (moduleGenerator == null) return;

            if (!active)
                base.OnFixedUpdate();
        }

        public ResourcePriorityValue ResourceProcessPriority() => maximumPowerGeneration == 0 ? ResourcePriorityValue.First : ResourcePriorityValue.Second;

        public void KITFixedUpdate(IResourceManager resMan)
        {

            powerGeneratorPowerOutput = 0;
            powerGeneratorPowerInput = 0;

            if (moduleGenerator == null) return;

            if (outputType == ResourceType.other) return;

            if (!moduleGenerator.generatorIsActive && !moduleGenerator.isAlwaysActive)
            {
                mockInputResource.rate = 0;
                // supplyFNResourcePerSecondWithMax(0, 0, ResourceSettings.Config.ElectricPowerInMegawatt);
                return;
            }

            if (maximumPowerGeneration != 0)
            {
                currentMegajoulesDemand = Math.Max(0, 0);
                // TODO - current demand. GetCurrentUnfilledResourceDemand(ResourceSettings.Config.ElectricPowerInMegawatt));
                currentMegajoulesSupply = Math.Min(currentMegajoulesDemand, maximumPowerGeneration);
            }

            if (moduleInputResource != null)
            {
                moduleInputResource.rate = currentMegajoulesSupply > 0 ? currentMegajoulesSupply : initialInputAmount;

                part.GetConnectedResourceTotals(moduleInputResource.id, out inputAmount, out inputMaxAmount);

                var availableRatio = Math.Min(1, inputAmount / (moduleInputResource.rate * fixedDeltaTime));

                currentMegajoulesSupply *= availableRatio;
                moduleInputResource.rate *= availableRatio;

                generatorInputRate = moduleInputResource.rate;
                powerGeneratorPowerInput = inputType == ResourceType.megajoule ?
                    generatorInputRate : generatorInputRate / GameConstants.ecPerMJ;
            }

            if (moduleOutputResource != null)
            {
                generatorOutputRateInElectricCharge = maximumPowerGeneration > 0
                    ? currentMegajoulesSupply * (outputType == ResourceType.megajoule ? 1 : GameConstants.ecPerMJ)
                    : initialOutputAmount;

                if (maximumPowerGeneration > 0)
                    moduleOutputResource.rate = 1 + generatorOutputRateInElectricCharge;
                else
                    moduleOutputResource.rate = 1 + generatorOutputRateInElectricCharge;

                mockInputResource.rate = generatorOutputRateInElectricCharge;

                double generatorSupplyInMegajoules = outputType == ResourceType.megajoule ?
                    generatorOutputRateInElectricCharge : generatorOutputRateInElectricCharge / GameConstants.ecPerMJ;

                powerGeneratorPowerOutput = supplyFNResourcePerSecondWithMax(generatorSupplyInMegajoules, generatorSupplyInMegajoules, ResourceSettings.Config.ElectricPowerInMegawatt);
            }

        }

        public string KITPartName() => part.partInfo.title;
    }
    */
}
