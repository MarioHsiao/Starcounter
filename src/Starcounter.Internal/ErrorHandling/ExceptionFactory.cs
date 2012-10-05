
using System;
using System.IO;
using System.Security;
using Starcounter.CommandLine;

namespace Starcounter.Internal
{

    /// <summary>
    /// Top-level exception factory class, shared by all tools and components to
    /// create exceptions from error codes.
    /// </summary>
    public class ExceptionFactory
    {
        /// <summary>
        /// The customizable factory method responsible for creating an
        /// <see cref="Exception"/> from given error parameters.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Custom factory methods that override the default is strongly
        /// advice to use the supplied <paramref name="messageFactory"/>
        /// to create the error message, before actually creating the
        /// <see cref="Exception"/> they return.
        /// </para>
        /// <para>
        /// If a custom factory method does not support the creation of
        /// an exception given the specified <paramref name="errorCode"/>,
        /// it is expected to forward the call to the base implementation,
        /// using <c>base.CreateException</c>.
        /// </para>
        /// </remarks>
        /// <param name="errorCode">The error code.</param>
        /// <param name="innerException">
        /// An possible inner exception.
        /// </param>
        /// <param name="messagePostfix">
        /// An possible message postfix.
        /// </param>
        /// <param name="messageFactory">
        /// The default message factory, normally used to translate the
        /// <paramref name="errorCode"/>, <paramref name="messagePostfix"/>
        /// and the <paramref name="messageArguments"/> to an error message
        /// whose string is used when creating the <see cref="Exception"/>.
        /// </param>
        /// <param name="messageArguments">
        /// Possible message arguments.
        /// </param>
        /// <returns>
        /// An <see cref="Exception"/> whose type best represent the error
        /// specified by <paramref name="errorCode"/>.
        /// </returns>
        public virtual Exception CreateException(
            uint errorCode,
            Exception innerException,
            string messagePostfix,
            Func<uint, string, object[], string> messageFactory,
            params object[] messageArguments
            )
        {
            string msg;
            uint facilityCode;
            Exception ex;

            // Not to be used when tranlating errors originating from the
            // database kernel since the exceptions set up to handle database
            // errors doesn't exist in this assembly.

            // Format the message, according to the given input

            msg = messageFactory(errorCode, messagePostfix, messageArguments);
            ex = null;

            // Create appropriate exception

            facilityCode = ErrorCode.ToFacilityCode(errorCode);
            switch (facilityCode)
            {
                case 0x0001:
                    switch (errorCode)
                    {
                        case Error.SCERRBADARGUMENTS:
                            ex = new ArgumentException(msg, innerException);
                            break;
                        case Error.SCERROUTOFMEMORY:
                            ex = new OutOfMemoryException(msg, innerException);
                            break;
                        case Error.SCERRNOTSUPPORTED:
                            ex = new NotSupportedException(msg, innerException);
                            break;
                        case Error.SCERRNOTIMPLEMENTED:
                            ex = new NotImplementedException(msg, innerException);
                            break;
                        case Error.SCERRINVALIDOPERATION:
                            ex = new InvalidOperationException(msg, innerException);
                            break;
                        case Error.SCERRENVVARIABLENOTACCESSIBLE:
                            ex = new SecurityException(msg, innerException);
                            break;
                        case Error.SCERRBINDIRENVNOTFOUND:
                            ex = new InvalidOperationException(msg, innerException);
                            break;
                        case Error.SCERRBADCOMMANDLINESYNTAX:
                        case Error.SCERRBADCOMMANDLINEFORMAT:
                            ex = new InvalidCommandLineException(msg, innerException) { ErrorCode = errorCode };
                            break;
                        case Error.SCERRWRONGERRORMESSAGEFORMAT:
                            ex = new FormatException(msg, innerException);
                            break;
                        case Error.SCERROPERATIONCANCELLED:
                            ex = new OperationCanceledException(msg, innerException);
                            break;
                        default:
                            ex = new Exception(msg, innerException);
                            break;
                    }
                    break;
                case 0x0003:
                    ex = new IOException(msg, innerException);
                    break;
                default:
                    ex = new Exception(msg, innerException);
                    break;
            }

            return DecorateException(ex, errorCode);
        }

        protected Exception DecorateException(Exception exception, uint errorCode)
        {
            return ErrorCode.DecorateException(exception, errorCode);
        }
    }
}
