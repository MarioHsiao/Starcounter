#include "internal.h"
#include <windows.h>
#include <stdlib.h>
#include <stdint.h>
#include <stdio.h>
#include <string.h>

extern "C" int32_t make_sc_process_uri(const char *server_name, const char *process_name, wchar_t *buffer, size_t *pbuffer_size);

#define IPC_MONITOR_INHERIT_CONSOLE 0
#define GATEWAY_INHERIT_CONSOLE 0
#define SCDATA_INHERIT_CONSOLE 0
#define SCCODE_INHERIT_CONSOLE 1

// Handle IDs for all processes.
enum ProcessIds
{
    ID_SCSERVICE,
    ID_IPC_MONITOR,
    ID_GATEWAY,
    ID_ADMIN_SCCODE,
    ID_ADMIN_SCDATA,
    ID_PROLOG,
    ID_LAST_ID
};

#define LOG_BUFFER_MESSAGE_SIZE 1024

//#define WITH_DATABASE // if defined the scservice requires a database with the name "administrator"
//#define START_PROLOG

static void *hcontrol_event;

// Global handle to server log.
uint64_t g_sc_log_handle_ = 0;
int32_t logsteps = 0; // 0 = Normal, >0 = Log to console and into logfile when it's available

static void __shutdown_event_handler()
{
	_set_event(hcontrol_event);
}

// Is called when scservice crashes.
VOID LogGatewayCrash(VOID *pc, LPCWSTR str)
{
	LogWriteCritical(str);
}

VOID PrintCommandHelp() {
    wprintf(L"scservice.exe [ServerName] [--logsteps]\n");
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
}

