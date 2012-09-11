
using System;
using System.Collections.Generic;

namespace Starcounter.CommandLine.Syntax
{
    public sealed class ApplicationSyntaxDefinition : SyntaxDefinition, IApplicationSyntax
    {
        private Dictionary<string, CommandSyntaxDefinition> commands;

        public string ProgramDescription { get; set; }

        public bool RequiresCommand { get; set; }

        public string DefaultCommand { get; set; }

        public ApplicationSyntaxDefinition()
            : base()
        {
            this.commands = new Dictionary<string, CommandSyntaxDefinition>();
            this.ProgramDescription = null;
            this.RequiresCommand = false;
        }

        public CommandSyntaxDefinition DefineCommand(string name, string description)
        {
            return InternalDefineCommand(name, description, null, null);
        }

        public CommandSyntaxDefinition DefineCommand(string name, string description, int parameterCount)
        {
            return InternalDefineCommand(name, description, parameterCount, parameterCount);
        }

        public CommandSyntaxDefinition DefineCommand(string name, string description, int minParameterCount, int maxParameterCount)
        {
            return InternalDefineCommand(name, description, minParameterCount, maxParameterCount);
        }

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

        public IApplicationSyntax CreateSyntax()
        {
            VerifyConsistency();
            return this;
        }

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

        bool IApplicationSyntax.RequiresCommand
        {
            get { return this.RequiresCommand; }
        }

        string IApplicationSyntax.ProgramDescription
        {
            get { return this.ProgramDescription; }
        }

        OptionInfo[] IApplicationSyntax.Properties
        {
            get
            {
                return this.CreateOptionSet(this.Properties);
            }
        }

        OptionInfo[] IApplicationSyntax.Flags
        {
            get
            {
                return this.CreateOptionSet(this.Flags);
            }
        }

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

        void RaiseInconsistentSyntaxException(string message, params object[] arguments)
        {
            throw new FormatException(string.Format(message, arguments));
        }
    }
}
