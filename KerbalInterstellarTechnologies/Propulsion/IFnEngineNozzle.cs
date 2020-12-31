
using KIT.Interfaces;

namespace KIT.Propulsion
{
    public interface IFnEngineNozzle : IEngineNozzle
    {
        Part part { get; }
        bool RequiresPlasmaHeat { get; }
        bool RequiresThermalHeat { get; }
		bool PropellantAbsorbsNeutrons { get; }
    }
}
