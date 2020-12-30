namespace KIT
{
    public interface INuclearFuelReprocessable
    {
        double WasteToReprocess { get; }

        double ReprocessFuel(double rate);
    }
}
