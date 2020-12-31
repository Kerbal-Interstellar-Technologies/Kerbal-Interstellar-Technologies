using KIT.Interfaces;
using KIT.ResourceScheduler;

namespace KIT.Powermanagement.Interfaces
{
    public interface IFNPowerSource : IPowerSource
    {
        void NotifyActiveThermalEnergyGenerator(double efficiency, double powerRatio, bool isMHD, double mass);

        void NotifyActiveChargedEnergyGenerator(double efficiency, double powerRatio, double mass);

        double EngineHeatProductionMult { get; }

        double PlasmaHeatProductionMult { get; }

        double EngineWasteheatProductionMult { get; }

        double PlasmaWasteheatProductionMult { get; }

        double MinCoolingFactor { get; }

        bool CanProducePower { get; }

        double FuelRatio { get; }

        double MinThermalNozzleTempRequired { get; }

        bool CanUseAllPowerForPlasma { get; }

        bool UsePropellantBaseIsp { get; }

        double CurrentMeVPerChargedProduct { get; }

        bool MayExhaustInAtmosphereHomeworld { get; }

        bool MayExhaustInLowSpaceHomeworld { get; }

        double MagneticNozzlePowerMult { get; }

        void UseProductForPropulsion(IResourceManager resMan, double ratio, double propellantMassPerSecond, PartResourceDefinition resource);

        double RawMaximumPowerForPowerGeneration { get; }

        double MaxCoreTemperature { get; }
    }
}
