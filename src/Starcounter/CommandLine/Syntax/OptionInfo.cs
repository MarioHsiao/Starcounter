
using System;

namespace Starcounter.CommandLine.Syntax
{
    public class OptionInfo
    {
        public string Name { get; set; }
        public string[] AlternativeNames { get; set; }
        public string Description { get; set; }
        public OptionAttributes Attributes { get; set; }

        public bool HasName(string name)
        {
            return HasName(name, StringComparison.InvariantCultureIgnoreCase);
        }

        public bool HasName(string name, StringComparison comparisonMethod)
        {
            if (this.Name.Equals(name, comparisonMethod))
                return true;

            if (this.AlternativeNames == null)
                return false;

            foreach (string alternative in this.AlternativeNames)
            {
                if (alternative.Equals(name, comparisonMethod))
                    return true;
            }

            return false;
        }
    }
}
