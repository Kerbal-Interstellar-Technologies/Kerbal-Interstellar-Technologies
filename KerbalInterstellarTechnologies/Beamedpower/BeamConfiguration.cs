using UnityEngine;

namespace KIT.Beamedpower
{
    [KSPModule("#LOC_KSPIE_BeamConfiguration_MouduleName")]//Beamed Power Transmit Configuration
    public class BeamConfiguration
    {
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamConfiguration_BeamWaveName")]//Wavelength Name
        public string beamWaveName = "";
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamConfiguration_Wavelength", guiFormat = "F9", guiUnits = " m")]//Wavelength
        public double wavelength = 0.003189281;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamConfiguration_AtmosphericAbsorption", guiFormat = "F4", guiUnits = "%")]//Atmospheric Absorption
        public double atmosphericAbsorptionPercentage = 1;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamConfiguration_WaterAbsorption", guiFormat = "F4", guiUnits = "%")]//Water Absorption
        public double waterAbsorptionPercentage = 1;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamConfiguration_EfficiencyPercentage0", guiFormat = "F0", guiUnits = "%")]//Power to Beam Efficiency 0
        public double efficiencyPercentage0;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamConfiguration_TechRequirement0")]//Tech Requirement 0
        public string techRequirement0 = "";
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamConfiguration_EfficiencyPercentage1", guiFormat = "F0", guiUnits = "%")]//Power to Beam Efficiency 1
        public double efficiencyPercentage1;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamConfiguration_TechRequirement1")]//Tech Requirement 1
        public string techRequirement1 = "";
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamConfiguration_EfficiencyPercentage2", guiFormat = "F0", guiUnits = "%")]//Power to Beam Efficiency 2
        public double efficiencyPercentage2;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamConfiguration_TechRequirement2")]//Tech Requirement 2
        public string techRequirement2 = "";
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamConfiguration_EfficiencyPercentage3", guiFormat = "F0", guiUnits = "%")]//Power to Beam Efficiency 3
        public double efficiencyPercentage3;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamConfiguration_TechRequirement3")]//Tech Requirement 3
        public string techRequirement3 = "";

        public bool IsValid;
        public int TechLevel = -1;

        public BeamConfiguration() { }
        public BeamConfiguration(ConfigNode node, string partTitle)
        {
            if (! node.TryGetValue(nameof(beamWaveName), ref beamWaveName))
            {
                Debug.Log($"[BeamConfiguration ({partTitle} / {node.name})] Unable to get beamWaveName, returning");
                return;
            }

            node.TryGetValue(nameof(wavelength), ref wavelength);
            node.TryGetValue(nameof(atmosphericAbsorptionPercentage), ref atmosphericAbsorptionPercentage);
            node.TryGetValue(nameof(waterAbsorptionPercentage), ref waterAbsorptionPercentage);
            node.TryGetValue(nameof(efficiencyPercentage0), ref efficiencyPercentage0);
            node.TryGetValue(nameof(techRequirement0), ref techRequirement0);
            node.TryGetValue(nameof(efficiencyPercentage1), ref efficiencyPercentage1);
            node.TryGetValue(nameof(techRequirement1), ref techRequirement1);
            node.TryGetValue(nameof(efficiencyPercentage2), ref efficiencyPercentage2);
            node.TryGetValue(nameof(techRequirement2), ref techRequirement2);
            node.TryGetValue(nameof(efficiencyPercentage3), ref efficiencyPercentage3);
            node.TryGetValue(nameof(techRequirement3), ref techRequirement3);
            
            if (! PluginHelper.HasTechRequirementAndNotEmpty(techRequirement3))
            {
                techRequirement3 = "";
                efficiencyPercentage3 = 0;
            } else TechLevel++;

            if (! PluginHelper.HasTechRequirementAndNotEmpty(techRequirement2))
            {
                techRequirement3 = "";
                efficiencyPercentage2 = 0;
            } else TechLevel++;

            if (! PluginHelper.HasTechRequirementAndNotEmpty(techRequirement1))
            {
                techRequirement1 = "";
                efficiencyPercentage1 = 0;
            } else TechLevel++;

            if (! PluginHelper.HasTechRequirementAndNotEmpty(techRequirement0))
            {
                techRequirement0 = "";
                efficiencyPercentage0 = 0;
            } else TechLevel++;

            if (TechLevel != -1) IsValid = true;
        }

        public void Save(ConfigNode node)
        {
            var myself = new ConfigNode("BeamConfiguration");
            myself.AddValue(nameof(beamWaveName), beamWaveName);
            myself.AddValue(nameof(wavelength), wavelength);
            myself.AddValue(nameof(atmosphericAbsorptionPercentage), atmosphericAbsorptionPercentage);
            myself.AddValue(nameof(waterAbsorptionPercentage), waterAbsorptionPercentage);
            myself.AddValue(nameof(efficiencyPercentage0), efficiencyPercentage0);
            myself.AddValue(nameof(techRequirement0), techRequirement0);
            myself.AddValue(nameof(efficiencyPercentage1), efficiencyPercentage1);
            myself.AddValue(nameof(techRequirement1), techRequirement1);
            myself.AddValue(nameof(efficiencyPercentage2), efficiencyPercentage2);
            myself.AddValue(nameof(techRequirement2), techRequirement2);
            myself.AddValue(nameof(efficiencyPercentage3), efficiencyPercentage3);
            myself.AddValue(nameof(techRequirement3), techRequirement3);

            node.AddNode(myself);
        }
    }

}
