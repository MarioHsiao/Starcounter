// ***********************************************************************
// <copyright file="TransactionAbortedException.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Runtime.Serialization;

namespace Starcounter {

    /// <summary>
    /// Exception thrown on when a transaction is aborted (8000 series error codes).
    /// </summary>
    [Serializable]
    public class TransactionAbortedException : DbException {

        internal TransactionAbortedException(UInt32 errorCode, String message, Exception innerException) : base(errorCode, message, innerException) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionAbortedException" /> class.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <param name="context">The context.</param>
        protected TransactionAbortedException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context) {
        }
    }
}