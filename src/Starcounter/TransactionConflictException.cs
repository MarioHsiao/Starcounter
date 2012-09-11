
using System;
using System.Runtime.Serialization;

namespace Starcounter {
    [Serializable]
    public class TransactionConflictException : TransactionAbortedException, ITransactionConflictException {

        internal TransactionConflictException(UInt32 errorCode, String message, Exception innerException) : base(errorCode, message, innerException) { }

        protected TransactionConflictException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context) {
        }
    }
}
