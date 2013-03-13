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
		
		if ((err = process_uri(server_name, process_name, 0, &size) != 0)) {
			throw log_exception(err);
		}
		
		std::vector<wchar_t> host_name(size);
		
		if ((err = process_uri(server_name, process_name, &host_name[0], &size)) != 0) {
			throw log_exception(err);
		}
		
		if ((err = ::sccorelog_init(0)) != 0) {
			throw log_exception(err);
		}
		
		if ((err = ::sccorelog_connect_to_logs(&host_name[0], 0, &handle_)) != 0) {
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

inline void log::debug(uint32_t error_code) {
	log_exception::error_code_type err = ::sccorelog_kernel_write_to_logs(handle_,
	entry_debug, error_code, 0);
	_SC_ASSERT(err == 0);
	err = ::sccorelog_flush_to_logs(handle_);
	_SC_ASSERT(err == 0);
}

inline void log::debug(const wchar_t* message, uint32_t error_code) {
	log_exception::error_code_type err = ::sccorelog_kernel_write_to_logs(handle_,
	entry_debug, error_code, message);
	_SC_ASSERT(err == 0);
	err = ::sccorelog_flush_to_logs(handle_);
	_SC_ASSERT(err == 0);
}

inline void log::success_audit(uint32_t error_code) {
	log_exception::error_code_type err = ::sccorelog_kernel_write_to_logs(handle_,
	entry_success_audit, error_code, 0);
	_SC_ASSERT(err == 0);
	err = ::sccorelog_flush_to_logs(handle_);
	_SC_ASSERT(err == 0);
}

inline void log::success_audit(const wchar_t* message, uint32_t error_code) {
	log_exception::error_code_type err = ::sccorelog_kernel_write_to_logs(handle_,
	entry_success_audit, error_code, message);
	_SC_ASSERT(err == 0);
	err = ::sccorelog_flush_to_logs(handle_);
	_SC_ASSERT(err == 0);
}

inline void log::failure_audit(uint32_t error_code) {
	log_exception::error_code_type err = ::sccorelog_kernel_write_to_logs(handle_,
	entry_failure_audit, error_code, 0);
	_SC_ASSERT(err == 0);
	err = ::sccorelog_flush_to_logs(handle_);
	_SC_ASSERT(err == 0);
}

inline void log::failure_audit(const wchar_t* message, uint32_t error_code) {
	log_exception::error_code_type err = ::sccorelog_kernel_write_to_logs(handle_,
	entry_failure_audit, error_code, message);
	_SC_ASSERT(err == 0);
	err = ::sccorelog_flush_to_logs(handle_);
	_SC_ASSERT(err == 0);
}

inline void log::notice(uint32_t error_code) {
	log_exception::error_code_type err = ::sccorelog_kernel_write_to_logs(handle_,
	entry_notice, error_code, 0);
	_SC_ASSERT(err == 0);
	err = ::sccorelog_flush_to_logs(handle_);
	_SC_ASSERT(err == 0);
}

inline void log::notice(const wchar_t* message, uint32_t error_code) {
	log_exception::error_code_type err = ::sccorelog_kernel_write_to_logs(handle_,
	entry_notice, error_code, message);
	_SC_ASSERT(err == 0);
	err = ::sccorelog_flush_to_logs(handle_);
	_SC_ASSERT(err == 0);
}

inline void log::warning(uint32_t error_code) {
	log_exception::error_code_type err = ::sccorelog_kernel_write_to_logs(handle_,
	entry_warning, error_code, 0);
	_SC_ASSERT(err == 0);
	err = ::sccorelog_flush_to_logs(handle_);
	_SC_ASSERT(err == 0);
}

inline void log::warning(const wchar_t* message, uint32_t error_code) {
	log_exception::error_code_type err = ::sccorelog_kernel_write_to_logs(handle_,
	entry_warning, error_code, message);
	_SC_ASSERT(err == 0);
	err = ::sccorelog_flush_to_logs(handle_);
	_SC_ASSERT(err == 0);
}

inline void log::error(uint32_t error_code) {
	log_exception::error_code_type err = ::sccorelog_kernel_write_to_logs(handle_,
	entry_error, error_code, 0);
	_SC_ASSERT(err == 0);
	err = ::sccorelog_flush_to_logs(handle_);
	_SC_ASSERT(err == 0);
}

inline void log::error(const wchar_t* message, uint32_t error_code) {
	log_exception::error_code_type err = ::sccorelog_kernel_write_to_logs(handle_,
	entry_error, error_code, message);
	_SC_ASSERT(err == 0);
	err = ::sccorelog_flush_to_logs(handle_);
	_SC_ASSERT(err == 0);
}

inline void log::critical(uint32_t error_code) {
	log_exception::error_code_type err = ::sccorelog_kernel_write_to_logs(handle_,
	entry_critical, error_code, 0);
	_SC_ASSERT(err == 0);
	err = ::sccorelog_flush_to_logs(handle_);
	_SC_ASSERT(err == 0);
}

inline void log::critical(const wchar_t* message, uint32_t error_code) {
	log_exception::error_code_type err = ::sccorelog_kernel_write_to_logs(handle_,
	entry_critical, error_code, message);
	_SC_ASSERT(err == 0);
	err = ::sccorelog_flush_to_logs(handle_);
	_SC_ASSERT(err == 0);
}

inline int32_t log::process_uri(const char* server_name, const char* process_name,
wchar_t* buffer, std::size_t* size) {
	char computer_name[MAX_COMPUTERNAME_LENGTH +1];
	uint32_t computer_name_size = MAX_COMPUTERNAME_LENGTH +1;
	
	if (::GetComputerNameA(computer_name, (DWORD*) &computer_name_size) == 0) {
		return ::GetLastError();
	}

	std::size_t buffer_size_needed =
	+5 // 'sc://'
	+computer_name_size
	+1 // '/'
	+strlen(server_name)
	+1 // '/'
	+strlen(process_name)
	+1; // '\0'
	
	std::size_t buffer_size = *size;
	*size = buffer_size_needed;

	if (buffer_size_needed <= buffer_size) {
		swprintf(buffer, L"sc://%S/%S/%S", computer_name, server_name, process_name);
		_wcslwr(buffer);
		return 0;
	}
	return 0;
}

} // namespace starcounter

#endif // STARCOUNTER_IMPL_LOG_HPP
