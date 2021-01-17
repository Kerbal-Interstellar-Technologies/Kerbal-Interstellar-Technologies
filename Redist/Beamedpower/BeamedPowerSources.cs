using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KIT.BeamedPower
{
    public delegate IVesselMicrowavePersistence GetVesselMicrowavePersistenceForProtoVessel(Vessel vessel);
    public delegate IVesselRelayPersistence GetVesselRelayPersistenceForProtoVessel(Vessel vessel);
    public delegate IVesselMicrowavePersistence GetVesselMicrowavePersistenceForVessel(Vessel vessel);
    public delegate IVesselRelayPersistence GetVesselRelayPersistenceForVessel(Vessel vessel);

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class BeamedPowerSources : MonoBehaviour
    {
        public Dictionary<Vessel, IVesselMicrowavePersistence> GlobalTransmitters = new Dictionary<Vessel, IVesselMicrowavePersistence>();
        public Dictionary<Vessel, IVesselRelayPersistence> GlobalRelays = new Dictionary<Vessel, IVesselRelayPersistence>();

        public static GetVesselMicrowavePersistenceForProtoVessel GetVesselMicrowavePersistenceForProtoVesselCallback;
        public static GetVesselRelayPersistenceForProtoVessel GetVesselRelayPersistenceForProtoVesselCallback;
        public static GetVesselMicrowavePersistenceForVessel GetVesselMicrowavePersistenceForVesselCallback;
        public static GetVesselRelayPersistenceForVessel GetVesselRelayPersistenceForVesselCallback;

        public static BeamedPowerSources Instance
        {
            get;
            private set;
        }

        void Start()
        {
            DontDestroyOnLoad(gameObject);
            Instance = this;
            Debug.Log("[KSPI]: MicrowaveSources initialized");
        }

        private int _counter = -1;
        private bool _initialized;

        public void CalculateTransmitters()
        {
            _counter++;

            if (_counter > FlightGlobals.Vessels.Count)
                _counter = 0;

            //foreach (var vessel in FlightGlobals.Vessels)
            for (int i = 0; i < FlightGlobals.Vessels.Count; i++ )
            {
                var vessel = FlightGlobals.Vessels[i];

                // first check if vessel is dead
                if (vessel.state == Vessel.State.DEAD)
                {
                    if (GlobalTransmitters.ContainsKey(vessel))
                    {
                        GlobalTransmitters.Remove(vessel);
                        GlobalRelays.Remove(vessel);
                        Debug.Log("[KSPI]: Unregistered Transmitter for vessel " + vessel.name + " " + vessel.id + " because is was destroyed!");
                    }
                    continue;
                }

                // if vessel is offloaded on rails, parse file system
                if (!vessel.loaded)
                {
                    if (_initialized && i != _counter)
                        continue;

                    if (GetVesselMicrowavePersistenceForProtoVesselCallback != null)
                    {
                        // add if vessel can act as a transmitter or relay
                        var transPers = GetVesselMicrowavePersistenceForProtoVesselCallback(vessel);

                        var hasAnyPower = transPers.GetAvailablePowerInKW() > 0.001;
                        if (transPers.IsActive && hasAnyPower)
                        {
                            if (!GlobalTransmitters.ContainsKey(vessel))
                            {
                                Debug.Log("[KSPI]: Added unloaded Transmitter for vessel " + vessel.name);
                            }
                            GlobalTransmitters[vessel] = transPers;
                        }
                        else
                        {
                            if (GlobalTransmitters.Remove(vessel))
                            {
                                if (!transPers.IsActive && !hasAnyPower)
                                    Debug.Log("[KSPI]: Unregistered unloaded Transmitter for vessel " + vessel.name + " " + vessel.id + " because transmitter is not active and has no power!");
                                else if (!transPers.IsActive)
                                    Debug.Log("[KSPI]: Unregistered unloaded Transmitter for vessel " + vessel.name + " " + vessel.id + " because transmitter is not active!");
                                else if (!hasAnyPower)
                                    Debug.Log("[KSPI]: Unregistered unloaded Transmitter for vessel " + vessel.name + " " + vessel.id + " because transmitter is has no power");
                            }
                        }
                    }

                    if (GetVesselRelayPersistenceForProtoVesselCallback != null)
                    {
                        // only add if vessel can act as a relay
                        var relayPower = GetVesselRelayPersistenceForProtoVesselCallback(vessel);

                        if (relayPower.IsActive)
                            GlobalRelays[vessel] = relayPower;
                        else
                            GlobalRelays.Remove(vessel);
                    }

                    continue;
                }

                // if vessel is loaded
                if (vessel.FindPartModulesImplementing<IMicrowavePowerTransmitter>().Any())
                {
                    if (GetVesselMicrowavePersistenceForVesselCallback != null)
                    {
                        // add if vessel can act as a transmitter or relay
                        var transmitterPower = GetVesselMicrowavePersistenceForVesselCallback(vessel);

                        if (transmitterPower != null && transmitterPower.IsActive && transmitterPower.GetAvailablePowerInKW() > 0.001)
                        {
                            if (!GlobalTransmitters.ContainsKey(vessel))
                            {
                                Debug.Log("[KSPI]: Added loaded Transmitter for vessel " + vessel.name + " " + vessel.id);
                            }
                            GlobalTransmitters[vessel] = transmitterPower;
                        }
                        else
                            GlobalTransmitters.Remove(vessel);

                        // only add if vessel can act as a relay otherwise remove
                        var relayPower = GetVesselRelayPersistenceForVesselCallback(vessel);

                        if (relayPower != null && relayPower.IsActive)
                            GlobalRelays[vessel] = relayPower;
                        else
                            GlobalRelays.Remove(vessel);
                    }
                }
            }
            _initialized = true;

        }

        void Update()
        {
            if (HighLogic.LoadedSceneIsFlight)
                CalculateTransmitters();
        }
    }
}
