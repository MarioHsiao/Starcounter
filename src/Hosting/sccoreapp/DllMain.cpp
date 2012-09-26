
#include "internal.h"

#define WIN32_LEAN_AND_MEAN
#include <Windows.h>


BOOL WINAPI DllMain(
	HINSTANCE hInstance,
	DWORD reason,
	LPVOID pReserved
	)
{
	switch (reason)
	{
	case DLL_PROCESS_ATTACH:
		HMODULE hmodule = LoadLibrary(L"sccorelib.dll");
		if (!hmodule) return FALSE;
		coalmine_standby = (COALMINE_STANDBY)GetProcAddress(hmodule, "cm2_standby");
		if (!coalmine_standby) return FALSE;
		break;
	}

	return TRUE;
}
