using static GameParameters;

namespace KIT
{
    abstract class KITDifficultyCustomParams : CustomParameterNode
    {
        public override string Section => "Kerbal Interstellar Technologies";
        public override string DisplaySection => "#LOC_KIT_DifficultyConfig_DisplaySection";
        public override GameMode GameMode => GameMode.SANDBOX | GameMode.CAREER | GameMode.SCIENCE;
        public override bool HasPresets => true;
    }

    class KITResourceParams : KITDifficultyCustomParams
    {
        public override string Title => "#LOC_KIT_DifficultyConfig_Resources";
        public override int SectionOrder => 1;

        [CustomParameterUI("#LOC_KIT_DifficultyConfig_RateLimitResourceConsumption", toolTip = "#LOC_KIT_DifficultyConfig_RateLimitResourceConsumption_tip")]
        public bool DisableResourceConsumptionRateLimit;

        [CustomFloatParameterUI("#LOC_KIT_DifficultyConfig_EmergencyShutdownTemperaturePercentage", toolTip = "#LOC_KIT_DifficultyConfig_EmergencyShutdownTemperaturePercentage_tip", minValue = 0.90f, maxValue = 1.0f, displayFormat = "F2", asPercentage = true)]
        public float EmergencyShutdownTemperaturePercentage;

        [CustomParameterUI("#LOC_KIT_DifficultyConfig_IgnoreResourceFlow", toolTip = "#LOC_KIT_DifficultyConfig_IgnoreResourceFlow_tip")]
        public bool IgnoreResourceFlowRestrictions;

        public override void SetDifficultyPreset(Preset preset)
        {
            EmergencyShutdownTemperaturePercentage = 0.95f;
            DisableResourceConsumptionRateLimit = false;
            var ignoreResourceFlowRestrictions = false;

            switch (preset)
            {
                case Preset.Easy:
                    ignoreResourceFlowRestrictions = true;
                    break;
                case Preset.Moderate:
                case Preset.Normal:
                    break;
                case Preset.Hard:
                    EmergencyShutdownTemperaturePercentage = 1f;
                    DisableResourceConsumptionRateLimit = true;
                    break;
            }
            
            IgnoreResourceFlowRestrictions = ignoreResourceFlowRestrictions;
        }
    }

}
