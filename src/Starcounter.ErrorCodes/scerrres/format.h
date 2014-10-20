
#include <stdlib.h>
#include <stdio.h>

#ifdef WIN32
#include <windows.h>
VOID __stdcall FormatStarcounterErrorMessage(
	DWORD errorCode,
	LPWSTR outputBuffer,
	DWORD outputBufferLength
);
#else
#ifdef __cplusplus
extern "C" {
#endif
void FormatStarcounterErrorMessage(
	unsigned errorCode,
	char* outputBuffer,
	size_t outputBufferLength
);
#ifdef __cplusplus
}
#endif
#endif

