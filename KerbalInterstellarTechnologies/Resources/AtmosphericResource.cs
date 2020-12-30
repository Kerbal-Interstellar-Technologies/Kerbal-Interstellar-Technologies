using System.Collections.Generic;
using System.Linq;

namespace KIT.Resources
{
    public class AtmosphericResource
    {
        public AtmosphericResource(PartResourceDefinition definition, double abundance)
        {
            this.ResourceName = definition.name;
            this.ResourceAbundance = abundance;
            this.DisplayName = string.IsNullOrEmpty(definition.displayName) ? definition.name : definition.displayName;
            this.Synonyms = new[] { ResourceName, DisplayName }.Distinct().ToList();
        }

        public AtmosphericResource(string resourceName, double abundance, string displayName)
        {
            this.ResourceName = resourceName;
            this.ResourceAbundance = abundance;
            this.DisplayName = displayName;
            this.Synonyms = new[] { resourceName }.ToList();
        }

        public AtmosphericResource(string resourceName, double abundance, string displayName, string[] synonyms)
        {
            this.ResourceName = resourceName;
            this.ResourceAbundance = abundance;
            this.DisplayName = displayName;
            this.Synonyms = synonyms.ToList();
        }

        public string DisplayName { get; private set; }
        public string ResourceName {get; private set;}
        public double ResourceAbundance { get; private set; }
        public List<string> Synonyms { get; private set; }
    }
}
