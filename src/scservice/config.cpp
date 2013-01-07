
#include "internal.h"

#include <windows.h>
#include <malloc.h>
#include <stdio.h>
#include <stdint.h>
#include "rapidxml.hpp"

uint32_t _read_service_config(const wchar_t *name, wchar_t **pserver_dir)
{
    using namespace rapidxml;

    uint32_t r;
    wchar_t *file_name;
    FILE *file;
    char *config_data;
    xml_document<> *doc; // Character type defaults to char.
    size_t str_num_chars;
    size_t str_size_bytes;

    r = SCERRBADSERVICECONFIG;

    file_name = 0;
    file = 0;
    config_data = 0;
    doc = 0;

    str_num_chars = wcslen(name) + 4 + 1;
    str_size_bytes = str_num_chars * sizeof(wchar_t);

    file_name = (wchar_t *)malloc(str_size_bytes);
    if (!file_name) goto end;

    swprintf(file_name, str_num_chars, L"%s.xml", name);

    errno_t errno;
    int32_t file_size;
    size_t read;

    errno = _wfopen_s(&file, file_name, L"r");
    if (errno) goto end;
    fseek(file, 0, SEEK_END);
    file_size = ftell(file);
    fseek(file, 0, SEEK_SET);

    config_data = (char *)malloc(file_size + 1);
    if (!config_data) goto end;

    read = fread(config_data, 1, file_size, file);
    config_data[read] = 0;
    fclose(file);
    file = 0;

    // TODO: Handle rapidxml error (exception thrown).

    doc = new xml_document<>; // Character type defaults to char.
    if (!doc) goto end;

    try
    {
        doc->parse<0>(config_data); // 0 means default parse flags.
    }
    catch (parse_error)
    {
        goto end;
    }

    xml_node<> *root_elem = doc->first_node("service");
    if (!root_elem) goto end;

    xml_node<> *server_dir_elem = root_elem->first_node("server-dir");
    if (!server_dir_elem) goto end;

    str_num_chars = server_dir_elem->value_size() + 1;
    str_size_bytes = str_num_chars * sizeof(wchar_t);
    *pserver_dir = (wchar_t *)malloc(str_size_bytes);
    if (!*pserver_dir) goto end;

    // Converting server directory from UTF-8 to wchar_t.
    int32_t num_chars_converted = MultiByteToWideChar(CP_UTF8, 0, server_dir_elem->value(), -1, *pserver_dir, (int)str_num_chars);
    if (0 == num_chars_converted) goto end;

    r = 0;

end:
    if (doc) delete doc;
    if (config_data) free(config_data);
    if (file) fclose(file);
    if (file_name) free(file_name);
    return r;
}

uint32_t _read_server_config(const wchar_t *server_config_path, wchar_t **pserver_logs_dir)
{
    using namespace rapidxml;

    uint32_t r;
    FILE *file;
    char *config_data;
    xml_document<> *doc; // Character type defaults to char.
    size_t str_num_chars;
    size_t str_size_bytes;

    r = SCERRBADSERVERCONFIG;

    file = 0;
    config_data = 0;
    doc = 0;

    errno_t errno;
    int32_t file_size;
    size_t read;

    errno = _wfopen_s(&file, server_config_path, L"r");
    if (errno) goto end;
    fseek(file, 0, SEEK_END);
    file_size = ftell(file);
    fseek(file, 0, SEEK_SET);

    config_data = (char *)malloc(file_size + 1);
    if (!config_data) goto end;

    read = fread(config_data, 1, file_size, file);
    config_data[read] = 0;
    fclose(file);
    file = 0;

    // TODO: Handle rapidxml error (exception thrown).

    doc = new xml_document<>; // Character type defaults to char.
    if (!doc) goto end;

    try
    {
        doc->parse<0>(config_data); // 0 means default parse flags.
    }
    catch (parse_error)
    {
        goto end;
    }

    xml_node<> *root_elem = doc->first_node("Server");
    if (!root_elem) goto end;

    xml_node<> *server_log_dir_elem = root_elem->first_node("LogDirectory");
    if (!server_log_dir_elem) goto end;

    str_num_chars = server_log_dir_elem->value_size() + 1;
    str_size_bytes = str_num_chars * sizeof(wchar_t);
    *pserver_logs_dir = (wchar_t *)malloc(str_size_bytes);
    if (!*pserver_logs_dir) goto end;

    // Converting server directory from UTF-8 to wchar_t.
    int32_t num_chars_converted = MultiByteToWideChar(CP_UTF8, 0, server_log_dir_elem->value(), -1, *pserver_logs_dir, (int)str_num_chars);
    if (0 == num_chars_converted) goto end;

    r = 0;

end:
    if (doc) delete doc;
    if (config_data) free(config_data);
    if (file) fclose(file);
    return r;
}