int wmain(int argc, wchar_t* argv[], wchar_t* envp[])
{
    BOOL exit_code_is_scerr;
    DWORD process_exit_code;
	wchar_t logmessagebuffer[LOG_BUFFER_MESSAGE_SIZE];

	// Catching all unhandled exceptions in this thread.
	_SC_BEGIN_FUNC
	_SetCriticalLogHandler(LogGatewayCrash, NULL);

	uint32_t r;
	const wchar_t *srv_name = L"PERSONAL";
	const char *srv_name_ascii = "PERSONAL";

    process_exit_code = 0;

	if (argc > 1)
	{
		// Checking if help is needed.
		if ((0 == wcscmp(argv[1], L"?")) ||
            (0 == wcscmp(argv[1], L"-h")) ||
            (0 == wcscmp(argv[1], L"--help")) ||
            (argc > 3))
		{
			PrintCommandHelp();
			return 0;
		}

		for (int i = 1; i < argc; i++)
		{
			if (0 == wcscmp(argv[i], L"--logsteps"))
			{
				logsteps = 1;
			}
            else
            {
                srv_name = argv[i];
            }
		}
	}

	if(logsteps != 0 ) { 
		LogVerboseMessage(L"Have entered scservice.exe");
	}

	wprintf(L"Starting Starcounter %s engine...\n", srv_name);

	// Getting executable directory.
	if(logsteps != 0 ) { 
		LogVerboseMessage(L"Getting executable directory.");
	}

	wchar_t exe_dir[1024];
	r = GetModuleFileName(NULL, exe_dir, 1024);
	if ((r == 0) || (r >= 1024))
		goto log_winerr;

	if(logsteps != 0 ) { 
		_snwprintf_s(logmessagebuffer,_countof(logmessagebuffer),L"Got the executable directory: %s", exe_dir);
		LogVerboseMessage(logmessagebuffer);
	}

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
	if(logsteps != 0 ) { 
		LogVerboseMessage(L"Setting the current directory");
	}
	if (!SetCurrentDirectory(exe_dir))
		goto log_winerr;

	if(logsteps != 0 ) { 
		_snwprintf_s(logmessagebuffer,_countof(logmessagebuffer),L"Current directory set to %s", exe_dir);
		LogVerboseMessage(logmessagebuffer);
	}

	const wchar_t *admin_dbname = L"administrator";	
	const char *admin_dbname_ascii = "administrator";	
	const wchar_t *mingw = L"MinGW\\bin\\x86_64-w64-mingw32-gcc.exe";

	void *handles[ID_LAST_ID];
	memset(handles, 0, sizeof(handles));

	// Read server configuration.
	wchar_t *server_dir;

	if(logsteps != 0 ) { 
		_snwprintf_s(logmessagebuffer,_countof(logmessagebuffer),L"About to read config file %s", srv_name);
		LogVerboseMessage(logmessagebuffer);
	}

	r = _read_service_config(srv_name, &server_dir);
	if (r) goto log_scerr;

	if(logsteps != 0 ) { 
		_snwprintf_s(logmessagebuffer,_countof(logmessagebuffer),L"Config file %s read", srv_name);
		LogVerboseMessage(logmessagebuffer);
	}

	wchar_t *srv_name_upr;
	wchar_t *admin_dbname_upr;
	const wchar_t *str_template;
	size_t str_num_chars, str_size_bytes;

	wchar_t *event_name;
	wchar_t *monitor_cmd;
	wchar_t *gateway_cmd;

#ifdef START_PROLOG
    wchar_t *prolog_cmd;
#endif

#ifdef WITH_DATABASE
	wchar_t *scdata_cmd;
    wchar_t *admin_dburi;
#endif

	wchar_t *sccode_cmd;
	wchar_t *admin_exe_path;
	wchar_t *admin_working_dir;
	wchar_t *database_cfg_path;
	wchar_t *server_cfg_path;
	wchar_t *admin_logsteps_flag = logsteps != 0 ? L"--LogSteps" : L"";

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

#ifdef WITH_DATABASE
	str_num_chars = 0;
	make_sc_process_uri(srv_name_ascii, admin_dbname_ascii, 0, &str_num_chars);
	admin_dburi = (wchar_t *)malloc(str_num_chars * sizeof(wchar_t));
	if (!admin_dburi) goto err_nomem;
	make_sc_process_uri(srv_name_ascii, admin_dbname_ascii, admin_dburi, &str_num_chars);
#endif

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

	if(logsteps != 0 ) { 
		_snwprintf_s(logmessagebuffer,_countof(logmessagebuffer),L"About to read config file %s", server_cfg_path);
		LogVerboseMessage(logmessagebuffer);
	}

	// Reading server logs directory.
	wchar_t *server_logs_dir;
	wchar_t *server_temp_dir;
	wchar_t *server_database_dir;
	wchar_t *system_http_port;
	wchar_t *default_user_http_port;
    wchar_t *prolog_port;

	r = _read_server_config(
		server_cfg_path,
		&server_logs_dir,
		&server_temp_dir,
		&server_database_dir,
		&system_http_port,
		&default_user_http_port,
        &prolog_port);

	if (r) goto log_scerr;

	if(logsteps != 0 ) { 
		_snwprintf_s(logmessagebuffer,_countof(logmessagebuffer),L"Config file %s read", server_cfg_path);
		LogVerboseMessage(logmessagebuffer);
	}

	if(logsteps != 0 ) { 
		_snwprintf_s(logmessagebuffer,_countof(logmessagebuffer),L"About to open the log in %s", server_logs_dir );
		LogVerboseMessage(logmessagebuffer);
	}

	// Registering the logger.
	r = OpenStarcounterLog(srv_name_ascii, server_logs_dir);
	if (r) goto log_winerr;

	if(logsteps != 0 ) { 
		_snwprintf_s(logmessagebuffer,_countof(logmessagebuffer),L"Log opened");
		LogVerboseMessage(logmessagebuffer);
	}

    // Check if there is any starcounter processes still running without an existing 
    // parent. This can happen for example if scservice.exe was closed down manually.
    _kill_and_cleanup_orphaned_children(logsteps);
    
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

#ifdef WITH_DATABASE
	wchar_t *database_logs_dir;
	wchar_t *database_temp_dir;
	wchar_t *database_image_dir;
	wchar_t *database_scheduler_count;

	if(logsteps != 0 ) { 
		_snwprintf_s(logmessagebuffer,_countof(logmessagebuffer),L"About to read config file %s", database_cfg_path);
		LogVerboseMessage(logmessagebuffer);
	}

	// Reading database configuration
	r = _read_database_config(database_cfg_path, &database_logs_dir, &database_temp_dir, &database_image_dir, &database_scheduler_count);
	if (r) goto log_scerr;

	if(logsteps != 0 ) { 
		_snwprintf_s(logmessagebuffer,_countof(logmessagebuffer),L"Config file %s read", database_cfg_path);
		LogVerboseMessage(logmessagebuffer);
	}
#endif

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

	str_template = L"scnetworkgateway.exe \"%s\" \"%s\\scnetworkgateway.xml\" \"%s\"";
	str_num_chars =
		wcslen(str_template) +
		wcslen(srv_name_upr) +
        wcslen(server_dir) +
		wcslen(server_logs_dir) +
		1;

	str_size_bytes = str_num_chars * sizeof(wchar_t);
	gateway_cmd = (wchar_t *)malloc(str_size_bytes);
	if (!gateway_cmd) goto err_nomem;

	swprintf(gateway_cmd, str_num_chars, str_template, srv_name_upr, server_dir, server_logs_dir);

#ifdef START_PROLOG

    str_template = L"32BitComponents\\scsqlparser.exe %s";
    str_num_chars =
        wcslen(str_template) +
        wcslen(prolog_port) +
        1;

    str_size_bytes = str_num_chars * sizeof(wchar_t);
    prolog_cmd = (wchar_t *)malloc(str_size_bytes);
    if (!prolog_cmd) goto err_nomem;

    swprintf(prolog_cmd, str_num_chars, str_template, prolog_port);

#endif

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

#ifdef WITH_DATABASE
	// Creating scdata command
	str_template = L"scdata.exe %s %s \"%s\"";
	str_num_chars = 
		wcslen(str_template) + 
		wcslen(admin_dbname_upr) +	// database name uppercase
		wcslen(admin_dburi) +		// database uri
		wcslen(server_logs_dir) + 
		1;

	str_size_bytes = str_num_chars * sizeof(wchar_t);
	scdata_cmd = (wchar_t *)malloc(str_size_bytes);
	if (!scdata_cmd) goto err_nomem;

	swprintf(scdata_cmd, str_num_chars, str_template, admin_dbname_upr, admin_dburi, server_logs_dir);
#endif

	// Creating sccode command
	str_num_chars = 0;

	// Checking if number of schedulers is defined.
#ifdef WITH_DATABASE
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

#else
	str_template = L"sccode.exe %s --ServerName=%s --OutputDir=\"%s\" --TempDir=\"%s\" --CompilerPath=\"%s\" --AutoStartExePath=\"%s\" --UserArguments=\"\\\"%s\\\"\" --WorkingDir=\"%s\" --DefaultSystemHttpPort=%s --DefaultUserHttpPort=%s --FLAG:NoDb %s";

	str_num_chars +=
		wcslen(str_template) + 
		wcslen(admin_dbname_upr) +			// APP name
		wcslen(srv_name_upr) +				// ServerName
		wcslen(server_logs_dir) +			// OutputDir
		wcslen(server_temp_dir) +			// TempDir
		wcslen(mingw) +						// CompilerPath
		wcslen(admin_exe_path) +			// AutoStartExePath
		wcslen(server_cfg_path) +			// UserArguments
		wcslen(admin_working_dir) +			// WorkingDir
        wcslen(system_http_port) +	        // DefaultSystemHttpPort
		wcslen(default_user_http_port) +	// DefaultUserHttpPort
		wcslen(admin_logsteps_flag) +		// --LogSteps (or "" if not set)
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
		server_logs_dir,
		server_temp_dir,
		mingw,
		admin_exe_path,
		server_cfg_path,
		admin_working_dir,
        system_http_port,
		default_user_http_port,
		admin_logsteps_flag);
