using KIT.ResourceScheduler;
using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace KIT.Resources
{
    class CrustalResourceAbundance
    {
        public CrustalResource Resource { get; set; }
        public double Local { get; set; }
    }

    class UniversalCrustExtractor : PartModule, IKITModule
    {
        public const string Group = "UniversalCrustExtractor";
        public const string GroupTitle = "#LOC_KSPIE_UniversalCrustExtractor_groupName";

        // state of the extractor
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = true, guiActive = true, guiName = "#LOC_KSPIE_UniversalCrustExtractor_DrillEnabled")]//Drill Enabled
        public bool bIsEnabled;
        [KSPField(groupName = Group, isPersistant = true, guiActive = false, guiName = "#LOC_KSPIE_UniversalCrustExtractor_DrillDeployed")]//Deployed
        public bool isDeployed;

        [KSPField(isPersistant = true)]
        public float windowPositionX = 20;
        [KSPField(isPersistant = true)]
        public float windowPositionY = 20;

        // drill properties, need to be addressed in the cfg file of the part
        [KSPField(groupName = Group, groupDisplayName = GroupTitle, isPersistant = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_UniversalCrustExtractor_Drillsize", guiUnits = " m\xB3")]//Drill size
        public double drillSize = 5; // Volume of the collector's drill. Raise in part config (for larger drills) to make collecting faster.
        [KSPField(groupName = Group, isPersistant = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_UniversalCrustExtractor_DrillEffectiveness", guiFormat = "P1")]//Drill effectiveness
        public double effectiveness = 1; // Effectiveness of the drill. Lower in part config (to a 0.5, for example) to slow down resource collecting.
        [KSPField(groupName = Group, isPersistant = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_UniversalCrustExtractor_MWRequirements", guiUnits = "#LOC_KSPIE_Reactor_megawattUnit")]//MW Requirements
        public double mwRequirements = 1; // MW requirements of the drill. Affects heat produced.
        [KSPField(groupName = Group, isPersistant = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_UniversalCrustExtractor_WasteHeatModifier", guiFormat = "P1")]//Waste Heat Modifier
        public double wasteHeatModifier = 0.25; // How much of the power requirements ends up as heat. Change in part cfg, treat as a percentage (1 = 100%). Higher modifier means more energy ends up as waste heat.
        [KSPField(groupName = Group, isPersistant = false, guiActiveEditor = true, guiName = "#LOC_KSPIE_UniversalCrustExtractor_DrillReach", guiUnits = " m\xB3")]//Drill reach
        public float drillReach = 5; // How far can the drill actually reach? Used in calculating raycasts to hit ground down below the part. The 5 is just about the reach of the generic drill. Change in part cfg for different models.
        [KSPField(groupName = Group, isPersistant = false, guiActive = false)]
        public string loopingAnimationName = "";
        [KSPField(groupName = Group, isPersistant = false, guiActive = false)]
        public string deployAnimationName = "";
        [KSPField(groupName = Group, isPersistant = false, guiActive = false)]
        public float animationState;
        [KSPField(groupName = Group, isPersistant = false, guiActive = true, guiName = "#LOC_KSPIE_UniversalCrustExtractor_ReasonNotCollecting")]//Reason Not Collecting
        public string reasonNotCollecting;

        [KSPField(groupName = Group, isPersistant = false, guiActive = true, guiName = "Harvest Type")]
        public string harvestType;

        private int powerCountdown;
        private int powerCountdownMax = 90;
        private const double minimumPowerNeeded = 0.15;

        // GUI elements declaration
        private Rect _windowPosition = new Rect(50, 50, labelWidth + valueWidth * 5, 150);
        private int _windowId;
        private bool _renderWindow;


        private ModuleScienceExperiment _moduleScienceExperiment;

        private Animation _deployAnimation;
        private Animation _loopAnimation;

        private const int labelWidth = 200;
        private const int valueWidth = 100;

        private GUIStyle _boldLabel;
        private GUIStyle _normalLabel;

        private KSPParticleEmitter[] _particleEmitters;

        private readonly Dictionary<string, CrustalResourceAbundance> _crustalResourceAbundanceDict = new Dictionary<string, CrustalResourceAbundance>();

        private AbundanceRequest _resourceRequest = new AbundanceRequest // create a new request object that we'll reuse to get the current stock-system resource concentration
        {
            ResourceType = HarvestTypes.Planetary,
            ResourceName = "", // this will need to be updated before 'sending the request'
            BodyId = 1, // this will need to be updated before 'sending the request'
            Latitude = 0, // this will need to be updated before 'sending the request'
            Longitude = 0, // this will need to be updated before 'sending the request'
            Altitude = 0, // this will need to be updated before 'sending the request'
            CheckForLock = false,
            ExcludeVariance = false,
        };

        // *** KSP Events ***



        [KSPEvent(groupName = Group, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_UniversalCrustExtractor_DeployDrill", active = true)]//Deploy Drill
        public void DeployDrill()
        {
            isDeployed = true;
            if (_deployAnimation == null) return;

            _deployAnimation[deployAnimationName].speed = 1;
            _deployAnimation[deployAnimationName].normalizedTime = 0;
            _deployAnimation.Blend(deployAnimationName);
        }

        [KSPEvent(groupName = Group, guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_UniversalCrustExtractor_RetractDrill", active = true)]//Retract Drill
        public void RetractDrill()
        {
            bIsEnabled = false;
            isDeployed = false;

            animationState = 0;
            if (_loopAnimation != null)
            {
                _loopAnimation[loopingAnimationName].speed = -1;
                _loopAnimation[loopingAnimationName].normalizedTime = 0;
                _loopAnimation.Blend(loopingAnimationName);
            }

            if (_deployAnimation != null)
            {
                _deployAnimation[deployAnimationName].speed = -1;
                _deployAnimation[deployAnimationName].normalizedTime = 1;
                _deployAnimation.Blend(deployAnimationName);
            }
        }


        // *** KSP Events ***
        [KSPEvent(groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_UniversalCrustExtractor_ActivateDrill", active = true)]//Activate Drill
        public void ActivateCollector()
        {
            powerCountdown = powerCountdownMax;
            isDeployed = true;
            bIsEnabled = true;
            OnFixedUpdate();
        }

        [KSPEvent(groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_UniversalCrustExtractor_DisableDrill", active = true)]//Disable Drill
        public void DisableCollector()
        {
            bIsEnabled = false;
            OnFixedUpdate();
        }

        [KSPEvent(groupName = Group, guiActive = true, guiName = "#LOC_KSPIE_UniversalCrustExtractor_ToggleMiningInterface", active = false)]//Toggle Mining Interface
        public void ToggleWindow()
        {
            _renderWindow = !_renderWindow;
        }

        // *** END of KSP Events

        // *** KSP Actions ***

        [KSPAction("Toggle Deployment")]
        public void ToggleDeployAction(KSPActionParam param)
        {
            if (isDeployed)
                RetractDrill();
            else
                DeployDrill();
        }

        [KSPAction("Toggle Drill")]
        public void ToggleDrillAction(KSPActionParam param)
        {
            if (bIsEnabled)
                DisableCollector();
            else
                ActivateCollector();
        }

        [KSPAction("Activate Drill")]
        public void ActivateScoopAction(KSPActionParam param)
        {
            ActivateCollector();
        }

        [KSPAction("Disable Drill")]
        public void DisableScoopAction(KSPActionParam param)
        {
            DisableCollector();
        }
        // *** END of KSP Actions

        public override void OnStart(StartState state)
        {
            // initialise resources
            //resources_to_supply = new[] { ResourceSettings.Config.WasteHeatInMegawatt };
            base.OnStart(state);

            _moduleScienceExperiment = part.FindModuleImplementing<ModuleScienceExperiment>();

            _deployAnimation = part.FindModelAnimators(deployAnimationName).FirstOrDefault();
            _loopAnimation = part.FindModelAnimators(loopingAnimationName).FirstOrDefault();

            _particleEmitters = part.GetComponentsInChildren<KSPParticleEmitter>();

            if (_deployAnimation != null)
            {
                if (isDeployed)
                {
                    _deployAnimation[deployAnimationName].speed = 1;
                    _deployAnimation[deployAnimationName].normalizedTime = 1;
                    _deployAnimation.Blend(deployAnimationName);
                }
                else
                {
                    _deployAnimation[deployAnimationName].speed = -1;
                    _deployAnimation[deployAnimationName].normalizedTime = 0;
                    _deployAnimation.Blend(deployAnimationName);
                }
            }

            ToggleEmitters(false);

            // if the setup went well, do the offline collecting dance
            if (StartupSetup(state))
            {
                // force activate this part if not in editor; otherwise the OnFixedUpdate etc. would not work

                Debug.Log("[KSPI]: UniversalCrustExtractor on " + part.name + " was Force Activated");
                part.force_activate();

                // create the id for the GUI window
                _windowId = new System.Random(part.GetInstanceID()).Next(int.MinValue, int.MaxValue);
            }

        }

        public override void OnUpdate()
        {
            reasonNotCollecting = CheckIfCollectingPossible();

            Events["DeployDrill"].active = !isDeployed && !_deployAnimation.IsPlaying(deployAnimationName);
            Events["RetractDrill"].active = isDeployed && !_deployAnimation.IsPlaying(deployAnimationName);

            if (string.IsNullOrEmpty(reasonNotCollecting))
            {
                if (_moduleScienceExperiment != null)
                {
                    _moduleScienceExperiment.Events["DeployExperiment"].active = true;
                    _moduleScienceExperiment.Events["DeployExperimentExternal"].active = true;
                    _moduleScienceExperiment.Actions["DeployAction"].active = true;
                }

                if (effectiveness > 0)
                {
                    Events["ActivateCollector"].active = !bIsEnabled; // will activate the event (i.e. show the gui button) if the process is not enabled
                    Events["DisableCollector"].active = bIsEnabled; // will show the button when the process IS enabled
                }

                Events["ToggleWindow"].active = true;

                if (_terrainType == TerrainType.Planetary)
                {
                    GetResourceData(vessel, out _, _crustalResourceAbundanceDict);
                }
                else
                {
                    UpdateAsteroidCometResources();
                }
            }
            else
            {
                if (_moduleScienceExperiment != null)
                {
                    _moduleScienceExperiment.Events["DeployExperiment"].active = false;
                    _moduleScienceExperiment.Events["DeployExperimentExternal"].active = false;
                    _moduleScienceExperiment.Actions["DeployAction"].active = false;
                }

                Events["ActivateCollector"].active = false;
                Events["DisableCollector"].active = false;
                Events["ToggleWindow"].active = false;

                _renderWindow = false;
            }

            //if (bIsEnabled && loopingAnimation != "")
            //    PlayAnimation(loopingAnimation, false, false, true); //plays independently of other anims

            base.OnUpdate();
        }

        interface C
        {
            int GetFucked();
        }

        private void UpdateAsteroidCometResources()
        {
            if (_drillTarget == null)
            {
                // Debug.Log($"[Universal Drill] no resources because of no drillTarget");
                return;
            }

            // throw new NotImplementedException();
            if (_rateLimit++ % 250 == 0)
            {
                var resources = _drillTarget.vessel.FindPartModulesImplementing<ModuleSpaceObjectResource>();

                Debug.Log($"[Universal Crust Extractor] there's {resources.Count} resources present on {_drillTarget.vessel.GetDisplayName()}");
            }
        }

        private void OnGUI()
        {
            if (vessel == FlightGlobals.ActiveVessel && _renderWindow)
                _windowPosition = GUILayout.Window(_windowId, _windowPosition, DrawGui, Localizer.Format("#LOC_KSPIE_UniversalCrustExtractor_windowtitle"));//"Universal Mining Interface"

            //scrollPosition[1] = GUI.VerticalScrollbar(_window_position, scrollPosition[1], 1, 0, 150, "Scroll");
        }

        // *** STARTUP FUNCTIONS ***
        private bool StartupSetup(StartState state)
        {
            // this bit goes through parts that contain animations and disables the "Status" field in GUI part window so that it's less crowded
            List<ModuleAnimateGeneric> MAGlist = part.FindModulesImplementing<ModuleAnimateGeneric>();
            foreach (ModuleAnimateGeneric MAG in MAGlist)
            {
                MAG.Fields["status"].guiActive = false;
                MAG.Fields["status"].guiActiveEditor = false;
            }
            if (state == StartState.Editor)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        // *** END OF STARTUP FUNCTIONS ***

        // *** MINING FACILITATION FUNCTIONS ***

        /// <summary>
        /// The main "check-if-we-can-mine-here" function.
        /// </summary>
        /// <returns>Bool signifying whether yes, we can mine here, or not.</returns>
        private string CheckIfCollectingPossible()
        {
            if (!IsDrillExtended())
                return Localizer.Format("#LOC_KSPIE_UniversalCrustExtractor_msg2");//"needs to be extended before it can be used."

            if (!IsTerrainReachable())
                return " " + Localizer.Format("#LOC_KSPIE_UniversalCrustExtractor_msg3");//trouble reaching the terrain.

            switch (_terrainType)
            {
                case TerrainType.Planetary:
                    if (vessel.checkLanded() == false || vessel.checkSplashed())
                        return Localizer.Format("#LOC_KSPIE_UniversalCrustExtractor_msg1");//"Vessel is not landed properly."
                    break;
                case TerrainType.Asteroid:
                case TerrainType.Comet:
                    var attached = vessel.parts.Find(x => x == _drillTarget.part);
                    if (attached == null)
                    {
                        return Localizer.Format("#LOC_KSPIE_UniversalCrustExtractor_msg1");
                    }
                    break;
            }
            // cleared all the prerequisites
            return string.Empty;
        }

        /// <summary>
        /// Helper function to see if the drill part is extended.
        /// </summary>
        /// <returns>Bool signifying whether the part is extended or not (if it's animation is played out).</returns>
        private bool IsDrillExtended() => isDeployed && !_deployAnimation.IsPlaying(deployAnimationName);

        /// <summary>
        /// Helper function to raycast what the drill could hit.
        /// </summary>
        /// <returns>The RaycastHit, which allows us to determine what is underneath us</returns>
        private RaycastHit WhatsUnderneath()
        {
            Vector3d partPosition = part.transform.position; // find the position of the transform in 3d space
            var scaleFactor = part.rescaleFactor; // what is the rescale factor of the drill?
            var drillDistance = drillReach * scaleFactor; // adjust the distance for the ray with the rescale factor, needs to be a float for raycast.

            Ray drillPartRay = new Ray(partPosition, -part.transform.up); // this ray will start at the part's center and go down in local space coordinates (Vector3d.down is in world space)

            /* This little bit will fire a ray from the part, straight down, in the distance that the part should be able to reach.
             * It returns true if there is solid terrain in the reach AND the drill is extended. Otherwise false.
             * This is actually needed because stock KSP terrain detection is not really dependable. This module was formerly using just part.GroundContact
             * to check for contact, but that seems to be bugged somehow, at least when paired with this drill - it works enough times to pass tests, but when testing
             * this module in a difficult terrain, it just doesn't work properly. (I blame KSP planet meshes + Unity problems with accuracy further away from origin).
            */
            Physics.Raycast(drillPartRay, out var hit, drillDistance); // use the defined ray, pass info about a hit, go the proper distance and choose the proper layermask

            return hit;
        }

        private int _rateLimit;

        private enum TerrainType
        {
            None,
            Planetary,
            Comet,
            Asteroid,
        }

        private TerrainType _terrainType;

        private PartModule _drillTarget;

        /// <summary>
        /// Helper function to calculate (and raycast) if the drill could potentially hit the terrain.
        /// </summary>
        /// <returns>True if the raycast hits the terrain layermask and it's close enough for the drill to reach (affected by the drillReach part property).</returns>
        private bool IsTerrainReachable()
        {
            _terrainType = TerrainType.None;
            RaycastHit hit = WhatsUnderneath();

            _drillTarget = null;
            _asteroidCometResources = null;

            if (hit.collider == null) return false;

            if (hit.collider.gameObject.GetComponentUpwards<LocalSpace>())
            {
                _terrainType = TerrainType.Planetary;
                harvestType = $"LocalSpace (planet / moon) - {vessel.checkLanded()}";
            }

            _drillTarget = hit.collider.gameObject.GetComponentUpwards<PartModule>();

            if (_drillTarget == null) return false;

            if (_drillTarget.ClassName == "ModuleComet")
            {
                _terrainType = TerrainType.Comet;
                harvestType = $"Comet harvesting - {vessel.SituationString}";
            }

            if (_drillTarget.ClassName == "ModuleAsteroid")
            {
                _terrainType = TerrainType.Asteroid;
                harvestType = $"Asteroid harvesting - {vessel.SituationString}";
            }

            if (_terrainType == TerrainType.None)
            {
                _drillTarget = null;

                if (_rateLimit++ % 250 == 0)
                {
                    Debug.Log($"[Universal Drill] Hit /something/ but I have no idea what. Please let a developer know about this, what planet pack you're using, so on and so fourth. Thanks.");
                    Debug.Log($"[Universal Drill] drillTarget.className is {_drillTarget.ClassName}, name is {_drillTarget.name}");
                    return false;
                }
            }

            return true;

        }

        /// <summary>
        /// Function for accessing the resource data for the current planet.
        /// Returns true if getting the data went okay.
        /// </summary>
        /// <returns>List of resources found</returns>
        private static bool GetResourceData(Vessel vessel, out List<CrustalResource> localResources, Dictionary<string, CrustalResourceAbundance> resourceAbundance)
        {
            localResources = null;

            try
            {
                localResources = CrustalResourceHandler.GetCrustalCompositionForBody(FlightGlobals.currentMainBody)
                    .OrderBy(m => m.ResourceName).ToList();

                foreach (CrustalResource resource in localResources)
                {
                    var currentAbundance = GetResourceAbundance(vessel, resource);

                    if (resourceAbundance.TryGetValue(resource.ResourceName, out var existingAbundance))
                    {
                        existingAbundance.Local = currentAbundance.Local;
                    }
                    else
                        resourceAbundance.Add(resource.ResourceName, currentAbundance);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    "[KSPI] UniversalCrustExtractor - Error while getting the crustal composition for the current body. Msg: " +
                    e.Message + ". StackTrace: " + e.StackTrace);
                localResources = null;
            }
            finally
            {
                if (localResources == null)
                {
                    Console.WriteLine("[KSPI] UniversalCrustExtractor - Error while getting the crustal composition. The composition arrived, but it was null.");

                }
            }

            return localResources != null;
        }

        /// <summary>
        /// Gets the resource content percentage on the current planet.
        /// Takes a CrustalResource as a parameter.
        /// Returns boolean true if the data was gotten without trouble and also returns a double with the percentage.
        /// </summary>
        /// <param name="vessel"></param>
        /// <param name="currentResource">A CrustalResource we want to get the percentage for.</param>
        /// <returns></returns>
        private static CrustalResourceAbundance GetResourceAbundance(Vessel vessel, CrustalResource currentResource)
        {
            var abundance = new CrustalResourceAbundance() { Resource = currentResource };

            if (currentResource != null)
            {
                try
                {
                    abundance.Local = GetAbundance(new AbundanceRequest()
                    {
                        ResourceType = HarvestTypes.Planetary,
                        ResourceName = currentResource.ResourceName,
                        BodyId = FlightGlobals.currentMainBody.flightGlobalsIndex,
                        Latitude = vessel.latitude,
                        Longitude = vessel.longitude,
                        CheckForLock = false
                    });

                }
                catch (Exception)
                {
                    Console.WriteLine("[KSPI]: UniversalCrustExtractor - Error while retrieving crustal resource percentage for " + currentResource.ResourceName + " from CrustalResourceHandler. Setting to zero.");
                    return null; // if the percentage was not gotten correctly, we want to know, so return false
                }

                return abundance; // if we got here, the percentage-getting went well, so return true
            }
            else
            {
                Console.WriteLine("[KSPI]: UniversalCrustExtractor - Error while calculating percentage, resource null. Setting to zero.");
                return null; // resource was null, we want to know that we should disregard it, so return false
            }
        }

        private static double GetAbundance(AbundanceRequest request)
        {
            // retrieve and convert to double
            double abundance = (double)(decimal)ResourceMap.Instance.GetAbundance(request) * 100;

            if (abundance < 1)
                abundance = Math.Pow(abundance, 3);

            return abundance;
        }

        /// <summary>
        /// Gets the 'thickness' of the planet's crust. Returns true if the calculation went without a hitch.
        /// </summary>
        /// <param name="altitude">Current altitude of the vessel doing the mining.</param>
        /// <param name="planet">Current planetary body.</param>
        /// <param name="thickness">The output parameter that gets returned, the thickness of the crust (i.e. how much resources can be mined here).</param>
        /// <returns>True if data was acquired okay. Also returns an output parameter, the thickness of the crust.</returns>
        private static bool CalculateCrustThickness(double altitude, CelestialBody planet, out double thickness)
        {
            thickness = 0;
            CelestialBody homeWorld = FlightGlobals.Bodies.SingleOrDefault(b => b.isHomeWorld);
            if (homeWorld == null)
            {
                Console.WriteLine("[KSPI]: UniversalCrustExtractor. Home world not found, setting crust thickness to 0.");
                return false;
            }
            double homePlanetMass = homeWorld.Mass; // This will usually be Kerbin, but players can always use custom planet packs with a custom home planet or resized systems
            double planetMass = planet.Mass;

            /* I decided to incorporate an altitude modifier (similarly to regolith collector before).
             * According to various source, crust thickness is higher in higher altitudes (duh).
             * This is great from a game play perspective, because it creates an incentive for players to mine resources in more difficult circumstances
             * (i.e. landing on highlands instead of flats etc.) and breaks the flatter-is-better base building strategy at least a bit.
             * This check will divide current altitude by 2500. At that arbitrarily-chosen altitude, we should be getting the basic concentration for the planet.
             * Go to a higher terrain and you will find **more** resources. The + 500 shift is there so that even at altitude of 0 (i.e. Minmus flats etc.) there will
             * still be at least SOME resources to be mined, but not all that much.
             * This is pretty much the same as the regolith collector (which might get phased out eventually).
             */
            double dAltModifier = (altitude + 500.0) / 2500.0;

            // if the dAltModifier is negative (if we're somehow trying to mine in a crack under sea level, perhaps), assign 0, otherwise keep it as it is
            dAltModifier = dAltModifier < 0 ? 0 : dAltModifier;

            /* The actual concentration calculation is pretty simple. The more mass the current planet has in comparison to the homeworld, the more resources can be mined here.
             * While this might seem unfair to smaller moons and planets, this is actually somewhat realistic - bodies with smaller mass would be more porous,
             * so there might be lesser amount of heavier elements and less useful stuff to go around altogether.
             * This is then adjusted for the altitude modifier - there is simply more material to mine at high hills and mountains.
            */
            thickness = dAltModifier * (planetMass / homePlanetMass); // get a basic concentration. The more mass the current planet has, the more crustal resources to be found here
            return true;
        }
        
        /// <summary>
        /// Does the actual addition (collection) of the current resource.
        /// </summary>
        /// <param name="amount">The amount of resource to collect/add.</param>
        /// <param name="resourceName">The name of the current resource.</param>
        private double AddResource(IResourceManager resMan, double amount, string resourceName)
        {
            var resID = KITResourceSettings.NameToResource(resourceName);
            if (resID == ResourceName.Unknown)
            {
                return part.RequestResource(resourceName, -amount * resMan.FixedDeltaTime(), ResourceFlowMode.ALL_VESSEL);
            }

            resMan.Produce(resID, amount);
            return amount;
        }

        // *** The important function controlling the mining ***
        /// <summary>
        /// The controlling function of the mining. Calls individual/granular functions and decides whether to continue
        /// collecting resources or not.
        /// </summary>
        /// <param name="offlineCollecting">Bool parameter, signifies if this collection is done in catch-up mode (i.e. after the focus has been on another vessel).</param>
        /// <param name="deltaTime">Double, signifies the amount of time since last Fixed Update (Unity).</param>
        private void MineResources(IResourceManager resMan)
        {
            reasonNotCollecting = CheckIfCollectingPossible();

            if (!string.IsNullOrEmpty(reasonNotCollecting)) // collecting not possible due to some reasons.
            {
                ScreenMessages.PostScreenMessage(reasonNotCollecting, 3.0f, ScreenMessageStyle.LOWER_CENTER);

                DisableCollector();
                return; // let's get out of here, no mining for now
            }

            double wantedPower = PluginSettings.Config.PowerConsumptionMultiplier * mwRequirements;
            double percentPower = resMan.Consume(ResourceName.ElectricCharge, wantedPower) / wantedPower;

            if (percentPower < minimumPowerNeeded)
            {
                if (powerCountdown > 0)
                {
                    powerCountdown -= 1;
                    return;
                }

                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_UniversalCrustExtractor_PostMsg1"), 3.0f, ScreenMessageStyle.LOWER_CENTER);//"Not enough power to run the universal drill."
                DisableCollector();
                return;
            }

            switch (_terrainType)
            {
                case TerrainType.None:
                    return;

                case TerrainType.Planetary:
                    if (!MinePlanetaryBody(resMan, vessel, _crustalResourceAbundanceDict, percentPower, drillSize, effectiveness))
                    {
                        DisableCollector();
                    }
                    return;
                case TerrainType.Comet:
                case TerrainType.Asteroid:
                    var spaceObjectInfo = _drillTarget?.part.FindModuleImplementing<ModuleSpaceObjectInfo>();
                    if (spaceObjectInfo == null)
                    {
                        Debug.Log($"[Universal Drill] no ModuleSpaceObjectInfo available. Free resources!");
                        DisableCollector();
                        return;
                    }

                    var resourceObjects = _drillTarget.part.FindModulesImplementing<ModuleSpaceObjectResource>();
                    
                    if (!MineAsteroidComet(resMan, spaceObjectInfo, resourceObjects, drillSize, effectiveness, percentPower))
                    {
                        DisableCollector();
                    }
                    
                    return;
            }
        }

        private static bool MineAsteroidComet(IResourceManager resMan, ModuleSpaceObjectInfo spaceObjectInfo, List<ModuleSpaceObjectResource> asteroidCometResources, double drillSize, double effectiveness, double percentPower)
        {
            // TODO - can we add a bonus multiplier here? Perhaps based on asteroid mass?
            double bonusMultiplier = 0.05; // 0.0005
            double minedAmount = bonusMultiplier * drillSize * effectiveness * percentPower;

            minedAmount = Math.Min(minedAmount, (spaceObjectInfo.currentMassVal - spaceObjectInfo.massThresholdVal));
            if (minedAmount < 1e-6)
            {
                spaceObjectInfo.currentMassVal = spaceObjectInfo.massThresholdVal;
                return false;
            }
            
            foreach (var resource in asteroidCometResources)
            {
                var def = PartResourceLibrary.Instance.GetDefinition(resource.resourceName);
                if (def == null) continue;

                var amount = (minedAmount * resource.abundance) * def.density;

                if (amount == 0) continue;

                var resId = KITResourceSettings.NameToResource(resource.resourceName);
                resMan.Produce(resId, amount);
            }

            spaceObjectInfo.currentMassVal = Math.Max(spaceObjectInfo.massThresholdVal, spaceObjectInfo.currentMassVal - (minedAmount * resMan.FixedDeltaTime()));

            return true;
        }

        private static bool MinePlanetaryBody(IResourceManager resMan, Vessel vessel, Dictionary<string, CrustalResourceAbundance> resourceAbundance, double percentPower, double drillSize, double effectiveness)
        {
            var ret = GetResourceData(vessel, out var localResources, resourceAbundance);
            if (ret == false) // if gathering resource data was not okay, no mining
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_UniversalCrustExtractor_PostMsg2"), 3.0f, ScreenMessageStyle.LOWER_CENTER);//"The universal drill is not sure where you are trying to mine. Please contact the mod author, tell him the details of this situation and provide the output log."
                // DisableCollector();
            }

            if (!CalculateCrustThickness(vessel.altitude, FlightGlobals.currentMainBody, out var crustThickness)) // crust thickness calculation off, no mining
            {
                return false;
            }

            double minedAmount = crustThickness * drillSize * effectiveness * percentPower;
            double minedAmountStock = 0.0005 * drillSize * effectiveness * percentPower;

            foreach (CrustalResource resource in localResources)
            {
                resourceAbundance.TryGetValue(resource.ResourceName, out var abundance);

                if (abundance == null)
                    continue;

                if (resource.ResourceName == "Ore")
                    resource.Production = minedAmountStock * abundance.Local;
                else
                    resource.Production = minedAmount * abundance.Local;

                var resId = KITResourceSettings.NameToResource(resource.ResourceName);
                var ok = resMan.CapacityInformation(resId, out var maxAmount,
                    out var spareRoom, out var currentAmount, out var _);

                if (!ok) continue;

                if (resource.SpareRoom > 0) // if there's space, add the resource
                    resMan.Produce(resId, resource.Production);
            }

            return true;
        }

        private List<ModuleSpaceObjectResource> _asteroidCometResources;

        private void GetAsteroidCometResourceData()
        {
            if (_drillTarget == null) return;

            _asteroidCometResources = _drillTarget.part.FindModulesImplementing<ModuleSpaceObjectResource>();
        }

        private void DrawGui(int window)
        {
            if (_boldLabel == null)
                _boldLabel = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, font = PluginHelper.MainFont };

            if (_normalLabel == null)
                _normalLabel = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Normal, font = PluginHelper.MainFont };

            if (GUI.Button(new Rect(_windowPosition.width - 20, 2, 18, 18), "x"))
                _renderWindow = false;

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_UniversalCrustExtractor_Drillparameters"), _boldLabel, GUILayout.Width(labelWidth));//"Drill parameters:"
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_UniversalCrustExtractor_Size") + ": " + drillSize.ToString("#.#") + " m\xB3", _normalLabel);//Size
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_UniversalCrustExtractor_MWRequirements") + ": " + PluginHelper.GetFormattedPowerString(mwRequirements), _normalLabel);//MW Requirements
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_UniversalCrustExtractor_DrillEffectiveness") + ": " + effectiveness.ToString("P1"), _normalLabel);//Drill effectiveness
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_UniversalCrustExtractor_Resourcesabundances") + ":", _boldLabel, GUILayout.Width(labelWidth));//Resources abundances
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_UniversalCrustExtractor_Name"), _boldLabel, GUILayout.Width(valueWidth));//"Name"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_UniversalCrustExtractor_Abundance"), _boldLabel, GUILayout.Width(valueWidth));//"Abundance"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_UniversalCrustExtractor_Productionpersecond"), _boldLabel, GUILayout.Width(valueWidth));//"Production per second"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_UniversalCrustExtractor_Productionperhour"), _boldLabel, GUILayout.Width(valueWidth));//"Production per hour"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_UniversalCrustExtractor_SpareRoom"), _boldLabel, GUILayout.Width(valueWidth));//"Spare Room"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_UniversalCrustExtractor_Stored"), _boldLabel, GUILayout.Width(valueWidth));//"Stored"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_UniversalCrustExtractor_MaxCapacity"), _boldLabel, GUILayout.Width(valueWidth));//"Max Capacity"
            GUILayout.EndHorizontal();

            if (_terrainType == TerrainType.Planetary)
            {
                var ok = GetResourceData(vessel, out var localResources, _crustalResourceAbundanceDict);

                if (ok)
                {
                    foreach (CrustalResource resource in localResources)
                    {
                        _crustalResourceAbundanceDict.TryGetValue(resource.ResourceName, out var abundance);
                        if (abundance == null)
                            continue;

                        GUILayout.BeginHorizontal();
                        GUILayout.Label(resource.DisplayName, _normalLabel, GUILayout.Width(valueWidth));
                        GUILayout.Label(abundance.Local.ToString("##.######") + "%", _normalLabel, GUILayout.Width(valueWidth));

                        if (resource.Definition != null)
                        {
                            if (resource.MaxAmount > 0)
                            {
                                var spareRoomMass = resource.SpareRoom * resource.Definition.density;

                                if (Math.Round(spareRoomMass, 6) > 0.000001)
                                {
                                    GUILayout.Label(resource.Production.ToString("##.######") + " U/s", _normalLabel, GUILayout.Width(valueWidth));
                                    GUILayout.Label((resource.Production * resource.Definition.density * 3600).ToString("##.######") + " t/h", _normalLabel, GUILayout.Width(valueWidth));
                                    GUILayout.Label(spareRoomMass.ToString("##.######") + " t", _normalLabel, GUILayout.Width(valueWidth));
                                }
                                else
                                {
                                    GUILayout.Label("", _normalLabel, GUILayout.Width(valueWidth));
                                    GUILayout.Label("", _normalLabel, GUILayout.Width(valueWidth));
                                    GUILayout.Label(Localizer.Format("#LOC_KSPIE_UniversalCrustExtractor_full"), _normalLabel, GUILayout.Width(valueWidth));//"full"
                                }

                                GUILayout.Label((resource.Amount * resource.Definition.density).ToString("##.######") + " t", _normalLabel, GUILayout.Width(valueWidth));
                                GUILayout.Label((resource.MaxAmount * resource.Definition.density).ToString("##.######") + " t", _normalLabel, GUILayout.Width(valueWidth));
                            }
                            else
                            {
                                GUILayout.Label("", _normalLabel, GUILayout.Width(valueWidth));
                                GUILayout.Label("", _normalLabel, GUILayout.Width(valueWidth));
                                GUILayout.Label(Localizer.Format("#LOC_KSPIE_UniversalCrustExtractor_missing"), _normalLabel, GUILayout.Width(valueWidth));//"missing"
                            }
                        }

                        GUILayout.EndHorizontal();
                    }
                }
            }
            else
            {
                GetAsteroidCometResourceData();

                foreach (var spaceObjectResource in _asteroidCometResources.OrderByDescending(sor => sor.abundance))
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(spaceObjectResource.resourceName, _normalLabel, GUILayout.Width(valueWidth));
                    GUILayout.Label((spaceObjectResource.abundance * 100).ToString("##.######") + "%", _normalLabel, GUILayout.Width(valueWidth));
                    GUILayout.EndHorizontal();
                }

            }

            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void ToggleEmitters(bool state)
        {
            foreach (var e in _particleEmitters)
            {
                e.emit = state;
                e.enabled = state;
            }
        }

        public void UpdateLoopingAnimation()
        {
            if (_loopAnimation == null)
                return;

            if (animationState > 1)
                animationState = 0;

            animationState += 0.05f;

            _loopAnimation[loopingAnimationName].speed = 0;
            _loopAnimation[loopingAnimationName].normalizedTime = animationState;
            _loopAnimation.Blend(loopingAnimationName, 1);
        }

        public ModuleConfigurationFlags ModuleConfiguration() => ModuleConfigurationFlags.Fifth;
        public void KITFixedUpdate(IResourceManager resMan)
        {
            if (bIsEnabled)
            {
                ToggleEmitters(true);
                UpdateLoopingAnimation();

                MineResources(resMan);
            }
        }

        public string KITPartName() => part.partInfo.title;
    }
}
