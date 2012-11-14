
#pragma once


#include <stdint.h>


extern void _set_shutdown_event_handler(void (*shutdown_event_handler)());

extern uint32_t _create_event(const char *name, void **phandle);
extern void _destroy_event(void *handle);
extern void _set_event(void *handle);

extern uint32_t _exec(char *command_line, void **phandle);
extern uint32_t _wait(void **handles, uint32_t count, uint32_t *psignaled_index);
extern void _kill_and_cleanup(void *handle);


extern uint32_t _read_config(const char *name, char **pserver_dir);
