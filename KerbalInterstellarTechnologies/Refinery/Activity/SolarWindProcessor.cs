using KIT.Extensions;
using KIT.Resources;
using KIT.ResourceScheduler;
using KSP.Localization;
using System;
using System.Linq;
using UnityEngine;

namespace KIT.Refinery.Activity
{
    class SolarWindProcessor : RefineryActivity, IRefineryActivity
    {
        public SolarWindProcessor()
        {
            ActivityName = "Solar Wind Process";
            PowerRequirements = PluginSettings.Config.BaseELCPowerConsumption;
            EnergyPerTon = PluginSettings.Config.ElectrolysisEnergyPerTon;
        }

        private double _fixedConsumptionRate;

        private PartResourceDefinition _solarWind;

        private PartResourceDefinition _hydrogenLiquid;
        private PartResourceDefinition _hydrogenGas;

        private PartResourceDefinition _deuteriumLiquid;
        private PartResourceDefinition _deuteriumGas;

        private PartResourceDefinition _liquidHelium3Liquid;
        private PartResourceDefinition _liquidHelium3Gas;

        private PartResourceDefinition _liquidHelium4Liquid;
        private PartResourceDefinition _liquidHelium4Gas;

        private PartResourceDefinition _monoxideLiquid;
        private PartResourceDefinition _monoxideGas;

        private PartResourceDefinition _nitrogenLiquid;
        private PartResourceDefinition _nitrogenGas;

        private PartResourceDefinition _neonLiquid;
        private PartResourceDefinition _neonGas;

        private double _solarWindConsumptionRate;
        private double _hydrogenProductionRate;
        private double _deuteriumProductionRate;
        private double _liquidHelium3ProductionRate;
        private double _liquidHelium4ProductionRate;
        private double _monoxideProductionRate;
        private double _nitrogenProductionRate;
        private double _neonProductionRate;

  
        public RefineryType RefineryType => RefineryType.Cryogenics;

        public bool HasActivityRequirements ()
        {
           return _part.GetConnectedResources(KITResourceSettings.SolarWind).Any(rs => rs.maxAmount > 0);
        }


        public string Status => string.Copy(_status);


        public void Initialize(Part localPart)
        {
            _part = localPart;
            _vessel = localPart.vessel;

            _solarWind = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.SolarWind);

