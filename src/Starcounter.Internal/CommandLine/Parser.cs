// ***********************************************************************
// <copyright file="Parser.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.CommandLine.Syntax;
using Starcounter.Internal;
using System;
using System.Linq;

namespace Starcounter.CommandLine
{
    /// <summary>
    /// Class Parser
    /// </summary>
    public sealed class Parser
    {
        /// <summary>
        /// The array of arguments the parser parses.
        /// </summary>
        public readonly string[] Arguments;

        /// <summary>
        /// Known option prefixes.
        /// </summary>
        public static readonly string[] OptionPrefixes = { "-", "--" };

        /// <summary>
        /// Known option suffixes.
        /// </summary>
        public static char[] OptionSuffixes = new char[] { ':', '=' };

        /// <summary>
        /// Keyword used to distinguish a flag from a property.
        /// </summary>
        public const string FlagKeyword = "FLAG";

        /// <summary>
        /// Initializes a <see cref="Parser" /> using the arguments
        /// returned from <see cref="Environment.GetCommandLineArgs" />.
        /// </summary>
        public Parser()
        {
            string[] args1;
            string[] args2;
            
            args1 = Environment.GetCommandLineArgs();
            args2 = new string[args1.Length - 1];
            Array.Copy(args1, 1, args2, 0, args1.Length - 1);

            this.Arguments = args2;
        }

        /// <summary>
        /// Initializes a <see cref="Parser" /> by specifying the
        /// arguments to be parsed.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <exception cref="System.ArgumentNullException">args</exception>
        public Parser(string[] args)
        {
            if (args == null) throw new ArgumentNullException("args");
            this.Arguments = args;
        }

        /// <summary>
        /// Parses the specified syntax.
        /// </summary>
        /// <param name="syntax">The syntax.</param>
        /// <returns>ApplicationArguments.</returns>
        /// <exception cref="System.ArgumentNullException">syntax</exception>
        public ApplicationArguments Parse(IApplicationSyntax syntax)
        {
            if (syntax == null)
                throw new ArgumentNullException("syntax");

            return InternalParse(syntax);
        }

        /// <summary>
        /// Internals the parse.
        /// </summary>
        /// <param name="syntax">The syntax.</param>
        /// <returns>ApplicationArguments.</returns>
        ApplicationArguments InternalParse(IApplicationSyntax syntax)
        {
            ApplicationArguments parsedArguments;
            string[] args;
            string token;
            string optionName;
            string optionValue;
            OptionAttributes optionAttributes;

            parsedArguments = new ApplicationArguments();
            args = this.Arguments;
            
            for (int i = 0; args != null && i < args.Length; i++)
            {
                token = args[i];

                if (StartsWithOptionPrefix(token))
                {
                    // Parse the token and assure the name.

                    ParseOptionString(token, i, out optionName, out optionValue, out optionAttributes);

                    // Both name and value was given. Put the option in it's right
                    // place depending on what section we are parsing.

                    if ((optionAttributes & OptionAttributes.Flag) != 0)
                        parsedArguments.AddFlag(optionName);
                    else
                        parsedArguments.AddProperty(optionName, optionValue);
                }
                else
                {
                    // The token didn't start with any known option prefix.
                    // This is either the command, or it is a parameter to the
                    // command.

                    if (parsedArguments.HasCommmand)
                    {
                        parsedArguments.AddParameter(token);
                    }
                    else
                    {
                        // Either this is a command or it is the first parameter to
                        // the "default", implicit command. We must consult the syntax
                        // to figure out which.

                        if (string.IsNullOrEmpty(syntax.DefaultCommand))
                        {
                            // If no default command exist, we treat the token as
                            // the command.
                            
                            parsedArguments.Command = token;
                        }
                        else
                        {
                            // There is a default command specification. We must check
                            // the precense of this command.

                            var commandSyntax = syntax.Commands.FirstOrDefault<ICommandSyntax>(delegate(ICommandSyntax candidate)
                            {
                                return candidate.Name.Equals(token, StringComparison.InvariantCultureIgnoreCase);
                            });

                            if (commandSyntax != null)
                            {
                                // The specified token was part of the commandset defined by
                                // the syntax. Treat it as such.

                                parsedArguments.Command = token;
                            }
                            else
                            {
                                // There was not representation of this token defined as a command
                                // in the syntax but the syntax specified a default command. We treat
                                // the given token as a parameter to the default command after first
                                // setting it.

                                parsedArguments.Command = syntax.DefaultCommand;
                                parsedArguments.AddParameter(token);
                            }
                        }
                    }
                }
            }

            // If we still have not found a command and we have a default command
            // specified in the syntax, apply that before enforcing the syntax.

            if (!parsedArguments.HasCommmand && !string.IsNullOrEmpty(syntax.DefaultCommand))
            {
                parsedArguments.Command = syntax.DefaultCommand;
            }

            parsedArguments.EnforceSyntax(syntax);

            return parsedArguments;
        }

        /// <summary>
        /// Returns true if the given value, after leading and trailing spaces
        /// have been removed, starts with an option prefix, meaning the user
        /// has provided an option and a value with no spaces in between, like
        /// "-myOption:MyValue".
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        bool StartsWithOptionPrefix(string value)
        {
            value = value.Trim();
            return OptionPrefixes.Any(delegate(string s)
            {
                return value.StartsWith(s, StringComparison.InvariantCultureIgnoreCase);
            });
        }

