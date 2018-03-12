﻿using System;
using System.Linq;
using UnityEngine;
using FNPlugin.Extensions;

namespace FNPlugin.Refinery
{
    class CarbonDioxideElectroliser : RefineryActivityBase, IRefineryActivity
    {
        const double carbonMonoxideMassByFraction = 28.010 / (28.010 + 15.999);
        const double oxygenMassByFraction = 1 - carbonMonoxideMassByFraction;

        double _fixedMaxConsumptionDioxideRate;
        double _consumptionStorageRatio;

        double _dioxide_consumption_rate;
        double _monoxide_production_rate;
        double _oxygen_production_rate;

        string _dioxideResourceName;
        string _oxygenResourceName;
        string _monoxideResourceName;

        double _dioxide_density;
        double _oxygen_density;
        double _monoxide_density;

        double _availableDioxideMass;
        double _spareRoomOxygenMass;
        double _spareRoomMonoxideMass;

        double _maxCapacityDioxideMass;
        double _maxCapacityMonoxideMass;
        double _maxCapacityOxygenMass;

        public RefineryType RefineryType { get { return RefineryType.electrolysis; } }

        public String ActivityName { get { return "CarbonDioxide Electrolysis"; } }

        public bool HasActivityRequirements { get { return _part.GetConnectedResources(_dioxideResourceName).Any(rs => rs.amount > 0); } }

        public double PowerRequirements { get { return PluginHelper.BaseELCPowerConsumption; } }

        public String Status { get { return String.Copy(_status); } }

        public CarbonDioxideElectroliser(Part part) 
        {
            _part = part;
            _vessel = part.vessel;

            _dioxideResourceName = InterstellarResourcesConfiguration.Instance.CarbonDioxide;
            _oxygenResourceName = InterstellarResourcesConfiguration.Instance.LqdOxygen;
            _monoxideResourceName = InterstellarResourcesConfiguration.Instance.CarbonMoxoxide;
            
            _dioxide_density = PartResourceLibrary.Instance.GetDefinition(_dioxideResourceName).density;
            _oxygen_density = PartResourceLibrary.Instance.GetDefinition(_oxygenResourceName).density;
            _monoxide_density = PartResourceLibrary.Instance.GetDefinition(_monoxideResourceName).density;
        }

