// ***********************************************************************
// <copyright file="ErrorInfoException.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Server.PublicModel;
using System;

namespace Starcounter.Server.Commands {
    
    /// <summary>
    /// Exception wrapping an array of <see cref="ErrorInfo"/>.
    /// </summary>
    /// <remarks>
    /// This exception is typically thrown by a concrete <see cref="CommandProcessor"/>
    /// as a convenient way to throw an exception stemming from <see cref="ErrorInfo"/>.
    /// This exception is then typically immediately handled and the array of <see cref="ErrorInfo"/>
    /// is unwrapped. Therefore, this exception never gets into user code and its message is never displayed.
    /// </remarks>
    internal sealed class ErrorInfoException : Exception {
        private readonly ErrorInfo[] errorInfo;

        /// <summary>
        /// Initializes a new <see cref="ErrorInfoException"/>.
        /// </summary>
        /// <param name="errorInfo">The array of <see cref="ErrorInfo"/> to wrap.</param>
        public ErrorInfoException(ErrorInfo[] errorInfo)
            : base("") {
            this.errorInfo = errorInfo;
        }

        /// <summary>
        /// Initializes a new <see cref="ErrorInfoException"/>.
        /// </summary>
        /// <param name="errorInfo">A single error info that will be stored in
        /// the <see cref="ErrorInfo"/> array wrapped.</param>
        public ErrorInfoException(ErrorInfo errorInfo)
            : base("") {
            this.errorInfo = new ErrorInfo[] { errorInfo };
        }

        /// <summary>
        /// Gets the wrapped array of <see cref="ErrorInfo"/>.
        /// </summary>
        /// <returns>The wrapped array of <see cref="ErrorInfo"/></returns>
        public ErrorInfo[] GetErrorInfo() {
            return this.errorInfo;
        }
    }
}