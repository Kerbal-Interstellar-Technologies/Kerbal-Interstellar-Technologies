﻿using KIT.Resources;
using KIT.ResourceScheduler;

namespace KIT.ReplacementPartModules
{ 
    class KITModuleEnginesFX : ModuleEnginesFX, IKITMod
    {
        public new void FixedUpdate() { }

        public override  void UpdateThrottle()
        {
            // take into account resMan.fixedDeltaTime
        }
        
        protected new double RequiredPropellantMass(float throttleAmount)
        {
            // take into account resMan.fixedDeltaTime
            return base.RequiredPropellantMass(throttleAmount);
        }

        public override double RequestPropellant(double mass)
        {
            // use resMan.fixedDeltaTime
            // use KIT RM to request resources

            return base.RequestPropellant(mass);
        }

        public void KITFixedUpdate(IResourceManager resMan)
        {
            base.FixedUpdate();
        }

        public string KITPartName() => part.partInfo.title;
        public ResourcePriorityValue ResourceProcessPriority() => ResourcePriorityValue.Fourth;
    }
}
