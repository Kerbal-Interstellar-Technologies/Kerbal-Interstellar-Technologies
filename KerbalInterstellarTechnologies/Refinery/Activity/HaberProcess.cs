using KIT.Resources;
using KIT.ResourceScheduler;
using KSP.Localization;
using System;
using UnityEngine;

namespace KIT.Refinery.Activity
{
    class HaberProcess : RefineryActivity, IRefineryActivity
    {
        public HaberProcess()
        {
            ActivityName = "Haber Process";
            Formula = "H<size=7>2</size> + N<size=7>2</size> => NH<size=7>3</size> (Ammonia)";
            PowerRequirements = PluginSettings.Config.BaseHaberProcessPowerConsumption;
            EnergyPerTon = PluginSettings.Config.HaberProcessEnergyPerTon;
        }

        private double _hydrogenConsumptionRate;
        private double _ammoniaProductionRate;
        private double _nitrogenConsumptionRate;

        private PartResourceDefinition _definitionHydrogen;
        private PartResourceDefinition _definitionNitrogen;
        private PartResourceDefinition _definitionAmmonia;

        private double _availableHydrogen;
        private double _availableNitrogen;
        private double _spareCapacityAmmonia;

        private double _ammoniaDensity;
        private double _hydrogenDensity;
        private double _nitrogenDensity;

        public RefineryType RefineryType => RefineryType.Synthesize;

        // These functions are called from a GUI thread, not the KITFixedUpdate thread
        public bool HasActivityRequirements ()
        {
            return HasAccessToHydrogen() && HasAccessToNitrogen() && HasSpareCapacityAmmonia();
        }

        private bool HasAccessToHydrogen()
        {
            part.GetConnectedResourceTotals(_definitionHydrogen.GetHashCode(), out _, out _availableHydrogen);
            return _availableHydrogen > 0;
        }

        private bool HasAccessToNitrogen()
        {
            part.GetConnectedResourceTotals(_definitionNitrogen.GetHashCode(), out _, out _availableNitrogen);
            return _availableNitrogen > 0;
        }

        private bool HasSpareCapacityAmmonia()
        {
            part.GetConnectedResourceTotals(_definitionAmmonia.GetHashCode(), out var tmp, out var tmp1);
            _spareCapacityAmmonia = tmp1 - tmp;
            return _spareCapacityAmmonia > 0;
        }

        private double _effectiveMaxPowerRequirements;

        public string Status => string.Copy(_status);

