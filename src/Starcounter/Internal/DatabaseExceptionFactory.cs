// ***********************************************************************
// <copyright file="DatabaseExceptionFactory.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using Starcounter.Hosting;

namespace Starcounter.Internal {

    /// <summary>
    /// The database specific exception factory to be installed inside the
    /// database worker process.
    /// </summary>
    public sealed class DatabaseExceptionFactory : ExceptionFactory {
        /// <summary>
        /// Installs the in current app domain.
        /// </summary>
        public static void InstallInCurrentAppDomain() {
            if (ErrorCode.ExceptionFactory is DatabaseExceptionFactory)
                return;

            ErrorCode.SetExceptionFactory(new DatabaseExceptionFactory());
        }

        /// <summary>
        /// Prevents a default instance of the <see cref="DatabaseExceptionFactory" /> class from being created.
        /// </summary>
        private DatabaseExceptionFactory() {
        }

        /// <inheritdoc />
        public override Exception CreateException(
            uint errorCode,
            Exception innerException,
            string messagePostfix,
            Func<uint, string, object[], string> messageFactory,
            params object[] messageArguments) {
            Exception exception;
            uint facilityCode;
            string message;

            exception = null;

            // Handle if the error is a database error.

            facilityCode = ErrorCode.ToFacilityCode(errorCode);
            if (facilityCode == 0x0004) {
                message = messageFactory(errorCode, messagePostfix, messageArguments);
                switch (errorCode) {
                    case Error.SCERRERRORINHOOKCALLBACK:
                        if ((innerException as ITransactionConflictException) != null) {
                            exception = new TransactionConflictDbException(errorCode, message, innerException);
                            break;
                        }
                        exception = new DbException(errorCode, message, innerException);
                        break;
                    case Error.SCERROBJECTDOESNTEXIST:
                        exception = new ObjectDoesntExistException(errorCode, message, innerException);
                        break;
                    case Error.SCERRUNHANDLEDTRANSACTCONFLICT:
                        exception = new UnhandledTransactionConflictException(errorCode, message, innerException);
                        break;
                    case Error.SCERRCLIENTBACKENDNOTINITIALIZED:
                        // NOTE:
                        // I straddle whether this error should be given it's own custom
                        // exception or not (specializing DbException). For now, we'll use
                        // an invalid operation exception, but this might be target for
                        // revision.
                        exception = new InvalidOperationException(message, innerException);
                        break;
                    case Error.SCERRASSEMBLYSPECNOTFOUND:
                    case Error.SCERRBACKINGRETREIVALFAILED:
                    case Error.SCERRBACKINGDBINDEXTYPENOTFOUND:
                        exception = new BackingException(errorCode, message, innerException);
                        break;
                    case Error.SCERRCANTRUNSHAREDAPPHOST:
                        exception = new NotSupportedException(message, innerException);
                        break;
                    default:
                        exception = new DbException(errorCode, message, innerException);
                        break;
                }
            } else if (facilityCode == 0x0008) {
                message = messageFactory(errorCode, messagePostfix, messageArguments);
                switch (errorCode) {
                    case Error.SCERRTRANSACTIONCONFLICTABORT:
                        exception = new TransactionConflictException(errorCode, message, innerException);
                        break;
                    case Error.SCERRCONSTRAINTVIOLATIONABORT:
                        exception = new ConstraintViolationException(errorCode, message, innerException);
                        break;
                    default:
                        exception = new TransactionAbortedException(errorCode, message, innerException);
                        break;
                }
            } else if (facilityCode == 7) {
#if true
                switch (errorCode) {
                    case Error.SCERRSQLVERIFYPROCESSFAILED:
                        message = messageFactory(errorCode, messagePostfix, messageArguments);
                        exception = new Starcounter.Query.Sql.SqlExecutableException(message, innerException);
                        break;
                    default:
                        break;
                }
#endif
            } else if (facilityCode == 0x0001) {
                message = messageFactory(errorCode, messagePostfix, messageArguments);
                switch (errorCode) {
                    case Error.SCERRCODENOTENHANCED:
                        // Previously, we had a strongly typed exception for this, but
                        // I can't really see we can handle it runtime, so we'll just
                        // stick with the standard DbException until (and if) we find
                        // it unsufficient.
                        exception = new DbException(errorCode, message, innerException);
                        break;
                    default:
                        break;
                }
            } else if (facilityCode == 0x000A) {
                // All managment errors occuring inside the database (or in a client
                // using the Starcounter.dll assembly) we currently map to DbException.
                // Possibly, management errors will have more specific exception types
                // in the future if found needed.

                message = messageFactory(errorCode, messagePostfix, messageArguments);
                exception = new DbException(errorCode, message, innerException);
            }

            if (exception != null) {
                // We have decided on an exception here. Decorate it further and then
                // return the result.

                return DecorateException(exception, errorCode);
            }

            // Fall back on the default error translation routine that handles
            // errors not categorized as database errors.

            return base.CreateException(errorCode, innerException, messagePostfix, messageFactory, messageArguments);
        }
    }
}