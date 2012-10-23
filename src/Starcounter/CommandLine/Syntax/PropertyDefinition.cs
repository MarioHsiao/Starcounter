// ***********************************************************************
// <copyright file="PropertyDefinition.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

namespace Starcounter.CommandLine.Syntax
{
    /// <summary>
    /// Defines a property.
    /// </summary>
    public sealed class PropertyDefinition : OptionDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyDefinition" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public PropertyDefinition(string name)
            : base(name)
        {
        }
    }
}
