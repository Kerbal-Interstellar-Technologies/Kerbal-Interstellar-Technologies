using System;
using System.Collections.Generic;
using System.Linq;
using KIT.ResourceScheduler;
using UnityEngine;

namespace KIT.Resources
{
    public class GasLiquidConversion : IKITModule, IKITVariableSupplier
    {
        static readonly ResourceName GasStart = ResourceName.CarbonDioxideGas;
        static readonly ResourceName GasEnd = ResourceName.XenonGas;
        static readonly ResourceName LiquidStart = ResourceName.CarbonDioxideLqd;
        static readonly ResourceName LiquidEnd = ResourceName.XenonLqd;

        public struct Conversion // Software version 7.0
        {
            // Primary is liquid, secondary is gas
            internal double MaxPowerPrimary;
            internal double MaxPowerSecondary;
            internal double PrimaryConversionEnergyCost;
            internal double SecondaryConversionEnergyCost;

            internal double PrimaryConversionRatio;
            internal double SecondaryConversionRatio;

            public Conversion(double maxPowerPrimary, double maxPowerSecondary, double primaryConversionEnergyCost, double secondaryConversionEnergyCost, PartResourceDefinition primaryDefinition, PartResourceDefinition secondaryDefinition)
            {
                MaxPowerPrimary = maxPowerPrimary;
                MaxPowerSecondary = maxPowerSecondary;
                PrimaryConversionEnergyCost = primaryConversionEnergyCost;
                SecondaryConversionEnergyCost = secondaryConversionEnergyCost;

                PrimaryConversionRatio = secondaryDefinition.density / primaryDefinition.density;
                SecondaryConversionRatio = primaryDefinition.density / secondaryDefinition.density;
            }
        }

        // Dictionary to look for Conversion table above, indexed by liquid ResourceName
        private static Dictionary<ResourceName, Conversion> _conversionTable;

        public GasLiquidConversion()
        {
            if (_conversionTable != null) return;
            _conversionTable = new Dictionary<ResourceName, Conversion>(24);

            var rootNode = GameDatabase.Instance.GetConfigNodes("KIT_GAS_LIQUID_CONVERSION");
            if (rootNode == null || rootNode.Length == 0)
            {
                Debug.Log("[GasLiquidConversion] Can not find configuration node KIT_GAS_LIQUID_CONVERSION");
                return;
            }
            
            foreach (var node in rootNode[0].GetNodes("Conversion"))
            {
                string secondaryResourceName;

                double maxPowerSecondary, primaryConversionEnergyCost, secondaryConversionEnergyCost;

                var primaryResourceName = secondaryResourceName = "";
                var maxPowerPrimary = maxPowerSecondary = primaryConversionEnergyCost = secondaryConversionEnergyCost = 0;
                var failed = !node.TryGetValue(nameof(primaryResourceName), ref primaryResourceName);

                if (!node.TryGetValue(nameof(secondaryResourceName), ref secondaryResourceName)) failed = true;
                if (!node.TryGetValue(nameof(maxPowerPrimary), ref maxPowerPrimary)) failed = true;
                if (!node.TryGetValue(nameof(maxPowerSecondary), ref maxPowerSecondary)) failed = true;
                if (!node.TryGetValue(nameof(primaryConversionEnergyCost), ref primaryConversionEnergyCost)) failed = true;
                if (!node.TryGetValue(nameof(secondaryConversionEnergyCost), ref secondaryConversionEnergyCost)) failed = true;

                if (failed)
                {
                    Debug.Log($"[GasLiquidConversion] unable to parse the entry of {primaryResourceName} / {secondaryResourceName} / {maxPowerPrimary} / {maxPowerSecondary}");
                    continue;
                }

                var primaryId = KITResourceSettings.NameToResource(primaryResourceName);
                var secondaryId = KITResourceSettings.NameToResource(secondaryResourceName);

                if (primaryId == ResourceName.Unknown || secondaryId == ResourceName.Unknown)
                {
                    Debug.Log($"[GasLiquidConversion] can't convert either {primaryResourceName} or {secondaryResourceName} to KIT resource");
                    continue;
                }

                var primaryDefinition = PartResourceLibrary.Instance.GetDefinition(primaryResourceName);
                var secondaryDefinition = PartResourceLibrary.Instance.GetDefinition(secondaryResourceName);

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

                _conversionTable[primaryId] = new Conversion(maxPowerPrimary, maxPowerSecondary, primaryConversionEnergyCost, secondaryConversionEnergyCost, primaryDefinition, secondaryDefinition);
            }

        }

        public ModuleConfigurationFlags ModuleConfiguration()
        {
            return ModuleConfigurationFlags.Fifth;
        }
        
