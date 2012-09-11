
using System;
using System.Runtime.Serialization;

namespace Starcounter {
    /// <summary>
    /// Exception thrown when attempting to access or modify an object that was
    /// deleted before the current transaction was started (a transferred
    /// object).
    /// </summary>
    [Serializable]
    public class ObjectDoesntExistException : DbException {

        internal ObjectDoesntExistException(UInt32 errorCode, String message, Exception innerException) : base(errorCode, message, innerException) { }

        protected ObjectDoesntExistException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context) {
        }
    }
}
