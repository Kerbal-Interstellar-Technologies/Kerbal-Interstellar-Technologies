using KIT.Extensions;
using KIT.Resources;
using KIT.ResourceScheduler;
using KSP.Localization;
using System;
using System.Linq;
using UnityEngine;

namespace KIT.Refinery.Activity
{
    class WaterGasShift : RefineryActivity, IRefineryActivity
    {
        public WaterGasShift()
        {
            ActivityName = "Water Gas Shift";
            Formula = "H<size=7>2</size>0 + CO => CO<size=7>2</size> + H<size=7>2</size>";
            PowerRequirements = PluginSettings.Config.BaseHaberProcessPowerConsumption * 5;
            EnergyPerTon = PluginSettings.Config.HaberProcessEnergyPerTon;
        }

        private const double WaterMassByFraction = 18.01528 / (18.01528 + 28.010);
        private const double MonoxideMassByFraction = 1 - WaterMassByFraction;
        private const double HydrogenMassByFraction = (2 * 1.008) / (44.01 + (2 * 1.008));
        private const double DioxideMassByFraction = 1 - HydrogenMassByFraction;

        private double _fixedConsumptionRate;
        private double _consumptionStorageRatio;

        private double _waterConsumptionRate;
        private double _monoxideConsumptionRate;
        private double _hydrogenProductionRate;
        private double _dioxideProductionRate;

        private PartResourceDefinition _water;
        private PartResourceDefinition _dioxide;
        private PartResourceDefinition _hydrogen;
        private PartResourceDefinition _monoxide;

        private double _availableWaterMass;
        private double _availableMonoxideMass;
        private double _spareRoomDioxideMass;
        private double _spareRoomHydrogenMass;

        private double _maxCapacityWaterMass;
        private double _maxCapacityDioxideMass;
        private double _maxCapacityMonoxideMass;
        private double _maxCapacityHydrogenMass;

        public RefineryType RefineryType => RefineryType.Synthesize;

        public bool HasActivityRequirements()
        {
            return _part.GetConnectedResources(KITResourceSettings.WaterPure).Any(rs => rs.amount > 0) && _part.GetConnectedResources(KITResourceSettings.CarbonMonoxideGas).Any(rs => rs.amount > 0);
        }

        public string Status => string.Copy(_status);

