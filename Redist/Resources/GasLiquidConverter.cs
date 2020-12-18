using KIT.Resources;
using KIT.ResourceScheduler;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class GasLiquidConversion : IKITMod, IKITVariableSupplier
{
    static readonly ResourceName GasStart = ResourceName.CarbonDioxideGas;
    static readonly ResourceName GasEnd = ResourceName.XenonGas;
    static readonly ResourceName LiquidStart = ResourceName.CarbonDioxideLqd;
    static readonly ResourceName LiquidEnd = ResourceName.XenonLqd;

    struct Conversion // Software version 7.0
    {
        // Primary is liquid, secondary is gas
        double maxPowerPrimary;
		double maxPowerSecondary;
        double primaryConversionEnergyCost;
		double secondaryConversionEnergyCost;

        double primaryConversionRatio;
        double secondaryConversionRatio;

        public Conversion(double maxPowerPrimary, double maxPowerSecondary, double primaryConversionEnergyCost, double secondaryConversionEnergyCost, PartResourceDefinition primaryDefinition, PartResourceDefinition secondaryDefinition)
        {
            this.maxPowerPrimary = maxPowerPrimary;
            this.maxPowerSecondary = maxPowerSecondary;
            this.primaryConversionEnergyCost = primaryConversionEnergyCost;
            this.secondaryConversionEnergyCost = secondaryConversionEnergyCost;

            primaryConversionRatio = secondaryDefinition.density / primaryDefinition.density;
            secondaryConversionRatio = primaryDefinition.density / secondaryDefinition.density;
        }
    }

    // Dictionary to look for Conversion table above, indexed by liquid ResourceName
    private static Dictionary<ResourceName, Conversion> conversionTable;

    public GasLiquidConversion()
    {
        if (conversionTable != null) return;
        conversionTable = new Dictionary<ResourceName, Conversion>(24);

        var rootNode = GameDatabase.Instance.GetConfigNodes("KIT_GAS_LIQUID_CONVERSION");
        if(rootNode == null || rootNode.Count() == 0)
        {
            Debug.Log($"[GasLiquidConversion] Can not find configuration node KIT_GAS_LIQUID_CONVERSION");
            return;
        }
        var nodeList = rootNode[0].GetNodes("Conversion");

        string primaryResourceName, secondaryResourceName;

        double maxPowerPrimary, maxPowerSecondary, primaryConversionEnergyCost, secondaryConversionEnergyCost;
        bool failed;
        ResourceName primaryID, secondaryID;

        PartResourceDefinition primaryDefinition;
        PartResourceDefinition secondaryDefinition;

        foreach (var node in nodeList)
        {
            primaryResourceName = secondaryResourceName = "";
            maxPowerPrimary = maxPowerSecondary = primaryConversionEnergyCost = secondaryConversionEnergyCost = 0;
            failed = false;

            if (node.TryGetValue(nameof(primaryResourceName), ref primaryResourceName) == false) failed = true;
            if (node.TryGetValue(nameof(secondaryResourceName), ref secondaryResourceName) == false) failed = true;
            if (node.TryGetValue(nameof(maxPowerPrimary), ref maxPowerPrimary) == false) failed = true;
            if (node.TryGetValue(nameof(maxPowerSecondary), ref maxPowerSecondary) == false) failed = true;
            if (node.TryGetValue(nameof(primaryConversionEnergyCost), ref primaryConversionEnergyCost) == false) failed = true;
            if (node.TryGetValue(nameof(secondaryConversionEnergyCost), ref secondaryConversionEnergyCost) == false) failed = true;

            if (failed)
            {
                Debug.Log($"[GasLiquidConversion] unable to parse the entry of {primaryResourceName} / {secondaryResourceName} / {maxPowerPrimary} / {maxPowerSecondary}");
                continue;
            }

            primaryID = KITResourceSettings.NameToResource(primaryResourceName);
            secondaryID = KITResourceSettings.NameToResource(secondaryResourceName);

            if (primaryID == ResourceName.Unknown || secondaryID == ResourceName.Unknown)
            {
                Debug.Log($"[GasLiquidConversion] can't convert either {primaryResourceName} or {secondaryResourceName} to KIT resource");
                continue;
            }

            primaryDefinition = PartResourceLibrary.Instance.GetDefinition(primaryResourceName);
            secondaryDefinition = PartResourceLibrary.Instance.GetDefinition(secondaryResourceName);

            if (primaryDefinition == null || secondaryDefinition == null)
            {
                Debug.Log($"[GasLiquidConversion] unable to find resource definition {primaryResourceName} and/or {secondaryResourceName}");
                return;
            }

            if (primaryDefinition.density == 0 || secondaryDefinition.density == 0)
            {
                Debug.Log("[GasLiquidConversion] why is the definition density 0 on {primaryResourceName} and/or {secondaryResourceName}");
                return;
            }

            conversionTable[primaryID] = new Conversion(maxPowerPrimary, maxPowerSecondary, primaryConversionEnergyCost, secondaryConversionEnergyCost, primaryDefinition, secondaryDefinition);

        }
            
    }

    public void KITFixedUpdate(IResourceManager resMan)
    {
        // nothing needs doing here.
    }

    public string KITPartName() => "Vessel Resource Converter";

    public bool ProvideResource(IResourceManager resMan, ResourceName resource, double requestedAmount)
    {
        bool gasToLiquidRequest = (resource >= LiquidStart && resource <= LiquidEnd);
        bool liquidToGasRequest = (resource >= GasStart && resource <= GasEnd);

        if (gasToLiquidRequest == liquidToGasRequest)
        {
            throw new NotImplementedException($"Neither a gas nor a liquid request {resource} and {KITResourceSettings.ResourceToName(resource)}");
        }

        throw new NotImplementedException();
    }

    public ResourcePriorityValue ResourceProcessPriority() => ResourcePriorityValue.Fifth;

    private ResourceName[] resourcesConverted = new ResourceName[] {
            ResourceName.NeonGas, ResourceName.CarbonDioxideGas, ResourceName.CarbonMonoxideGas, ResourceName.DeuteriumGas,
            ResourceName.Helium4Gas, ResourceName.Helium3Gas, ResourceName.HydrogenGas, ResourceName.KryptonGas, ResourceName.MethaneGas,
            ResourceName.NeonGas, ResourceName.NitrogenGas, ResourceName.OxygenGas, ResourceName.TritiumGas, ResourceName.XenonGas,
            ResourceName.CarbonDioxideLqd, ResourceName.CarbonMonoxideLqd, ResourceName.DeuteriumLqd, ResourceName.Helium4Lqd,
            ResourceName.Helium3Lqd, ResourceName.HydrogenLqd, ResourceName.KryptonLqd, ResourceName.MethaneLqd, ResourceName.NeonLqd,
            ResourceName.NitrogenLqd, ResourceName.OxygenLqd, ResourceName.TritiumLqd, ResourceName.XenonLqd,
        };

    public ResourceName[] ResourcesProvided() => resourcesConverted;
}
