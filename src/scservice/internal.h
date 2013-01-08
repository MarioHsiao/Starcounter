
#pragma once


#include "../Starcounter.ErrorCodes/scerrres/scerrres.h"


#include <stdint.h>


extern void _set_shutdown_event_handler(void (*shutdown_event_handler)());

extern uint32_t _create_event(const wchar_t *name, void **phandle);
extern void _destroy_event(void *handle);
extern void _set_event(void *handle);

extern uint32_t _exec(wchar_t *command_line, int32_t inherit_console, void **phandle);
extern uint32_t _wait(void **handles, uint32_t count, uint32_t *psignaled_index);
extern void _kill_and_cleanup(void *handle);

extern uint32_t _read_service_config(const wchar_t *name, wchar_t **pserver_dir);
extern uint32_t _read_server_config(const wchar_t *server_config_path, wchar_t **pserver_logs_dir);
