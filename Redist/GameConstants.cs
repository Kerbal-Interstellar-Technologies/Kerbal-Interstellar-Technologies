namespace KIT
{
    public static class GameConstants
    {
        public const double KerbinSunDistance = 13599840256;
        public const double AverageKerbinSolarFlux = 1409.285;  // this seems to be the average flux at Kerbin just above the atmosphere (from my tests)
        public const double AvogadroConstant = 6.022140857e+23; // number of atoms in 1 mol
        public const double BasePowerConsumption = 5;
        public const double BaseAMFPowerConsumption = 5000;
        public const double BaseCentriPowerConsumption = 43.5;
        public const double BaseELCPowerConsumption = 40;
        public const double BaseAnthraquiononePowerConsumption = 5;
        public const double BasePechineyUgineKuhlmannPowerConsumption = 5;
        public const double BaseHaberProcessPowerConsumption = 20;
        public const double BaseUraniumAmmonolysisPowerConsumption = 12;
        public const double AnthraquinoneEnergyPerTon = 1834.321;
        public const double HaberProcessEnergyPerTon = 34200;
        public const double WaterElectrolysisEnergyPerTon = 18159;
        public const double AluminiumElectrolysisEnergyPerTon = 35485.714;
        public const double PechineyUgineKuhlmannEnergyPerTon = 1021;
        public const double EarthAtmospherePressureAtSeaLevel = 101.325;
        public const double EarthRadius = 6371000;
        public const double AluminiumElectrolysisMassRatio = 1.5;
        public const double DeuteriumAbudance = 0.00015625;
        public const double DeuteriumTimescale = 0.0016667;
        public const double BaseReprocessingRate = 400;
        public const double BaseScienceRate = 0.1;
        public const double BaseUraniumAmmonolysisRate = 0.0002383381;   
        public const double MicrowaveAngle = 3.64773814E-10;
        public const double MicrowaveDishEfficiency = 0.85;
        public const double MicrowaveAlpha = 0.00399201596806387225548902195609;
        public const double MicrowaveBeta = 1 - MicrowaveAlpha;
        public const double StefanConst = 5.67036713e-8;  // Stefan-Botzman const for watts / m2
        public const double RadConstH = 1000;
        public const double AtmosphericNonPrecooledLimit = 740;
        public const double InitialAlcubierreMegajoulesRequired = 100;
        public const double TelescopePerformanceTimescale = 2.1964508725630127431022388314009e-8;
        public const double TelescopeBaseScience = 0.1666667;
        public const double TelescopeGLensScience = 5;
        public const double SpeedOfLight = 299792458;
        public const double LightSpeedSquared = SpeedOfLight * SpeedOfLight;
        public const double TritiumBreedRate = 428244.662271 / 0.17639 / 1.25;  // 0.222678566;
        public const double HeliumBoilOffFraction = 1.667794e-8;
        public const double AmmoniaHydrogenFractionByMass = 0.17647;
        public const double KerbinYearInDays = 426.08;
        public const double ElectronCharge = 1.602176565e-19;
        public const double AtomicMassUnit =  1.660538921e-27;
        public const double StandardGravity = 9.80665;
        public const double DilutionFactor = 15000.0;
        public const double IspCoreTemperatureMultiplier = 22.371670613;
        public const double BaseThrustPowerMultiplier = 2000;
        public const double HighCoreTempThrustMultiplier = 1600;
        public const float MaxThermalNozzleIsp = 2997.13f;
        public const double EngineHeatProduction = 1000;
        public const double AirflowHeatMultiplier = 1;

        public const int KerbinHoursDay = 6;
        public const int SecondsInHour = 3600;
        public const int KerbinDaySeconds = SecondsInHour * KerbinHoursDay;

        public const int DefaultSupportedPropellantAtoms = 511; // any atom type
        public const int DefaultSupportedPropellantTypes = 127; // any molecular type
    }
}
