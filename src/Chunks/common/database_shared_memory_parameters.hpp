//
// database_shared_memory_parameters.hpp
//
// Copyright © 2006-2013 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#ifndef STARCOUNTER_CORE_DATABASE_SHARED_MEMORY_PARAMETERS_HPP
#define STARCOUNTER_CORE_DATABASE_SHARED_MEMORY_PARAMETERS_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#include <cstdint>
#include <cstdlib>
#include <cstring>
#include <boost/interprocess/sync/interprocess_mutex.hpp>
#include <boost/interprocess/sync/scoped_lock.hpp>
#include "../common/config_param.hpp"
#include "../../Starcounter.ErrorCodes/scerrres/scerrres.h"

#if defined(_MSC_VER)
# define WIN32_LEAN_AND_MEAN
# include <windows.h>
# include <mmintrin.h>
# undef WIN32_LEAN_AND_MEAN
#endif // defined(_MSC_VER)

namespace starcounter {
namespace core {

/// All database processes have a shared memory segment (the databases shared
/// memory segment) with several objects in it such as chunks, channels,
/// scheduler_interface[s] and client_interface[s], etc. For each database
/// process there is also a shared memory object that contains a
/// database_shared_memory_parameters object. It contains parameters for that
/// database process. The purpose is for processes that need to open such a
/// database shared memory segment to know the suffix (a sequence number) of the
/// name of the databases shared memory segment, etc.
class database_shared_memory_parameters {
public:
	// type definitions
	typedef uint32_t sequence_number_type;
	
	// construction/destruction
	
	/// Constructor.
	/**
	 * @throws Nothing.
	 * @par Complexity
	 *      Constant.
	 */
	database_shared_memory_parameters();
	
	/// A client process can get the current sequence number, and postfix it to
	/// the name of the database shared memory segment.
	sequence_number_type get_sequence_number();
	
	/// Database processes can set the current sequence number.
	const database_shared_memory_parameters& set_sequence_number(
	sequence_number_type file_number);
	
	/// Database processes can increment the current sequence number by 1.
	const database_shared_memory_parameters& increment_sequence_number();
	
	/// Get server_name.
	/**
	 * @return The server name as a C-string in char format.
	 * @throws Nothing.
	 * @par Exception Safety
	 *      No-throw.
	 */
	const char* get_server_name() const;
	
	/// Set server_name, char version.
	/**
	 * @param name The server name, char version.
	 * @return Nothing.
	 * @throws Nothing.
	 * @par Exception Safety
	 *      No-throw.
	 */
	void set_server_name(const char* name);

	/// Set server_name, wchar_t version.
	/**
	 * @param name The server name (LPCWSTR c-string) to be set.
	 * @return Nothing.
	 * @throws Nothing.
	 * @par Exception Safety
	 *      No-throw.
	 */
	void set_server_name(const wchar_t* name);
	
private:
	// Synchronization between processes.
	boost::interprocess::interprocess_mutex mutex_;
	
	// These parameters are rarely accessed so they don't need to be on separate
	// cache-lines and it is anyway better that they are closer together.
	sequence_number_type sequence_number_;
	
	// Server name. I think all APIs wants it in char format (not wchar_t.)
	char server_name_[server_name_size];
};

/// Exception class.
class database_shared_memory_parameters_ptr_exception {
public:
	explicit database_shared_memory_parameters_ptr_exception(int err)
	: err_(err) {}
	
	int error_code() const {
		return err_;
	}
	
private:
	int err_;
};

/// class database_shared_memory_parameters_ptr act as a smart pointer that
/// opens a database shared memory parameter file and obtains a pointer to the
/// shared structure. The file is closed by the destructor at the end of the
/// scope, or if an exception is thrown.
class database_shared_memory_parameters_ptr {
public:
	/// The default constructor does nothing.
	database_shared_memory_parameters_ptr() {}
	
	/// Constructor that open or create a database_shared_memory_parameter file.
	/**
	 * @param segment_name Has the format
	 *		<DATABASE_NAME_PREFIX>_<DATABASE_NAME>_0
	 * @param is_system Flag is true if the server name is "SYSTEM", false
	 *		otherwise.
	 * @param mapping Is (default) memory_mapped, or it can be file_mapped.
	 * @param db_data_dir_path Path to where the shared memory parameters are
	 *		stored if the shared memory object is file mapped, but if it is
	 *		memory_mapped then this parameter is default 0.
	 */
	database_shared_memory_parameters_ptr(const char* segment_name, bool
	is_system, shared_memory_object::mapping mapping = shared_memory_object
	::memory_mapped, const char* db_data_dir_path = 0)
	: ptr_(0) {
		init(segment_name, is_system, mapping, db_data_dir_path);
	}
	
