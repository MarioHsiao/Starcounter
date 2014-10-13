//
// shared_memory_object.cpp
// Network Gateway
//
// Copyright © 2006-2012 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#include "interprocess.hpp"
#include "config_param.hpp"
#include "macro_definitions.hpp"
/* #include <boost/thread/thread.hpp> /// thread debug info */

using namespace starcounter::core;

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <iostream> /// debug

#ifdef _MSC_VER
# pragma warning(push)
// This function or variable may be unsafe.
# pragma warning(disable: 4996)
#endif // _MSC_VER

shared_memory_object::~shared_memory_object() {
	if (handle_) {
		CloseHandle(handle_);
		handle_ = 0;
	}
	if (file_ != INVALID_HANDLE_VALUE) {
		CloseHandle(file_);
		file_ = INVALID_HANDLE_VALUE;
	}
}

void shared_memory_object::init_create(const char *name, uint32_t size, bool
is_system, mapping mapping, const char* db_data_dir_path) {
	char name_with_prefix[8 /* "Global\\" or "Local\\" will fit */
	+sizeof(DATABASE_NAME_PREFIX) +interface_name_size];
	name_with_prefix[0] = '\0';
	char sec_desc_arr[SECURITY_DESCRIPTOR_MIN_LENGTH];
	DWORD dr;
	SECURITY_ATTRIBUTES sa;
	wchar_t w_db_data_dir_path_and_name[maximum_path_and_file_name_length];
	char db_data_dir_path_and_name[maximum_path_and_file_name_length];
	std::size_t length;
	
	handle_ = 0;
	
	// The current permission set is a read/write free for all. If we
	// need a more finegrained set we need to change this code.
	sa.bInheritHandle = FALSE;
	sa.nLength = sizeof(SECURITY_ATTRIBUTES);
	sa.lpSecurityDescriptor = &sec_desc_arr;
	InitializeSecurityDescriptor(sa.lpSecurityDescriptor,
	SECURITY_DESCRIPTOR_REVISION);
	SetSecurityDescriptorDacl(sa.lpSecurityDescriptor, TRUE, 0, FALSE);
	
	if (size > 0 && size < 0x7FFFFFFF)
	{
		// Default mapping is memory_mapped.
		if (mapping == file_mapped) {
			// Concatenate the db_data_dir_path and the name.
			if ((length = _snprintf_s(db_data_dir_path_and_name,
			_countof(db_data_dir_path_and_name),
			maximum_path_and_file_name_length -1 /* null */,
			"%s%s", db_data_dir_path, name))
			< 0) {
				return; // error
			}
			db_data_dir_path_and_name[length] = '\0';
			
			/// TODO: Fix insecure
			if ((length = mbstowcs(w_db_data_dir_path_and_name,
			db_data_dir_path_and_name, maximum_path_and_file_name_length)) < 0)
			{
				return; // error
			}
			
			w_db_data_dir_path_and_name[length] = L'\0';
			
			file_ = CreateFile(/*(LPCTSTR)*/ w_db_data_dir_path_and_name,
			GENERIC_READ | GENERIC_WRITE,
			FILE_SHARE_READ | FILE_SHARE_WRITE /*| FILE_SHARE_DELETE*/,
			&sa,
			OPEN_ALWAYS,
			FILE_ATTRIBUTE_NORMAL,
			NULL);
		}
		// If database is started in system mode we create a global
		// namespace otherwise we only use local namespace.
		if (is_system)
		{
			length = _snprintf_s(name_with_prefix, 
					_countof(name_with_prefix), 
					sizeof(name_with_prefix) -1 /* null */, 
					"Global\\%s", 
					name);
		}
		else
		{
			length = _snprintf_s(name_with_prefix, 
					_countof(name_with_prefix), 
					sizeof(name_with_prefix) -1 /* null */, 
					"Local\\%s", 
					name);
		}
		if (length < 0)
		{
			// TODO: 
			// Handle error somehow.
			return;
		}
		name_with_prefix[length] = '\0';
		handle_ = CreateFileMappingA(file_, &sa, PAGE_READWRITE, 0, size,
		name_with_prefix);
		
		if (handle_)
		{
			dr = GetLastError();
			if (dr != ERROR_ALREADY_EXISTS) {
				return;
			}
			if (CloseHandle(handle_) == false) {
			}
			handle_ = 0;
		}
	}
}

