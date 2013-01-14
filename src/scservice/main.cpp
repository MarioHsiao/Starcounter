
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

int wmain(int argc, wchar_t* argv[], wchar_t* envp[])
{
    uint32_t r;

    const wchar_t *name = L"PERSONAL";

    void *handles[4];
    memset(handles, 0, sizeof(handles));

    // Read server configuration.
    wchar_t *server_dir;
    r = _read_service_config(name, &server_dir);
    if (r) goto end;

    wchar_t *name_upr;
    const wchar_t *str_template;
    size_t str_num_chars, str_size_bytes;

    wchar_t *event_name;
    wchar_t *monitor_cmd;
    wchar_t *gateway_cmd;
    wchar_t *admin_cmd;
    wchar_t *server_cfg_path;

    str_num_chars = wcslen(name) + 1;
    str_size_bytes = str_num_chars * sizeof(wchar_t);

    name_upr = (wchar_t *)malloc(str_size_bytes);
    if (!name_upr) goto err_nomem;

    wcscpy_s(name_upr, str_num_chars, name);
    _wcsupr_s(name_upr, str_num_chars);

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
    r = _read_server_config(server_cfg_path, &server_logs_dir);
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

    str_template = L"screferenceserver.exe \"%s\"";
    str_num_chars =
        wcslen(str_template) +
        wcslen(server_cfg_path) +
        1;

    str_size_bytes = str_num_chars * sizeof(wchar_t);
    admin_cmd = (wchar_t *)malloc(str_size_bytes);
    if (!admin_cmd) goto err_nomem;

    swprintf(admin_cmd, str_num_chars, str_template, server_cfg_path);

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
            // Shutdown signaled.
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
