using System;

namespace KIT
{
    public interface IResourceSupplier
    {
        Guid Id { get; }

        string getResourceManagerDisplayName();

        double supplyFNResourceFixed(double supply, String resourceName);
    }
}
