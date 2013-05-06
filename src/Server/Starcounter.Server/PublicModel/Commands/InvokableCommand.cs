// ***********************************************************************
// <copyright file="InvokableCommand.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Server.Commands;
using System;

namespace Starcounter.Server.PublicModel.Commands {

    /// <summary>
    /// Base class of all server commands that executes some
    /// runtime-bound piece of code (e.g. invoking some delegate).
    /// </summary>
    public abstract class InvokableCommand : ServerCommand {
        /// <summary>
        /// Initializes a new <see cref="InvokableCommand"/>.
        /// </summary>
        /// <param name="engine">The engine to which the command belongs.</param>
        /// <param name="descriptionFormat">Description of the command, possibly
        /// in a template format.</param>
        /// <param name="descriptionArgs">Optional args to be inserted into the
        /// description template.</param>
        protected InvokableCommand(ServerEngine engine, string descriptionFormat, params object[] descriptionArgs) :
            base(engine, descriptionFormat, descriptionArgs) {
        }

        /// <summary>
        /// Executes the logic attached to this command as decided
        /// by contrete implementations / subclasses of this class.
        /// </summary>
        /// <param name="processor">Interface to the processor that runs
        /// the current command.</param>
        internal abstract void Invoke(ICommandProcessor processor);
    }
}