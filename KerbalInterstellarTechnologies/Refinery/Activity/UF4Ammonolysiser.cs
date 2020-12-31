using KIT.Extensions;
using KIT.Resources;
using KIT.ResourceScheduler;
using KSP.Localization;
using System.Linq;
using UnityEngine;

namespace KIT.Refinery.Activity
{
    class UF4Ammonolysiser : RefineryActivity, IRefineryActivity
    {
        public UF4Ammonolysiser()
        {
            ActivityName = "Uranium Tetrafluoride Ammonolysis";
            PowerRequirements = PluginSettings.Config.BaseUraniumAmmonolysisPowerConsumption;
            EnergyPerTon = 1 / GameConstants.BaseUraniumAmmonolysisRate;
        }

        double _ammoniaDensity;
        double _uraniumTetrafluorideDensity;
        double _uraniumNitrideDensity;

        double _ammoniaConsumptionRate;
        double _uraniumTetrafluorideConsumptionRate;
        double _uraniumNitrideProductionRate;

        public RefineryType RefineryType => RefineryType.Synthesize;

        public bool HasActivityRequirements()
        {
            return _part.GetConnectedResources(KITResourceSettings.UraniumTetraflouride)
                .Any(rs => rs.amount > 0) && _part.GetConnectedResources(KITResourceSettings.AmmoniaLqd).Any(rs => rs.amount > 0);
        }

        public string Status => string.Copy(_status);

        public void Initialize(Part localPart)
        {
            _part = localPart;
            _vessel = localPart.vessel;
            _ammoniaDensity = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.AmmoniaLqd).density;
            _uraniumTetrafluorideDensity = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.UraniumTetraflouride).density;
            _uraniumNitrideDensity = PartResourceLibrary.Instance.GetDefinition(KITResourceSettings.UraniumNitride).density;
        }

        public void UpdateFrame(IResourceManager resMan, double rateMultiplier, double powerFraction, double productionModifier, bool allowOverflow,  bool isStartup = false)
        {
            _current_power = PowerRequirements * rateMultiplier;
            _current_rate = CurrentPower / EnergyPerTon;
            double uf4Fraction = _current_rate * 1.24597 / _uraniumTetrafluorideDensity;
            double ammoniaFraction = _current_rate * 0.901 / _ammoniaDensity;
            _uraniumTetrafluorideConsumptionRate = _part.RequestResource(KITResourceSettings.UraniumTetraflouride, uf4Fraction, ResourceFlowMode.ALL_VESSEL) * _uraniumTetrafluorideDensity;
            _ammoniaConsumptionRate = _part.RequestResource(KITResourceSettings.AmmoniaLqd, ammoniaFraction, ResourceFlowMode.ALL_VESSEL) * _ammoniaDensity;

            if (_ammoniaConsumptionRate > 0 && _uraniumTetrafluorideConsumptionRate > 0)
                _uraniumNitrideProductionRate = -_part.RequestResource(KITResourceSettings.UraniumNitride, -_uraniumTetrafluorideConsumptionRate / 1.24597 / _uraniumNitrideDensity, ResourceFlowMode.ALL_VESSEL) /  _uraniumNitrideDensity;

            UpdateStatusMessage();
        }

        public override void UpdateGUI()
        {
            base.UpdateGUI();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_UF4Ammonolysiser_Power"), _bold_label, GUILayout.Width(labelWidth));//"Power"
            GUILayout.Label(PluginHelper.GetFormattedPowerString(CurrentPower) + "/" + PluginHelper.GetFormattedPowerString(PowerRequirements), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_UF4Ammonolysiser_AmmonaConsumptionRate"), _bold_label, GUILayout.Width(labelWidth));//"Ammona Consumption Rate"
            GUILayout.Label(_ammoniaConsumptionRate * GameConstants.SecondsInHour + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_UF4Ammonolysiser_ConsumptionRate"), _bold_label, GUILayout.Width(labelWidth));//"Uranium Tetraflouride Consumption Rate"
            GUILayout.Label(_uraniumTetrafluorideConsumptionRate * GameConstants.SecondsInHour + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_UF4Ammonolysiser_ProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Uranium Nitride Production Rate"
            GUILayout.Label(_uraniumNitrideProductionRate * GameConstants.SecondsInHour + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void UpdateStatusMessage()
        {
            if (_uraniumNitrideProductionRate > 0)
            {
                _status = Localizer.Format("#LOC_KSPIE_UF4Ammonolysiser_Statumsg1");//"Uranium Tetraflouride Ammonolysis Process Ongoing"
            } else if (CurrentPower <= 0.01*PowerRequirements)
            {
                _status = Localizer.Format("#LOC_KSPIE_UF4Ammonolysiser_Statumsg2");//"Insufficient Power"
            }
            else
            {
                if (_ammoniaConsumptionRate > 0 && _uraniumTetrafluorideConsumptionRate > 0)
                    _status = Localizer.Format("#LOC_KSPIE_UF4Ammonolysiser_Statumsg3");//"Insufficient Storage"
                else if (_ammoniaConsumptionRate > 0)
                    _status = Localizer.Format("#LOC_KSPIE_UF4Ammonolysiser_Statumsg4");//"Uranium Tetraflouride Deprived"
                else if (_uraniumTetrafluorideConsumptionRate > 0)
                    _status = Localizer.Format("#LOC_KSPIE_UF4Ammonolysiser_Statumsg5");//"Ammonia Deprived"
                else
                    _status = Localizer.Format("#LOC_KSPIE_UF4Ammonolysiser_Statumsg6");//"UF4 and Ammonia Deprived"

            }
        }
        public void PrintMissingResources()
        {
            if (!_part.GetConnectedResources(KITResourceSettings.AmmoniaLqd).Any(rs => rs.amount > 0))
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_UF4Ammonolysiser_Postmsg") + " " + KITResourceSettings.AmmoniaLqd, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
            if (!_part.GetConnectedResources(KITResourceSettings.UraniumTetraflouride).Any(rs => rs.amount > 0))
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_UF4Ammonolysiser_Postmsg") + " " + KITResourceSettings.UraniumTetraflouride, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
        }
    }
}
