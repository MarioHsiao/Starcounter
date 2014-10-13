
#include <stdlib.h>
#include <stdio.h>
#ifdef WIN32
# include <windows.h>
#endif

#ifdef __cplusplus
extern "C" {
#endif

#ifdef WIN32
VOID __stdcall FormatStarcounterErrorMessage(
    DWORD errorCode,
    LPWSTR outputBuffer,
    DWORD outputBufferLength
);
#else
void FormatStarcounterErrorMessage(
    unsigned errorCode,
    char* outputBuffer,
    size_t outputBufferLength
);
#endif

#ifdef __cplusplus
}
#endif
