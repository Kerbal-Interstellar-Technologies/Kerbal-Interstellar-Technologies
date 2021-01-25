using KIT.Extensions;
using KIT.Resources;
using KIT.ResourceScheduler;
using KSP.Localization;
using System;
using System.Linq;
using UnityEngine;

namespace KIT.Refinery.Activity
{
    [KSPModule("ISRU Methane Pyrolysis")]
    class MethanePyrolysis : RefineryActivity, IRefineryActivity
    {
        public MethanePyrolysis()
        {
            ActivityName = "Methane Pyrolysis";
            Formula = "CH<size=7>4</size> => H<size=7>2</size> + C";
            PowerRequirements = PluginSettings.Config.BaseELCPowerConsumption;
            EnergyPerTon = PluginSettings.Config.ElectrolysisEnergyPerTon;
        }

        private double _monoxideDensity;
        private double _methaneDensity;
        private double _hydrogenDensity;
        private double _oxygenDensity;

        private double _methaneConsumptionRate;
        private double _oxygenConsumptionRate;

        private double _hydrogenProductionRate;
        private double _monoxideProductionRate;

        private string _monoxideResourceName;
        private string _methaneResourceName;
        private string _hydrogenResourceName;
        private string _oxygenResourceName;

        private double _maxCapacityMonoxideMass;
        private double _maxCapacityHydrogenMass;
        private double _maxCapacityMethaneMass;
        private double _maxCapacityOxygenMass;

        private double _availableMethaneMass;
        private double _availableOxygenMass;
        private double _spareRoomHydrogenMass;
        private double _spareRoomMonoxideMass;

        private const double MonoxideMassByFraction = 1 - 18.01528 / (18.01528 + 28.010); // taken from reverse water gas shift
        private const double HydrogenMassByFraction = (8 * 1.008) / (44.01 + (8 * 1.008));
        private const double OxygenMassByFraction = 32.0 / 52.0;
        private const double MethaneMassByFraction = 20.0 / 52.0;

        public RefineryType RefineryType => RefineryType.Heating;

        public bool HasActivityRequirements()
        {
            return _part.GetConnectedResources(_methaneResourceName).Any(rs => rs.amount > 0) &
                _part.GetConnectedResources(_oxygenResourceName).Any(rs => rs.amount > 0);
        }

        public string Status => string.Copy(_status);


        public void Initialize(Part localPart)
        {
            _part = localPart;
            _vessel = localPart.vessel;

            _monoxideResourceName = KITResourceSettings.CarbonMonoxideLqd;
            _hydrogenResourceName = KITResourceSettings.HydrogenLqd;
            _methaneResourceName = KITResourceSettings.MethaneLqd;
            _oxygenResourceName = KITResourceSettings.OxygenLqd;

            Debug.Log("${_monoxideResourceName}");
            _monoxideDensity = PartResourceLibrary.Instance.GetDefinition(_monoxideResourceName).density;
            Debug.Log("${_hydrogenResourceName}");
            _hydrogenDensity = PartResourceLibrary.Instance.GetDefinition(_hydrogenResourceName).density;
            Debug.Log("${_methaneResourceName}");
            _methaneDensity = PartResourceLibrary.Instance.GetDefinition(_methaneResourceName).density;
            Debug.Log("${_oxygenResourceName}");
            _oxygenDensity = PartResourceLibrary.Instance.GetDefinition(_oxygenResourceName).density;
        }

        private double _combinedConsumptionRate;

