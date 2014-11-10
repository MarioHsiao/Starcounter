// ***********************************************************************
// <copyright file="ErrorMessage.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Text.RegularExpressions;

namespace Starcounter.Internal
{

    /// <summary>
    /// Represents an error message, with properties for different parts
    /// of the encapsulated message. Derived classes are expected to implement
    /// the <see cref="object.ToString()"/> to return the full message.
    /// </summary>
    /// <remarks>
    /// Derived classes are expected to implement as many of the properties
    /// as possible and raise exceptions for those they can not implement,
    /// either as <see cref="System.NotSupportedException"/> if they have no
    /// intention to support the property or <see cref="System.NotImplementedException"/>
    /// if they intend to support a property but doesn't just yet.
    /// </remarks>
    /// <example>
    /// Given this error message,
    /// 
    /// [Message]
    /// ScErrCantReadFile (SCERR12345): The program failed to read the file. File: 'MyFile.txt'.
    /// Version: 2.0.123.3456.
    /// Help page: http://www.starcounter.com/wiki/SCERR12345.
    /// [/Message]
    /// 
    /// the various properties, when implemented by a derived class, is
    /// expected to return:
    /// 
    /// Code:           12345
    /// DecoratedCode:  SCERR12345
    /// Header:         ScErrCantReadFile (SCERR12345)
    /// Body:           The program failed to read the file. File: 'MyFile.txt'.
    /// Brief:          The program failed to read the file.
    /// ShortMessage:   ScErrCantReadFile (SCERR12345): The program failed to read the file.
    /// Message:        ScErrCantReadFile (SCERR12345): The program failed to read the file. File: 'MyFile.txt'.        
    /// Helplink:       http://www.starcounter.com/wiki/SCERR12345
    /// Version:        2.0.123.3456.
    /// ToString():     The full error message, as seen above.
    /// </example>
    public abstract class ErrorMessage
    {
        /// <summary>
        /// Delimiter used to end the header (marking the beginning of
        /// the body>.
        /// </summary>
        public const string HeaderBodyDelimiter = ":";

        /// <summary>
        /// Gets the error code.
        /// </summary>
        public abstract uint Code { get; }

        /// <summary>
        /// Gets the header ("ScErrMyError (SCERR123)")
        /// </summary>
        public abstract string Header { get; }

        /// <summary>
        /// Gets only the body, i.e the message without the header
        /// and without decoration.
        /// </summary>
        public abstract string Body { get; }

        /// <summary>
        /// Gets a shorter variant of the message, i.e the the header
        /// and the body but without prefixes/postfixes and decoration.
        /// </summary>
        public abstract string ShortMessage { get; }

        /// <summary>
        /// Gets only the message, i.e the the header and the body
        /// but without decoration.
        /// </summary>
        public abstract string Message { get; }

        /// <summary>
        /// Gets the help link.
        /// </summary>
        public abstract string Helplink { get; }

        /// <summary>
        /// Gets the version.
        /// </summary>
        public abstract string Version { get; }

        /// <summary>
        /// Gets the decorated form of the error code.
        /// </summary>
        public string DecoratedCode
        {
            get
            {
                return ErrorCode.ToDecoratedCode(this.Code);
            }
        }

        /// <summary>
        /// Gets the brief of the error message.
        /// </summary>
        public virtual string Brief
        {
            get
            {
                int index;
                string brief;

                brief = this.Body;
                index = brief.IndexOf(".");
                return index == -1 ? brief : brief.Substring(0, index + 1);
            }
        }

        /// <summary>
        /// Creates an error message from an error message string.
        /// </summary>
        /// <param name="errorMessage">The message string to parse.</param>
        /// <returns>An error message exposing the properties of the parsed
        /// error message string.</returns>
        public static ErrorMessage Parse(string errorMessage)
        {
            return ParsedErrorMessage.Parse(errorMessage);
        }

        /// <summary>
        /// Creates an error message from an error message string.
        /// </summary>
        /// <param name="errorMessage">The message string to parse.</param>
        /// <param name="location">A <see cref="FileLocation"/> indicating
        /// a source code location of the error, if such location information
        /// is part of the given string. If it's not, or if parsing it fails,
        /// <see cref="FileLocation.Unknown"/> is returned.</param>
        /// <returns>An error message exposing the properties of the parsed
        /// error message string.</returns>
        public static ErrorMessage Parse(string errorMessage, out FileLocation location)
        {
            Match match;

            location = FileLocation.Unknown;
            if (string.IsNullOrEmpty(errorMessage))
                throw new ArgumentNullException("errorMessage");

            // Check for the presence of location information

            match = FileLocation.RegexPattern.Match(errorMessage);
            if (match.Success)
            {
                // We should strip the location information from the string
                // and just pass the real one on.

                location = FileLocation.FromMatch(match);
                errorMessage = errorMessage.Substring(0, match.Index);
            }

            // Parse the actual error message

            return ParsedErrorMessage.Parse(errorMessage);
        }

        /// <summary>
        /// Converts this message to an exception.
        /// </summary>
        /// <remarks>
        /// The type of exception is decided by the installed exception
        /// factory.
        /// </remarks>
        /// <returns></returns>
        public Exception ToException()
        {
            return ToException(null);
        }

        /// <summary>
        /// Converts this message to an exception, specifying an
        /// inner exception in the call.
        /// </summary>
        /// <remarks>
        /// The type of exception is decided by the installed exception
        /// factory.
        /// </remarks>
        /// <param name="inner"></param>
        /// <returns></returns>
        public Exception ToException(Exception inner)
        {
            return ErrorCode.RecreateException(this.Code, ToString(), inner);
        }

        /// <summary>
        /// Returns the error message in it's full string form.
        /// </summary>
        /// <remarks>
        /// Gets a string representing the full error message, similar to
        /// the original message examplified in the API documentation of
        /// <see cref="ErrorMessage"/>.
        /// </remarks>
        /// <returns>A string describing the represented error.</returns>
        public abstract override string ToString();

        /// <summary>
        /// Gets the index of the header-body delimiter from the given
        /// error message.
        /// </summary>
        /// <remarks>
        /// If the delimiter is not found, an exception is thrown with
        /// an error code indicating the message doesn't apply to the
        /// expected error message format.
        /// </remarks>
        /// <param name="message">The message to parse.</param>
        /// <returns>Index of the header-body delimiter.</returns>
        internal static int IndexOfHeaderBodyDelimiter(string message)
        {
            int delimiterIndex;

            delimiterIndex = message.IndexOf(ErrorMessage.HeaderBodyDelimiter);
            if (delimiterIndex == -1)
                throw ParsedErrorMessage.ToParsingException(message);

            return delimiterIndex;
        }
    }
}
