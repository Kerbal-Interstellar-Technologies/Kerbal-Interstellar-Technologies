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

    class KITGamePlayParams : KITDifficultyCustomParams
    {
        public override string Title => "#LOC_KIT_DifficultyConfig_GamePlay";
        public override int SectionOrder => 2;

        [GameParameters.CustomParameterUI("#LOC_KIT_DifficultyConfig_DestructiveEngines", toolTip = "#LOC_KIT_DifficultyConfig_DestructiveEngines_tip")]
        public bool allowDestructiveEngines;

        [GameParameters.CustomParameterUI("#LOC_KIT_DifficultyConfig_PreventRadioactiveDecay", toolTip = "#LOC_KIT_DifficultyConfig_PreventRadioactiveDecay_tip")]
        public bool preventRadioactiveDecay;

        [GameParameters.CustomParameterUI("#LOC_KIT_DifficultyConfig_ReconfigureAntennas", toolTip = "#LOC_KIT_DifficultyConfig_ReconfigureAntennas_tip")]
        public bool reconfigureAntennas;

        [GameParameters.CustomFloatParameterUI("#LOC_KIT_DifficultyConfig_MinimumRTGOutput", toolTip = "#LOC_KIT_DifficultyConfig_MinimumRTGOutput_tip", minValue = 0.0f, maxValue = 0.10f, displayFormat = "F2", asPercentage = true)]
        public float minimumRTGOutput;

        [GameParameters.CustomParameterUI("#LOC_KIT_DifficultyConfig_ExtendedReactorControl", toolTip = "#LOC_KIT_DifficultyConfig_ExtendedReactorControl_tip")]
        public bool extendedReactorControl;

        public override void SetDifficultyPreset(Preset preset)
        {
            switch (preset)
            {
                case Preset.Easy:
                    allowDestructiveEngines = reconfigureAntennas = true;
                    preventRadioactiveDecay = true;
                    extendedReactorControl = false;
                    minimumRTGOutput = 0.1f;
                    break;
                case Preset.Moderate:
                    reconfigureAntennas = true;
                    minimumRTGOutput = 0.05f;
                    extendedReactorControl = allowDestructiveEngines = preventRadioactiveDecay = false;
                    break;
                case Preset.Normal:
                    allowDestructiveEngines = reconfigureAntennas = preventRadioactiveDecay = false;
                    extendedReactorControl = false;
                    minimumRTGOutput = 0;
                    break;
                case Preset.Hard:
                    allowDestructiveEngines = reconfigureAntennas = preventRadioactiveDecay = false;
                    extendedReactorControl = false;
                    minimumRTGOutput = 0;
                    break;
            }
        }

    }

}
