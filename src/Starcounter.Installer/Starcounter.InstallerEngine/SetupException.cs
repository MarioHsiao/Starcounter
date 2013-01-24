using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration.Install;
using System.Xml;
using System.IO;
using Starcounter;
using Starcounter.Internal;

namespace Starcounter.InstallerEngine
{
    /// <summary>
    /// Indicates that installation has been aborted either by user or some other reason.
    /// Not considered as an error.
    /// </summary>
    public class InstallerAbortedException : Exception
    {
        /// <summary>
        /// Constructor that copies the message.
        /// </summary>
        public InstallerAbortedException(String message)
            : base(message)
        { }
    }

    /// <summary>
    /// Creating exception factory specially for handling installer internal exceptions.
    /// </summary>
    internal sealed class InstallerExceptionFactory : ExceptionFactory
    {
        internal static void InstallInCurrentAppDomain()
        {
            if (ErrorCode.ExceptionFactory is InstallerExceptionFactory)
                return;

            ErrorCode.SetExceptionFactory(new InstallerExceptionFactory());
        }

        private InstallerExceptionFactory()
        { }

        /// <inheritdoc />
        public override Exception CreateException(
            uint errorCode, 
            Exception innerException, 
            string messagePostfix, 
            Func<uint, string, object[], string> messageFactory, 
            params object[] messageArguments)
        {
            Exception exception;
            uint facility;
            string message;

            exception = null;

            // Handle if the error is an installer error.
            facility = ErrorCode.ToFacilityCode(errorCode);
            if (facility == 0x000B)
            {
                message = messageFactory(errorCode, messagePostfix, messageArguments);
                switch (errorCode)
                {
                    case Starcounter.Error.SCERRINSTALLERABORTED:
                        exception = new InstallerAbortedException(message);
                        break;

                    case Starcounter.Error.SCERRVSIXENGINENOTFOUND:
                    case Starcounter.Error.SCERRVSIXPACKAGENOTFOUND:
                        exception = new FileNotFoundException(message);
                        break;
                }
            }

            if (exception != null)
            {
                // We have decided on an exception here. Decorate it further and then
                // return the result.

                return DecorateException(exception, errorCode);
            }

            // Fall back on the default error translation routine that handles
            // errors not categorized as installer errors.

            return base.CreateException(errorCode, innerException, messagePostfix, messageFactory, messageArguments);
        }
    }
}
