
namespace Starcounter.Server.Windows {
    /// <summary>
    /// Expose a set of Windows platform error constants.
    /// </summary>
    public static class Win32Error {
        public const int ERROR_SUCCESS = 0;
        public const int ERROR_FILE_NOT_FOUND = 2;
        public const int ERROR_ACCESS_DENIED = 5;
        public const int ERROR_INSUFFICIENT_BUFFER = 122;
        public const int ERROR_NO_MORE_ITEMS = 259;
        public const int ERROR_SERVICE_DOES_NOT_EXIST = 1060;
        public const int ERROR_SERVICE_MARKED_FOR_DELETE = 1072;
        public const int ERROR_NONE_MAPPED = 1332;
    }
}