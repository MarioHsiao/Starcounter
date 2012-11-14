
#include "internal.h"

#include <malloc.h>
#include <stdio.h>
#include <stdint.h>
#include "rapidxml.hpp"


uint32_t _read_config(const char *name, char **pserver_dir)
{
	using namespace rapidxml;

	uint32_t r;
	char *file_name;
	FILE *file;
	char *config_data;

	r = SCERRBADSERVICECONFIG;

	file_name = 0;
	file = 0;
	config_data = 0;

	// TODO: Handle rapidxml error (exception thrown).

	xml_document<> doc; // Character type defaults to char.

	file_name = (char *)malloc(strlen(name) + 4 + 1);
	if (!file_name) goto end;
#pragma warning (disable: 4996)
	sprintf(file_name, "%s.xml", name);
#pragma warning (default: 4996)

	errno_t errno;
	int32_t file_size;
	size_t read;

	errno = fopen_s(&file, file_name, "r");
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

	doc.parse<0>(config_data); // 0 means default parse flags.

	xml_node<> *root_elem = doc.first_node("service");
	if (!root_elem) goto end;

	xml_node<> *server_dir_elem = root_elem->first_node("server-dir");
	if (!server_dir_elem) goto end;

	*pserver_dir = (char *)malloc(server_dir_elem->value_size() + 1);
#pragma warning (disable: 4996)
	strcpy(*pserver_dir, server_dir_elem->value());
#pragma warning (default: 4996)

	r = 0;

end:
	if (config_data) free(config_data);
	if (file) fclose(file);
	if (file_name) free(file_name);
	return r;
}