            _hydrogenLiquid = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.HydrogenLqd);
            _hydrogenGas = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.HydrogenGas);
            _deuteriumLiquid = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.DeuteriumLqd);
            _deuteriumGas = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.DeuteriumGas);
            _liquidHelium3Liquid = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.Helium3Lqd);
            _liquidHelium3Gas = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.Helium3Gas);
            _liquidHelium4Liquid = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.Helium4Lqd);
            _liquidHelium4Gas = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.Helium4Gas);
            _monoxideLiquid = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.CarbonMonoxideLqd);
            _monoxideGas = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.CarbonMonoxideGas);
            _nitrogenLiquid = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.NitrogenLqd);
            _nitrogenGas = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.NitrogenGas);
            _neonLiquid = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.NeonLqd);
            _neonGas = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.NeonGas);
        }

        // TODO, modify the GetResource function below to store all three. current, spare, max, etc.
        private double _storedHydrogenMass, _storedHelium4Mass, _storedMonoxideMass, _storedDeuteriumMass, _storedHelium3Mass, _storedNitrogenMass, _storedNeonMass;

        private double _maxCapacitySolarWindMass;
        private double _maxCapacityHydrogenMass;
        private double _maxCapacityDeuteriumMass;
        private double _maxCapacityHelium3Mass;
        private double _maxCapacityHelium4Mass;
        private double _maxCapacityMonoxideMass;
        private double _maxCapacityNitrogenMass;
        private double _maxCapacityNeonMass;

        private double _storedSolarWindMass;
        
        private double _spareRoomHydrogenMass;
        private double _spareRoomDeuteriumMass;
        private double _spareRoomHelium3Mass;
        private double _spareRoomHelium4Mass;
        private double _spareRoomMonoxideMass;
        private double _spareRoomNitrogenMass;
        private double _spareRoomNeonMass;

        /* these are the constituents of solar wind with their appropriate mass ratios. According to http://solar-center.stanford.edu/FAQ/Qsolwindcomp.html and other sources,
         * about 90 % of atoms in solar wind are hydrogen. About 8 % is helium (he-3 is less stable than he-4 and apparently the He-3/He-4 ratio is very close to 1/2000), so that's about 7,996 % He-4 and 0.004 % He-3. There are supposedly only trace amounts of heavier elements such as C, O, N and Ne,
         * so I decided on 1 % of atoms for CO and 0.5 % for Nitrogen and Neon. The exact fractions were calculated as (percentage * atomic mass of the element) / Sum of (percentage * atomic mass of the element) for every element.
        */
        private double _hydrogenMassByFraction  = 0.540564; // see above how I got this number (as an example 90 * 1.008 / 167.8245484 = 0.540564), ie. percentage times atomic mass of H / sum of the same stuff in numerator for every element
        private double _deuteriumMassByFraction = 0.00001081128; // because D/H ratio in solarwind is 2 *10e-5 * Hydrogen mass
        private double _helium3MassByFraction   = 0.0000071; // because examples are nice to have (0.004 * 3.016 / 167.8245484 = 0.0000071)
        private double _helium4MassByFraction   = 0.1906752;
        private double _monoxideMassByFraction  = 0.1669004;
        private double _nitrogenMassByFraction  = 0.04173108;
        private double _neonMassByFraction      = 0.06012;

        public void UpdateFrame(IResourceManager resMan, double rateMultiplier, double powerFraction, double productionModifier, bool allowOverflow,  bool isStartup = false)
        {
            _current_power = PowerRequirements * rateMultiplier;
            _current_rate = CurrentPower / EnergyPerTon;

            _storedSolarWindMass = _spareRoomHydrogenMass = _maxCapacityHydrogenMass = _spareRoomDeuteriumMass = _maxCapacityDeuteriumMass = _spareRoomHelium3Mass = 0;
            _maxCapacityHelium3Mass = _spareRoomHelium4Mass = _maxCapacityHelium4Mass = _spareRoomMonoxideMass = _maxCapacityMonoxideMass = _spareRoomNitrogenMass = 0;
            _maxCapacityNitrogenMass = _maxCapacityNeonMass = 0;

            GetResourceMass(resMan, ResourceName.SolarWind, _solarWind, ref _storedSolarWindMass, ref _maxCapacitySolarWindMass);

            var cur = resMan.CurrentCapacity(ResourceName.SolarWind);
            var spare = resMan.SpareCapacity(ResourceName.SolarWind);

            _storedSolarWindMass = cur * _solarWind.density;
            _maxCapacitySolarWindMass = (cur + spare) * _solarWind.density;

            GetResourceMass(resMan, ResourceName.HydrogenLqd, _hydrogenLiquid, ref _spareRoomHydrogenMass, ref _maxCapacityHydrogenMass);
            GetResourceMass(resMan, ResourceName.HydrogenGas, _hydrogenLiquid, ref _spareRoomHydrogenMass, ref _maxCapacityHydrogenMass);
            GetResourceMass(resMan, ResourceName.DeuteriumLqd, _hydrogenLiquid, ref _spareRoomDeuteriumMass, ref _maxCapacityDeuteriumMass);
            GetResourceMass(resMan, ResourceName.DeuteriumGas, _hydrogenLiquid, ref _spareRoomDeuteriumMass, ref _maxCapacityDeuteriumMass);
            GetResourceMass(resMan, ResourceName.Helium3Lqd, _hydrogenLiquid, ref _spareRoomHelium3Mass, ref _maxCapacityHelium3Mass);
            GetResourceMass(resMan, ResourceName.Helium3Gas, _hydrogenLiquid, ref _spareRoomHelium3Mass, ref _maxCapacityHelium3Mass);
            GetResourceMass(resMan, ResourceName.Helium4Lqd, _hydrogenLiquid, ref _spareRoomHelium4Mass, ref _maxCapacityHelium4Mass);
            GetResourceMass(resMan, ResourceName.Helium4Gas, _hydrogenLiquid, ref _spareRoomHelium4Mass, ref _maxCapacityHelium4Mass);
            GetResourceMass(resMan, ResourceName.CarbonMonoxideLqd, _hydrogenLiquid, ref _spareRoomMonoxideMass, ref _maxCapacityMonoxideMass);
            GetResourceMass(resMan, ResourceName.CarbonMonoxideGas, _hydrogenLiquid, ref _spareRoomMonoxideMass, ref _maxCapacityMonoxideMass);
            GetResourceMass(resMan, ResourceName.NitrogenLqd, _hydrogenLiquid, ref _spareRoomNitrogenMass, ref _maxCapacityNitrogenMass);
            GetResourceMass(resMan, ResourceName.NitrogenGas, _hydrogenLiquid, ref _spareRoomNitrogenMass, ref _maxCapacityNitrogenMass);
            GetResourceMass(resMan, ResourceName.NeonLqd, _hydrogenLiquid, ref _spareRoomNeonMass, ref _maxCapacityNeonMass);
            GetResourceMass(resMan, ResourceName.NeonGas, _hydrogenLiquid, ref _spareRoomNeonMass, ref _maxCapacityNeonMass);


            // this should determine how much resource this process can consume
            double fixedMaxSolarWindConsumptionRate = _current_rate  * _solarWind.density;
            double solarWindConsumptionRatio = fixedMaxSolarWindConsumptionRate > 0
                ? Math.Min(fixedMaxSolarWindConsumptionRate, _storedSolarWindMass) / fixedMaxSolarWindConsumptionRate
                : 0;

            _fixedConsumptionRate = _current_rate  * solarWindConsumptionRatio;

            // begin the solar wind processing
            if (_fixedConsumptionRate > 0 && (_spareRoomHydrogenMass > 0 || _spareRoomDeuteriumMass > 0 || _spareRoomHelium3Mass > 0 || _spareRoomHelium4Mass > 0 || _spareRoomMonoxideMass > 0 || _spareRoomNitrogenMass > 0)) // check if there is anything to consume and spare room for at least one of the products
            {
                var fixedMaxHydrogenRate = _fixedConsumptionRate * _hydrogenMassByFraction;
                var fixedMaxDeuteriumRate = _fixedConsumptionRate * _deuteriumMassByFraction;
                var fixedMaxHelium3Rate = _fixedConsumptionRate * _helium3MassByFraction;
                var fixedMaxHelium4Rate = _fixedConsumptionRate * _helium4MassByFraction;
                var fixedMaxMonoxideRate = _fixedConsumptionRate * _monoxideMassByFraction;
                var fixedMaxNitrogenRate = _fixedConsumptionRate * _nitrogenMassByFraction;
                var fixedMaxNeonRate = _fixedConsumptionRate * _neonMassByFraction;

                var fixedMaxPossibleHydrogenRate = allowOverflow ? fixedMaxHydrogenRate : Math.Min(_spareRoomHydrogenMass, fixedMaxHydrogenRate);
                var fixedMaxPossibleDeuteriumRate = allowOverflow ? fixedMaxDeuteriumRate : Math.Min(_spareRoomDeuteriumMass, fixedMaxDeuteriumRate);
                var fixedMaxPossibleHelium3Rate = allowOverflow ? fixedMaxHelium3Rate : Math.Min(_spareRoomHelium3Mass, fixedMaxHelium3Rate);
                var fixedMaxPossibleHelium4Rate = allowOverflow ? fixedMaxHelium4Rate : Math.Min(_spareRoomHelium4Mass, fixedMaxHelium4Rate);
                var fixedMaxPossibleMonoxideRate = allowOverflow ? fixedMaxMonoxideRate : Math.Min(_spareRoomMonoxideMass, fixedMaxMonoxideRate);
                var fixedMaxPossibleNitrogenRate = allowOverflow ? fixedMaxNitrogenRate : Math.Min(_spareRoomNitrogenMass, fixedMaxNitrogenRate);
                var fixedMaxPossibleNeonRate = allowOverflow ? fixedMaxNeonRate : Math.Min(_spareRoomNeonMass, fixedMaxNeonRate);

                // finds the minimum of these five numbers (fixedMaxPossibleZZRate / fixedMaxZZRate), adapted from water electrolyser. Could be more pretty with a custom Min5() function, but eh.
                var consumptionStorageRatios = new[]
                {
                    fixedMaxPossibleHydrogenRate / fixedMaxHydrogenRate,
                    fixedMaxPossibleDeuteriumRate / fixedMaxDeuteriumRate,
                    fixedMaxPossibleHelium3Rate / fixedMaxHelium3Rate,
                    fixedMaxPossibleHelium4Rate / fixedMaxHelium4Rate,
                    fixedMaxPossibleMonoxideRate / fixedMaxMonoxideRate,
                    fixedMaxPossibleNitrogenRate / fixedMaxNitrogenRate,
                    fixedMaxPossibleNeonRate / fixedMaxNeonRate
                };

                double minConsumptionStorageRatio = consumptionStorageRatios.Min();

                // this consumes the resource
                _solarWindConsumptionRate = resMan.Consume(ResourceName.SolarWind, minConsumptionStorageRatio  / _solarWind.density) / _solarWind.density;

                // this produces the products
                var hydrogenRateTemp = _solarWindConsumptionRate * _hydrogenMassByFraction;
                var deuteriumRateTemp = _solarWindConsumptionRate * _deuteriumMassByFraction;
                var helium3RateTemp = _solarWindConsumptionRate * _helium3MassByFraction;
                var helium4RateTemp = _solarWindConsumptionRate * _helium4MassByFraction;
                var monoxideRateTemp = _solarWindConsumptionRate * _monoxideMassByFraction;
                var nitrogenRateTemp = _solarWindConsumptionRate * _nitrogenMassByFraction;
                var neonRateTemp = _solarWindConsumptionRate * _neonMassByFraction;

                /*
                 * 
            _hydrogenLiquid = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.HydrogenLqd);
            _hydrogenGas = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.HydrogenGas);
            _deuteriumLiquid = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.DeuteriumLqd);
            _deuteriumGas = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.DeuteriumGas);
            _liquidHelium3Liquid = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.Helium3Lqd);
            _liquidHelium3Gas = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.Helium3Gas);
            _liquidHelium4Liquid = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.Helium4Lqd);
            _liquidHelium4Gas = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.Helium4Gas);
            _monoxideLiquid = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.CarbonMonoxideLqd);
            _monoxideGas = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.CarbonMonoxideGas);
            _nitrogenLiquid = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.NitrogenLqd);
            _nitrogenGas = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.NitrogenGas);
            _neonLiquid = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.NeonLqd);
            _neonGas = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.NeonGas);
                */

                _hydrogenProductionRate = -_part.RequestResource(KITResourceSettings.HydrogenGas, -hydrogenRateTemp  / _hydrogenGas.density, ResourceFlowMode.ALL_VESSEL) /  _hydrogenGas.density;
                _hydrogenProductionRate += -_part.RequestResource(KITResourceSettings.HydrogenLqd, -(hydrogenRateTemp - _hydrogenProductionRate)  / _hydrogenLiquid.density, ResourceFlowMode.ALL_VESSEL) / _hydrogenLiquid.density;

                _deuteriumProductionRate = -_part.RequestResource(KITResourceSettings.DeuteriumGas, -deuteriumRateTemp  / _deuteriumGas.density, ResourceFlowMode.ALL_VESSEL) / _deuteriumGas.density;
                _deuteriumProductionRate += -_part.RequestResource(KITResourceSettings.DeuteriumLqd, -(deuteriumRateTemp - _deuteriumProductionRate)  / _deuteriumLiquid.density, ResourceFlowMode.ALL_VESSEL) /  _deuteriumLiquid.density;

                _liquidHelium3ProductionRate = -_part.RequestResource(KITResourceSettings.Helium3Gas, -helium3RateTemp / _liquidHelium3Gas.density, ResourceFlowMode.ALL_VESSEL) /  _liquidHelium3Gas.density;
                _liquidHelium3ProductionRate += -_part.RequestResource(KITResourceSettings.Helium3Lqd, -(helium3RateTemp - _liquidHelium3ProductionRate)  / _liquidHelium3Liquid.density, ResourceFlowMode.ALL_VESSEL) /  _liquidHelium3Liquid.density;

                _liquidHelium4ProductionRate = -_part.RequestResource(KITResourceSettings.Helium4Gas, -helium4RateTemp / _liquidHelium4Gas.density, ResourceFlowMode.ALL_VESSEL) / _liquidHelium4Gas.density;
                _liquidHelium4ProductionRate += -_part.RequestResource(KITResourceSettings.Helium4Lqd, -(helium4RateTemp - _liquidHelium4ProductionRate)  / _liquidHelium4Liquid.density, ResourceFlowMode.ALL_VESSEL) / _liquidHelium4Liquid.density;

                _monoxideProductionRate = -_part.RequestResource(KITResourceSettings.CarbonMonoxideGas, -monoxideRateTemp  / _monoxideGas.density, ResourceFlowMode.ALL_VESSEL) / _monoxideGas.density;
                _monoxideProductionRate += -_part.RequestResource(KITResourceSettings.CarbonMonoxideLqd, -(monoxideRateTemp - _monoxideProductionRate)  / _monoxideLiquid.density, ResourceFlowMode.ALL_VESSEL) /  _monoxideLiquid.density;

                _nitrogenProductionRate = -_part.RequestResource(KITResourceSettings.NitrogenGas, -nitrogenRateTemp  / _nitrogenGas.density, ResourceFlowMode.ALL_VESSEL) / _nitrogenGas.density;
                _nitrogenProductionRate += -_part.RequestResource(KITResourceSettings.NitrogenLqd, -(nitrogenRateTemp - _nitrogenProductionRate)  / _nitrogenLiquid.density, ResourceFlowMode.ALL_VESSEL) /  _nitrogenLiquid.density;

                _neonProductionRate = -_part.RequestResource(KITResourceSettings.NeonGas, -neonRateTemp / _neonGas.density, ResourceFlowMode.ALL_VESSEL) / _neonGas.density;
                _neonProductionRate += -_part.RequestResource(KITResourceSettings.NeonLqd, -(neonRateTemp - _neonProductionRate) / _neonLiquid.density, ResourceFlowMode.ALL_VESSEL) /  _neonLiquid.density;
            }
            else
            {
                _solarWindConsumptionRate = 0;
                _hydrogenProductionRate = 0;
                _deuteriumProductionRate = 0;
                _liquidHelium3ProductionRate = 0;
                _liquidHelium4ProductionRate = 0;
                _monoxideProductionRate = 0;
                _nitrogenProductionRate = 0;
                _neonProductionRate = 0;
            }
            UpdateStatusMessage();
        }

        public override void UpdateGUI()
        {
            base.UpdateGUI();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SolarWindProcessor_Power"), _bold_label, GUILayout.Width(labelWidth));//"Power"
            GUILayout.Label(PluginHelper.GetFormattedPowerString(CurrentPower) + "/" + PluginHelper.GetFormattedPowerString(PowerRequirements), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SolarWindProcessor_SolarWindAvailable"), _bold_label, GUILayout.Width(labelWidth));//"Solar Wind Available"
            GUILayout.Label((_storedSolarWindMass * 1e6).ToString("0.000") + " "+Localizer.Format("#LOC_KSPIE_SolarWindProcessor_gram"), _value_label, GUILayout.Width(valueWidth));//gram
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SolarWindProcessor_SolarWindMaxCapacity"), _bold_label, GUILayout.Width(labelWidth));//"Solar Wind Max Capacity"
            GUILayout.Label((_maxCapacitySolarWindMass * 1e6).ToString("0.000") + " gram", _value_label, GUILayout.Width(valueWidth));//
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SolarWindProcessor_SolarWindConsumption"), _bold_label, GUILayout.Width(labelWidth));//"Solar Wind Consumption"
            GUILayout.Label((((float)_solarWindConsumptionRate * GameConstants.SecondsInHour * 1e6).ToString("0.0000")) + " g/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SolarWindProcessor_Output"), _bold_label, GUILayout.Width(150));//"Output"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SolarWindProcessor_Percentage"), _bold_label, GUILayout.Width(100));//"Percentage"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SolarWindProcessor_Capacity"), _bold_label, GUILayout.Width(100));//"Capacity"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SolarWindProcessor_Stored"), _bold_label, GUILayout.Width(100));//"Stored"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SolarWindProcessor_SpareRoom"), _bold_label, GUILayout.Width(100));//"Spare Room"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SolarWindProcessor_Production"), _bold_label, GUILayout.Width(100));//"Production"
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(KITResourceSettings.HydrogenGas + " / " + KITResourceSettings.HydrogenLqd, _value_label, GUILayout.Width(150));
            GUILayout.Label((_hydrogenMassByFraction * 100).ToString("0.000000") + "%", _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_maxCapacityHydrogenMass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_storedHydrogenMass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_spareRoomHydrogenMass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(GramPerHour(_hydrogenProductionRate * GameConstants.SecondsInHour * 1e6, "0.0000"), _value_label, GUILayout.Width(100));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(KITResourceSettings.Helium4Gas + " / " + KITResourceSettings.Helium4Lqd, _value_label, GUILayout.Width(150));
            GUILayout.Label((_helium4MassByFraction * 100).ToString("0.000000") + "%", _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_maxCapacityHelium4Mass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_storedHelium4Mass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_spareRoomHelium4Mass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(GramPerHour(_liquidHelium4ProductionRate * GameConstants.SecondsInHour * 1e6, "0.0000"), _value_label, GUILayout.Width(100));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(KITResourceSettings.CarbonMonoxideGas + " / " + KITResourceSettings.CarbonMonoxideLqd, _value_label, GUILayout.Width(150));
            GUILayout.Label((_monoxideMassByFraction * 100).ToString("0.000000") + "%", _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_maxCapacityMonoxideMass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_storedMonoxideMass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_spareRoomMonoxideMass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(GramPerHour(_monoxideProductionRate * GameConstants.SecondsInHour * 1e6, "0.0000"), _value_label, GUILayout.Width(100));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(KITResourceSettings.NitrogenGas + " / " + KITResourceSettings.NitrogenLqd, _value_label, GUILayout.Width(150));
            GUILayout.Label((_nitrogenMassByFraction * 100).ToString("0.000000") + "%", _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_maxCapacityNitrogenMass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_storedNitrogenMass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_spareRoomNitrogenMass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(GramPerHour(_nitrogenProductionRate * GameConstants.SecondsInHour * 1e6, "0.0000"), _value_label, GUILayout.Width(100));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(KITResourceSettings.NeonGas + " / " + KITResourceSettings.NeonLqd, _value_label, GUILayout.Width(150));
            GUILayout.Label((_neonMassByFraction * 100).ToString("0.000000") + "%", _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_maxCapacityNeonMass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_storedNeonMass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_spareRoomNeonMass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(GramPerHour(_neonProductionRate * GameConstants.SecondsInHour * 1e6, "0.0000"), _value_label, GUILayout.Width(100));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(KITResourceSettings.DeuteriumGas + " / " + KITResourceSettings.DeuteriumLqd, _value_label, GUILayout.Width(150));
            GUILayout.Label((_deuteriumMassByFraction * 100).ToString("0.000000") + "%", _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_maxCapacityDeuteriumMass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_storedDeuteriumMass,"0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_spareRoomDeuteriumMass, "0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(GramPerHour(_deuteriumProductionRate * GameConstants.SecondsInHour * 1e6, "0.0000"), _value_label, GUILayout.Width(100));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(KITResourceSettings.Helium3Gas + " / " + KITResourceSettings.Helium3Lqd, _value_label, GUILayout.Width(150));
            GUILayout.Label((_helium3MassByFraction * 100).ToString("0.000000") + "%", _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_maxCapacityHelium3Mass, "0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_storedHelium3Mass, "0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(MetricTon(_spareRoomHelium3Mass, "0.000000"), _value_label, GUILayout.Width(100));
            GUILayout.Label(GramPerHour(_liquidHelium3ProductionRate * GameConstants.SecondsInHour * 1e6, "0.0000"), _value_label, GUILayout.Width(100));
            GUILayout.EndHorizontal();
        }

        private void UpdateStatusMessage()
        {
            if (_solarWindConsumptionRate > 0)
                _status = Localizer.Format("#LOC_KSPIE_SolarWindProcessor_Statumsg1");//"Processing of Solar Wind Particles Ongoing"
            else if (CurrentPower <= 0.01*PowerRequirements)
                _status = Localizer.Format("#LOC_KSPIE_SolarWindProcessor_Statumsg2");//"Insufficient Power"
            else if (_storedSolarWindMass <= float.Epsilon)
                _status = Localizer.Format("#LOC_KSPIE_SolarWindProcessor_Statumsg3");//"No Solar Wind Particles Available"
            else
                _status = Localizer.Format("#LOC_KSPIE_SolarWindProcessor_Statumsg4");//"Insufficient Storage, try allowing overflow"
        }

        private string MetricTon(double value, string format = "")
        {
            return value == 0 ? "-" : value < 1 ? value < 0.001 ? (value * 1e6).ToString(format) + " g" : (value * 1e3).ToString(format) + " kg" : value.ToString(format) + " mT";
        }

        private string GramPerHour(double value, string format = "")
        {
            return value == 0 ? "-" : value < 1 ? (value * 1000).ToString(format) + " mg/hour" : value.ToString(format) + " g/hour";
        }

        public void PrintMissingResources()
        {
            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_SolarWindProcessor_Postmsg") +" " + KITResourceSettings.SolarWind, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
        }
    }
}
