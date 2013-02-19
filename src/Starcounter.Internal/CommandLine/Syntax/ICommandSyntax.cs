// ***********************************************************************
// <copyright file="ICommandSyntax.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

namespace Starcounter.CommandLine.Syntax
{
    /// <summary>
    /// Interface ICommandSyntax
    /// </summary>
    public interface ICommandSyntax
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }
        /// <summary>
        /// Gets the command description.
        /// </summary>
        /// <value>The command description.</value>
        string CommandDescription { get; }
        /// <summary>
        /// Gets the min parameter count.
        /// </summary>
        /// <value>The min parameter count.</value>
        int? MinParameterCount { get; }
        /// <summary>
        /// Gets the max parameter count.
        /// </summary>
        /// <value>The max parameter count.</value>
        int? MaxParameterCount { get; }
        /// <summary>
        /// Gets the properties.
        /// </summary>
        /// <value>The properties.</value>
        OptionInfo[] Properties { get; }
        /// <summary>
        /// Gets the flags.
        /// </summary>
        /// <value>The flags.</value>
        OptionInfo[] Flags { get; }
    }
}
