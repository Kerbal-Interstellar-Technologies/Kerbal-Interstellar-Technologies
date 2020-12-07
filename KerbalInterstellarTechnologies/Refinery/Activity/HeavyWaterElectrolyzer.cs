﻿using KIT.Constants;
using KIT.Extensions;
using KIT.Resources;
using KIT.ResourceScheduler;
using KSP.Localization;
using System;
using System.Linq;
using UnityEngine;

namespace KIT.Refinery.Activity
{
    class HeavyWaterElectrolyzer : RefineryActivity, IRefineryActivity
    {
        public HeavyWaterElectrolyzer()
        {
            ActivityName = "Heavy Water Electrolysis";
            Formula = "D<size=7>2</size>O => D<size=7>2</size> + O<size=7>2</size>";
            PowerRequirements = PluginHelper.BaseELCPowerConsumption;
            EnergyPerTon = PluginHelper.ElectrolysisEnergyPerTon;
        }

        private const double DeuteriumAtomicMass = 2.01410178;
        private const double OxygenAtomicMass = 15.999;
        private const double DeuteriumMassByFraction = (2 * DeuteriumAtomicMass) / (OxygenAtomicMass + (2 * DeuteriumAtomicMass)); // 0.201136
        private const double OxygenMassByFraction = 1 - DeuteriumMassByFraction;

        private double _heavyWaterConsumptionRate;
        private double _deuteriumProductionRate;
        private double _oxygenProductionRate;
        private double _fixedMaxConsumptionWaterRate;
        private double _consumptionStorageRatio;

        private string _waterHeavyResourceName;
        private string _oxygenResourceName;
        private string _deuteriumResourceName;

        private double _heavyWaterDensity;
        private double _oxygenDensity;
        private double _deuteriumDensity;

        private double _availableHeavyWaterMass;
        private double _spareRoomOxygenMass;
        private double _spareRoomDeuteriumMass;

        private double _maxCapacityHeavyWaterMass;
        private double _maxCapacityDeuteriumMass;
        private double _maxCapacityOxygenMass;

        public RefineryType RefineryType => RefineryType.Electrolysis;

        public bool HasActivityRequirements() {  return _part.GetConnectedResources(ResourceSettings.Config.WaterHeavy).Any(rs => rs.amount > 0);   }

        public string Status => string.Copy(_status);

        public void Initialize(Part localPart)
        {
            _part = localPart;

            _waterHeavyResourceName = ResourceSettings.Config.WaterHeavy;
            _oxygenResourceName = ResourceSettings.Config.OxygenGas;
            _deuteriumResourceName = ResourceSettings.Config.DeuteriumGas;

            _vessel = localPart.vessel;
            _heavyWaterDensity = PartResourceLibrary.Instance.GetDefinition(_waterHeavyResourceName).density;
            _oxygenDensity = PartResourceLibrary.Instance.GetDefinition(_oxygenResourceName).density;
            _deuteriumDensity = PartResourceLibrary.Instance.GetDefinition(_deuteriumResourceName).density;
        }

        public void UpdateFrame(IResourceManager resMan, double rateMultiplier, double powerFraction, double productionModifier, bool allowOverflow,  bool isStartup = false)
        {
            // determine how much mass we can produce at max
            _current_power = PowerRequirements * rateMultiplier;
            _current_rate = CurrentPower / EnergyPerTon;

            var partsThatContainWater = _part.GetConnectedResources(_waterHeavyResourceName).ToList();
            var partsThatContainOxygen = _part.GetConnectedResources(_oxygenResourceName).ToList();
            var partsThatContainDeuterium = _part.GetConnectedResources(_deuteriumResourceName).ToList();

            _maxCapacityHeavyWaterMass = partsThatContainWater.Sum(p => p.maxAmount) * _heavyWaterDensity;
            _maxCapacityOxygenMass = partsThatContainOxygen.Sum(p => p.maxAmount) * _oxygenDensity;
            _maxCapacityDeuteriumMass = partsThatContainDeuterium.Sum(p => p.maxAmount) * _deuteriumDensity;

            _availableHeavyWaterMass = partsThatContainWater.Sum(p => p.amount) * _heavyWaterDensity;
            _spareRoomOxygenMass = partsThatContainOxygen.Sum(r => r.maxAmount - r.amount) * _oxygenDensity;
            _spareRoomDeuteriumMass = partsThatContainDeuterium.Sum(r => r.maxAmount - r.amount) * _deuteriumDensity;

            // determine how much water we can consume
            _fixedMaxConsumptionWaterRate = Math.Min(_current_rate , _availableHeavyWaterMass);

            if (_fixedMaxConsumptionWaterRate > 0 && (_spareRoomOxygenMass > 0 || _spareRoomDeuteriumMass > 0))
            {
                // calculate consumptionStorageRatio
                var fixedMaxHydrogenRate = _fixedMaxConsumptionWaterRate * DeuteriumMassByFraction;
                var fixedMaxOxygenRate = _fixedMaxConsumptionWaterRate * OxygenMassByFraction;

                var fixedMaxPossibleHydrogenRate = allowOverflow ? fixedMaxHydrogenRate : Math.Min(_spareRoomDeuteriumMass, fixedMaxHydrogenRate);
                var fixedMaxPossibleOxygenRate = allowOverflow ? fixedMaxOxygenRate : Math.Min(_spareRoomOxygenMass, fixedMaxOxygenRate);

                var fixedMaxPossibleHydrogenRatio = fixedMaxPossibleHydrogenRate / fixedMaxHydrogenRate;
                var fixedMaxPossibleOxygenRatio = fixedMaxPossibleOxygenRate / fixedMaxOxygenRate;
                _consumptionStorageRatio = Math.Min(fixedMaxPossibleHydrogenRatio, fixedMaxPossibleOxygenRatio);

                // now we do the real electrolysis
                _heavyWaterConsumptionRate = _part.RequestResource(_waterHeavyResourceName, _consumptionStorageRatio * _fixedMaxConsumptionWaterRate / _heavyWaterDensity) / _heavyWaterDensity;

                var deuteriumRateTemp = _heavyWaterConsumptionRate * DeuteriumMassByFraction;
                var oxygenRateTemp = _heavyWaterConsumptionRate * OxygenMassByFraction;

                _deuteriumProductionRate = -_part.RequestResource(_deuteriumResourceName, -deuteriumRateTemp  / _deuteriumDensity, ResourceFlowMode.ALL_VESSEL) / _deuteriumDensity;
                _oxygenProductionRate = -_part.RequestResource(_oxygenResourceName, -oxygenRateTemp  / _oxygenDensity, ResourceFlowMode.ALL_VESSEL) /  _oxygenDensity;
            }
            else
            {
                _heavyWaterConsumptionRate = 0;
                _deuteriumProductionRate = 0;
                _oxygenProductionRate = 0;
            }

            UpdateStatusMessage();
        }

