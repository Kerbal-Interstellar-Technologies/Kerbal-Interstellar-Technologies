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
    class FNThermalPowerGenerator : PartModule, IKITMod, IKITVariableSupplier
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
        private double hotColdBathRatio;
        private double thermalConversionEfficiency;

        public Part Part
        {
            get { return this.part; }
        }


        public override void OnStart(PartModule.StartState state)
        {
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
            return "Maximum Power: " + PluginHelper.getFormattedPowerString(maximumPowerCapacity) + "<br>Requires radiators to work.<br>";
        }

        public ResourcePriorityValue ResourceProcessPriority() => ResourcePriorityValue.First;

        public void KITFixedUpdate(IResourceManager resMan)
        {

            spaceTemperature = FlightIntegrator.ActiveVesselFI == null ? 4 : FlightIntegrator.ActiveVesselFI.backgroundRadiationTemp;
            hotBathTemperature = Math.Max(4, Math.Max(part.temperature, part.skinTemperature));

            var hasRadiators = FNRadiator.HasRadiatorsForVessel(vessel);

            if (!hasRadiators)
            {
                radiatorTemperature = 0;
                maximumPowerSupplyInMegaWatt = 0;
                currentPowerSupplyInMegaWatt = 0;
                return;
            }

            radiatorTemperature = FNRadiator.GetAverageRadiatorTemperatureForVessel(vessel);
            timeWarpModifer = PluginHelper.GetTimeWarpModifer();

            hotColdBathRatio = 1 - Math.Min(1, radiatorTemperature / hotBathTemperature);
            thermalConversionEfficiency = maxConversionEfficiency * hotColdBathRatio;

            maximumPowerSupplyInMegaWatt = hotColdBathRatio > requiredTemperatureRatio
                ? thermalConversionEfficiency * maximumPowerCapacity * (1 / maxConversionEfficiency)
                : thermalConversionEfficiency * maximumPowerCapacity * (1 / maxConversionEfficiency) *
                  Math.Pow(hotColdBathRatio * (1 / requiredTemperatureRatio), hotColdBathRatioExponent);
        }

        public string KITPartName() => part.partInfo.title;

        public ResourceName[] ResourcesProvided() => new ResourceName[] { ResourceName.ElectricCharge };

        public bool ProvideResource(IResourceManager resMan, ResourceName resource, double requestedAmount)
        {
            var tmp = Math.Min(requestedAmount, maximumPowerSupplyInMegaWatt - currentPowerSupplyInMegaWatt);
            if (tmp == 0) return false;

            currentPowerSupplyInMegaWatt += tmp;

            var wasteheatInMegaJoules = (1 - thermalConversionEfficiency) * tmp;

            resMan.ProduceResource(ResourceName.WasteHeat, wasteheatInMegaJoules);
            resMan.ProduceResource(ResourceName.ElectricCharge, tmp);

            return true;
        }
    }
}
