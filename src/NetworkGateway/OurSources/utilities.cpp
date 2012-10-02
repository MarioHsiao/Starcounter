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
uint32_t PrintLastError()
{
    // Retrieve the system error message for the last-error code.
    TCHAR *msgBuf;
    TCHAR *displayBuf;
    uint32_t dw = GetLastError();

    FormatMessage(
        FORMAT_MESSAGE_ALLOCATE_BUFFER |
        FORMAT_MESSAGE_FROM_SYSTEM |
        FORMAT_MESSAGE_IGNORE_INSERTS,
        NULL,
        dw,
        MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
        (LPTSTR) &msgBuf,
        0, NULL );

    // Display the error message and exit the process.
    displayBuf = (TCHAR *)LocalAlloc(LMEM_ZEROINIT, (lstrlen(msgBuf) + 50) * sizeof(TCHAR)); 

    // Copying the error message.
    StringCchPrintf(displayBuf, LocalSize(displayBuf) / sizeof(TCHAR), TEXT("Error %d: %s"), dw, msgBuf); 

    // Printing the error to console.
    tcout << displayBuf << std::endl;

    // Free message resources.
    LocalFree(msgBuf);
    LocalFree(displayBuf);

    return dw;
}

} // namespace network
} // namespace starcounter
