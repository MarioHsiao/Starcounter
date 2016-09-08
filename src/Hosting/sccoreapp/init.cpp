
#include "internal.h"


// These are here and not in external.h because the include Windows.h
#include <sccorelib.h>
#include <sccorelog.h>


static void *__hlogs = 0;


static void __critical_log_handler(void *c, uint32_t error_code);
static void __critical_log_handler(void *c, const char *message);
static void __critical_log_handler(void *c, const wchar_t *message);


void _init(void *hlogs)
{
	__hlogs = hlogs;
}

void _log_critical(uint32_t e)
{
	__critical_log_handler(0, e);
}

void _log_critical(const wchar_t *message)
{
	__critical_log_handler(0, message);
}


void __critical_log_handler(void *c, uint32_t error_code)
{
	DWORD dr;
	wchar_t message[256];

	dr = 0;

	HMODULE hmodule;
	hmodule = LoadLibrary(L"scerrres.dll");
	if (hmodule)
	{
		dr = FormatMessage(
			FORMAT_MESSAGE_FROM_HMODULE, 
			hmodule, 
			error_code,
			0,
			message,
			256,
			NULL
			);
		FreeLibrary(hmodule);
	}
	
	if (!dr)
	{
#pragma warning (disable: 4996)
		swprintf(message, L"%u.", error_code);
#pragma warning (default: 4996)
	}

	__critical_log_handler(c, message);
}

void __critical_log_handler(void *c, const char *message)
{
	uint64_t hlogs = (uint64_t)__hlogs;
	if (hlogs)
	{
		if (message)
		{
			star_kernel_write_to_logs_utf8(
				hlogs, SC_ENTRY_CRITICAL, 0, message
				);
		}
		star_flush_to_logs(hlogs);
	}
}

void __critical_log_handler(void *c, const wchar_t *message)
{
	uint64_t hlogs = (uint64_t)__hlogs;
	if (hlogs)
	{
		if (message)
		{
			star_kernel_write_to_logs(
				hlogs, SC_ENTRY_CRITICAL, 0, (const ucs2_char *)message
				);
		}
		star_flush_to_logs(hlogs);
	}
}
