
using System;

namespace Starcounter.CommandLine.Syntax
{
    public abstract class OptionDefinition
    {
        /// <summary>
        /// The standard name of the option being defined.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The description of the option being defined.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Alternative names of the option being defined.
        /// </summary>
        public string[] AlternativeNames { get; set; }

        protected OptionDefinition(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            this.Name = name;
        }
    }
}
