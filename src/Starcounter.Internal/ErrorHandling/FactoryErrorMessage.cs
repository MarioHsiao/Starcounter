// ***********************************************************************
// <copyright file="FactoryErrorMessage.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Text;

namespace Starcounter.Internal
{

    /// <summary>
    /// Represents a error message describing a certain Starcounter
    /// error. Instances of this class is materialized by the exception
    /// factory classes and returned by the <see cref="ErrorCode.ToMessage(uint)"/>
    /// methods (internally used by <see cref="ErrorCode.ToException(uint)"/>).
    /// </summary>
    /// <remarks>
    /// The ErrorMessage class has an implicit conversion operator
    /// that converts a message object to a string, witch is the normal
    /// behaviour. But it also supports different kind of formatting of
    /// the message, such as shorter summary strings.
    /// </remarks>
    public sealed class FactoryErrorMessage : ErrorMessage
    {
        private readonly string messageFromResource;
        private readonly uint code;

        /// <inheritdoc />
        public override uint Code { get { return code; } }

        /// <summary>
        /// A possible postfix.
        /// </summary>
        public readonly string Postfix;

        /// <summary>
        /// A set of possible message arguments.
        /// </summary>
        public readonly object[] Arguments;

        /// <inheritdoc />
        public override string Helplink
        {
            get
            {
                return ErrorCode.ToHelpLink(this.Code);
            }
        }

        /// <inheritdoc />
        public override string ShortMessage
        {
            get
            {
                return this.Arguments == null || this.Arguments.Length == 0
                    ? this.messageFromResource
                    : string.Format(this.messageFromResource, this.Arguments);
            }
        }

        /// <inheritdoc />
        public override string Message
        {
            get
            {
                return InternalToString(false);
            }
        }

        /// <inheritdoc />
        public override string Body
        {
            get
            {
                // To support retreival of the body, we must parse the
                // underlying message, removing the header (the decoration
                // part is not part of the message).

                string message;

                message = this.Message;
                message = message.Substring(ErrorMessage.IndexOfHeaderBodyDelimiter(message) + 1);
                message = message.Trim();

                return message;
            }
        }

        /// <inheritdoc />
        public override string Header
        {
            get
            {
                return messageFromResource.Substring(0, ErrorMessage.IndexOfHeaderBodyDelimiter(this.messageFromResource));
            }
        }

        /// <inheritdoc />
        public override string Version
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Initializes an instance of <see cref="FactoryErrorMessage"/>.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        /// <param name="formattedShortMessage">
        /// The short, standard error message, as defined in the resource stream.</param>
        /// <param name="messagePostfix">A possible postfix.</param>
        /// <param name="messageArguments">Possible message arguments.</param>
        internal FactoryErrorMessage(
            uint errorCode,
            string formattedShortMessage,
            string messagePostfix,
            params object[] messageArguments
            )
        {
            this.code = errorCode;
            this.messageFromResource = formattedShortMessage;
            this.Postfix = messagePostfix;
            this.Arguments = messageArguments;
        }

        /// <summary>
        /// Allows <see cref="ErrorMessage"/> objects to be used as strings.
        /// </summary>
        /// <param name="message">The <see cref="ErrorMessage"/> to convert
        /// to a string.</param>
        /// <returns>A string representing the error message.</returns>
        /// <see cref="FactoryErrorMessage.ToString()"/>
        public static implicit operator string(FactoryErrorMessage message)
        {
            return message.ToString();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return InternalToString(true);
        }

        private string InternalToString(bool includeDecoration)
        {
            StringBuilder buffer;
            string message;

            buffer = new StringBuilder(this.messageFromResource, 1024);

            // Apply postfix if given

            if (!string.IsNullOrEmpty(this.Postfix))
            {
                buffer.Append(" ");
                buffer.Append(this.Postfix);
            }

            // Append the help link to the end of the message, if told
            // to do so.

            if (includeDecoration)
            {
                buffer.Append(Environment.NewLine);
                buffer.Append(ErrorCode.ToVersionMessage());

                buffer.Append(Environment.NewLine);
                buffer.Append(ErrorCode.ToHelpLinkMessage(this.Helplink));
            }

            // Construct the message

            message = buffer.ToString();

            // Apply arguments if available and return the result

            if (this.Arguments != null && this.Arguments.Length > 0)
            {
                message = string.Format(message, this.Arguments);
            }

            return message;
        }
    }
}
