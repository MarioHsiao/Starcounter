
#include "internal.h"

#include <crtdbg.h>
#include <stdint.h>
#include <string.h>

#define WIN32_LEAN_AND_MEAN
#include <Windows.h>
#include <TlHelp32.h>

_STATIC_ASSERT(sizeof(HANDLE) == sizeof(void *));

#define PROCESS_NAME_NETWORKGATEWAY L"scnetworkgateway.exe"
#define PROCESS_NAME_IPCMONITOR L"scipcmonitor.exe"
#define PROCESS_NAME_CODEHOST L"sccode.exe"
#define PROCESS_NAME_CODEDATA L"scdata.exe"
#define PROCESS_NAME_CODELOGW L"log_writer.exe"
#define PROCESS_NAME_PROLOG L"scsqlparser.exe"

static void (*__shutdown_event_handler)();

static BOOL WINAPI __console_handler(DWORD console_event);
static void __kill_and_cleanup_children(DWORD process_id);

BOOL _set_shutdown_event_handler(void (*shutdown_event_handler)())
{
    __shutdown_event_handler = shutdown_event_handler;
    return SetConsoleCtrlHandler(__console_handler, TRUE);
}

BOOL WINAPI __console_handler(DWORD console_event)
{
    switch(console_event)
    {
    case CTRL_C_EVENT:
    case CTRL_BREAK_EVENT:
    case CTRL_CLOSE_EVENT:
    case CTRL_LOGOFF_EVENT:
    case CTRL_SHUTDOWN_EVENT:
        __shutdown_event_handler();
        return TRUE;
    default:
        return FALSE;
    }
}

extern DWORD _init_secattr_user(VOID *mem512); // win32_acl.cpp

uint32_t _create_event(const wchar_t *name, void **phandle)
{
    DWORD dr;
    uint8_t buf512[512];
    SECURITY_ATTRIBUTES *psa;
    HANDLE hevent;

    dr = _init_secattr_user(buf512);
    if (dr == 0)
    {
        psa = (SECURITY_ATTRIBUTES *)buf512;
        hevent = CreateEvent(psa, TRUE, FALSE, name);
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
    }
    return (uint32_t)dr;
}

void _destroy_event(void *handle)
{
    CloseHandle((HANDLE)handle);
}

void _set_event(void *handle)
{
    SetEvent((HANDLE)handle);
}

uint32_t _exec(wchar_t *command_line, int32_t inherit_console, void **phandle)
{
    DWORD pcf;
    STARTUPINFO si;
    PROCESS_INFORMATION pi;
    DWORD dr;

    pcf = 0;
    if (!inherit_console) pcf |= CREATE_NO_WINDOW;

    memset(&si, 0, sizeof(si));
    si.cb = sizeof(si);
    memset(&si, 0, sizeof(pi));

    if(CreateProcess(0, command_line, NULL, NULL, FALSE, pcf, NULL, NULL, &si, &pi))
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
    BOOL br;
    DWORD dr;
    DWORD process_id;

    hprocess = (HANDLE)handle;

    br = TerminateProcess(hprocess, SCERRKILLEDBYPROCESSMANAGER);
    if (!br)
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

    // We kill the child processes after we killed the parent process to make
    // sure that it stopped spawning new processes. 
    //
    // As long as the handle is opened we can still get the process id
    // regardless of the process is dead or not. The child processes will also
    // still reference the process id as the parent process.

    process_id = GetProcessId(handle);
    __kill_and_cleanup_children(process_id);

    br = GetExitCodeProcess(hprocess, &dr);
    br = CloseHandle(hprocess);
}

