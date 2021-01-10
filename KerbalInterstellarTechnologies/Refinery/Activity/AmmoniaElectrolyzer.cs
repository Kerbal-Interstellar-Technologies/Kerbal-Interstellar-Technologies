using KIT.Extensions;
using KIT.Resources;
using KIT.ResourceScheduler;
using KSP.Localization;
using System.Linq;
using UnityEngine;

namespace KIT.Refinery.Activity
{
    class AmmoniaElectrolyzer : RefineryActivity, IRefineryActivity
    {
        public AmmoniaElectrolyzer()
        {
            ActivityName = "Ammonia Electrolysis";
            Formula = "NH<size=7>3</size> => N<size=7>2</size> + H<size=7>2</size>";
            PowerRequirements = PluginSettings.Config.BaseELCPowerConsumption;
            EnergyPerTon = PluginSettings.Config.ElectrolysisEnergyPerTon / 14.45;
        }

        private double _currentMassRate;
        private double _ammoniaDensity;
        private double _nitrogenDensity;
        private double _hydrogenDensity;

        private double _ammoniaConsumptionMassRate;
        private double _hydrogenProductionMassRate;
        private double _nitrogenProductionMassRate;

        public RefineryType RefineryType => RefineryType.Electrolysis;

        public bool HasActivityRequirements() { return _part.GetConnectedResources(KITResourceSettings.AmmoniaLqd).Any(rs => rs.amount > 0);  }

        public string Status => string.Copy(_status);

        public void Initialize(Part localPart)
        {
            _part = localPart;
            _vessel = localPart.vessel;
            _ammoniaDensity = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.AmmoniaLqd).density;
            _nitrogenDensity = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.NitrogenLqd).density;
            _hydrogenDensity = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.HydrogenLqd).density;
        }

        public void UpdateFrame(IResourceManager resMan, double rateMultiplier, double powerFraction, double productionModifier, bool allowOverflow, bool isStartup = false)
        {
            _current_power = PowerRequirements * rateMultiplier;
            _currentMassRate = (CurrentPower / EnergyPerTon);

            var spareCapacityNitrogen = resMan.SpareCapacity(ResourceName.NitrogenLqd);
            var spareCapacityHydrogen = resMan.SpareCapacity(ResourceName.HydrogenLqd);

            var maxNitrogenMassRate = (_currentMassRate * (1 - GameConstants.AmmoniaHydrogenFractionByMass))  / _nitrogenDensity;
            var maxHydrogenMassRate = (_currentMassRate * GameConstants.AmmoniaHydrogenFractionByMass)  / _hydrogenDensity;

            // prevent overflow
            if (spareCapacityNitrogen <= maxNitrogenMassRate || spareCapacityHydrogen <= maxHydrogenMassRate)
            {
                _ammoniaConsumptionMassRate = 0;
                _hydrogenProductionMassRate = 0;
                _nitrogenProductionMassRate = 0;
            }
            else
            {
                var tmp = resMan.Consume(ResourceName.AmmoniaLqd, _currentMassRate / _ammoniaDensity);
                _ammoniaConsumptionMassRate = _currentMassRate * (tmp / (_currentMassRate / _ammoniaDensity));

                var hydrogenMassRate = _ammoniaConsumptionMassRate * GameConstants.AmmoniaHydrogenFractionByMass;
                var nitrogenMassRate = _ammoniaConsumptionMassRate * (1 - GameConstants.AmmoniaHydrogenFractionByMass);

                _hydrogenProductionMassRate = hydrogenMassRate;
                resMan.Produce(ResourceName.HydrogenLqd, hydrogenMassRate / _hydrogenDensity);
                _nitrogenProductionMassRate = nitrogenMassRate;
                resMan.Produce(ResourceName.NitrogenLqd, nitrogenMassRate / _nitrogenDensity);
            }

            UpdateStatusMessage();
        }

        public override void UpdateGUI()
        {
            base.UpdateGUI();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_Power"), _bold_label, GUILayout.Width(labelWidth));//"Power"
            GUILayout.Label(PluginHelper.GetFormattedPowerString(CurrentPower) + "/" + PluginHelper.GetFormattedPowerString(PowerRequirements), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_ConsumptionRate"), _bold_label, GUILayout.Width(labelWidth));//"Ammonia Consumption Rate"
            GUILayout.Label((_ammoniaConsumptionMassRate * GameConstants.SecondsInHour).ToString("0.000") + " mT/"+Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_perhour"), _value_label, GUILayout.Width(valueWidth));//hour
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_HydrProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Hydrogen Production Rate"
            GUILayout.Label((_hydrogenProductionMassRate * GameConstants.SecondsInHour).ToString("0.000") + " mT/" + Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_perhour"), _value_label, GUILayout.Width(valueWidth));//hour
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_NitrogenProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Nitrogen Production Rate"
            GUILayout.Label((_nitrogenProductionMassRate * GameConstants.SecondsInHour).ToString("0.000") + " mT/" + Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_perhour"), _value_label, GUILayout.Width(valueWidth));//hour
            GUILayout.EndHorizontal();

            part.GetConnectedResourceTotals(KITResourceSettings.NitrogenLqd.GetHashCode(), out var amount, out var maxAmount);
            var spareCapacityNitrogen = maxAmount - amount;

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_SpareCapacityNitrogen"), _bold_label, GUILayout.Width(labelWidth));//"Spare Capacity Nitrogen"
            GUILayout.Label(spareCapacityNitrogen.ToString("0.000"), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void UpdateStatusMessage()
        {
            if (_hydrogenProductionMassRate > 0 && _nitrogenProductionMassRate > 0)
                _status = Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_statumsg1");//"Electrolysing"
            else if (_hydrogenProductionMassRate > 0)
                _status = Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_statumsg2");//"Electrolysing: Insufficient Nitrogen Storage"
            else if (_nitrogenProductionMassRate > 0)
                _status = Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_statumsg3");//"Electrolysing: Insufficient Hydrogen Storage"
            else if (CurrentPower <= 0.01 * PowerRequirements)
                _status = Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_statumsg4");//"Insufficient Power"
            else
                _status = Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_statumsg5");//"Insufficient Storage"
        }

        public void PrintMissingResources()
        {
            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_AmmoniaElectrolyzer_Postmsg") +" " + KITResourceSettings.AmmoniaLqd, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
        }
    }
}
