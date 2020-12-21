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
        public override int SectionOrder => 1;

        [GameParameters.CustomParameterUI("#LOC_KIT_DifficultyConfig_ExhaustHomeWorld", toolTip = "#LOC_KIT_DifficultyConfig_ExhaustHomeWorld_tip")]
        public bool allowDestructiveEngines;

        [GameParameters.CustomParameterUI("#LOC_KIT_DifficultyConfig_PreventRadioactiveDecay", toolTip = "#LOC_KIT_DifficultyConfig_PreventRadioactiveDecay_tip")]
        public bool preventRadioactiveDecay;

        [GameParameters.CustomParameterUI("#LOC_KIT_DifficultyConfig_ReconfigureAntennas", toolTip = "#LOC_KIT_DifficultyConfig_ReconfigureAntennas_tip")]
        public bool reconfigureAntennas;

        [GameParameters.CustomFloatParameterUI("#LOC_KIT_DifficultyConfig_MinimumRTGOutput", toolTip = "#LOC_KIT_DifficultyConfig_MinimumRTGOutput_tip", minValue = 0.0f, maxValue = 0.10f, displayFormat = "F2", asPercentage = true)]
        public float minimumRTGOutput;

        // Per garoand_ran, toggle on/off reactor without EVA ing
        //   -> perhaps we want to change fuel sources?
        //   -> refuel reactors without an EVA'd kerbal?
        // [GameParameters.CustomParameterUI("#LOC_KIT_DifficultyConfig_ExtendedReactorControl", toolTip = "#LOC_KIT_DifficultyConfig_ExtendedReactorControl_tip")]
        // public bool extendedReactorControl;

        // Per AntaresMC, some engines that can kill by radiation should have a on/off switch
        //   Daedalus engine is on example, Kerbalism helper FNEmitter is another area

        public override void SetDifficultyPreset(Preset preset)
        {
            switch(preset)
            {
                case Preset.Easy:
                    allowDestructiveEngines = reconfigureAntennas = true;
                    preventRadioactiveDecay = true;
                    minimumRTGOutput = 0.1f;
                    break;
                case Preset.Moderate:
                    allowDestructiveEngines = reconfigureAntennas = true;
                    minimumRTGOutput = 0.05f;
                    preventRadioactiveDecay = false;
                    break;
                case Preset.Normal:
                case Preset.Hard:
                    allowDestructiveEngines = reconfigureAntennas = preventRadioactiveDecay = false;
                    minimumRTGOutput = 0;
                    break;
            }
        }

    }

}
