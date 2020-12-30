using System.Collections.Generic;

namespace KIT.Propulsion
{
    public class ThermalEngineFuel
    {
        private readonly Part _part;

        private readonly List<Propellant> list_of_propellants = new List<Propellant>();

        public string TechRequirement { get; }

        public double CoolingFactor { get; }

        public bool RequiresUpgrade { get; }

        public int Index { get; }

        public string GuiName { get; }

        public double PropellantSootFactorFullThrottle { get; }

        public double PropellantSootFactorMinThrottle { get; }

        public double PropellantSootFactorEquilibrium { get; }

        public double MinDecompositionTemp { get; }

        public double MaxDecompositionTemp { get; }

        public double DecompositionEnergy { get; }

        public double BaseIspMultiplier { get; }

        public double Toxicity { get; }

        public double MinimumCoreTemp { get; }

        public bool IsLFO { get; }

        public bool IsJet { get; }

        public int AtomType { get; } = 1;

        public int PropType { get; } = 1;

        public double IspPropellantMultiplier { get; }

        public double ThrustPropellantMultiplier { get; }

        public ThermalEngineFuel(ConfigNode node, int index, Part part)
        {
            _part = part;
            Index = index;
            GuiName = node.GetValue("guiName");
            IsLFO = node.HasValue("isLFO") && bool.Parse(node.GetValue("isLFO"));
            IsJet = node.HasValue("isJet") && bool.Parse(node.GetValue("isJet"));

            PropellantSootFactorFullThrottle = node.HasValue("maxSootFactor") ? double.Parse(node.GetValue("maxSootFactor")) : 0;
            PropellantSootFactorMinThrottle = node.HasValue("minSootFactor") ? double.Parse(node.GetValue("minSootFactor")) : 0;
            PropellantSootFactorEquilibrium = node.HasValue("levelSootFraction") ? double.Parse(node.GetValue("levelSootFraction")) : 0;
            MinDecompositionTemp = node.HasValue("MinDecompositionTemp") ? double.Parse(node.GetValue("MinDecompositionTemp")) : 0;
            MaxDecompositionTemp = node.HasValue("MaxDecompositionTemp") ? double.Parse(node.GetValue("MaxDecompositionTemp")) : 0;
            DecompositionEnergy = node.HasValue("DecompositionEnergy") ? double.Parse(node.GetValue("DecompositionEnergy")) : 0;
            BaseIspMultiplier = node.HasValue("BaseIspMultiplier") ? double.Parse(node.GetValue("BaseIspMultiplier")) : 0;
            TechRequirement = node.HasValue("TechRequirement") ? node.GetValue("TechRequirement") : string.Empty;
            CoolingFactor = node.HasValue("coolingFactor") ? float.Parse(node.GetValue("coolingFactor")) : 1;
            Toxicity = node.HasValue("Toxicity") ? double.Parse(node.GetValue("Toxicity")) : 0;
            MinimumCoreTemp = node.HasValue("minimumCoreTemp") ? float.Parse(node.GetValue("minimumCoreTemp")) : 0;

            RequiresUpgrade = node.HasValue("RequiresUpgrade") && bool.Parse(node.GetValue("RequiresUpgrade"));
            AtomType = node.HasValue("atomType") ? int.Parse(node.GetValue("atomType")) : 1;
            PropType = node.HasValue("propType") ? int.Parse(node.GetValue("propType")) : 1;
            IspPropellantMultiplier = node.HasValue("ispMultiplier") ? double.Parse(node.GetValue("ispMultiplier")) : 1;
            ThrustPropellantMultiplier = node.HasValue("thrustMultiplier") ? double.Parse(node.GetValue("thrustMultiplier")) : 1;

            ConfigNode[] propellantNodes = node.GetNodes("PROPELLANT");

            foreach (ConfigNode propNode in propellantNodes)
            {
                var currentPropellant = new ExtendedPropellant();
                currentPropellant.Load(propNode);

                list_of_propellants.Add(currentPropellant);
            }
        }

        public bool HasAnyStorage()
        {
            foreach (var extendedPropellant in list_of_propellants)
            {
                _part.GetConnectedResourceTotals(extendedPropellant.id, extendedPropellant.GetFlowMode(), out _, out double maxAmount);

                if (maxAmount <= 0)
                    return false;
            }

            return true;
        }

    }
}
