namespace KIT.Interfaces
{   
    /// <summary>
    /// ICheatOptions provides an interface the same as the CheatOptions static global class.
    /// 
    /// This interface is used to ensure that code can be tested without relying on global
    /// variables that would otherwise have to modify global state to ensure they operate as
    /// intended. For release code / production code, use RealCheatOptions.Instance to get a
    /// global instance that uses the CheatOptions static class.
    /// </summary>
    public interface ICheatOptions
    {
        bool PauseOnVesselUnpack { get; }
        bool UnbreakableJoints { get; }
        bool NoCrashDamage { get; }
        bool IgnoreMaxTemperature { get; }
        bool InfinitePropellant { get; }
        bool InfiniteElectricity { get; }
        bool BiomesVisible { get; }
        bool AllowPartClipping { get; }
        bool NonStrictAttachmentOrientation { get; }
        bool IgnoreAgencyMindsetOnContracts { get; }
    }

    /// <summary>
    /// RealCheatOptions is a wrapper class around the global CheatOptions static class, intended for use when
    /// playing the game.
    /// </summary>
    public class RealCheatOptions : ICheatOptions
    {
        public bool PauseOnVesselUnpack => CheatOptions.PauseOnVesselUnpack;
        public bool UnbreakableJoints => CheatOptions.UnbreakableJoints;
        public bool NoCrashDamage => CheatOptions.NoCrashDamage;
        public bool IgnoreMaxTemperature => CheatOptions.IgnoreMaxTemperature;
        public bool InfinitePropellant => CheatOptions.InfinitePropellant;
        public bool InfiniteElectricity => CheatOptions.InfiniteElectricity;
        public bool BiomesVisible => CheatOptions.BiomesVisible;
        public bool AllowPartClipping => CheatOptions.AllowPartClipping;
        public bool NonStrictAttachmentOrientation => CheatOptions.NonStrictAttachmentOrientation;
        public bool IgnoreAgencyMindsetOnContracts => CheatOptions.IgnoreAgencyMindsetOnContracts;

        private static RealCheatOptions _instance;
        public static RealCheatOptions Instance => _instance ?? (_instance = new RealCheatOptions());
    }

    /// <summary>
    /// ConfigurableCheatOptions provides a class that can be easily used for testing code, and does not 
    /// interfere with other code that may be under test at the same time.
    /// </summary>
    public class ConfigurableCheatOptions : ICheatOptions
    {
        public bool PauseOnVesselUnpack { get; set; }
        public bool UnbreakableJoints { get; set; }
        public bool NoCrashDamage { get; set; }
        public bool IgnoreMaxTemperature { get; set; }
        public bool InfinitePropellant { get; set; }
        public bool InfiniteElectricity { get; set; }
        public bool BiomesVisible { get; set; }
        public bool AllowPartClipping { get; set; }
        public bool NonStrictAttachmentOrientation { get; set; }
        public bool IgnoreAgencyMindsetOnContracts { get; set; }
    }

}
