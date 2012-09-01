
using System;

namespace Starcounter
{

    public class ErrorCode
    {

        public static System.Exception ToException(uint e)
        {
            return new System.Exception(e.ToString());
        }

        public static System.Exception ToException(uint e, string message)
        {
            return new System.Exception(e.ToString());
        }

        public static System.Exception ToException(uint e, System.Exception ex, string message)
        {
            return new System.Exception(e.ToString());
        }

        public static System.Exception ToException(
            uint e,
            System.Exception innerException,
            string messagePostfix,
            params object[] messageArguments
            )
        {
            return new System.Exception(e.ToString());
        }

        public static Exception ToException(uint e, Func<string, Exception, Exception> customFactory)
        {
            return customFactory(e.ToString(), new Exception(e.ToString()));
        }

        public static bool TryGetOrigMessage(System.Exception exc, out string excMessage)
        {
            excMessage = null;
            return false;
        }

        public static bool TryGetCode(System.Exception exc, out uint errorCode)
        {
            errorCode = 0;
            return false;
        }
    }
}
