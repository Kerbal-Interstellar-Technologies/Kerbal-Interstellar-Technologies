using KIT.Redist;

namespace KIT.Powermanagement.Interfaces
{
    interface IFNElectricPowerGeneratorSource: IElectricPowerGeneratorSource
    {
        double GetHotBathTemperature(double coldBathTemperature);

        double RawGeneratorSourcePower { get; }

        double MaxEfficiency { get; }
    }
}
