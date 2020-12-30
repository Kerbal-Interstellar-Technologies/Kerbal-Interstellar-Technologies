using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace KIT.Extensions
{
    public static class PartExtensions
    {
        public static IEnumerable<PartResource> GetConnectedResources(this Part part, string resourceName)
        {
            return part.vessel.parts.SelectMany(p => p.Resources.Where(r => r.resourceName == resourceName));
        }

        public static void GetResourceMass(this Part part, PartResourceDefinition definition,  out double spareRoomMass, out double maximumMass) 
        {
            part.GetConnectedResourceTotals(definition.id, out var currentAmount, out var maxAmount);

            maximumMass = maxAmount * (double)(decimal)definition.density;
            spareRoomMass = (maxAmount - currentAmount) * (double)(decimal)definition.density;
        }

        private static FieldInfo _windowListField;

        /// <summary>
        /// Find the UIPartActionWindow for a part. Usually this is useful just to mark it as dirty.
        /// </summary>
        public static UIPartActionWindow FindActionWindow(this Part part)
        {
            if (part == null)
                return null;

            // We need to do quite a bit of piss-farting about with reflection to 
            // dig the thing out. We could just use Object.Find, but that requires hitting a heap more objects.
            UIPartActionController controller = UIPartActionController.Instance;
            if (controller == null)
                return null;

            if (_windowListField == null)
            {
                Type cntrType = typeof(UIPartActionController);
                foreach (FieldInfo info in cntrType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
                {
                    if (info.FieldType == typeof(List<UIPartActionWindow>))
                    {
                        _windowListField = info;
                        goto foundField;
                    }
                }
                Debug.LogWarning("*PartUtils* Unable to find UIPartActionWindow list");
                return null;
            }
            foundField:

            List<UIPartActionWindow> uiPartActionWindows = (List<UIPartActionWindow>)_windowListField.GetValue(controller);

            return uiPartActionWindows?.FirstOrDefault(window => window != null && window.part == part);
        }

        public static bool IsConnectedToModule(this Part currentPart, String partModule, int maxChildDepth, Part previousPart = null)
        {
            bool found = currentPart.Modules.Contains(partModule);
            if (found)
                return true;

            if (currentPart.parent != null && currentPart.parent != previousPart)
            {
                bool foundPart = IsConnectedToModule(currentPart.parent, partModule, maxChildDepth, currentPart);
                if (foundPart)
                    return true;
            }

            if (maxChildDepth > 0)
            {
                foreach (var child in currentPart.children.Where(c => c != null && c != previousPart))
                {
                    bool foundPart = IsConnectedToModule(child, partModule, (maxChildDepth - 1), currentPart);
                    if (foundPart)
                        return true;
                }
            }

            return false;
        }

        public static bool IsConnectedToPart(this Part currentPart, string partName, int maxChildDepth, Part previousPart = null)
        {
            bool found = currentPart.name == partName;
            if (found)
                return true;

            if (currentPart.parent != null && currentPart.parent != previousPart)
            {
                bool foundPart = IsConnectedToPart(currentPart.parent, partName, maxChildDepth, currentPart);
                if (foundPart)
                    return true;
            }

            if (maxChildDepth > 0)
            {
                foreach (var child in currentPart.children.Where(c => c != null && c != previousPart))
                {
                    bool foundPart = IsConnectedToPart(child, partName, (maxChildDepth - 1), currentPart);
                    if (foundPart)
                        return true;
                }
            }

            return false;
        }

        public static double FindAmountOfAvailableFuel(this Part currentPart, string resourceName, int maxChildDepth, Part previousPart = null)
        {
            double amount = 0;

            if (currentPart.Resources.Contains(resourceName))
            {
                var partResourceAmount = currentPart.Resources[resourceName].amount;
                //UnityEngine.Debug.Log("[KSPI]: found " + partResourceAmount.ToString("0.0000") + " " + resourceName + " resource in " + currentPart.name);
                amount += partResourceAmount;
            }

            if (currentPart.parent != null && currentPart.parent != previousPart)
                amount += FindAmountOfAvailableFuel(currentPart.parent, resourceName, maxChildDepth, currentPart);

            if (maxChildDepth <= 0) return amount;

            foreach (var child in currentPart.children.Where(c => c != null && c != previousPart))
            {
                amount += FindAmountOfAvailableFuel(child, resourceName, (maxChildDepth - 1), currentPart);
            }

            return amount;
        }

        public static double FindMaxAmountOfAvailableFuel(this Part currentPart, string resourceName, int maxChildDepth, Part previousPart = null)
        {
            double maxAmount = 0;

            if (currentPart.Resources.Contains(resourceName))
            {
                var partResourceAmount = currentPart.Resources[resourceName].maxAmount;
                //UnityEngine.Debug.Log("[KSPI]: found " + partResourceAmount.ToString("0.0000") + " " + resourceName + " resource in " + currentPart.name);
                maxAmount += partResourceAmount;
            }

            if (currentPart.parent != null && currentPart.parent != previousPart)
                maxAmount += FindMaxAmountOfAvailableFuel(currentPart.parent, resourceName, maxChildDepth, currentPart);

            if (maxChildDepth > 0)
            {
                foreach (var child in currentPart.children.Where(c => c != null && c != previousPart))
                {
                    maxAmount += FindMaxAmountOfAvailableFuel(child, resourceName, (maxChildDepth - 1), currentPart);
                }
            }

            return maxAmount;
        }
    }
}
