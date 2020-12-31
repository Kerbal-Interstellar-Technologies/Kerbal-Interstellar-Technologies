namespace KIT.Interfaces
{
    public interface IPowerSource : IThermalReceiver
    {
        Part Part { get; }

        int ProviderPowerPriority { get; }

        /// <summary>
        /// // The absolute maximum amount of power the thermalSource can possibly produce
        /// </summary>
        double RawMaximumPower { get; }

        bool SupportMHD { get; }

        double PowerRatio { get; }

        double RawTotalPowerProduced { get; }

        /// <summary>
        /// Influences the Mass in Electric Generator
        /// </summary>
        double ThermalProcessingModifier { get; }

        int SupportedPropellantAtoms { get; }

        int SupportedPropellantTypes { get; }

        bool FullPowerForNonNeutronAbsorbents { get; }

        double ProducedThermalHeat { get; }

        double ProducedChargedPower { get; }

        //double RequestedThermalHeat { get; set; }

        double ProducedWasteHeat { get; }

        double PowerBufferBonus { get; }

        double StableMaximumReactorPower { get; }

        double MinimumThrottle { get; }

        double MaximumPower { get; }

        double MinimumPower { get; }

        double ChargedPowerRatio { get; }

        double NormalisedMaximumPower { get; }

        double MaximumThermalPower { get; }

        double MaximumChargedPower { get; }

        double CoreTemperature { get; }

        double HotBathTemperature { get; }

        bool IsSelfContained { get; }

        bool IsActive { get; }

        bool IsVolatileSource { get; }

        double Radius { get; }

        bool IsNuclear { get; }

        void EnableIfPossible();

        bool ShouldScaleDownJetISP();

        double GetCoreTempAtRadiatorTemp(double radTemp);

        double GetThermalPowerAtTemp(double temp);

        bool IsThermalSource { get; }

        double ConsumedFuelFixed { get; }

        double ThermalPropulsionWasteHeatModifier { get; }

        double ThermalPropulsionEfficiency { get; }
        double PlasmaPropulsionEfficiency { get; }
        double ChargedParticlePropulsionEfficiency { get; }

        double ThermalEnergyEfficiency { get; }
        double PlasmaEnergyEfficiency { get; }
        double ChargedParticleEnergyEfficiency { get; }

        double EfficiencyConnectedThermalEnergyGenerator { get; }

        double EfficiencyConnectedChargedEnergyGenerator { get; }

        double ReactorSpeedMult { get; }

        IElectricPowerGeneratorSource ConnectedThermalElectricGenerator { get; set; }

        IElectricPowerGeneratorSource ConnectedChargedParticleElectricGenerator { get; set; }

        void NotifyActiveThermalEnergyGenerator(double efficiency, double powerRatio);

        void NotifyActiveChargedEnergyGenerator(double efficiency, double powerRatio);

        bool ShouldApplyBalance(ElectricGeneratorType generatorType);

        void ConnectWithEngine(IEngineNozzle engine);

        void DisconnectWithEngine(IEngineNozzle engine);
    }
}
