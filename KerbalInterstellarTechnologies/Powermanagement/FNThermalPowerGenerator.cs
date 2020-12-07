using System;
using System.Collections.Generic;
using System.Linq;
using KIT.Extensions;
using KIT.Resources;
using KIT.ResourceScheduler;
using KIT.Wasteheat;

namespace KIT.Powermanagement
{
    [KSPModule("Flat Thermal Power Generator")]
    class FNFlatThermalPowerGenerator : FNThermalPowerGenerator
    {
    }


    [KSPModule("Thermal Power Generator")]
    class FNThermalPowerGenerator : PartModule, IKITMod
    {
        //Configuration
        [KSPField] public double maximumPowerCapacity = 0.02; // 20 Kw
        [KSPField] public double maxConversionEfficiency = 0.5; // 50%
        [KSPField] public double requiredTemperatureRatio = 0.1; // 50%
        [KSPField] public double hotColdBathRatioExponent = 0.5;

        //GUI
        [KSPField(groupName = FNGenerator.GROUP, groupDisplayName = FNGenerator.GROUP_TITLE, guiActive = false, guiName = "Maximum Power Supply", guiFormat = "F2", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit")]
        public double maximumPowerSupplyInMegaWatt;
        [KSPField(groupName = FNGenerator.GROUP, guiActive = true, guiName = "Maximum Power Supply", guiFormat = "F2")]
        public string maximumPowerSupply;
        [KSPField(groupName = FNGenerator.GROUP, guiActive = false, guiName = "Current Power Supply", guiFormat = "F2", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit")]
        public double currentPowerSupplyInMegaWatt;
        [KSPField(groupName = FNGenerator.GROUP, guiActive = true, guiName = "Current Power Supply", guiFormat = "F2")]
        public string currentPowerSupply;
        [KSPField(groupName = FNGenerator.GROUP, guiActive = true, guiName = "Hot Bath Temperature", guiFormat = "F0", guiUnits = " K")]
        public double hotBathTemperature;
        [KSPField(groupName = FNGenerator.GROUP, guiActive = true, guiName = "Cold Bath Temperature", guiFormat = "F0", guiUnits = " K")]
        public double radiatorTemperature;

        // reference types
        private List<Part> _stackAttachedParts;
        private double timeWarpModifer;
        private double spaceTemperature;

        public Part Part
        {
            get { return this.part; }
        }


        public override void OnStart(PartModule.StartState state)
        {
            String[] resources = { ResourceSettings.Config.ElectricPowerInMegawatt, ResourceSettings.Config.WasteHeatInMegawatt };
            //this.resources_to_supply = resources;
            base.OnStart(state);

            if (state == StartState.Editor)
                return;
            // look for attached parts
            _stackAttachedParts = part.attachNodes
                .Where(atn => atn.attachedPart != null && atn.nodeType == AttachNode.NodeType.Stack)
                .Select(m => m.attachedPart).ToList();

            part.force_activate();
        }

        public override void OnUpdate()
        {
            maximumPowerSupply = PluginHelper.getFormattedPowerString(maximumPowerSupplyInMegaWatt);
            currentPowerSupply = PluginHelper.getFormattedPowerString(currentPowerSupplyInMegaWatt);
        }

        public override string GetInfo()
        {
            return "Maximum Power: " + PluginHelper.getFormattedPowerString(maximumPowerCapacity);
        }

        public ResourcePriorityValue ResourceProcessPriority() => ResourcePriorityValue.First;

        public void KITFixedUpdate(IResourceManager resMan)
        {
            var hasRadiators = FNRadiator.HasRadiatorsForVessel(vessel);

            // get radiator temperature
            if (hasRadiators)
                radiatorTemperature = FNRadiator.GetAverageRadiatorTemperatureForVessel(vessel);
            else if (!_stackAttachedParts.Any())
                return;
            else
                radiatorTemperature = _stackAttachedParts.Min(m => m.temperature);

            if (double.IsNaN(radiatorTemperature))
                return;

            timeWarpModifer = PluginHelper.GetTimeWarpModifer();
            spaceTemperature = FlightIntegrator.ActiveVesselFI == null ? 4 : FlightIntegrator.ActiveVesselFI.backgroundRadiationTemp;

            hotBathTemperature = Math.Max(4, Math.Max(part.temperature, part.skinTemperature));

            var hotColdBathRatio = 1 - Math.Min(1, radiatorTemperature / hotBathTemperature);

            var thermalConversionEfficiency = maxConversionEfficiency * hotColdBathRatio;

            maximumPowerSupplyInMegaWatt = hotColdBathRatio > requiredTemperatureRatio
                ? thermalConversionEfficiency * maximumPowerCapacity * (1 / maxConversionEfficiency)
                : thermalConversionEfficiency * maximumPowerCapacity * (1 / maxConversionEfficiency) *
                  Math.Pow(hotColdBathRatio * (1 / requiredTemperatureRatio), hotColdBathRatioExponent);

            // TODO 
            // var currentUnfilledResourceDemand = Math.Max(0, GetCurrentUnfilledResourceDemand(ResourceSettings.Config.ElectricPowerInMegawatt));
            double currentUnfilledResourceDemand = 10000;

            var requiredRatio = Math.Min(1, currentUnfilledResourceDemand / maximumPowerSupplyInMegaWatt);

            currentPowerSupplyInMegaWatt = requiredRatio * maximumPowerSupplyInMegaWatt;

            var wasteheatInMegaJoules = (1 - thermalConversionEfficiency) * currentPowerSupplyInMegaWatt;

            if (hasRadiators)
                resMan.ProduceResource(ResourceName.WasteHeat, wasteheatInMegaJoules * 1000);
            // TODO supplyFNResourcePerSecondWithMax(maximumPowerSupplyInMegaWatt, wasteheatInMegaJoules, ResourceSettings.Config.WasteHeatInMegawatt);

            // generate thermal power
            resMan.ProduceResource(ResourceName.ElectricCharge, currentPowerSupplyInMegaWatt * 1000);
            
        }

        public string KITPartName() => part.partInfo.title;
    }
}
