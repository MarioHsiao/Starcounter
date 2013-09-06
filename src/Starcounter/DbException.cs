// ***********************************************************************
// <copyright file="DbException.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Runtime.Serialization;

namespace Starcounter {
    
    /// <summary>
    /// Exception thrown on when a database error is detected (4000 or 8000 series error codes).
    /// </summary>
    /// <remarks>
    /// On some errors a specialized exception inheriting <see cref="Starcounter.DbException"/> is
    /// raised.
    /// </remarks>
    [Serializable]
    public class DbException : Exception {
        private readonly UInt32 _errorCode;

        internal DbException(UInt32 errorCode, String message)
            : base(message, null) {
            _errorCode = errorCode;
            Data[Starcounter.ErrorCode.EC_TRANSPORT_KEY] = errorCode;
        }

        internal DbException(UInt32 errorCode, String message, Exception innerException)
            : base(message, innerException) {
            _errorCode = errorCode;
            Data[Starcounter.ErrorCode.EC_TRANSPORT_KEY] = errorCode;
        }

        /// <summary>
        /// Starcounter error code.
        /// </summary>
        public UInt32 ErrorCode {
            get {
                return _errorCode;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected DbException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context) {
            _errorCode = info.GetUInt32("_errorCode");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context) {
            base.GetObjectData(info, context);
            info.AddValue("_errorCode", _errorCode);
        }
    }
}