        public void Initialize(Part localPart)
        {
            _part = localPart;
            _vessel = localPart.vessel;

            _definitionAmmonia = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.AmmoniaLqd);
            _definitionHydrogen = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.HydrogenLqd);
            _definitionNitrogen = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.NitrogenLqd);

            _ammoniaDensity = _definitionAmmonia.density;
            _hydrogenDensity = _definitionHydrogen.density;
            _nitrogenDensity = _definitionNitrogen.density;
        }

        public void UpdateFrame(IResourceManager resMan, double rateMultiplier, double powerFraction, double productionModifier, bool allowOverflow, bool isStartup = false)
        {
            _effectiveMaxPowerRequirements = PowerRequirements * productionModifier;
            _current_power = powerFraction * _effectiveMaxPowerRequirements;
            _current_rate = CurrentPower / EnergyPerTon;

            var hydrogenRate = _current_rate * GameConstants.AmmoniaHydrogenFractionByMass;
            var nitrogenRate = _current_rate * (1 - GameConstants.AmmoniaHydrogenFractionByMass);

            var requiredHydrogen = hydrogenRate / _hydrogenDensity;
            var requiredNitrogen = nitrogenRate / _nitrogenDensity;
            var maxProductionAmmonia = requiredHydrogen * _hydrogenDensity / GameConstants.AmmoniaHydrogenFractionByMass / _ammoniaDensity;

            var supplyRatioHydrogen = requiredHydrogen > 0 ? Math.Min(1, _availableHydrogen / requiredHydrogen) : 0;
            var supplyRatioNitrogen = requiredNitrogen > 0 ? Math.Min(1, _availableNitrogen / requiredNitrogen) : 0;
            var productionRatioAmmonia = maxProductionAmmonia > 0 ? Math.Min(1, _spareCapacityAmmonia / maxProductionAmmonia) : 0;

            var adjustedRateRatio = Math.Min(productionRatioAmmonia, Math.Min(supplyRatioHydrogen, supplyRatioNitrogen));

            _hydrogenConsumptionRate = _part.RequestResource(_definitionHydrogen.id, adjustedRateRatio * requiredHydrogen, ResourceFlowMode.ALL_VESSEL) * _hydrogenDensity;
            _nitrogenConsumptionRate = _part.RequestResource(_definitionNitrogen.id, adjustedRateRatio * requiredNitrogen, ResourceFlowMode.ALL_VESSEL) * _nitrogenDensity;

            var consumedRatioHydrogen = hydrogenRate > 0 ? _hydrogenConsumptionRate / hydrogenRate : 0;
            var consumedRatioNitrogen = nitrogenRate > 0 ? _nitrogenConsumptionRate / nitrogenRate : 0;

            var consumedRatio = Math.Min(consumedRatioHydrogen, consumedRatioNitrogen);

            if (consumedRatio > 0)
            {
                var ammoniaProduction = -consumedRatio * maxProductionAmmonia;
                var ammoniaProduced = -_part.RequestResource(_definitionAmmonia.id, ammoniaProduction, ResourceFlowMode.ALL_VESSEL);
                _ammoniaProductionRate = ammoniaProduced * _ammoniaDensity;

                if (isStartup)
                {
                    string message = "produced: " + (ammoniaProduced * _ammoniaDensity * 1000).ToString("0.000") + " kg Ammonia";//
                    Debug.Log("[KSPI]: " + message);
                    ScreenMessages.PostScreenMessage(message, 20, ScreenMessageStyle.LOWER_CENTER);
                }
            }

            UpdateStatusMessage();
        }

        public override void UpdateGUI()
        {
            base.UpdateGUI();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_HaberProcess_Power"), _bold_label, GUILayout.Width(labelWidth));//"Power"
            GUILayout.Label(PluginHelper.GetFormattedPowerString(CurrentPower) + "/" + PluginHelper.GetFormattedPowerString(_effectiveMaxPowerRequirements), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_HaberProcess_CurrentRate"), _bold_label, GUILayout.Width(labelWidth));//"Current Rate:"
            GUILayout.Label(_current_rate * GameConstants.SecondsInHour + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_HaberProcess_NitrogenAvailable"), _bold_label, GUILayout.Width(labelWidth));//"Nitrogen Available:"
            GUILayout.Label((_availableNitrogen * _nitrogenDensity * 1000).ToString("0.0000") + " kg", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_HaberProcess_NitrogenConsumptionRate"), _bold_label, GUILayout.Width(labelWidth));//"Nitrogen Consumption Rate:"
            GUILayout.Label(_nitrogenConsumptionRate * GameConstants.SecondsInHour + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_HaberProcess_HydrogenAvailable"), _bold_label, GUILayout.Width(labelWidth));//"Hydrogen Available:"
            GUILayout.Label((_availableHydrogen * _hydrogenDensity * 1000).ToString("0.0000") + " kg", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_HaberProcess_HydrogenConsumptionRate"), _bold_label, GUILayout.Width(labelWidth));//"Hydrogen Consumption Rate:"
            GUILayout.Label(_hydrogenConsumptionRate * GameConstants.SecondsInHour + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_HaberProcess_AmmoniaSpareCapacity"), _bold_label, GUILayout.Width(labelWidth));//"Ammonia Spare Capacity:"
            GUILayout.Label((_spareCapacityAmmonia * _ammoniaDensity * 1000).ToString("0.0000") + " kg", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_HaberProcess_AmmoniaProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Ammonia Production Rate:"
            GUILayout.Label(_ammoniaProductionRate * GameConstants.SecondsInHour + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void UpdateStatusMessage()
        {
            if (_ammoniaProductionRate > 0)
                _status = Localizer.Format("#LOC_KSPIE_HaberProcess_Statumsg1");//"Haber Process Ongoing"
            else if (CurrentPower <= 0.01 * PowerRequirements)
                _status = Localizer.Format("#LOC_KSPIE_HaberProcess_Statumsg2");//"Insufficient Power"
            else
                _status = Localizer.Format("#LOC_KSPIE_HaberProcess_Statumsg4");//"Insufficient Storage"
        }

        public void PrintMissingResources()
        {
            if (!HasAccessToHydrogen())
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_HaberProcess_Postmsg1") + " " + KITResourceSettings.HydrogenLqd, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
            if (!HasAccessToNitrogen())
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_HaberProcess_Postmsg1") + " " + KITResourceSettings.NitrogenLqd, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
            if (!HasSpareCapacityAmmonia())
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_HaberProcess_Postmsg2") + " " + KITResourceSettings.AmmoniaLqd, 3.0f, ScreenMessageStyle.UPPER_CENTER);//No Spare Capacity
        }
    }
}
