using System;
using System.Linq;
using KIT.Refinery;
using KIT.Refinery.Activity;
using KIT.Resources;
using KIT.ResourceScheduler;
using KSP.Localization;
using UnityEngine;

namespace KIT.Science
{
    class ScienceModule : PartModule, IKITModule, ITelescopeController, IUpgradeableModule
    {
        // persistant true
        [KSPField(isPersistant = true)]
        public bool IsEnabled;
        [KSPField(isPersistant = true)]
        public int active_mode;
        [KSPField(isPersistant = true)]
        public double electrical_power_ratio;
        //[KSPField(isPersistant = true)]
        //public double science_to_add;

        [KSPField(isPersistant = false, guiActive = true, guiName = "#LOC_KSPIE_ScienceModule_Activity")]//Activity
        public string statusTitle;
        [KSPField(isPersistant = false, guiActive = true, guiName = "#LOC_KSPIE_ScienceModule_Power")]//Power
        public string powerStr;
        //[KSPField(isPersistant = false, guiActive = false, guiName = "Data Scan Rate")]
        //public string scienceRate;
        //[KSPField(isPersistant = false, guiActive = false, guiName = "Scanned Data")]
        //public string collectedScience;
        [KSPField(isPersistant = false, guiActive = true, guiName = "R")]
        public string reprocessingRate;
        [KSPField(isPersistant = false, guiActive = true, guiName = "A")]
        public string antimatterRate;
        [KSPField(isPersistant = false, guiActive = true, guiName = "E")]
        public string electrolysisRate;
        [KSPField(isPersistant = false, guiActive = true, guiName = "C")]
        public string centrifugeRate;
        [KSPField(isPersistant = false, guiActive = true, guiName = "#LOC_KSPIE_ScienceModule_AntimatterEfficiency")]//Antimatter Efficiency
        public string antimatterProductionEfficiency;
        //[KSPField(isPersistant = false)]
        //public string beginResearchName = "Begin Scanning";
        [KSPField(isPersistant = false, guiActive = true, guiName = "#LOC_KSPIE_ScienceModule_DataProcessingMultiplier")] //Data Processing Multiplier
        public float dataProcessingMultiplier;


        [KSPField(isPersistant = false)]
        public string Mk2Tech = "longTermScienceTech";
        [KSPField(isPersistant = false)]
        public string Mk3Tech = "scientificOutposts";
        [KSPField(isPersistant = false)]
        public string Mk4Tech = "highEnergyScience";
        [KSPField(isPersistant = false)]
        public string Mk5Tech = "appliedHighEnergyPhysics";
        [KSPField(isPersistant = false)]
        public string Mk6Tech = "ultraHighEnergyPhysics";
        [KSPField(isPersistant = false)]
        public string Mk7Tech = "extremeHighEnergyPhysics";


        [KSPField(isPersistant = false)]
        public int Mk1ScienceCap = 1000;
        [KSPField(isPersistant = false)]
        public int Mk2ScienceCap = 1600;
        [KSPField(isPersistant = false)]
        public int Mk3ScienceCap = 2500;
        [KSPField(isPersistant = false)]
        public int Mk4ScienceCap = 4000;
        [KSPField(isPersistant = false)]
        public int Mk5ScienceCap = 6350;
        [KSPField(isPersistant = false)]
        public int Mk6ScienceCap = 10000;
        [KSPField(isPersistant = false)]
        public int Mk7ScienceCap = 16000;


        // persistant false
        [KSPField(isPersistant = false)]
        public string animName1 = "";
        [KSPField(isPersistant = false)]
        public string animName2 = "";
        [KSPField(isPersistant = false)]
        public string upgradeTechReq = null;
        [KSPField(isPersistant = false)]
        public float upgradeCost = 20;
        [KSPField(isPersistant = true)]
        public bool isupgraded;
        [KSPField(isPersistant = false)]
        public float powerReqMult = 1;
        [KSPField(isPersistant = false)]
        public float baseDataStorage = 750;

        protected int techLevel;
        protected float megajoules_supplied = 0;
        protected String[] modes = { "Scanning", "Reprocessing", "Producing Antimatter", "Electrolysing", "Centrifuging" };
        protected double science_rate_f;
        protected double reprocessing_rate_f;
        protected float crew_capacity_ratio;
        protected double antimatter_rate_f;

