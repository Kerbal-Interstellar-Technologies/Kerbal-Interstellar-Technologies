using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KIT.ResourceScheduler
{
    /// <summary>
    /// VesselEventData is used to track game events occurring. Once it picks up an event occurring, it tries to
    /// find the corresponding KITResourceManager and lets it know to refresh it's part module cache.
    /// </summary>
    
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class VesselEventData : MonoBehaviour
    {
        /// <summary>
        /// Initializes the VesselEventData class, and hooks into the GameEvents code.
        /// </summary>
        void Start()
        {
            GameEvents.onVesselGoOnRails.Add(refreshActiveParts);
            GameEvents.onVesselGoOffRails.Add(refreshActiveParts);
            GameEvents.onVesselWasModified.Add(refreshActiveParts);
            GameEvents.onVesselPartCountChanged.Add(refreshActiveParts);
            GameEvents.onVesselLoaded.Add(refreshActiveParts);
            GameEvents.onPartDeCouple.Add(refreshActiveParts);
            GameEvents.onPartDestroyed.Add(refreshActiveParts);
            GameEvents.onPartPriorityChanged.Add(refreshActiveParts);
            GameEvents.onPartDie.Add(refreshActiveParts);
            GameEvents.onPartWillDie.Add(refreshActiveParts);
            GameEvents.onPartDeCouple.Add(refreshActiveParts);
            GameEvents.onPartFailure.Add(refreshActiveParts);
        }

        void OnDestroy()
        {
            GameEvents.onVesselGoOnRails.Remove(refreshActiveParts);
            GameEvents.onVesselGoOffRails.Remove(refreshActiveParts);
            GameEvents.onVesselWasModified.Remove(refreshActiveParts);
            GameEvents.onVesselPartCountChanged.Remove(refreshActiveParts);
            GameEvents.onVesselLoaded.Remove(refreshActiveParts);
            GameEvents.onPartDeCouple.Remove(refreshActiveParts);
            GameEvents.onPartDestroyed.Remove(refreshActiveParts);
            GameEvents.onPartPriorityChanged.Remove(refreshActiveParts);
            GameEvents.onPartDie.Remove(refreshActiveParts);
            GameEvents.onPartWillDie.Remove(refreshActiveParts);
            GameEvents.onPartDeCouple.Remove(refreshActiveParts);
            GameEvents.onPartFailure.Remove(refreshActiveParts);
        }

        private KITResourceVesselModule FindVesselModuleImplementing(Vessel v)
        {
            KITResourceVesselModule ret;
            for (var i = 0; i < v.vesselModules.Count; i++)
            {
                ret = v.vesselModules[i] as KITResourceVesselModule;
                if (ret != null) return ret;
            }
            return null;
        }

        /// <summary>
        /// Looks up the corresponding KITResourceManager and tells it to refresh its module cache.
        /// </summary>
        /// <param name="data">Part triggering the event</param>
        private void refreshActiveParts(Part data)
        {
            if (data == null || data.vessel == null) return;
            var resourceMod = FindVesselModuleImplementing(data.vessel);
            if (resourceMod == null) return;
            resourceMod.refreshEventOccurred = true;
        }
        /// <summary>
        /// /// Looks up the corresponding KITResourceManager and tells it to refresh its module cache.
        /// </summary>
        /// <param name="data">Vessel triggering the event</param>
        private void refreshActiveParts(Vessel data)
        {
            if (data == null) return;
            var resourceMod = FindVesselModuleImplementing(data);
            if (resourceMod == null) return;
            resourceMod.refreshEventOccurred = true;
        }
    }
}
