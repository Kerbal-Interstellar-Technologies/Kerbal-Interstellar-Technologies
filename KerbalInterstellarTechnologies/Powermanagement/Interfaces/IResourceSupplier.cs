using System;

namespace KIT.PowerManagement.Interfaces
{
    public interface IResourceSupplier
    {
        Guid Id { get; }

        string GetResourceManagerDisplayName();

    }
}
