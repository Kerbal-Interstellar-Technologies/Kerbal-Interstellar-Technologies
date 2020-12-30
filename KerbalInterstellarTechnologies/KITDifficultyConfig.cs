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
        public bool AllowDestructiveEngines;

        [GameParameters.CustomParameterUI("#LOC_KIT_DifficultyConfig_PreventRadioactiveDecay", toolTip = "#LOC_KIT_DifficultyConfig_PreventRadioactiveDecay_tip")]
        public bool PreventRadioactiveDecay;

        [GameParameters.CustomParameterUI("#LOC_KIT_DifficultyConfig_ReconfigureAntennas", toolTip = "#LOC_KIT_DifficultyConfig_ReconfigureAntennas_tip")]
        public bool ReconfigureAntennas;

        [GameParameters.CustomFloatParameterUI("#LOC_KIT_DifficultyConfig_MinimumRTGOutput", toolTip = "#LOC_KIT_DifficultyConfig_MinimumRTGOutput_tip", minValue = 0.0f, maxValue = 0.10f, displayFormat = "F2", asPercentage = true)]
        public float MinimumRtgOutput;

        [GameParameters.CustomParameterUI("#LOC_KIT_DifficultyConfig_ExtendedReactorControl", toolTip = "#LOC_KIT_DifficultyConfig_ExtendedReactorControl_tip")]
        public bool ExtendedReactorControl;

        public override void SetDifficultyPreset(Preset preset)
        {
            switch (preset)
            {
                case Preset.Easy:
                    AllowDestructiveEngines = ReconfigureAntennas = true;
                    PreventRadioactiveDecay = true;
                    ExtendedReactorControl = false;
                    MinimumRtgOutput = 0.1f;
                    break;
                case Preset.Moderate:
                    ReconfigureAntennas = true;
                    MinimumRtgOutput = 0.05f;
                    ExtendedReactorControl = AllowDestructiveEngines = PreventRadioactiveDecay = false;
                    break;
                case Preset.Normal:
                    AllowDestructiveEngines = ReconfigureAntennas = PreventRadioactiveDecay = false;
                    ExtendedReactorControl = false;
                    MinimumRtgOutput = 0;
                    break;
                case Preset.Hard:
                    AllowDestructiveEngines = ReconfigureAntennas = PreventRadioactiveDecay = false;
                    ExtendedReactorControl = false;
                    MinimumRtgOutput = 0;
                    break;
            }
        }

    }

}
