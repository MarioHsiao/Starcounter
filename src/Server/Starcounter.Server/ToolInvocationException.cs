// ***********************************************************************
// <copyright file="ToolInvocationException.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;

namespace Starcounter.Server {
    /// <summary>
    /// Exception thrown by <see cref="ToolInvocationHelper"/> when an external process
    /// dot not complete successfully.
    /// </summary>
    public class ToolInvocationException : Exception {
        /// <summary>
        /// Initializes a new <see cref="ToolInvocationException"/>
        /// and builds the error message from a <see cref="ToolInvocationResult"/>.
        /// </summary>
        /// <param name="result"></param>
        internal ToolInvocationException(ToolInvocationResult result)
            : this(string.Format("Error invoking \"\"{0}\" {1}\": exit code is 0x{2:x}. Tool output:\n\n {3}",
                                 result.FileName, result.Arguments, result.ExitCode, string.Join(Environment.NewLine, result.GetOutput()))) {
            this.ExitCode = result.ExitCode;
            this.Result = result;
        }

        /// <summary>
        /// Initializes a new <see cref="ToolInvocationException"/>.
        /// </summary>
        public ToolInvocationException() {
        }

        /// <summary>
        /// Initializes a new <see cref="ToolInvocationException"/> and specifies the
        /// exception message.
        /// </summary>
        /// <param name="message">Exception message.</param>
        public ToolInvocationException(string message)
            : base(message) {
        }

        /// <summary>
        /// Initializes a new <see cref="ToolInvocationException"/> and specifies
        /// the exception message and an inner <see cref="Exception"/>.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="inner">The inner <see cref="Exception"/>.</param>
        public ToolInvocationException(string message, Exception inner)
            : base(message, inner) {
        }

        /// <summary>
        /// Gets the process exit code.
        /// </summary>
        public int ExitCode {
            get;
            private set;
        }

        /// <summary>
        /// Gets the <see cref="ToolInvocationResult"/> for exceptions
        /// that stem from such.
        /// </summary>
        internal ToolInvocationResult Result {
            get;
            private set;
        }
    }
}