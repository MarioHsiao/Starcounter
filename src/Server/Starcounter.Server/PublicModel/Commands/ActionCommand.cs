// ***********************************************************************
// <copyright file="ActionCommand.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Server.Commands;
using System;

namespace Starcounter.Server.PublicModel.Commands {
    /// <summary>
    /// Runs an <see cref="Action"/> in the server.
    /// </summary>
    public class ActionCommand : InvokableCommand {
        public readonly Action Target;

        /// <summary>
        /// Initializes a new <see cref="ActionCommand"/>.
        /// </summary>
        /// <param name="engine">The engine to which the command belongs.</param>
        /// <param name="action">The action to invoke.</param>
        /// <param name="descriptionFormat">Description of the command, possibly
        /// in a template format.</param>
        /// <param name="descriptionArgs">Optional args to be inserted into the
        /// description template.</param>
        public ActionCommand(
            ServerEngine engine, Action action, string descriptionFormat = "Running anonymous action", params object[] descriptionArgs) :
            base(engine, descriptionFormat, descriptionArgs) {
            this.Target = action;
        }

        /// <summary>
        /// Invokes the action attached to this command.
        /// </summary>
        /// <param name="processor"></param>
        internal override void Invoke(ICommandProcessor processor) {
            Target();
        }
    }

    public class ActionCommand<T> : InvokableCommand {
        public readonly Action<ICommandProcessor, T> Target;
        public readonly T Parameter1;

        /// <summary>
        /// Initializes a new <see cref="ActionCommand"/>.
        /// </summary>
        /// <param name="engine">The engine to which the command belongs.</param>
        /// <param name="action">The action to invoke.</param>
        /// <param name="param1">The first parameter to pass to the given action.
        /// </param>
        /// <param name="descriptionFormat">Description of the command, possibly
        /// in a template format.</param>
        /// <param name="descriptionArgs">Optional args to be inserted into the
        /// description template.</param>
        public ActionCommand(
            ServerEngine engine, 
            Action<ICommandProcessor, T> action,
            T param1,
            string descriptionFormat = "Running anonymous action",
            params object[] descriptionArgs) :
            base(engine, descriptionFormat, descriptionArgs) {
            this.Target = action;
            this.Parameter1 = param1;
        }

        /// </inheritdoc>
        internal override void Invoke(ICommandProcessor processor) {
            Target(processor, Parameter1);
        }
    }

    public class ActionCommand<T1, T2> : InvokableCommand {
        public readonly Action<ICommandProcessor, T1, T2> Target;
        public readonly T1 Parameter1;
        public readonly T2 Parameter2;

        /// <summary>
        /// Initializes a new <see cref="ActionCommand"/>.
        /// </summary>
        /// <param name="engine">The engine to which the command belongs.</param>
        /// <param name="action">The action to invoke.</param>
        /// <param name="param1">The first parameter to pass to the given action.
        /// </param>
        /// <param name="param2">The second parameter to pass to the given action.
        /// </param>
        /// <param name="descriptionFormat">Description of the command, possibly
        /// in a template format.</param>
        /// <param name="descriptionArgs">Optional args to be inserted into the
        /// description template.</param>
        public ActionCommand(
            ServerEngine engine,
            Action<ICommandProcessor, T1, T2> action,
            T1 param1,
            T2 param2,
            string descriptionFormat = "Running anonymous action",
            params object[] descriptionArgs) :
            base(engine, descriptionFormat, descriptionArgs) {
            this.Target = action;
            this.Parameter1 = param1;
            this.Parameter2 = param2;
        }

        /// </inheritdoc>
        internal override void Invoke(ICommandProcessor processor) {
            Target(processor, Parameter1, Parameter2);
        }
    }
}