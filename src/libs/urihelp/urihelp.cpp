
#include <stdint.h>


extern "C" int32_t make_sc_process_uri(const char *server_name, const char *process_name, wchar_t *buffer, size_t *pbuffer_size);
extern "C" int32_t make_sc_server_uri(const char *server_name, wchar_t *buffer, size_t *pbuffer_size);


#define WIN32_LEAN_AND_MEAN
#include <Windows.h>
#include <wchar.h>


int32_t make_sc_process_uri(const char *server_name, const char *process_name, wchar_t *buffer, size_t *pbuffer_size)
{
	uint32_t computer_name_size;
	char computer_name[MAX_COMPUTERNAME_LENGTH + 1];
	
	computer_name_size = MAX_COMPUTERNAME_LENGTH + 1;
	
	if(	GetComputerNameA(computer_name, (DWORD *)&computer_name_size) == 0 ) {
		return GetLastError();
	}

	size_t buffer_size_needed;
	size_t buffer_size;
	
	buffer_size_needed =
		5 + // "sc://"
		computer_name_size +
		1 + // "/"
		strlen(server_name) +
		1 + // "/"
		strlen(process_name) +
		1
		;
	buffer_size = *pbuffer_size;

	*pbuffer_size = (uint32_t)buffer_size_needed;

	if (buffer_size_needed <= buffer_size)
	{
#pragma warning (disable:4996)
		swprintf(buffer, L"sc://%S/%S/%S", computer_name, server_name, process_name);
		wcslwr(buffer);
#pragma warning (default:4996)
		return 0;
	}
	
	return 0;
}

int32_t make_sc_server_uri(const char *server_name, wchar_t *buffer, size_t *pbuffer_size)
{
	uint32_t computer_name_size;
	char computer_name[MAX_COMPUTERNAME_LENGTH + 1];
	
	computer_name_size = MAX_COMPUTERNAME_LENGTH;
	if( GetComputerNameA(computer_name, (DWORD *)&computer_name_size) == 0 ) {
		return GetLastError();
	}

	size_t buffer_size_needed;
	size_t buffer_size;
	
	buffer_size_needed =
		5 + // "sc://"
		computer_name_size +
		1 + // "/"
		strlen(server_name) +
		1
		;
	buffer_size = *pbuffer_size;

	*pbuffer_size = (uint32_t)buffer_size_needed;

	if (buffer_size_needed <= buffer_size)
	{
#pragma warning (disable:4996)
		swprintf(buffer, L"sc://%S/%S", computer_name, server_name);
		wcslwr(buffer);
#pragma warning (default:4996)
		return 1;
	}
	
	return 0;
}
