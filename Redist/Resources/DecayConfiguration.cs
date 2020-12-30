using System.Collections.Generic;
using UnityEngine;

namespace KIT.Resources
{
    public struct DecayConfiguration
    {
        public bool Valid;

        // Provided in the configuration file
        public ResourceName ResourceId;
        public ResourceName DecayProduct;
        public double DecayConstant;

        // Calculated at run time
        public double DensityRatio;
    }

    static class ResourceDecayConfiguration
    {
        public static bool Disabled;

        private static DecayConfiguration ParseConfig(ConfigNode node)
        {
            DecayConfiguration ret = new DecayConfiguration();
            string tmpStr = "";

            if (node.TryGetValue("decayProduct", ref tmpStr) == false)
            {
                Debug.Log($"[VesselDecay.ParseConfig] - resource configuration {node.name} is invalid. Error getting decayProduct");
                return ret;
            }
            ret.DecayProduct = KITResourceSettings.NameToResource(tmpStr);
            if (ret.DecayProduct == ResourceName.Unknown)
            {
                Debug.Log($"[VesselDecay.ParseConfig] - resource configuration {node.name} is invalid. Unable to convert decayProduct to a resource identifier");
                return ret;
            }
            if (!PartResourceLibrary.Instance.resourceDefinitions.Contains(tmpStr))
            {
                Debug.Log($"[VesselDecay.ParseConfig] - resource configuration {node.name} is invalid. {tmpStr} is an undefined resource");
                return ret;
            }
            ret.DensityRatio = PartResourceLibrary.Instance.GetDefinition(node.name).density / PartResourceLibrary.Instance.GetDefinition(tmpStr).density;

            if (node.TryGetValue("decayConstant", ref ret.DecayConstant) == false)
            {
                Debug.Log($"[VesselDecay.ParseConfig] - resource configuration {node.name} is invalid. Error getting decayConstant");
                return ret;
            }

            if (!PartResourceLibrary.Instance.resourceDefinitions.Contains(node.name))
            {
                Debug.Log($"[VesselDecay.ParseConfig] missing resource definition for {node.name}");
                return ret;
            }

            ret.ResourceId = KITResourceSettings.NameToResource(node.name);
            if (ret.ResourceId == ResourceName.Unknown)
            {
                Debug.Log($"[VesselDecay.ParseConfig] missing resource definition for either {ret.DecayProduct} or {node.name}");
                return ret;
            }

            ret.Valid = true;

            return ret;
        }

        public static void Initialize()
        {
            _configuration = new Dictionary<string, DecayConfiguration>(16);
            var decayConfigs = GameDatabase.Instance.GetConfigNodes("KIT_DECAY_CONFIG");
            if (decayConfigs == null || decayConfigs.Length == 0)
            {
                Disabled = true;
                return;
            }

            var decayConfig = decayConfigs[0];
            foreach (var v in decayConfig.GetNodes())
            {
                var c = ParseConfig(v);
                if (c.Valid == false)
                {
                    Debug.Log($"[VesselDecayConfiguration.Initialize] ignoring invalid configuration entry {v.name}");
                    continue;
                }

                _configuration[v.name] = c;
            }
        }

        private static Dictionary<string, DecayConfiguration> _configuration;

        public static Dictionary<string, DecayConfiguration> Instance()
        {
            if (_configuration == null && !Disabled) Initialize();
            return _configuration;
        }
    }
}
