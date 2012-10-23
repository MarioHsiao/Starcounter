// ***********************************************************************
// <copyright file="TransactionConflictDbException.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Runtime.Serialization;

namespace Starcounter {
    /// <summary>
    /// Variant of <see cref="DbException" /> thrown when it wraps a
    /// <see cref="ITransactionConflictException" />. Used to make sure that
    /// automatic restarts and such are triggered because of conflicts detected
    /// in hooks.
    /// </summary>
    [Serializable]
    public class TransactionConflictDbException : DbException, ITransactionConflictException {
        internal TransactionConflictDbException(UInt32 errorCode, String message, Exception innerException) : base(errorCode, message, innerException) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionConflictDbException" /> class.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <param name="context">The context.</param>
        protected TransactionConflictDbException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context) {
        }
    }
}