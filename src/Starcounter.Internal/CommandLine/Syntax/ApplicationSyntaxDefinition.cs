// ***********************************************************************
// <copyright file="ApplicationSyntaxDefinition.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Internal;
using System;
using System.Collections.Generic;

namespace Starcounter.CommandLine.Syntax
{
    /// <summary>
    /// Class ApplicationSyntaxDefinition
    /// </summary>
    public sealed class ApplicationSyntaxDefinition : SyntaxDefinition, IApplicationSyntax
    {
        /// <summary>
        /// The commands
        /// </summary>
        private Dictionary<string, CommandSyntaxDefinition> commands;

        /// <summary>
        /// Gets or sets the program description.
        /// </summary>
        /// <value>The program description.</value>
        public string ProgramDescription { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [requires command].
        /// </summary>
        /// <value><c>true</c> if [requires command]; otherwise, <c>false</c>.</value>
        public bool RequiresCommand { get; set; }

        /// <summary>
        /// Gets or sets the default command.
        /// </summary>
        /// <value>The default command.</value>
        public string DefaultCommand { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationSyntaxDefinition" /> class.
        /// </summary>
        public ApplicationSyntaxDefinition()
            : base()
        {
            this.commands = new Dictionary<string, CommandSyntaxDefinition>();
            this.ProgramDescription = null;
            this.RequiresCommand = false;
        }

        /// <summary>
        /// Defines the command.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="description">The description.</param>
        /// <returns>CommandSyntaxDefinition.</returns>
        public CommandSyntaxDefinition DefineCommand(string name, string description)
        {
            return InternalDefineCommand(name, description, null, null);
        }

        /// <summary>
        /// Defines the command.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="description">The description.</param>
        /// <param name="parameterCount">The parameter count.</param>
        /// <returns>CommandSyntaxDefinition.</returns>
        public CommandSyntaxDefinition DefineCommand(string name, string description, int parameterCount)
        {
            return InternalDefineCommand(name, description, parameterCount, parameterCount);
        }

        /// <summary>
        /// Defines the command.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="description">The description.</param>
        /// <param name="minParameterCount">The min parameter count.</param>
        /// <param name="maxParameterCount">The max parameter count.</param>
        /// <returns>CommandSyntaxDefinition.</returns>
        public CommandSyntaxDefinition DefineCommand(string name, string description, int minParameterCount, int maxParameterCount)
        {
            return InternalDefineCommand(name, description, minParameterCount, maxParameterCount);
        }

        /// <summary>
        /// Internals the define command.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="description">The description.</param>
        /// <param name="minParameterCount">The min parameter count.</param>
        /// <param name="maxParameterCount">The max parameter count.</param>
        /// <returns>CommandSyntaxDefinition.</returns>
        /// <exception cref="System.ArgumentNullException">name</exception>
        /// <exception cref="System.ArgumentException"></exception>
        CommandSyntaxDefinition InternalDefineCommand(string name, string description, int? minParameterCount, int? maxParameterCount)
        {
            CommandSyntaxDefinition builder;

            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException("name");
            foreach (string prefix in Parser.OptionPrefixes)
                if (name.StartsWith(prefix))
                    throw new ArgumentException(string.Format("A command can not start with {0}", prefix));

            builder = new CommandSyntaxDefinition(name, description);
            builder.MinParameterCount = minParameterCount;
            builder.MaxParameterCount = maxParameterCount;

            commands.Add(name, builder);

            return builder;
        }

        /// <summary>
        /// Creates the syntax.
        /// </summary>
        /// <returns>IApplicationSyntax.</returns>
        public IApplicationSyntax CreateSyntax()
        {
            VerifyConsistency();
            return this;
        }

        /// <summary>
        /// Verifies the consistency.
        /// </summary>
        void VerifyConsistency()
        {
            int? minValue;
            int? maxValue;

            if (!string.IsNullOrEmpty(this.DefaultCommand))
            {
                // Check that the default command is part of the specified commands;
                // raise an exception if not.
                //
                // Proper error code.
                // TODO:

                if (!this.commands.ContainsKey(this.DefaultCommand))
                    throw ErrorCode.ToException(Error.SCERRUNSPECIFIED);
            }

            foreach (CommandSyntaxDefinition commandDefinition in this.commands.Values)
            {
                minValue = commandDefinition.MinParameterCount;
                maxValue = commandDefinition.MaxParameterCount;

                if (minValue.HasValue)
                {
                    if (minValue < 0 || (maxValue.HasValue && (minValue > maxValue.Value)))
                        RaiseInconsistentSyntaxException(
                            "The specified minimum parameter count for command {0} is invalid.", commandDefinition.Name);
                }

                if (maxValue.HasValue)
                {
                    if (maxValue < 0 || (minValue.HasValue && (maxValue < minValue.Value)))
                        RaiseInconsistentSyntaxException(
                            "The specified minimum parameter count for command {0} is invalid.", commandDefinition.Name);
                }
            }

            // NOTE:
            // Possibly verify the consistency of all given names,
            // including all alternative names, making sure they are
            // unique in the entire set, meaning we compare over properties
            // and flags for each command and match to those of the
            // application.
            //
            // Before doing this, we need a discussion of exactly how
            // to treat options that are allowed either with or w/o
            // a command, like for example a "server" property that
            // can be used to start the interpreter in the context of
            // a given server, but is also supported by individual
            // commands. From this, we get that an option can have
            // different meaning from global to a command. How do we
            // do when specifying the syntax for such a case?
        }

        #region IApplicationSyntax Members

        /// <summary>
        /// Gets a value indicating whether [requires command].
        /// </summary>
        /// <value><c>true</c> if [requires command]; otherwise, <c>false</c>.</value>
        bool IApplicationSyntax.RequiresCommand
        {
            get { return this.RequiresCommand; }
        }

        /// <summary>
        /// Gets the program description.
        /// </summary>
        /// <value>The program description.</value>
        string IApplicationSyntax.ProgramDescription
        {
            get { return this.ProgramDescription; }
        }

        /// <summary>
        /// Gets the properties.
        /// </summary>
        /// <value>The properties.</value>
        OptionInfo[] IApplicationSyntax.Properties
        {
            get
            {
                return this.CreateOptionSet(this.Properties);
            }
        }

        /// <summary>
        /// Gets the flags.
        /// </summary>
        /// <value>The flags.</value>
        OptionInfo[] IApplicationSyntax.Flags
        {
            get
            {
                return this.CreateOptionSet(this.Flags);
            }
        }

        /// <summary>
        /// Gets the commands.
        /// </summary>
        /// <value>The commands.</value>
        ICommandSyntax[] IApplicationSyntax.Commands
        {
            get
            {
                ICommandSyntax[] commandSet;
                int index;

                commandSet = new ICommandSyntax[this.commands.Count];
                index = 0;

                foreach (var item in this.commands.Values)
                {
                    commandSet[index++] = item;
                }

                return commandSet;
            }
        }

        #endregion

        /// <summary>
        /// Raises the inconsistent syntax exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="arguments">The arguments.</param>
        /// <exception cref="System.FormatException"></exception>
        void RaiseInconsistentSyntaxException(string message, params object[] arguments)
        {
            throw new FormatException(string.Format(message, arguments));
        }
    }
}