        protected float electrolysis_rate_f;
        protected double deut_rate_f;
        protected bool play_down = true;
        protected bool hasRequiredUpgrade;

        protected Animation anim;
        protected Animation anim2;
        protected NuclearFuelReprocessor reprocessor;
        protected AntimatterGenerator antimatterGenerator;
        protected ModuleScienceConverter moduleScienceConverter;
        protected ModuleScienceLab moduleScienceLab;


        public bool CanProvideTelescopeControl => part.protoModuleCrew.Count > 0;

        /*
        [KSPEvent(guiActive = true, guiName = "Begin Scanning", active = true)]
        public void BeginResearch()
        {
            if (crew_capacity_ratio == 0) return;

            IsEnabled = true;
            active_mode = 0;

            anim[animName1].speed = 1;
            anim[animName1].normalizedTime = 0;
            anim.Blend(animName1, 2);
            anim2[animName2].speed = 1;
            anim2[animName2].normalizedTime = 0;
            anim2.Blend(animName2, 2);
            play_down = true;
        }
        */

        private void DetermineTechLevel()
        {
            techLevel = 1;
            if (PluginHelper.UpgradeAvailable(Mk2Tech))
                techLevel++;
            if (PluginHelper.UpgradeAvailable(Mk3Tech))
                techLevel++;
            if (PluginHelper.UpgradeAvailable(Mk4Tech))
                techLevel++;
            if (PluginHelper.UpgradeAvailable(Mk5Tech))
                techLevel++;
            if (PluginHelper.UpgradeAvailable(Mk6Tech))
                techLevel++;
            if (PluginHelper.UpgradeAvailable(Mk7Tech))
                techLevel++;
        }

        private int GetScienceCap()
        {
            if (techLevel == 1)
                return Mk1ScienceCap;
            if (techLevel == 2)
                return Mk2ScienceCap;
            if (techLevel == 3)
                return Mk3ScienceCap;
            if (techLevel == 4)
                return Mk4ScienceCap;
            if (techLevel == 5)
                return Mk5ScienceCap;
            if (techLevel == 6)
                return Mk6ScienceCap;
            if (techLevel == 7)
                return Mk7ScienceCap;
            return 0;
        }

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_ScienceModule_ReprocessNuclearFuel", active = true)]//Reprocess Nuclear Fuel
        public void ReprocessFuel()
        {
            if (crew_capacity_ratio == 0) return;

            IsEnabled = true;
            active_mode = 1;

            anim[animName1].speed = 1;
            anim[animName1].normalizedTime = 0;
            anim.Blend(animName1, 2);
            anim2[animName2].speed = 1;
            anim2[animName2].normalizedTime = 0;
            anim2.Blend(animName2, 2);
            play_down = true;
        }

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_ScienceModule_ActivateAntimatterProduction", active = true)]//Activate Antimatter Production
        public void ActivateFactory()
        {
            if (crew_capacity_ratio == 0) return;

            IsEnabled = true;
            active_mode = 2;

            anim[animName1].speed = 1;
            anim[animName1].normalizedTime = 0;
            anim.Blend(animName1, 2);
            anim2[animName2].speed = 1;
            anim2[animName2].normalizedTime = 0;
            anim2.Blend(animName2, 2);
            play_down = true;
        }

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_ScienceModule_ActivateElectrolysis", active = true)]//Activate Electrolysis
        public void ActivateElectrolysis()
        {
            if (crew_capacity_ratio == 0) return;

            IsEnabled = true;
            active_mode = 3;

            anim[animName1].speed = 1;
            anim[animName1].normalizedTime = 0;
            anim.Blend(animName1, 2);
            anim2[animName2].speed = 1;
            anim2[animName2].normalizedTime = 0;
            anim2.Blend(animName2, 2);
            play_down = true;
        }

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_ScienceModule_ActivateCentrifuge", active = true)]//Activate Centrifuge
        public void ActivateCentrifuge()
        {
            if (crew_capacity_ratio == 0) return;

            IsEnabled = true;
            active_mode = 4;

            anim[animName1].speed = 1;
            anim[animName1].normalizedTime = 0;
            anim.Blend(animName1, 2);
            anim2[animName2].speed = 1;
            anim2[animName2].normalizedTime = 0;
            anim2.Blend(animName2, 2);
            play_down = true;
        }

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_ScienceModule_StopCurrentActivity", active = false)]//Stop Current Activity
        public void StopActivity()
        {
            IsEnabled = false;
        }

