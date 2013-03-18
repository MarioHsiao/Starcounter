#include "static_headers.hpp"
#include "utilities.hpp"

namespace starcounter {
namespace network {

// Reads decimal from the given string.
// (reads until characters are digits)
/*uint64_t ReadDecimal(const char *start)
{
    if (!isdigit(*start))
        return -1;

    const char *origStart = start;
    while (isdigit(*start))
        start++;

    uint64_t degree = 1, res = 0;
    start--;
    while(1)
    {
        res += ((*start) - '0') * degree;
        degree *= 10;

        if (start != origStart)
            start--;
        else
            break;
    }

    return res;
}*/

// Print error code.
uint32_t PrintLastError(bool report_to_log)
{
    const int32_t max_err_msg_len = 512;

    // Retrieve the system error message for the last-error code.
    TCHAR *err_buf;
    uint32_t dw = GetLastError();

    // Getting system error message.
    FormatMessage(
        FORMAT_MESSAGE_ALLOCATE_BUFFER |
        FORMAT_MESSAGE_FROM_SYSTEM |
        FORMAT_MESSAGE_IGNORE_INSERTS,
        NULL,
        dw,
        MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
        (LPTSTR) &err_buf,
        0,
        NULL);

    // Copying the error message.
    TCHAR err_display_buf[max_err_msg_len];
    StringCchPrintf(err_display_buf, max_err_msg_len, TEXT("Error %d: %s"), dw, err_buf); 

    // Converting wchar_t to char.
    char err_display_buf_char[max_err_msg_len];
    wcstombs(err_display_buf_char, err_display_buf, max_err_msg_len);

    // Printing error to console/log.
    GW_COUT << err_display_buf_char << GW_ENDL;

    // Printing error to server log.
    if (report_to_log)
        GW_LOG_ERROR << err_display_buf_char << GW_WENDL;

    // Free message resources.
    LocalFree(err_buf);

    return dw;
}

} // namespace network
} // namespace starcounter
