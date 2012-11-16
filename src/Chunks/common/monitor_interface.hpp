//
// monitor_interface.hpp
//
// Copyright © 2006-2012 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//
//------------------------------------------------------------------------------
// NOTES:
// The monitor_interface shared memory object name shall match the server's
// (not the database process) name, because we need to be able to start multiple
// monitors.
//------------------------------------------------------------------------------

#ifndef STARCOUNTER_CORE_MONITOR_INTERFACE_HPP
#define STARCOUNTER_CORE_MONITOR_INTERFACE_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#include <iostream> // debug
#include <string>
#include <cstring>
#include <cstddef>
#include <boost/cstdint.hpp>
#include <memory>
#include <boost/interprocess/managed_shared_memory.hpp>
#include <boost/interprocess/allocators/allocator.hpp>
#include <boost/interprocess/sync/interprocess_mutex.hpp>
#include <boost/interprocess/sync/scoped_lock.hpp>
#include <boost/interprocess/sync/interprocess_condition.hpp>
#include <boost/date_time/posix_time/posix_time_types.hpp> // No I/O just types.
#include <boost/date_time/microsec_time_clock.hpp>
#include <boost/thread/thread.hpp>
#include <boost/thread/win32/thread_primitives.hpp>
#include <boost/call_traits.hpp>
#include <boost/bind.hpp>
#if defined(_MSC_VER)
# define WIN32_LEAN_AND_MEAN
# include <windows.h> /// TODO: thread_primitives.hpp might replace this include
//# include <intrin.h>
# undef WIN32_LEAN_AND_MEAN
#endif // defined(_MSC_VER)
#include "../common/pid_type.hpp"
#include "../common/owner_id.hpp"
#include "../common/config_param.hpp"
#include "../../Starcounter.ErrorCodes/scerrres/scerrres.h"
#include "../common/spinlock.hpp"
#include "../common/macro_definitions.hpp"

namespace starcounter {
namespace core {

class monitor_interface {
public:
	explicit monitor_interface();
	
	uint32_t register_database_process(pid_type pid, std::string segment_name,
	owner_id& oid, uint32_t timeout_milliseconds);
	
	uint32_t register_client_process(pid_type pid, owner_id& oid,
	uint32_t timeout_milliseconds);
	
	uint32_t unregister_database_process(pid_type pid, owner_id& oid,
	uint32_t timeout_milliseconds);
	
	uint32_t unregister_client_process(pid_type pid, owner_id& oid,
	uint32_t timeout_milliseconds);
	
	// The monitor thread registrar_ calls this function in order to wait for
	// processes to register and unregister. It obtains the parameters.
	void wait_for_registration();
	
	enum process_type {
		database_process,
		client_process
	};
	
	enum operation {
		registration_request,
		unregistration_request,
		shutdown /// TODO: Implement this and see if it works fine.
	};
	
	void set_pid(pid_type pid); // make it private?
	pid_type get_pid() const;
	
	void set_segment_name(std::string segment_name);
	std::string get_segment_name() const;
	
	void set_process_type(process_type pt);
	process_type get_process_type() const;
	
	void set_operation(operation op);
	operation get_operation() const;
	
	void set_owner_id(owner_id oid);
	owner_id get_owner_id() const;
	
	void out_data_is_available_notify_one() {
		out_.data_is_available_.notify_one();
	}
	
	void wait_until_ready();
	void is_ready_notify_all();
	
	void set_in_data_available_state(bool state);
	void set_out_data_available_state(bool state);
	
	smp::spinlock& sp() {
		return sp_;
	}

private:
	// Synchronization to check if the monitor_interface is ready or not.
	boost::interprocess::interprocess_mutex ready_mutex_;
	boost::interprocess::interprocess_condition is_ready_;
	bool is_ready() const;
	bool is_ready_flag_;
	smp::spinlock sp_;

	// Synchronize access to the monitor_interface among all database- and
	// client processes. The monitor threads never locks this mutex.
	boost::interprocess::interprocess_mutex monitor_interface_mutex_;
	
	// Database- and client processes store the data they need to communicate to
	// the monitor in the in_ structure.
	struct in {
		in();
		
