using System;
using System.Collections.Generic;
using System.Linq;

namespace KIT.Resources
{
    class CrustalResource
    {
        public CrustalResource(string resourceName, double abundance, string displayName)
        {
            if (!String.IsNullOrEmpty(resourceName))
                Definition = PartResourceLibrary.Instance.GetDefinition(resourceName);

            ResourceName = resourceName;
            ResourceAbundance = abundance;
            DisplayName = displayName;
            Synonyms = new[] { resourceName }.ToList();


        }

        public CrustalResource(PartResourceDefinition definition, double abundance)
        {
            Definition = definition;
            ResourceName = definition.name;
            ResourceAbundance = abundance;
            DisplayName = string.IsNullOrEmpty(definition.displayName) ? definition.name : definition.displayName;
            Synonyms = new[] { ResourceName, DisplayName }.Distinct().ToList();
        }

        public CrustalResource(string resourceName, double abundance, string displayName, string[] synonyms)
        {
            if (!String.IsNullOrEmpty(resourceName))
                Definition = PartResourceLibrary.Instance.GetDefinition(resourceName);

            ResourceName = resourceName;
            ResourceAbundance = abundance;
            DisplayName = displayName;
            Synonyms = synonyms.ToList();
        }

        public double Production { get; set; }
        public double MaxAmount { get; set; }
        public double Amount { get; set; }
        public double SpareRoom { get; set; }
        public PartResourceDefinition Definition { get; }
        public string DisplayName { get; }
        public string ResourceName { get; }
        public double ResourceAbundance { get; }
        public List<string> Synonyms { get; }
    }
}
