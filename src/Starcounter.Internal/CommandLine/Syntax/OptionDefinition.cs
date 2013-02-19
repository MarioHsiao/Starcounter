// ***********************************************************************
// <copyright file="OptionDefinition.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;

namespace Starcounter.CommandLine.Syntax
{
    /// <summary>
    /// Class OptionDefinition
    /// </summary>
    public abstract class OptionDefinition
    {
        /// <summary>
        /// The standard name of the option being defined.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// The description of the option being defined.
        /// </summary>
        /// <value>The description.</value>
        public string Description { get; set; }

        /// <summary>
        /// Alternative names of the option being defined.
        /// </summary>
        /// <value>The alternative names.</value>
        public string[] AlternativeNames { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionDefinition" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <exception cref="System.ArgumentNullException">name</exception>
        protected OptionDefinition(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            this.Name = name;
        }
    }
}