		// Synchronization between the process that registers and the monitor.
		boost::interprocess::interprocess_mutex mutex_;
		boost::interprocess::interprocess_condition data_is_available_;
		//bool is_data_available() const; /// TODO: Figure how to bind this.
		bool data_available_;
		
		// Both database- and client processes store their pid here.
		pid_type pid_;
		
		// A database process store it's segment_name here.
		char segment_name_[segment_name_size];
		
		// Type of process that is registering. The monitor uses it to decide to
		// which thread group to send the APC and if to store the segment_name.
		process_type process_type_;
		
		// Is the process registering or unregistering?
		operation operation_;
	} in_;
	
	// Hack until I can figure how to bind it.
	bool in_is_data_available() const;
	
	// The monitor store the data it need to the process that registers, in the
	// out_ structure.
	struct out {
		out();
		
		// Synchronization between the process that registers and the monitor.
		boost::interprocess::interprocess_mutex mutex_;
		boost::interprocess::interprocess_condition data_is_available_;
		//bool is_data_available() const; /// TODO: Figure how to bind this.
		bool data_available_;
		
		// The owner_id to be returned to the process that registers.
		owner_id owner_id_;
	} out_;
	
	// Hack until I can figure how to bind it.
	bool out_is_data_available() const;
};

/// Exception class.
class monitor_interface_ptr_exception {
public:
	explicit monitor_interface_ptr_exception(int err)
	: err_(err) {}
	
	int error_code() const {
		return err_;
	}
	
private:
	int err_;
};

/// class monitor_interface_ptr act as a smart pointer that opens a
/// monitor_interface and obtains a pointer to the shared structure.
class monitor_interface_ptr {
public:
	/// Constructor that open a monitor_interface. If it doesn't exist an
	/// exception is thrown with an error code.
	/**
	 * @param monitor_interface_name Has the format
	 *		<SERVER_NAME>_<MONITOR_INTERFACE_SUFFIX>
	 */
	monitor_interface_ptr(const char* monitor_interface_name)
	: ptr_(0) {	
		// Try to open the monitor interface shared memory object.
		shared_memory_object_.init_open(monitor_interface_name);
		
		if (!shared_memory_object_.is_valid()) {
			// Failed to open monitor interface.
			throw monitor_interface_ptr_exception(SCERROPENMONITORINTERFACE);
		}
		
		// Map the whole database shared memory parameters shared memory object
		// in this process.
		mapped_region_.init(shared_memory_object_);
		
		if (!mapped_region_.is_valid()) {
			// Failed to map monitor interface in shared memory.
			throw monitor_interface_ptr_exception
			(SCERRMAPMONITORINTERFACEINSHM);
		}
		
		// Obtain a pointer to the shared structure.
		ptr_ = static_cast<monitor_interface*>(mapped_region_.get_address());
	}
	
    void init() {


    }

	monitor_interface_ptr(monitor_interface* ptr) {
		ptr_ = ptr;
	}
	
	/// Destructor.
	~monitor_interface_ptr() {
		ptr_ = 0;
		// The shared_memory_object and mapped_region destructors are called.
	}
	
	/// Dereferences the smart pointer.
	/**
	 * @return A reference to the shared structure.
	 */
	monitor_interface& operator*() const {
		return *ptr_;
	}
	
	/// Dereferences the smart pointer to get at a member of what it points to.
	/**
	 * @return A pointer to the shared structure.
	 */
	monitor_interface* operator->() const {
		return ptr_;
	}
	
	/// Extract pointer.
	/**
	 * @return A pointer to the shared structure.
	 */
	monitor_interface* get() const {
		return ptr_;
	}
	
private:
	/// The default copy constructor and assignment operator are made private.
	monitor_interface_ptr(monitor_interface_ptr&);
	monitor_interface_ptr& operator=(monitor_interface_ptr&);
	
	shared_memory_object shared_memory_object_;
	mapped_region mapped_region_;
	monitor_interface* ptr_;
};

} // namespace core
} // namespace starcounter

#include "impl/monitor_interface.hpp"

#endif // STARCOUNTER_CORE_MONITOR_INTERFACE_HPP

