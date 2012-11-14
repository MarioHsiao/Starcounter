
#include "internal.h"

#include <stdlib.h>
#include <stdint.h>
#include <stdio.h>
#include <string.h>


int main (int argc, char *argv[])
{
	// Read server name from command-line.

	const char *name = "PERSONAL"; // TODO:

	// Read server configuration from file in installation directory.

	// TODO:

	const char *inout_dir = "C:\\Test";
	const char *config_path = ".srv\\personal\\personal.server.config";

	void *handles[4];
	memset(handles, 0, sizeof(handles));

	// TODO:
	//
	// 1. Is gateway configuration always in installation directory?
	// 2. Is it ok that the gateway and IPC monitor always has the same output
	//    directory?

	char *name_upr;
	char *event_name;
	char *ipc_monitor_cmd;
	char *gateway_cmd;
	char *admin_cmd;

	name_upr = (char *)malloc(1024); // TODO:
#pragma warning (disable: 4996)
	strcpy(name_upr, name);
	_strupr(name_upr);
#pragma warning (default: 4996)

	event_name = (char *)malloc(1024); // TODO:
#pragma warning (disable: 4996)
	sprintf(event_name, "SCSERVICE_%s", name_upr);
#pragma warning (default: 4996)
	
	ipc_monitor_cmd = (char *)malloc(1024); // TODO:
#pragma warning (disable: 4996)
	sprintf(ipc_monitor_cmd, "ScConnMonitor.exe \"%s\" \"%s\"", name_upr, inout_dir);
#pragma warning (default: 4996)

	gateway_cmd = (char *)malloc(1024); // TODO:
#pragma warning (disable: 4996)
	sprintf(gateway_cmd, "ScGateway.exe \"%s\" \"%s\" \"%s\"", name_upr, "ScGateway.xml", inout_dir);
#pragma warning (default: 4996)

	admin_cmd = (char *)malloc(1024); // TODO:
#pragma warning (disable: 4996)
	sprintf(admin_cmd, "ReferenceServer.exe \"%s\"", config_path);
#pragma warning (default: 4996)

	// Create shutdown event. Will fail if event already exists and so also
	// confirm that no server with the specific name already is running.

	uint32_t r;
	r = _create_event(event_name,  (handles + 0));
	if (r) goto end;

	// Start and register IPC monitor.

	r = _exec(ipc_monitor_cmd, (handles + 1));
	if (r) goto end;

	// Start and register network gateway.

	r = _exec(gateway_cmd, (handles + 2));
	if (r) goto end;

	// Start and register admin application.
	//
	// NOTE: For now we start the "reference server".

	r = _exec(admin_cmd, (handles + 3));
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
			// Gateway died. Kill the server. TODO: Restart gateway?
			goto end;
		case 3:
			// Administrator application died. Kill the process.
				
			// TODO:
			// Should be restarted but we then need to find the running hosts
			// and reattach them.
				
			goto end;
		default:
			__assume(0);
		}
	}

end:
	// Terminating.

	if (handles[3]) _kill_and_cleanup(handles[3]);
	if (handles[2]) _kill_and_cleanup(handles[2]);
	if (handles[1]) _kill_and_cleanup(handles[1]);
	if (handles[0]) _destroy_event(handles[0]);

	return (int32_t)r;
}
