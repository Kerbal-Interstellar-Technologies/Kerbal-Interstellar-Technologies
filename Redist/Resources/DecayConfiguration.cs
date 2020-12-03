using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KIT.Resources
{
    public struct DecayConfiguration
    {
        public bool valid;

        // Provided in the configuration file
        public ResourceName resourceID;
        public ResourceName decayProduct;
        public double decayConstant;

        // Calculated at run time
        public double densityRatio;
    }

    static class ResourceDecayConfiguration
    {
        public static bool disabled;

        private static DecayConfiguration ParseConfig(ConfigNode node)
        {
            DecayConfiguration ret = new DecayConfiguration();
            string tmpStr = "";

            if (node.TryGetValue("decayProduct", ref tmpStr) == false)
            {
                Debug.Log($"[VesselDecay.ParseConfig] - resource configuration {node.name} is invalid. Error getting decayProduct");
                return ret;
            }
            ret.decayProduct = KITResourceSettings.NameToResource(tmpStr);
            if (ret.decayProduct == ResourceName.Unknown)
            {
                Debug.Log($"[VesselDecay.ParseConfig] - resource configuration {node.name} is invalid. Unable to convert decayProduct to a resource identifer");
                return ret;
            }
            if (!PartResourceLibrary.Instance.resourceDefinitions.Contains(tmpStr))
            {
                Debug.Log($"[VesselDecay.ParseConfig] - resource configuration {node.name} is invalid. {tmpStr} is an undefined resource");
                return ret;
            }
            ret.densityRatio = PartResourceLibrary.Instance.GetDefinition(node.name).density / PartResourceLibrary.Instance.GetDefinition(tmpStr).density;

            if (node.TryGetValue("decayConstant", ref ret.decayConstant) == false)
            {
                Debug.Log($"[VesselDecay.ParseConfig] - resource configuration {node.name} is invalid. Error getting decayConstant");
                return ret;
            }

            if (!PartResourceLibrary.Instance.resourceDefinitions.Contains(node.name))
            {
                Debug.Log($"[VesselDecay.ParseConfig] missing resource definition for {node.name}");
                return ret;
            }

            ret.resourceID = KITResourceSettings.NameToResource(node.name);
            if (ret.resourceID == ResourceName.Unknown)
            {
                Debug.Log($"[VesselDecay.ParseConfig] missing resource definition for either {ret.decayProduct} or {node.name}");
                return ret;
            }

            ret.valid = true;

            return ret;
        }

        public static void Initialize()
        {
            configuration = new Dictionary<string, DecayConfiguration>(16);
            var decayConfigs = GameDatabase.Instance.GetConfigNodes("KIT_DECAY_CONFIG");
            if (decayConfigs == null)
            {
                disabled = true;
                return;
            }

            var decayConfig = decayConfigs[0];
            foreach (var v in decayConfig.GetNodes())
            {
                var c = ParseConfig(v);
                if (c.valid == false)
                {
                    Debug.Log($"[VesselDecayConfiguration.Initialize] ignoring invalid configuration entry {v.name}");
                    continue;
                }

                configuration[v.name] = c;
            }
        }

        private static Dictionary<string, DecayConfiguration> configuration;

        public static Dictionary<string, DecayConfiguration> Instance()
        {
            if (configuration == null && !disabled) Initialize();
            return configuration;
        }
    }
}
