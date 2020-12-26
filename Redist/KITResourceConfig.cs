using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GameParameters;

namespace KIT
{
    abstract class KITDifficultyCustomParams : GameParameters.CustomParameterNode
    {
        public override string Section => "Kerbal Interstellar Technologies";
        public override string DisplaySection => "#LOC_KIT_DifficultyConfig_DisplaySection";
        public override GameParameters.GameMode GameMode => GameParameters.GameMode.SANDBOX | GameParameters.GameMode.CAREER | GameParameters.GameMode.SCIENCE;
        public override bool HasPresets => true;
    }

    class KITResourceParams : KITDifficultyCustomParams
    {
        public override string Title => "#LOC_KIT_DifficultyConfig_Resources";
        public override int SectionOrder => 1;

        [GameParameters.CustomParameterUI("#LOC_KIT_DifficultyConfig_RateLimitResourceConsumption", toolTip = "#LOC_KIT_DifficultyConfig_RateLimitResourceConsumption_tip")]
        public bool disableResourceConsumptionRateLimit;

        [GameParameters.CustomFloatParameterUI("#LOC_KIT_DifficultyConfig_EmergencyShutdownTemperaturePercentage", toolTip = "#LOC_KIT_DifficultyConfig_EmergencyShutdownTemperaturePercentage_tip", minValue = 0.90f, maxValue = 1.0f, displayFormat = "F2", asPercentage = true)]
        public float emergencyShutdownTemperaturePercentage;

        public override void SetDifficultyPreset(Preset preset)
        {
            emergencyShutdownTemperaturePercentage = 0.95f;
            disableResourceConsumptionRateLimit = false;

            switch (preset)
            {
                case Preset.Easy:
                case Preset.Moderate:
                case Preset.Normal:
                    break;
                case Preset.Hard:
                    emergencyShutdownTemperaturePercentage = 1f;
                    disableResourceConsumptionRateLimit = true;
                    break;
            }
        }

    }

}
