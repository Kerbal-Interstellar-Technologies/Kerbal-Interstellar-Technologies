using System;

namespace KIT
{
    public interface IResourceSupplier
    {
        Guid Id { get; }

        string getResourceManagerDisplayName();

    }
}
