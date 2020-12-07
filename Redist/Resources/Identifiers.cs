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

        Alumina,
        Aluminium,
        AmmoniaLqd,
        ArgonLqd,
        CarbonDioxideGas,
        CarbonDioxideLqd,
        CarbonMonoxideGas,
        CarbonMonoxideLqd,
        DeuteriumLqd,
        DeuteriumGas,
        Helium4Gas,
        Helium4Lqd,
        Helium3Gas,
        Helium3Lqd,
        HydrogenGas,
        HydrogenLqd,
        HydrogenPeroxide,
        Hydrazine,
        FluorineGas,
        KryptonGas,
        KryptonLqd,
        Lithium6,
        Lithium7,
        ChlorineGas,
        MethaneGas,
        MethaneLqd,
        NeonGas,
        NeonLqd,
        NitrogenGas,
        NitrogenLqd,
        Nitrogen15Lqd,
        OxygenGas,
        OxygenLqd,
        Regolith,
        Sodium,
        SolarWind,
        TritiumGas,
        TritiumLqd,
        WaterHeavy,
        WaterPure,
        WaterRaw,
        XenonGas,
        XenonLqd,

        Actinides,
        DepletedFuel,
        EnrichedUranium,
        Plutonium238,
        ThoriumTetraflouride,
        UraniumTetraflouride,
        Uranium233,
        UraniumNitride,

        AntiProtium,
        VacuumPlasma,
        ExoticMatter,

        IntakeOxygenAir,
        IntakeLiquid,
        IntakeAtmosphere,

        ChargedParticle,
        ThermalPower,

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

        #region Chemical Resources
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
        public static string Helium3Gas { get; private set; } = "Helium3";
        public static string Helium3Lqd { get; private set; } = "LqdHe3";
        public static string HydrogenGas { get; private set; } = "Hydrogen";
        public static string HydrogenLqd { get; private set; } = "LqdHydrogen";
        public static string HydrogenPeroxide { get; private set; } = "HTP";
        public static string Hydrazine { get; private set; } = "Hydrazine";
        public static string FluorineGas { get; private set; } = "Fluorine";
        public static string KryptonGas { get; private set; } = "KryptonGas";
        public static string KryptonLqd { get; private set; } = "LqdKrypton";
        public static string Lithium6 { get; private set; } = "Lithium6";
        public static string Lithium7 { get; private set; } = "Lithium";
        public static string ChlorineGas { get; private set; } = "Chlorine";
        public static string MethaneGas { get; private set; } = "Methane";
        public static string MethaneLqd { get; private set; } = "LqdMethane";
        public static string NeonGas { get; private set; } = "LqdGas";
        public static string NeonLqd { get; private set; } = "LqdNeon";
        public static string NitrogenGas { get; private set; } = "Nitrogen";
        public static string NitrogenLqd { get; private set; } = "LqdNitrogen";
        public static string Nitrogen15Lqd { get; private set; } = "LqdNitrogen15";
        public static string OxygenGas { get; private set; } = "Oxygen";
        public static string OxygenLqd { get; private set; } = "LqdOxygen";
        public static string Regolith { get; private set; } = "Regolith";
        public static string Sodium { get; private set; } = "Sodium";
        public static string SolarWind { get; private set; } = "SolarWind";
        public static string TritiumGas { get; private set; } = "Tritium";
        public static string TritiumLqd { get; private set; } = "LqdTritium";
        public static string WaterHeavy { get; private set; } = "HeavyWater";
        public static string WaterPure { get; private set; } = "Water";
        public static string WaterRaw { get; private set; } = "LqdWater";
        public static string XenonGas { get; private set; } = "XenonGas";
        public static string XenonLqd { get; private set; } = "LqdXenon";
