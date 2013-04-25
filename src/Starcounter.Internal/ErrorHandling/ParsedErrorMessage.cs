// ***********************************************************************
// <copyright file="ParsedErrorMessage.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Text.RegularExpressions;

namespace Starcounter.Internal
{

    /// <summary>
    /// Exposes the properties of a parsed error message string in the
    /// form or a <see cref="ErrorMessage"/>.
    /// </summary>
    public sealed class ParsedErrorMessage : ErrorMessage
    {
        /// <summary>
        /// The given message
        /// </summary>
        private readonly string givenMessage;
        /// <summary>
        /// The code
        /// </summary>
        private readonly uint code;
        /// <summary>
        /// The header
        /// </summary>
        private readonly string header;
        /// <summary>
        /// The body
        /// </summary>
        private readonly string body;
        /// <summary>
        /// The message
        /// </summary>
        private readonly string message;
        /// <summary>
        /// The helplink
        /// </summary>
        private readonly string helplink;
        /// <summary>
        /// The version
        /// </summary>
        private readonly string version;

        /// <summary>
        /// Creates an error message from an error message string.
        /// </summary>
        /// <param name="errorMessage">The message string to parse.</param>
        /// <returns>An error message exposing the properties of the parsed
        /// error message string.</returns>
        internal new static ParsedErrorMessage Parse(string errorMessage)
        {
            try
            {
                return InternalParseMessage(errorMessage);
            }
            catch (Exception e)
            {
                if (ErrorCode.IsFromErrorCode(e))
                    throw;

                throw ToParsingException(errorMessage, e);
            }
        }

        /// <summary>
        /// Internals the parse message.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>ParsedErrorMessage.</returns>
        /// <exception cref="System.ArgumentNullException">errorMessage</exception>
        private static ParsedErrorMessage InternalParseMessage(string errorMessage)
        {
            int index;
            int indexToDecoration;
            string header;
            uint code;
            string helplink;
            string message;
            string body;
            string versionMessage;
            string version;

            if (string.IsNullOrEmpty(errorMessage))
                throw new ArgumentNullException("errorMessage");

            // First extract the header, and via that, get the
            // error code. They are needed to parse the string.

            code = 0;
            index = ErrorMessage.IndexOfHeaderBodyDelimiter(errorMessage);
            header = errorMessage.Substring(0, index);

            // Get the error code from the header

            foreach (var number in Regex.Split(header, @"\D+"))
            {
                if (string.IsNullOrEmpty(number))
                    continue;

                if (code != 0) throw ToParsingException(errorMessage);

                code = uint.Parse(number);
            }

            // Get the decoration. The parsing of the message assumes
            // the message string is from the current version; if it is
            // not, parsing will fail.

            helplink = ErrorCode.ToHelpLink(code);
            version = CurrentVersion.Version;
            versionMessage = ErrorCode.ToVersionMessage();
            indexToDecoration = errorMessage.LastIndexOf(versionMessage);
            if (indexToDecoration == -1) throw ToParsingException(errorMessage);

            // With the index of the header-body delimiter still in the
            // register, get the message and the body.

            message = errorMessage.Remove(indexToDecoration);
            message = message.Trim();
            body = message.Substring(index + 1);
            body = body.Trim();

            return new ParsedErrorMessage(errorMessage, code, header, body, message, version, helplink);
        }

        /// <inheritdoc />
        public override uint Code
        {
            get { return code; }
        }

        /// <inheritdoc />
        public override string Header
        {
            get { return header; }
        }

        /// <inheritdoc />
        public override string Body
        {
            get { return body; }
        }

        /// <inheritdoc />
        public override string ShortMessage
        {
            get { throw new NotSupportedException(); }
        }

        /// <inheritdoc />
        public override string Message
        {
            get { return message; }
        }

        /// <inheritdoc />
        public override string Helplink
        {
            get { return helplink; }
        }

        /// <inheritdoc />
        public override string Version
        {
            get { return version; }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return givenMessage;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParsedErrorMessage" /> class.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="code">The code.</param>
        /// <param name="header">The header.</param>
        /// <param name="body">The body.</param>
        /// <param name="message">The message.</param>
        /// <param name="version">The version.</param>
        /// <param name="helplink">The helplink.</param>
        private ParsedErrorMessage(
            string input,
            uint code,
            string header,
            string body,
            string message,
            string version,
            string helplink)
        {
            this.givenMessage = input;
            this.code = code;
            this.header = header;
            this.body = body;
            this.message = message;
            this.version = version;
            this.helplink = helplink;
        }

        /// <summary>
        /// To the parsing exception.
        /// </summary>
        /// <param name="parsedMessage">The parsed message.</param>
        /// <returns>Exception.</returns>
        internal static Exception ToParsingException(string parsedMessage)
        {
            return ToParsingException(parsedMessage, null);
        }

        /// <summary>
        /// To the parsing exception.
        /// </summary>
        /// <param name="parsedMessage">The parsed message.</param>
        /// <param name="innerException">The inner exception.</param>
        /// <returns>Exception.</returns>
        internal static Exception ToParsingException(string parsedMessage, Exception innerException)
        {
            return ErrorCode.ToException(
                Error.SCERRWRONGERRORMESSAGEFORMAT,
                innerException,
                string.Format("Message: {0}", parsedMessage)
                );
        }
    }
}
