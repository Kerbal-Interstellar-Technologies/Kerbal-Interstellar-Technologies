using KSP.Localization;

namespace KIT.BeamedPower
{
    [KSPModule("Beamed Power Bandwidth Converter")]//#LOC_KSPIE_BeamedPowerBandwidthConverter
    class BandwidthConverter
    {
        [KSPField(groupName = BeamedPowerReceiver.Group, groupDisplayName = BeamedPowerReceiver.GroupTitle, isPersistant = false, guiActiveEditor = false, guiActive = false)]
        public string bandwidthName = Localizer.Format("#LOC_KSPIE_BandwidthConverter_missing");//"missing"
        [KSPField(groupName = BeamedPowerReceiver.Group, isPersistant = true, guiActiveEditor = false, guiActive = false, guiFormat = "F9", guiUnits = " m")]
        public double targetWavelength;

        [KSPField(groupName = BeamedPowerReceiver.Group, guiActiveEditor = false, guiActive = false, guiFormat = "F9", guiUnits = " m")]
        public double minimumWavelength = 0.001;
        [KSPField(groupName = BeamedPowerReceiver.Group, guiActiveEditor = false, guiActive = false, guiFormat = "F9", guiUnits = " m")]
        public double maximumWavelength = 1;

        [KSPField(groupName = BeamedPowerReceiver.Group, guiActiveEditor = false, guiActive = false)]
        public int AvailableTechLevel = -1;