#endif

	// Create shutdown event. Will fail if event already exists and so also
	// confirm that no server with the specific name already is running.

	if (logsteps != 0 ) { 
		_snwprintf_s(logmessagebuffer,_countof(logmessagebuffer),L"About to create shutdown event listener %s", event_name);
		LogVerboseMessage(logmessagebuffer);
	}

	r = _create_event(event_name, &hcontrol_event);
	if (r) goto log_winerr;

	if (logsteps != 0 ) { 
		_snwprintf_s(logmessagebuffer,_countof(logmessagebuffer),L"Event listener %s created", event_name);
		LogVerboseMessage(logmessagebuffer);
	}

	if (logsteps != 0 ) { 
		LogVerboseMessage(L"About to create console ctrl-c event listener");
	}

	if (_set_shutdown_event_handler(__shutdown_event_handler) == false ) {
		goto log_winerr;
	}

	if (logsteps != 0 ) { 
		LogVerboseMessage(L"Event listener created");
	}

	handles[ID_SCSERVICE] = hcontrol_event;

	if (logsteps != 0 ) { 
		_snwprintf_s(logmessagebuffer,_countof(logmessagebuffer),L"About to start the IPC monitor: %s", monitor_cmd);
		LogVerboseMessage(logmessagebuffer);
	}

	// Start and register IPC monitor.
	r = _exec(monitor_cmd, IPC_MONITOR_INHERIT_CONSOLE, &handles[ID_IPC_MONITOR]);
	if (r) goto log_winerr;

	if (logsteps != 0 ) { 
		LogVerboseMessage(L"IPC monitor started");
	}

	if (logsteps != 0 ) { 
		_snwprintf_s(logmessagebuffer,_countof(logmessagebuffer),L"About to start the Network gateway: %s", gateway_cmd);
		LogVerboseMessage(logmessagebuffer);
	}

	// Start and register network gateway.
	r = _exec(gateway_cmd, GATEWAY_INHERIT_CONSOLE, &handles[ID_GATEWAY]);
	if (r) goto log_winerr;

	if (logsteps != 0 ) { 
		LogVerboseMessage(L"Network gateway started");
	}

