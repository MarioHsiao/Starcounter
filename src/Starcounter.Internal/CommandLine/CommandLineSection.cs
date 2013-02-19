// ***********************************************************************
// <copyright file="CommandLineSection.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

namespace Starcounter.CommandLine
{
    /// <summary>
    /// Enum CommandLineSection
    /// </summary>
    public enum CommandLineSection
    {
        /// <summary>
        /// The global options
        /// </summary>
        GlobalOptions,
        /// <summary>
        /// The command
        /// </summary>
        Command,
        /// <summary>
        /// The command parameters and options
        /// </summary>
        CommandParametersAndOptions
    }
}
