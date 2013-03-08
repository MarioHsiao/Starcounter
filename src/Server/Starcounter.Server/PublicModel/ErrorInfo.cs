// ***********************************************************************
// <copyright file="ErrorInfo.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Internal;
using System;
using System.Collections.Generic;

namespace Starcounter.Server.PublicModel {

    /// <summary>
    /// An <see cref="ErrorInfo"/> can be converted to a <see cref="ErrorMessage"/>
    /// by calling <see cref="ToErrorMessage"/> and from this, various
    /// error related properties - including the full message - can be
    /// extracted and utilized by consumers (e.g. normally client management
    /// applications).
    /// </summary>
    public sealed class ErrorInfo {
        /// <summary>
        /// Prefix used when an <see cref="ErrorInfo"/> represent a message
        /// already properly created.
        /// </summary>
        internal const string FullMessagePrefix = "924C2085-2876-4F76-BE02-312CD21FFDE7";

        /// <summary>
        /// Prefix used when an <see cref="ErrorInfo"/> contains a given
        /// postfix, usually specified on the server, that needs to travel
        /// accross the wire.
        /// </summary>
        internal const string MessagePostfixPrefix = "34B66911-B05F-417D-82FA-3BA8BABE0F72";

        /// <summary>
        /// Enumerates over all error messages constructed from the given array
        /// of <see cref="ErrorInfo"/> instances, sorted from the most relevant
        /// to the outermost one.
        /// </summary>
        /// <param name="errors">Errors to iterate.</param>
        /// <returns>A enumerable collection with sorted error messages.</returns>
        public static IEnumerable<ErrorMessage> GetSortedErrorMessages(ErrorInfo[] errors) {
            for (int i = errors.Length - 1; i >= 0; i--) {
                yield return errors[i].ToErrorMessage();
            }
        }

        /// <summary>
        /// Creates a <see cref="ErrorInfo"/> from an exception.
        /// Normally used on the server to create error information
        /// objects that need to travel across the wire to a client
        /// where they can be recreated.
        /// </summary>
        /// <param name="e">
        /// The <see cref="Exception"/> from which we should create
        /// an <see cref="ErrorInfo"/> instance.</param>
        /// <returns>An <see cref="ErrorInfo"/> instance representing
        /// the given exception.</returns>
        internal static ErrorInfo FromException(Exception e) {
            uint errorCode;
            string argument;

            if (!ErrorCode.TryGetCode(e, out errorCode)) {
                // Parsing in this case will fail, since the message will
                // not be in a format the parser expect.
                //
                // We "wrap" the exception message inside an "unspecified
                // error", passing the original message as the postfix.

                errorCode = Error.SCERRUNSPECIFIED;

                return ErrorInfo.FromErrorCode(errorCode, string.Concat("Message: ", e.Message));
            }

            argument = string.Concat(ErrorInfo.FullMessagePrefix, e.Message);
            return new ErrorInfo(errorCode, argument);
        }

        /// <summary>
        /// Creates a <see cref="ErrorInfo"/> from an error code.
        /// Normally used on the server to create error information
        /// objects that need to travel across the wire to a client
        /// where they can be recreated.
        /// </summary>
        /// <param name="errorCode">The code from which we should
        /// create an <see cref="ErrorInfo"/> instance.</param>
        /// <returns>
        /// An <see cref="ErrorInfo"/> instance representing the
        /// given error code.
        /// </returns>
        internal static ErrorInfo FromErrorCode(uint errorCode) {
            return new ErrorInfo(errorCode);
        }

        /// <summary>
        /// Creates a <see cref="ErrorInfo"/> from an error code.
        /// Normally used on the server to create error information
        /// objects that need to travel across the wire to a client
        /// where they can be recreated.
        /// </summary>
        /// <param name="errorCode">The code from which we should
        /// create an <see cref="ErrorInfo"/> instance.</param>
        /// <param name="postfix">A message postfix.</param>
        /// <returns>
        /// An <see cref="ErrorInfo"/> instance representing the
        /// given error code.
        /// </returns>
        internal static ErrorInfo FromErrorCode(uint errorCode, string postfix) {
            if (string.IsNullOrEmpty(postfix))
                return FromErrorCode(errorCode);

            return new ErrorInfo(errorCode, string.Concat(ErrorInfo.MessagePostfixPrefix, postfix));
        }