        public void UpdateFrame(IResourceManager resMan, double rateMultiplier, double powerFraction, double productionModifier, bool allowOverflow, bool isStartup = false)
        {
            _current_power = PowerRequirements * rateMultiplier;
            _current_rate = CurrentPower / EnergyPerTon;

            // determine how much resource we have
            resMan.CapacityInformation(ResourceName.CarbonMonoxideLqd, out _maxCapacityMonoxideMass, out _spareRoomMonoxideMass, out _,
                out _);
            resMan.CapacityInformation(ResourceName.HydrogenLqd, out _maxCapacityHydrogenMass, out _, out _spareRoomHydrogenMass, out _);
            resMan.CapacityInformation(ResourceName.MethaneLqd, out _maxCapacityMethaneMass, out _,
                out _availableMethaneMass, out _);
            resMan.CapacityInformation(ResourceName.OxygenLqd, out _maxCapacityOxygenMass, out _,
                out _availableOxygenMass, out _);

            _maxCapacityMonoxideMass *= _monoxideDensity;
            _spareRoomMonoxideMass *= _monoxideDensity;

            _maxCapacityHydrogenMass *= _hydrogenDensity;
            _spareRoomHydrogenMass *= _hydrogenDensity;

            _maxCapacityMethaneMass *= _methaneDensity;
            _availableMethaneMass *= _methaneDensity;

            _maxCapacityOxygenMass *= _oxygenDensity;
            _availableOxygenMass *= _oxygenDensity;

            // this should determine how much resources this process can consume
            var fixedMaxMethaneConsumptionRate = _current_rate * MethaneMassByFraction;
            var methaneConsumptionRatio = fixedMaxMethaneConsumptionRate > 0
                ? Math.Min(fixedMaxMethaneConsumptionRate, _availableMethaneMass) / fixedMaxMethaneConsumptionRate
                : 0;

            var fixedMaxOxygenConsumptionRate = _current_rate * OxygenMassByFraction;
            var oxygenConsumptionRatio = fixedMaxOxygenConsumptionRate > 0 ? Math.Min(fixedMaxOxygenConsumptionRate, _availableOxygenMass) / fixedMaxOxygenConsumptionRate : 0;

            var fixedConsumptionRate = _current_rate * Math.Min(methaneConsumptionRatio, oxygenConsumptionRatio);

            // begin the pyrolysis process
            if (fixedConsumptionRate > 0 && (_spareRoomHydrogenMass > 0 || _spareRoomMonoxideMass > 0))
            {

                var fixedMaxMonoxideRate = fixedConsumptionRate * MonoxideMassByFraction;
                var fixedMaxHydrogenRate = fixedConsumptionRate * HydrogenMassByFraction;

                var fixedMaxPossibleMonoxideRate = allowOverflow ? fixedMaxMonoxideRate : Math.Min(_spareRoomMonoxideMass, fixedMaxMonoxideRate);
                var fixedMaxPossibleHydrogenRate = allowOverflow ? fixedMaxHydrogenRate : Math.Min(_spareRoomHydrogenMass, fixedMaxHydrogenRate);

                var consumptionStorageRatio = Math.Min(fixedMaxPossibleMonoxideRate / fixedMaxMonoxideRate, fixedMaxPossibleHydrogenRate / fixedMaxHydrogenRate);

                // this consumes the resources
                _oxygenConsumptionRate = resMan.Consume(ResourceName.OxygenLqd, OxygenMassByFraction * consumptionStorageRatio * fixedConsumptionRate / _oxygenDensity) / _oxygenDensity;
                
                _methaneConsumptionRate = resMan.Consume(ResourceName.MethaneLqd, MethaneMassByFraction * consumptionStorageRatio * fixedConsumptionRate / _methaneDensity) / _methaneDensity;
                _combinedConsumptionRate = _oxygenConsumptionRate + _methaneConsumptionRate;

                // this produces the products
                var monoxideRateTemp = _combinedConsumptionRate * MonoxideMassByFraction;
                var waterRateTemp = _combinedConsumptionRate * HydrogenMassByFraction;

                _monoxideProductionRate = resMan.Produce(ResourceName.CarbonMonoxideLqd, monoxideRateTemp / _monoxideDensity) / _monoxideDensity;
                _hydrogenProductionRate = resMan.Produce(ResourceName.HydrogenLqd, waterRateTemp / _hydrogenDensity) / _hydrogenDensity;
            }
            else
            {
                _methaneConsumptionRate = 0;
                _oxygenConsumptionRate = 0;
                _hydrogenProductionRate = 0;
            }
            UpdateStatusMessage();
        }

