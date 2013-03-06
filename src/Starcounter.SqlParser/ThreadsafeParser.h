#pragma once
#include "../../Starcounter.ErrorCodes/scerrres/scerrres.h"
#include <stdarg.h>
#include <math.h>

#define DEBUG_VERBOSE (0)

#define Thread __declspec( thread )

extern void ThrowException(char *msg);
extern void ThrowExceptionCode(int scerrcode, char *msg);
extern void ThrowExceptionReport(int scerrorcode, int position, wchar_t *tocken, char *message);

typedef struct ScError
{
	int scerrorcode;
	char *scerrmessage;
	int scerrposition;
	wchar_t *tocken;
} ScError;

//static Thread ScError scerror = {0, NULL, -1};
extern Thread ScError *scerror;
extern void InitScError();
void ResetScError();
extern void ScEreport(int scerrorcode, int position, wchar_t *tocken, char *message);
extern void ScCodeEreport(int scerrorcode);

extern char *ScErrMessage(const char *fmt);
extern char *ScErrMessageStr(const char *fmt, const char *msg);
extern char *ScErrMessageInt(const char *fmt, int val);
extern char *ScErrMessageLU(const char *fmt, long unsigned val);
extern char *ScErrMessagePointer(const char *fmt, void *ptr);
extern char *ScErrMessageStrPointer(const char *fmt, const char *msg, void *ptr);
extern char *ScErrMessageStrs(int nrStrs, const char *fmt, ...);

//extern void myprint(char *fmt, ...);
#if(DEBUG_VERBOSE)
#define errprint \
	printf
#else
#define errprint
#endif
