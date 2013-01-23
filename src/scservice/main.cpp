
#include "internal.h"
#include <windows.h>
#include <stdlib.h>
#include <stdint.h>
#include <stdio.h>
#include <string.h>

#define MONITOR_INHERIT_CONSOLE 0
#define GATEWAY_INHERIT_CONSOLE 0
#define SCDATA_INHERIT_CONSOLE 1
#define SCCODE_INHERIT_CONSOLE 1


static void *hcontrol_event;

static void __shutdown_event_handler()
{
    _set_event(hcontrol_event);
}

int wmain(int argc, wchar_t* argv[], wchar_t* envp[])
{
    uint32_t r;

    const wchar_t *name = L"PERSONAL";
    const wchar_t *admin_dbname = L"administrator";	
	const wchar_t *mingw = L"MinGW\\bin\\x86_64-w64-mingw32-gcc.exe";

    void *handles[5];

    memset(handles, 0, sizeof(handles));

    // Read server configuration.
    wchar_t *server_dir;
    r = _read_service_config(name, &server_dir);
    if (r) goto end;

    wchar_t *name_upr;
    wchar_t *admin_dbname_upr;
    const wchar_t *str_template;
    size_t str_num_chars, str_size_bytes;

    wchar_t *event_name;
    wchar_t *monitor_cmd;
    wchar_t *gateway_cmd;
    wchar_t *scdata_cmd;
    wchar_t *sccode_cmd;
    wchar_t *admin_exe_path;
    wchar_t *admin_working_dir;
    wchar_t *database_cfg_path;
    wchar_t *server_cfg_path;

    str_num_chars = wcslen(name) + 1;
    str_size_bytes = str_num_chars * sizeof(wchar_t);

    name_upr = (wchar_t *)malloc(str_size_bytes);
    if (!name_upr) goto err_nomem;

    wcscpy_s(name_upr, str_num_chars, name);
    _wcsupr_s(name_upr, str_num_chars);

	// Database uppercase
    str_num_chars = wcslen(admin_dbname) + 1;
    str_size_bytes = str_num_chars * sizeof(wchar_t);

    admin_dbname_upr = (wchar_t *)malloc(str_size_bytes);
    if (!admin_dbname_upr) goto err_nomem;

    wcscpy_s(admin_dbname_upr, str_num_chars, admin_dbname);
    _wcsupr_s(admin_dbname_upr, str_num_chars);



    str_template = L"SCSERVICE_%s";
    str_num_chars = 
        wcslen(str_template) +
        wcslen(name_upr) +
        1;

    str_size_bytes = str_num_chars * sizeof(wchar_t);

    event_name = (wchar_t *)malloc(str_size_bytes);
    if (!event_name) goto err_nomem;

    swprintf(event_name, str_num_chars, str_template, name_upr);

    // Creating path to server configuration file.
    str_template = L"%s\\%s\\%s.server.config";
    str_num_chars =
        wcslen(str_template) +
        wcslen(server_dir) +
        wcslen(name_upr) +
        wcslen(name_upr) +
        1;

    str_size_bytes = str_num_chars * sizeof(wchar_t);
    server_cfg_path = (wchar_t *)malloc(str_size_bytes);
    if (!server_cfg_path) goto err_nomem;

    swprintf(server_cfg_path, str_num_chars, str_template, server_dir, name_upr, name_upr);

    // Reading server logs directory.
    wchar_t *server_logs_dir;
    wchar_t *server_temp_dir;
    wchar_t *server_database_dir;
    r = _read_server_config(server_cfg_path, &server_logs_dir, &server_temp_dir, &server_database_dir);
    if (r) goto end;

	// Creating path to the database configuration file.
    str_template = L"%s\\%s\\%s.db.config";
    str_num_chars =
        wcslen(str_template) +
        wcslen(server_database_dir) +
        wcslen(admin_dbname) +
        wcslen(admin_dbname) +
        1;
    str_size_bytes = str_num_chars * sizeof(wchar_t);
	database_cfg_path = (wchar_t *)malloc(str_size_bytes);
    if (!database_cfg_path) goto err_nomem;

    swprintf(database_cfg_path, str_num_chars, str_template, server_database_dir, admin_dbname, admin_dbname);

    wchar_t *database_logs_dir;
    wchar_t *database_temp_dir;
    wchar_t *database_image_dir;
    wchar_t *database_scheduler_count;

	// Reading database configuration
    r = _read_database_config(database_cfg_path, &database_logs_dir, &database_temp_dir, &database_image_dir, &database_scheduler_count);
    if (r) goto end;

    str_template = L"scipcmonitor.exe \"%s\" \"%s\"";
    str_num_chars =
        wcslen(str_template) +
        wcslen(name_upr) +
        wcslen(server_logs_dir) +
        1;

    str_size_bytes = str_num_chars * sizeof(wchar_t);
    monitor_cmd = (wchar_t *)malloc(str_size_bytes);
    if (!monitor_cmd) goto err_nomem;

    swprintf(monitor_cmd, str_num_chars, str_template, name_upr, server_logs_dir);

    // TODO:
    // Gateway configuration directory where? Currently set to installation
    // directory.

    str_template = L"scnetworkgateway.exe \"%s\" \"scnetworkgateway.xml\" \"%s\"";
    str_num_chars =
        wcslen(str_template) +
        wcslen(name_upr) +
        wcslen(server_logs_dir) +
        1;

    str_size_bytes = str_num_chars * sizeof(wchar_t);
    gateway_cmd = (wchar_t *)malloc(str_size_bytes);
    if (!gateway_cmd) goto err_nomem;

    swprintf(gateway_cmd, str_num_chars, str_template, name_upr, server_logs_dir);

	// Creating Admin exepath
	str_template = L"applications\\Starcounter.Administrator\\Administrator.exe";
    str_num_chars = 
		wcslen(str_template) + 
		1;

    str_size_bytes = str_num_chars * sizeof(wchar_t);
    admin_exe_path = (wchar_t *)malloc(str_size_bytes);
    if (!admin_exe_path) goto err_nomem;
	swprintf(admin_exe_path, str_num_chars, str_template);


	// Creating Admin working dir (.srv\\Personal\\Apps\\Administrator)
    str_template = L"applications\\Starcounter.Administrator";
    str_num_chars = 
		wcslen(str_template) + 
		1;

    str_size_bytes = str_num_chars * sizeof(wchar_t);
    admin_working_dir = (wchar_t *)malloc(str_size_bytes);
    if (!admin_working_dir) goto err_nomem;
	swprintf(admin_working_dir, str_num_chars, str_template);

	// Creating scdata command
    str_template = L"scdata.exe %s %s \"%s\"";
    str_num_chars = 
		wcslen(str_template) + 
		wcslen(admin_dbname_upr) +	// database name uppercase
		wcslen(admin_dbname) +		// databse uri
		wcslen(server_logs_dir) + 
		1;

    str_size_bytes = str_num_chars * sizeof(wchar_t);
    scdata_cmd = (wchar_t *)malloc(str_size_bytes);
    if (!scdata_cmd) goto err_nomem;

	swprintf(scdata_cmd, str_num_chars, str_template, admin_dbname_upr, admin_dbname, server_logs_dir);

	// Creating sccode command
	str_num_chars = 0;	

	if( wcslen( database_scheduler_count ) > 0 ) {
		str_template = L"sccode.exe %s --DatabaseDir=\"%s\" --OutputDir=\"%s\" --TempDir=\"%s\" --CompilerPath=\"%s\" --AutoStartExePath=\"%s\" --UserArguments=\"%s\" --WorkingDir=\"%s\" --SchedulerCount=%s";
		str_num_chars+= wcslen(database_scheduler_count);
	}	
	else
	{
		str_template = L"sccode.exe %s --DatabaseDir=\"%s\" --OutputDir=\"%s\" --TempDir=\"%s\" --CompilerPath=\"%s\" --AutoStartExePath=\"%s\" --UserArguments=\"%s\" --WorkingDir=\"%s\"";
	}

    str_num_chars += wcslen(str_template) + 
		wcslen(admin_dbname_upr) + 
		wcslen(database_image_dir) + 
		wcslen(database_logs_dir) + 
		wcslen(database_temp_dir) +
		wcslen(mingw) + 
		wcslen(admin_exe_path) +
		wcslen(server_cfg_path) +
		wcslen(admin_working_dir) + 1;

    str_size_bytes = str_num_chars * sizeof(wchar_t);
    sccode_cmd = (wchar_t *)malloc(str_size_bytes);
    if (!sccode_cmd) goto err_nomem;

	if( wcslen( database_scheduler_count ) > 0 ) {
		swprintf(sccode_cmd, str_num_chars, str_template, admin_dbname_upr, database_image_dir, database_logs_dir, database_temp_dir, mingw, admin_exe_path, server_cfg_path, admin_working_dir, database_scheduler_count);
	}
	else {
	    swprintf(sccode_cmd, str_num_chars, str_template, admin_dbname_upr, database_image_dir, database_logs_dir, database_temp_dir, mingw, admin_exe_path, server_cfg_path, admin_working_dir);
	}

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

	// TODO: Wait until ipcmonitor/gateway is ready.
	// TODO: Remove the Sleep().
	Sleep(2000);

    r = _exec(scdata_cmd, SCDATA_INHERIT_CONSOLE, (handles + 3));
    if (r) goto end;

    r = _exec(sccode_cmd, SCCODE_INHERIT_CONSOLE, (handles + 4));
    if (r) goto end;

    // Wait for signal.

    for (;;)    
    {
        uint32_t signaled_index;
		r = _wait(handles, 5, &signaled_index);
        if (r) goto end;
        
        switch (signaled_index)
        {
        case 0:
            // Shutdown signaled.
            goto end;
        case 1:
            // IPC monitor died. Kill the server.
            goto end;
        case 2:
            // Gateway died. Kill the server. Kill the system.
            goto end;
        case 3:
            // sccode died. Kill the server. Kill the system.
            goto end;
        case 4:
            // scdata died. Kill the server. Kill the system.
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
	if(r) {
	 printf( "Exited with error code:%d\n", r );	// TODO: Make "Starcounter error text" (FormatStarcounterErrorMessage?)
	}
	
    if (handles[4]) _kill_and_cleanup(handles[4]);	// SCDODE
    if (handles[3]) _kill_and_cleanup(handles[3]);	// SCDATA
    if (handles[2]) _kill_and_cleanup(handles[2]);	// Gateway
    if (handles[1]) _kill_and_cleanup(handles[1]);	// Monitor
    if (handles[0]) _destroy_event(handles[0]);

    return (int32_t)r;
}
