
using System;
using System.Runtime.Serialization;

namespace Starcounter {
    
    /// <summary>
    /// Exception raised when a database constraint is violated and a transaction
    /// is aborted because of that.
    /// </summary>
    [Serializable]
    public class ConstraintViolationException : TransactionAbortedException {

        internal ConstraintViolationException(UInt32 errorCode, String message, Exception innerException) : base(errorCode, message, innerException) { }

        protected ConstraintViolationException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context) {
        }
    }
}
