
namespace Starcounter.Server.Windows {
    /// <summary>
    /// Expose a set of Windows platform error constants.
    /// </summary>
    public enum Win32Error {
        ERROR_SUCCESS = 0,
        ERROR_FILE_NOT_FOUND = 2,
        ERROR_ACCESS_DENIED = 5,
        ERROR_INSUFFICIENT_BUFFER = 122,
        ERROR_NO_MORE_ITEMS = 259,
        ERROR_NONE_MAPPED = 1332
    }
}