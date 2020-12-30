using KIT.Extensions;
using System;
using UnityEngine;

namespace KIT.Reactors
{
    class ReactorFuel
    {
        double _tons_fuel_usage_per_mw;
        double _amountFuelUsePerMJ;
        string _fuel_name;
        string _resource_name;
        double _density;
        double _densityInKg;
        double _ratio;
        string _unit;
        bool _consumeGlobal;
        bool _simulate;

        public ReactorFuel(ConfigNode node)
        {
            _fuel_name = node.GetValue("name");
            _ratio = node.HasValue("ratio") ? Convert.ToDouble(node.GetValue("ratio")) : 1;
            _simulate = node.HasValue("simulate") ? Boolean.Parse(node.GetValue("simulate")) : false;
            _resource_name = node.HasValue("resource") ? node.GetValue("resource") : _fuel_name;
            _tons_fuel_usage_per_mw = Convert.ToDouble(node.GetValue("UsagePerMW"));
            _unit = node.GetValue("Unit");
            _consumeGlobal = node.HasValue("consumeGlobal") ? Boolean.Parse(node.GetValue("consumeGlobal")) : true;

            Definition = PartResourceLibrary.Instance.GetDefinition(_resource_name);
            if (Definition == null)
                Debug.LogError("[KSPI]: No definition found for resource '" + _resource_name + "' for ReactorFuel " + _fuel_name);
            else
            {
                _density = (double)(decimal)Definition.density;
                _densityInKg = _density * 1000;
                _amountFuelUsePerMJ = _tons_fuel_usage_per_mw / _density;
            }
        }

        public PartResourceDefinition Definition { get; private set; }

        public double Ratio => _ratio;

        public bool ConsumeGlobal => _consumeGlobal;

        public double DensityInTon => _density;

        public double DensityInKg => _densityInKg;

        public bool Simulate => _simulate;

        public double AmountFuelUsePerMJ => _amountFuelUsePerMJ;

        public double TonsFuelUsePerMJ => _tons_fuel_usage_per_mw;

        public double EnergyDensity => _tons_fuel_usage_per_mw > 0 ?  0.001 / _tons_fuel_usage_per_mw : 0;

        public string FuelName => _fuel_name;

        public string ResourceName => _resource_name;

        public string Unit => _unit;

        public double GetFuelRatio(Part part, double fuelEfficiency, double megajoules, double fuelUsePerMJMult, bool simulate)
        {
            if (CheatOptions.InfinitePropellant)
                return 1;

            if (simulate)
                return 1;

            var fuelUseForPower = this.GetFuelUseForPower(fuelEfficiency, megajoules, fuelUsePerMJMult);

            return fuelUseForPower > 0 ?  Math.Min(this.GetFuelAvailability(part) / fuelUseForPower, 1) : 0;
        }

        public double GetFuelUseForPower(double efficiency, double megajoules, double fuelUsePerMJMult)
        {
            return efficiency > 0 ?  AmountFuelUsePerMJ * fuelUsePerMJMult * megajoules / efficiency : 0;
        }

        public double GetFuelAvailability(Part part)
        {
            if (!this.ConsumeGlobal)
            {
                if (part.Resources.Contains(this.ResourceName))
                    return part.Resources[this.ResourceName].amount;
                else
                    return 0;
            }

            if (HighLogic.LoadedSceneIsFlight)
            {
                foreach (var t in part.Resources)
                    if (t.resourceName == this.Definition.name)
                        return t.amount;

                return 0;
            }  
            else
                return part.FindAmountOfAvailableFuel(this.ResourceName, 4);
        }

    }

    class ReactorProduct
    {
        double _tonsProductUsagePerMw;
        string _fuelName;
        string _resourceName;
        double _density;

        public ReactorProduct(ConfigNode node)
        {
            _fuelName = node.GetValue("name");
            _resourceName = node.HasValue("resource") ? node.GetValue("resource") : _fuelName;
            Unit = node.GetValue("Unit");
            Simulate = node.HasValue("simulate") && Boolean.Parse(node.GetValue("simulate"));
            IsPropellant = !node.HasValue("isPropellant") || Boolean.Parse(node.GetValue("isPropellant"));
            ProduceGlobal = !node.HasValue("produceGlobal") || Boolean.Parse(node.GetValue("produceGlobal"));
            _tonsProductUsagePerMw = Convert.ToDouble(node.GetValue("ProductionPerMW"));

            Definition = PartResourceLibrary.Instance.GetDefinition(_fuelName);
            if (Definition == null)
                Debug.LogError("[KSPI]: No definition found for ReactorProduct '" + _resourceName + "'");
            else
            {
                _density = (double)(decimal)Definition.density;
                DensityInKg = _density * 1000;
                AmountProductUsePerMJ = _density > 0 ? _tonsProductUsagePerMw / _density : 0;
            }
        }

        public PartResourceDefinition Definition { get; private set; }

        public bool ProduceGlobal { get; }

        public bool IsPropellant { get; }

        public bool Simulate { get; }

        public double DensityInTon => _density;

        public double DensityInKg { get; }

        public double AmountProductUsePerMJ { get; }

        public double TonsProductUsePerMJ => _tonsProductUsagePerMw;

        public double EnergyDensity => 0.001 / _tonsProductUsagePerMw;

        public string FuelName => _fuelName;

        public string ResourceName => _resourceName;

        public string Unit { get; }

        public double GetProductionForPower(double efficiency, double megajoules, double productionPerMJMult)
        {
            return AmountProductUsePerMJ * productionPerMJMult * megajoules / efficiency;
        }
    }
}