        public override void UpdateGUI()
        {
            base.UpdateGUI();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_MethanePyrolysis_Power"), _bold_label, GUILayout.Width(labelWidth));//"Power"
            GUILayout.Label(PluginHelper.GetFormattedPowerString(CurrentPower) + "/" + PluginHelper.GetFormattedPowerString(PowerRequirements), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_MethanePyrolysis_OveralConsumption"), _bold_label, GUILayout.Width(labelWidth));//"Overal Consumption"
            GUILayout.Label(((_combinedConsumptionRate * GameConstants.SecondsInHour).ToString("0.0000")) + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_MethanePyrolysis_OxygenAvailable"), _bold_label, GUILayout.Width(labelWidth));//"Oxygen Available"
            GUILayout.Label(_availableOxygenMass.ToString("0.0000") + " mT / " + _maxCapacityOxygenMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_MethanePyrolysis_OxygenConsumptionRate"), _bold_label, GUILayout.Width(labelWidth));//"Oxygen Consumption Rate"
            GUILayout.Label((_oxygenConsumptionRate * GameConstants.SecondsInHour).ToString("0.000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_MethanePyrolysis_MethaneAvailable"), _bold_label, GUILayout.Width(labelWidth));//"Methane Available"
            GUILayout.Label(_availableMethaneMass.ToString("0.0000") + " mT / " + _maxCapacityMethaneMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_MethanePyrolysis_ConsumptionRate"), _bold_label, GUILayout.Width(labelWidth));//"Methane Consumption Rate"
            GUILayout.Label((_methaneConsumptionRate * GameConstants.SecondsInHour).ToString("0.000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_MethanePyrolysis_HydrogenStorage"), _bold_label, GUILayout.Width(labelWidth));//"Hydrogen Storage"
            GUILayout.Label(_spareRoomHydrogenMass.ToString("0.0000") + " mT / " + _maxCapacityHydrogenMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_MethanePyrolysis_HydrogenProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Hydrogen Production Rate"
            GUILayout.Label((_hydrogenProductionRate * GameConstants.SecondsInHour).ToString("0.000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_MethanePyrolysis_CarbonMonoxideStorage"), _bold_label, GUILayout.Width(labelWidth));//"Carbon Monoxide Storage"
            GUILayout.Label(_spareRoomMonoxideMass.ToString("0.0000") + " mT / " + _maxCapacityMonoxideMass.ToString("0.0000") + " mT", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_MethanePyrolysis_CarbonMonoxideProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Carbon Monoxide Production Rate"
            GUILayout.Label((_monoxideProductionRate * GameConstants.SecondsInHour).ToString("0.000") + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void UpdateStatusMessage()
        {
            if (_hydrogenProductionRate > 0)
                _status = Localizer.Format("#LOC_KSPIE_MethanePyrolysis_Statumsg1");//"Methane Pyrolysis Ongoing"
            else if (CurrentPower <= 0.01 * PowerRequirements)
                _status = Localizer.Format("#LOC_KSPIE_MethanePyrolysis_Statumsg2");//"Insufficient Power"
            else
                _status = Localizer.Format("#LOC_KSPIE_MethanePyrolysis_Statumsg3");//"Insufficient Storage"
        }

        public void PrintMissingResources()
        {
            if (!_part.GetConnectedResources(_methaneResourceName).Any(rs => rs.amount > 0))
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_MethanePyrolysis_PostMsg") + " " + _methaneResourceName, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
            if (!_part.GetConnectedResources(_oxygenResourceName).Any(rs => rs.amount > 0))
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_MethanePyrolysis_PostMsg") + " " + _oxygenResourceName, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
        }
    }
}
