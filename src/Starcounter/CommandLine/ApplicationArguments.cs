
using Starcounter.CommandLine.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Starcounter.CommandLine
{
    public sealed class ApplicationArguments : IApplicationInput
    {
        readonly StringDictionary GlobalOptions;
        readonly StringDictionary CommandOptions;
        Dictionary<string, GivenOption> OptionIndex;

        #region Standard API, used by clients to consult given input

        public readonly StringCollection CommandParameters;

        public string Command
        {
            get;
            internal set;
        }

        public bool HasCommmand
        {
            get { return string.IsNullOrEmpty(this.Command) == false; }
        }

        public bool IsCommand(string command)
        {
            if (string.IsNullOrEmpty(command))
                throw new ArgumentNullException("command");

            return this.HasCommmand 
                ? this.Command.Equals(command, StringComparison.InvariantCultureIgnoreCase) : 
                false;
        }

        /// <summary>
        /// Returns the value of a property identified by <paramref name="name"/>.
        /// If invoked with a name that is not found, null is returned.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GetProperty(string name)
        {
            GivenOption option = GetOptionFromIndex(name, OptionAttributes.Default, null);
            return option == null
                ? null
                : option.Value;
        }

        public string GetProperty(string name, CommandLineSection section)
        {
            GivenOption option = GetOptionFromIndex(name, OptionAttributes.Default, section);
            return option == null
                ? null
                : option.Value;
        }

        public bool TryGetProperty(string name, out string value)
        {
            GivenOption option;
            bool found;

            found = TryGetOptionFromIndex(name, OptionAttributes.Default, null, out option);
            value = found 
                ? option.Value : 
                null;
            
            return found;
        }

        public bool TryGetProperty(string name, CommandLineSection section, out string value)
        {
            GivenOption option;
            bool found;

            found = TryGetOptionFromIndex(name, OptionAttributes.Default, section, out option);
            value = found
                ? option.Value 
                : null;

            return found;
        }

        public bool ContainsProperty(string name)
        {
            GivenOption option;
            return TryGetOptionFromIndex(name, OptionAttributes.Default, null, out option);
        }

        public bool ContainsProperty(string name, CommandLineSection section)
        {
            GivenOption option;
            return TryGetOptionFromIndex(name, OptionAttributes.Default, section, out option);
        }

        public bool ContainsFlag(string name)
        {
            GivenOption option;
            return TryGetOptionFromIndex(name, OptionAttributes.Flag, null, out option);
        }

        public bool ContainsFlag(string name, CommandLineSection section)
        {
            GivenOption option;
            return TryGetOptionFromIndex(name, OptionAttributes.Flag, section, out option);
        }

        GivenOption GetOptionFromIndex(string name, OptionAttributes attributes, CommandLineSection? section)
        {
            GivenOption option;

            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            if (section.HasValue && section.Value == CommandLineSection.Command)
                throw new ArgumentOutOfRangeException("section");

            option = this.OptionIndex[name];

            if ((attributes & OptionAttributes.Flag) != 0 && option.IsFlag == false)
                new ArgumentException(string.Format("The option {0} is property, not a flag", name));

            if ((attributes & OptionAttributes.Flag) == 0 && option.IsFlag)
                new ArgumentException(string.Format("The option {0} is a flag, not a property", name));

            return section.HasValue
                ? section.Value == option.Section ? option : null
                : option;
        }

        bool TryGetOptionFromIndex(string name, OptionAttributes attributes, CommandLineSection? section, out GivenOption option)
        {
            bool found;

            option = null;

            if (string.IsNullOrEmpty(name)) 
                return false;

            if (section.HasValue && section.Value == CommandLineSection.Command)
                return false;

            found = this.OptionIndex.TryGetValue(name, out option);
            if (found == false)
                return found;

            if ((attributes & OptionAttributes.Flag) != 0 && option.IsFlag == false)
                return false;

            if ((attributes & OptionAttributes.Flag) == 0 && option.IsFlag)
                return false;

            return section.HasValue
                ? section.Value == option.Section ? true : false
                : true;
        }

        #endregion

        #region Implementation of, and support for, IApplicationInput functionality

        StringDictionary IApplicationInput.GlobalOptions
        {
            get { return this.GlobalOptions; }
        }

        StringDictionary IApplicationInput.CommandOptions
        {
            get { return this.CommandOptions; }
        }

        StringCollection IApplicationInput.CommandParameters
        {
            get { return this.CommandParameters; }
        }

        string IApplicationInput.Command
        {
            get { return this.Command; }
        }

        bool IApplicationInput.HasCommand
        {
            get { return this.HasCommmand; }
        }

        string IApplicationInput.GetProperty(string name)
        {
            IApplicationInput input = GetInput();
            string value = input.GetProperty(name, CommandLineSection.CommandParametersAndOptions);
            return value
                ?? input.GetProperty(name, CommandLineSection.GlobalOptions);
        }

        string IApplicationInput.GetProperty(string name, CommandLineSection section)
        {
            string value;

            value = GetOptionFromInput(name, section);
            return value != null && value.Equals(string.Empty) == false
                ? value
                : null;
        }

        bool IApplicationInput.ContainsProperty(string name)
        {
            return GetInput().GetProperty(name) != null;
        }

        bool IApplicationInput.ContainsProperty(string name, CommandLineSection section)
        {
            return GetInput().GetProperty(name, section) != null;
        }

        bool IApplicationInput.ContainsFlag(string name)
        {
            IApplicationInput input = GetInput();
            bool found = input.ContainsFlag(name, CommandLineSection.CommandParametersAndOptions);
            return found
                ? found
                : input.ContainsFlag(name, CommandLineSection.GlobalOptions);
        }

        bool IApplicationInput.ContainsFlag(string name, CommandLineSection section)
        {
            string value;
            value = GetOptionFromInput(name, section);
            return value != null && value.Equals(string.Empty);
        }

        IApplicationInput GetInput()
        {
            return this;
        }

        string GetOptionFromInput(string name, CommandLineSection section)
        {
            string value;

            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            value = null;

            switch (section)
            {
                case CommandLineSection.GlobalOptions:
                    value = this.GlobalOptions[name];
                    break;
                case CommandLineSection.CommandParametersAndOptions:
                    value = this.CommandOptions[name];
                    break;
                default:
                    throw new ArgumentOutOfRangeException("section");
            }

            return value;
        }

        #endregion

        internal ApplicationArguments()
        {
            this.GlobalOptions = new StringDictionary();
            this.CommandOptions = new StringDictionary();
            this.CommandParameters = new StringCollection();
        }

        internal void EnforceSyntax(IApplicationSyntax syntax)
        {
            ICommandSyntax commandSyntax;

            // This is executed before any client ever gets ahold of the
            // object, i.e. prior to it ever being queried for a command.
            // Hence, this is a good place to build up the proper
            // structure allowing intermixing between long and short names.

            if (syntax.RequiresCommand && this.HasCommmand == false)
                RaiseSyntaxErrorException(
                    "No command was specified.");

            if (this.HasCommmand == false) commandSyntax = null;
            else
            {
                commandSyntax = syntax.Commands.FirstOrDefault<ICommandSyntax>(delegate(ICommandSyntax candidate)
                {
                    return candidate.Name.Equals(this.Command, StringComparison.InvariantCultureIgnoreCase);
                });

                if (commandSyntax == null)
                    RaiseSyntaxErrorException("Command \"{0}\" is not recognized.", this.Command);
            }

            // Create the shared option index, used when clients ask for options
            // using the public API's and used by us to continue with a deeper
            // enforcement of syntax.

            this.OptionIndex = CreateOptionIndex(syntax, commandSyntax);

            // In the syntax definition, all flags and properties defined
            // on the application level is allowed in the global section
            // only. We must verify this, and verify all options that are
            // marked mandatory are really supplied too.

            EnforceGlobalOptionSyntax(syntax.Properties, OptionAttributes.Default);
            EnforceGlobalOptionSyntax(syntax.Flags, OptionAttributes.Flag);

            // Move from enforcing global application level syntax to check
            // if a command is given and if so, enforce that too.

            if (this.HasCommmand == false)
                return;

            // Verify the number of given parameters are right.

            if (commandSyntax.MinParameterCount.HasValue)
            {
                if (this.CommandParameters.Count < commandSyntax.MinParameterCount.Value)
                    RaiseSyntaxErrorException(
                        "Command {0} was given only {1} parameters. The minimum count is {2}.",
                        commandSyntax.Name,
                        this.CommandParameters.Count,
                        commandSyntax.MinParameterCount.Value
                        );
            }

            if (commandSyntax.MaxParameterCount.HasValue)
            {
                if (this.CommandParameters.Count > commandSyntax.MaxParameterCount.Value)
                    RaiseSyntaxErrorException(
                        "Command {0} was given {1} parameters. The maximum count is {2}.",
                        commandSyntax.Name,
                        this.CommandParameters.Count,
                        commandSyntax.MaxParameterCount.Value
                        );
            }

            // Verify the correctness of the command syntax

            EnforceCommandOptionSyntax(commandSyntax.Properties, OptionAttributes.Default);
            EnforceCommandOptionSyntax(commandSyntax.Flags, OptionAttributes.Flag);
        }

        void EnforceCommandOptionSyntax(OptionInfo[] options, OptionAttributes attributes)
        {
            GivenOption givenOption;
            bool found;

            foreach (OptionInfo option in options)
            {
                found = TryGetOptionFromIndex(option.Name, attributes, null, out givenOption);

                if (found == false)
                {
                    if ((option.Attributes & OptionAttributes.Required) == 0)
                        continue;

                    RaiseSyntaxErrorException(
                        "The command {0} requires the {1} {2} to be specified, but it was not given.",
                        this.Command,
                        option.Name,
                        (attributes & OptionAttributes.Flag) != 0 ? "flag" : "property"
                        );
                }
            }
        }

        void EnforceGlobalOptionSyntax(OptionInfo[] options, OptionAttributes attributes)
        {
            GivenOption givenOption;
            bool found;

            foreach (OptionInfo option in options)
            {
                found = TryGetOptionFromIndex(option.Name, attributes, null, out givenOption);

                if (found == false)
                {
                    if ((option.Attributes & OptionAttributes.Required) == 0)
                        continue;

                    RaiseSyntaxErrorException(
                        "The global {0} {1} is required but was not given.",
                        (attributes & OptionAttributes.Flag) != 0 ? "flag" : "property",
                        option.Name
                        );
                }
                
                if (givenOption.Section != CommandLineSection.GlobalOptions)
                    RaiseSyntaxErrorException(
                        "The {0} {1} is a global {0}, it must be given in the global section.",
                        (attributes & OptionAttributes.Flag) != 0 ? "flag" : "property",
                        option.Name
                        );
            }
        }

        #region Emission API, used by the parser

        internal void AddProperty(string name, string value)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            if (string.IsNullOrEmpty(value))
                throw new ArgumentNullException("value");

            AddOptionToDictionary(name, value);
        }

        internal void AddFlag(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            AddOptionToDictionary(name, string.Empty);
        }

        internal void AddParameter(string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentNullException("value");

            this.CommandParameters.Add(value);
        }

        void AddOptionToDictionary(string key, string value)
        {
            StringDictionary dictionary;

            dictionary = this.HasCommmand
                ? CommandOptions
                : GlobalOptions;

            dictionary.Add(key, value);
        }

        #endregion

        Dictionary<string, GivenOption> CreateOptionIndex(IApplicationSyntax syntax, ICommandSyntax commandSyntax)
        {
            Func<OptionInfo, bool> infoMatchPredicate;
            OptionInfo matchingInfo;
            GivenOption givenValue;
            Dictionary<string, GivenOption> resolvedOptions;
            CommandLineSection currentSection;
            string[] names;
            string value;

            resolvedOptions = new Dictionary<string, GivenOption>();

            foreach (StringDictionary dictionary in new object[] { this.GlobalOptions, this.CommandOptions })
            {
                currentSection = object.ReferenceEquals(dictionary, this.GlobalOptions)
                    ? CommandLineSection.GlobalOptions
                    : CommandLineSection.CommandParametersAndOptions;

                foreach (string key in dictionary.Keys)
                {
                    infoMatchPredicate = delegate(OptionInfo candidate)
                    {
                        return candidate.HasName(key);
                    };

                    matchingInfo = syntax.Flags.FirstOrDefault<OptionInfo>(infoMatchPredicate);
                    if (matchingInfo == null)
                        matchingInfo = syntax.Properties.FirstOrDefault<OptionInfo>(infoMatchPredicate);

                    if (matchingInfo == null && commandSyntax != null)
                    {
                        matchingInfo = commandSyntax.Flags.FirstOrDefault<OptionInfo>(infoMatchPredicate);
                        if (matchingInfo == null)
                            matchingInfo = commandSyntax.Properties.FirstOrDefault<OptionInfo>(infoMatchPredicate);
                    }

                    if (matchingInfo == null)
                        RaiseSyntaxErrorException("Option \"{0}\" is not recognized.", key);

                    value = dictionary[key];
                    if ((matchingInfo.Attributes & OptionAttributes.Flag) != 0 && value.Equals(string.Empty) == false)
                        RaiseSyntaxErrorException(
                            "Option \"{0}\" is a flag, it can not have a value.", key);

                    givenValue = new GivenOption();
                    givenValue.SpecifiedName = key;
                    givenValue.Value = value;
                    givenValue.Section = currentSection;

                    names = new string[1 + matchingInfo.AlternativeNames.Length];
                    names[0] = matchingInfo.Name;
                    matchingInfo.AlternativeNames.CopyTo(names, 1);

                    foreach (string validName in names)
                    {
                        resolvedOptions[validName] = givenValue;
                    }
                }
            }

            return resolvedOptions;
        }

        void RaiseSyntaxErrorException(string message, params object[] arguments)
        {
            string postfix;
            postfix = string.Format(message, arguments);
            throw ErrorCode.ToException(Error.SCERRBADCOMMANDLINESYNTAX, postfix);
        }
    }
}
