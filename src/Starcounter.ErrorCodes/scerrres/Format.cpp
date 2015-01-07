
#include <wchar.h>
#include <string.h>
#include <stdio.h>
#include <stdarg.h>
#include "scerrres.h"
#include "format.h"

#include "scerrres.c"

size_t StarcounterErrorMessageFormatWithArgs(long ec, wchar_t* buf, size_t max, ...) {
  static const wchar_t* empty_arg = L"(NULL)";
  wchar_t* buf_end;
  wchar_t* p;
  const wchar_t* arg[10];
  va_list argptr;
  const char* msg = StarcounterErrorMessageTemplate(ec);

  va_start(argptr, max);
  if (!buf) max = 0;
  buf_end = buf + max;
  p = buf;

  if (msg) {
    for (int i = 0; i < 10; ++i)
      arg[i] = NULL;
    while (*msg) {
      if (msg[0] == '{' && isdigit(msg[1]) && msg[2] == '}') {
        int arg_num = msg[1] - '0';
        for (int i = 0; i <= arg_num; ++i) {
          if (!arg[i]) {
            arg[i] = va_arg(argptr, const wchar_t*);
            if (!arg[i]) {
              for (;i < 10; ++i)
                arg[i] = empty_arg;
			}
          }
        }
        for (const wchar_t* s = arg[arg_num]; s && *s; ++s) {
          if (p < buf_end) *p = *s;
          ++p;
        }
        ++msg;
        ++msg;
      } else {
        if (p < buf_end) *p = (wchar_t)(*msg);
        ++p;
      }
      ++msg;
    }
  } else {
    p = buf + swprintf(buf, max, L"(SCERR%ld).", ec);
  }

  if (p < buf_end) *p = L'\0';
  else if (buf < buf_end) buf_end[-1] = L'\0';
  va_end(argptr);
  return p - buf;
}

VOID __stdcall FormatStarcounterErrorMessage(
    DWORD errorCode,
    LPWSTR outputBuffer,
    DWORD outputBufferLength
)
{
	StarcounterErrorMessageFormatWithArgs(errorCode, outputBuffer, outputBufferLength, NULL);
#if 0
    DWORD dr;
    int ir;

    // Since the length is the total length including the terminator character
    // we first allocate room for that. No matter if we run out of buffer space
    // or not we will now always have room for the terminator (assumes that the
    // legth of the buffer is at least 1).
    //  _ASSERT(length != 0);

    outputBufferLength--;
    dr = 0;

    // Load error message from the resource library into the buffer. If no
    // error message was found for the error code, or the buffer wasn't large
    // enough, we include a generic message instead.

    dr = FormatMessageW(
        FORMAT_MESSAGE_FROM_HMODULE,
        GetModuleHandleW(L"scerrres.dll"),
        errorCode,
        0,
        outputBuffer,
        outputBufferLength,
        NULL
    );
    if (dr == 0 && outputBufferLength >= 23)
    {
        ir = swprintf_s(outputBuffer, outputBufferLength, L"Error code=0x%X.", errorCode);
        dr = (DWORD)ir;
    }

    // Add a terminator and we are done

    outputBuffer += dr;
    *outputBuffer = 0;
#endif
}