        public override void UpdateGUI()
        {
            base.UpdateGUI();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_HeavyWaterElectroliser_Power"), _bold_label, GUILayout.Width(labelWidth));//"Power"
            GUILayout.Label(PluginHelper.getFormattedPowerString(CurrentPower) + "/" + PluginHelper.getFormattedPowerString(PowerRequirements), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_HeavyWaterElectroliser_Available"), _bold_label, GUILayout.Width(labelWidth));//"Heavy Water Available"
            GUILayout.Label(_availableHeavyWaterMass.ToString("0.0000") + " mT / " + _maxCapacityHeavyWaterMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_HeavyWaterElectroliser_ConsumptionStorageRatio"), _bold_label, GUILayout.Width(labelWidth));//"Consumption Storage Ratio"
            GUILayout.Label(((_consumptionStorageRatio * 100).ToString("0.0000") + "%"), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_HeavyWaterElectroliser_HeavyWaterConsumptionRate"), _bold_label, GUILayout.Width(labelWidth));//"Heavy Water Consumption Rate"
            GUILayout.Label((_heavyWaterConsumptionRate * GameConstants.SECONDS_IN_HOUR).ToString("0.0000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_HeavyWaterElectroliser_DeuteriumStorage"), _bold_label, GUILayout.Width(labelWidth));//"Deuterium Storage"
            GUILayout.Label(_spareRoomDeuteriumMass.ToString("0.00000") + " mT / " + _maxCapacityDeuteriumMass.ToString("0.00000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_HeavyWaterElectroliser_DeuteriumProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Deuterium Production Rate"
            GUILayout.Label((_deuteriumProductionRate * GameConstants.SECONDS_IN_HOUR).ToString("0.0000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_HeavyWaterElectroliser_OxygenStorage"), _bold_label, GUILayout.Width(labelWidth));//"Oxygen Storage"
            GUILayout.Label(_spareRoomOxygenMass.ToString("0.0000") + " mT / " + _maxCapacityOxygenMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_HeavyWaterElectroliser_OxygenProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Oxygen Production Rate"
            GUILayout.Label((_oxygenProductionRate * GameConstants.SECONDS_IN_HOUR).ToString("0.0000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void UpdateStatusMessage()
        {
            if (_deuteriumProductionRate > 0 && _oxygenProductionRate > 0)
                _status = Localizer.Format("#LOC_KSPIE_HeavyWaterElectroliser_Statumsg1");//"Electrolyzing Water"
            else if (_fixedMaxConsumptionWaterRate <= 0.0000000001)
                _status = Localizer.Format("#LOC_KSPIE_HeavyWaterElectroliser_Statumsg2");//"Out of water"
            else if (_deuteriumProductionRate > 0)
                _status = Localizer.Format("#LOC_KSPIE_HeavyWaterElectroliser_Statumsg3", _oxygenResourceName);//"Insufficient " +  + " Storage"
            else if (_oxygenProductionRate > 0)
                _status = Localizer.Format("#LOC_KSPIE_HeavyWaterElectroliser_Statumsg3", _deuteriumResourceName);//"Insufficient " +  + " Storage"
            else if (CurrentPower <= 0.01 * PowerRequirements)
                _status = Localizer.Format("#LOC_KSPIE_HeavyWaterElectroliser_Statumsg4");//"Insufficient Power"
            else
                _status = Localizer.Format("#LOC_KSPIE_HeavyWaterElectroliser_Statumsg5");//"Insufficient Storage"
        }

        public void PrintMissingResources()
        {
            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_HeavyWaterElectroliser_Postmsg") + " " + _waterHeavyResourceName, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
        }
    }
}
