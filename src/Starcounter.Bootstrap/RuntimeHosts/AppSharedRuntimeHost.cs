
using Starcounter.Bootstrap.Management;
using Starcounter.Hosting;
using Starcounter.Internal;
using StarcounterInternal.Hosting;
using System;

namespace Starcounter.Bootstrap.RuntimeHosts
{
    public class AppSharedRuntimeHost : RuntimeHost
    {
        class ExceptionManagerImpl : IExceptionManager
        {
            public ExceptionManagerImpl()
            {
                AppDomain.CurrentDomain.UnhandledException += (sender, e) => {
                    HandleUnhandledException((Exception)e.ExceptionObject);
                };
            }

            /// <summary>
            /// Handles the unhandled exception.
            /// </summary>
            /// <param name="ex">The ex.</param>
            /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
            public bool HandleUnhandledException(Exception ex)
            {
                // We begin formatting and logging the exception just
                // as it arrives, no modifications or analysis at all.

                string message = Starcounter.Logging.ExceptionFormatter.ExceptionToString(ex);
                sccoreapp.sccoreapp_log_critical_message(message);

                if (!Console.IsInputRedirected)
                {
                    Console.Error.WriteLine(message);
                }

                uint e = 0;
                Exception current = ex;
                Exception entrypointException = null;

                // Might be that the real exception is an inner exception wrapped in some 
                // other exception that does not contain the errorcode so we traverse all 
                // exceptions until we find an errorcode or no more innerexceptions exist.

                while (current != null)
                {
                    if (ErrorCode.TryGetCode(current, out e))
                    {
                        if (e == Error.SCERRFAILINGENTRYPOINT)
                        {
                            entrypointException = current;
                            e = 0;
                        }
                        else
                        {
                            break;
                        }
                    }
                    current = current.InnerException;
                }

                if (e == 0)
                { // No errorcode is found.
                    if (entrypointException != null)
                    {
                        current = entrypointException;
                        e = Error.SCERRFAILINGENTRYPOINT;
                    }
                    else
                    {
                        current = ex;
                        e = 1;
                    }
                }

                // Report the stacktrace for any Starcounter-based exception
                // wrapped in an entrypoint exception. For non-Starcounter
                // exceptions in such a case, we have the stack trace as part
                // of the entrypoint exception message. And for all "clean"
                // Starcounter-based exceptions, we never give the stack-trace,
                // since it's pure Starcounter source code.

                bool reportStackTrace = false;
                if (entrypointException != null && !object.ReferenceEquals(current, entrypointException))
                {
                    if (ErrorCode.IsFromErrorCode(current))
                    {
                        reportStackTrace = true;
                    }
                }

                CodeHostError.Report(current, reportStackTrace);

                Kernel32.ExitProcess(e);
                return true;
            }
        }

        public AppSharedRuntimeHost() : base()
        {
            RedirectConsoleOutput = true;
        }

        public override void Run(Func<IHostConfiguration> configProvider, Func<IAppStart> autoStartProvdier = null)
        {
            try
            {
                base.Run(configProvider, autoStartProvdier);
            }
            catch (Exception ex)
            {
                if (!StarcounterInternal.Hosting.ExceptionManager.HandleUnhandledException(ex)) throw;
            }
        }

        protected override ILifetimeService CreateLifetimeService()
        {
            return new ManagementService();
        }

        protected override IExceptionManager CreateExceptionManager()
        {
            return new ExceptionManagerImpl();
        }
    }
}
