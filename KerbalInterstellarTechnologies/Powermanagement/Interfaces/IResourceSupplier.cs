using System;

namespace KIT.Powermanagement.Interfaces
{
    public interface IResourceSupplier
    {
        Guid Id { get; }

        string GetResourceManagerDisplayName();

    }
}
