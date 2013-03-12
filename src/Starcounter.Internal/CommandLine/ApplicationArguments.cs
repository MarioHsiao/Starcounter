// ***********************************************************************
// <copyright file="ApplicationArguments.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.CommandLine.Syntax;
using Starcounter.Internal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Starcounter.CommandLine
{
    /// <summary>
    /// Class ApplicationArguments
    /// </summary>
    public sealed class ApplicationArguments : IApplicationInput
    {
        /// <summary>
        /// The global options
        /// </summary>
        readonly Dictionary<string, string> GlobalOptions;
        /// <summary>
        /// The command options
        /// </summary>
        readonly Dictionary<string, string> CommandOptions;
        /// <summary>
        /// The option index
        /// </summary>
        Dictionary<string, GivenOption> OptionIndex;

        #region Standard API, used by clients to consult given input

        /// <summary>
        /// The command parameters
        /// </summary>
        public readonly List<string> CommandParameters;

        /// <summary>
        /// Gets the command.
        /// </summary>
        /// <value>The command.</value>
        public string Command
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets a value indicating whether this instance has commmand.
        /// </summary>
        /// <value><c>true</c> if this instance has commmand; otherwise, <c>false</c>.</value>
        public bool HasCommmand
        {
            get { return string.IsNullOrEmpty(this.Command) == false; }
        }

        /// <summary>
        /// Determines whether the specified command is command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns><c>true</c> if the specified command is command; otherwise, <c>false</c>.</returns>
        /// <exception cref="System.ArgumentNullException">command</exception>
        public bool IsCommand(string command)
        {
            if (string.IsNullOrEmpty(command))
                throw new ArgumentNullException("command");

            return this.HasCommmand 
                ? this.Command.Equals(command, StringComparison.InvariantCultureIgnoreCase) : 
                false;
        }

        /// <summary>
        /// Returns the value of a property identified by <paramref name="name" />.
        /// If invoked with a name that is not found, null is returned.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>System.String.</returns>
        public string GetProperty(string name)
        {
            GivenOption option = GetOptionFromIndex(name, OptionAttributes.Default, null);
            return option == null
                ? null
                : option.Value;
        }

        /// <summary>
        /// Gets the property.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="section">The section.</param>
        /// <returns>System.String.</returns>
        public string GetProperty(string name, CommandLineSection section)
        {
            GivenOption option = GetOptionFromIndex(name, OptionAttributes.Default, section);
            return option == null
                ? null
                : option.Value;
        }

        /// <summary>
        /// Tries the get property.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
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

        /// <summary>
        /// Tries the get property.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="section">The section.</param>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
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

        /// <summary>
        /// Determines whether the specified name contains property.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns><c>true</c> if the specified name contains property; otherwise, <c>false</c>.</returns>
        public bool ContainsProperty(string name)
        {
            GivenOption option;
            return TryGetOptionFromIndex(name, OptionAttributes.Default, null, out option);
        }

        /// <summary>
        /// Determines whether the specified name contains property.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="section">The section.</param>
        /// <returns><c>true</c> if the specified name contains property; otherwise, <c>false</c>.</returns>
        public bool ContainsProperty(string name, CommandLineSection section)
        {
            GivenOption option;
            return TryGetOptionFromIndex(name, OptionAttributes.Default, section, out option);
        }

        /// <summary>
        /// Determines whether the specified name contains flag.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns><c>true</c> if the specified name contains flag; otherwise, <c>false</c>.</returns>
        public bool ContainsFlag(string name)
        {
            GivenOption option;
            return TryGetOptionFromIndex(name, OptionAttributes.Flag, null, out option);
        }

        /// <summary>
        /// Determines whether the specified name contains flag.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="section">The section.</param>
        /// <returns><c>true</c> if the specified name contains flag; otherwise, <c>false</c>.</returns>
        public bool ContainsFlag(string name, CommandLineSection section)
        {
            GivenOption option;
            return TryGetOptionFromIndex(name, OptionAttributes.Flag, section, out option);
        }

        /// <summary>
        /// Gets the index of the option from.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="attributes">The attributes.</param>
        /// <param name="section">The section.</param>
        /// <returns>GivenOption.</returns>
        /// <exception cref="System.ArgumentNullException">name</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">section</exception>
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

        /// <summary>
        /// Tries the index of the get option from.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="attributes">The attributes.</param>
        /// <param name="section">The section.</param>
        /// <param name="option">The option.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
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

        /// <summary>
        /// Gets the global options.
        /// </summary>
        /// <value>The global options.</value>
        Dictionary<string, string> IApplicationInput.GlobalOptions
        {
            get { return this.GlobalOptions; }
        }

        /// <summary>
        /// Gets the command options.
        /// </summary>
        /// <value>The command options.</value>
        Dictionary<string, string> IApplicationInput.CommandOptions
        {
            get { return this.CommandOptions; }
        }

        /// <summary>
        /// Gets the command parameters.
        /// </summary>
        /// <value>The command parameters.</value>
        List<string> IApplicationInput.CommandParameters
        {
            get { return this.CommandParameters; }
        }

        /// <summary>
        /// Gets the command.
        /// </summary>
        /// <value>The command.</value>
        string IApplicationInput.Command
        {
            get { return this.Command; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance has command.
        /// </summary>
        /// <value><c>true</c> if this instance has command; otherwise, <c>false</c>.</value>
        bool IApplicationInput.HasCommand
        {
            get { return this.HasCommmand; }
        }

        /// <summary>
        /// Gets the property.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>System.String.</returns>
        string IApplicationInput.GetProperty(string name)
        {
            IApplicationInput input = GetInput();
            string value = input.GetProperty(name, CommandLineSection.CommandParametersAndOptions);
            return value
                ?? input.GetProperty(name, CommandLineSection.GlobalOptions);
        }

        /// <summary>
        /// Gets the property.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="section">The section.</param>
        /// <returns>System.String.</returns>
        string IApplicationInput.GetProperty(string name, CommandLineSection section)
        {
            string value;

            value = GetOptionFromInput(name, section);
            return value != null && value.Equals(string.Empty) == false
                ? value
                : null;
        }

        /// <summary>
        /// Determines whether the specified name contains property.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns><c>true</c> if the specified name contains property; otherwise, <c>false</c>.</returns>
        bool IApplicationInput.ContainsProperty(string name)
        {
            return GetInput().GetProperty(name) != null;
        }

        /// <summary>
        /// Determines whether the specified name contains property.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="section">The section.</param>
        /// <returns><c>true</c> if the specified name contains property; otherwise, <c>false</c>.</returns>
        bool IApplicationInput.ContainsProperty(string name, CommandLineSection section)
        {
            return GetInput().GetProperty(name, section) != null;
        }

        /// <summary>
        /// Determines whether the specified name contains flag.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns><c>true</c> if the specified name contains flag; otherwise, <c>false</c>.</returns>
        bool IApplicationInput.ContainsFlag(string name)
        {
            IApplicationInput input = GetInput();
            bool found = input.ContainsFlag(name, CommandLineSection.CommandParametersAndOptions);
            return found
                ? found
                : input.ContainsFlag(name, CommandLineSection.GlobalOptions);
        }

        /// <summary>
        /// Determines whether the specified name contains flag.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="section">The section.</param>
        /// <returns><c>true</c> if the specified name contains flag; otherwise, <c>false</c>.</returns>
        bool IApplicationInput.ContainsFlag(string name, CommandLineSection section)
        {
            string value;
            value = GetOptionFromInput(name, section);
            return value != null && value.Equals(string.Empty);
        }

        /// <summary>
        /// Gets the input.
        /// </summary>
        /// <returns>IApplicationInput.</returns>
        IApplicationInput GetInput()
        {
            return this;
        }

        /// <summary>
        /// Gets the option from input.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="section">The section.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">name</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">section</exception>
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

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationArguments" /> class.
        /// </summary>
        internal ApplicationArguments()
        {
            this.GlobalOptions = new Dictionary<string, string>();
            this.CommandOptions = new Dictionary<string, string>();
            this.CommandParameters = new List<string>();
        }

        /// <summary>
        /// Enforces the syntax.
        /// </summary>
        /// <param name="syntax">The syntax.</param>
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

        /// <summary>
        /// Enforces the command option syntax.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="attributes">The attributes.</param>
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

        /// <summary>
        /// Enforces the global option syntax.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="attributes">The attributes.</param>
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

        /// <summary>
        /// Adds the property.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.ArgumentNullException">name</exception>
        internal void AddProperty(string name, string value)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            if (value == null)
                throw new ArgumentNullException("value");

            AddOptionToDictionary(name, value);
        }

        /// <summary>
        /// Adds the flag.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <exception cref="System.ArgumentNullException">name</exception>
        internal void AddFlag(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            AddOptionToDictionary(name, string.Empty);
        }

        /// <summary>
        /// Adds the parameter.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <exception cref="System.ArgumentNullException">value</exception>
        internal void AddParameter(string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentNullException("value");

            this.CommandParameters.Add(value);
        }

        /// <summary>
        /// Adds the option to dictionary.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        void AddOptionToDictionary(string key, string value)
        {
            Dictionary<string, string> dictionary;

            dictionary = this.HasCommmand
                ? CommandOptions
                : GlobalOptions;

            dictionary.Add(key, value);
        }

        #endregion

        /// <summary>
        /// Creates the index of the option.
        /// </summary>
        /// <param name="syntax">The syntax.</param>
        /// <param name="commandSyntax">The command syntax.</param>
        /// <returns>Dictionary{System.StringGivenOption}.</returns>
        Dictionary<string, GivenOption> CreateOptionIndex(IApplicationSyntax syntax, ICommandSyntax commandSyntax)
        {
            Func<OptionInfo, bool> infoMatchPredicate;
            OptionInfo matchingInfo;
            GivenOption givenValue;
            Dictionary<string, GivenOption> resolvedOptions;
            CommandLineSection currentSection;
            string value;

            resolvedOptions = new Dictionary<string, GivenOption>();

            foreach (var dictionary in new Dictionary<string, string>[] { this.GlobalOptions, this.CommandOptions })
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
                    givenValue.IsFlag = (matchingInfo.Attributes & OptionAttributes.Flag) != 0;

                    foreach (string validName in matchingInfo.AllNames)
                    {
                        resolvedOptions[validName] = givenValue;
                    }
                }
            }

            return resolvedOptions;
        }

        /// <summary>
        /// Raises the syntax error exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="arguments">The arguments.</param>
        void RaiseSyntaxErrorException(string message, params object[] arguments)
        {
            string postfix;
            postfix = string.Format(message, arguments);
            throw ErrorCode.ToException(Error.SCERRBADCOMMANDLINESYNTAX, postfix);
        }
    }
}