        public void KITFixedUpdate(IResourceManager resMan)
        {
            // nothing needs doing here.
        }

        public string KITPartName() => "Vessel Resource Converter";

        public bool ProvideResource(IResourceManager resMan, ResourceName resource, double requestedAmount)
        {
            var gasToLiquidRequest = (resource >= LiquidStart && resource <= LiquidEnd);
            var liquidToGasRequest = (resource >= GasStart && resource <= GasEnd);

            if (gasToLiquidRequest == liquidToGasRequest)
            {
                throw new NotImplementedException($"Neither a gas nor a liquid request {resource} and {KITResourceSettings.ResourceToName(resource)}");
            }

            if (gasToLiquidRequest)
            {
                ConvertGasToLiquid(resMan, resource, requestedAmount);
            }
            else
            {
                ConvertLiquidToGas(resMan, resource, requestedAmount);
            }

            return true;
        }

        private readonly ResourceKeyValue[] _inputResources =
        {
            new ResourceKeyValue(ResourceName.ElectricCharge, 0),
            new ResourceKeyValue(ResourceName.Unknown, 0)
        };

        private readonly ResourceKeyValue[] _outputResources =
        {
            new ResourceKeyValue(ResourceName.Unknown, 0)
        };

        // Since ResourceKeyValue[] is ICollection<ResourceKeyValue>, List.AddRange() uses a memory copy
        // to set the values, thus preventing memory allocation on each tick.
        private readonly List<ResourceKeyValue> _inputList = new List<ResourceKeyValue>(4);
        private readonly List<ResourceKeyValue> _outputList = new List<ResourceKeyValue>(4);
        
        private void ConvertLiquidToGas(IResourceManager resMan, ResourceName resource, double requestedAmount)
        {
            var inputResource = LiquidStart + (resource - GasStart);
            
            var conversionInfo = _conversionTable[inputResource];
            _inputResources[0].Resource = ResourceName.WasteHeat;
            _inputResources[0].Amount = requestedAmount * conversionInfo.PrimaryConversionEnergyCost;
            _inputResources[1].Resource = inputResource;
            _inputResources[1].Amount = requestedAmount * conversionInfo.PrimaryConversionRatio;

            _outputResources[0].Resource = resource;
            _outputResources[0].Amount = requestedAmount;

            _inputList.Clear();
            _inputList.AddRange(_inputResources);

            _outputList.Clear();
            _outputList.AddRange(_outputResources);
            
            resMan.ScaledConsumptionProduction(_inputList, _outputList, 0,
                ConsumptionProductionFlags.FallbackToElectricCharge);
        }

        private void ConvertGasToLiquid(IResourceManager resMan, ResourceName resource, double requestedAmount)
        {
            var inputResource = GasStart + (resource - LiquidStart);

            var conversionInfo = _conversionTable[resource];
            _inputResources[0].Resource = ResourceName.ElectricCharge;
            _inputResources[0].Amount = requestedAmount * conversionInfo.SecondaryConversionEnergyCost;
            _inputResources[1].Resource = inputResource;
            _inputResources[1].Amount = requestedAmount * conversionInfo.SecondaryConversionRatio;

            _outputResources[0].Resource = resource;
            _outputResources[0].Amount = requestedAmount;

            _inputList.Clear();
            _inputList.AddRange(_inputResources);

            _outputList.Clear();
            _outputList.AddRange(_outputResources);

            resMan.ScaledConsumptionProduction(_inputList, _outputList);
        }

        private readonly ResourceName[] _resourcesConverted = {
            ResourceName.NeonGas, ResourceName.CarbonDioxideGas, ResourceName.CarbonMonoxideGas, ResourceName.DeuteriumGas,
            ResourceName.Helium4Gas, ResourceName.Helium3Gas, ResourceName.HydrogenGas, ResourceName.KryptonGas, ResourceName.MethaneGas,
            ResourceName.NeonGas, ResourceName.NitrogenGas, ResourceName.OxygenGas, ResourceName.TritiumGas, ResourceName.XenonGas,
            ResourceName.CarbonDioxideLqd, ResourceName.CarbonMonoxideLqd, ResourceName.DeuteriumLqd, ResourceName.Helium4Lqd,
            ResourceName.Helium3Lqd, ResourceName.HydrogenLqd, ResourceName.KryptonLqd, ResourceName.MethaneLqd, ResourceName.NeonLqd,
            ResourceName.NitrogenLqd, ResourceName.OxygenLqd, ResourceName.TritiumLqd, ResourceName.XenonLqd,
        };

        public ResourceName[] ResourcesProvided() => _resourcesConverted;
    }
}
