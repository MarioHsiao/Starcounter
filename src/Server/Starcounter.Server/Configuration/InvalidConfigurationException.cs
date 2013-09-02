// ***********************************************************************
// <copyright file="InvalidConfigurationException.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Starcounter.Advanced.Configuration {
    /// <summary>
    /// Exception thrown upon invalid configuration.
    /// </summary>
    [Serializable]
    public class InvalidConfigurationException : ApplicationException {
        /// <summary>
        /// Initializes a new <see cref="InvalidConfigurationException"/>.
        /// </summary>
        public InvalidConfigurationException() {
        }

        /// <summary>
        /// Initializes a new <see cref="InvalidConfigurationException"/> and specifies the message.
        /// </summary>
        /// <param name="message">Message.</param>
        public InvalidConfigurationException(string message)
            : base(message) {
        }

        /// <summary>
        /// Initializes a new <see cref="InvalidConfigurationException"/> and specifies the message
        /// and inner <see cref="Exception"/>.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <param name="inner">Inner exception.</param>
        public InvalidConfigurationException(string message, Exception inner)
            : base(message, inner) {
        }

        /// <summary>
        /// Initializes a new <see cref="InvalidConfigurationException"/> and formats the exception message
        /// from a list of error messages.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="errors">List of error messages.</param>
        public InvalidConfigurationException(string message, ICollection<string> errors) :
            base(FormatMessage(message, errors)) {
        }

        private static string FormatMessage(string message, ICollection<string> errors) {
            if (errors == null || errors.Count == 0) {
                return message;
            }
            StringBuilder s = new StringBuilder();
            s.Append(message);
            s.Append(':');
            s.Append(Environment.NewLine);
            foreach (string error in errors) {
                s.Append("  ");
                s.Append(error);
                s.Append(Environment.NewLine);
            }
            return s.ToString();
        }

        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        /// <param name="info">Streaming info.</param>
        /// <param name="context">Context.</param>
        protected InvalidConfigurationException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context) {
        }
    }
}