        [KSPEvent(guiActive = true, guiName = "#LOC_KSPIE_ScienceModule_Retrofit", active = true)]//Retrofit
        public void RetrofitEngine()
        {
            if (ResearchAndDevelopment.Instance == null || isupgraded || ResearchAndDevelopment.Instance.Science < upgradeCost) return;

            upgradePartModule();
            ResearchAndDevelopment.Instance.AddScience(-upgradeCost, TransactionReasons.RnDPartPurchase);
        }

        public String UpgradeTechnology => upgradeTechReq;

        public void upgradePartModule()
        {
            //canDeploy = true;
            isupgraded = true;
        }

        public override void OnStart(StartState state)
        {
            moduleScienceLab = part.FindModuleImplementing<ModuleScienceLab>();
            moduleScienceConverter = part.FindModuleImplementing<ModuleScienceConverter>();

            if (moduleScienceConverter != null && moduleScienceLab != null)
            {
                DetermineTechLevel();

                var scienceCap = GetScienceCap();
                moduleScienceLab.dataStorage = scienceCap;
                moduleScienceConverter.scienceCap = scienceCap;

                var deminishingScienceModifier = moduleScienceLab.dataStored >= baseDataStorage ? 1 : moduleScienceLab.dataStored / baseDataStorage;

                dataProcessingMultiplier = moduleScienceLab.dataStored < float.Epsilon ? 0.5f
                    : deminishingScienceModifier * (baseDataStorage / moduleScienceLab.dataStorage) * (moduleScienceLab.dataStorage / moduleScienceLab.dataStored) * 0.5f;
                moduleScienceConverter.dataProcessingMultiplier = dataProcessingMultiplier;
            }

            if (state == StartState.Editor)
            {
                if (this.HasTechsRequiredToUpgrade())
                {
                    isupgraded = true;
                    upgradePartModule();
                }
                return;
            }

            if (isupgraded)
                upgradePartModule();
            else
            {
                if (this.HasTechsRequiredToUpgrade())
                    hasRequiredUpgrade = true;
            }

            // update gui names
            /*
            Events["BeginResearch"].guiName = beginResearchName;
            */

            reprocessor = new NuclearFuelReprocessor();
            reprocessor.Initialize(part);
            antimatterGenerator = new AntimatterGenerator(part, 1, PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.AntiProtium));

            Debug.Log("[KSPI]: ScienceModule on " + part.name + " was Force Activated");
            part.force_activate();

