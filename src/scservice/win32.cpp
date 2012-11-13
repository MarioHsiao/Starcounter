
#include "internal.h"

#include <crtdbg.h>
#include <stdint.h>
#include <string.h>

#define WIN32_LEAN_AND_MEAN
#include <Windows.h>


_STATIC_ASSERT(sizeof(HANDLE) == sizeof(void *));


uint32_t _create_event(const char *name, void **phandle)
{
	SECURITY_ATTRIBUTES *psa;
	HANDLE hevent;
	DWORD dr;
	
	psa = NULL; // TODO:
	hevent = CreateEventA(psa, TRUE, FALSE, name);
	if (hevent)
	{
		if (GetLastError() != ERROR_ALREADY_EXISTS)
		{
			*phandle = (void *)hevent;
			return 0;
		}
		CloseHandle(hevent);
		return ERROR_ALREADY_EXISTS;
	}

	dr = GetLastError();
	return (uint32_t)dr;
}

void _destroy_event(void *handle)
{
	CloseHandle((HANDLE)handle);
}

uint32_t _exec(char *command_line, void **phandle)
{
	STARTUPINFOA si;
	PROCESS_INFORMATION pi;
	DWORD dr;

	memset(&si, 0, sizeof(si));
	si.cb = sizeof(si);
	memset(&si, 0, sizeof(pi));

	if(CreateProcessA(0, command_line, NULL, NULL, TRUE, 0, NULL, NULL, &si, &pi))
	{
		CloseHandle(pi.hThread);
		*phandle = (void *)pi.hProcess;
		return 0;
	}

	dr = GetLastError();
	return (uint32_t)dr;
}

uint32_t _wait(void **handles, uint32_t count, uint32_t *psignaled_index)
{
	DWORD dr;

	dr = WaitForMultipleObjects(count, (HANDLE *)handles, FALSE, INFINITE);
	if (dr != WAIT_FAILED)
	{
		*psignaled_index = (dr - WAIT_OBJECT_0);
		return 0;
	}

	dr = GetLastError();
	return (uint32_t)dr;
}

void _kill_and_cleanup(void *handle)
{
	HANDLE hprocess;
	BOOL terminated;
	DWORD dr;
	BOOL br;

	hprocess = (HANDLE)handle;

	terminated = TerminateProcess(hprocess, 1); // TODO: Exit code
	if (!terminated)
	{
		dr = GetLastError();
		if (dr == ERROR_ACCESS_DENIED)
		{
			// Process already dead.
		}
	}

	dr = WaitForSingleObject(hprocess, INFINITE);
	if (dr == WAIT_FAILED)
	{
		dr = GetLastError();
	}

	br = GetExitCodeProcess(hprocess, &dr);
	br = CloseHandle(hprocess);
}