#ifdef START_PROLOG

    if (logsteps != 0 ) { 
        _snwprintf_s(logmessagebuffer,_countof(logmessagebuffer),L"About to start the Prolog SQL: %s", prolog_cmd);
        LogVerboseMessage(logmessagebuffer);
    }

    // Start and register Prolog.
    r = _exec(prolog_cmd, 0, &handles[ID_PROLOG]);
    if (r) goto log_winerr;

    if (logsteps != 0 ) { 
        LogVerboseMessage(L"Prolog SQL started");
    }

#endif

#ifdef WITH_DATABASE
	if (logsteps != 0 ) { 
		_snwprintf_s(logmessagebuffer,_countof(logmessagebuffer),L"About to start the database: %s", scdata_cmd);
		LogVerboseMessage(logmessagebuffer);
	}
	r = _exec(scdata_cmd, SCDATA_INHERIT_CONSOLE, &handles[ID_ADMIN_SCDATA]);
	if (r) goto log_scerr;

	if (logsteps != 0 ) { 
		LogVerboseMessage(L"Database started");
	}
#endif

	if (logsteps != 0 ) { 
		_snwprintf_s(logmessagebuffer,_countof(logmessagebuffer),L"About to start sccode: %s", sccode_cmd);
		LogVerboseMessage(logmessagebuffer);
	}
	r = _exec(sccode_cmd, SCCODE_INHERIT_CONSOLE, &handles[ID_ADMIN_SCCODE]);
	if (r) goto log_winerr;

	if (logsteps != 0 ) { 
		LogVerboseMessage(L"sccode started");
	}

	// Wait for signal.
	for (;;)    
	{
		uint32_t signaled_index;

		if (logsteps != 0 ) { 
			LogVerboseMessage(L"scservice done (waiting)");
		}

		r = _wait(handles, ID_LAST_ID, &signaled_index);
		if (r) goto log_winerr;

		if (logsteps != 0 ) { 
			LogVerboseMessage(L"scservice got event signal");
		}

		switch (signaled_index)
		{
		    case ID_SCSERVICE:
            {
			    // Shutdown signaled.

			    if (logsteps != 0 ) { 
				    LogVerboseMessage(L"scservice got shutdown signal");
			    }
			    goto end;
            }

		    case ID_IPC_MONITOR:
            {
			    // IPC monitor died. Kill the server.
			    if (logsteps != 0 ) { 
				    LogVerboseMessage(L"IPC monitor exited");
			    }
                GetExitCodeProcess(handles[ID_IPC_MONITOR], &process_exit_code);
                exit_code_is_scerr = false;
                r = SCERRIPCMONITORTERMINATED;
			    goto log_scerr;
            }

		    case ID_GATEWAY:
            {
			    // Gateway died. Kill the server. Kill the system.
			    if (logsteps != 0 ) { 
				    LogVerboseMessage(L"Network gateway exited");
			    }
                GetExitCodeProcess(handles[ID_GATEWAY], &process_exit_code);
                exit_code_is_scerr = false;
                r = SCERRNETWORKGATEWAYTERMINATED;
			    goto log_scerr;
            }

		    case ID_ADMIN_SCCODE:
            {
			    // sccode died. Kill the server. Kill the system.
			    if (logsteps != 0 ) { 
				    LogVerboseMessage(L"sccode exited");
			    }

                GetExitCodeProcess(handles[ID_ADMIN_SCCODE], &process_exit_code);
                exit_code_is_scerr = true;
                r = SCERRDATABASEENGINETERMINATED;
			    goto log_scerr;
            }

            case ID_PROLOG:
            {
                // scsqlparser died. Kill the server. Kill the system.
                if (logsteps != 0 ) { 
                    LogVerboseMessage(L"scsqlparser exited");
                }

                GetExitCodeProcess(handles[ID_PROLOG], &process_exit_code);
                exit_code_is_scerr = true;
                r = SCERRDATABASEENGINETERMINATED;
                goto log_scerr;
            }

		    case ID_ADMIN_SCDATA:
            {
			    if (logsteps != 0 ) { 
				    LogVerboseMessage(L"scdata exited");
			    }

			    // scdata died. Kill the server. Kill the system.
    		    goto end;
            }

		    default:
			    __assume(0);
		}
	}

