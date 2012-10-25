// ***********************************************************************
// <copyright file="InvalidCommandLineException.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Runtime.Serialization;

namespace Starcounter.CommandLine
{
    /// <summary>
    /// Exception thrown when a set of command line arguments doesn't match
    /// the valid syntax or semantics of a program definition.
    /// </summary>
    [Serializable]
    public class InvalidCommandLineException : Exception
    {
        /// <summary>
        /// Gets the error code.
        /// </summary>
        /// <value>The error code.</value>
        public uint ErrorCode { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidCommandLineException" /> class.
        /// </summary>
        internal InvalidCommandLineException() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidCommandLineException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        internal InvalidCommandLineException(string message) : base(message) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidCommandLineException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="innerException">The inner exception.</param>
        internal InvalidCommandLineException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidCommandLineException" /> class.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <param name="context">The context.</param>
        protected InvalidCommandLineException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}