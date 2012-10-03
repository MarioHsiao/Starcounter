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
	
private:
	// Synchronization to check if the monitor_interface is ready or not.
	boost::interprocess::interprocess_mutex ready_mutex_;
	boost::interprocess::interprocess_condition is_ready_;
	bool is_ready() const;
	bool is_ready_flag_;
	
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



#if 0 /// TOO MESSY:
	
/// class monitor_interface.
/**
 * @param T The type of the elements stored.
 *
 * @par Type Requirements T
 *      The T has to be SGIAssignable (SGI STL defined combination of Assignable
 *      and CopyConstructible), and EqualityComparable and/or LessThanComparable
 *      if the (?) will be compared with another container.
 *
 * @param Alloc The allocator type used for all internal memory management.
 *
 * @par Type Requirements Alloc
 *      The Alloc has to meet the allocator requirements imposed by STL.
 *
 * @par Default Alloc
 *      std::allocator<T>
 */
class monitor_interface {
public:
	enum client_monitor_state {
		waiting_for_first_client_pid_registration,
		monitoring_and_waiting_for_pid_register_update
	};

	// Construction/Destruction.
	
	/// TODO fix constructor.
	
	/// Create an (?) with the specified capacity.
	/**
	 * @param buffer_capacity The maximum number of elements which can be stored
	 *      in the bounded_buffer.
	 *
	 * @param alloc The allocator.
	 *
	 * @throws "An allocation error" if memory is exhausted (std::bad_alloc if
	 *      the standard allocator is used).
	 *
	 * @par Complexity
	 *      Constant.
	 */
	explicit monitor_interface();

	//--------------------------------------------------------------------------
	/// Database processes that want to be monitored call
	/// register_database_process(). Probably the instance process need to
	/// allocate resources, such as chunks. A feedback chunk is allocated with
	/// the owner_id of the client that is to receive it, because the client is
	/// responsible for freeing the chunk.
	/**
	 * @param pid The instance process identifier retrieved from the OS.
	 *
	 * @param oid The owner_id is returned via this parameter. If failed to
	 *      register, oid is assigned owner_id::none.
	 *
	 * @param segment_name The name of the managed shared memory segment that
	 *      this instance process manages.
	 *
	 * @return false if failed to register, true otherwise.
	 */
	bool register_database_process(pid_type pid, std::string segment_name,
	owner_id& oid);
	
	//--------------------------------------------------------------------------
	/// Instance processes call unregister_database_process() so that the
	/// monitor knows that the server intend to terminate gracefully.
	/**
	 * @param pid The instance process identifier retrieved from the OS.
	 *
	 * @param oid The owner_id is returned via this parameter. It will be set to
	 *      no_owner_id if the unregistration was successfull.
	 *
	 * @return false if the call is returning because the time period specified
	 *      by timeout_milliseconds has elapsed, true otherwise.
	 */
	bool unregister_database_process(pid_type pid, owner_id& oid);
	
	//--------------------------------------------------------------------------
	/// Client processes that want to be monitored call
	/// register_client_process(). The owner_id is used when acquiring resources
	/// such as chunks and channels.
	/**
	 * @param pid The client process identifier retrieved from the OS.
	 *
	 * @param oid The owner_id is returned via this parameter.
	 *
	 * @param timeout_milliseconds The number of milliseconds to wait before
	 *      a timeout may occur.
	 *
	 * @return false if the call is returning because the time period specified
	 *      by timeout_milliseconds has elapsed, true otherwise.
	 */
	bool register_client_process(pid_type pid, owner_id& oid);
	
	//--------------------------------------------------------------------------
	/// Client processes call unregister_client_process() after having released
	/// all resources in shared memory, so that the monitor knows that the
	/// client intend to terminate gracefully.
	/**
	 * @param pid The client process identifier retrieved from the OS.
	 *
	 * @param oid The owner_id is returned via this parameter. It will be set to
	 *      no_owner_id if the unregistration was successfull.
	 *
	 * @param timeout_milliseconds The number of milliseconds to wait before
	 *      a timeout may occur.
	 *
	 * @return false if the call is returning because the time period specified
	 *      by timeout_milliseconds has elapsed, true otherwise.
	 */
	bool unregister_client_process(pid_type pid, owner_id& oid);

	// The monitor calls this.
	void pop_back_process_id(process_id* the_process_id) {
		process_id_.pop_back(the_process_id);
		// Here the client_number (and owner_id) must be written to the channel.
		// But from here we can not access the channel...
		// And if we try to do this in the shared_interface, 
		// Set the process_id flag in channel_mask_.
		//channel_mask_.set_process_id_flag(the_process_id);
	}

	// The client and server processes calls this.
	void push_front_process_id(process_id the_process_id) {
		// Clear the process_id flag in this channel_mask_.
		//channel_mask_.clear_process_id_flag(the_process_id);
		// Here the client_number (and owner_id) must be written as not used,
		// to the channel.
		// But from here we can not access the channel...
		// Release the_process_id to this process_id_ queue.
		process_id_.push_front(the_process_id);
	}

	void set_process_id_flag(process_id the_process_id) {
		channel_mask_.set_process_id_flag(the_process_id);
	}

	void clear_process_id_flag(process_id the_process_id) {
		channel_mask_.clear_process_id_flag(the_process_id);
	}

	uint64_t get_channel_mask_word(std::size_t n) const {
		return channel_mask_.get_channel_mask_word(n);
	}

	/// Get spin count.
	/**
	 * @return The spin_count.
	 *
	 * @throws Nothing.
	 *
	 * @par Exception Safety
	 *      No-throw.
	 */
	std::size_t get_spin_count() const {
		return process_id_.get_spin_count();
	}