	/// init() open or create a database_shared_memory_parameter file.
	/**
	 * @param segment_name Has the format
	 *		<DATABASE_NAME_PREFIX>_<DATABASE_NAME>_0
	 * @param is_system Flag is true if the server name is "SYSTEM", false
	 *		otherwise.
	 * @param mapping Is (default) memory_mapped, or it can be file_mapped.
	 * @param db_data_dir_path Path to where the shared memory parameters are
	 *		stored if the shared memory object is file mapped, but if it is
	 *		memory_mapped then this parameter is default 0.
	 */
	void init(const char* segment_name, bool is_system,
	shared_memory_object::mapping mapping = shared_memory_object::memory_mapped,
	const char* db_data_dir_path = 0) {
		// Try to open the database_shared_memory_parameters shared memory
		// object. If it does not exist it will be created instead.
		shared_memory_object_.init_open(segment_name);
		
		if (!shared_memory_object_.is_valid()) {
			// It failed because it doesn't exist. Try to create it instead.
			shared_memory_object_.init_create(segment_name,
			sizeof(database_shared_memory_parameters), is_system, mapping,
			db_data_dir_path);
			
			if (!shared_memory_object_.is_valid()) {
				// Failed to create the database shared memory parameter file.
				throw database_shared_memory_parameters_ptr_exception
				(SCERRCREATEDBSHMPARAMETERS);
			}
		}
		
		// Map the whole database shared memory parameters shared memory object
		// in this process.
		mapped_region_.init(shared_memory_object_);
		
		if (!mapped_region_.is_valid()) {
			// Failed to map the database shared memory parameter file in shared
			// memory.
			throw database_shared_memory_parameters_ptr_exception
			(SCERRMAPDBSHMPARAMETERSINSHM);
		}
		
		// Obtain a pointer to the shared structure.
		ptr_ = static_cast<database_shared_memory_parameters*>
		(mapped_region_.get_address());
	}
	
	/// Constructor that open the database shared memory. If it doesn't exist an
	/// exception is thrown with an error code.
	/**
	 * @param segment_name Has the format
	 *		<DATABASE_NAME_PREFIX>_<DATABASE_NAME>_0
	 * @param mapping Is (default) memory_mapped, or it can be file_mapped.
	 * @param db_data_dir_path Path to where the shared memory parameters are
	 *		stored if the shared memory object is file mapped, but if it is
	 *		memory_mapped then this parameter is default 0.
	 */
	database_shared_memory_parameters_ptr(const char* segment_name)
	: ptr_(0) {
		// Try to open the database shared memory parameters shared memory
		// object.
		shared_memory_object_.init_open(segment_name);
		
		if (!shared_memory_object_.is_valid()) {
			// Failed to open the database shared memory parameters.
			throw database_shared_memory_parameters_ptr_exception
			(SCERROPENDBSHMPARAMETERS);
		}
		
		// Map the whole database shared memory parameters shared memory object
		// in this process.
		mapped_region_.init(shared_memory_object_);
		
		if (!mapped_region_.is_valid()) {
			// Failed to map the database shared memory parameter file in shared
			// memory.
			throw database_shared_memory_parameters_ptr_exception
			(SCERRMAPDBSHMPARAMETERSINSHM);
		}
		
		// Obtain a pointer to the shared structure.
		ptr_ = static_cast<database_shared_memory_parameters*>
		(mapped_region_.get_address());
	}
	
	database_shared_memory_parameters_ptr(database_shared_memory_parameters*
	ptr) {
		ptr_ = ptr;
	}
	
	/// Destructor.
	~database_shared_memory_parameters_ptr() {
		ptr_ = 0;
		// The shared_memory_object_ and mapped_region_ destructors are called.
	}
	
	/// Dereferences the smart pointer.
	/**
	 * @return A reference to the shared structure.
	 */
	database_shared_memory_parameters& operator*() const {
		return *ptr_;
	}
	
	/// Dereferences the smart pointer to get at a member of what it points to.
	/**
	 * @return A pointer to the shared structure.
	 */
	database_shared_memory_parameters* operator->() const {
		return ptr_;
	}
	
	/// Extract pointer.
	/**
	 * @return A pointer to the shared structure.
	 */
	database_shared_memory_parameters* get() const {
		return ptr_;
	}
	
private:
	/// The default copy constructor and assignment operator are made private.
	database_shared_memory_parameters_ptr
	(database_shared_memory_parameters_ptr&);
	
	database_shared_memory_parameters_ptr&
	operator=(database_shared_memory_parameters_ptr&);
	
	shared_memory_object shared_memory_object_;
	mapped_region mapped_region_;
	database_shared_memory_parameters* ptr_;
};

} // namespace core
} // namespace starcounter

#include "impl/database_shared_memory_parameters.hpp"

#endif // STARCOUNTER_CORE_DATABASE_SHARED_MEMORY_PARAMETERS_HPP
