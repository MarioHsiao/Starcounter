extern "C"
{
	#include "General/postgres.h"
	#include "Parser/keywords.h"
};

#ifdef _MSC_VER
# pragma warning(push)
#pragma warning(disable: 4996)
#endif // _MSC_VER

Thread ScError *scerror = NULL;

void ResetScError()
{
	scerror->scerrorcode = 0;
	scerror->scerrmessage = NULL;
	scerror->scerrposition = -1;
	scerror->tocken = NULL;
	scerror->isKeyword = false;
}

void InitScError()
{
	if (scerror == NULL)
		scerror = (ScError*)palloc(sizeof(ScError));
	ResetScError();
}

///<summary>Reporting errors into thread global variable, which is used to propogate error in managed code.
///</summary>
void ScEreport(int scerrorcode, int position, wchar_t *token, char *message)
{
	if (scerror->scerrorcode==SCERRUNEXPERRSPRINTFSQLSYNTAX)
		return;
	if (scerror->scerrorcode == 0)
		scerror->scerrorcode = scerrorcode;
	scerror->scerrposition = position / sizeof(wchar_t);
	scerror->tocken = token;
	scerror->scerrmessage = message;
	const ScanKeyword *keyword = NULL;
	if (token != NULL)
		keyword = ScanKeywordLookup(token, ScanKeywords, NumScanKeywords);
	scerror->isKeyword = keyword != NULL;
}

void ScCodeEreport(int scerrorcode)
{
	scerror->scerrorcode = scerrorcode;
}

void AssertSprintf(int res)
{
	if (res != -1)
		return;
	scerror->scerrorcode = SCERRUNEXPERRSPRINTFSQLSYNTAX;
	ThrowExceptionCode(SCERRUNEXPERRSPRINTFSQLSYNTAX,"Incorrect size for sprintf, which is called due to an error.");
}

/* 
 * Methods to allocate and create error message used in reporting.
*/

char *ScErrMessage(const char *fmt)
{
	char *buffer = (char *) palloc(strlen(fmt)+2);
	AssertSprintf(sprintf(buffer,fmt));
	return buffer;
}

char *ScErrMessageStr(const char *fmt, const char *msg)
{
	Size slen = strlen(fmt)+strlen(msg) +2;
	char *buffer = (char *) palloc(slen);
	AssertSprintf(sprintf(buffer,fmt,msg));
	return buffer;
}

char *ScErrMessageInt(const char *fmt, int val)
{
	Size vallen = 1;
	if (val > 0)
		vallen = vallen + (int)log10((float)val);
	if (val < 0)
		vallen = vallen + (int)log10((float)(-val)) + 1;
	Size slen = strlen(fmt) + 1 + vallen + 2;
	char *buffer = (char *) palloc(slen);
	AssertSprintf(sprintf(buffer,fmt,val));
	return buffer;
}

char *ScErrMessageLU(const char *fmt, long unsigned val)
{
	Size vallen = 1;
	if (val > 0)
		vallen = vallen + (int)log10((float)val);
	//if (val < 0)
	//	vallen = vallen + (int)log10((float)(-val)) + 1;
	Size slen = strlen(fmt) + 1+vallen + 2;
	char *buffer = (char *) palloc(slen);
	AssertSprintf(sprintf(buffer,fmt,val));
	return buffer;
}

char *ScErrMessagePointer(const char *fmt, void *ptr)
{
	Size slen = strlen(fmt)+8 + 2;
	char *buffer = (char *) palloc(slen);
	AssertSprintf(sprintf(buffer,fmt,ptr));
	return buffer;
}

char *ScErrMessageStrPointer(const char *fmt, const char *msg, void *ptr)
{
	Size slen = strlen(fmt)+strlen(msg) +2+8;
	char *buffer = (char *) palloc(slen);
	AssertSprintf(sprintf(buffer,fmt,msg, ptr));
	return buffer;
}

char *ScErrMessageStrs(int nrStrs, const char *fmt, ...)
{
	va_list strs;
	Size slen = strlen(fmt)+2;
	va_start(strs, fmt);
	for (int i=0; i<nrStrs; i++)
		slen = slen + strlen(va_arg(strs, char*));
	char *buffer = (char *) palloc(slen);
	va_end(strs);
	va_start(strs, fmt);
	AssertSprintf(vsprintf(buffer, fmt, strs));
	va_end(strs);
	return buffer;
}

/*
 * Methods to throw exceptions from C code. The exceptions are cought in the interface parsing function.
*/
#include "ErrorManagment.h"

void ThrowException(char *msg)
{
	scerror->scerrorcode = SCERRUNEXPSQLPARSER;
	scerror->scerrmessage = msg;
	throw UnmanagedParserException(SCERRUNEXPSQLPARSER);
}

void ThrowExceptionCode(int scerrcode, char *msg)
{
	scerror->scerrorcode = scerrcode;
	scerror->scerrmessage = msg;
	throw UnmanagedParserException(scerrcode);
}

void ThrowExceptionReport(int scerrorcode, int position, wchar_t *tocken, char *message)
{
	ScEreport(scerrorcode, position, tocken, message);
	throw UnmanagedParserException(scerrorcode);
}

#ifdef _MSC_VER
# pragma warning(pop)
#endif // _MSC_VER