        public void UpdateFrame(double rateMultiplier, double powerFraction, double productionModidier, bool allowOverflow, double fixedDeltaTime)
        {
            // determine how much mass we can produce at max
            _current_power = PowerRequirements * rateMultiplier;
            _current_rate = CurrentPower / PluginHelper.ElectrolysisEnergyPerTon;

            var partsThatContainDioxide = _part.GetConnectedResources(_dioxideResourceName);
            var partsThatContainOxygen = _part.GetConnectedResources(_oxygenResourceName);
            var partsThatContainMonoxide = _part.GetConnectedResources(_monoxideResourceName);

            _maxCapacityDioxideMass = partsThatContainDioxide.Sum(p => p.maxAmount) * _dioxide_density;
            _maxCapacityOxygenMass = partsThatContainOxygen.Sum(p => p.maxAmount) * _oxygen_density;
            _maxCapacityMonoxideMass = partsThatContainMonoxide.Sum(p => p.maxAmount) * _monoxide_density;

            _availableDioxideMass = partsThatContainDioxide.Sum(p => p.amount) * _dioxide_density;
            _spareRoomOxygenMass = partsThatContainOxygen.Sum(r => r.maxAmount - r.amount) * _oxygen_density;
            _spareRoomMonoxideMass = partsThatContainMonoxide.Sum(r => r.maxAmount - r.amount) * _monoxide_density;

            // determine how much carbondioxide we can consume
            _fixedMaxConsumptionDioxideRate = Math.Min(_current_rate * fixedDeltaTime, _availableDioxideMass);

            if (_fixedMaxConsumptionDioxideRate > 0 && (_spareRoomOxygenMass > 0 || _spareRoomMonoxideMass > 0))
            {
                // calculate consumptionStorageRatio
                var fixedMaxMonoxideRate = _fixedMaxConsumptionDioxideRate * carbonMonoxideMassByFraction;
                var fixedMaxOxygenRate = _fixedMaxConsumptionDioxideRate * oxygenMassByFraction;

                var fixedMaxPossibleMonoxideRate = allowOverflow ? fixedMaxMonoxideRate : Math.Min(_spareRoomMonoxideMass, fixedMaxMonoxideRate);
                var fixedMaxPossibleOxygenRate = allowOverflow ? fixedMaxOxygenRate : Math.Min(_spareRoomOxygenMass, fixedMaxOxygenRate);

                var fixedMaxPossibleMonoxideRatio = fixedMaxPossibleMonoxideRate / fixedMaxMonoxideRate;
                var fixedMaxPossibleOxygenRatio = fixedMaxPossibleOxygenRate / fixedMaxOxygenRate;
                _consumptionStorageRatio = Math.Min(fixedMaxPossibleMonoxideRatio, fixedMaxPossibleOxygenRatio);

                // now we do the real elextrolysis
                _dioxide_consumption_rate = _part.RequestResource(_dioxideResourceName, _consumptionStorageRatio * _fixedMaxConsumptionDioxideRate / _dioxide_density) / fixedDeltaTime * _dioxide_density;

                var monoxide_rate_temp = _dioxide_consumption_rate * carbonMonoxideMassByFraction;
                var oxygen_rate_temp = _dioxide_consumption_rate * oxygenMassByFraction;

                _monoxide_production_rate = -_part.RequestResource(_monoxideResourceName, -monoxide_rate_temp * fixedDeltaTime / _monoxide_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _monoxide_density;
                _oxygen_production_rate = -_part.RequestResource(_oxygenResourceName, -oxygen_rate_temp * fixedDeltaTime / _oxygen_density, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _oxygen_density;
            }
            else
            {
                _dioxide_consumption_rate = 0;
                _monoxide_production_rate = 0;
                _oxygen_production_rate = 0;
            }

            updateStatusMessage();
        }

        public override void UpdateGUI()
        {
            base.UpdateGUI();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Power", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(PluginHelper.getFormattedPowerString(CurrentPower) + "/" + PluginHelper.getFormattedPowerString(PowerRequirements), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Consumption Storage Ratio", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(((_consumptionStorageRatio * 100).ToString("0.0000") + "%"), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("CarbonDioxide Available", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_availableDioxideMass.ToString("0.0000") + " mT / " + _maxCapacityDioxideMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("CarbonDioxide Consumption Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((_dioxide_consumption_rate * GameConstants.SECONDS_IN_HOUR).ToString("0.0000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("CarbonMonoxide Storage", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_spareRoomMonoxideMass.ToString("0.00000") + " mT / " + _maxCapacityMonoxideMass.ToString("0.00000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("CarbonMonoxide Production Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((_monoxide_production_rate * GameConstants.SECONDS_IN_HOUR).ToString("0.0000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Oxygen Storage", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_spareRoomOxygenMass.ToString("0.0000") + " mT / " + _maxCapacityOxygenMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Oxygen Production Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((_oxygen_production_rate * GameConstants.SECONDS_IN_HOUR).ToString("0.0000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void updateStatusMessage()
        {
            if (_monoxide_production_rate > 0 && _oxygen_production_rate > 0)
                _status = "Electrolysing CarbonDioxide";
            else if (_fixedMaxConsumptionDioxideRate <= 0.0000000001)
                _status = "Out of CarbonDioxide";
            else if (_monoxide_production_rate > 0)
                _status = "Insufficient " + _oxygenResourceName + " Storage";
            else if (_oxygen_production_rate > 0)
                _status = "Insufficient " + _monoxideResourceName + " Storage";
            else if (CurrentPower <= 0.01 * PowerRequirements)
                _status = "Insufficient Power";
            else
                _status = "Insufficient Storage";
        }

        public void PrintMissingResources()
        {
            ScreenMessages.PostScreenMessage("Missing " + InterstellarResourcesConfiguration.Instance.CarbonDioxide, 3.0f, ScreenMessageStyle.UPPER_CENTER);
        }
    }
}