#if 0 /// takeback
void shared_memory_object::init_open(const char* name, mapping mapping, const
char* db_data_dir_path) {
	std::size_t length;
	
	if (mapping == file_mapped) {
		// File mapped.
		char db_data_dir_path_and_name[maximum_path_and_file_name_length];
		wchar_t w_db_data_dir_path_and_name[maximum_path_and_file_name_length];
		char sec_desc_arr[SECURITY_DESCRIPTOR_MIN_LENGTH];
		SECURITY_ATTRIBUTES sa;
		// The current permission set is a read/write free for all. If we need a
		// more finegrained set we need to change this code.
		sa.bInheritHandle = FALSE;
		sa.nLength = sizeof(SECURITY_ATTRIBUTES);
		sa.lpSecurityDescriptor = &sec_desc_arr;
		InitializeSecurityDescriptor(sa.lpSecurityDescriptor,
		SECURITY_DESCRIPTOR_REVISION);
		SetSecurityDescriptorDacl(sa.lpSecurityDescriptor, TRUE, 0, FALSE);
		
		// Concatenate the db_data_dir_path and the name.
		if ((length = _snprintf_s(db_data_dir_path_and_name,
		_countof(db_data_dir_path_and_name),
		maximum_path_and_file_name_length -1 /* null */,
		"%s%s", db_data_dir_path, name)) < 0) {
			return; // error
		}
		
		db_data_dir_path_and_name[length] = '\0';
		
		/// TODO: Fix insecure
		if ((length = mbstowcs(w_db_data_dir_path_and_name,
		db_data_dir_path_and_name, maximum_path_and_file_name_length)) < 0) {
			return; // error
		}
		
		w_db_data_dir_path_and_name[length] = L'\0';
		
		file_ = CreateFile((LPCTSTR) w_db_data_dir_path_and_name,
		GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, &sa,
		OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
		
		uint32_t err = GetLastError();
		
		if (err == ERROR_FILE_NOT_FOUND) {
			/// If the specified file or device does not exist, CreateFile()
			/// fails and the last-error code is set to ERROR_FILE_NOT_FOUND.
			return;
		}

		handle_ = CreateFileMapping(file_, NULL, PAGE_READWRITE, 0, 4096, NULL);
		if (!handle_)
		{
			err = GetLastError();
		}
	}
	else {
		// Memory mapped.
		
		// Try to open the shared_memory_object with the "Global" prefix.
		char name_with_prefix[128];
		
		if ((length = _snprintf_s(name_with_prefix, _countof(name_with_prefix),
		sizeof(name_with_prefix) -1 /* null */, "Global\\%s", name)) < 0) {
			return; // error
		}
		name_with_prefix[length] = '\0';
		uint32_t err = 0;
		
		if ((handle_ = OpenFileMappingA(FILE_MAP_READ | FILE_MAP_WRITE, FALSE,
		name_with_prefix)) == 0) {
			if ((err = GetLastError()) == ERROR_FILE_NOT_FOUND) {
				// "Global" failed. Try with the "Local" prefix.
				if ((length = _snprintf_s(name_with_prefix,
				_countof(name_with_prefix), sizeof(name_with_prefix) -1
				/* null */, "Local\\%s", name)) < 0) {
					return; // error
				}
				name_with_prefix[length] = '\0';
				err = 0;
				if ((handle_ = OpenFileMappingA(FILE_MAP_READ | FILE_MAP_WRITE,
				FALSE, name_with_prefix)) == 0) {
					// The "Local" prefix also failed.
					err = GetLastError();
				}
			}
		}
	}
}
#endif /// takeback

#if 1 /// Before I modified it
void shared_memory_object::init_open(const char *name)
{
	// Try to open the shared_memory_object with the "Global" prefix.
	char name_with_prefix[128];
	std::size_t length;
	
	if ((length = _snprintf_s(name_with_prefix, _countof(name_with_prefix),
	sizeof(name_with_prefix) -1 /* null */, "Global\\%s", name)) < 0) {
		return; // error
	}
	name_with_prefix[length] = '\0';
	uint32_t err = 0;
	
	if ((handle_ = OpenFileMappingA(FILE_MAP_READ | FILE_MAP_WRITE, FALSE,
	name_with_prefix)) == 0) {
		if ((err = GetLastError()) == ERROR_FILE_NOT_FOUND) {
			// "Global" failed. Try with the "Local" prefix.
			if ((length = _snprintf_s(name_with_prefix,
			_countof(name_with_prefix), sizeof(name_with_prefix) -1 /* null */,
			"Local\\%s", name)) < 0) {
				return; // error
			}
			name_with_prefix[length] = '\0';
			err = 0;
			if ((handle_ = OpenFileMappingA(FILE_MAP_READ | FILE_MAP_WRITE,
			FALSE, name_with_prefix)) == 0) {
				// The "Local" prefix also failed.
				err = GetLastError();
			}
		}
	}
}
#endif /// Before I modified it

#ifdef _MSC_VER
# pragma warning(pop)
#endif // _MSC_VER
