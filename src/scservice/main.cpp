
#include "internal.h"

#include <stdlib.h>
#include <stdint.h>
#include <stdio.h>
#include <string.h>


#define MONITOR_INHERIT_CONSOLE 0
#define GATEWAY_INHERIT_CONSOLE 0
#define ADMIN_INHERIT_CONSOLE 1


static void *hcontrol_event;

static void __shutdown_event_handler()
{
	_set_event(hcontrol_event);
}


int main (int argc, char *argv[])
{
	uint32_t r;

	const char *name = "PERSONAL";

	void *handles[4];
	memset(handles, 0, sizeof(handles));

	// Read server configuration.

	char *server_dir;
	r = _read_config(name, &server_dir);
	if (r) goto end;

	char *name_upr;
	const char *str_template;
	size_t str_size;
	char *event_name;
	char *monitor_cmd;
	char *gateway_cmd;
	char *admin_cmd;

	name_upr = (char *)malloc(strlen(name) + 1);
	if (!name_upr) goto err_nomem;
#pragma warning (disable: 4996)
	strcpy(name_upr, name);
	_strupr(name_upr);
#pragma warning (default: 4996)

	str_template = "SCSERVICE_%s";
	str_size = strlen(str_template) + strlen(name_upr) + 1;
	event_name = (char *)malloc(str_size);
	if (!event_name) goto err_nomem;
#pragma warning (disable: 4996)
	sprintf(event_name, str_template, name_upr);
#pragma warning (default: 4996)

	str_template = "ScConnMonitor.exe \"%s\" \"%s\\%s\"";
	str_size =
		strlen(str_template) +
		strlen(name_upr) +
		strlen(server_dir) +
		strlen(name_upr) +
		1;
	monitor_cmd = (char *)malloc(str_size);
	if (!monitor_cmd) goto err_nomem;
	sprintf_s(monitor_cmd, str_size, str_template, name_upr, server_dir, name_upr);

	// TODO:
	// Gateway configuration directory where? Currently set to installation
	// directory.

	str_template = "ScGateway.exe \"%s\" \"ScGateway.xml\" \"%s\\%s\"";
	str_size =
		strlen(str_template) +
		strlen(name_upr) +
		strlen(server_dir) +
		strlen(name_upr) +
		1;
	gateway_cmd = (char *)malloc(str_size);
	if (!gateway_cmd) goto err_nomem;
#pragma warning (disable: 4996)
	sprintf(gateway_cmd, str_template, name_upr, server_dir, name_upr);
#pragma warning (default: 4996)

	str_template = "ReferenceServer.exe \"%s\\%s\\%s.server.config\"";
	str_size =
		strlen(str_template) +
		strlen(server_dir) +
		strlen(name_upr) +
		strlen(name_upr) +
		1;
	admin_cmd = (char *)malloc(str_size);
	if (!admin_cmd) goto err_nomem;
#pragma warning (disable: 4996)
	sprintf(admin_cmd, str_template, server_dir, name_upr, name_upr);
#pragma warning (default: 4996)

	// Create shutdown event. Will fail if event already exists and so also
	// confirm that no server with the specific name already is running.

	r = _create_event(event_name, &hcontrol_event);
	if (r) goto end;
	_set_shutdown_event_handler(__shutdown_event_handler);
	handles[0] = hcontrol_event;

	// Start and register IPC monitor.

	r = _exec(monitor_cmd, MONITOR_INHERIT_CONSOLE, (handles + 1));
	if (r) goto end;

	// Start and register network gateway.

	r = _exec(gateway_cmd, GATEWAY_INHERIT_CONSOLE, (handles + 2));
	if (r) goto end;

	// Start and register admin application.
	//
	// NOTE: For now we start the "reference server".

	r = _exec(admin_cmd, ADMIN_INHERIT_CONSOLE, (handles + 3));
	if (r) goto end;

	// Wait for signal.

	for (;;)	
	{
		uint32_t signaled_index;
		r = _wait(handles, 4, &signaled_index);
		if (r) goto end;
		
		switch (signaled_index)
		{
		case 0:
			// Shutdown signalled.
			goto end;
		case 1:
			// IPC monitor died. Kill the server.
			goto end;
		case 2:
			// Gateway died. Kill the server. Kill the system.
			goto end;
		case 3:
			// Administrator application died. Kill the system.
			goto end;
		default:
			__assume(0);
		}
	}

err_nomem:
	r = SCERROUTOFMEMORY;
	goto end;

end:
	// Terminating.

	if (handles[3]) _kill_and_cleanup(handles[3]);
	if (handles[2]) _kill_and_cleanup(handles[2]);
	if (handles[1]) _kill_and_cleanup(handles[1]);
	if (handles[0]) _destroy_event(handles[0]);

	return (int32_t)r;
}
