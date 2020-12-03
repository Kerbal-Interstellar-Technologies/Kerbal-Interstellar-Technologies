using System;
using KIT.Resources;
using UnityEngine;

namespace KIT.Powermanagement
{
    internal class TPResourceManager : ResourceManager
    {
        public TPResourceManager(Guid overmanagerId, PartModule pm) : base(overmanagerId, pm, ResourceSettings.Config.ThermalPowerInMegawatt, FNRESOURCE_FLOWTYPE_EVEN)
        {
            WindowPosition = new Rect(600, 50, LABEL_WIDTH + VALUE_WIDTH + PRIORITY_WIDTH, 50);
        }
    }
}
