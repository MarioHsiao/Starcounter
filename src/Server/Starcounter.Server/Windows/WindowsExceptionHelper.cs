
using System;
using System.ComponentModel;

namespace Starcounter.Server.Windows {
    /// <summary>
    /// 
    /// </summary>
    public static class WindowsExceptionHelper {
        public static bool IsOriginatingFromNativeError(Exception exception, int windowsErrorCode) {
            int? result = TryGetNativeErrorCode(exception);
            return result.HasValue && result.Value == windowsErrorCode;
        }

        static int? TryGetNativeErrorCode(Exception exception, bool recursive = true) {
            if (exception == null) throw new ArgumentNullException("exception");

            var windowsException = exception as Win32Exception;
            for (; ; ) {
                if (windowsException == null) {
                    if (recursive == false)
                        break;

                    exception = exception.InnerException;
                    if (exception == null)
                        break;
                }
            }

            if (windowsException == null) return null;
            return windowsException.NativeErrorCode;
        }
    }
}
