using KIT.Extensions;
using KIT.Resources;
using KIT.ResourceScheduler;
using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KIT.Refinery.Activity
{
    [KSPModule("ISRU Regolith Processor")]
    class RegolithProcessor : RefineryActivity, IRefineryActivity
    {
        public RegolithProcessor()
        {
            ActivityName = "Regolith Process";
            PowerRequirements = PluginSettings.Config.BaseELCPowerConsumption;
            EnergyPerTon = PluginSettings.Config.ElectrolysisEnergyPerTon;
        }

        private double _dFixedConsumptionRate;
        private double _dConsumptionStorageRatio;

        private double _dRegolithDensity;
        private double _dHydrogenDensity;
        private double _dDeuteriumDensity;
        private double _dLiquidHelium3Density;
        private double _dLiquidHelium4Density;
        private double _dMonoxideDensity;
        private double _dDioxideDensity;
        private double _dMethaneDensity;
        private double _dNitrogenDensity;
        private double _dWaterDensity;

        private double _fixedRegolithConsumptionRate;
        private double _regolithConsumptionRate;

        private double _dHydrogenProductionRate;
        private double _dDeuteriumProductionRate;
        private double _dLiquidHelium3ProductionRate;
        private double _dLiquidHelium4ProductionRate;
        private double _dMonoxideProductionRate;
        private double _dDioxideProductionRate;
        private double _dMethaneProductionRate;
        private double _dNitrogenProductionRate;
        private double _dWaterProductionRate;

        private string _strRegolithResourceName;
        private string _strHydrogenResourceName;
        private string _stDeuteriumResourceName;
        private string _strLiquidHelium3ResourceName;
        private string _strLiquidHelium4ResourceName;
        private string _strMonoxideResourceName;
        private string _strDioxideResourceName;
        private string _strMethaneResourceName;
        private string _strNitrogenResourceName;
        private string _strWaterResourceName;

        public RefineryType RefineryType => RefineryType.Heating;

        public bool HasActivityRequirements()
        {
            return _part.GetConnectedResources(_strRegolithResourceName).Any(rs => rs.amount > 0);
        }

        public string Status => string.Copy(_status);


        protected PartResourceDefinition DeuteriumDefinition;

        public void Initialize(Part localPart)
        {
            _part = localPart;
            _vessel = localPart.vessel;

            _strRegolithResourceName = KITResourceSettings.Regolith;
            _strHydrogenResourceName = KITResourceSettings.HydrogenLqd;
            _stDeuteriumResourceName = KITResourceSettings.DeuteriumLqd;
            _strLiquidHelium3ResourceName = KITResourceSettings.Helium3Lqd;
            _strLiquidHelium4ResourceName = KITResourceSettings.Helium4Lqd;
            _strMonoxideResourceName = KITResourceSettings.CarbonMonoxideLqd;
            _strDioxideResourceName = KITResourceSettings.CarbonDioxideLqd;
            _strMethaneResourceName = KITResourceSettings.MethaneLqd;
            _strNitrogenResourceName = KITResourceSettings.NitrogenLqd;
            _strWaterResourceName = KITResourceSettings.WaterPure;

            // should add Nitrogen15 and Argon

            _dRegolithDensity = PartResourceLibrary.Instance.GetDefinition(_strRegolithResourceName).density;
            _dHydrogenDensity = PartResourceLibrary.Instance.GetDefinition(_strHydrogenResourceName).density;
            _dDeuteriumDensity = PartResourceLibrary.Instance.GetDefinition(_stDeuteriumResourceName).density;
            _dLiquidHelium3Density = PartResourceLibrary.Instance.GetDefinition(_strLiquidHelium3ResourceName).density;
            _dLiquidHelium4Density = PartResourceLibrary.Instance.GetDefinition(_strLiquidHelium4ResourceName).density;
            _dMonoxideDensity = PartResourceLibrary.Instance.GetDefinition(_strMonoxideResourceName).density;
            _dDioxideDensity = PartResourceLibrary.Instance.GetDefinition(_strDioxideResourceName).density;
            _dMethaneDensity = PartResourceLibrary.Instance.GetDefinition(_strMethaneResourceName).density;
            _dNitrogenDensity = PartResourceLibrary.Instance.GetDefinition(_strNitrogenResourceName).density;
            _dWaterDensity = PartResourceLibrary.Instance.GetDefinition(_strWaterResourceName).density;

            DeuteriumDefinition = PartResourceLibrary.Instance.GetDefinition(_stDeuteriumResourceName);
        }

        protected double MaxCapacityRegolithMass;
        protected double MaxCapacityHydrogenMass;
        protected double MaxCapacityDeuteriumMass;
        protected double MaxCapacityHelium3Mass;
        protected double MaxCapacityHelium4Mass;
        protected double MaxCapacityMonoxideMass;
        protected double MaxCapacityDioxideMass;
        protected double MaxCapacityMethaneMass;
        protected double MaxCapacityNitrogenMass;
        protected double MaxCapacityWaterMass;

        protected double AvailableRegolithMass;
        protected double SpareRoomHydrogenMass;
        protected double SpareRoomDeuteriumMass;
        protected double SpareRoomHelium3Mass;
        protected double SpareRoomHelium4Mass;
        protected double SpareRoomMonoxideMass;
        protected double SpareRoomDioxideMass;
        protected double SpareRoomMethaneMass;
        protected double SpareRoomNitrogenMass;
        protected double SpareRoomWaterMass;
        
        /*
         * these are the constituents of regolith with their appropriate mass ratios. I'm using concentrations from lunar regolith, yes, I
         * know regolith on other planets varies, let's keep this simple.
         * The exact fractions were calculated mostly from a chart that's also available on http://imgur.com/lpaE1Ah.
         */
        protected double HydrogenMassByFraction = 0.3351424205;
        protected double Helium3MassByFraction = 0.000054942036;
        protected double Helium4MassByFraction = 0.1703203120;
        protected double MonoxideMassByFraction = 0.1043898686;
        protected double DioxideMassByFraction = 0.0934014614;
        protected double MethaneMassByFraction = 0.0879072578;
        protected double NitrogenMassByFraction = 0.0274710180;
        protected double WaterMassByFraction = 0.18130871930;

        // deuterium/hydrogen: 13 ppm source https://www.researchgate.net/publication/234236795_Deuterium_content_of_lunar_material/link/5444faa20cf2e6f0c0fbff43/download
        // based on a measurement of 13 ppm of hydrogen being deuterium (13 ppm * 0.335 = 0.000004355)
        protected double DeuteriumMassByFraction = 0.000004355;
        
        private double GetTotalExtractedPerSecond()
        {
            var collectorsList = _vessel.FindPartModulesImplementing<RegolithCollector>(); // add any atmosphere intake localPart on the vessel to our list
            return collectorsList.Where(m => m.bIsEnabled).Sum(m => m.resourceProduction);
        }

        public void UpdateFrame(IResourceManager resMan, double rateMultiplier, double powerFraction, double productionModifier, bool allowOverflow, bool isStartup = false)
        {
            _effectiveMaxPower = PowerRequirements * productionModifier;
            _current_power = _effectiveMaxPower * powerFraction;
            _current_rate = CurrentPower / EnergyPerTon;

            // determine how much resource we have
            resMan.CapacityInformation(ResourceName.Regolith, out MaxCapacityRegolithMass, out _,out AvailableRegolithMass, out _);
            resMan.CapacityInformation(ResourceName.HydrogenLqd, out MaxCapacityHydrogenMass, out SpareRoomHydrogenMass,out _, out _);
            resMan.CapacityInformation(ResourceName.DeuteriumLqd, out MaxCapacityDeuteriumMass, out SpareRoomDeuteriumMass, out _, out _);
            resMan.CapacityInformation(ResourceName.Helium3Lqd, out MaxCapacityHelium3Mass, out SpareRoomHelium3Mass, out _, out _);
            resMan.CapacityInformation(ResourceName.Helium4Lqd, out MaxCapacityHelium4Mass, out SpareRoomHelium4Mass, out _, out _);
            resMan.CapacityInformation(ResourceName.CarbonMonoxideLqd, out MaxCapacityMonoxideMass, out SpareRoomMonoxideMass, out _, out _);
            resMan.CapacityInformation(ResourceName.CarbonDioxideLqd, out MaxCapacityDioxideMass, out SpareRoomDioxideMass, out _, out _);
            resMan.CapacityInformation(ResourceName.MethaneLqd, out MaxCapacityMethaneMass, out SpareRoomMethaneMass, out _, out _);
            resMan.CapacityInformation(ResourceName.NitrogenLqd, out MaxCapacityNitrogenMass, out SpareRoomNitrogenMass, out _, out _);
            resMan.CapacityInformation(ResourceName.WaterPure, out MaxCapacityWaterMass, out SpareRoomWaterMass, out _, out _);

            // convert to density
            MaxCapacityRegolithMass *= _dRegolithDensity; AvailableRegolithMass *= _dRegolithDensity;

            MaxCapacityHydrogenMass *= _dHydrogenDensity;
            SpareRoomHydrogenMass *= _dHydrogenDensity;
            
            MaxCapacityDeuteriumMass *= DeuteriumDefinition.density;
            SpareRoomDeuteriumMass *= DeuteriumDefinition.density;

            MaxCapacityHelium3Mass *= _dLiquidHelium3Density;
            SpareRoomHelium3Mass *= _dLiquidHelium3Density;

            MaxCapacityHelium4Mass *= _dLiquidHelium4Density;
            SpareRoomHelium4Mass *= _dLiquidHelium4Density;

            MaxCapacityMonoxideMass *= _dMonoxideDensity;
            SpareRoomMonoxideMass *= _dMonoxideDensity;

            MaxCapacityDioxideMass *= _dDioxideDensity;
            SpareRoomDioxideMass *= _dDioxideDensity;

            MaxCapacityMethaneMass *= _dMethaneDensity;
            SpareRoomMethaneMass *= _dMethaneDensity;

            MaxCapacityNitrogenMass *= _dNitrogenDensity;
            SpareRoomNitrogenMass *= _dNitrogenDensity;

            MaxCapacityWaterMass *= _dWaterDensity;
            SpareRoomWaterMass *= _dWaterDensity;

            // this should determine how much resource this process can consume
            var dFixedMaxRegolithConsumptionRate = _current_rate * _dRegolithDensity;

            // determine the amount of regolith collected
            var availableRegolithExtractionMassFixed = GetTotalExtractedPerSecond() * _dRegolithDensity;

            var dRegolithConsumptionRatio = dFixedMaxRegolithConsumptionRate > 0
                ? Math.Min(dFixedMaxRegolithConsumptionRate, Math.Max(availableRegolithExtractionMassFixed, AvailableRegolithMass)) / dFixedMaxRegolithConsumptionRate
                : 0;

            _dFixedConsumptionRate = _current_rate * dRegolithConsumptionRatio;

            // begin the regolith processing
            if (_dFixedConsumptionRate > 0 && (
                SpareRoomHydrogenMass > 0 ||
                SpareRoomDeuteriumMass > 0 ||
                SpareRoomHelium3Mass > 0 ||
                SpareRoomHelium4Mass > 0 ||
                SpareRoomMonoxideMass > 0 ||
                SpareRoomDioxideMass > 0 ||
                SpareRoomMethaneMass > 0 ||
                SpareRoomNitrogenMass > 0 ||
                SpareRoomWaterMass > 0)) // check if there is anything to consume and spare room for at least one of the products
            {

                var dFixedMaxHydrogenRate = _dFixedConsumptionRate * HydrogenMassByFraction;
                var dFixedMaxDeuteriumRate = _dFixedConsumptionRate * DeuteriumMassByFraction;
                var dFixedMaxHelium3Rate = _dFixedConsumptionRate * Helium3MassByFraction;
                var dFixedMaxHelium4Rate = _dFixedConsumptionRate * Helium4MassByFraction;
                var dFixedMaxMonoxideRate = _dFixedConsumptionRate * MonoxideMassByFraction;
                var dFixedMaxDioxideRate = _dFixedConsumptionRate * DioxideMassByFraction;
                var dFixedMaxMethaneRate = _dFixedConsumptionRate * MethaneMassByFraction;
                var dFixedMaxNitrogenRate = _dFixedConsumptionRate * NitrogenMassByFraction;
                var dFixedMaxWaterRate = _dFixedConsumptionRate * WaterMassByFraction;

                var dFixedMaxPossibleHydrogenRate  = Math.Min(SpareRoomHydrogenMass,  dFixedMaxHydrogenRate);
                var dFixedMaxPossibleDeuteriumRate = Math.Min(SpareRoomDeuteriumMass, dFixedMaxDeuteriumRate);
                var dFixedMaxPossibleHelium3Rate   = Math.Min(SpareRoomHelium3Mass,   dFixedMaxHelium3Rate);
                var dFixedMaxPossibleHelium4Rate   = Math.Min(SpareRoomHelium4Mass,   dFixedMaxHelium4Rate);
                var dFixedMaxPossibleMonoxideRate  = Math.Min(SpareRoomMonoxideMass,  dFixedMaxMonoxideRate);
                var dFixedMaxPossibleDioxideRate   = Math.Min(SpareRoomDioxideMass,   dFixedMaxDioxideRate);
                var dFixedMaxPossibleMethaneRate   = Math.Min(SpareRoomMethaneMass,   dFixedMaxMethaneRate);
                var dFixedMaxPossibleNitrogenRate  = Math.Min(SpareRoomNitrogenMass,  dFixedMaxNitrogenRate);
                var dFixedMaxPossibleWaterRate     = Math.Min(SpareRoomWaterMass,     dFixedMaxWaterRate);

                var ratios = new List<double> {
                    dFixedMaxPossibleHydrogenRate / dFixedMaxHydrogenRate,
                    dFixedMaxPossibleDeuteriumRate / dFixedMaxDeuteriumRate,
                    dFixedMaxPossibleHelium3Rate / dFixedMaxHelium3Rate,
                    dFixedMaxPossibleHelium4Rate / dFixedMaxHelium4Rate,
                    dFixedMaxPossibleMonoxideRate / dFixedMaxMonoxideRate,
                    dFixedMaxPossibleNitrogenRate / dFixedMaxNitrogenRate,
                    dFixedMaxPossibleWaterRate / dFixedMaxWaterRate,
                    dFixedMaxPossibleDioxideRate / dFixedMaxDioxideRate,
                    dFixedMaxPossibleMethaneRate / dFixedMaxMethaneRate };

                _dConsumptionStorageRatio =  allowOverflow ? ratios.Max(m => m) : ratios.Min(m => m);

                // this consumes the resource
                var fixedCollectedRegolith = _part.RequestResource(_strRegolithResourceName, _dConsumptionStorageRatio * _dFixedConsumptionRate / _dRegolithDensity, ResourceFlowMode.STACK_PRIORITY_SEARCH) * _dRegolithDensity;

                _fixedRegolithConsumptionRate = Math.Max(fixedCollectedRegolith, availableRegolithExtractionMassFixed);

                _regolithConsumptionRate = _fixedRegolithConsumptionRate;

                // this produces the products
                var dHydrogenRateTemp = _fixedRegolithConsumptionRate * HydrogenMassByFraction;
                var dDeuteriumRateTemp = _fixedRegolithConsumptionRate * DeuteriumMassByFraction;
                var dHelium3RateTemp = _fixedRegolithConsumptionRate * Helium3MassByFraction;
                var dHelium4RateTemp = _fixedRegolithConsumptionRate * Helium4MassByFraction;
                var dMonoxideRateTemp = _fixedRegolithConsumptionRate * MonoxideMassByFraction;
                var dDioxideRateTemp = _fixedRegolithConsumptionRate * DioxideMassByFraction;
                var dMethaneRateTemp = _fixedRegolithConsumptionRate * MethaneMassByFraction;
                var dNitrogenRateTemp = _fixedRegolithConsumptionRate * NitrogenMassByFraction;
                var dWaterRateTemp = _fixedRegolithConsumptionRate * WaterMassByFraction;

                _dHydrogenProductionRate = -_part.RequestResource(_strHydrogenResourceName, -dHydrogenRateTemp  / _dHydrogenDensity) / _dHydrogenDensity;
                _dDeuteriumProductionRate = -_part.RequestResource(_stDeuteriumResourceName, -dDeuteriumRateTemp / _dDeuteriumDensity) /  _dDeuteriumDensity;
                _dLiquidHelium3ProductionRate = -_part.RequestResource(_strLiquidHelium3ResourceName, -dHelium3RateTemp  / _dLiquidHelium3Density) /  _dLiquidHelium3Density;
                _dLiquidHelium4ProductionRate = -_part.RequestResource(_strLiquidHelium4ResourceName, -dHelium4RateTemp  / _dLiquidHelium4Density) /  _dLiquidHelium4Density;
                _dMonoxideProductionRate = -_part.RequestResource(_strMonoxideResourceName, -dMonoxideRateTemp  / _dMonoxideDensity) /  _dMonoxideDensity;
                _dDioxideProductionRate = -_part.RequestResource(_strDioxideResourceName, -dDioxideRateTemp  / _dDioxideDensity) /  _dDioxideDensity;
                _dMethaneProductionRate = -_part.RequestResource(_strMethaneResourceName, -dMethaneRateTemp  / _dMethaneDensity) / _dMethaneDensity;
                _dNitrogenProductionRate = -_part.RequestResource(_strNitrogenResourceName, -dNitrogenRateTemp  / _dNitrogenDensity) / _dNitrogenDensity;
                _dWaterProductionRate = -_part.RequestResource(_strWaterResourceName, -dWaterRateTemp  / _dWaterDensity) /  _dWaterDensity;
            }
            else
            {
                _fixedRegolithConsumptionRate = 0;
                _dHydrogenProductionRate = 0;
                _dDeuteriumProductionRate = 0;
                _dLiquidHelium3ProductionRate = 0;
                _dLiquidHelium4ProductionRate = 0;
                _dMonoxideProductionRate = 0;
                _dDioxideProductionRate = 0;
                _dMethaneProductionRate = 0;
                _dNitrogenProductionRate = 0;
                _dWaterProductionRate = 0;
            }
            UpdateStatusMessage();
        }

        public override void UpdateGUI()
        {
            base.UpdateGUI();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_RegolithProcessor_Power"), _bold_label, GUILayout.Width(labelWidth));//"Power"
            GUILayout.Label(PluginHelper.GetFormattedPowerString(CurrentPower) + "/" + PluginHelper.GetFormattedPowerString(_effectiveMaxPower), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_RegolithProcessor_Consumption"), _bold_label, GUILayout.Width(labelWidth));//"Regolith Consumption"
            GUILayout.Label(((_regolithConsumptionRate * GameConstants.SecondsInHour).ToString("0.000000")) + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_RegolithProcessor_Available"), _bold_label, GUILayout.Width(labelWidth));//"Regolith Available"
            GUILayout.Label(AvailableRegolithMass.ToString("0.000000") + " mT / " + MaxCapacityRegolithMass.ToString("0.000000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_RegolithProcessor_Output"), _bold_label, GUILayout.Width(labelWidth));//"Output"
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_RegolithProcessor_ResourceName"), _bold_label, GUILayout.Width(labelWidth));//"Resource Name"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_RegolithProcessor_SpareRoom"), _bold_label, GUILayout.Width(labelWidth));//"Spare Room"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_RegolithProcessor_MaximumStorage"), _bold_label, GUILayout.Width(labelWidth));//"Maximum Storage"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_RegolithProcessor_ProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Production Rate"
            GUILayout.EndHorizontal();

            DisplayResourceOutput(_strHydrogenResourceName, SpareRoomHydrogenMass, MaxCapacityHydrogenMass, _dHydrogenProductionRate);
            DisplayResourceOutput(_stDeuteriumResourceName, SpareRoomDeuteriumMass, MaxCapacityDeuteriumMass, _dDeuteriumProductionRate);
            DisplayResourceOutput(_strLiquidHelium3ResourceName, SpareRoomHelium3Mass, MaxCapacityHelium3Mass, _dLiquidHelium3ProductionRate);
            DisplayResourceOutput(_strLiquidHelium4ResourceName, SpareRoomHelium4Mass, MaxCapacityHelium4Mass, _dLiquidHelium4ProductionRate);
            DisplayResourceOutput(_strMonoxideResourceName, SpareRoomMonoxideMass, MaxCapacityMonoxideMass, _dMonoxideProductionRate);
            DisplayResourceOutput(_strDioxideResourceName, SpareRoomDioxideMass, MaxCapacityDioxideMass, _dDioxideProductionRate);
            DisplayResourceOutput(_strMethaneResourceName, SpareRoomMethaneMass, MaxCapacityMethaneMass, _dMethaneProductionRate);
            DisplayResourceOutput(_strNitrogenResourceName, SpareRoomNitrogenMass, MaxCapacityNitrogenMass, _dNitrogenProductionRate);
            DisplayResourceOutput(_strWaterResourceName, SpareRoomWaterMass, MaxCapacityWaterMass, _dWaterProductionRate);
        }

        private void DisplayResourceOutput(string resourceName, double spareRoom, double maxCapacity, double productionRate)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(resourceName, _value_label, GUILayout.Width(labelWidth));
            GUILayout.Label(spareRoom.ToString("0.000000") + " mT", maxCapacity > 0 && spareRoom == 0 ? _value_label_red : _value_label, GUILayout.Width(labelWidth));
            GUILayout.Label(maxCapacity.ToString("0.000000") + " mT", maxCapacity == 0 ? _value_label_red : _value_label, GUILayout.Width(labelWidth));
            GUILayout.Label((productionRate * GameConstants.SecondsInHour).ToString("0.000000") + " mT/hour", productionRate > 0 ? _value_label_green : _value_label, GUILayout.Width(labelWidth));
            GUILayout.EndHorizontal();
        }

        private void UpdateStatusMessage()
        {
            if (_fixedRegolithConsumptionRate > 0)
                _status = Localizer.Format("#LOC_KSPIE_RegolithProcessor_Statumsg1");//"Processing of Regolith Ongoing"
            else if (CurrentPower <= 0.01 * PowerRequirements)
                _status = Localizer.Format("#LOC_KSPIE_RegolithProcessor_Statumsg2");//"Insufficient Power"
            else
                _status = Localizer.Format("#LOC_KSPIE_RegolithProcessor_Statumsg3");//"Insufficient Storage, try allowing overflow"
        }

        public void PrintMissingResources()
        {
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_RegolithProcessor_Postmsg") +" " + KITResourceSettings.Regolith, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
        }
    }
}
