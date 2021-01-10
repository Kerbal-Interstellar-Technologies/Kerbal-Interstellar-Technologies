using System;
using System.Linq;
using KIT.PowerManagement.Interfaces;
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
        public IFNPowerSource Source { get; }

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

        public static PowerSourceSearchResult FindThermalSource(Part previousPart, Part currentPart, Func<IFNPowerSource, bool> condition, int stackDepth, int parentDepth, int surfaceDepth, bool skipSelfContained)
        {
            if (stackDepth <= 0)
            {
                var thermalSources = currentPart.FindModulesImplementing<IFNPowerSource>().Where(condition);

                var source = skipSelfContained
                    ? thermalSources.FirstOrDefault(s => !s.IsSelfContained)
                    : thermalSources.FirstOrDefault();

                if (source != null)
                    return new PowerSourceSearchResult(source, 0);
                else
                    return null;
            }

            var thermalCostModifier = currentPart.FindModuleImplementing<ThermalPowerTransport>();

            double stackDepthCost = thermalCostModifier != null
                ? thermalCostModifier.thermalCost
                : 1;

            // first look at docked parts
            foreach (var attachNodes in currentPart.attachNodes.Where(atn => atn.attachedPart != null && atn.attachedPart != previousPart && atn.nodeType == AttachNode.NodeType.Dock))
            {
                var source = FindThermalSource(currentPart, attachNodes.attachedPart, condition, (stackDepth - 1), parentDepth, surfaceDepth, skipSelfContained);

                if (source != null)
                    return source.IncreaseCost(stackDepthCost);
            }

            // then look at stack attached parts
            foreach (var attachNodes in currentPart.attachNodes.Where(atn => atn.attachedPart != null && atn.attachedPart != previousPart && atn.nodeType == AttachNode.NodeType.Stack))
            {
                var source = FindThermalSource(currentPart, attachNodes.attachedPart, condition, (stackDepth - 1), parentDepth, surfaceDepth, skipSelfContained);

                if (source != null)
                    return source.IncreaseCost(stackDepthCost);
            }

            // then look at parent parts
            if (parentDepth > 0 && currentPart.parent != null && currentPart.parent != previousPart)
            {
                var source = FindThermalSource(currentPart, currentPart.parent, condition, (stackDepth - 1), (parentDepth - 1), surfaceDepth, skipSelfContained);

                if (source != null)
                    return source.IncreaseCost(stackDepthCost);
            }

            // then look at surface attached parts
            if (surfaceDepth > 0)
            {
                foreach (var attachNodes in currentPart.attachNodes.Where(atn => atn.attachedPart != null && atn.attachedPart != previousPart && atn.nodeType == AttachNode.NodeType.Surface))
                {
                    var source = FindThermalSource(currentPart, attachNodes.attachedPart, condition, (stackDepth - 1), parentDepth, (surfaceDepth - 1), skipSelfContained);

                    if (source != null)
                        return source.IncreaseCost(stackDepthCost);
                }
            }

            return null;
        }
    }
}
