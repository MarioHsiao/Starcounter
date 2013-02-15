#include "internal.h"
#include <windows.h>
#include <stdlib.h>
#include <stdint.h>
#include <stdio.h>
#include <string.h>

// Global handle to server log.
extern uint64_t g_sc_log_handle_;

// Opens Starcounter log for writing.
uint32_t OpenStarcounterLog(wchar_t* server_log_dir)
{
    uint32_t err_code = sccorelog_init(0);
    if (err_code)
        return err_code;

    err_code = sccorelog_connect_to_logs(L"scservice", NULL, &g_sc_log_handle_);
    if (err_code)
    {
        g_sc_log_handle_ = 0;
        return err_code;
    }

    err_code = sccorelog_bind_logs_to_dir(g_sc_log_handle_, server_log_dir);
    if (err_code)
    {
        g_sc_log_handle_ = 0;
        return err_code;
    }

    return 0;
}

// Closes Starcounter log.
void CloseStarcounterLog()
{
    uint32_t err_code = sccorelog_release_logs(g_sc_log_handle_);

    _SC_ASSERT(0 == err_code);
}

// Write critical into log.
void LogWriteCritical(const wchar_t* msg)
{
    uint32_t err_code = sccorelog_kernel_write_to_logs(g_sc_log_handle_, SC_ENTRY_CRITICAL, msg);

    _SC_ASSERT(0 == err_code);

    err_code = sccorelog_flush_to_logs(g_sc_log_handle_);

    _SC_ASSERT(0 == err_code);
}

// Write error into log.
void LogWriteError(const wchar_t* msg)
{
    uint32_t err_code = sccorelog_kernel_write_to_logs(g_sc_log_handle_, SC_ENTRY_ERROR, msg);

    _SC_ASSERT(0 == err_code);

    err_code = sccorelog_flush_to_logs(g_sc_log_handle_);

    _SC_ASSERT(0 == err_code);
}