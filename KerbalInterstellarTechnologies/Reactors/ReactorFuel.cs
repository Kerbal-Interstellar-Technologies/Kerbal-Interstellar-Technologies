using KIT.Extensions;
using System;
using UnityEngine;

namespace KIT.Reactors
{
    class ReactorFuel
    {
        readonly double _tonsFuelUsagePerMW;

        public ReactorFuel(ConfigNode node)
        {
            FuelName = node.GetValue("name");
            Ratio = node.HasValue("ratio") ? Convert.ToDouble(node.GetValue("ratio")) : 1;
            Simulate = node.HasValue("simulate") && Boolean.Parse(node.GetValue("simulate"));
            ResourceName = node.HasValue("resource") ? node.GetValue("resource") : FuelName;
            _tonsFuelUsagePerMW = Convert.ToDouble(node.GetValue("UsagePerMW"));
            Unit = node.GetValue("Unit");
            ConsumeGlobal = !node.HasValue("consumeGlobal") || Boolean.Parse(node.GetValue("consumeGlobal"));

            Definition = PartResourceLibrary.Instance.GetDefinition(ResourceName);
            if (Definition == null)
                Debug.LogError("[KSPI]: No definition found for resource '" + ResourceName + "' for ReactorFuel " + FuelName);
            else
            {
                DensityInTon = (double)(decimal)Definition.density;
                DensityInKg = DensityInTon * 1000;
                AmountFuelUsePerMJ = _tonsFuelUsagePerMW / DensityInTon;
            }
        }

        public PartResourceDefinition Definition { get; }

        public double Ratio { get; }

        public bool ConsumeGlobal { get; }

        public double DensityInTon { get; }

        public double DensityInKg { get; }

        public bool Simulate { get; }

        public double AmountFuelUsePerMJ { get; }

        public double TonsFuelUsePerMJ => _tonsFuelUsagePerMW;

        public double EnergyDensity => _tonsFuelUsagePerMW > 0 ?  0.001 / _tonsFuelUsagePerMW : 0;

        public string FuelName { get; }

        public string ResourceName { get; }

        public string Unit { get; }

        public double GetFuelRatio(Part part, double fuelEfficiency, double megajoules, double fuelUsePerMJMult, bool simulate)
        {
            if (CheatOptions.InfinitePropellant)
                return 1;

            if (simulate)
                return 1;

            var fuelUseForPower = GetFuelUseForPower(fuelEfficiency, megajoules, fuelUsePerMJMult);

            return fuelUseForPower > 0 ?  Math.Min(GetFuelAvailability(part) / fuelUseForPower, 1) : 0;
        }

        public double GetFuelUseForPower(double efficiency, double megajoules, double fuelUsePerMJMult)
        {
            return efficiency > 0 ?  AmountFuelUsePerMJ * fuelUsePerMJMult * megajoules / efficiency : 0;
        }

        public double GetFuelAvailability(Part part)
        {
            if (!ConsumeGlobal)
            {
                if (part.Resources.Contains(ResourceName))
                    return part.Resources[ResourceName].amount;
                else
                    return 0;
            }

            if (HighLogic.LoadedSceneIsFlight)
            {
                foreach (var t in part.Resources)
                    if (t.resourceName == Definition.name)
                        return t.amount;

                return 0;
            }
            else
                return part.FindAmountOfAvailableFuel(ResourceName, 4);
        }

    }

    class ReactorProduct
    {
        readonly double _tonsProductUsagePerMw;
        readonly string _fuelName;
        readonly string _resourceName;
        readonly double _density;

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

        public PartResourceDefinition Definition { get; }

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
