
using System;
using System.Collections.Generic;

namespace Starcounter.CommandLine.Syntax
{
    public abstract class SyntaxDefinition
    {
        protected readonly Dictionary<string, OptionInfo> Properties;
        protected readonly Dictionary<string, OptionInfo> Flags;

        static readonly string[] EmptyStringArray = new string[] { };

        protected SyntaxDefinition()
        {
            this.Properties = new Dictionary<string, OptionInfo>();
            this.Flags = new Dictionary<string, OptionInfo>();
        }

        public void DefineFlag(string name, string description)
        {
            DefineFlag(name, description, OptionAttributes.Flag, null);
        }

        public void DefineFlag(FlagDefinition definition)
        {
            DefineFlag(definition, OptionAttributes.Flag);
        }

        public void DefineFlag(FlagDefinition definition, OptionAttributes attributes)
        {
            DefineFlag(definition.Name, definition.Description, attributes, definition.AlternativeNames);
        }

        public void DefineFlag(string name, string description, OptionAttributes attributes, string[] alternativeNames)
        {
            DefineOption(
                name,
                description,
                attributes | OptionAttributes.Flag,
                alternativeNames
                );
        }

        public void DefineProperty(string name, string description)
        {
            DefineProperty(name, description, OptionAttributes.Default, null);
        }

        public void DefineProperty(PropertyDefinition definition)
        {
            DefineProperty(definition, OptionAttributes.Default);
        }

        public void DefineProperty(PropertyDefinition definition, OptionAttributes attributes)
        {
            DefineProperty(definition.Name, definition.Description, attributes, definition.AlternativeNames);
        }

        public void DefineProperty(string name, string description, OptionAttributes attributes, string[] alternativeNames)
        {
            if ((attributes & OptionAttributes.Flag) != 0)
                attributes ^= OptionAttributes.Flag;

            DefineOption(
                name,
                description,
                attributes,
                alternativeNames
                );
        }

        protected OptionInfo[] CreateOptionSet(Dictionary<string, OptionInfo> dictionary)
        {
            OptionInfo[] options;
            int index;

            options = new OptionInfo[dictionary.Count];
            index = 0;

            foreach (var item in dictionary.Values)
            {
                options[index++] = item;
            }

            return options;
        }

        void DefineOption(string name, string description, OptionAttributes attributes, string[] alternativeNames)
        {
            Dictionary<string, OptionInfo> dictionary;
            OptionInfo info;

            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            info = new OptionInfo()
            {
                Name = name,
                Description = description,
                Attributes = attributes,
                AlternativeNames = alternativeNames ?? SyntaxDefinition.EmptyStringArray
            };

            dictionary = (attributes & OptionAttributes.Flag) != 0
                ? this.Flags
                : this.Properties;

            dictionary.Add(name, info);
        }
    }
}
