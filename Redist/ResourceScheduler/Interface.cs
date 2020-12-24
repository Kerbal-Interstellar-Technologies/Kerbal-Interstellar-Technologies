using KIT.Interfaces;
using KIT.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIT.ResourceScheduler
{
    /// <summary>
    /// ResourceProduction tracks what was supplied in a previous update. Code must assume that the previous entries can be 0
    /// </summary>
    public interface IResourceProduction
    {
        /// <summary>
        /// Does this have previous update data available?
        /// </summary>
        /// <returns>May be false at any time for any reason.</returns>
        bool PreviousDataSupplied();
        /// <summary>
        /// PreviouslyRequested is the total of the resource previously requested.
        /// </summary>
        /// <returns>previouslyRequested is the total of the resource previously requested.</returns>
        double PreviouslyRequested();
        /// <summary>
        /// PreviouslySupplied total of the resource previously supplied.
        /// </summary>
        /// <returns>The amount of the resource that was previously supplied in an update</returns>
        double PreviouslySupplied();
        /// <summary>
        /// PreviousDemandMet indicates if in the last update, we were able to supply all the requested demand of a resource.
        /// </summary>
        /// <returns>
        /// returns true if previouslySupplied >= previouslyRequested, false if all demands were met.
        /// </returns>
        bool PreviousDemandMet();
        /// <summary>
        /// Returns the surplus resource production in the previous update. The minimum will be 0
        /// </summary>
        /// <returns>Math.Max(0, previouslySupplied - previouslyRequested) for the most part.</returns>
        double PreviousSurplus();
        /// <summary>
        /// Returns the unmet demand in the previous update.
        /// </summary>
        /// <returns>Math.Max(0, previouslyRequested - previouslySupplied) for the most part.</returns>
        double PreviousUnmetDemand();

        /// <summary>
        /// CurrentlyRequested is the total so far requested in this Update.
        /// </summary>
        /// <returns>resources requested in this KITFixedUpdate()</returns>
        double CurrentlyRequested();
        /// <summary>
        /// CurrentlySupplied is the total so far that has been supplied in this 
        /// </summary>
        /// <returns></returns>
        double CurrentSupplied();
    }

    /// This interface is passed to the part modules in IKITMod.KITFixedUpdate. It allows the 
    /// production and consumption of resources, and access to some wrapper variables to avoid global
    /// variable access.
    /// </summary>
    public interface IResourceManager
    {
        /// <summary>
        /// Consumes resources from the resource pool. It automatically converts your request to per-seconds,
        /// so you do not need to account for that yourself.
        /// </summary>
        /// <param name="name">Resource Name</param>
        /// <param name="wanted">Requested amount of resource to consume per second</param>
        /// <returns>Amount of resource that there is to consume (per second)</returns>
        double ConsumeResource(ResourceName resource, double wanted);

        /// <summary>
        /// Adds resources to the resource pool. It automatically converts your request to per-seconds,
        /// so you do not need to account for that yourself.
        /// </summary>
        /// <param name="name">Resource Name</param>
        /// <param name="amount">Amount of resource to produce per second</param>
        /// <param name="max">The maximum that this part can produce of this resource, in total. If -1, then it will add up all the times the resource has been produced.</param>
        double ProduceResource(ResourceName resource, double amount, double max = -1);

        /// <summary>
        /// Checks to see how much storage is available for the given resource, returning 0 if there is none.
        /// </summary>
        /// <param name="resourceIdentifier"></param>
        /// <returns></returns>
        double ResourceSpareCapacity(ResourceName resourceIdentifier);

        /// <summary>
        /// ResourceCurrentCapacity returns how much of a resource is available, ship wide.
        /// </summary>
        /// <param name="resourceIdentifier"></param>
        /// <returns></returns>
        double ResourceCurrentCapacity(ResourceName resourceIdentifier);

        /// <summary>
        /// ResourceFillFraction returns how full the resource is, expressed between 0 and 1.
        /// </summary>
        /// <param name="resourceIdentifier"></param>
        /// <returns></returns>
        double ResourceFillFraction(ResourceName resourceIdentifier);

        /// <summary>Provides access to the (equivilient) of TimeWarp.fixedDeltaTime.</summary>
        /// <remarks>
        /// The resource interface automatically converts everything to per-second values for you. You only need
        /// access to this in special cases.
        /// </remarks>
        double FixedDeltaTime();

        /// <summary>
        /// Access to the cheat options that the code should use.  Use this instead of the global variable CheatOptions
        /// </summary>
        /// <returns>The ICheatOptions associated with this resource manager.</returns>
        ICheatOptions CheatOptions();

        IResourceProduction ResourceProductionStats(ResourceName resourceIdentifier);

    }

    public interface IResourceScheduler
    {
        void ExecuteKITModules(double deltaTime, ref Dictionary<ResourceName, double> resourceAmount, ref Dictionary<ResourceName, double> resourceMaxAmount);
    }

    public interface IVesselResources
    {
        // since the last call.
        bool VesselModified();
        void VesselKITModules(ref List<IKITMod> moduleList, ref Dictionary<ResourceName, List<IKITVariableSupplier>> variableSupplierModules);
        void OnKITProcessingFinished(IResourceManager resourceManager);
    }

    /// <summary>
    /// IKITMod defines an interface used by Kerbal Interstellar Technologies PartModules. Through this interface,
    /// various functions are called on the PartModule from the KIT Resource Manager Vessel Module.
    /// </summary>
    public interface IKITMod
    {
        /// <summary>
        /// This is the priority that the module should run at, from 1 to 5. 1 will be ran first, and 5 will be ran last.
        /// </summary>
        /// <returns>Part Priority</returns>
        ResourcePriorityValue ResourceProcessPriority();
        /// <summary>
        /// KITFixedUpdate replaces the FixedUpdate function for Kerbal Interstellar Technologies PartModules. 
        /// </summary>
        /// <param name="resMan">Interface to the resource manager, and options that should be used when the code is running</param>
        void KITFixedUpdate(IResourceManager resMan);
        /// <summary>
        /// 
        /// </summary>
        /// <returns>String to identify the part</returns>
        string KITPartName();
    }

    /// <summary>
    /// Marks a PartModule that can be called as needed to produce resources for the vessel.
    /// </summary>
    public interface IKITVariableSupplier
    {
        /// <summary>
        /// What resources can this module provide on demand?
        /// </summary>
        /// <returns>Returns a list of strings for the resources it can provide</returns>
        ResourceName[] ResourcesProvided();

        /// <summary>
        /// Checks to see if the requested resource can be provided.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="requestedAmount"></param>
        /// <returns>bool - indicating if this module should be called again this KITFixedUpdate() cycle.</returns>
        bool ProvideResource(IResourceManager resMan, ResourceName resource, double requestedAmount);
    }

    public interface IDCElectricalSystem
    {
        double unallocatedElectricChargeConsumption();
    }
}
