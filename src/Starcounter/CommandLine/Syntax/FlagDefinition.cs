// ***********************************************************************
// <copyright file="FlagDefinition.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

namespace Starcounter.CommandLine.Syntax
{
    /// <summary>
    /// Defines a flag.
    /// </summary>
    public sealed class FlagDefinition : OptionDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FlagDefinition" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public FlagDefinition(string name)
            : base(name)
        {
        }
    }
}
