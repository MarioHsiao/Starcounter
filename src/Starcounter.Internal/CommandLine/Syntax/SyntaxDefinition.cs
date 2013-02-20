// ***********************************************************************
// <copyright file="SyntaxDefinition.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;

namespace Starcounter.CommandLine.Syntax
{
    /// <summary>
    /// Class SyntaxDefinition
    /// </summary>
    public abstract class SyntaxDefinition
    {
        /// <summary>
        /// The properties
        /// </summary>
        protected readonly Dictionary<string, OptionInfo> Properties;
        /// <summary>
        /// The flags
        /// </summary>
        protected readonly Dictionary<string, OptionInfo> Flags;

        /// <summary>
        /// The empty string array
        /// </summary>
        static readonly string[] EmptyStringArray = new string[] { };

        /// <summary>
        /// Initializes a new instance of the <see cref="SyntaxDefinition" /> class.
        /// </summary>
        protected SyntaxDefinition()
        {
            this.Properties = new Dictionary<string, OptionInfo>();
            this.Flags = new Dictionary<string, OptionInfo>();
        }

        /// <summary>
        /// Defines the flag.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="description">The description.</param>
        public void DefineFlag(string name, string description)
        {
            DefineFlag(name, description, OptionAttributes.Flag, null);
        }

        /// <summary>
        /// Defines the flag.
        /// </summary>
        /// <param name="definition">The definition.</param>
        public void DefineFlag(FlagDefinition definition)
        {
            DefineFlag(definition, OptionAttributes.Flag);
        }

        /// <summary>
        /// Defines the flag.
        /// </summary>
        /// <param name="definition">The definition.</param>
        /// <param name="attributes">The attributes.</param>
        public void DefineFlag(FlagDefinition definition, OptionAttributes attributes)
        {
            DefineFlag(definition.Name, definition.Description, attributes, definition.AlternativeNames);
        }

        /// <summary>
        /// Defines the flag.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="description">The description.</param>
        /// <param name="attributes">The attributes.</param>
        /// <param name="alternativeNames">The alternative names.</param>
        public void DefineFlag(string name, string description, OptionAttributes attributes, string[] alternativeNames)
        {
            DefineOption(
                name,
                description,
                attributes | OptionAttributes.Flag,
                alternativeNames
                );
        }

        /// <summary>
        /// Defines the property.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="description">The description.</param>
        public void DefineProperty(string name, string description)
        {
            DefineProperty(name, description, OptionAttributes.Default, null);
        }

        /// <summary>
        /// Defines the property.
        /// </summary>
        /// <param name="definition">The definition.</param>
        public void DefineProperty(PropertyDefinition definition)
        {
            DefineProperty(definition, OptionAttributes.Default);
        }

        /// <summary>
        /// Defines the property.
        /// </summary>
        /// <param name="definition">The definition.</param>
        /// <param name="attributes">The attributes.</param>
        public void DefineProperty(PropertyDefinition definition, OptionAttributes attributes)
        {
            DefineProperty(definition.Name, definition.Description, attributes, definition.AlternativeNames);
        }

        /// <summary>
        /// Defines the property.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="description">The description.</param>
        /// <param name="attributes">The attributes.</param>
        /// <param name="alternativeNames">The alternative names.</param>
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

        /// <summary>
        /// Creates the option set.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <returns>OptionInfo[][].</returns>
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

        /// <summary>
        /// Defines the option.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="description">The description.</param>
        /// <param name="attributes">The attributes.</param>
        /// <param name="alternativeNames">The alternative names.</param>
        /// <exception cref="System.ArgumentNullException">name</exception>
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
