// ***********************************************************************
// <copyright file="ErrorInfoExtensions.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Internal;
using System;
using System.Collections.Generic;

namespace Starcounter.Server.PublicModel {

    /// <summary>
    /// Expose a set of utility/extension methods making it easier to work
    /// with <see cref="ErrorInfo"/> instances, either in single form and as
    /// collections in the form they are normally produced by the server.
    /// </summary>
    public static class ErrorInfoExtensions {

        /// <summary>
        /// Tries getting a single reason error from a given set of
        /// errors. The error is considered single reason if either
        /// it's the sole entry in the given list, OR if the list
        /// contains exactly two entries and the first (index 0) entry
        /// is based on an error code equal to <paramref name="aggregateErrorCode"/>.
        /// </summary>
        /// <param name="aggregateErrorCode">The aggregate error code
        /// allowed. Pass <see cref="uint.MaxValue"/> to allow any code.
        /// </param>
        /// <param name="errors">The set of errors to inspect.</param>
        /// <param name="info">The <see cref="ErrorInfo"/> returned if
        /// a single reason error was in fact found.</param>
        /// <returns>True if the method could extract a single reason
        /// error from the given set of errors; false otherwise.</returns>
        public static bool TryGetSingleReasonError(
            uint aggregateErrorCode,
            ErrorInfo[] errors,
            out ErrorInfo info) {
            if (errors == null)
                throw new ArgumentNullException("errors");

            if (errors.Length == 1) {
                info = errors[0];
                return true;
            }

            if (errors.Length != 2) {
                info = null;
                return false;
            }

            uint aggregateFound;
            info = errors[1];
            if (errors[0].TryGetErrorCode(out aggregateFound)) {
                return aggregateErrorCode == aggregateFound || aggregateErrorCode == uint.MaxValue;
            }

            return false;
        }

        /// <summary>
        /// Tries to get a single reason code from the given set of errors,
        /// assuming the aggregate error is the one normally used by the server
        /// when executing command processors that fails. Internally calls
        /// <see cref="TryGetSingleReasonError"/> with the commonly used error
        /// code used by the server.
        /// </summary>
        /// <param name="errors">The set of errors to inspect.</param>
        /// <param name="info">The <see cref="ErrorInfo"/> returned if
        /// a single reason error was in fact found.</param>
        /// <returns>True if the method could extract a single reason
        /// error from the given set of errors; false otherwise.</returns>
        public static bool TryGetSingleReasonErrorBasedOnServerConvention(
            ErrorInfo[] errors,
            out ErrorInfo info) {
            return TryGetSingleReasonError(Error.SCERRSERVERCOMMANDFAILED, errors, out info);
        }

        /// <summary>
        /// Tries to get a single reason code from the given set of errors,
        /// allowing any error code as the aggregate error. Internally calls
        /// <see cref="TryGetSingleReasonError"/>.
        /// </summary>
        /// <param name="errors">The set of errors to inspect.</param>
        /// <param name="info">The <see cref="ErrorInfo"/> returned if
        /// a single reason error was in fact found.</param>
        /// <returns>True if the method could extract a single reason
        /// error from the given set of errors; false otherwise.</returns>
        public static bool TryGetSingleReasonErrorAggregatedInAny(
            ErrorInfo[] errors,
            out ErrorInfo info) {
            return TryGetSingleReasonError(uint.MaxValue, errors, out info);
        }

        /// <summary>
        /// Gets the error code behind the <see cref="ErrorInfo"/> if there
        /// is one, and <see cref="Error.SCERRUNSPECIFIED"/> if not.
        /// </summary>
        /// <param name="info">The <see cref="ErrorInfo"/> whos code to
        /// return.</param>
        /// <returns>The error code as specified in the summary.</returns>
        public static uint GetErrorCode(this ErrorInfo info) {
            uint code;
            if (!info.TryGetErrorCode(out code)) {
                code = Error.SCERRUNSPECIFIED;
            }
            return code;
        }
    }
}