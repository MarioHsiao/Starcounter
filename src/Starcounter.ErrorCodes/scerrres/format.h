
#include <stdlib.h>
#include <stdio.h>
#include <wchar.h>
#include <windows.h>

#ifdef __cplusplus
extern "C" {
#endif
const char* StarcounterErrorMessageTemplate(long ec);
size_t StarcounterErrorMessageFormatWithArgs(long ec, wchar_t* buf, size_t max, ...);
#ifdef __cplusplus
}
#endif

VOID __stdcall FormatStarcounterErrorMessage(
    DWORD errorCode,
    LPWSTR outputBuffer,
    DWORD outputBufferLength
);