void __kill_and_cleanup_children(DWORD process_id)
{
    HANDLE hsnapshot;
    PROCESSENTRY32 pe;
    HANDLE hchild_proc;
    DWORD dr;

    hsnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);

    memset(&pe, 0, sizeof(PROCESSENTRY32));
    pe.dwSize = sizeof(PROCESSENTRY32);

    if (Process32First(hsnapshot, &pe))
    {
        do
        {
            if (pe.th32ParentProcessID == process_id)
            {
                hchild_proc = OpenProcess(PROCESS_ALL_ACCESS, FALSE, pe.th32ProcessID);
                if (hchild_proc)
                {
                    _kill_and_cleanup(hchild_proc);
                }               
                else
                {
                    dr = GetLastError();
                    if (dr == ERROR_INVALID_PARAMETER)
                    {
                        // The referenced process was terminated after we
                        // created to snapshot.

                        __kill_and_cleanup_children(pe.th32ProcessID);
                    }
                }
            }
        }
        while (Process32Next(hsnapshot, &pe));
    }
}

void _kill_and_cleanup_orphaned_children(int32_t logsteps)
{
    HANDLE hsnapshot;
    PROCESSENTRY32 pe;
    HANDLE hparent_proc;
    HANDLE hchild_proc;
    BOOL process_found;
    DWORD exitcode;
    DWORD parent_pid;
    wchar_t logmessagebuffer[1024];

    if (logsteps) {
        LogVerboseMessage(L"Checking for orphaned starcounter processes.");
    }

    hsnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);

    memset(&pe, 0, sizeof(PROCESSENTRY32));
    pe.dwSize = sizeof(PROCESSENTRY32);

    parent_pid = 0;
    hparent_proc = 0;
    if (Process32First(hsnapshot, &pe))
    {
        do
        {
            process_found = false;
            if (pe.szExeFile[0] == 's' && pe.szExeFile[1] == 'c')
            {
                if (lstrcmpi(pe.szExeFile, PROCESS_NAME_NETWORKGATEWAY) == 0
                        || lstrcmpi(pe.szExeFile, PROCESS_NAME_IPCMONITOR) == 0
                        || lstrcmpi(pe.szExeFile, PROCESS_NAME_CODEHOST) == 0
                        || lstrcmpi(pe.szExeFile, PROCESS_NAME_CODEDATA) == 0
                        || lstrcmpi(pe.szExeFile, PROCESS_NAME_CODELOGW) == 0
                        || lstrcmpi(pe.szExeFile, PROCESS_NAME_PROLOG) == 0)
                {
                    process_found = true;
                    goto KILL_PROCESS;
                }
            }

            if (process_found)
            {
                // This is a starcounter process. Lets check the parent. If we have a parent set 
                // that is not running we have a process we need to terminate.

                if (pe.th32ParentProcessID != 0) {
                    if (parent_pid != pe.th32ParentProcessID) {
                        if (hparent_proc) {
                            CloseHandle(hparent_proc);
                        }

                        parent_pid = pe.th32ParentProcessID;
                        hparent_proc = OpenProcess(PROCESS_ALL_ACCESS, FALSE, pe.th32ParentProcessID);
                    }

                    if (hparent_proc) {
                        if (GetExitCodeProcess(hparent_proc, &exitcode) && (exitcode == STILL_ACTIVE)) {
                            // The parent is still active. The childprocess is not orphaned.
                            continue; 
                        } 
                    }
KILL_PROCESS:
                    // Parent process is terminated. Lets kill the process.
                    if (logsteps) {
                        _snwprintf_s(logmessagebuffer, 1024, L"Terminating process '%s' with parent pid %d", pe.szExeFile, parent_pid);
                        LogVerboseMessage(logmessagebuffer);
                    }
		            
                    hchild_proc = OpenProcess(PROCESS_ALL_ACCESS, FALSE, pe.th32ProcessID);
                    if (hchild_proc) {
                        _kill_and_cleanup(hchild_proc);
                    }
                } else {
                    // No parent. Do we kill those as well?
                }
            }
        }
        while (Process32Next(hsnapshot, &pe));

        if (hparent_proc) {
            CloseHandle(hparent_proc);
        }
    }
}