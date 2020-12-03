using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KIT.Resources
{
    /// <summary>
    /// If you change this, then you'll also need to change the following areas of code at a minimum:
    ///   - ResourceSettings.ResourceToName
    ///   - ResourceSettings.ValidateResource
    /// </summary>
    public enum ResourceName
    {
        Unknown,
        ElectricCharge,
        LiquidFuel,
        Oxidizer,
        MonoPropellant,

        Actinides,
        DepletedFuel,
        EnrichedUranium,
        Plutonium238,
        ThoriumTetraflouride,
        UraniumTetraflouride,
        Uranium233,
        UraniumNitride,

        WasteHeat,
        EndResource,
    }

    public enum ResourcePriorityValue
    {
        First = 1,
        Second = 2,
        Third = 3,
        Fourth = 4,
        Fifth = 5,
        SupplierOnlyFlag = 0x80,
    }

    public static class KITResourceSettings
    {
        public static string Unknown { get; private set; } = "Unknown";
        public static string EndResource { get; private set; } = "EndResource";

        #region Builtin resources
        public static string ElectricCharge { get; private set; } = "ElectricCharge";
        public static string LiquidFuel { get; private set; } = "LiquidFuel";
        public static string Oxidizer { get; private set; } = "Oxidizer";
        public static string MonoPropellant { get; private set; } = "MonoPropellant";
        #endregion

        #region KIT Basic Resources
        public static string WasteHeat { get; private set; } = "WasteHeat";
        #endregion

        #region Radioactive Resources
        public static string Actinides { get; private set; } = "Actinides";
        public static string DepletedFuel { get; private set; } = "DepletedFuel";
        public static string EnrichedUranium { get; private set; } = "EnrichedUranium";
        public static string Plutonium238 { get; private set; } = "Plutonium-238";
        public static string ThoriumTetraflouride { get; private set; } = "ThF4";
        public static string UraniumTetraflouride { get; private set; } = "UF4";
        public static string Uranium233 { get; private set; } = "Uranium-233";
        public static string UraniumNitride { get; private set; } = "UraniumNitride";
        #endregion

        public static string ResourceToName(ResourceName resource)
        {
            switch (resource)
            {
                case ResourceName.Unknown: return ElectricCharge;
                case ResourceName.ElectricCharge: return ElectricCharge;
                case ResourceName.LiquidFuel: return LiquidFuel;
                case ResourceName.Oxidizer: return Oxidizer;
                case ResourceName.MonoPropellant: return MonoPropellant;
                case ResourceName.Actinides: return Actinides;
                case ResourceName.DepletedFuel: return DepletedFuel;
                case ResourceName.EnrichedUranium: return EnrichedUranium;
                case ResourceName.Plutonium238: return Plutonium238;
                case ResourceName.ThoriumTetraflouride: return ThoriumTetraflouride;
                case ResourceName.UraniumTetraflouride: return UraniumTetraflouride;
                case ResourceName.Uranium233: return Uranium233;
                case ResourceName.UraniumNitride: return UraniumNitride;
                case ResourceName.WasteHeat: return WasteHeat;
                case ResourceName.EndResource: return EndResource;
                default: throw new InvalidEnumArgumentException(nameof(resource), (int)resource, typeof(ResourceName));
            }
        }

        private static Dictionary<string, ResourceName> nameToResourceMap;

        public static ResourceName NameToResource(string name)
        {
            if (nameToResourceMap == null)
            {
                nameToResourceMap = new Dictionary<string, ResourceName>(64);

                for (var i = 0; i < (int)ResourceName.EndResource; i++)
                {
                    nameToResourceMap[ResourceToName((ResourceName)i)] = (ResourceName)i;
                }
            }

            if (nameToResourceMap.ContainsKey(name) == false)
            {
                Debug.Log($"[ResourceSettings.ResourceName] requested to map unknown resource {name} - this will likely blow up");
                return ResourceName.Unknown;
            }

            return nameToResourceMap[name];
        }

        /// <summary>
        /// Blows up if the supplied resource value does not map to the enum structure above.
        /// </summary>
        /// <param name="resource"></param>
        public static void ValidateResource(ResourceName resource)
        {
            if (resource <= ResourceName.Unknown || resource >= ResourceName.EndResource)
                throw new InvalidEnumArgumentException(nameof(resource), (int)resource, typeof(ResourceName));
        }

        /*

        #region Chemical resources

        public static string Alumina { get; private set; } = "Alumina";
        public static string Aluminium { get; private set; } = "Aluminium";
        public static string AmmoniaLqd { get; private set; } = "LqdAmmonia";
        public static string ArgonLqd { get; private set; } = "LqdArgon";
        public static string CarbonDioxideGas { get; private set; } = "CarbonDioxide";
        public static string CarbonDioxideLqd { get; private set; } = "LqdCO2";
        public static string CarbonMonoxideGas { get; private set; } = "CarbonMonoxide";
        public static string CarbonMonoxideLqd { get; private set; } = "LqdCO";
        public static string DeuteriumLqd { get; private set; } = "LqdDeuterium";
        public static string DeuteriumGas { get; private set; } = "Deuterium";
        public static string Helium4Gas { get; private set; } = "Helium";
        public static string Helium4Lqd { get; private set; } = "LqdHelium";

        public static string HydrogenLqd { get; private set; } = LqdHydrogen;
        public static string HydrogenGas { get; private set; } = "Hydrogen";
        public static string FluorineGas { get; private set; } = "Fluorine";
        public static string Lithium6 { get; private set; } = "Lithium6";
        public static string Lithium7 { get; private set; } = "Lithium";

        #endregion

        #region Abstract resources
        public static string IntakeOxygenAir { get; private set; } = "IntakeAir";
        public static string IntakeLiquid { get; private set; } = "IntakeLqd";
        public static string IntakeAtmosphere { get; private set; } = "IntakeAtm";

        #endregion

        #region Pseudo resources
        public static string ElectricChargePower { get; private set; } = ElectricCharge;
        public static string AntiProtium { get; private set; } = "Antimatter";
        public static string VacuumPlasma { get; private set; } = "VacuumPlasma";
        public static string ExoticMatter { get; private set; } = "ExoticMatter";
        #endregion

        */

    }
}
