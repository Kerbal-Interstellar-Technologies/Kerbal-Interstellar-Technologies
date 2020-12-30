using System;
using System.Linq;
using KIT.Powermanagement;
using KIT.Reactors;

namespace KIT.Extensions
{
    public class PowerSourceSearchResult
    {
        public PowerSourceSearchResult(IFNPowerSource source, float cost)
        {
            Cost = cost;
            Source = source;
        }

        public double Cost { get; private set; }
        public IFNPowerSource Source { get; private set; }

        public PowerSourceSearchResult IncreaseCost(double cost)
        {
            Cost += cost;
            return this;
        }

        public static PowerSourceSearchResult BreadthFirstSearchForThermalSource(Part currentPart, Func<IFNPowerSource, bool> condition, int stackDepth, int parentDepth, int surfaceDepth, bool skipSelfContained = false)
        {
            // first search without parent search
            for (int currentDepth = 0; currentDepth <= stackDepth; currentDepth++)
            {
                var source = FindThermalSource(null, currentPart, condition, currentDepth, parentDepth, surfaceDepth, skipSelfContained);

                if (source != null)
                    return source;
            }

            return null;
        }

        public static PowerSourceSearchResult FindThermalSource(Part previousPart, Part currentpart, Func<IFNPowerSource, bool> condition, int stackdepth, int parentdepth, int surfacedepth, bool skipSelfContained)
        {
            if (stackdepth <= 0)
            {
                var thermalSources = currentpart.FindModulesImplementing<IFNPowerSource>().Where(condition);

                var source = skipSelfContained
                    ? thermalSources.FirstOrDefault(s => !s.IsSelfContained)
                    : thermalSources.FirstOrDefault();

                if (source != null)
                    return new PowerSourceSearchResult(source, 0);
                else
                    return null;
            }

            var thermalCostModifier = currentpart.FindModuleImplementing<ThermalPowerTransport>();

            double stackDepthCost = thermalCostModifier != null 
                ? thermalCostModifier.thermalCost 
                : 1;

            // first look at docked parts
            foreach (var attachNodes in currentpart.attachNodes.Where(atn => atn.attachedPart != null && atn.attachedPart != previousPart && atn.nodeType == AttachNode.NodeType.Dock))
            {
                var source = FindThermalSource(currentpart, attachNodes.attachedPart, condition, (stackdepth - 1), parentdepth, surfacedepth, skipSelfContained);

                if (source != null)
                    return source.IncreaseCost(stackDepthCost);
            }

            // then look at stack attached parts
            foreach (var attachNodes in currentpart.attachNodes.Where(atn => atn.attachedPart != null && atn.attachedPart != previousPart && atn.nodeType == AttachNode.NodeType.Stack))
            {
                var source = FindThermalSource(currentpart, attachNodes.attachedPart, condition, (stackdepth - 1), parentdepth, surfacedepth, skipSelfContained);

                if (source != null)
                    return source.IncreaseCost(stackDepthCost);
            }

            // then look at parent parts
            if (parentdepth > 0 && currentpart.parent != null && currentpart.parent != previousPart)
            {
                var source = FindThermalSource(currentpart, currentpart.parent, condition, (stackdepth - 1), (parentdepth - 1), surfacedepth, skipSelfContained);

                if (source != null)
                    return source.IncreaseCost(stackDepthCost);
            }

            // then look at surface attached parts
            if (surfacedepth > 0)
            {
                foreach (var attachNodes in currentpart.attachNodes.Where(atn => atn.attachedPart != null && atn.attachedPart != previousPart && atn.nodeType == AttachNode.NodeType.Surface))
                {
                    var source = FindThermalSource(currentpart, attachNodes.attachedPart, condition, (stackdepth - 1), parentdepth, (surfacedepth - 1), skipSelfContained);

                    if (source != null)
                        return source.IncreaseCost(stackDepthCost);
                }
            }

            return null;
        }
    }
}
