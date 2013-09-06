// ***********************************************************************
// <copyright file="TransactionConflictException.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Runtime.Serialization;

namespace Starcounter {

    /// <summary>
    /// Exception thrown when a transaction is aborted because of a conflict with another
    /// transaction (error code 8002, ScErrTransactionConflictAbort).
    /// </summary>
    [Serializable]
    public class TransactionConflictException : TransactionAbortedException, ITransactionConflictException {

        internal TransactionConflictException(UInt32 errorCode, String message, Exception innerException) : base(errorCode, message, innerException) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionConflictException" /> class.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <param name="context">The context.</param>
        protected TransactionConflictException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context) {
        }
    }
}
