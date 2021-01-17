namespace KIT.PowerManagement.Interfaces
{
    public interface IResourceSuppliable
    {
        string GetResourceManagerDisplayName();
        int GetPowerPriority();
    }
}
