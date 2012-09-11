
using System;
using System.Runtime.Serialization;

namespace Starcounter {
    
    /// <summary>
    /// Exception raised when a transaction is aborted.
    /// </summary>
    [Serializable]
    public class TransactionAbortedException : DbException {

        internal TransactionAbortedException(UInt32 errorCode, String message, Exception innerException) : base(errorCode, message, innerException) { }

        protected TransactionAbortedException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context) {
        }
    }
}