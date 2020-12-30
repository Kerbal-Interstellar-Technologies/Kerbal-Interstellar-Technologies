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
        protected bool isInfinite;
        protected int prop_type;
        protected double efficiency;
        protected double ispMultiplier;
        protected double thrustMultiplier;
        protected double decomposedIspMult;
        protected double thrustMultiplierCold;
        protected Propellant propellant;
        protected string propellantname;
        protected string propellantguiname;
        protected string effectname;
        protected double wasteheatMultiplier;
        protected string techRquirement;
        protected PartResourceDefinition resourceDefinition;

        public PartResourceDefinition ResourceDefinition => resourceDefinition;

        public int SupportedEngines => prop_type;

        public double Efficiency => efficiency;

        public double IspMultiplier => ispMultiplier;

        public double DecomposedIspMult => decomposedIspMult;

        public double ThrustMultiplier => thrustMultiplier;

        public double ThrustMultiplierCold => thrustMultiplierCold;

        public Propellant Propellant => propellant;

        public String PropellantName => propellantname;

        public String PropellantGUIName => propellantguiname;

        public String ParticleFXName => effectname;

        public double WasteHeatMultiplier => wasteheatMultiplier;

        public string TechRequirement => techRquirement;

        public bool IsInfinite => isInfinite;

        public ElectricEnginePropellant(ConfigNode node)
        {
            propellantname = node.GetValue("name");

            propellantguiname = node.HasValue("guiName") ? node.GetValue("guiName") : propellantname;
            isInfinite = node.HasValue("isInfinite") && Convert.ToBoolean(node.GetValue("isInfinite"));
            ispMultiplier = node.HasValue("ispMultiplier") ? Convert.ToSingle(node.GetValue("ispMultiplier")) : 1;
            decomposedIspMult = node.HasValue("decomposedIspMult") ? Convert.ToDouble(node.GetValue("decomposedIspMult")) : ispMultiplier;
            thrustMultiplier = node.HasValue("thrustMultiplier") ? Convert.ToDouble(node.GetValue("thrustMultiplier")) : 1;
            thrustMultiplierCold = node.HasValue("thrustMultiplierCold") ? Convert.ToDouble(node.GetValue("thrustMultiplierCold")) : thrustMultiplier;
            wasteheatMultiplier = node.HasValue("wasteheatMultiplier") ? Convert.ToDouble(node.GetValue("wasteheatMultiplier")) : 1;
            efficiency = node.HasValue("efficiency") ? Convert.ToDouble(node.GetValue("efficiency")) : 1;
            prop_type = node.HasValue("type") ? Convert.ToInt32(node.GetValue("type")) : 1;
            effectname = node.HasValue("effectName")  ? node.GetValue("effectName") : "none";
            techRquirement = node.HasValue("techRequirement") ? node.GetValue("techRequirement") : String.Empty;

            ConfigNode propellantNode = node.GetNode("PROPELLANT");
            propellant = new Propellant();
            propellant.Load(propellantNode);
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
                propellant.resourceDefinition = PartResourceLibrary.Instance.GetDefinition(propellant.propellant.name);
            }

            return propellantList;
        }

    }
}
