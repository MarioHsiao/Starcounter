//
// impl/log.hpp
//
// Copyright © 2006-2013 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//
// Implementation of class log.
//

#ifndef STARCOUNTER_IMPL_LOG_HPP
#define STARCOUNTER_IMPL_LOG_HPP

// Implementation

namespace starcounter {

inline log::log()
: handle_(0),
is_open_(false) {
	// Default constructor doesn't try to open the log.
}

inline log::log(const char* server_name, const char* process_name,
const wchar_t* server_log_dir)
: handle_(0),
is_open_(false) {
	// Try to open the log, if not already open.
	open(server_name, process_name, server_log_dir);
}

inline log::~log() {
	// Close the log if open.
	close();
}

inline void log::open(const char* server_name, const char* process_name,
const wchar_t* server_log_dir)
try {
	if (!is_open_) {
		std::size_t size = 0;
		log_exception::error_code_type err = 0;
		
		if ((err = ::make_sc_process_uri(server_name, process_name, 0, &size) != 0)) {
			throw log_exception(err);
		}
		
		std::vector<wchar_t> host_name(size);
		
		if ((err = ::make_sc_process_uri(server_name, process_name, &host_name[0], &size)) != 0) {
			throw log_exception(err);
		}
		
		if ((err = ::sccorelog_init(0)) != 0) {
			throw log_exception(err);
		}
		
		if ((err = ::sccorelog_connect_to_logs(&host_name[0], 0, &handle_)) != 0) { // &host_name[0] is host_name.begin()
			throw log_exception(err);
		}
		
		if ((err = ::sccorelog_bind_logs_to_dir(handle_, server_log_dir)) != 0) {
			throw log_exception(err);
		}
	}
}
catch (const std::bad_alloc&) {
	throw log_exception(SCERROUTOFMEMORY);
}

inline void log::close() {
	if (is_open_) {
		log_exception::error_code_type err = ::sccorelog_release_logs(handle_);
		_SC_ASSERT(err == 0);
		handle_ = 0;
		is_open_ = false;
	}
}

inline void log::critical(const wchar_t* message) {
	log_exception::error_code_type err = ::sccorelog_kernel_write_to_logs(handle_,
	SC_ENTRY_CRITICAL, 0, message);
	_SC_ASSERT(err == 0);
	err = ::sccorelog_flush_to_logs(handle_);
	_SC_ASSERT(err == 0);
}

inline void log::error(const wchar_t* message) {
	log_exception::error_code_type err = ::sccorelog_kernel_write_to_logs(handle_,
	SC_ENTRY_ERROR, 0, message);
	_SC_ASSERT(err == 0);
	err = ::sccorelog_flush_to_logs(handle_);
	_SC_ASSERT(err == 0);
}

inline void log::debug(const wchar_t* message) {
	log_exception::error_code_type err = ::sccorelog_kernel_write_to_logs(handle_,
	SC_ENTRY_DEBUG, 0, message);
	_SC_ASSERT(err == 0);
	err = ::sccorelog_flush_to_logs(handle_);
	_SC_ASSERT(err == 0);
}

inline void log::verbose(const wchar_t* message) {
	::wprintf(L"<DEBUG> %s\n", message);
	
	if (handle_ != 0) {
		// Logging to log file.
		debug(message);
	}
}

} // namespace starcounter

#endif // STARCOUNTER_IMPL_LOG_HPP
