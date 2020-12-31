namespace KIT.Interfaces
{
    public interface IEngineNozzle
    {
        double GetNozzleFlowRate();
        float CurrentThrottle { get; }
        bool RequiresChargedPower { get; }
    }
}
