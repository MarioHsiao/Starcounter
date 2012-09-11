
using System;
using System.Runtime.Serialization;

namespace Starcounter {
    
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

        protected UnhandledTransactionConflictException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context) {
        }
    }
}
