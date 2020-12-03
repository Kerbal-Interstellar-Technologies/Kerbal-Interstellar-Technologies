using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KIT
{
    public interface INuclearFuelReprocessable
    {
        double WasteToReprocess { get; }

        double ReprocessFuel(double rate);
    }
}
