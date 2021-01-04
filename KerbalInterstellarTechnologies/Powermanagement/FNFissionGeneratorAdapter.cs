using KIT.Resources;
using KIT.ResourceScheduler;
using System;
using UnityEngine;

namespace KIT.Powermanagement
{
    [KSPModule("Near Future Fission Generator Adapter")]
    class FNFissionGeneratorAdapter : PartModule, IKITModule
    {
        [KSPField(groupName = FNGenerator.GROUP, groupDisplayName = FNGenerator.GROUP_TITLE, isPersistant = false, guiActiveEditor = false, guiActive = true, guiName = "#LOC_KSPIE_NFFAdapter_Currentpower", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit", guiFormat = "F5")]//Generator current power
        public double megaJouleGeneratorPowerSupply;
        [KSPField(groupName = FNGenerator.GROUP, groupDisplayName = FNGenerator.GROUP_TITLE, isPersistant = false, guiActive = true, guiName = "#LOC_KSPIE_NFFAdapter_Efficiency")]//Efficiency
        public string efficiency;

        [KSPField]
        public float wasteHeatMultiplier = 0.01f;

        private PartModule _moduleGenerator;
        private BaseField _fieldStatus;
        private BaseField _fieldGenerated;
        private BaseField _fieldEfficiency;
        private BaseField _fieldMax;

        public override void OnStart(StartState state)
        {
            try
            {
                if (state == StartState.Editor) return;

                if (part.Modules.Contains("FissionGenerator"))
                {
                    _moduleGenerator = part.Modules["FissionGenerator"];
                    _fieldStatus = _moduleGenerator.Fields["Status"];
                    _fieldGenerated = _moduleGenerator.Fields["CurrentGeneration"];
                    _fieldEfficiency = _moduleGenerator.Fields["Efficiency"];
                    _fieldMax = _moduleGenerator.Fields["PowerGeneration"];
                }

                if (_moduleGenerator == null) return;

                // resources_to_supply = new string[] { ResourceSettings.Config.ElectricPowerInMegawatt, ResourceSettings.Config.WasteHeatInMegawatt };
                base.OnStart(state);
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: Exception in FNFissionGeneratorAdapter.OnStart " + e.Message);
                throw;
            }
        }

        public bool ModuleConfiguration(out int priority, out bool supplierOnly, out bool hasLocalResources)
        {
            priority = 1;
            supplierOnly = false;
            hasLocalResources = false;

            return true;
        }

        public void KITFixedUpdate(IResourceManager resMan)
        {
            // TODO: do we just want to generate the wasteheat for this part?
            // TODO: does NFE generate it's own heat.

            /*
            if (!HighLogic.LoadedSceneIsFlight) return;
            if (_moduleGenerator == null || _fieldStatus == null || _fieldGenerated == null) return;

            bool status = _fieldStatus.GetValue<bool>(_moduleGenerator);

            float generatorRate = status ? _fieldGenerated.GetValue<float>(_moduleGenerator) : 0;
            float generatorMax = _fieldMax.GetValue<float>(_moduleGenerator);
            float generatorEfficiency = _fieldEfficiency.GetValue<float>(_moduleGenerator);

            efficiency = generatorEfficiency.ToString("P2");

            //extract power otherwise we end up with double power
            resMan.ConsumeResource(ResourceName.ElectricCharge, generatorRate);

            double megajoulesRate = generatorRate / GameConstants.ecPerMJ;
            double maxMegajoulesRate = generatorMax / GameConstants.ecPerMJ;

            megaJouleGeneratorPowerSupply = supplyFNResourcePerSecondWithMax(megajoulesRate, maxMegajoulesRate, ResourceSettings.Config.ElectricPowerInMegawatt);

            if (CheatOptions.IgnoreMaxTemperature) return;

            double maxWasteheat = generatorEfficiency > 0.0 ? maxMegajoulesRate * (1.0 / generatorEfficiency - 1.0) : maxMegajoulesRate;
            double throttledWasteheat = generatorEfficiency > 0.0 ? megajoulesRate * (1.0 / generatorEfficiency - 1.0) : megajoulesRate;
            supplyFNResourcePerSecondWithMax(throttledWasteheat, maxWasteheat, ResourceSettings.Config.WasteHeatInMegawatt);
            */
        }

        public string KITPartName() => part.partInfo.title;
    }
}
