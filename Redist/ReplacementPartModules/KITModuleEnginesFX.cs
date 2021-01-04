using KIT.Resources;
using KIT.ResourceScheduler;

namespace KIT.ReplacementPartModules
{ 
    class KITModuleEnginesFX : ModuleEnginesFX, IKITModule
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
        public bool ModuleConfiguration(out int priority, out bool supplierOnly, out bool hasLocalResources)
        {
            priority = 4;
            supplierOnly = false;
            hasLocalResources = true;

            return true;
        }

    }
}
