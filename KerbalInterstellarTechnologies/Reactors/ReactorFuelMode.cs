using System;
using System.Collections.Generic;
using System.Linq;

namespace KIT.Reactors
{
    class ReactorProduction
    {
        public ReactorProduct FuelMode;
        public double Mass;
    }

    class FuelResourceMetaData
    {
        public FuelResourceMetaData(PartResourceDefinition resourceDefinition, double ratio)
        {
            this.ResourceDefinition = resourceDefinition;
            this.Ratio = ratio;
        }

        public PartResourceDefinition ResourceDefinition;
        public double Ratio;
    }

    class ResourceGroupMetaData
    {
        public string Name;
        public List<FuelResourceMetaData> ResourceVariantsMetaData;
    }


    class ReactorFuelType
    {
        public ReactorFuelType(IEnumerable<ReactorFuelMode> reactorFuelModes)
        {
            Variants = reactorFuelModes.ToList();

            ResourceGroups = new List<ResourceGroupMetaData>();
            foreach (var group in Variants.SelectMany(m => m.ReactorFuels).GroupBy(m => m.FuelName))
            {
                ResourceGroups.Add(new ResourceGroupMetaData()
                {
                    Name = group.Key,
                    ResourceVariantsMetaData = group.Select(m => new FuelResourceMetaData(m.Definition, m.Ratio)).Distinct().ToList()
                });
            }

            var first = Variants.First();

            AlternativeFuelType1 = first.AlternativeFuelType1;
            AlternativeFuelType2 = first.AlternativeFuelType2;
            AlternativeFuelType3 = first.AlternativeFuelType3;
            AlternativeFuelType4 = first.AlternativeFuelType4;
            AlternativeFuelType5 = first.AlternativeFuelType5;

            Index = first.Index;
            ModeGUIName = first.ModeGUIName;
            TechLevel = first.TechLevel;
            MinimumFusionGainFactor = first.MinimumFusionGainFactor;
            TechRequirement = first.TechRequirement;
            SupportedReactorTypes = first.SupportedReactorTypes;
            Aneutronic = first.Aneutronic;
            GammaRayEnergy = first.GammaRayEnergy;
            RequiresLab = first.RequiresLab;
            RequiresUpgrade = first.RequiresUpgrade;
            ChargedPowerRatio = first.ChargedPowerRatio;
            MeVPerChargedProduct = first.MeVPerChargedProduct;
            NormalisedReactionRate = first.NormalisedReactionRate;
            NormalisedPowerRequirements = first.NormalisedPowerRequirements;
            NeutronsRatio = first.NeutronsRatio;
            TritiumBreedModifier = first.TritiumBreedModifier;
            FuelEfficiencyMultiplier = first.FuelEfficiencyMultiplier;
        }

        public int SupportedReactorTypes { get; }
        public int Index { get; }
        public string ModeGUIName { get; }
        public string TechRequirement { get; }
        public bool Aneutronic { get; }
        public double GammaRayEnergy { get; }
        public bool RequiresLab { get; }
        public bool RequiresUpgrade { get; }
        public float ChargedPowerRatio { get; }
        public double MeVPerChargedProduct { get; }
        public float NormalisedReactionRate { get; }
        public float NormalisedPowerRequirements { get; }
        public int TechLevel { get; }
        public int MinimumFusionGainFactor { get; }
        public float NeutronsRatio { get; }
        public float TritiumBreedModifier { get; }
        public double FuelEfficiencyMultiplier { get; }

        public string AlternativeFuelType1 { get; set; }
        public string AlternativeFuelType2 { get; }
        public string AlternativeFuelType3 { get; }
        public string AlternativeFuelType4 { get; }
        public string AlternativeFuelType5 { get; }

        public List<ReactorFuelMode> Variants { get; }
        public List<ResourceGroupMetaData> ResourceGroups { get; }

        // Methods
        public List<ReactorFuelMode> GetVariantsOrderedByFuelRatio(Part part, double fuelEfficiency, double powerToSupply, double fuelUsePerMjMult, bool allowSimulate = true)
        {
            foreach (var fuelMode in Variants)
            {
                fuelMode.FuelRatio = fuelMode.ReactorFuels.Min(fuel => fuel.GetFuelRatio(part, fuelEfficiency, powerToSupply, fuelUsePerMjMult, allowSimulate && fuel.Simulate));
            }

            return Variants.OrderByDescending(m => m.FuelRatio).ThenBy(m => m.Position).ToList();
        }
    }

    class ReactorFuelMode
    {
        protected int ReactorType;
        public int Index { get; protected set; }
        public string Name { get; protected set; }
        public string ModeGUIName { get; protected set; }
        public string TechRequirement { get; protected set; }
        public List<ReactorFuel> ReactorFuels { get; protected set; }
        public  List<ReactorProduct> ReactorProducts { get; protected set; }
        public float ReactionRate { get; protected set; }
        public float PowerMultiplier { get; protected set; }
        public float NormalisedPowerRequirements { get; protected set; }
        public float ChargedPowerRatio { get; protected set; }
        public double MeVPerChargedProduct { get; protected set; }
        public float NeutronsRatio { get; protected set; }
        public float TritiumBreedModifier { get; protected set; }
        public double FuelEfficiencyMultiplier { get; protected set; }
        public bool RequiresLab { get; protected set; }
        public bool RequiresUpgrade { get; protected set; }
        public int TechLevel { get; protected set; }
        public int MinimumFusionGainFactor { get; protected set; }
        public bool Aneutronic { get; protected set; }

