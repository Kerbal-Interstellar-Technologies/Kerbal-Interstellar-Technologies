using KIT.Redist;

namespace FNPlugin.Powermanagement
{
    interface IFNElectricPowerGeneratorSource: IElectricPowerGeneratorSource
    {
        double RawGeneratorSourcePower { get; }

        double MaxEfficiency { get; }
    }
}