        public void Initialize(Part localPart)
        {
            _part = localPart;
            _vessel = localPart.vessel;

            _water = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.WaterPure);
            _dioxide = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.CarbonDioxideLqd);
            _hydrogen = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.HydrogenLqd);
            _monoxide = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.CarbonMonoxideLqd);
        }

        public void UpdateFrame(IResourceManager resMan, double rateMultiplier, double powerFraction, double productionModifier, bool allowOverflow, bool isStartup = false)
        {
            _allowOverflow = allowOverflow;

            // determine how much mass we can produce at max
            _current_power = PowerRequirements * rateMultiplier;
            _current_rate = CurrentPower / EnergyPerTon;

            resMan.CapacityInformation(ResourceName.CarbonDioxideLqd, out _maxCapacityDioxideMass,
                out _spareRoomDioxideMass, out _, out _);
            resMan.CapacityInformation(ResourceName.CarbonMonoxideGas, out _maxCapacityMonoxideMass, out _,
                out _availableMonoxideMass, out _);
            resMan.CapacityInformation(ResourceName.WaterPure, out _maxCapacityWaterMass, out _,
                out _availableWaterMass, out _);
            resMan.CapacityInformation(ResourceName.HydrogenLqd, out _maxCapacityHydrogenMass,
                out _spareRoomHydrogenMass, out _, out _);

            _maxCapacityDioxideMass *= _dioxide.density;
            _spareRoomDioxideMass *= _dioxide.density;

            _maxCapacityMonoxideMass *= _monoxide.density;
            _availableMonoxideMass *= _monoxide.density;

            _maxCapacityWaterMass *= _water.density;
            _availableWaterMass *= _water.density;

            _maxCapacityHydrogenMass *= _hydrogen.density;
            _spareRoomHydrogenMass *= _hydrogen.density;
            

            // determine how much carbon dioxide we can consume
            var fixedMaxWaterConsumptionRate = _current_rate * WaterMassByFraction;
            var waterConsumptionRatio = fixedMaxWaterConsumptionRate > 0 ? Math.Min(fixedMaxWaterConsumptionRate, _availableWaterMass) / fixedMaxWaterConsumptionRate : 0;

            var fixedMaxMonoxideConsumptionRate =  _current_rate * MonoxideMassByFraction;
            var monoxideConsumptionRatio = fixedMaxMonoxideConsumptionRate > 0 ? Math.Min(fixedMaxMonoxideConsumptionRate, _availableMonoxideMass) / fixedMaxMonoxideConsumptionRate : 0;

            _fixedConsumptionRate = _current_rate * Math.Min(waterConsumptionRatio, monoxideConsumptionRatio);

            if (_fixedConsumptionRate > 0 && (_spareRoomHydrogenMass > 0 || _spareRoomDioxideMass > 0))
            {
                // calculate consumptionStorageRatio
                var fixedMaxHydrogenRate = _fixedConsumptionRate * HydrogenMassByFraction;
                var fixedMaxDioxideRate = _fixedConsumptionRate * DioxideMassByFraction;

                var fixedMaxPossibleHydrogenRate = allowOverflow ? fixedMaxHydrogenRate : Math.Min(_spareRoomHydrogenMass, fixedMaxHydrogenRate);
                var fixedMaxPossibleDioxideRate = allowOverflow ? fixedMaxDioxideRate : Math.Min(_spareRoomDioxideMass, fixedMaxDioxideRate);

                _consumptionStorageRatio = Math.Min(fixedMaxPossibleHydrogenRate / fixedMaxHydrogenRate, fixedMaxPossibleDioxideRate / fixedMaxDioxideRate);

                // now we do the real electrolysis
                _waterConsumptionRate = resMan.Consume(ResourceName.WaterPure, WaterMassByFraction * _consumptionStorageRatio * _fixedConsumptionRate / _water.density) / _water.density;
                _monoxideConsumptionRate = resMan.Consume(ResourceName.CarbonMonoxideGas, MonoxideMassByFraction * _consumptionStorageRatio * _fixedConsumptionRate / _monoxide.density) / _monoxide.density;
                var combinedConsumptionRate = _waterConsumptionRate + _monoxideConsumptionRate;

                var hydrogenRateTemp = combinedConsumptionRate * HydrogenMassByFraction;
                var dioxideRateTemp = combinedConsumptionRate * DioxideMassByFraction;

                resMan.Produce(ResourceName.HydrogenLqd, hydrogenRateTemp / _hydrogen.density);
                resMan.Produce(ResourceName.CarbonDioxideLqd, dioxideRateTemp / _dioxide.density);

                _hydrogenProductionRate = hydrogenRateTemp;
                _dioxideProductionRate = dioxideRateTemp;
            }
            else
            {
                _waterConsumptionRate = 0;
                _monoxideConsumptionRate = 0;
                _hydrogenProductionRate = 0;
                _dioxideProductionRate = 0;
            }

            UpdateStatusMessage();
        }

        public override void UpdateGUI()
        {
            base.UpdateGUI();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_WaterGasShift_Power"), _bold_label, GUILayout.Width(labelWidth));//"Power"
            GUILayout.Label(PluginHelper.GetFormattedPowerString(CurrentPower) + "/" + PluginHelper.GetFormattedPowerString(PowerRequirements), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_WaterGasShift_Consumption"), _bold_label, GUILayout.Width(labelWidth));//"Current Consumption"
            GUILayout.Label(((_fixedConsumptionRate / TimeWarp.fixedDeltaTime * GameConstants.SecondsInHour).ToString("0.0000")) + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_WaterGasShift_ConsumptionStorageRatio"), _bold_label, GUILayout.Width(labelWidth));//"Consumption Storage Ratio"
            GUILayout.Label(((_consumptionStorageRatio * 100).ToString("0.0000") + "%"), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_WaterGasShift_WaterAvailable"), _bold_label, GUILayout.Width(labelWidth));//"Water Available"
            GUILayout.Label(_availableWaterMass.ToString("0.0000") + " mT / " + _maxCapacityWaterMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_WaterGasShift_ConsumptionRate"), _bold_label, GUILayout.Width(labelWidth));//"Water Consumption Rate"
            GUILayout.Label((_waterConsumptionRate * GameConstants.SecondsInHour).ToString("0.0000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_WaterGasShift_CarbonMonoxideAvailable"), _bold_label, GUILayout.Width(labelWidth));//"CarbonMonoxide Available"
            GUILayout.Label(_availableMonoxideMass.ToString("0.0000") + " mT / " + _maxCapacityMonoxideMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_WaterGasShift_CarbonMonoxideConsumptionRate"), _bold_label, GUILayout.Width(labelWidth));//"CarbonMonoxide Consumption Rate"
            GUILayout.Label((_monoxideConsumptionRate * GameConstants.SecondsInHour).ToString("0.0000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_WaterGasShift_CarbonDioxideStorage"), _bold_label, GUILayout.Width(labelWidth));//"CarbonDioxide Storage"
            GUILayout.Label(_spareRoomDioxideMass.ToString("0.0000") + " mT / " + _maxCapacityDioxideMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_WaterGasShift_CarbonDioxideProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"CarbonDioxide Production Rate"
            GUILayout.Label((_dioxideProductionRate * GameConstants.SecondsInHour).ToString("0.0000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_WaterGasShift_HydrogenStorage"), _bold_label, GUILayout.Width(labelWidth));//"Hydrogen Storage"
            GUILayout.Label(_spareRoomHydrogenMass.ToString("0.00000") + " mT / " + _maxCapacityHydrogenMass.ToString("0.00000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_WaterGasShift_HydrogenProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Hydrogen Production Rate"
            GUILayout.Label((_dioxideProductionRate * GameConstants.SecondsInHour).ToString("0.00000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void UpdateStatusMessage()
        {
            if (_hydrogenProductionRate > 0 && _dioxideProductionRate > 0)
                _status = Localizer.Format("#LOC_KSPIE_WaterGasShift_Statumsg1");//"Water Gas Swifting"
            else if (_fixedConsumptionRate <= 0.0000000001)
            {
                _status = Localizer.Format(_availableWaterMass <= 0.0000000001 ? "#LOC_KSPIE_WaterGasShift_Statumsg2" : "#LOC_KSPIE_WaterGasShift_Statumsg3");
            }
            else if (_hydrogenProductionRate > 0)
                _status = _allowOverflow
                    ? Localizer.Format("#LOC_KSPIE_WaterGasShift_Statumsg4")
                    : Localizer.Format("#LOC_KSPIE_WaterGasShift_Statumsg5",
                        KITResourceSettings.CarbonDioxideLqd); //"Overflowing ""Insufficient " +  + " Storage"
            else if (_dioxideProductionRate > 0)
                _status = _allowOverflow ? Localizer.Format("#LOC_KSPIE_WaterGasShift_Statumsg4") : Localizer.Format("#LOC_KSPIE_WaterGasShift_Statumsg5", KITResourceSettings.HydrogenLqd);//"Overflowing ""Insufficient " +  + " Storage"
            else if (CurrentPower <= 0.01 * PowerRequirements)
                _status = Localizer.Format("#LOC_KSPIE_WaterGasShift_Statumsg6");//"Insufficient Power"
            else
                _status = Localizer.Format("#LOC_KSPIE_WaterGasShift_Statumsg7");//"Insufficient Storage"
        }

        public void PrintMissingResources()
        {
            if (!_part.GetConnectedResources(KITResourceSettings.WaterPure).Any(rs => rs.amount > 0))
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_WaterGasShift_Postmsg") +" " + KITResourceSettings.WaterPure, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
            if (!_part.GetConnectedResources(KITResourceSettings.CarbonMonoxideGas).Any(rs => rs.amount > 0))
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_WaterGasShift_Postmsg") + " " + KITResourceSettings.CarbonMonoxideGas, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
        }
    }
}
