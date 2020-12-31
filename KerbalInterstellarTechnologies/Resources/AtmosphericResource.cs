using System.Collections.Generic;
using System.Linq;

namespace KIT.Resources
{
    public class AtmosphericResource
    {
        public AtmosphericResource(PartResourceDefinition definition, double abundance)
        {
            ResourceName = definition.name;
            ResourceAbundance = abundance;
            DisplayName = string.IsNullOrEmpty(definition.displayName) ? definition.name : definition.displayName;
            Synonyms = new[] { ResourceName, DisplayName }.Distinct().ToList();
        }

        public AtmosphericResource(string resourceName, double abundance, string displayName)
        {
            ResourceName = resourceName;
            ResourceAbundance = abundance;
            DisplayName = displayName;
            Synonyms = new[] { resourceName }.ToList();
        }

        public AtmosphericResource(string resourceName, double abundance, string displayName, string[] synonyms)
        {
            ResourceName = resourceName;
            ResourceAbundance = abundance;
            DisplayName = displayName;
            Synonyms = synonyms.ToList();
        }

        public string DisplayName { get; }
        public string ResourceName {get; }
        public double ResourceAbundance { get; }
        public List<string> Synonyms { get; }
    }
}