err_nomem:
	r = SCERROUTOFMEMORY;
	
log_scerr:
	if (r)
	{
        wchar_t* error_msg_buf = new wchar_t[4096];
		FormatStarcounterErrorMessage(r, error_msg_buf, 4096);
        
		// Logging this error.
		wprintf(L"Error: %s\n", error_msg_buf);

		if (g_sc_log_handle_)
		{
			wprintf(L"Please review server log in: %s\n", server_logs_dir);

			// Logging to log file.
			LogWriteError(error_msg_buf);

            if ((process_exit_code > 0) && (process_exit_code != MAXDWORD)) {
                if (exit_code_is_scerr && process_exit_code > 1) 
                    FormatStarcounterErrorMessage(process_exit_code, error_msg_buf, 4096);
                else 
                    swprintf(error_msg_buf, 100, L"Process exitcode: %i", process_exit_code);
                LogWriteError(error_msg_buf);
            }
		}
	}
    goto end;

log_winerr:
	if (r)
	{
        wchar_t* error_msg_buf = new wchar_t[512];
		wsprintf(error_msg_buf, L"Error: process exited with error code: %s\n", r);

		// Logging this error.
		wprintf(error_msg_buf);

		if (g_sc_log_handle_)
		{
			wprintf(L"Please review server log in: %s\n", server_logs_dir);

			// Logging to log file.
			LogWriteError(error_msg_buf);
		}
	}

end:
	if (handles[ID_ADMIN_SCDATA]) _kill_and_cleanup(handles[ID_ADMIN_SCDATA]);
	if (handles[ID_ADMIN_SCCODE]) _kill_and_cleanup(handles[ID_ADMIN_SCCODE]);
	if (handles[ID_GATEWAY]) _kill_and_cleanup(handles[ID_GATEWAY]);
    if (handles[ID_PROLOG]) _kill_and_cleanup(handles[ID_PROLOG]);
	if (handles[ID_IPC_MONITOR]) _kill_and_cleanup(handles[ID_IPC_MONITOR]);
	if (handles[ID_SCSERVICE]) _destroy_event(handles[ID_SCSERVICE]);

	return (int32_t)r;

	// Catching all unhandled exceptions in this thread.
	_SC_END_FUNC
}
