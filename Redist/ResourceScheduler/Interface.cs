using KIT.Interfaces;
using KIT.Resources;
using System.Collections.Generic;

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

    /// <summary>
    /// This interface is passed to the part modules in IKITModule.KITFixedUpdate. It allows the 
    /// production and consumption of resources, and access to some wrapper variables to avoid global
    /// variable access.
    /// </summary>
    public interface IResourceManager
    {
        /// <summary>
        /// Consumes resources from the resource pool. It automatically converts your request to per-seconds,
        /// so you do not need to account for that yourself.
        /// </summary>
        /// <param name="resource">Resource Name</param>
        /// <param name="wanted">Requested amount of resource to consume per second</param>
        /// <returns>Amount of resource that there is to consume (per second)</returns>
        double Consume(ResourceName resource, double wanted);

        /// <summary>
        /// Adds resources to the resource pool. It automatically converts your request to per-seconds,
        /// so you do not need to account for that yourself.
        /// </summary>
        /// <param name="resource">Resource Name</param>
        /// <param name="amount">Amount of resource to produce per second</param>
        /// <param name="max">The maximum that this part can produce of this resource, in total. If -1, then it will add up all the times the resource has been produced.</param>
        double Produce(ResourceName resource, double amount, double max = -1);

        /// <summary>
        /// Capacity information returns all you need to know about available resources and it's associated storage information
        /// </summary>
        /// <param name="resourceIdentifier">Resource to get information from</param>
        /// <param name="maxCapacity">Total storage information available</param>
        /// <param name="spareCapacity">Spare capacity for this resource</param>
        /// <param name="currentCapacity">How much of this resource we have</param>
        /// <param name="fillFraction">Fraction of how full it is</param>
        /// <returns>Returns false if there is no resource of this type defined, otherwise true</returns>
        bool CapacityInformation(
            ResourceName resourceIdentifier, out double maxCapacity,
            out double spareCapacity, out double currentCapacity, out double fillFraction);

        /// <summary>Provides access to the (equivalent) of TimeWarp.fixedDeltaTime.</summary>
        /// <remarks>
        /// The resource interface automatically converts everything to per-second values for you. You only need
        /// access to this in special cases.
        /// </remarks>
        double FixedDeltaTime();

        /// <summary>
        /// ScaledConsumptionProduction scales resource production by the lowest ratio of consumed resources
        /// </summary>
        /// <param name="consumeResources">Resources to consume in this request</param>
        /// <param name="produceResources">Resources to produce in this request</param>
        /// <param name="minimumRatio">Minimum ratio needed, if calculated ratio is less than this, 0 is returned</param>
        /// <param name="flags">Flags controlling the function</param>
        /// <returns>Ratio used, between 0 and 1</returns>

        double ScaledConsumptionProduction(
            List<KeyValuePair<ResourceName, double>> consumeResources,
            List<KeyValuePair<ResourceName, double>> produceResources,
            double minimumRatio = 0,
            ConsumptionProductionFlags flags = ConsumptionProductionFlags.Empty
        );

        /// <summary>
        /// CurrentCapacity returns current amount of the indicated resource available
        /// </summary>
        /// <param name="resourceIdentifier">Resource to get information about</param>
        /// <returns>Amount</returns>
        double CurrentCapacity(ResourceName resourceIdentifier);
        
        /// <summary>
        /// FillFraction returns how full the storage is of the indicated resource
        /// </summary>
        /// <param name="resourceIdentifier">Resource to get information about</param>
        /// <returns>How full, between 0 to 1.</returns>
        double FillFraction(ResourceName resourceIdentifier);
        
        /// <summary>
        /// SpareCapacity returns how much storage space is available
        /// </summary>
        /// <param name="resourceIdentifier">Resource to get information about</param>
        /// <returns></returns>
        double SpareCapacity(ResourceName resourceIdentifier);
        
        /// <summary>
        /// MaxCapacity returns the maximum storage space available for this resource
        /// </summary>
        /// <param name="resourceIdentifier"></param>
        /// <returns></returns>
        double MaxCapacity(ResourceName resourceIdentifier);

        /// <summary>
        /// Access to the cheat options that the code should use.  Use this instead of the global variable CheatOptions
        /// </summary>
        /// <returns>The ICheatOptions associated with this resource manager.</returns>
        ICheatOptions CheatOptions();

        IResourceProduction ProductionStats(ResourceName resourceIdentifier);

    }

    public enum ConsumptionProductionFlags
    {
        Empty = 0,
        DoNotOverFill = 1,
    }


    public interface IResourceScheduler
    {
        void ExecuteKITModules(double deltaTime, ResourceData resourceData);
    }

    public interface IVesselResources
    {
        // since the last call.
        bool VesselModified();
        void VesselKITModules(ref List<IKITModule> moduleList, ref Dictionary<ResourceName, List<IKITVariableSupplier>> variableSupplierModules);
        void OnKITProcessingFinished(IResourceManager resourceManager);
    }

    /// <summary>
    /// IKITModule defines an interface used by Kerbal Interstellar Technologies PartModules. Through this interface,
    /// various functions are called on the PartModule from the KIT Resource Manager Vessel Module.
    /// </summary>
    public interface IKITModule
    {
        /// <summary>
        /// Tells the resource scheduler about this module, and how it should be ran, when it should be accessed
        /// </summary>
        /// <param name="priority">number between 1-5 indicating when the resource should be initialized</param>
        /// <param name="supplierOnly">does this module only supply resources (solar panel, etc?)</param>
        /// <param name="hasLocalResources">does this module use resources specific to that part? (radioactive, no flow, etc) The ResourceManager interface will give access to those if true</param>
        /// <returns>true if the module is ready to run, false otherwise</returns>
        bool ModuleConfiguration(out int priority, out bool supplierOnly, out bool hasLocalResources);

        /// <summary>
        /// KITFixedUpdate replaces the FixedUpdate function for Kerbal Interstellar Technologies PartModules. 
        /// </summary>
        /// <param name="resMan">Interface to the resource manager, and options that should be used when the code is running</param>
        void KITFixedUpdate(IResourceManager resMan);

        /// <summary>
        /// String to identify the part
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
        /// <param name="resMan">resource manager object</param>
        /// <param name="resource"></param>
        /// <param name="requestedAmount"></param>
        /// <returns>bool - indicating if this module should be called again this KITFixedUpdate() cycle.</returns>
        bool ProvideResource(IResourceManager resMan, ResourceName resource, double requestedAmount);
    }

    // ReSharper disable once InconsistentNaming
    public interface IDCElectricalSystem
    {
        double UnallocatedElectricChargeConsumption();
    }
}
