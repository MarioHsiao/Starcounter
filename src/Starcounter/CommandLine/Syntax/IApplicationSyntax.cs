// ***********************************************************************
// <copyright file="IApplicationSyntax.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

namespace Starcounter.CommandLine.Syntax
{
    /// <summary>
    /// Interface IApplicationSyntax
    /// </summary>
    public interface IApplicationSyntax
    {
        /// <summary>
        /// Gets a value indicating whether [requires command].
        /// </summary>
        /// <value><c>true</c> if [requires command]; otherwise, <c>false</c>.</value>
        bool RequiresCommand { get; }
        /// <summary>
        /// Gets the default command.
        /// </summary>
        /// <value>The default command.</value>
        string DefaultCommand { get; }
        /// <summary>
        /// Gets the program description.
        /// </summary>
        /// <value>The program description.</value>
        string ProgramDescription { get; }
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
        /// <summary>
        /// Gets the commands.
        /// </summary>
        /// <value>The commands.</value>
        ICommandSyntax[] Commands { get; }
    }
}
