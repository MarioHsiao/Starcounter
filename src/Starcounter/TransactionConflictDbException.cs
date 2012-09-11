
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

        protected TransactionConflictDbException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context) {
        }
    }
}