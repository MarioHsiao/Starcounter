#include "internal.hpp"
#include <windows.h>
#include <stdlib.h>
#include <stdint.h>
#include <stdio.h>
#include <string.h>

extern "C" int32_t make_sc_process_uri(const char *server_name, const char *process_name, wchar_t *buffer, size_t *pbuffer_size);
extern "C" int32_t make_sc_server_uri(const char *server_name, wchar_t *buffer, size_t *pbuffer_size);

// Global handle to server log.
extern uint64_t g_sc_log_handle_;

extern int32_t logsteps;

// Opens Starcounter log for writing.
uint32_t OpenStarcounterLog(const char *server_name, const wchar_t *server_log_dir)
{
	size_t host_name_size;
	wchar_t *host_name = NULL;
	uint32_t err_code;

	host_name_size = 0;
	//	make_sc_server_uri(server_name, 0, &host_name_size);
	err_code = make_sc_process_uri(server_name, "service", 0, &host_name_size);
	if (err_code) goto err;

	host_name = (wchar_t *)malloc(host_name_size * sizeof(wchar_t));
	if (host_name)
	{
		//		make_sc_server_uri(server_name, host_name, &host_name_size);
		err_code = make_sc_process_uri(server_name, "service", host_name, &host_name_size);
		if (err_code) goto err;

		err_code = sccorelog_init(0);
		if (err_code) goto err;

		err_code = sccorelog_connect_to_logs(reinterpret_cast<const ucs2_char *>(host_name),
			reinterpret_cast<const ucs2_char *>(server_log_dir), NULL, &g_sc_log_handle_);
		if (err_code) goto err;

		goto end;
	}

	err_code = SCERROUTOFMEMORY;
err:
	g_sc_log_handle_ = 0;
end:
	if (host_name) free(host_name);
	return err_code;
}

// Closes Starcounter log.
void CloseStarcounterLog()
{
	uint32_t err_code = sccorelog_release_logs(g_sc_log_handle_);

	_SC_ASSERT(0 == err_code);
}

// Write critical into log.
void LogWriteCritical(const char* msg)
{
	uint32_t err_code;

	if (msg)
	{
		err_code = star_kernel_write_to_logs_utf8(g_sc_log_handle_, SC_ENTRY_CRITICAL, 0, msg);
	}

	err_code = sccorelog_flush_to_logs(g_sc_log_handle_);
}

// Write critical into log.
void LogWriteCritical(const wchar_t* msg)
{
	// NOTE:
	// No asserts in critical log handler. Assertion fails calls critical log
	// handler to log.

	uint32_t err_code;

	if (msg)
	{
		err_code = sccorelog_kernel_write_to_logs(g_sc_log_handle_, SC_ENTRY_CRITICAL, 0,
			reinterpret_cast<const ucs2_char *>(msg));

		//_SC_ASSERT(0 == err_code);
	}

	err_code = sccorelog_flush_to_logs(g_sc_log_handle_);

	//_SC_ASSERT(0 == err_code);
}

// Write error into log.
void LogWriteError(const wchar_t* msg)
{
	uint32_t err_code = sccorelog_kernel_write_to_logs(g_sc_log_handle_, SC_ENTRY_ERROR, 0,
		reinterpret_cast<const ucs2_char *>(msg));

	_SC_ASSERT(0 == err_code);

	err_code = sccorelog_flush_to_logs(g_sc_log_handle_);

	_SC_ASSERT(0 == err_code);
}

// Write debug into log.
void LogWriteDebug(const wchar_t* msg)
{
	uint32_t err_code = sccorelog_kernel_write_to_logs(g_sc_log_handle_, SC_ENTRY_DEBUG, 0,
		reinterpret_cast<const ucs2_char *>(msg));

	_SC_ASSERT(0 == err_code);

	err_code = sccorelog_flush_to_logs(g_sc_log_handle_);

	_SC_ASSERT(0 == err_code);
}

void LogVerboseMessage(const wchar_t* msg)
{
	wprintf(L"<DEBUG> %s\n", msg );

	if (g_sc_log_handle_)
	{

		// Logging to log file.
		LogWriteDebug(msg);
	}

}