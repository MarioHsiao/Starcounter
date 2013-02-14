#include "internal.h"
#include <windows.h>
#include <stdlib.h>
#include <stdint.h>
#include <stdio.h>
#include <string.h>

#define MONITOR_INHERIT_CONSOLE 0
#define GATEWAY_INHERIT_CONSOLE 0
#define SCDATA_INHERIT_CONSOLE 0
#define SCCODE_INHERIT_CONSOLE 1

static void *hcontrol_event;

// Global handle to server log.
uint64_t g_sc_log_handle_ = 0;

static void __shutdown_event_handler()
{
    _set_event(hcontrol_event);
}

// Is called when scservice crashes.
VOID SCAPI LogGatewayCrash(VOID *pc, LPCWSTR str)
{
    LogWriteCritical(str);
}

int wmain(int argc, wchar_t* argv[], wchar_t* envp[])
{
    // Catching all unhandled exceptions in this thread.
    _SC_BEGIN_FUNC

    // Setting the critical log handler.
    _SetCriticalLogHandler(LogGatewayCrash, NULL);

    uint32_t r;

    wchar_t *srv_name = L"PERSONAL";

    if (argc > 1)
    {
        // Checking if help is needed.
        if (argv[1][0] == L'?')
        {
            wprintf(L"scservice.exe [ServerName]\n");
            wprintf(L"Example: scservice.exe personal\n");
            wprintf(L"When no ServerName argument is supplied 'personal' is used.\n\n");
            wprintf(L"How it works:\n");
            wprintf(L"scservice will load XML-file called [ServerName].xml\n");
            wprintf(L"from the same directory as scservice.exe and\n");
            wprintf(L"will fetch corresponding server directory from it.\n");
            wprintf(L"From obtained directory it will load [ServerName].config.xml\n");
            wprintf(L"to read server-related settings.\n");
            wprintf(L"scservice will then start and monitor all required\n");
            wprintf(L"Starcounter components, like scnetworkgateway, scipcmonitor, etc.\n");

            return 0;
        }

        // Reading the server name if specified.
        srv_name = argv[1];
    }

    wprintf(L"Starting Starcounter %s engine...\n", srv_name);

    // Getting executable directory.
    wchar_t exe_dir[1024];
    r = GetModuleFileName(NULL, exe_dir, 1024);
    if ((r == 0) || (r >= 1024))
        goto end;

    // Getting directory name from executable path.
    int32_t c = r;
    while (c > 0)
    {
        c--;

        if (exe_dir[c] == L'\\')
            break;
    }
    exe_dir[c] = L'\0';

    // Setting executable directory as current.
    if (!SetCurrentDirectory(exe_dir))
        goto end;
    
    const wchar_t *admin_dbname = L"administrator";	
	const wchar_t *mingw = L"MinGW\\bin\\x86_64-w64-mingw32-gcc.exe";

    void *handles[5];

    memset(handles, 0, sizeof(handles));

    // Read server configuration.
    wchar_t *server_dir;
    r = _read_service_config(srv_name, &server_dir);
    if (r) goto end;

    wchar_t *srv_name_upr;
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

    str_num_chars = wcslen(srv_name) + 1;
    str_size_bytes = str_num_chars * sizeof(wchar_t);

    srv_name_upr = (wchar_t *)malloc(str_size_bytes);
    if (!srv_name_upr) goto err_nomem;

    wcscpy_s(srv_name_upr, str_num_chars, srv_name);
    _wcsupr_s(srv_name_upr, str_num_chars);

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
        wcslen(srv_name_upr) +
        1;

    str_size_bytes = str_num_chars * sizeof(wchar_t);

    event_name = (wchar_t *)malloc(str_size_bytes);
    if (!event_name) goto err_nomem;

    swprintf(event_name, str_num_chars, str_template, srv_name_upr);

    // Creating path to server configuration file.
    str_template = L"%s\\%s.server.config";
    str_num_chars =
        wcslen(str_template) +
        wcslen(server_dir) +
        wcslen(srv_name_upr) +
        1;

    str_size_bytes = str_num_chars * sizeof(wchar_t);
    server_cfg_path = (wchar_t *)malloc(str_size_bytes);
    if (!server_cfg_path) goto err_nomem;

    swprintf(server_cfg_path, str_num_chars, str_template, server_dir, srv_name_upr);

    wprintf(L"Reading server configuration from: %s\n", server_cfg_path);

    // Reading server logs directory.
    wchar_t *server_logs_dir;
    wchar_t *server_temp_dir;
    wchar_t *server_database_dir;
    wchar_t *system_http_port;
    wchar_t *default_user_http_port;
    r = _read_server_config(
        server_cfg_path,
        &server_logs_dir,
        &server_temp_dir,
        &server_database_dir,
        &system_http_port,
        &default_user_http_port);

    if (r) goto end;

    // Registering the logger.
    r = OpenStarcounterLog(server_logs_dir);
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
        wcslen(srv_name_upr) +
        wcslen(server_logs_dir) +
        1;

    str_size_bytes = str_num_chars * sizeof(wchar_t);
    monitor_cmd = (wchar_t *)malloc(str_size_bytes);
    if (!monitor_cmd) goto err_nomem;

    swprintf(monitor_cmd, str_num_chars, str_template, srv_name_upr, server_logs_dir);

    // TODO:
    // Gateway configuration directory where? Currently set to installation
    // directory.

    str_template = L"scnetworkgateway.exe \"%s\" \"scnetworkgateway.xml\" \"%s\"";
    str_num_chars =
        wcslen(str_template) +
        wcslen(srv_name_upr) +
        wcslen(server_logs_dir) +
        1;

    str_size_bytes = str_num_chars * sizeof(wchar_t);
    gateway_cmd = (wchar_t *)malloc(str_size_bytes);
    if (!gateway_cmd) goto err_nomem;

    swprintf(gateway_cmd, str_num_chars, str_template, srv_name_upr, server_logs_dir);

	// Creating Admin exepath
	str_template = L"scadmin\\Administrator.exe";
    str_num_chars = 
		wcslen(str_template) + 
		1;

    str_size_bytes = str_num_chars * sizeof(wchar_t);
    admin_exe_path = (wchar_t *)malloc(str_size_bytes);
    if (!admin_exe_path) goto err_nomem;
	swprintf(admin_exe_path, str_num_chars, str_template);

	// Creating Admin working dir.
    str_template = L"scadmin";
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
		wcslen(admin_dbname) +		// database uri
		wcslen(server_logs_dir) + 
		1;

    str_size_bytes = str_num_chars * sizeof(wchar_t);
    scdata_cmd = (wchar_t *)malloc(str_size_bytes);
    if (!scdata_cmd) goto err_nomem;

	swprintf(scdata_cmd, str_num_chars, str_template, admin_dbname_upr, admin_dbname, server_logs_dir);

	// Creating sccode command
	str_num_chars = 0;

    // Checking if number of schedulers is defined.
	str_template = L"sccode.exe %s --ServerName=%s --DatabaseDir=\"%s\" --OutputDir=\"%s\" --TempDir=\"%s\" --CompilerPath=\"%s\" --AutoStartExePath=\"%s\" --UserArguments=\"\\\"%s\\\" %s\" --WorkingDir=\"%s\" --DefaultUserHttpPort=%s --SchedulerCount=%s";

    // TODO: Remove the scheduler count at all?
    database_scheduler_count = L"1";

    str_num_chars +=
        wcslen(str_template) + 
		wcslen(admin_dbname_upr) + 
        wcslen(srv_name_upr) + 
		wcslen(database_image_dir) + 
		wcslen(server_logs_dir) + 
		wcslen(database_temp_dir) +
		wcslen(mingw) + 
		wcslen(admin_exe_path) +
		wcslen(server_cfg_path) +
        wcslen(system_http_port) +
		wcslen(admin_working_dir) +
        wcslen(default_user_http_port) +
        wcslen(database_scheduler_count) +
        1;

    str_size_bytes = str_num_chars * sizeof(wchar_t);
    sccode_cmd = (wchar_t *)malloc(str_size_bytes);
    if (!sccode_cmd) goto err_nomem;

	swprintf(
        sccode_cmd,
        str_num_chars,
        str_template,
        admin_dbname_upr,
        srv_name_upr,
        database_image_dir,
        server_logs_dir,
        database_temp_dir,
        mingw,
        admin_exe_path,
        server_cfg_path,
        system_http_port,
        admin_working_dir,
        default_user_http_port,
        database_scheduler_count);

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
	if (r)
    {
        wchar_t* error_msg_buf = new wchar_t[4096];
        FormatStarcounterErrorMessage(r, error_msg_buf, 4096);

        // Logging this error.
        wprintf(L"Exited with error code: %s\n", error_msg_buf);

        if (g_sc_log_handle_)
        {
            wprintf(L"Please review server log in: %s\n", server_logs_dir);
            
            // Logging to log file.
            LogWriteError(error_msg_buf);
        }
	}
	
    if (handles[4]) _kill_and_cleanup(handles[4]);	// SCDODE
    if (handles[3]) _kill_and_cleanup(handles[3]);	// SCDATA
    if (handles[2]) _kill_and_cleanup(handles[2]);	// Gateway
    if (handles[1]) _kill_and_cleanup(handles[1]);	// Monitor
    if (handles[0]) _destroy_event(handles[0]);

    return (int32_t)r;

    // Catching all unhandled exceptions in this thread.
    _SC_END_FUNC
}