        /// <summary>
        /// Determines whether [is option flag] [the specified value].
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if [is option flag] [the specified value]; otherwise, <c>false</c>.</returns>
        bool IsOptionFlag(string value)
        {
            return value.Equals(Parser.FlagKeyword, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Parses the option string.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="argumentIndex">The index in the args[] array we are currently parsing.</param>
        /// <param name="optionName">Name of the option.</param>
        /// <param name="optionValue">The option value.</param>
        /// <param name="attributes">Attributes that applies to the given option,
        /// if such could be derived from looking at the specification itself.
        /// </param>
        void ParseOptionString(
            string value,
            int argumentIndex,
            out string optionName,
            out string optionValue,
            out OptionAttributes attributes) {
            int delimiterIndex;
            
            attributes = OptionAttributes.Default;

            // The given value starts with any of the prefixes. We 
            // expect everything to be part of the token, since we
            // have deprecated the usage of space as a delimiter.

            // Remove any found prefix characters.

            foreach (var prefix in OptionPrefixes) {
                while (value.StartsWith(prefix)) {
                    value = value.Substring(prefix.Length);
                }
            }

            value = value.Trim();
            if (value.Length == 0) {
                // No delimiter, and no value, meaning we found
                // only prefixes. Not allowed.
                RaiseSyntaxErrorException(Error.SCERRBADCOMMANDLINEFORMAT, argumentIndex, "Option prefix with no named option.");
            }

            delimiterIndex = value.IndexOfAny(OptionSuffixes);
            if (delimiterIndex == -1) {
                // No delimiter, indicating the syntax --option.
                // We consider this a flag option and the value
                // does represent the actual name of the flag
                // being set.

                optionName = value;
                optionValue = bool.TrueString;
                attributes |= OptionAttributes.Flag;

            } else if (delimiterIndex == (value.Length - 1)) {
                // The delimiter was the last character, like:
                // --foo=
                // We don't allow this.

                optionName = null;
                optionValue = null;
                RaiseSyntaxErrorException(
                    Error.SCERRBADCOMMANDLINEFORMAT, 
                    argumentIndex,
                    "Option with no value: {0}",
                    value
                    );

            } else {
                // It's a key/value pair, i.e. "key=value". We just
                // split it, handing out the name/key and the value
                // in the output.
                optionName = value.Substring(0, delimiterIndex);
                optionValue = value.Substring(delimiterIndex + 1);
            }

            // The final thing we do is that we check if the given
            // option is a flag, using the older --FLAG:x syntax.

            if (optionName.Equals(FlagKeyword)) {
                // The value contains the name of the flag.

                if (optionValue.Equals(string.Empty)) {
                    // The syntax --FLAG: with no named flag has been
                    // used. This we don't allow.
                    RaiseSyntaxErrorException(Error.SCERRBADCOMMANDLINEFORMAT, argumentIndex, "The {0} keyword was use, but the name of the flag was not given.", FlagKeyword);
                }

                optionName = optionValue;
                attributes |= OptionAttributes.Flag;
            }

            // Before returning, assure that we really got the name,
            // or else raise an exception.
            if (optionName.Equals(string.Empty))
                RaiseSyntaxErrorException(Error.SCERRBADCOMMANDLINEFORMAT, argumentIndex, "Option prefix with no named option.");
        }

        /// <summary>
        /// Raises the syntax error exception.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        /// <param name="argument">The argument.</param>
        /// <param name="message">The message.</param>
        /// <param name="messageParameters">The message parameters.</param>
        void RaiseSyntaxErrorException(
            uint errorCode,
            int argument, 
            string message, 
            params object[] messageParameters)
        {
            string postfix;

            if (string.IsNullOrEmpty(message) == false)
            {
                message = string.Format(message, messageParameters);
            }

            postfix = string.Format("Error near argument {0}, '{1}'. {2}.",
                argument + 1,
                this.Arguments[argument],
                message
                );

            throw ErrorCode.ToException(errorCode, postfix);
        }

        /// <summary>
        /// Raises the option with no value exception.
        /// </summary>
        /// <param name="specifiedOption">The specified option.</param>
        /// <param name="illegalArgument">The illegal argument.</param>
        void RaiseOptionWithNoValueException(string specifiedOption, int illegalArgument)
        {
            uint code;

            code = Error.SCERRBADCOMMANDLINEFORMAT;

            if (IsOptionFlag(specifiedOption))
            {
                RaiseSyntaxErrorException(
                    code,
                    illegalArgument, 
                    "Expected name of previously specified flag."
                    );
            }
            else
            {
                // Hint about the possible cause of the error is that the
                // user tries specifying an option that is a flag.

                RaiseSyntaxErrorException(
                    code,
                    illegalArgument,
                    "Expected value to option '{0}'. Did you intend to specify an option with no parameter, i.e a \"flag\"? If so, use the {1} keyword (like \"-{1}:NoLogging\"), or consult help to learn more.",
                    specifiedOption,
                    Parser.FlagKeyword
                    );
            }
        }
    }
}