#endregion

        #region Pseudo resources
        public static string AntiProtium { get; private set; } = "Antimatter";
        public static string VacuumPlasma { get; private set; } = "VacuumPlasma";
        public static string ExoticMatter { get; private set; } = "ExoticMatter";
        public static string ChargedParticle { get; private set; } = "ChargedParticles";
        public static string ThermalPower { get; private set; } = "ThermalPower";

        #endregion

        #region Abstract Resources
        public static string IntakeOxygenAir { get; private set; } = "IntakeAir";
        public static string IntakeLiquid { get; private set; } = "IntakeLqd";
        public static string IntakeAtmosphere { get; private set; } = "IntakeAtm";
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

                case ResourceName.Alumina: return "Alumina";
                case ResourceName.Aluminium: return "Aluminium";
                case ResourceName.AmmoniaLqd: return "LqdAmmonia";
                case ResourceName.ArgonLqd: return "LqdArgon";
                case ResourceName.CarbonDioxideGas: return "CarbonDioxide";
                case ResourceName.CarbonDioxideLqd: return "LqdCO2";
                case ResourceName.CarbonMonoxideGas: return "CarbonMonoxide";
                case ResourceName.CarbonMonoxideLqd: return "LqdCO";
                case ResourceName.DeuteriumLqd: return "LqdDeuterium";
                case ResourceName.DeuteriumGas: return "Deuterium";
                case ResourceName.Helium4Gas: return "Helium";
                case ResourceName.Helium4Lqd: return "LqdHelium";
                case ResourceName.Helium3Gas: return "Helium3";
                case ResourceName.Helium3Lqd: return "LqdHe3";
                case ResourceName.HydrogenGas: return "Hydrogen";
                case ResourceName.HydrogenLqd: return "LqdHydrogen";
                case ResourceName.HydrogenPeroxide: return "HTP";
                case ResourceName.Hydrazine: return "Hydrazine";
                case ResourceName.FluorineGas: return "Fluorine";
                case ResourceName.KryptonGas: return "KryptonGas";
                case ResourceName.KryptonLqd: return "LqdKrypton";
                case ResourceName.Lithium6: return "Lithium6";
                case ResourceName.Lithium7: return "Lithium";
                case ResourceName.ChlorineGas: return "Chlorine";
                case ResourceName.MethaneGas: return "Methane";
                case ResourceName.MethaneLqd: return "LqdMethane";
                case ResourceName.NeonGas: return "LqdGas";
                case ResourceName.NeonLqd: return "LqdNeon";
                case ResourceName.NitrogenGas: return "Nitrogen";
                case ResourceName.NitrogenLqd: return "LqdNitrogen";
                case ResourceName.Nitrogen15Lqd: return "LqdNitrogen15";
                case ResourceName.OxygenGas: return "Oxygen";
                case ResourceName.OxygenLqd: return "LqdOxygen";
                case ResourceName.Regolith: return "Regolith";
                case ResourceName.Sodium: return "Sodium";
                case ResourceName.SolarWind: return "SolarWind";
                case ResourceName.TritiumGas: return "Tritium";
                case ResourceName.TritiumLqd: return "LqdTritium";
                case ResourceName.WaterHeavy: return "HeavyWater";
                case ResourceName.WaterPure: return "Water";
                case ResourceName.WaterRaw: return "LqdWater";
                case ResourceName.XenonGas: return "XenonGas";
                case ResourceName.XenonLqd: return "LqdXenon";

                case ResourceName.AntiProtium: return AntiProtium;
                case ResourceName.VacuumPlasma: return VacuumPlasma;
                case ResourceName.ExoticMatter: return ExoticMatter;
                case ResourceName.ChargedParticle: return ChargedParticle;
                case ResourceName.ThermalPower: return ThermalPower;

                case ResourceName.IntakeOxygenAir: return IntakeOxygenAir;
                case ResourceName.IntakeLiquid: return IntakeLiquid;
                case ResourceName.IntakeAtmosphere: return IntakeAtmosphere;

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

     
    }
}