        /// <summary>
        /// Creates a <see cref="ErrorInfo"/> from an error code.
        /// Normally used on the server to create error information
        /// objects that need to travel across the wire to a client
        /// where they can be recreated.
        /// </summary>
        /// <param name="errorCode">The code from which we should
        /// create an <see cref="ErrorInfo"/> instance.</param>
        /// <param name="postfix">A message postfix.</param>
        /// <param name="arguments">Message arguments to be inserted
        /// to the message.</param>
        /// <returns>
        /// An <see cref="ErrorInfo"/> instance representing the
        /// given error code.
        /// </returns>
        internal static ErrorInfo FromErrorCode(uint errorCode, string postfix, params string[] arguments) {
            string[] argumentsWithPostfix;
            string postfixEntry;

            if (string.IsNullOrEmpty(postfix))
                return new ErrorInfo(errorCode, arguments);

            postfixEntry = string.Concat(ErrorInfo.MessagePostfixPrefix, postfix);
            if (arguments == null || arguments.Length == 0)
                return new ErrorInfo(errorCode, postfixEntry);

            // We must make a new array, insert the postfix entry in the
            // first bucket and add the arguments.

            argumentsWithPostfix = new string[arguments.Length + 1];
            argumentsWithPostfix[0] = postfixEntry;
            Array.Copy(arguments, 0, argumentsWithPostfix, 1, arguments.Length);

            return new ErrorInfo(errorCode, argumentsWithPostfix);
        }

        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        internal ErrorInfo() {
        }

        /// <summary>
        /// Initializes a new <see cref="ErrorInfo"/>.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        /// <param name="arguments">Arguments. These arguments can be used to format
        /// the human-readable error message on the client; they can also be used
        /// by the client to implement repair actions.</param>
        internal ErrorInfo(string errorCode, params string[] arguments) {
            if (string.IsNullOrEmpty(errorCode)) {
                throw new ArgumentNullException("errorCode");
            }
            this.ErrorId = errorCode;
            this.Arguments = arguments;
        }

        /// <summary>
        /// Initializes a new <see cref="ErrorInfo"/>.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        /// <param name="arguments">Arguments. These arguments can be used to format
        /// the human-readable error message on the client; they can also be used
        /// by the client to implement repair actions.</param>
        internal ErrorInfo(uint errorCode, params string[] arguments) {
            this.ErrorId = errorCode.ToString();
            this.Arguments = arguments;
        }

        /// <summary>
        /// Gets or sets the error id.
        /// </summary>
        public string ErrorId {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the error arguments. These arguments can be used to format
        /// the human-readable error message on the client; they can also be used
        /// by the client to implement repair actions.
        /// </summary>
        public string[] Arguments {
            get;
            set;
        }

        /// <summary>
        /// Converts the current <see cref="ErrorInfo"/> to an <see cref="ErrorMessage"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="ErrorMessage"/> instance representing the error encapsulated
        /// by the current instance.
        /// </returns>
        public ErrorMessage ToErrorMessage() {
            string message;
            uint numericCode;
            string firstArgument;
            string[] argumentsWithoutPrefix;
            
            if (uint.TryParse(ErrorId, out numericCode)) {
                // Check the various tricks we have up our sleeve to
                // see how we should format the returned string.

                if (this.Arguments == null || this.Arguments.Length == 0)
                    return ErrorCode.ToMessage(numericCode);

                firstArgument = this.Arguments[0];

                if (firstArgument.StartsWith(ErrorInfo.FullMessagePrefix))
                    return ParsedErrorMessage.Parse(firstArgument.Remove(0, ErrorInfo.FullMessagePrefix.Length));

                if (firstArgument.StartsWith(ErrorInfo.MessagePostfixPrefix)) {
                    // First entry is a message postfix. Use it as such and
                    // create the string from the library.

                    firstArgument = firstArgument.Remove(0, ErrorInfo.MessagePostfixPrefix.Length);

                    if (this.Arguments.Length == 1)
                        return ErrorCode.ToMessage(numericCode, firstArgument);

                    argumentsWithoutPrefix = new string[this.Arguments.Length - 1];
                    Array.Copy(this.Arguments, 1, argumentsWithoutPrefix, 0, argumentsWithoutPrefix.Length);

                    return ErrorCode.ToMessageWithArguments(numericCode, firstArgument, argumentsWithoutPrefix);
                }

                return ErrorCode.ToMessageWithArguments(numericCode, string.Empty, this.Arguments);
            }

            message = string.Format("Invalid invocation: ErrorInfo.ToErrorMessage([\"{0}\"])", this.ErrorId);
            numericCode = Error.SCERRINVALIDOPERATION;
            throw ErrorCode.ToException(numericCode, message);
        }

        /// <summary>
        /// Tries to retrieve the underlying error code for the current
        /// instance.
        /// </summary>
        /// <param name="code">The error code, if the current instance
        /// was based on such.</param>
        /// <returns>True if an underlying error code could be retreived;
        /// false otherwise.</returns>
        public bool TryGetErrorCode(out uint code) {
            return uint.TryParse(this.ErrorId, out code);
        }
    }
}