            anim = part.FindModelAnimators(animName1).FirstOrDefault();
            anim2 = part.FindModelAnimators(animName2).FirstOrDefault();
            if (anim != null && anim2 != null)
            {
                if (IsEnabled)
                {
                    //anim [animName1].normalizedTime = 1f;
                    //anim2 [animName2].normalizedTime = 1f;
                    //anim [animName1].speed = -1f;
                    //anim2 [animName2].speed = -1f;
                    anim.Blend(animName1, 1, 0);
                    anim2.Blend(animName2, 1, 0);
                }
                else
                {
                    //anim [animName1].normalizedTime = 0f;
                    //anim2 [animName2].normalizedTime = 0f;
                    //anim [animName1].speed = 1f;
                    //anim2 [animName2].speed = 1f;
                    //anim.Blend (animName1, 0, 0);global_rate_multipliers
                    //anim2.Blend (animName2, 0, 0);
                    play_down = false;
                }
                //anim.Play ();
                //anim2.Play ();
            
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            try
            {
                //Events["BeginResearch"].active = isUpgraded && !IsEnabled;
                Events[nameof(ReprocessFuel)].active = !IsEnabled;
                Events[nameof(ActivateFactory)].active = isupgraded && !IsEnabled;
                Events[nameof(ActivateElectrolysis)].active = false;
                Events[nameof(ActivateCentrifuge)].active = isupgraded && !IsEnabled && vessel.Splashed;
                Events[nameof(StopActivity)].active = isupgraded && IsEnabled;
                Fields[nameof(statusTitle)].guiActive = isupgraded;
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: ScienceModule OnUpdate 1 " + e.Message);
            }

            try
            {
                // only show retrofit button if we can actually upgrade
                Events[nameof(RetrofitEngine)].active = ResearchAndDevelopment.Instance != null && (!isupgraded && ResearchAndDevelopment.Instance.Science >= upgradeCost && hasRequiredUpgrade);
            }
            catch (Exception e)
            {
                Debug.LogError("[KSPI]: ScienceModule OnUpdate 2 " + e.Message);
            }

            if (IsEnabled)
            {
                try
                {
                    //anim [animName1].normalizedTime = 1f;
                    statusTitle = modes[active_mode] + "...";
                }
                catch (Exception e)
                {
                    Debug.LogError("[KSPI]: ScienceModule OnUpdate 3 " + e.Message);
                }

                try
                {
                    /*
                    Fields["scienceRate"].guiActive = false;
                    Fields["collectedScience"].guiActive = false;
                    */
                    Fields[nameof(reprocessingRate)].guiActive = false;
                    Fields[nameof(antimatterRate)].guiActive = false;
                    Fields[nameof(electrolysisRate)].guiActive = false;
                    Fields[nameof(centrifugeRate)].guiActive = false;
                    Fields[nameof(antimatterProductionEfficiency)].guiActive = false;
                    Fields[nameof(powerStr)].guiActive = true;
                }
                catch (Exception e)
                {
                    Debug.LogError("[KSPI]: ScienceModule 4 " + e.Message);
                }

                double currentpowertmp = electrical_power_ratio * PluginSettings.Config.BasePowerConsumption * powerReqMult;

                try
                {
                    powerStr = currentpowertmp.ToString("0.0000") + "MW / " + (powerReqMult * PluginSettings.Config.BasePowerConsumption).ToString("0.0000") + "MW";
                }
                catch (Exception e)
                {
                    Debug.LogError("[KSPI]: ScienceModule 5 " + e.Message);
                }

                /*
                if (active_mode == 0) // Research
                {
                    //Fields["scienceRate"].guiActive = true;
                    //Fields["collectedScience"].guiActive = true;
                    double scienceratetmp = science_rate_f * PluginHelper.SecondsInDay * PluginHelper.getScienceMultiplier(vessel);
                    scienceRate = scienceratetmp.ToString("0.0000") + "/Day";
                    collectedScience = science_to_add.ToString("0.000000");
                }
                else
                */
                if (active_mode == 1) // Fuel Reprocessing
                {
                    try
                    {
                        Fields[nameof(reprocessingRate)].guiActive = true;
                        reprocessingRate = Localizer.Format("#LOC_KSPIE_ScienceModule_Reprocessing", reprocessing_rate_f.ToString("0.0"));// + " Hours Remaining"
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("[KSPI]: ScienceModule Fuel Reprocessing " + e.Message);
                    }
                }
                else if (active_mode == 2) // Antimatter
                {
                    try
                    {
                        currentpowertmp = electrical_power_ratio * PluginSettings.Config.BaseAMFPowerConsumption * powerReqMult;
                        Fields[nameof(antimatterRate)].guiActive = true;
                        Fields[nameof(antimatterProductionEfficiency)].guiActive = true;
                        powerStr = currentpowertmp.ToString("0.00") + "MW / " + (powerReqMult * PluginSettings.Config.BaseAMFPowerConsumption).ToString("0.00") + "MW";
                        antimatterProductionEfficiency = (antimatterGenerator.Efficiency * 100).ToString("0.0000") + "%";
                        double antimatterRatePerDay = antimatter_rate_f * PluginSettings.Config.SecondsInDay;

                        if (antimatterRatePerDay > 0.1)
                            antimatterRate = (antimatterRatePerDay).ToString("0.0000") + " mg/day";
                        else
                        {
                            if (antimatterRatePerDay > 0.1e-3)
                                antimatterRate = (antimatterRatePerDay * 1e3).ToString("0.0000") + " ug/day";
                            else
                                antimatterRate = (antimatterRatePerDay * 1e6).ToString("0.0000") + " ng/day";
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("[KSPI]: ScienceModule Antimatter " + e.Message);
                    }
                }
                else if (active_mode == 3) // Electrolysis
                {
                    currentpowertmp = electrical_power_ratio * PluginSettings.Config.BaseELCPowerConsumption * powerReqMult;
                    Fields[nameof(electrolysisRate)].guiActive = true;
                    double electrolysisratetmp = -electrolysis_rate_f * PluginSettings.Config.SecondsInDay;
                    electrolysisRate = electrolysisratetmp.ToString("0.0") + "mT/day";
                    powerStr = currentpowertmp.ToString("0.00") + "MW / " + (powerReqMult * PluginSettings.Config.BaseELCPowerConsumption).ToString("0.00") + "MW";
                }
                else if (active_mode == 4) // Centrifuge
                {
                    currentpowertmp = electrical_power_ratio * PluginSettings.Config.BaseCentriPowerConsumption * powerReqMult;
                    Fields[nameof(centrifugeRate)].guiActive = true;
                    powerStr = currentpowertmp.ToString("0.00") + "MW / " + (powerReqMult * PluginSettings.Config.BaseCentriPowerConsumption).ToString("0.00") + "MW";
                    double deut_per_hour = deut_rate_f * 3600;
                    centrifugeRate = Localizer.Format("#LOC_KSPIE_ScienceModule_Centrifuge", deut_per_hour.ToString("0.00"));// + " Kg Deuterium/Hour"
                }
                else
                {
                    // nothing
                }
            }
            else
            {
                if (play_down)
                {
                    if (anim != null)
                    {
                        anim[animName1].speed = -1;
                        anim[animName1].normalizedTime = 1;
                        anim.Blend(animName1, 2);
                    }
                    if (anim2 != null)
                    {
                        anim2[animName2].speed = -1;
                        anim2[animName2].normalizedTime = 1;
                        anim2.Blend(animName2, 2);
                    }
                    play_down = false;
                }

                //anim [animName1].normalizedTime = 0;


                try
                {
                    /*
                    Fields["scienceRate"].guiActive = false;
                    Fields["collectedScience"].guiActive = false;
                    */


                    Fields[nameof(reprocessingRate)].guiActive = false;
                    Fields[nameof(antimatterRate)].guiActive = false;
                    Fields[nameof(powerStr)].guiActive = false;
                    Fields[nameof(centrifugeRate)].guiActive = false;
                    Fields[nameof(electrolysisRate)].guiActive = false;
                    Fields[nameof(antimatterProductionEfficiency)].guiActive = false;
                }
                catch (Exception e)
                {
                    Debug.LogError("[KSPI]: OnUpdate Else " + e.Message);
                }

                statusTitle = Localizer.Format(crew_capacity_ratio > 0 ? "#LOC_KSPIE_ScienceModule_Idle" : "#LOC_KSPIE_ScienceModule_Notenoughcrew");
            }
        }

        private float GetKerbalScienceFactor(ProtoCrewMember kerbal)
        {
            float kerbalFactor;

            // initialise with profession
            if (kerbal.experienceTrait.Title == "Scientist")
                kerbalFactor = 1.5f;
            else if (kerbal.experienceTrait.Title == "Engineer")
                kerbalFactor = 1f;
            else
                kerbalFactor = 0.5f;

            // moidy by experience level
            kerbalFactor *= (kerbal.experienceLevel + 10f) / 10f;

            // final modify result by kerbal stupidity (+/- 10%)
            return kerbalFactor * (1.1f - (kerbal.stupidity / 5f));
        }

        public bool ModuleConfiguration(out int priority, out bool supplierOnly, out bool hasLocalResources)
        {
            priority = 4;
            supplierOnly = false;
            hasLocalResources = false;

            return true;
        }

        public void KITFixedUpdate(IResourceManager resMan)
        {
            double global_rate_multipliers = 1;
            crew_capacity_ratio = part.protoModuleCrew.Count / ((float)part.CrewCapacity);
            global_rate_multipliers = global_rate_multipliers * crew_capacity_ratio;

            if (!IsEnabled) return;

            /*
            if (active_mode == 0)  // Research
            {
                var powerRequest = powerReqMult * PluginHelper.BasePowerConsumption * TimeWarp.fixedDeltaTime;

                double electrical_power_provided = CheatOptions.InfiniteElectricity
                    ? powerRequest
                    : consumeFNResource(powerRequest, ResourceManager.FNRESOURCE_MEGAJOULES);

                electrical_power_ratio = electrical_power_provided / TimeWarp.fixedDeltaTime / PluginHelper.BasePowerConsumption / powerReqMult;
                global_rate_multipliers = global_rate_multipliers * electrical_power_ratio;

                double kerbalScienceSkillFactor = part.protoModuleCrew.Sum(proto_crew_member => GetKerbalScienceFactor(proto_crew_member) / 2f);
                double altitude_multiplier = Math.Max(vessel.altitude / (vessel.mainBody.Radius), 1);

                science_rate_f = (kerbalScienceSkillFactor * GameConstants.baseScienceRate * PluginHelper.getScienceMultiplier(vessel)
                    / PluginHelper.SecondsInDay * global_rate_multipliers
                    / (Math.Sqrt(altitude_multiplier)));

                if (ResearchAndDevelopment.Instance != null && !double.IsNaN(science_rate_f) && !double.IsInfinity(science_rate_f))
                {
                    science_to_add += science_rate_f * TimeWarp.fixedDeltaTime;
                }
            }
            else
            */
            if (active_mode == 1) // Fuel Reprocessing
            {
                var powerRequest = powerReqMult * PluginSettings.Config.BasePowerConsumption;

                double electrical_power_provided = resMan.ConsumeResource(ResourceName.ElectricCharge, powerRequest);

                electrical_power_ratio = electrical_power_provided / PluginSettings.Config.BasePowerConsumption / powerReqMult;

                var productionModifier = global_rate_multipliers;
                global_rate_multipliers = global_rate_multipliers * electrical_power_ratio;
                reprocessor.UpdateFrame(resMan, global_rate_multipliers, electrical_power_ratio, productionModifier, true);

                if (reprocessor.getActinidesRemovedPerHour() > 0)
                    reprocessing_rate_f = reprocessor.getRemainingAmountToReprocess() / reprocessor.getActinidesRemovedPerHour();
                else
                    IsEnabled = false;
            }
            else if (active_mode == 2) //Antimatter
            {
                var powerRequestInMegajoules = powerReqMult * PluginSettings.Config.BaseAMFPowerConsumption;

                var energy_provided_in_megajoules = resMan.ConsumeResource(ResourceName.ElectricCharge, powerRequestInMegajoules);

                electrical_power_ratio = powerRequestInMegajoules > 0 ? energy_provided_in_megajoules / powerRequestInMegajoules : 0;
                antimatterGenerator.Produce(energy_provided_in_megajoules * global_rate_multipliers);
                antimatter_rate_f = antimatterGenerator.ProductionRate;
            }
            else if (active_mode == 3)
            {
                IsEnabled = false;
            }
            else if (active_mode == 4) // Centrifuge
            {
                if (vessel.Splashed)
                {
                    var powerRequest = powerReqMult * PluginSettings.Config.BaseCentriPowerConsumption;

                    double electrical_power_provided = resMan.ConsumeResource(ResourceName.ElectricCharge, powerRequest);

                    electrical_power_ratio = electrical_power_provided  / PluginSettings.Config.BaseCentriPowerConsumption / powerReqMult;
                    global_rate_multipliers = global_rate_multipliers * electrical_power_ratio;
                    double deut_produced = global_rate_multipliers * GameConstants.DeuteriumTimescale * GameConstants.DeuteriumAbudance * 1000.0f;
                    deut_rate_f = -part.RequestResource(KITResourceSettings.DeuteriumLqd, -deut_produced);
                }
                else
                {
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_ScienceModule_Postmsg"), 5.0f, ScreenMessageStyle.UPPER_CENTER);//"You must be splashed down to perform this activity."
                    IsEnabled = false;
                }
            }

            if (electrical_power_ratio <= 0)
            {
                deut_rate_f = 0;
                electrolysis_rate_f = 0;
                science_rate_f = 0;
                antimatter_rate_f = 0;
                reprocessing_rate_f = 0;
            }
        }

        public string KITPartName()
        {
            if (IsEnabled)
                return Localizer.Format("#LOC_KSPIE_ScienceModule_ResourceManagerDisplayName", modes[active_mode]);//"Science Lab (" +  + ")"

            return Localizer.Format("#LOC_KSPIE_ScienceModule_ResourceManagerDisplayName2");//"Science Lab"
        }
    }
}
