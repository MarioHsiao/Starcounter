//
// log.hpp
//
// Copyright © 2006-2013 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#ifndef STARCOUNTER_LOG_HPP
#define STARCOUNTER_LOG_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#include <vector>
#include <cstdint>
#if defined(_MSC_VER)
# define WIN32_LEAN_AND_MEAN
# include <windows.h>
# undef WIN32_LEAN_AND_MEAN
#endif // defined(_MSC_VER)
#include <stdlib.h>
#include <stdint.h>
#include <stdio.h>
#include <string.h>

//---
#include "../../Starcounter.ErrorCodes/scerrres/scerrres.h"
#include "../../Starcounter.ErrorCodes/scerrres/format.h"

// Level0 includes.
#include "../../../../Level0/src/include/sccorelog.h"
#include "../../../../Level0/src/include/sccoredbg.h"
#include "../../../../Level0/src/include/sccorelib.h"

////extern uint32_t _exec(wchar_t *command_line, int32_t inherit_console, void **phandle);
////extern uint32_t _wait(void **handles, uint32_t count, uint32_t *psignaled_index);
////extern void _kill_and_cleanup(void *handle);

////extern uint32_t _read_service_config(const wchar_t *name, wchar_t **pserver_dir);

////extern uint32_t _read_server_config(
////const wchar_t *server_config_path,
////wchar_t **pserver_logs_dir,
////wchar_t **pserver_temp_dir,
////wchar_t **pserver_database_dir,
////wchar_t **psystem_http_port,
////wchar_t **pdefault_user_http_port);

////extern uint32_t _read_database_config(
////const wchar_t *database_config_path,
////wchar_t **pdatabase_logs_dir,
////wchar_t **pdatabase_temp_dir,
////wchar_t **pdatabase_image_dir,
////wchar_t **pdatabase_scheduler_count);
//---

extern "C" int32_t make_sc_process_uri(const char* server_name,
const char* process_name, wchar_t* buffer, size_t* pbuffer_size);

////extern "C" int32_t make_sc_server_uri(const char* server_name,
////wchar_t* buffer, size_t* pbuffer_size);

namespace starcounter {

/// Exception class.
class log_exception {
public:
	typedef uint32_t error_code_type;
	
	explicit log_exception(error_code_type err)
	: err_(err) {}
	
	error_code_type error_code() const {
		return err_;
	}
	
private:
	error_code_type err_;
};

/// Class log.
class log {
public:
	/// Default constructor. It will not open the log.
	log();
	
	/// Constructor that will try to open a Starcounter log.
	/**
	 * @param server_name The name of the server, for example "PERSONAL" or
	 *		"SYSTEM".
	 * @param process_name The name of the process doing the logging, for
	 *		example "scipcmonitor".
	 * @param server_log_dir
	 */
	explicit log(const char* server_name, const char* process_name,
	const wchar_t* server_log_dir);
	
	/// Destructor will try to close the log if it is open.
	~log();
	
	/// Try to open a Starcounter log if not already open.
	/**
	 * @param server_name The name of the server, for example "PERSONAL" or
	 *		"SYSTEM".
	 * @param process_name The name of the process doing the logging, for
	 *		example "scipcmonitor".
	 * @param server_log_dir
	 */
	void open(const char* server_name, const char* process_name,
	const wchar_t* server_log_dir);
	
	/// Closes Starcounter log.
	void close();
	
	/// Write critical into log.
	void critical(const wchar_t* message);
	
	/// Write error into log.
	void error(const wchar_t* message);
	
	/// Write debug into log.
	void debug(const wchar_t* message);
	
	/// Write verbose message.
	void verbose(const wchar_t* message);
	
private:
	// Objects of type log are not copyable.
	log(const log&);
	const log& operator=(const log&);
	
	uint64_t handle_;
	bool is_open_;
};

} // namespace starcounter

#include "impl/log.hpp"

#endif // STARCOUNTER_LOG_HPP