        public double GammaRayEnergy { get; protected set; }
        public double FuelUseInGramPerTeraJoule { get; protected set; }
        public double GigawattPerGram { get; protected set; }

        public string AlternativeFuelType1 { get; protected set; }
        public string AlternativeFuelType2 { get; protected set; }
        public string AlternativeFuelType3 { get; protected set; }
        public string AlternativeFuelType4 { get; protected set; }
        public string AlternativeFuelType5 { get; protected set; }

        public ReactorFuelMode(ConfigNode node)
        {
            Name = node.GetValue("name");
            ModeGUIName = node.GetValue("GUIName");
            ReactorType = Convert.ToInt32(node.GetValue("ReactorType"));
            Index = node.HasValue("Index") ? int.Parse(node.GetValue("Index")) : 0;

            TechRequirement = node.HasValue("TechRequirement") ? node.GetValue("TechRequirement") : String.Empty;

            AlternativeFuelType1 = node.HasValue("AlternativeFuelType1") ? node.GetValue("AlternativeFuelType1") : String.Empty;
            AlternativeFuelType2 = node.HasValue("AlternativeFuelType2") ? node.GetValue("AlternativeFuelType2") : String.Empty;
            AlternativeFuelType3 = node.HasValue("AlternativeFuelType3") ? node.GetValue("AlternativeFuelType3") : String.Empty;
            AlternativeFuelType4 = node.HasValue("AlternativeFuelType4") ? node.GetValue("AlternativeFuelType4") : String.Empty;
            AlternativeFuelType5 = node.HasValue("AlternativeFuelType5") ? node.GetValue("AlternativeFuelType5") : String.Empty;

            ReactionRate = node.HasValue("NormalisedReactionRate") ? Single.Parse(node.GetValue("NormalisedReactionRate")) : 1;
            PowerMultiplier = node.HasValue("NormalisedPowerMultiplier") ? Single.Parse(node.GetValue("NormalisedPowerMultiplier")) : 1;
            NormalisedPowerRequirements = node.HasValue("NormalisedPowerConsumption") ? Single.Parse(node.GetValue("NormalisedPowerConsumption")) : 1;
            ChargedPowerRatio = node.HasValue("ChargedParticleRatio") ? Single.Parse(node.GetValue("ChargedParticleRatio")) : 0;

            MeVPerChargedProduct = node.HasValue("MeVPerChargedProduct") ? Double.Parse(node.GetValue("MeVPerChargedProduct")) : 0;
            NeutronsRatio = node.HasValue("NeutronsRatio") ? Single.Parse(node.GetValue("NeutronsRatio")) : 1;
            TritiumBreedModifier = node.HasValue("TritiumBreedMultiplier") ? Single.Parse(node.GetValue("TritiumBreedMultiplier")) : 1;
            FuelEfficiencyMultiplier = node.HasValue("FuelEfficiencyMultiplier") ? Double.Parse(node.GetValue("FuelEfficiencyMultiplier")) : 1;

            RequiresLab = node.HasValue("RequiresLab") && Boolean.Parse(node.GetValue("RequiresLab"));
            RequiresUpgrade = node.HasValue("RequiresUpgrade") && Boolean.Parse(node.GetValue("RequiresUpgrade"));
            TechLevel = node.HasValue("TechLevel") ? Int32.Parse(node.GetValue("TechLevel")) : 0;
            MinimumFusionGainFactor = node.HasValue("MinimumQ") ? Int32.Parse(node.GetValue("MinimumQ")) : 0;
            Aneutronic = node.HasValue("Aneutronic") && Boolean.Parse(node.GetValue("Aneutronic"));
            GammaRayEnergy = node.HasValue("GammaRayEnergy") ? Double.Parse(node.GetValue("GammaRayEnergy")) : 0;


            ConfigNode[] fuelNodes = node.GetNodes("FUEL");
            ReactorFuels = fuelNodes.Select(nd => new ReactorFuel(nd)).ToList();

            ConfigNode[] productsNodes = node.GetNodes("PRODUCT");
            ReactorProducts = productsNodes.Select(nd => new ReactorProduct(nd)).ToList();

            AllFuelResourcesDefinitionsAvailable = ReactorFuels.All(m => m.Definition != null);
            AllProductResourcesDefinitionsAvailable = ReactorProducts.All(m => m.Definition != null);

            var totalTonsFuelUsePerMegaJoule = ReactorFuels.Sum(m => m.TonsFuelUsePerMJ);

            FuelUseInGramPerTeraJoule = totalTonsFuelUsePerMegaJoule * 1e12;

            GigawattPerGram = 1 / (totalTonsFuelUsePerMegaJoule * 1e9);
        }

        public int SupportedReactorTypes => ReactorType;

        public float NormalisedReactionRate => ReactionRate * PowerMultiplier;
        public int Position { get; set; }

        public double FuelRatio { get; set; }

        public bool AllFuelResourcesDefinitionsAvailable { get; }
        public bool AllProductResourcesDefinitionsAvailable { get; }

    }
}
