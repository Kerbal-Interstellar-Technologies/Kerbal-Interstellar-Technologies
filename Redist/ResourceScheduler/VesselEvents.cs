using UnityEngine;

namespace KIT.ResourceScheduler
{
    /// <summary>
    /// VesselEventData is used to track game events occurring. Once it picks up an event occurring, it tries to
    /// find the corresponding KITResourceManager and lets it know to refresh it's part module cache.
    /// </summary>
    
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    // ReSharper disable once UnusedMember.Global - KSP will initialize and use this class
    public class VesselEventData : MonoBehaviour
    {
        /// <summary>
        /// Initializes the VesselEventData class, and hooks into the GameEvents code.
        /// </summary>
        void Start()
        {
            GameEvents.onVesselGoOnRails.Add(RefreshActiveParts);
            GameEvents.onVesselGoOffRails.Add(RefreshActiveParts);
            GameEvents.onVesselWasModified.Add(RefreshActiveParts);
            GameEvents.onVesselPartCountChanged.Add(RefreshActiveParts);
            GameEvents.onVesselLoaded.Add(RefreshActiveParts);
            GameEvents.onPartDeCouple.Add(RefreshActiveParts);
            GameEvents.onPartDestroyed.Add(RefreshActiveParts);
            GameEvents.onPartPriorityChanged.Add(RefreshActiveParts);
            GameEvents.onPartDie.Add(RefreshActiveParts);
            GameEvents.onPartWillDie.Add(RefreshActiveParts);
            GameEvents.onPartDeCouple.Add(RefreshActiveParts);
            GameEvents.onPartFailure.Add(RefreshActiveParts);
        }

        void OnDestroy()
        {
            GameEvents.onVesselGoOnRails.Remove(RefreshActiveParts);
            GameEvents.onVesselGoOffRails.Remove(RefreshActiveParts);
            GameEvents.onVesselWasModified.Remove(RefreshActiveParts);
            GameEvents.onVesselPartCountChanged.Remove(RefreshActiveParts);
            GameEvents.onVesselLoaded.Remove(RefreshActiveParts);
            GameEvents.onPartDeCouple.Remove(RefreshActiveParts);
            GameEvents.onPartDestroyed.Remove(RefreshActiveParts);
            GameEvents.onPartPriorityChanged.Remove(RefreshActiveParts);
            GameEvents.onPartDie.Remove(RefreshActiveParts);
            GameEvents.onPartWillDie.Remove(RefreshActiveParts);
            GameEvents.onPartDeCouple.Remove(RefreshActiveParts);
            GameEvents.onPartFailure.Remove(RefreshActiveParts);
        }

        // TODO replace with vessel built in, now that we target a newer api version
        private KITResourceVesselModule FindVesselModuleImplementing(Vessel v)
        {
            foreach (var t in v.vesselModules)
            {
                KITResourceVesselModule ret = t as KITResourceVesselModule;
                if (ret != null) return ret;
            }
            return null;
        }

        /// <summary>
        /// Looks up the corresponding KITResourceManager and tells it to refresh its module cache.
        /// </summary>
        /// <param name="data">Part triggering the event</param>
        private void RefreshActiveParts(Part data)
        {
            if (data == null || data.vessel == null) return;
            var resourceMod = FindVesselModuleImplementing(data.vessel);
            if (resourceMod == null) return;
            resourceMod.RefreshEventOccurred = true;
        }
        /// <summary>
        /// /// Looks up the corresponding KITResourceManager and tells it to refresh its module cache.
        /// </summary>
        /// <param name="data">Vessel triggering the event</param>
        private void RefreshActiveParts(Vessel data)
        {
            if (data == null) return;
            var resourceMod = FindVesselModuleImplementing(data);
            if (resourceMod == null) return;
            resourceMod.RefreshEventOccurred = true;
        }
    }
}
