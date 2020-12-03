
namespace KIT.Propulsion
{
    public interface IFNEngineNoozle : IEngineNoozle
    {
        Part part { get; }
        bool RequiresPlasmaHeat { get; }
        bool RequiresThermalHeat { get; }
		bool PropellantAbsorbsNeutrons { get; }
    }
}
