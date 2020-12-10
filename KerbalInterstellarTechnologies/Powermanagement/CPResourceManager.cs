using System;
using KIT.Resources;
using UnityEngine;

namespace KIT.Powermanagement
{
    internal class CPResourceManager : ResourceManager
    {
        public CPResourceManager(Guid overmanagerId, PartModule pm) : base(overmanagerId, pm, KITResourceSettings.ChargedParticle, FNRESOURCE_FLOWTYPE_EVEN)
        {
            WindowPosition = new Rect(50, 600, LABEL_WIDTH + VALUE_WIDTH + PRIORITY_WIDTH, 50);
        }
    }
}
