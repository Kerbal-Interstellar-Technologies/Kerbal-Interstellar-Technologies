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
    class ReverseWaterGasShift : RefineryActivity, IRefineryActivity
    {
        public ReverseWaterGasShift()
        {
            ActivityName = "Reverse Water Gas Shift";
            PowerRequirements = PluginHelper.BaseHaberProcessPowerConsumption * 5;
            EnergyPerTon = PluginHelper.HaberProcessEnergyPerTon;
        }

        private const double WaterMassByFraction = 18.01528 / (18.01528 + 28.010);
        private const double MonoxideMassByFraction = 1 - WaterMassByFraction;
        private const double HydrogenMassByFraction = (2 * 1.008) / (44.01 + (2 * 1.008));
        private const double DioxideMassByFraction = 1 - HydrogenMassByFraction;

        private double _fixedConsumptionRate;
        private double _consumptionRate;
        private double _consumptionStorageRatio;

        private double _dioxideConsumptionRate;
        private double _hydrogenConsumptionRate;
        private double _monoxideProductionRate;
        private double _waterProductionRate;

        private string _waterResourceName;
        private string _monoxideResourceName;
        private string _dioxideResourceName;
        private string _hydrogenResourceName;

        private double _waterDensity;
        private double _dioxideDensity;
        private double _hydrogenDensity;
        private double _monoxideDensity;

        private double _availableDioxideMass;
        private double _availableHydrogenMass;
        private double _spareRoomWaterMass;
        private double _spareRoomMonoxideMass;

        private double _maxCapacityWaterMass;
        private double _maxCapacityDioxideMass;
        private double _maxCapacityMonoxideMass;
        private double _maxCapacityHydrogenMass;

        public RefineryType RefineryType => RefineryType.Synthesize;

        public bool HasActivityRequirements()
        {
            return _part.GetConnectedResources(_dioxideResourceName).Any(rs => rs.amount > 0) &&
                   _part.GetConnectedResources(_hydrogenResourceName).Any(rs => rs.amount > 0);
        }

        public string Status => string.Copy(_status);

        public void Initialize(Part localPart)
        {
            _part = localPart;
            _vessel = localPart.vessel;

            _waterResourceName = ResourceSettings.Config.WaterPure;
            _monoxideResourceName = ResourceSettings.Config.CarbonMonoxideGas;
            _dioxideResourceName = ResourceSettings.Config.CarbonDioxideLqd;
            _hydrogenResourceName = ResourceSettings.Config.HydrogenLqd;

            _waterDensity = PartResourceLibrary.Instance.GetDefinition(_waterResourceName).density;
            _dioxideDensity = PartResourceLibrary.Instance.GetDefinition(_dioxideResourceName).density;
            _hydrogenDensity = PartResourceLibrary.Instance.GetDefinition(_hydrogenResourceName).density;
            _monoxideDensity = PartResourceLibrary.Instance.GetDefinition(_monoxideResourceName).density;
        }

        public void UpdateFrame(IResourceManager resMan, double rateMultiplier, double powerFraction, double productionModifier, bool allowOverflow,  bool isStartup = false)
        {
            _allowOverflow = allowOverflow;

            // determine overall maximum production rate
            _current_power = PowerRequirements * rateMultiplier;
            _current_rate = CurrentPower / EnergyPerTon;

            // determine how much resource we have
            var partsThatContainWater = _part.GetConnectedResources(_waterResourceName).ToList();
            var partsThatContainMonoxide = _part.GetConnectedResources(_monoxideResourceName).ToList();
            var partsThatContainHydrogen = _part.GetConnectedResources(_hydrogenResourceName).ToList();
            var partsThatContainDioxide = _part.GetConnectedResources(_dioxideResourceName).ToList();

            _maxCapacityWaterMass = partsThatContainWater.Sum(p => p.maxAmount) * _waterDensity;
            _maxCapacityDioxideMass = partsThatContainDioxide.Sum(p => p.maxAmount) * _dioxideDensity;
            _maxCapacityHydrogenMass = partsThatContainHydrogen.Sum(p => p.maxAmount) * _hydrogenDensity;
            _maxCapacityMonoxideMass = partsThatContainMonoxide.Sum(p => p.maxAmount) * _monoxideDensity;

            _availableDioxideMass = partsThatContainDioxide.Sum(r => r.amount) * _dioxideDensity;
            _availableHydrogenMass = partsThatContainHydrogen.Sum(r => r.amount) * _hydrogenDensity;

            _spareRoomWaterMass = partsThatContainWater.Sum(r => r.maxAmount - r.amount) * _waterDensity;
            _spareRoomMonoxideMass = partsThatContainMonoxide.Sum(r => r.maxAmount - r.amount) * _monoxideDensity;

            // determine how much we can consume
            var fixedMaxDioxideConsumptionRate = _current_rate * DioxideMassByFraction ;
            var dioxideConsumptionRatio = fixedMaxDioxideConsumptionRate > 0 ? Math.Min(fixedMaxDioxideConsumptionRate, _availableDioxideMass) / fixedMaxDioxideConsumptionRate : 0;

            var fixedMaxHydrogenConsumptionRate =  _current_rate * HydrogenMassByFraction ;
            var hydrogenConsumptionRatio = fixedMaxHydrogenConsumptionRate > 0 ? Math.Min(fixedMaxHydrogenConsumptionRate, _availableHydrogenMass) / fixedMaxHydrogenConsumptionRate : 0;

            _fixedConsumptionRate = _current_rate  * Math.Min(dioxideConsumptionRatio, hydrogenConsumptionRatio);
            _consumptionRate = _fixedConsumptionRate;

            if (_fixedConsumptionRate > 0 && (_spareRoomMonoxideMass > 0 || _spareRoomWaterMass > 0))
            {
                // calculate consumptionStorageRatio
                var fixedMaxMonoxideRate = _fixedConsumptionRate * MonoxideMassByFraction;
                var fixedMaxWaterRate = _fixedConsumptionRate * WaterMassByFraction;

                var fixedMaxPossibleMonoxideRate = allowOverflow ? fixedMaxMonoxideRate : Math.Min(_spareRoomMonoxideMass, fixedMaxMonoxideRate);
                var fixedMaxPossibleWaterRate = allowOverflow ? fixedMaxWaterRate : Math.Min(_spareRoomWaterMass, fixedMaxWaterRate);

                _consumptionStorageRatio = Math.Min(fixedMaxPossibleMonoxideRate / fixedMaxMonoxideRate, fixedMaxPossibleWaterRate / fixedMaxWaterRate);

                // now we do the real consumption
                _dioxideConsumptionRate = _part.RequestResource(_dioxideResourceName, DioxideMassByFraction * _consumptionStorageRatio * _fixedConsumptionRate / _dioxideDensity) /  _dioxideDensity;
                _hydrogenConsumptionRate = _part.RequestResource(_hydrogenResourceName, HydrogenMassByFraction * _consumptionStorageRatio * _fixedConsumptionRate / _hydrogenDensity) /  _hydrogenDensity;
                var combinedConsumptionRate = _dioxideConsumptionRate + _hydrogenConsumptionRate;

                var monoxideRateTemp = combinedConsumptionRate * MonoxideMassByFraction;
                var waterRateTemp = combinedConsumptionRate * WaterMassByFraction;

                _monoxideProductionRate = -_part.RequestResource(_monoxideResourceName, -monoxideRateTemp / _monoxideDensity) / _monoxideDensity;
                _waterProductionRate = -_part.RequestResource(_waterResourceName, -waterRateTemp  / _waterDensity) / _waterDensity;
            }
            else
            {
                _dioxideConsumptionRate = 0;
                _hydrogenConsumptionRate = 0;
                _monoxideProductionRate = 0;
                _waterProductionRate = 0;
            }

            updateStatusMessage();
        }

        public override void UpdateGUI()
        {
            base.UpdateGUI();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_ReverseWaterGasShift_Power"), _bold_label, GUILayout.Width(labelWidth));//"Power"
            GUILayout.Label(PluginHelper.getFormattedPowerString(CurrentPower) + "/" + PluginHelper.getFormattedPowerString(PowerRequirements), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_ReverseWaterGasShift_CurrentConsumption"), _bold_label, GUILayout.Width(labelWidth));//"Current Consumption"
            GUILayout.Label(((_consumptionRate * GameConstants.SECONDS_IN_HOUR).ToString("0.0000")) + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_ReverseWaterGasShift_ConsumptionStorageRatio"), _bold_label, GUILayout.Width(labelWidth));//"Consumption Storage Ratio"
            GUILayout.Label(((_consumptionStorageRatio * 100).ToString("0.0000") + "%"), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_ReverseWaterGasShift_CarbonDioxideAvailable"), _bold_label, GUILayout.Width(labelWidth));//"CarbonDioxide Available"
            GUILayout.Label(_availableDioxideMass.ToString("0.0000") + " mT / " + _maxCapacityDioxideMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_ReverseWaterGasShift_CarbonDioxideConsumptionRate"), _bold_label, GUILayout.Width(labelWidth));//"CarbonDioxide Consumption Rate"
            GUILayout.Label((_dioxideConsumptionRate * GameConstants.SECONDS_IN_HOUR).ToString("0.0000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_ReverseWaterGasShift_HydrogenAvailable"), _bold_label, GUILayout.Width(labelWidth));//"Hydrogen Available"
            GUILayout.Label(_availableHydrogenMass.ToString("0.00000") + " mT / " + _maxCapacityHydrogenMass.ToString("0.00000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_ReverseWaterGasShift_HydrogenConsumptionRate"), _bold_label, GUILayout.Width(labelWidth));//"Hydrogen Consumption Rate"
            GUILayout.Label((_hydrogenConsumptionRate * GameConstants.SECONDS_IN_HOUR).ToString("0.00000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_ReverseWaterGasShift_WaterStorage"), _bold_label, GUILayout.Width(labelWidth));//"Water Storage"
            GUILayout.Label(_spareRoomWaterMass.ToString("0.0000") + " mT / " + _maxCapacityWaterMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_ReverseWaterGasShift_WaterProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Water Production Rate"
            GUILayout.Label((_waterProductionRate * GameConstants.SECONDS_IN_HOUR).ToString("0.0000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_ReverseWaterGasShift_MonoxideMonoxideStorage"), _bold_label, GUILayout.Width(labelWidth));//"MonoxideMonoxide Storage"
            GUILayout.Label(_spareRoomMonoxideMass.ToString("0.0000") + " mT / " + _maxCapacityMonoxideMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_ReverseWaterGasShift_MonoxideMonoxideProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"MonoxideMonoxide Production Rate"
            GUILayout.Label((_monoxideProductionRate * GameConstants.SECONDS_IN_HOUR).ToString("0.0000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void updateStatusMessage()
        {
            if (_monoxideProductionRate > 0 && _waterProductionRate > 0)
                _status = Localizer.Format("#LOC_KSPIE_ReverseWaterGasShift_Statumsg1");//"Water Gas Shifting"
            else if (_fixedConsumptionRate <= 0.0000000001)
                _status = Localizer.Format(_availableDioxideMass <= 0.0000000001 ? "#LOC_KSPIE_ReverseWaterGasShift_Statumsg2" : "#LOC_KSPIE_ReverseWaterGasShift_Statumsg3");
            else if (_monoxideProductionRate > 0)
                _status = _allowOverflow ? Localizer.Format("#LOC_KSPIE_ReverseWaterGasShift_Statumsg4") : Localizer.Format("#LOC_KSPIE_ReverseWaterGasShift_Statumsg5", _waterResourceName);//"Overflowing ""Insufficient " +  + " Storage"
            else if (_waterProductionRate > 0)
                _status = _allowOverflow ? Localizer.Format("#LOC_KSPIE_ReverseWaterGasShift_Statumsg4") : Localizer.Format("#LOC_KSPIE_ReverseWaterGasShift_Statumsg5", _monoxideResourceName);//"Overflowing ""Insufficient " +  + " Storage"
            else if (CurrentPower <= 0.01 * PowerRequirements)
                _status = Localizer.Format("#LOC_KSPIE_ReverseWaterGasShift_Statumsg6");//"Insufficient Power"
            else
                _status = Localizer.Format("#LOC_KSPIE_ReverseWaterGasShift_Statumsg7");//"Insufficient Storage"
        }

        public void PrintMissingResources()
        {
            if (!_part.GetConnectedResources(ResourceSettings.Config.CarbonDioxideLqd).Any(rs => rs.amount > 0))
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_ReverseWaterGasShift_Postmsg") + " " + ResourceSettings.Config.CarbonDioxideLqd, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
            if (!_part.GetConnectedResources(ResourceSettings.Config.HydrogenLqd).Any(rs => rs.amount > 0))
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_ReverseWaterGasShift_Postmsg") + " " + ResourceSettings.Config.HydrogenLqd, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
        }
    }
}
