// ***********************************************************************
// <copyright file="ObjectDoesntExistException.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

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

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectDoesntExistException" /> class.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <param name="context">The context.</param>
        protected ObjectDoesntExistException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context) {
        }
    }
}