        [KSPField(groupName = BeamedPowerReceiver.Group, guiActiveEditor = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double efficiencyPercentage0 = 45;
        [KSPField(groupName = BeamedPowerReceiver.Group, guiActiveEditor = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double electricEfficiencyPercentage0;
        [KSPField(groupName = BeamedPowerReceiver.Group, guiActiveEditor = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double thermalEfficiencyPercentage0;
        [KSPField(groupName = BeamedPowerReceiver.Group, guiActiveEditor = false, guiActive = false)]
        public string techRequirement0 = "";

        [KSPField(groupName = BeamedPowerReceiver.Group, guiActiveEditor = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double efficiencyPercentage1 = 45;
        [KSPField(groupName = BeamedPowerReceiver.Group, guiActiveEditor = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double electricEfficiencyPercentage1;
        [KSPField(groupName = BeamedPowerReceiver.Group, guiActiveEditor = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double thermalEfficiencyPercentage1;
        [KSPField(groupName = BeamedPowerReceiver.Group, guiActiveEditor = false, guiActive = false)]
        public string techRequirement1 = "";

        [KSPField(groupName = BeamedPowerReceiver.Group, guiActiveEditor = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double efficiencyPercentage2 = 45;
        [KSPField(groupName = BeamedPowerReceiver.Group, guiActiveEditor = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double electricEfficiencyPercentage2;
        [KSPField(groupName = BeamedPowerReceiver.Group, guiActiveEditor = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double thermalEfficiencyPercentage2;
        [KSPField(groupName = BeamedPowerReceiver.Group, guiActiveEditor = false, guiActive = false)]
        public string techRequirement2 = "";

        [KSPField(groupName = BeamedPowerReceiver.Group, isPersistant = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double efficiencyPercentage3 = 45;
        [KSPField(groupName = BeamedPowerReceiver.Group, isPersistant = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double electricEfficiencyPercentage3;
        [KSPField(groupName = BeamedPowerReceiver.Group, isPersistant = false, guiActive = false, guiFormat = "F0", guiUnits = "%")]
        public double thermalEfficiencyPercentage3;
        [KSPField(groupName = BeamedPowerReceiver.Group, isPersistant = false, guiActive = false)]
        public string techRequirement3 = "";

        public bool isValid;

        public BandwidthConverter() { }

        public BandwidthConverter(ConfigNode node, string partTitle)
        {
            var original = bandwidthName;

            node.TryGetValue(nameof(bandwidthName), ref bandwidthName);
            node.TryGetValue(nameof(targetWavelength), ref targetWavelength);
            node.TryGetValue(nameof(minimumWavelength), ref minimumWavelength);
            node.TryGetValue(nameof(maximumWavelength), ref maximumWavelength);
            node.TryGetValue(nameof(AvailableTechLevel), ref AvailableTechLevel);

            node.TryGetValue(nameof(efficiencyPercentage0), ref efficiencyPercentage0);
            node.TryGetValue(nameof(electricEfficiencyPercentage0), ref electricEfficiencyPercentage0);
            node.TryGetValue(nameof(thermalEfficiencyPercentage0), ref thermalEfficiencyPercentage0);
            node.TryGetValue(nameof(techRequirement0), ref techRequirement0);

            node.TryGetValue(nameof(efficiencyPercentage1), ref efficiencyPercentage1);
            node.TryGetValue(nameof(electricEfficiencyPercentage1), ref electricEfficiencyPercentage1);
            node.TryGetValue(nameof(thermalEfficiencyPercentage1), ref thermalEfficiencyPercentage1);
            node.TryGetValue(nameof(techRequirement1), ref techRequirement1);

            node.TryGetValue(nameof(efficiencyPercentage2), ref efficiencyPercentage2);
            node.TryGetValue(nameof(electricEfficiencyPercentage2), ref electricEfficiencyPercentage2);
            node.TryGetValue(nameof(thermalEfficiencyPercentage2), ref thermalEfficiencyPercentage2);
            node.TryGetValue(nameof(techRequirement2), ref techRequirement2);

            node.TryGetValue(nameof(efficiencyPercentage3), ref efficiencyPercentage3);
            node.TryGetValue(nameof(electricEfficiencyPercentage3), ref electricEfficiencyPercentage3);
            node.TryGetValue(nameof(thermalEfficiencyPercentage3), ref thermalEfficiencyPercentage3);
            node.TryGetValue(nameof(techRequirement3), ref techRequirement3);

            Initialize();

            switch(AvailableTechLevel)
            {
                case -1:
                case -2:
                    return;
                case 0:
                    techRequirement3 = techRequirement2 = techRequirement1 = string.Empty;
                    break;
                case 1:
                    techRequirement3 = techRequirement2 = string.Empty;
                    break;
                case 2:
                    techRequirement3 = string.Empty;
                    break;
            }

            isValid = (original != bandwidthName);
        }

        public void Save(ConfigNode node)
        {
            ConfigNode myself = new ConfigNode("BandwidthConverter");

            myself.AddValue(nameof(bandwidthName), bandwidthName);
            myself.AddValue(nameof(targetWavelength), targetWavelength);
            myself.AddValue(nameof(minimumWavelength), minimumWavelength);
            myself.AddValue(nameof(maximumWavelength), maximumWavelength);
            myself.AddValue(nameof(AvailableTechLevel), AvailableTechLevel);

            myself.AddValue(nameof(efficiencyPercentage0), efficiencyPercentage0);
            myself.AddValue(nameof(electricEfficiencyPercentage0), electricEfficiencyPercentage0);
            myself.AddValue(nameof(thermalEfficiencyPercentage0), thermalEfficiencyPercentage0);
            myself.AddValue(nameof(techRequirement0), techRequirement0);

            myself.AddValue(nameof(efficiencyPercentage1), efficiencyPercentage1);
            myself.AddValue(nameof(electricEfficiencyPercentage1), electricEfficiencyPercentage1);
            myself.AddValue(nameof(thermalEfficiencyPercentage1), thermalEfficiencyPercentage1);
            myself.AddValue(nameof(techRequirement1), techRequirement1);

            myself.AddValue(nameof(efficiencyPercentage2), efficiencyPercentage2);
            myself.AddValue(nameof(electricEfficiencyPercentage2), electricEfficiencyPercentage2);
            myself.AddValue(nameof(thermalEfficiencyPercentage2), thermalEfficiencyPercentage2);
            myself.AddValue(nameof(techRequirement2), techRequirement2);

            myself.AddValue(nameof(efficiencyPercentage3), efficiencyPercentage3);
            myself.AddValue(nameof(electricEfficiencyPercentage3), electricEfficiencyPercentage3);
            myself.AddValue(nameof(thermalEfficiencyPercentage3), thermalEfficiencyPercentage3);
            myself.AddValue(nameof(techRequirement3), techRequirement3);

            node.AddNode(myself);
        }

        public void Initialize()
        {
            if (AvailableTechLevel > 0) return;
            AvailableTechLevel = -2;

            if (PluginHelper.HasTechRequirementAndNotEmpty(techRequirement3))
                AvailableTechLevel = 3;
            else if (PluginHelper.HasTechRequirementAndNotEmpty(techRequirement2))
                AvailableTechLevel = 2;
            else if (PluginHelper.HasTechRequirementAndNotEmpty(techRequirement1))
                AvailableTechLevel = 1;
            else if (PluginHelper.HasTechRequirementOrEmpty(techRequirement0))
                AvailableTechLevel = 0;
        }

        public double MaxElectricEfficiencyPercentage
        {
            get
            {
                if (AvailableTechLevel == 0)
                    return electricEfficiencyPercentage0 > 0 ? electricEfficiencyPercentage0 : efficiencyPercentage0;
                else if (AvailableTechLevel == 1)
                    return electricEfficiencyPercentage1 > 0 ? electricEfficiencyPercentage1 : efficiencyPercentage1;
                else if (AvailableTechLevel == 2)
                    return electricEfficiencyPercentage2 > 0 ? electricEfficiencyPercentage2 : efficiencyPercentage2;
                else if (AvailableTechLevel == 3)
                    return electricEfficiencyPercentage3 > 0 ? electricEfficiencyPercentage3 : efficiencyPercentage3;
                else
                    return 0;
            }
        }

        public double MaxThermalEfficiencyPercentage
        {
            get
            {
                if (AvailableTechLevel == 0)
                    return thermalEfficiencyPercentage0 > 0 ? thermalEfficiencyPercentage0 : efficiencyPercentage0;
                else if (AvailableTechLevel == 1)
                    return thermalEfficiencyPercentage1 > 0 ? thermalEfficiencyPercentage1 : efficiencyPercentage1;
                else if (AvailableTechLevel == 2)
                    return thermalEfficiencyPercentage2 > 0 ? thermalEfficiencyPercentage2 : efficiencyPercentage2;
                else if (AvailableTechLevel == 3)
                    return thermalEfficiencyPercentage3 > 0 ? thermalEfficiencyPercentage3 : efficiencyPercentage3;
                else
                    return 0;
            }
        }

        public double MaxEfficiencyPercentage
        {
            get
            {
                if (AvailableTechLevel == 0)
                    return efficiencyPercentage0 > 0 ? efficiencyPercentage0 : thermalEfficiencyPercentage0;
                else if (AvailableTechLevel == 1)
                    return efficiencyPercentage1 > 0 ? efficiencyPercentage1 : thermalEfficiencyPercentage1;
                else if (AvailableTechLevel == 2)
                    return efficiencyPercentage2 > 0 ? efficiencyPercentage2 : thermalEfficiencyPercentage2;
                else if (AvailableTechLevel == 3)
                    return efficiencyPercentage3 > 0 ? efficiencyPercentage3 : thermalEfficiencyPercentage3;
                else
                    return 0;
            }
        }

        public double TargetWavelength
        {
            get
            {
                if (targetWavelength == 0)
                    targetWavelength = (minimumWavelength + maximumWavelength) / 2;

                return targetWavelength;
            }
        }

        public /* override */ string GetInfo()
        {
            var info = StringBuilderCache.Acquire();

            info.AppendLine("<size=10>");
            info.Append(Localizer.Format("#LOC_KSPIE_BandwidthConverter_InfoName")).Append(": ").AppendLine(bandwidthName);//Name
            info.Append(Localizer.Format("#LOC_KSPIE_BandwidthConverter_Bandwidthstart")).Append(": ").Append(minimumWavelength).AppendLine(" m");//Bandwidth start
            info.Append(Localizer.Format("#LOC_KSPIE_BandwidthConverter_Bandwidthend")).Append(": ").Append(maximumWavelength).AppendLine(" m");//Bandwidth end

            if (!string.IsNullOrEmpty(techRequirement0))
            {
                info.Append(Localizer.Format("#LOC_KSPIE_BandwidthConverter_Mk1technode")).AppendLine(": ");
                info.AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(techRequirement0)));//Mk1 technode
                info.Append(Localizer.Format("#LOC_KSPIE_BandwidthConverter_Mk1efficiency")).Append(": ");
                info.Append(efficiencyPercentage0.ToString("F0")).AppendLine("%");//Mk1 efficiency
            }
            if (!string.IsNullOrEmpty(techRequirement1))
            {
                info.Append(Localizer.Format("#LOC_KSPIE_BandwidthConverter_Mk2technode")).AppendLine(": ");
                info.AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(techRequirement1)));//Mk2 technode
                info.Append(Localizer.Format("#LOC_KSPIE_BandwidthConverter_Mk2efficiency")).Append(": ");
                info.Append(efficiencyPercentage1.ToString("F0")).AppendLine("%");//Mk2 efficiency
            }
            if (!string.IsNullOrEmpty(techRequirement2))
            {
                info.Append(Localizer.Format("#LOC_KSPIE_BandwidthConverter_Mk3technode")).AppendLine(": ");
                info.AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(techRequirement2)));//Mk3 technode
                info.Append(Localizer.Format("#LOC_KSPIE_BandwidthConverter_Mk3efficiency")).Append(": ");
                info.Append(efficiencyPercentage2.ToString("F0")).AppendLine("%");//Mk3 efficiency
            }
            if (!string.IsNullOrEmpty(techRequirement3))
            {
                info.Append(Localizer.Format("#LOC_KSPIE_BandwidthConverter_Mk4technode")).AppendLine(": ");
                info.AppendLine(Localizer.Format(PluginHelper.GetTechTitleById(techRequirement3)));//Mk4 technode
                info.Append(Localizer.Format("#LOC_KSPIE_BandwidthConverter_Mk4efficiency")).Append(": ");
                info.Append(efficiencyPercentage3.ToString("F0")).AppendLine("%");//Mk4 efficiency
            }
            info.AppendLine("</size>");

            return info.ToStringAndRelease();
        }
    }
}
