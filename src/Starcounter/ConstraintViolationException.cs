// ***********************************************************************
// <copyright file="ConstraintViolationException.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Runtime.Serialization;

namespace Starcounter {
    
    /// <summary>
    /// Exception thrown when a transaction is aborted because of a constraint violation (error
    /// code 8001, ScErrConstraintViolationAbort).
    /// </summary>
    [Serializable]
    public class ConstraintViolationException : TransactionAbortedException {

        internal ConstraintViolationException(UInt32 errorCode, String message, Exception innerException) : base(errorCode, message, innerException) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConstraintViolationException" /> class.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <param name="context">The context.</param>
        protected ConstraintViolationException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context) {
        }
    }
}