	/// Set spin count.
	/**
	 * A thread that performs the operations push_front() or pop_back()
	 * will spin for spin_count number of times before blocking.
	 *
	 * @param spin_count The number of times to re-try acquiring the lock
	 *      before blocking. 0 = no spin.
	 *
	 * @throws Nothing.
	 *
	 * @par Exception Safety
	 *      No-throw.
	 */
	void set_spin_count(std::size_t spin_count) {
		process_id_.set_spin_count(spin_count);
	}

	//--------------------------------------------------------------------------
	// Each time a scan of a set of channels have been completed, regardless of
	// the number of channels that have been scanned, the scheduler calls
	// increment_channel_scan_counter(). The idéa is that when a client wants to
	// release a channel it want to make sure that the scheduler no longer will
	// access the channel. The scheduler must see that the channel_mask has been
	// updated by the client so that it no longer checks the channel.
	// One way to make sure this is the case is for the client to call
	// get_channel_scan_counter(), and then check the counter again sometime
	// later to see if the value has changed. When the client sees that the
	// counter has changed (since the counter may wrap, operator != is used),
	// then it knows for sure that the scheduler will no longer check the in
	// queue of the channel. But how do we guarantee that the scheduler will not
	// access the out queue of the channel?
	//--------------------------------------------------------------------------
	
	//the scheduler have seen the update of the channel_mask_ and the client
	// can then release the channel. However, the scheduler may take a
	// relatively long time to complete a scan. We may enter a periodic sleep
	// of 1 ms and poll the get_channel_scan_counter() once every 1 ms.
	std::size_t get_channel_scan_counter() const {
		return channel_scan_counter_;
	}
	
	//--------------------------------------------------------------------------
	// Clients and schedulers call notify() each time they push a message on a
	// channel, in order to wake up a scheduler if it is waiting for work.
	void notify() {
		boost::interprocess::scoped_lock<boost::interprocess::interprocess_mutex>
		lock(mutex_);
		
		// Need to notify the scheduler
		set_predicate(true);
		lock.unlock();
		work_.notify_one();
	}

	// Setting predicate to true means the condition is met
	// and the wait is over, the thread waiting will not wait any more.
	// Setting the predicate to false means the condition is not met
	// and the thread waiting will keep waiting.
	// Access to the predicate must be synchronized.
	void set_predicate(bool state) {
		predicate_ = state;
	}

	bool not_all_channels_in_queues_are_empty() const {
		return predicate_;
	}

	/// Scheduler's call wait_for_work() if they don't find any work to do.
	/// It is a "timed" function that can fail.
	/**
	 * @param timeout_milliseconds The number of milliseconds to wait before
	 *      a timeout may occur.
	 *
	 * @return false if the call is returning because the time period specified
	 *      by timeout_milliseconds has elapsed, true otherwise.
	 */
	bool wait_for_work(unsigned int timeout_milliseconds) {
		const boost::system_time timeout = boost::posix_time::microsec_clock
		::universal_time() +boost::posix_time::milliseconds
		(timeout_milliseconds);
		
		boost::interprocess::scoped_lock<boost::interprocess::interprocess_mutex>
		lock(mutex_);
		
		// Wait until at least one message has been pushed into some channel,
		// or the timeout_milliseconds time period has elapsed.
		if (work_.timed_wait(lock, timeout,
		boost::bind(&monitor_interface<value_type, allocator_type>
		::not_all_channels_in_queues_are_empty, this)) == true) {
			// The scheduler was notified that there is work to do.
			set_predicate(false);
			return true;
		}
		
		// The timeout_milliseconds time period has elapsed.
		// Shall the predicate be set to false on timeout? I think not.
		return false;
	}

	client_interface_type* get_client_interface() {
		boost::interprocess::scoped_lock<boost::interprocess::interprocess_mutex>
		lock(client_interface_mutex_);
		return client_interface_;
	}
	//--------------------------------------------------------------------------
	// Only one thread ever accesses this - the scheduler thread calls this once
	// to set the client_interface pointer. After that it is not changed for as
	// long as the scheduler owns this client interface. Therefore, no
	// synchronization is needed. Such assumptions are dangerous and must be
	// thought through, so in the mean time I use synchronization anyway.
	void set_client_interface(client_interface_type* p) {
		boost::interprocess::scoped_lock<boost::interprocess::interprocess_mutex>
		lock(monitor_interface_mutex_);
		client_interface_ = p;
	}
	
private:
	//--------------------------------------------------------------------------
	// Synchronize access to this monitor_interface between server and client
	// processes so that only one can register or unregister at any given time.
	boost::interprocess::interprocess_mutex monitor_interface_mutex_;

	//--------------------------------------------------------------------------
	// Used in conjunction with the monitor_task_done_ condition variable.
	boost::interprocess::interprocess_mutex monitor_task_mutex_;
	
	// Condition to wait for the monitor to register or unregister the pid.
	// Wait until it is true that the monitor have done its task (register or
	// unregister the pid in the pid_registry).
	boost::interprocess::interprocess_condition monitor_task_done_;
	//--------------------------------------------------------------------------

	// How do we know which client_process_id (0..255) or which server_process_id
	// (0..63) that this applies to? Do we have to know? Maybe not.
};

// Define an STL compatible allocator of process_id that allocates from the
// managed_shared_memory. This allocator will allow placing containers in the segment.
//------------------------------------------------------------------------------
typedef boost::interprocess::allocator<process_id, boost::interprocess
::managed_shared_memory::segment_manager>
shm_alloc_for_the_monitor_interfaces;

// Alias a monitor_interface that uses the previous STL-like allocator so that it
// allocates its values from the segment.
typedef monitor_interface<process_id, shm_alloc_for_the_monitor_interfaces>
monitor_interface_type;
#endif /// TOO MESSY

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

