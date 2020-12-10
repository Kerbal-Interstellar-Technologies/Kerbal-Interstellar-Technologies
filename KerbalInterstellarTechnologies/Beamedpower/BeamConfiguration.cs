using UnityEngine;

namespace KIT.Microwave
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
        public double efficiencyPercentage0 = 0;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamConfiguration_TechRequirement0")]//Tech Requirement 0
        public string techRequirement0 = "";
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamConfiguration_EfficiencyPercentage1", guiFormat = "F0", guiUnits = "%")]//Power to Beam Efficiency 1
        public double efficiencyPercentage1 = 0;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamConfiguration_TechRequirement1")]//Tech Requirement 1
        public string techRequirement1 = "";
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamConfiguration_EfficiencyPercentage2", guiFormat = "F0", guiUnits = "%")]//Power to Beam Efficiency 2
        public double efficiencyPercentage2 = 0;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamConfiguration_TechRequirement2")]//Tech Requirement 2
        public string techRequirement2 = "";
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamConfiguration_EfficiencyPercentage3", guiFormat = "F0", guiUnits = "%")]//Power to Beam Efficiency 3
        public double efficiencyPercentage3 = 0;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "#LOC_KSPIE_BeamConfiguration_TechRequirement3")]//Tech Requirement 3
        public string techRequirement3 = "";

        public bool isValid;
        public int techLevel = -1;

        public BeamConfiguration() { }
        public BeamConfiguration(ConfigNode node, string partTitle)
        {
            if (! node.TryGetValue(nameof(beamWaveName), ref this.beamWaveName))
            {
                Debug.Log($"[BeamConfiguration ({partTitle} / {node.name})] Unable to get beamWaveName, returning");
                return;
            }

            node.TryGetValue(nameof(wavelength), ref this.wavelength);
            node.TryGetValue(nameof(atmosphericAbsorptionPercentage), ref this.atmosphericAbsorptionPercentage);
            node.TryGetValue(nameof(waterAbsorptionPercentage), ref this.waterAbsorptionPercentage);
            node.TryGetValue(nameof(efficiencyPercentage0), ref this.efficiencyPercentage0);
            node.TryGetValue(nameof(techRequirement0), ref this.techRequirement0);
            node.TryGetValue(nameof(efficiencyPercentage1), ref this.efficiencyPercentage1);
            node.TryGetValue(nameof(techRequirement1), ref this.techRequirement1);
            node.TryGetValue(nameof(efficiencyPercentage2), ref this.efficiencyPercentage2);
            node.TryGetValue(nameof(techRequirement2), ref this.techRequirement2);
            node.TryGetValue(nameof(efficiencyPercentage3), ref this.efficiencyPercentage3);
            node.TryGetValue(nameof(techRequirement3), ref this.techRequirement3);
            
            if (! PluginHelper.HasTechRequirementAndNotEmpty(this.techRequirement3))
            {
                this.techRequirement3 = "";
                this.efficiencyPercentage3 = 0;
            } else techLevel++;

            if (! PluginHelper.HasTechRequirementAndNotEmpty(this.techRequirement2))
            {
                this.techRequirement3 = "";
                this.efficiencyPercentage2 = 0;
            } else techLevel++;

            if (! PluginHelper.HasTechRequirementAndNotEmpty(this.techRequirement1))
            {
                this.techRequirement1 = "";
                this.efficiencyPercentage1 = 0;
            } else techLevel++;

            if (! PluginHelper.HasTechRequirementAndNotEmpty(this.techRequirement0))
            {
                this.techRequirement0 = "";
                this.efficiencyPercentage0 = 0;
            } else techLevel++;

            if (techLevel != -1) isValid = true;
        }

        public void Save(ConfigNode node)
        {
            var myself = new ConfigNode("BeamConfiguration");
            myself.AddValue(nameof(beamWaveName), this.beamWaveName);
            myself.AddValue(nameof(wavelength), this.wavelength);
            myself.AddValue(nameof(atmosphericAbsorptionPercentage), this.atmosphericAbsorptionPercentage);
            myself.AddValue(nameof(waterAbsorptionPercentage), this.waterAbsorptionPercentage);
            myself.AddValue(nameof(efficiencyPercentage0), this.efficiencyPercentage0);
            myself.AddValue(nameof(techRequirement0), this.techRequirement0);
            myself.AddValue(nameof(efficiencyPercentage1), this.efficiencyPercentage1);
            myself.AddValue(nameof(techRequirement1), this.techRequirement1);
            myself.AddValue(nameof(efficiencyPercentage2), this.efficiencyPercentage2);
            myself.AddValue(nameof(techRequirement2), this.techRequirement2);
            myself.AddValue(nameof(efficiencyPercentage3), this.efficiencyPercentage3);
            myself.AddValue(nameof(techRequirement3), this.techRequirement3);

            node.AddNode(myself);
        }
    }

}
