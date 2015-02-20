// ***********************************************************************
// <copyright file="UnhandledTransactionConflictException.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Runtime.Serialization;

namespace Starcounter {

    /// <summary>
    /// Exception thrown on failure to restart transaction aborted because of a transaction
    /// conflict at transaction boundary (error code 4090, ScErrUnhandledTransactConflict).
    /// </summary>
    /// <seealso cref="Starcounter.TransactionConflictException"/>
    /// <seealso cref="Starcounter.Db.Transact(System.Action)"/>
    public class UnhandledTransactionConflictException : DbException {

        /// <summary>
        /// For internal use, not to be used in user code (used in generated
        /// transaction scopes).
        /// </summary>
        public UnhandledTransactionConflictException(Exception innerException)
            : base(
                Error.SCERRUNHANDLEDTRANSACTCONFLICT,
                Starcounter.ErrorCode.ToMessage(Error.SCERRUNHANDLEDTRANSACTCONFLICT),
                innerException
                ) { }

        internal UnhandledTransactionConflictException(UInt32 errorCode, String message, Exception innerException) : base(errorCode, message, innerException) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnhandledTransactionConflictException" /> class.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <param name="context">The context.</param>
        protected UnhandledTransactionConflictException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context) {
        }
    }
}
