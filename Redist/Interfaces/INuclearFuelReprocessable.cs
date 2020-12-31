namespace KIT.Interfaces
{
    public interface INuclearFuelReprocessable
    {
        double WasteToReprocess { get; }

        double ReprocessFuel(double rate);
    }
}
