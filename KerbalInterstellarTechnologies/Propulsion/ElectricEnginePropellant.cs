using System;
using System.Collections.Generic;
using System.Linq;

namespace KIT.Propulsion
{
    public enum ElectricEngineType
    {
        PLASMA = 1,
        ARCJET = 2,
        VASIMR = 4,
        VACUUMTHRUSTER = 8,
        RCS = 16,
        ION = 32
    }

    public class ElectricEnginePropellant
    {
        public bool IsInfinite { get; protected set; }
        public int PropType { get; protected set; }
        public double Efficiency { get; protected set; }
        public double IspMultiplier { get; protected set; }
        public double ThrustMultiplier { get; protected set; }
        public double DecomposedIspMult { get; protected set; }
        public double ThrustMultiplierCold { get; protected set; }
        public Propellant Propellant { get; protected set; }
        public string PropellantName { get; protected set; }
        public string PropellantGUIName { get; protected set; }
        public string ParticleFXName { get; protected set; }
        public double WasteHeatMultiplier { get; protected set; }
        public string TechRequirement { get; protected set; }
        public PartResourceDefinition ResourceDefinition { get; protected set; }

        public int SupportedEngines => PropType;

        public ElectricEnginePropellant(ConfigNode node)
        {
            PropellantName = node.GetValue("name");

            PropellantGUIName = node.HasValue("guiName") ? node.GetValue("guiName") : PropellantName;
            IsInfinite = node.HasValue("isInfinite") && Convert.ToBoolean(node.GetValue("isInfinite"));
            IspMultiplier = node.HasValue("ispMultiplier") ? Convert.ToSingle(node.GetValue("ispMultiplier")) : 1;
            DecomposedIspMult = node.HasValue("decomposedIspMult") ? Convert.ToDouble(node.GetValue("decomposedIspMult")) : IspMultiplier;
            ThrustMultiplier = node.HasValue("thrustMultiplier") ? Convert.ToDouble(node.GetValue("thrustMultiplier")) : 1;
            ThrustMultiplierCold = node.HasValue("thrustMultiplierCold") ? Convert.ToDouble(node.GetValue("thrustMultiplierCold")) : ThrustMultiplier;
            WasteHeatMultiplier = node.HasValue("wasteheatMultiplier") ? Convert.ToDouble(node.GetValue("wasteheatMultiplier")) : 1;
            Efficiency = node.HasValue("efficiency") ? Convert.ToDouble(node.GetValue("efficiency")) : 1;
            PropType = node.HasValue("type") ? Convert.ToInt32(node.GetValue("type")) : 1;
            ParticleFXName = node.HasValue("effectName")  ? node.GetValue("effectName") : "none";
            TechRequirement = node.HasValue("techRequirement") ? node.GetValue("techRequirement") : String.Empty;

            ConfigNode propellantNode = node.GetNode("PROPELLANT");
            Propellant = new Propellant();
            Propellant.Load(propellantNode);
        }


        public static List<ElectricEnginePropellant> GetPropellantsEngineForType(int type)
        {
            ConfigNode[] propellantListNode = GameDatabase.Instance.GetConfigNodes("ELECTRIC_PROPELLANT");
            List<ElectricEnginePropellant> propellantList;
            if (propellantListNode.Length == 0)
            {
                PluginHelper.ShowInstallationErrorMessage();
                propellantList = new List<ElectricEnginePropellant>();
            }
            else
            {
                propellantList = propellantListNode.Select(prop => new ElectricEnginePropellant(prop))
                    .Where(eep => (eep.SupportedEngines & type) == type && PluginHelper.HasTechRequirementOrEmpty(eep.TechRequirement)).ToList();
            }

            // initialize resource Definitions
            foreach (var propellant in propellantList)
            {
                propellant.ResourceDefinition = PartResourceLibrary.Instance.GetDefinition(propellant.Propellant.name);
            }

            return propellantList;
        }

    }
}
