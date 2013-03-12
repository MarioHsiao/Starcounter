// ***********************************************************************
// <copyright file="IApplicationInput.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Collections.Generic;

namespace Starcounter.CommandLine
{
    /// <summary>
    /// Interface IApplicationInput
    /// </summary>
    public interface IApplicationInput
    {
        /// <summary>
        /// Gets the global options.
        /// </summary>
        /// <value>The global options.</value>
        Dictionary<string, string> GlobalOptions { get; }
        /// <summary>
        /// Gets the command options.
        /// </summary>
        /// <value>The command options.</value>
        Dictionary<string, string> CommandOptions { get; }
        /// <summary>
        /// Gets the command parameters.
        /// </summary>
        /// <value>The command parameters.</value>
        List<string> CommandParameters { get; }
        /// <summary>
        /// Gets the command.
        /// </summary>
        /// <value>The command.</value>
        string Command { get; }
        /// <summary>
        /// Gets a value indicating whether this instance has command.
        /// </summary>
        /// <value><c>true</c> if this instance has command; otherwise, <c>false</c>.</value>
        bool HasCommand { get; }
        /// <summary>
        /// Gets the property.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>System.String.</returns>
        string GetProperty(string name);
        /// <summary>
        /// Gets the property.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="section">The section.</param>
        /// <returns>System.String.</returns>
        string GetProperty(string name, CommandLineSection section);
        /// <summary>
        /// Determines whether the specified name contains property.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns><c>true</c> if the specified name contains property; otherwise, <c>false</c>.</returns>
        bool ContainsProperty(string name);
        /// <summary>
        /// Determines whether the specified name contains property.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="section">The section.</param>
        /// <returns><c>true</c> if the specified name contains property; otherwise, <c>false</c>.</returns>
        bool ContainsProperty(string name, CommandLineSection section);
        /// <summary>
        /// Determines whether the specified name contains flag.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns><c>true</c> if the specified name contains flag; otherwise, <c>false</c>.</returns>
        bool ContainsFlag(string name);
        /// <summary>
        /// Determines whether the specified name contains flag.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="section">The section.</param>
        /// <returns><c>true</c> if the specified name contains flag; otherwise, <c>false</c>.</returns>
        bool ContainsFlag(string name, CommandLineSection section);
    }
}