using System;
using System.Collections.Generic;
using System.Linq;
using KIT.Resources;
using KIT.ResourceScheduler;
using KIT.Wasteheat;

namespace KIT.PowerManagement
{
    [KSPModule("Flat Thermal Power Generator")]
    class FNFlatThermalPowerGenerator : FNThermalPowerGenerator
    {
    }


    [KSPModule("Thermal Power Generator")]
    class FNThermalPowerGenerator : PartModule, IKITModule, IKITVariableSupplier
    {
        //Configuration
        [KSPField] public double maximumPowerCapacity = 0.02; // 20 Kw
        [KSPField] public double maxConversionEfficiency = 0.5; // 50%
        [KSPField] public double requiredTemperatureRatio = 0.1; // 50%
        [KSPField] public double hotColdBathRatioExponent = 0.5;

        //GUI
        [KSPField(groupName = FNGenerator.Group, groupDisplayName = FNGenerator.GroupTitle, guiActive = false, guiName = "Maximum Power Supply", guiFormat = "F2", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit")]
        public double maximumPowerSupplyInMegaWatt;
        [KSPField(groupName = FNGenerator.Group, guiActive = true, guiName = "Maximum Power Supply", guiFormat = "F2")]
        public string maximumPowerSupply;
        [KSPField(groupName = FNGenerator.Group, guiActive = false, guiName = "Current Power Supply", guiFormat = "F2", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit")]
        public double currentPowerSupplyInMegaWatt;
        [KSPField(groupName = FNGenerator.Group, guiActive = true, guiName = "Current Power Supply", guiFormat = "F2")]
        public string currentPowerSupply;
        [KSPField(groupName = FNGenerator.Group, guiActive = true, guiName = "Hot Bath Temperature", guiFormat = "F0", guiUnits = " K")]
        public double hotBathTemperature;
        [KSPField(groupName = FNGenerator.Group, guiActive = true, guiName = "Cold Bath Temperature", guiFormat = "F0", guiUnits = " K")]
        public double radiatorTemperature;

        // reference types
        private List<Part> _stackAttachedParts;
        private double _timeWarpModifer;
        private double _spaceTemperature;
        private double _hotColdBathRatio;
        private double _thermalConversionEfficiency;

        public Part Part => part;


        public override void OnStart(StartState state)
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
            maximumPowerSupply = PluginHelper.GetFormattedPowerString(maximumPowerSupplyInMegaWatt);
            currentPowerSupply = PluginHelper.GetFormattedPowerString(currentPowerSupplyInMegaWatt);
        }

        public override string GetInfo()
        {
            return "Maximum Power: " + PluginHelper.GetFormattedPowerString(maximumPowerCapacity) + "<br>Requires radiators to work.<br>";
        }

        public ModuleConfigurationFlags ModuleConfiguration() => ModuleConfigurationFlags.Third;

        public void KITFixedUpdate(IResourceManager resMan)
        {

            _spaceTemperature = FlightIntegrator.ActiveVesselFI == null ? 4 : FlightIntegrator.ActiveVesselFI.backgroundRadiationTemp;
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
            _timeWarpModifer = PluginHelper.GetTimeWarpModifer();

            _hotColdBathRatio = 1 - Math.Min(1, radiatorTemperature / hotBathTemperature);
            _thermalConversionEfficiency = maxConversionEfficiency * _hotColdBathRatio;

            maximumPowerSupplyInMegaWatt = _hotColdBathRatio > requiredTemperatureRatio
                ? _thermalConversionEfficiency * maximumPowerCapacity * (1 / maxConversionEfficiency)
                : _thermalConversionEfficiency * maximumPowerCapacity * (1 / maxConversionEfficiency) *
                  Math.Pow(_hotColdBathRatio * (1 / requiredTemperatureRatio), hotColdBathRatioExponent);
        }

        public string KITPartName() => part.partInfo.title;

        public ResourceName[] ResourcesProvided() => new[] { ResourceName.ElectricCharge };

        public bool ProvideResource(IResourceManager resMan, ResourceName resource, double requestedAmount)
        {
            var tmp = Math.Min(requestedAmount, maximumPowerSupplyInMegaWatt - currentPowerSupplyInMegaWatt);
            if (tmp == 0) return false;

            currentPowerSupplyInMegaWatt += tmp;

            var wasteheatInMegaJoules = (1 - _thermalConversionEfficiency) * tmp;

            resMan.Produce(ResourceName.WasteHeat, wasteheatInMegaJoules);
            resMan.Produce(ResourceName.ElectricCharge, tmp);

            return true;
        }
    }
}
