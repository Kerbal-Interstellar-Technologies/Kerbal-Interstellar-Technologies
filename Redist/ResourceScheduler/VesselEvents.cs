using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIT.ResourceScheduler
{
    /// <summary>
    /// VesselEventData is used to track game events occurring. Once it picks up an event occurring, it tries to
    /// find the corresponding KITResourceManager and lets it know to refresh it's part module cache.
    /// </summary>
    public static class VesselEventData
    {
        private static bool initialized;

        /// <summary>
        /// Initializes the VesselEventData class, and hooks into the GameEvents code.
        /// </summary>
        static void initialize()
        {
            if (!initialized && HighLogic.LoadedSceneIsGame | HighLogic.LoadedSceneIsFlight)
            {
                GameEvents.onVesselGoOnRails.Add(new EventData<Vessel>.OnEvent(refreshActiveParts));
                GameEvents.onVesselWasModified.Add(new EventData<Vessel>.OnEvent(refreshActiveParts));
                GameEvents.onVesselPartCountChanged.Add(new EventData<Vessel>.OnEvent(refreshActiveParts));
                GameEvents.onVesselGoOffRails.Add(new EventData<Vessel>.OnEvent(refreshActiveParts));
                GameEvents.onVesselLoaded.Add(new EventData<Vessel>.OnEvent(refreshActiveParts));
                GameEvents.onPartDestroyed.Add(new EventData<Part>.OnEvent(refreshActiveParts));
                GameEvents.onPartPriorityChanged.Add(new EventData<Part>.OnEvent(refreshActiveParts));
                GameEvents.onPartDie.Add(new EventData<Part>.OnEvent(refreshActiveParts));
                GameEvents.onPartDeCouple.Add(new EventData<Part>.OnEvent(refreshActiveParts));
                // GameEvents.
                initialized = true;
            }
        }

        /// <summary>
        /// Looks up the corresponding KITResourceManager and tells it to refresh its module cache.
        /// </summary>
        /// <param name="data">Part triggering the event</param>
        private static void refreshActiveParts(Part data)
        {
            if (data == null || data.vessel == null) return;
            var resourceMod = data.vessel.FindVesselModuleImplementing<KITResourceVesselModule>();
            if (resourceMod == null) return;
            resourceMod.refreshEventOccurred = true;
        }
        /// <summary>
        /// /// Looks up the corresponding KITResourceManager and tells it to refresh its module cache.
        /// </summary>
        /// <param name="data">Vessel triggering the event</param>
        private static void refreshActiveParts(Vessel data)
        {
            if (data == null) return;
            var resourceMod = data.FindVesselModuleImplementing<KITResourceVesselModule>();
            if (resourceMod == null) return;
            resourceMod.refreshEventOccurred = true;
        }

        /// <summary>
        /// Dummy func to ensure the class is initialized.
        /// </summary>
        /// <returns>true</returns>
        public static bool Ready()
        {
            if (initialized == false) initialize();
            return initialized;
        }
    }
}
