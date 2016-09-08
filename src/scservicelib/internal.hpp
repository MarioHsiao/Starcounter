
#pragma once


#include "../Starcounter.ErrorCodes/scerrres/scerrres.h"
#include "../Starcounter.ErrorCodes/scerrres/format.h"

// Level0 includes.
#include <sccorelog.h>
#include <sccorelib.h>

#include <stdint.h>


extern int _set_shutdown_event_handler(void (*shutdown_event_handler)());

extern uint32_t _create_event(const wchar_t *name, void **phandle);
extern void _destroy_event(void *handle);
extern void _set_event(void *handle);

extern uint32_t _exec(wchar_t *command_line, int32_t inherit_console, void **phandle);
extern uint32_t _wait(void **handles, uint32_t count, uint32_t *psignaled_index);
extern void _kill_and_cleanup(void *handle);
extern void _kill_and_cleanup_orphaned_children(int32_t logsteps);

extern uint32_t _read_service_config(
    const wchar_t *name,
    wchar_t **pserver_dir);

extern uint32_t _read_server_config(
    const wchar_t *server_config_path,
    wchar_t **pserver_logs_dir,
    wchar_t **pserver_temp_dir,
    wchar_t **pserver_database_dir,
    wchar_t **psystem_http_port,
    wchar_t **pdefault_user_http_port,
    wchar_t **pprolog_port);

extern uint32_t _read_gateway_config(
    const wchar_t *gateway_config_path,
    wchar_t **pgateway_workers_number);

extern uint32_t _read_database_config(
    const wchar_t *database_config_path,
    wchar_t **pdatabase_logs_dir,
    wchar_t **pdatabase_temp_dir,
    wchar_t **pdatabase_image_dir,
    wchar_t **pdatabase_scheduler_count);

// Opens Starcounter log for writing.
uint32_t OpenStarcounterLog(const char *server_name, const wchar_t* server_log_dir);

// Closes Starcounter log.
void CloseStarcounterLog();

// Write critical into log.
void LogWriteCritical(const wchar_t* msg);

// Write error into log.
void LogWriteError(const wchar_t* msg);

void LogVerboseMessage(const wchar_t* msg);