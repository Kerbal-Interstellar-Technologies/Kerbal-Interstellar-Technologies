using KIT.Resources;
using KIT.ResourceScheduler;
using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace KIT.Refinery.Activity
{
    class AtmosphereProcessor : RefineryActivity, IRefineryActivity
    {
        public AtmosphereProcessor()
        {
            ActivityName = "Atmospheric Extraction";
            PowerRequirements = PluginSettings.Config.BaseELCPowerConsumption;
            EnergyPerTon = PluginSettings.Config.ElectrolysisEnergyPerTon;
        }

        // persistent
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_KSPIE_AtmosphericExtractor_SurfaceArea", guiFormat = "F3")]//Surface Area
        public double surfaceArea = 1;

        [KSPField(isPersistant = true)]
        protected int lastBodyID = -1; // ID of the last body. Allows us to skip some expensive calls
        [KSPField(isPersistant = true)]
        public bool isDeployed;
        [KSPField(guiActive = false)]
        public float normalizedTime = -1;


        [KSPField]
        public double buildInAirIntake;
        [KSPField]
        public double atmosphereConsumptionRatio;
        [KSPField]
        public string animName = "";

        /* Individual percentages of all constituents of the local atmosphere. These are bound to be found in different
         * concentrations in all atmospheres. These are persistent because getting them every update through
         * the functions (see way below) would be wasteful. I'm placing them up here to make them easier to spot.
         */

        [KSPField(isPersistant = true)]
        protected double _ammoniaPercentage;
        [KSPField(isPersistant = true)]
        protected double _argonPercentage; // percentage of argon in the local atmosphere
        [KSPField(isPersistant = true)]
        protected double _chlorinePercentage;
        [KSPField(isPersistant = true)]
        protected double _dioxidePercentage; // percentage of carbon dioxide in the local atmosphere
        [KSPField(isPersistant = true)]
        protected double _helium3Percentage; // etc.
        [KSPField(isPersistant = true)]
        protected double _helium4Percentage;
        [KSPField(isPersistant = true)]
        protected double _hydrogenPercentage;
        [KSPField(isPersistant = true)]
        protected double _methanePercentage;
        [KSPField(isPersistant = true)]
        protected double _monoxidePercentage;
        [KSPField(isPersistant = true)]
        protected double _neonPercentage;
        [KSPField(isPersistant = true)]
        protected double _nitrogenPercentage;
        [KSPField(isPersistant = true)]
        protected double _nitrogen15Percentage;
        [KSPField(isPersistant = true)]
        protected double _oxygenPercentage;
        [KSPField(isPersistant = true)]
        protected double _waterPercentage;
        [KSPField(isPersistant = true)]
        protected double _heavywaterPercentage;
        [KSPField(isPersistant = true)]
        protected double _xenonPercentage;
        [KSPField(isPersistant = true)]
        protected double _deuteriumPercentage;
        [KSPField(isPersistant = true)]
        protected double _kryptonPercentage;
        [KSPField(isPersistant = true)]
        protected double _sodiumPercentage;

        private Animation _scoopAnimation;
        private double _fixedConsumptionRate;
        private double _consumptionStorageRatio;
        private double _intakeModifier;

        private PartResourceDefinition _atmosphere;

        // all the gases that it should be possible to collect from atmospheres
        private PartResourceDefinition _ammonia;
        private PartResourceDefinition _argon;
        private PartResourceDefinition _chlorine;
        private PartResourceDefinition _dioxide;
        private PartResourceDefinition _helium3;
        private PartResourceDefinition _helium4;
        private PartResourceDefinition _hydrogen;
        private PartResourceDefinition _methane;
        private PartResourceDefinition _monoxide;
        private PartResourceDefinition _neon;
        private PartResourceDefinition _nitrogen;
        private PartResourceDefinition _nitrogen15;
        private PartResourceDefinition _oxygen;
        private PartResourceDefinition _water; // water vapor can form a localPart of atmosphere as well
        private PartResourceDefinition _heavyWater;
        private PartResourceDefinition _xenon;
        private PartResourceDefinition _deuterium;
        private PartResourceDefinition _krypton;
        private PartResourceDefinition _sodium;

        private double _atmosphereConsumptionRate;

        private double _ammoniaProductionRate;
        private double _argonProductionRate;
        private double _chlorineProductionRate;
        private double _dioxideProductionRate;
        private double _helium3ProductionRate;
        private double _helium4ProductionRate;
        private double _hydrogenProductionRate;
        private double _methaneProductionRate;
        private double _monoxideProductionRate;
        private double _neonProductionRate;
        private double _nitrogenProductionRate;
        private double _nitrogen15ProductionRate;
        private double _oxygenProductionRate;
        private double _waterProductionRate;
        private double _heavyWaterProductionRate;
        private double _xenonProductionRate;
        private double _deuteriumProductionRate;
        private double _kryptonProductionRate;
        private double _sodiumProductionRate;

        [KSPEvent(guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_AtmosphericExtractor_DeployScoop", active = true, guiActiveUncommand = true, guiActiveUnfocused = true)]//Deploy Scoop
        public void DeployScoop()
        {
            RunAnimation(animName, _scoopAnimation, 0.5f, 0);
            isDeployed = true;
        }

        // GUI to retract sail
        [KSPEvent(guiActiveEditor = true, guiActive = true, guiName = "#LOC_KSPIE_AtmosphericExtractor_RetractScoop", active = false, guiActiveUncommand = true, guiActiveUnfocused = true)]//Retract Scoop
        public void RetractScoop()
        {
            RunAnimation(animName, _scoopAnimation, -0.5f, 1);
            isDeployed = false;
        }

        public RefineryType RefineryType => RefineryType.Cryogenics;

        public bool HasActivityRequirements()
        {
            return true;
        }

        public string Status => string.Copy(_status);

        public void Initialize(Part localPart)
        {
            _part = localPart;
            _vessel = localPart.vessel;
            _intakesList = _vessel.FindPartModulesImplementing<AtmosphericIntake>();

            if (!string.IsNullOrEmpty(animName))
            {
                _scoopAnimation = localPart.FindModelAnimators(animName).First();

                if (_scoopAnimation != null)
                {
                    _scoopAnimation[animName].speed = 0;
                    _scoopAnimation[animName].normalizedTime = isDeployed ? 1 : 0;
                    _scoopAnimation.Blend(animName);
                }
            }

            // get the densities of all relevant resources
            _atmosphere = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.IntakeAtmosphere);
            _ammonia = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.AmmoniaLqd);
            _argon = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.ArgonLqd);
            _chlorine = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.ChlorineGas);
            _dioxide = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.CarbonDioxideLqd);
            _helium3 = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.Helium3Lqd);
            _helium4 = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.Helium4Lqd);
            _hydrogen = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.HydrogenLqd);
            _methane = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.MethaneLqd);
            _monoxide = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.CarbonMonoxideLqd);
            _neon = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.NeonLqd);
            _nitrogen = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.NitrogenLqd);
            _nitrogen15 = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.Nitrogen15Lqd);
            _oxygen = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.OxygenLqd);
            _water = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.WaterPure);
            _heavyWater = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.WaterHeavy);
            _xenon = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.XenonLqd);
            _deuterium = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.DeuteriumLqd);
            _krypton = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.KryptonLqd);
            _sodium = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.Sodium);
        }

        double _maxCapacityAtmosphereMass;

        double _maxCapacityAmmoniaMass;
        double _maxCapacityArgonMass;
        double _maxCapacityChlorineMass;
        double _maxCapacityDioxideMass;
        double _maxCapacityHelium3Mass;
        double _maxCapacityHelium4Mass;
        double _maxCapacityHydrogenMass;
        double _maxCapacityMethaneMass;
        double _maxCapacityMonoxideMass;
        double _maxCapacityNeonMass;
        double _maxCapacityNitrogenMass;
        double _maxCapacityNitrogen15Mass;
        double _maxCapacityOxygenMass;
        double _maxCapacityWaterMass;
        double _maxCapacityHeavyWaterMass;
        double _maxCapacityXenonMass;
        double _maxCapacityDeuteriumMass;
        double _maxCapacityKryptonMass;
        double _maxCapacitySodiumMass;

        double _availableAtmosphereMass;

        double _spareRoomAtmosphereMass;
        double _spareRoomAmmoniaMass;
        double _spareRoomArgonMass;
        double _spareRoomChlorineMass;
        double _spareRoomDioxideMass;
        double _spareRoomHelium3Mass;
        double _spareRoomHelium4Mass;
        double _spareRoomHydrogenMass;
        double _spareRoomMethaneMass;
        double _spareRoomMonoxideMass;
        double _spareRoomNeonMass;
        double _spareRoomNitrogenMass;
        double _spareRoomNitrogen15Mass;
        double _spareRoomOxygenMass;
        double _spareRoomWaterMass;
        double _spareRoomHeavyWaterMass;
        double _spareRoomXenonMass;
        double _spareRoomDeuteriumMass;
        double _spareRoomKryptonMass;
        double _spareRoomSodiumMass;

        List<AtmosphericIntake> _intakesList; // create a new list for keeping track of atmosphere intakes

        public void UpdateFrame(IResourceManager resMan, double rateMultiplier, double powerFraction, double productionModifier, bool allowOverflow, bool isStartup = false)
        {
            ExtractAir(resMan, rateMultiplier, powerFraction, productionModifier, allowOverflow);

            UpdateStatusMessage();
        }
        /* This is just a short cycle that gets the total air production of all the intakes on the vessel per cycle
         * and then stores the value in the persistent totalAirValue, so that this process can access it when offline collecting.
         * tempAir is just a variable used to temporarily hold the total while cycling through parts, then gets reset at every engine update.
         */
        public double GetTotalAirScoopedPerSecond()
        {
            // add any atmosphere intake localPart on the vessel to our list
            double tempAir = 0; // reset tempAir before we go into the list
            foreach (AtmosphericIntake intake in _intakesList) // go through the list
            {
                // add the current intake's finalAir to our tempAir. When done with the foreach cycle, we will have the total amount of air these intakes collect per cycle
                tempAir += intake.FinalAir;
            }
            return tempAir;
        }

        public void ExtractAir(IResourceManager resMan, double rateMultiplier, double powerFraction, double productionModifier, bool allowOverflow)
        {
            _effectiveMaxPower = productionModifier * PowerRequirements;
            _current_power = _effectiveMaxPower * powerFraction;
            _current_rate = CurrentPower / EnergyPerTon;

            try
            {
                // determine the maximum amount of a resource the vessel can hold (ie. tank capacities combined)
                // determine how much spare room there is in the vessel's resource tanks (for the resources this is going to produce)
                GetResourceMass(resMan, ResourceName.IntakeAtmosphere, _atmosphere, ref _spareRoomAtmosphereMass, ref _maxCapacityAtmosphereMass);
                GetResourceMass(resMan, ResourceName.AmmoniaLqd, _ammonia, ref _spareRoomAmmoniaMass, ref _maxCapacityAmmoniaMass);
                GetResourceMass(resMan, ResourceName.ArgonLqd, _argon, ref _spareRoomArgonMass, ref _maxCapacityArgonMass);
                GetResourceMass(resMan, ResourceName.ChlorineGas, _chlorine, ref _spareRoomChlorineMass, ref _maxCapacityChlorineMass);
                GetResourceMass(resMan, ResourceName.CarbonDioxideLqd, _dioxide, ref _spareRoomDioxideMass, ref _maxCapacityDioxideMass);
                GetResourceMass(resMan, ResourceName.Helium3Lqd, _helium3, ref _spareRoomHelium3Mass, ref _maxCapacityHelium3Mass);
                GetResourceMass(resMan, ResourceName.Helium4Lqd, _helium4, ref _spareRoomHelium4Mass, ref _maxCapacityHelium4Mass);
                GetResourceMass(resMan, ResourceName.HydrogenLqd, _hydrogen, ref _spareRoomHydrogenMass, ref _maxCapacityHydrogenMass);
                GetResourceMass(resMan, ResourceName.MethaneLqd, _methane, ref _spareRoomMethaneMass, ref _maxCapacityMethaneMass);
                GetResourceMass(resMan, ResourceName.CarbonMonoxideLqd, _monoxide, ref _spareRoomMonoxideMass, ref _maxCapacityMonoxideMass);
                GetResourceMass(resMan, ResourceName.NeonLqd, _neon, ref _spareRoomNeonMass, ref _maxCapacityNeonMass);
                GetResourceMass(resMan, ResourceName.NitrogenLqd, _nitrogen, ref _spareRoomNitrogenMass, ref _maxCapacityNitrogenMass);
                GetResourceMass(resMan, ResourceName.Nitrogen15Lqd, _nitrogen15, ref _spareRoomNitrogen15Mass, ref _maxCapacityNitrogen15Mass);
                GetResourceMass(resMan, ResourceName.OxygenLqd, _oxygen, ref _spareRoomOxygenMass, ref _maxCapacityOxygenMass);
                GetResourceMass(resMan, ResourceName.WaterPure, _water, ref _spareRoomWaterMass, ref _maxCapacityWaterMass);
                GetResourceMass(resMan, ResourceName.WaterHeavy, _heavyWater, ref _spareRoomHeavyWaterMass, ref _maxCapacityHeavyWaterMass);
                GetResourceMass(resMan, ResourceName.XenonLqd, _xenon, ref _spareRoomXenonMass, ref _maxCapacityXenonMass);
                GetResourceMass(resMan, ResourceName.DeuteriumLqd, _deuterium, ref _spareRoomDeuteriumMass, ref _maxCapacityDeuteriumMass);
                GetResourceMass(resMan, ResourceName.KryptonLqd, _krypton, ref _spareRoomKryptonMass, ref _maxCapacityKryptonMass);
                GetResourceMass(resMan, ResourceName.Sodium, _sodium, ref _spareRoomSodiumMass, ref _maxCapacitySodiumMass);
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: ExtractAir GetResourceMass Exception: " + e.Message);
            }

            // determine the amount of resources needed for processing (i.e. intake atmosphere) that the vessel actually holds
            _availableAtmosphereMass = _maxCapacityAtmosphereMass - _spareRoomAtmosphereMass;
            if (_scoopAnimation != null)
            {
                var animationState = _scoopAnimation[animName];
                normalizedTime = animationState.normalizedTime <= 0
                    ? isDeployed ? 1 : 0
                    : animationState.normalizedTime;
            }
            else
                normalizedTime = 1;

            // intake can only function when heading towards orbital path
            _intakeModifier = _scoopAnimation == null ? 1 : Math.Max(0, Vector3d.Dot(part.transform.up, part.vessel.obt_velocity.normalized));

            try
            {
                // calculate build in scoop capacity
                buildInAirIntake = normalizedTime <= 0.2 ? 0 :
                    AtmosphericFloatCurves.GetAtmosphericGasDensityKgPerCubicMeter(_vessel) * (0.1 + _vessel.obt_speed) * surfaceArea * _intakeModifier * Math.Sqrt((normalizedTime - 0.2) * 1.25);
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: ExtractAir GetAtmosphericGasDensityKgPerCubicMeter Exception: " + e.Message);
            }


            atmosphereConsumptionRatio = _current_rate > 0
                        ? Math.Min(_current_rate, buildInAirIntake + _availableAtmosphereMass) / _current_rate
                        : 0;

            _fixedConsumptionRate = _current_rate * atmosphereConsumptionRatio;

            // begin the intake atmosphere processing
            // check if there is anything to consume and if there is spare room for at least one of the products
            if (_fixedConsumptionRate > 0 && (
                _spareRoomAmmoniaMass > 0 ||
                _spareRoomArgonMass > 0 ||
                _spareRoomChlorineMass > 0 ||
                _spareRoomHydrogenMass > 0 ||
                _spareRoomHelium3Mass > 0 ||
                _spareRoomHelium4Mass > 0 ||
                _spareRoomMonoxideMass > 0 ||
                _spareRoomNitrogenMass > 0 ||
                _spareRoomNitrogen15Mass > 0 ||
                _spareRoomDioxideMass > 0 ||
                _spareRoomMethaneMass > 0 ||
                _spareRoomNeonMass > 0 ||
                _spareRoomWaterMass > 0 ||
                _spareRoomHeavyWaterMass > 0 ||
                _spareRoomOxygenMass > 0 ||
                _spareRoomXenonMass > 0 ||
                _spareRoomDeuteriumMass > 0 ||
                _spareRoomKryptonMass > 0 ||
                _spareRoomSodiumMass > 0))
            {
                /* Now to get the actual percentages from AtmosphericResourceHandler Freethinker extended.
                 * Calls getAtmosphericResourceContent which calls getAtmosphericCompositionForBody which (if there's no definition, i.e. we're using a custom solar system
                 * with weird and fantastic new planets) in turn calls the new GenerateCompositionFromCelestialBody function Freethinker created, which creates a composition
                 * for the upper-level functions based on the planet's size and temperatures. So even though this is calling one method, it's actually going through two or three
                 *  total. Since we like CPUs and want to save them the hassle, let's close this off behind a cheap check.
                */
                if (FlightGlobals.currentMainBody.flightGlobalsIndex != lastBodyID) // did we change a SOI since last time? If yes, get new percentages. Should work the first time as well, since lastBodyID starts as -1, while bodies in the list start at 0
                    try
                    {
                        Debug.Log("[KSPI]: looking up Atmosphere contents for " + FlightGlobals.currentMainBody.name);

                        // remember, all these are persistent. Once we get them, we won't need to calculate them again until we change SOI
                        _ammoniaPercentage = AtmosphericResourceHandler.GetAtmosphericResourceContent(FlightGlobals.currentMainBody, KITResourceSettings.AmmoniaLqd);
                        _argonPercentage = AtmosphericResourceHandler.GetAtmosphericResourceContent(FlightGlobals.currentMainBody, KITResourceSettings.ArgonLqd);
                        _chlorinePercentage = AtmosphericResourceHandler.GetAtmosphericResourceContent(FlightGlobals.currentMainBody, KITResourceSettings.ChlorineGas);
                        _monoxidePercentage = AtmosphericResourceHandler.GetAtmosphericResourceContent(FlightGlobals.currentMainBody, KITResourceSettings.CarbonMonoxideLqd);
                        _dioxidePercentage = AtmosphericResourceHandler.GetAtmosphericResourceContent(FlightGlobals.currentMainBody, KITResourceSettings.CarbonDioxideLqd);
                        _helium3Percentage = AtmosphericResourceHandler.GetAtmosphericResourceContent(FlightGlobals.currentMainBody, KITResourceSettings.Helium3Lqd);
                        _helium4Percentage = AtmosphericResourceHandler.GetAtmosphericResourceContent(FlightGlobals.currentMainBody, KITResourceSettings.Helium4Lqd);
                        _hydrogenPercentage = AtmosphericResourceHandler.GetAtmosphericResourceContent(FlightGlobals.currentMainBody, KITResourceSettings.HydrogenLqd);
                        _methanePercentage = AtmosphericResourceHandler.GetAtmosphericResourceContent(FlightGlobals.currentMainBody, KITResourceSettings.MethaneLqd);
                        _neonPercentage = AtmosphericResourceHandler.GetAtmosphericResourceContent(FlightGlobals.currentMainBody, KITResourceSettings.NeonLqd);
                        _nitrogenPercentage = AtmosphericResourceHandler.GetAtmosphericResourceContent(FlightGlobals.currentMainBody, KITResourceSettings.NitrogenLqd);
                        _nitrogen15Percentage = AtmosphericResourceHandler.GetAtmosphericResourceContent(FlightGlobals.currentMainBody, KITResourceSettings.Nitrogen15Lqd);
                        _oxygenPercentage = AtmosphericResourceHandler.GetAtmosphericResourceContent(FlightGlobals.currentMainBody, KITResourceSettings.OxygenLqd);
                        _waterPercentage = AtmosphericResourceHandler.GetAtmosphericResourceContent(FlightGlobals.currentMainBody, KITResourceSettings.WaterPure);
                        _heavywaterPercentage = AtmosphericResourceHandler.GetAtmosphericResourceContent(FlightGlobals.currentMainBody, KITResourceSettings.WaterHeavy);
                        _xenonPercentage = AtmosphericResourceHandler.GetAtmosphericResourceContent(FlightGlobals.currentMainBody, KITResourceSettings.XenonLqd);
                        _deuteriumPercentage = AtmosphericResourceHandler.GetAtmosphericResourceContent(FlightGlobals.currentMainBody, KITResourceSettings.DeuteriumLqd);
                        _kryptonPercentage = AtmosphericResourceHandler.GetAtmosphericResourceContent(FlightGlobals.currentMainBody, KITResourceSettings.KryptonLqd);
                        _sodiumPercentage = AtmosphericResourceHandler.GetAtmosphericResourceContent(FlightGlobals.currentMainBody, KITResourceSettings.Sodium);

                        lastBodyID = FlightGlobals.currentMainBody.flightGlobalsIndex; // reassign the id of current body to the lastBodyID variable, ie. remember this planet, so that we skip this check next time!
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("[KSPI]: ExtractAir getAtmosphericResourceContent Exception: " + e.Message);
                    }

                // how much of the consumed atmosphere is going to end up as these?
                var fixedMaxAmmoniaRate = _fixedConsumptionRate * _ammoniaPercentage;
                var fixedMaxArgonRate = _fixedConsumptionRate * _argonPercentage;
                var fixedMaxChlorineRate = _fixedConsumptionRate * _chlorinePercentage;
                var fixedMaxDioxideRate = _fixedConsumptionRate * _dioxidePercentage;
                var fixedMaxHelium3Rate = _fixedConsumptionRate * _helium3Percentage;
                var fixedMaxHelium4Rate = _fixedConsumptionRate * _helium4Percentage;
                var fixedMaxHydrogenRate = _fixedConsumptionRate * _hydrogenPercentage;
                var fixedMaxMethaneRate = _fixedConsumptionRate * _methanePercentage;
                var fixedMaxMonoxideRate = _fixedConsumptionRate * _monoxidePercentage;
                var fixedMaxNeonRate = _fixedConsumptionRate * _neonPercentage;
                var fixedMaxNitrogenRate = _fixedConsumptionRate * _nitrogenPercentage;
                var fixedMaxNitrogen15Rate = _fixedConsumptionRate * _nitrogen15Percentage;
                var fixedMaxOxygenRate = _fixedConsumptionRate * _oxygenPercentage;
                var fixedMaxWaterRate = _fixedConsumptionRate * _waterPercentage;
                var fixedMaxHeavyWaterRate = _fixedConsumptionRate * _heavywaterPercentage;
                var fixedMaxXenonRate = _fixedConsumptionRate * _xenonPercentage;
                var fixedMaxDeuteriumRate = _fixedConsumptionRate * _deuteriumPercentage;
                var fixedMaxKryptonRate = _fixedConsumptionRate * _kryptonPercentage;
                var fixedMaxSodiumRate = _fixedConsumptionRate * _sodiumPercentage;

                // how much can we add to the tanks per cycle? If allowOverflow is on, just push it all in, regardless of if the tank can hold the amount. Otherwise adjust accordingly
                var fixedMaxPossibleAmmoniaRate = allowOverflow ? fixedMaxAmmoniaRate : Math.Min(_spareRoomAmmoniaMass, fixedMaxAmmoniaRate);
                var fixedMaxPossibleArgonRate = allowOverflow ? fixedMaxArgonRate : Math.Min(_spareRoomArgonMass, fixedMaxArgonRate);
                var fixedMaxPossibleChlorineRate = allowOverflow ? fixedMaxChlorineRate : Math.Min(_spareRoomChlorineMass, fixedMaxChlorineRate);
                var fixedMaxPossibleDioxideRate = allowOverflow ? fixedMaxDioxideRate : Math.Min(_spareRoomDioxideMass, fixedMaxDioxideRate);
                var fixedMaxPossibleHelium3Rate = allowOverflow ? fixedMaxHelium3Rate : Math.Min(_spareRoomHelium3Mass, fixedMaxHelium3Rate);
                var fixedMaxPossibleHelium4Rate = allowOverflow ? fixedMaxHelium4Rate : Math.Min(_spareRoomHelium4Mass, fixedMaxHelium4Rate);
                var fixedMaxPossibleHydrogenRate = allowOverflow ? fixedMaxHydrogenRate : Math.Min(_spareRoomHydrogenMass, fixedMaxHydrogenRate);
                var fixedMaxPossibleMethaneRate = allowOverflow ? fixedMaxMethaneRate : Math.Min(_spareRoomMethaneMass, fixedMaxMethaneRate);
                var fixedMaxPossibleMonoxideRate = allowOverflow ? fixedMaxMonoxideRate : Math.Min(_spareRoomMonoxideMass, fixedMaxMonoxideRate);
                var fixedMaxPossibleNeonRate = allowOverflow ? fixedMaxNeonRate : Math.Min(_spareRoomNeonMass, fixedMaxNeonRate);
                var fixedMaxPossibleNitrogenRate = allowOverflow ? fixedMaxNitrogenRate : Math.Min(_spareRoomNitrogenMass, fixedMaxNitrogenRate);
                var fixedMaxPossibleNitrogen15Rate = allowOverflow ? fixedMaxNitrogen15Rate : Math.Min(_spareRoomNitrogen15Mass, fixedMaxNitrogen15Rate);
                var fixedMaxPossibleOxygenRate = allowOverflow ? fixedMaxOxygenRate : Math.Min(_spareRoomOxygenMass, fixedMaxOxygenRate);
                var fixedMaxPossibleWaterRate = allowOverflow ? fixedMaxWaterRate : Math.Min(_spareRoomWaterMass, fixedMaxWaterRate);
                var fixedMaxPossibleHeavyWaterRate = allowOverflow ? fixedMaxHeavyWaterRate : Math.Min(_spareRoomHeavyWaterMass, fixedMaxHeavyWaterRate);
                var fixedMaxPossibleXenonRate = allowOverflow ? fixedMaxXenonRate : Math.Min(_spareRoomXenonMass, fixedMaxXenonRate);
                var fixedMaxPossibleDeuteriumRate = allowOverflow ? fixedMaxDeuteriumRate : Math.Min(_spareRoomDeuteriumMass, fixedMaxDeuteriumRate);
                var fixedMaxPossibleKryptonRate = allowOverflow ? fixedMaxKryptonRate : Math.Min(_spareRoomKryptonMass, fixedMaxKryptonRate);
                var fixedMaxPossibleSodiumRate = allowOverflow ? fixedMaxSodiumRate : Math.Min(_spareRoomSodiumMass, fixedMaxSodiumRate);

                // Check if the denominator for each is zero (in that case, assign zero outright, so that we don't end up with an infinite mess on our hands)
                var ammRatio = (fixedMaxAmmoniaRate == 0) ? 0 : fixedMaxPossibleAmmoniaRate / fixedMaxAmmoniaRate;
                var arRatio = (fixedMaxArgonRate == 0) ? 0 : fixedMaxPossibleArgonRate / fixedMaxArgonRate;
                var chlRatio = (fixedMaxChlorineRate == 0) ? 0 : fixedMaxPossibleChlorineRate / fixedMaxChlorineRate;
                var dioxRatio = (fixedMaxDioxideRate == 0) ? 0 : fixedMaxPossibleDioxideRate / fixedMaxDioxideRate;
                var he3Ratio = (fixedMaxHelium3Rate == 0) ? 0 : fixedMaxPossibleHelium3Rate / fixedMaxHelium3Rate;
                var he4Ratio = (fixedMaxHelium4Rate == 0) ? 0 : fixedMaxPossibleHelium4Rate / fixedMaxHelium4Rate;
                var hydroRatio = (fixedMaxHydrogenRate == 0) ? 0 : fixedMaxPossibleHydrogenRate / fixedMaxHydrogenRate;
                var methRatio = (fixedMaxMethaneRate == 0) ? 0 : fixedMaxPossibleMethaneRate / fixedMaxMethaneRate;
                var monoxRatio = (fixedMaxMonoxideRate == 0) ? 0 : fixedMaxPossibleMonoxideRate / fixedMaxMonoxideRate;
                var neonRatio = (fixedMaxNeonRate == 0) ? 0 : fixedMaxPossibleNeonRate / fixedMaxNeonRate;
                var nitroRatio = (fixedMaxNitrogenRate == 0) ? 0 : fixedMaxPossibleNitrogenRate / fixedMaxNitrogenRate;
                var nitro15Ratio = (fixedMaxNitrogen15Rate == 0) ? 0 : fixedMaxPossibleNitrogen15Rate / fixedMaxNitrogen15Rate;
                var oxyRatio = (fixedMaxOxygenRate == 0) ? 0 : fixedMaxPossibleOxygenRate / fixedMaxOxygenRate;
                var waterRatio = (fixedMaxWaterRate == 0) ? 0 : fixedMaxPossibleWaterRate / fixedMaxWaterRate;
                var heavywaterRatio = (fixedMaxHeavyWaterRate == 0) ? 0 : fixedMaxPossibleHeavyWaterRate / fixedMaxHeavyWaterRate;
                var xenonRatio = (fixedMaxXenonRate == 0) ? 0 : fixedMaxPossibleXenonRate / fixedMaxXenonRate;
                var deuteriumRatio = (fixedMaxDeuteriumRate == 0) ? 0 : fixedMaxPossibleDeuteriumRate / fixedMaxDeuteriumRate;
                var kryptonRatio = (fixedMaxKryptonRate == 0) ? 0 : fixedMaxPossibleKryptonRate / fixedMaxKryptonRate;
                var sodiumRatio = (fixedMaxSodiumRate == 0) ? 0 : fixedMaxPossibleSodiumRate / fixedMaxSodiumRate;

                /* finds a non-zero minimum of all the ratios (calculated above, as fixedMaxPossibleZZRate / fixedMaxZZRate). It needs to be non-zero
                * so that the collecting works even when some of consitutents are absent from the local atmosphere (ie. when their definition is zero).
                * Otherwise the consumptionStorageRatio would be zero and thus no atmosphere would be consumed. */
                _consumptionStorageRatio = new[] { ammRatio, arRatio, dioxRatio, he3Ratio, he4Ratio, hydroRatio, methRatio, monoxRatio, neonRatio, nitroRatio, nitro15Ratio, oxyRatio, waterRatio, heavywaterRatio, xenonRatio, deuteriumRatio, kryptonRatio, sodiumRatio }.Where(x => x > 0).Min();

                var maxAtmosphericConsumptionRate = _consumptionStorageRatio * _fixedConsumptionRate;

                // calculate atmospheric consumption per second
                _atmosphereConsumptionRate = buildInAirIntake;

                // calculate missing atmospheric which can be extracted from air intakes
                var remainingConsumptionNeeded = Math.Max(0, buildInAirIntake - maxAtmosphericConsumptionRate);

                // add the consumed atmosphere total atmospheric consumption rate
                _atmosphereConsumptionRate += resMan.Consume(ResourceName.IntakeAtmosphere, remainingConsumptionNeeded / _atmosphere.density) / _atmosphere.density;

                // produce the resources
                _ammoniaProductionRate = _ammoniaPercentage <= 0 ? 0 : _atmosphereConsumptionRate * _ammoniaPercentage;
                _argonProductionRate = _argonPercentage <= 0 ? 0 : _atmosphereConsumptionRate * _argonPercentage;
                _chlorineProductionRate = _chlorinePercentage <= 0 ? 0 : _atmosphereConsumptionRate * _chlorinePercentage;
                _dioxideProductionRate = _dioxidePercentage <= 0 ? 0 : _atmosphereConsumptionRate * _dioxidePercentage;
                _helium3ProductionRate = _helium3Percentage <= 0 ? 0 : _atmosphereConsumptionRate * _helium3Percentage;
                _helium4ProductionRate = _helium4Percentage <= 0 ? 0 : _atmosphereConsumptionRate * _helium4Percentage;
                _hydrogenProductionRate = _hydrogenPercentage <= 0 ? 0 : _atmosphereConsumptionRate * _hydrogenPercentage;
                _methaneProductionRate = _methanePercentage <= 0 ? 0 : _atmosphereConsumptionRate * _methanePercentage;
                _neonProductionRate = _neonPercentage <= 0 ? 0 : _atmosphereConsumptionRate * _neonPercentage;
                _nitrogenProductionRate = _nitrogenPercentage <= 0 ? 0 : -_atmosphereConsumptionRate * _nitrogenPercentage;
                _nitrogen15ProductionRate = _nitrogen15Percentage <= 0 ? 0 : _atmosphereConsumptionRate * _nitrogen15Percentage;
                _oxygenProductionRate = _oxygenPercentage <= 0 ? 0 : _atmosphereConsumptionRate * _oxygenPercentage;
                _waterProductionRate = _waterPercentage <= 0 ? 0 : _atmosphereConsumptionRate * _waterPercentage;
                _xenonProductionRate = _xenonPercentage <= 0 ? 0 : _atmosphereConsumptionRate * _xenonPercentage;
                _deuteriumProductionRate = _deuteriumPercentage <= 0 ? 0 : _atmosphereConsumptionRate * _deuteriumPercentage;
                _kryptonProductionRate = _kryptonPercentage <= 0 ? 0 : _atmosphereConsumptionRate * _kryptonPercentage;
                _sodiumProductionRate = _sodiumPercentage <= 0 ? 0 : _atmosphereConsumptionRate * _sodiumPercentage;

                resMan.Produce(ResourceName.AmmoniaLqd, _ammoniaProductionRate / _ammonia.density);
                resMan.Produce(ResourceName.ArgonLqd, _argonPercentage / _argon.density);
                resMan.Produce(ResourceName.ChlorineGas, _chlorinePercentage / _chlorine.density);
                resMan.Produce(ResourceName.CarbonDioxideLqd, _dioxidePercentage / _dioxide.density);
                resMan.Produce(ResourceName.Helium3Lqd, _helium3Percentage / _helium3.density);
                resMan.Produce(ResourceName.Helium4Lqd, _helium4Percentage / _helium4.density);
                resMan.Produce(ResourceName.MethaneLqd, _methanePercentage / _methane.density);
                resMan.Produce(ResourceName.CarbonMonoxideLqd, _monoxidePercentage / _monoxide.density);
                resMan.Produce(ResourceName.NeonLqd, _neonPercentage / _neon.density);
                resMan.Produce(ResourceName.NitrogenLqd, _nitrogenPercentage / _nitrogen.density);
                resMan.Produce(ResourceName.Nitrogen15Lqd, _nitrogen15Percentage / _nitrogen15.density);
                resMan.Produce(ResourceName.OxygenLqd, _oxygenPercentage / _oxygen.density);
                resMan.Produce(ResourceName.WaterPure, _waterPercentage / _water.density);
                resMan.Produce(ResourceName.WaterHeavy, _heavywaterPercentage / _heavyWater.density);
                resMan.Produce(ResourceName.XenonLqd, _xenonPercentage / _xenon.density);
                resMan.Produce(ResourceName.DeuteriumLqd, _deuteriumPercentage / _deuterium.density);
                resMan.Produce(ResourceName.KryptonLqd, _kryptonPercentage / _krypton.density);
                resMan.Produce(ResourceName.Sodium, _sodiumPercentage / _sodium.density);
            }
            else
            {
                _atmosphereConsumptionRate = 0;
                _ammoniaProductionRate = 0;
                _argonProductionRate = 0;
                _dioxideProductionRate = 0;
                _helium3ProductionRate = 0;
                _helium4ProductionRate = 0;
                _hydrogenProductionRate = 0;
                _methaneProductionRate = 0;
                _monoxideProductionRate = 0;
                _neonProductionRate = 0;
                _nitrogenProductionRate = 0;
                _nitrogen15ProductionRate = 0;
                _oxygenProductionRate = 0;
                _waterProductionRate = 0;
                _heavyWaterProductionRate = 0;
                _xenonProductionRate = 0;
                _deuteriumProductionRate = 0;
                _kryptonProductionRate = 0;
                _sodiumProductionRate = 0;
            }
        }


        public override void UpdateGUI()
        {
            base.UpdateGUI();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Power"), _bold_label, GUILayout.Width(labelWidth));//"Power"
            GUILayout.Label(PluginHelper.GetFormattedPowerString(CurrentPower) + "/" + PluginHelper.GetFormattedPowerString(_effectiveMaxPower), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_AtmoConsumption"), _bold_label, GUILayout.Width(labelWidth));//"Intake Atmo. Consumption"
            GUILayout.Label((GetValueText(_atmosphereConsumptionRate * GameConstants.SecondsInHour)) + " mT/hour", _value_label, GUILayout.Width(valueWidth));//
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_AtmoAvailable"), _bold_label, GUILayout.Width(labelWidth));//"Intake Atmo. Available"
            GUILayout.Label(GetValueText(_availableAtmosphereMass) + " mT / " + GetValueText(_maxCapacityAtmosphereMass) + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Name"), _bold_label, GUILayout.Width(valueWidth));                // "Name"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Abundance"), _bold_label, GUILayout.Width(valueWidth));           // "Abundance"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_SpareRoom"), _bold_label, GUILayout.Width(valueWidth));           // "Spare Room"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Stored"), _bold_label, GUILayout.Width(valueWidth));              // "Stored"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_MaxCapacity"), _bold_label, GUILayout.Width(valueWidth));         // "Max Capacity"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Productionpersecond"), _bold_label, GUILayout.Width(valueWidth)); // "Production per second"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Productionperhour"), _bold_label, GUILayout.Width(valueWidth));   // "Production per hour"
            GUILayout.EndHorizontal();

            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Hydrogen"), _hydrogenPercentage, _hydrogenProductionRate, _spareRoomHydrogenMass, _maxCapacityHydrogenMass);//"Hydrogen"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Deuterium"), _deuteriumPercentage, _deuteriumProductionRate, _spareRoomDeuteriumMass, _maxCapacityDeuteriumMass);//"Deuterium"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Helium3"), _helium3Percentage, _helium3ProductionRate, _spareRoomHelium3Mass, _maxCapacityHelium3Mass);//"Helium-3"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Helium"), _helium4Percentage, _helium4ProductionRate, _spareRoomHelium4Mass, _maxCapacityHelium4Mass);//"Helium"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Nitrogen"), _nitrogenPercentage, _nitrogenProductionRate, _spareRoomNitrogenMass, _maxCapacityNitrogenMass);//"Nitrogen"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Nitrogen15"), _nitrogen15Percentage, _nitrogen15ProductionRate, _spareRoomNitrogen15Mass, _maxCapacityNitrogen15Mass);//"Nitrogen-15"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Oxygen"), _oxygenPercentage, _oxygenProductionRate, _spareRoomOxygenMass, _maxCapacityOxygenMass);//"Oxygen"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Argon"), _argonPercentage, _argonProductionRate, _spareRoomArgonMass, _maxCapacityArgonMass);//"Argon"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Chlorine"), _chlorinePercentage, _chlorineProductionRate, _spareRoomChlorineMass, _maxCapacityChlorineMass);//"Chlorine"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Neon"), _neonPercentage, _neonProductionRate, _spareRoomNeonMass, _maxCapacityNeonMass);//"Neon"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Krypton"), _kryptonPercentage, _kryptonProductionRate, _spareRoomKryptonMass, _maxCapacityKryptonMass);//"Krypton"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Ammonia"), _ammoniaPercentage, _ammoniaProductionRate, _spareRoomAmmoniaMass, _maxCapacityAmmoniaMass);//"Ammonia"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Water"), _waterPercentage, _waterProductionRate, _spareRoomWaterMass, _maxCapacityWaterMass);//"Water"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_HeavyWater"), _heavywaterPercentage, _heavyWaterProductionRate, _spareRoomHeavyWaterMass, _maxCapacityHeavyWaterMass);//"Heavy Water"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_CarbonMonoxide"), _monoxidePercentage, _monoxideProductionRate, _spareRoomMonoxideMass, _maxCapacityMonoxideMass);//"Carbon Monoxide"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_CarbonDioxide"), _dioxidePercentage, _dioxideProductionRate, _spareRoomDioxideMass, _maxCapacityDioxideMass);//"Carbon Dioxide"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Methane"), _methanePercentage, _methaneProductionRate, _spareRoomMethaneMass, _maxCapacityMethaneMass);//"Methane"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Xenon"), _xenonPercentage, _xenonProductionRate, _spareRoomXenonMass, _maxCapacityXenonMass);//"Xenon"
            DisplayResourceExtraction(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Sodium"), _sodiumPercentage, _sodiumProductionRate, _spareRoomSodiumMass, _maxCapacitySodiumMass);//"Sodium"
        }

        private void DisplayResourceExtraction(string resourceName, double percentage, double productionRate, double spareRoom, double maximumCapacity)
        {
            if (percentage <= 0)
                return;

            GUILayout.BeginHorizontal();
            GUILayout.Label(resourceName, _value_label, GUILayout.Width(valueWidth));
            GUILayout.Label(GetValueText(percentage * 100) + "%", _value_label, GUILayout.Width(valueWidth));

            if (maximumCapacity > 0)
            {
                if (spareRoom > 0)
                    GUILayout.Label(GetValueText(spareRoom) + " t", _value_label_green, GUILayout.Width(valueWidth));
                else
                    GUILayout.Label("0", _value_label_red, GUILayout.Width(valueWidth));

                GUILayout.Label(GetValueText(maximumCapacity - spareRoom) + " t", _value_label, GUILayout.Width(valueWidth));
                GUILayout.Label(GetValueText(maximumCapacity) + " t", _value_label, GUILayout.Width(valueWidth));
            }
            else
            {
                GUILayout.Label("", _value_label_red, GUILayout.Width(valueWidth));
                GUILayout.Label("", _value_label_red, GUILayout.Width(valueWidth));
                GUILayout.Label("0", _value_label_red, GUILayout.Width(valueWidth));
            }

            if (productionRate > 0)
            {
                GUILayout.Label(GetValueText(productionRate) + " U/s", _value_label, GUILayout.Width(valueWidth));
                GUILayout.Label(GetValueText(productionRate * GameConstants.SecondsInHour) + " U/h", _value_label, GUILayout.Width(valueWidth));
            }
            else
            {
                GUILayout.Label("", _value_label_red, GUILayout.Width(valueWidth));
                GUILayout.Label("", _value_label_red, GUILayout.Width(valueWidth));
            }

            GUILayout.EndHorizontal();
        }

        private string GetValueText(double value)
        {
            return value >= 0.00000005 ? value.ToString("##.########") : ((float)value).ToString(CultureInfo.InvariantCulture);
        }

        private void UpdateStatusMessage()
        {
            if (normalizedTime <= 0)
                _status = Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Statumsg1");//"Scoop is not deployed"
            else if (_intakeModifier <= 0)
                _status = Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Statumsg2");//"Scoop is not heading into orbital direction"
            else if (_atmosphereConsumptionRate > 0)
                _status = Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Statumsg3");//"Extracting atmosphere"
            else if (CurrentPower <= 0.01 * PowerRequirements)
                _status = Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Statumsg4");//"Insufficient Power"
            else
                _status = Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Statumsg5");//"Insufficient Storage, try allowing overflow"
        }

        public void PrintMissingResources()
        {
            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_PostMsg") + " " + KITResourceSettings.IntakeAtmosphere, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
        }

        public void Update()
        {
            // Sail deployment GUI
            Events[nameof(DeployScoop)].active = _scoopAnimation != null && !isDeployed;
            Events[nameof(RetractScoop)].active = _scoopAnimation != null && isDeployed;
        }

        private static void RunAnimation(string animationName, Animation anim, float speed, float aTime)
        {
            if (animationName == null || anim == null || string.IsNullOrEmpty(animationName))
                return;

            anim[animationName].speed = speed;
            if (anim.IsPlaying(animationName))
                return;

            anim[animationName].wrapMode = WrapMode.Default;
            anim[animationName].normalizedTime = aTime;
            anim.Blend(animationName, 1);
        }
    }
}
