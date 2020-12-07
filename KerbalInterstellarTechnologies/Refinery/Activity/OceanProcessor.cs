﻿using KIT.Constants;
using KIT.Extensions;
using KIT.Resources;
using KIT.ResourceScheduler;
using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace KIT.Refinery.Activity
{
    [KSPModule("ISRU Ocean Processing")]
    class OceanProcessor : RefineryActivity, IRefineryActivity
    {
        public OceanProcessor()
        {
            ActivityName = "Ocean Extraction";
            PowerRequirements = PluginHelper.BaseELCPowerConsumption;
            EnergyPerTon = PluginHelper.ElectrolysisEnergyPerTon;
        }

        public double fixedConsumptionRate;

        public RefineryType RefineryType => RefineryType.Heating;

        public bool HasActivityRequirements() { return IsThereAnyLiquid();  }

        public string Status => string.Copy(_status);

        // characteristics of the intake liquid, a generic resource we 'collect' and process into resources. This will be the same on all planets, as the 'collection' doesn't rely on abundanceRequests etc. and the resource is not actually collected and stored anywhere anyway
        private double _intakeLqdConsumptionRate;
        private double _availableIntakeLiquidMass;

        private PartResourceDefinition _intakeLiquidDefinition;

        private readonly Dictionary<string, double> _productionRateDict = new Dictionary<string, double>();

        public void Initialize(Part localPart)
        {
            _part = localPart;
            _vessel = localPart.vessel;

            // get the definition of the 'generic' input resource
            _intakeLiquidDefinition = PartResourceLibrary.Instance.GetDefinition(ResourceSettings.Config.IntakeLiquid);
        }

        List<OceanicResource> _localResources = new List<OceanicResource>(); // create a list for keeping track of localResources

        // variables for the ExtractSeawater function
        double _currentResourceProductionRate;
        // end of variables for the ExtractSeawater function

        public void UpdateFrame(IResourceManager resMan, double rateMultiplier, double powerFraction, double productionModifier, bool allowOverflow, bool isStartup = false)
        {
            ExtractSeawater(resMan, rateMultiplier, powerFraction, productionModifier, allowOverflow);

            UpdateStatusMessage();
        }
        // this is a function used for IRefinery HasActivityRequirements check
        public bool IsThereAnyLiquid()
        {
            double tmpAvail, tmpMax;

            bool ret;
            ret = GetTotalLiquidScoopedPerSecond() > 0;

            part.GetConnectedResourceTotals(_intakeLiquidDefinition.GetHashCode(), out tmpAvail, out tmpMax);

            return ret;
        }

        /* This is just a short cycle that goes through the air intakes on the vessel, looks at which ones are submerged and multiplies the percentage of the part's submersion
         * with the amount of air it can intake (I'm taking the simplification that air intakes can also intake liquids and running with it).
         * This value is later stored in the persistent totalAirValue, so that this process can access it when offline collecting.
         * tempLqd is just a variable used to temporarily hold the total amount while cycling through parts, then gets reset at every engine update.
         */
        public double GetTotalLiquidScoopedPerSecond()
        {
            double tempLqd = 0; // reset tempLqd before we go into the list

            if (_vessel != null)
            {
                var intakesList = _vessel.FindPartModulesImplementing<AtmosphericIntake>(); // add any atmo intake part on the vessel to our list

                foreach (AtmosphericIntake intake in intakesList) // go through the list
                {
                    if (intake.IntakeEnabled) // only process open intakes
                    {
                        tempLqd += (intake.area * intake.part.submergedPortion); // add the current intake's liquid intake to our tempLqd. When done with the foreach cycle, we will have the total amount of liquid these intakes collect per cycle
                    }
                }
            }

            if (_part != null)
                tempLqd += _part.submergedPortion * _part.surfaceAreas.magnitude;

            return tempLqd;
        }

        public void ExtractSeawater(IResourceManager resMan, double rateMultiplier, double powerFraction, double productionModifier, bool allowOverflow)
        {
            _effectiveMaxPower = productionModifier * PowerRequirements;

            _current_power = _effectiveMaxPower * powerFraction;
            _current_rate = CurrentPower / EnergyPerTon;

            // determine the amount of liquid processed every frame
            _availableIntakeLiquidMass = GetTotalLiquidScoopedPerSecond() * _intakeLiquidDefinition.density;

            // this should determine how much resource this process can consume
            var fixedMaxLiquidConsumptionRate = _current_rate * _intakeLiquidDefinition.density;

            // supplement any missing LiquidIntake by intake
            var shortage = fixedMaxLiquidConsumptionRate > _availableIntakeLiquidMass ? fixedMaxLiquidConsumptionRate - _availableIntakeLiquidMass : 0;
            if (shortage > 0)
                _availableIntakeLiquidMass += _part.RequestResource(_intakeLiquidDefinition.id, shortage, ResourceFlowMode.ALL_VESSEL);

            var liquidConsumptionRatio = Math.Min(fixedMaxLiquidConsumptionRate, _availableIntakeLiquidMass) / fixedMaxLiquidConsumptionRate;

            fixedConsumptionRate = _current_rate * liquidConsumptionRatio;

            if (fixedConsumptionRate <= 0) return;

            _currentResourceProductionRate = 0;

            _productionRateDict.Clear();

            // get the resource for the current body
            _localResources = OceanicResourceHandler.GetOceanicCompositionForBody(FlightGlobals.currentMainBody);

            foreach (OceanicResource resource in _localResources)
            {
                if (resource.ResourceName == null)
                    continue; // this resource does not interest us anymore

                if (resource.Definition == null)
                    continue; // this resource is missing a resource definition

                // determine the spare room - gets parts that contain the current resource, gets the sum of their maxAmount - (current)amount and multiplies by density of resource
                var currentResourceSpareRoom = _part.GetConnectedResources(resource.ResourceName).Sum(r => r.maxAmount - r.amount) * resource.Definition.density;

                // how much we should add per cycle
                var currentResourceMaxRate = fixedConsumptionRate * resource.ResourceAbundance;

                // how much we actually CAN add per cycle (into the spare room in the vessel's tanks) - if the allowOverflow setting is on, dump it all in even though it won't fit (excess is lost), otherwise use the smaller of two values (spare room remaining and the full rate)
                var currentResourcePossibleRate = allowOverflow ? currentResourceMaxRate : Math.Min(currentResourceSpareRoom, currentResourceMaxRate);

                // calculate the ratio of rates, if the denominator is zero, assign zero outright to prevent problems
                var currentResourceRatio = currentResourceMaxRate <= 0 ? 0 : currentResourcePossibleRate / currentResourceMaxRate;

                // calculate the consumption rate of the intake liquid
                _intakeLqdConsumptionRate = (currentResourceRatio * fixedConsumptionRate / _intakeLiquidDefinition.density) /  _intakeLiquidDefinition.density;

                // calculate the rate of production
                var currentResourceTempProductionRate = _intakeLqdConsumptionRate * resource.ResourceAbundance;

                // add the produced resource
                 var currentProductionRate = -_part.RequestResource(resource.ResourceName, -currentResourceTempProductionRate  / resource.Definition.density, ResourceFlowMode.ALL_VESSEL) / resource.Definition.density;

                _productionRateDict.Add(resource.ResourceName, currentProductionRate);

                _currentResourceProductionRate += currentProductionRate;
            }
        }

        public override void UpdateGUI()
        {
            base.UpdateGUI();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SeawaterExtract_Power"), _bold_label, GUILayout.Width(labelWidth));//"Power"
            GUILayout.Label(PluginHelper.getFormattedPowerString(CurrentPower) + "/" + PluginHelper.getFormattedPowerString(_effectiveMaxPower), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SeawaterExtract_LqdConsumption"), _bold_label, GUILayout.Width(labelWidth));//"Intake Lqd Consumption"
            GUILayout.Label((GetValueText(_intakeLqdConsumptionRate * GameConstants.SECONDS_IN_HOUR)) + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_SeawaterExtract_ProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Production Rate"
            GUILayout.Label((GetValueText(_currentResourceProductionRate * GameConstants.SECONDS_IN_HOUR)) + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Name"), _bold_label, GUILayout.Width(valueWidth));                // "Name"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Abundance"), _bold_label, GUILayout.Width(valueWidth));           // "Abundance"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_MaxCapacity"), _bold_label, GUILayout.Width(valueWidth));         // "Max Capacity"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Stored"), _bold_label, GUILayout.Width(valueWidth));              // "Stored"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_SpareRoom"), _bold_label, GUILayout.Width(valueWidth));           // "Spare Room"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Productionpersecond"), _bold_label, GUILayout.Width(valueWidth)); // "Production per second"
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_AtmosphericExtractor_Productionperhour"), _bold_label, GUILayout.Width(valueWidth));   // "Production per hour"
            GUILayout.EndHorizontal();

            foreach (var resource in _localResources)
            {
                if (string.IsNullOrEmpty(resource.ResourceName))
                {
                    DisplayResourceExtraction(resourceName: resource.DisplayName, percentage: resource.ResourceAbundance, productionRate: 0, spareRoom: 0, maximumCapacity: 0);
                    continue;
                }

                if (resource.Definition == null)
                {
                    DisplayResourceExtraction(resourceName: resource.DisplayName, percentage: resource.ResourceAbundance, productionRate: 0, spareRoom: 0, maximumCapacity: 0);
                    continue;
                }

                var connectedResources = _part.GetConnectedResources(resource.ResourceName).ToList();
                var spareRoom = connectedResources.Sum(r => r.maxAmount - r.amount) * resource.Definition.density;
                var maximumCapacity = connectedResources.Sum(r => r.maxAmount) * resource.Definition.density;
                var productionRate = _productionRateDict[resource.ResourceName];

                DisplayResourceExtraction(resourceName: resource.DisplayName, percentage: resource.ResourceAbundance, productionRate: productionRate, spareRoom: spareRoom, maximumCapacity: maximumCapacity);
            }
        }

        private void DisplayResourceExtraction(string resourceName, double percentage, double productionRate, double spareRoom, double maximumCapacity)
        {
            if (percentage <= 0)
                return;

            GUILayout.BeginHorizontal();
            GUILayout.Label(resourceName, _value_label, GUILayout.Width(valueWidth));
            GUILayout.Label(GetValueText(percentage * 100) + "%", _value_label, GUILayout.Width(valueWidth));

            if (maximumCapacity > 0)
            {
                GUILayout.Label(GetValueText(maximumCapacity) + " t", _value_label, GUILayout.Width(valueWidth));
                GUILayout.Label(GetValueText(maximumCapacity - spareRoom) + " t", _value_label, GUILayout.Width(valueWidth));

                if (spareRoom > 0)
                    GUILayout.Label(GetValueText(spareRoom) + " t", _value_label_green, GUILayout.Width(valueWidth));
                else
                    GUILayout.Label("0", _value_label_red, GUILayout.Width(valueWidth));
            }
            else
            {
                GUILayout.Label("0", _value_label_red, GUILayout.Width(valueWidth));
                GUILayout.Label("", _value_label_red, GUILayout.Width(valueWidth));
                GUILayout.Label("", _value_label_red, GUILayout.Width(valueWidth));
            }

            if (productionRate > 0)
            {
                GUILayout.Label(GetValueText(productionRate) + " U/s", _value_label, GUILayout.Width(valueWidth));
                GUILayout.Label(GetValueText(productionRate * GameConstants.SECONDS_IN_HOUR) + " U/h", _value_label, GUILayout.Width(valueWidth));
            }
            else
            {
                GUILayout.Label("", _value_label_red, GUILayout.Width(valueWidth));
                GUILayout.Label("", _value_label_red, GUILayout.Width(valueWidth));
            }

            GUILayout.EndHorizontal();
        }

        private string GetValueText(double value)
        {
            return value >= 0.00000005 ? value.ToString("##.########") : ((float) value).ToString(CultureInfo.InvariantCulture);
        }

        private void UpdateStatusMessage()
        {
            if (_intakeLqdConsumptionRate > 0)
                _status = Localizer.Format("#LOC_KSPIE_SeawaterExtract_Statumsg1");//"Extracting intake liquid"
            else if (CurrentPower <= 0.01 * PowerRequirements)
                _status = Localizer.Format("#LOC_KSPIE_SeawaterExtract_Statumsg2");//"Insufficient Power"
            else
                _status = Localizer.Format("#LOC_KSPIE_SeawaterExtract_Statumsg3");//"Insufficient Storage, try allowing overflow"
        }

        public void PrintMissingResources()
        {
            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_SeawaterExtract_Postmsg2") + " " + ResourceSettings.Config.IntakeLiquid, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
        }
    }
}
