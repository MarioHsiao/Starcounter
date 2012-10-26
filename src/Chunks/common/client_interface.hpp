//
// client_interface.hpp
//
// Copyright © 2006-2012 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#ifndef STARCOUNTER_CORE_CLIENT_INTERFACE_HPP
#define STARCOUNTER_CORE_CLIENT_INTERFACE_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

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
#include <boost/call_traits.hpp>
#include <boost/bind.hpp>
#define WIN32_LEAN_AND_MEAN
# include <windows.h>
# include <intrin.h>
# include <strsafe.h>
#undef WIN32_LEAN_AND_MEAN
#include "../common/channel_number.hpp"
#include "../common/client_number.hpp"
#include "../common/owner_id.hpp"
#include "../common/resource_map.hpp"
#include "../common/macro_definitions.hpp"

namespace starcounter {
namespace core {

/// class client_interface.
/**
 * @param T The type of the elements stored in the bounded_buffer.
 * @par Type Requirements T
 *		The T has to be SGIAssignable (SGI STL defined combination of Assignable
 *		and CopyConstructible), and EqualityComparable and/or LessThanComparable
 *		if the bounded_buffer will be compared with another container.
 * @param Alloc The allocator type used for all internal memory management.
 * @par Type Requirements Alloc
 *		The Alloc has to meet the allocator requirements imposed by STL.
 * @par Default Alloc
 *		std::allocator<T>
 */
template<class T, class Alloc = std::allocator<T> >
class client_interface {
public:
	// Basic types
	
	// The type of queue for channel_number.
	//typedef bounded_buffer<T, Alloc> queue_type;
	
	// The type of elements stored in the bounded_buffer.
	typedef typename T value_type;
	//typedef typename queue_type::value_type value_type;
	
	// A pointer to an element.
	//typedef typename queue_type::pointer pointer;
	
	// A const pointer to the element.
	//typedef typename queue_type::const_pointer const_pointer;
	
	// A reference to an element.
	//typedef typename queue_type::reference reference;
	
	// A const reference to an element.
	//typedef typename queue_type::const_reference const_reference;
	
	// The distance type. (A signed integral type used to represent the distance
	// between two iterators.)
	//typedef typename queue_type::difference_type difference_type;
	
	// The size type. (An unsigned integral type that can represent any non-
	// negative value of the container's distance type.)
	//typedef typename queue_type::size_type size_type;
	
	// The type of an allocator used in the bounded_buffer.
	typedef Alloc allocator_type;
	//typedef typename queue_type::allocator_type allocator_type;
	
	// Helper types
	
	// A type representing the "best" way to pass the value_type to a method.
	typedef typename boost::call_traits<value_type>::param_type param_type;
	
	// A type representing the "best" way to return the value_type from a const
	// method.
	typedef typename boost::call_traits<value_type>::param_type return_type;
	
	// Construction/Destruction.
	
	/// Constructor.
	/**
	 * @param alloc The allocator.
	 * @param segment_name The segment name.
	 * @param id The index in the array of client_interface[s].
	 * @throws "An allocation error" if memory is exhausted (std::bad_alloc if
	 *		the standard allocator is used).
	 * @par Complexity
	 *		Constant.
	 */
	explicit client_interface(const allocator_type& alloc = allocator_type(),
	const char* segment_name = 0, int32_t id = -1)
	: notify_(false),
#if defined(INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC) // Use Windows Events.

#else // !defined(INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC) // Use Boost.Interprocess.
	predicate_(false),
#endif // defined(INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC) // Use Windows Events.
	owner_id_(owner_id::none),
	allocated_channels_(0) {
#if defined(INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC) // Use Windows Events.
		if (segment_name != 0) {
			char work_notify_name[segment_and_notify_name_size];
			std::size_t length;

			// Format: "Local\<segment_name>_notify_client_<id>".
			if ((length = _snprintf_s(work_notify_name, _countof
			(work_notify_name), segment_and_notify_name_size -1 /* null */,
			"Local\\%s_notify_client_%u", segment_name, id)) < 0) {
				return; // Throw exception error_code.
			}
			work_notify_name[length] = '\0';

			/// TODO: Fix insecure
			if ((length = mbstowcs(work_notify_name_, work_notify_name,
			segment_name_size)) < 0) {
				// Failed to convert work_notify_name to multi-byte string.
				return; // Throw exception error_code.
			}
			work_notify_name_[length] = L'\0';

			if ((work_ = ::CreateEvent(NULL, TRUE, FALSE, work_notify_name_))
			== NULL) {
				// Failed to create event.
				return; // Throw exception error_code.
			}
		}
		else {
			// Error: No segment name. Throw exception error_code.
		}
#endif // defined(INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC) // Use Windows Events.
	}
	
#if defined(INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC) // Use Windows Events.
	~client_interface() {
		if (work_ != 0) {
			::CloseHandle(work_);
			work_ = 0;
		}
	}
#endif // defined(INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC) // Use Windows Events.
	
	bool get_notify_flag() const {
		return notify_;
	}
	
	// Before a client decides to wait, it calls set_notify_flag(true);
	// then it checks the queues again. If it now finds a message it
	// calls set_notify_flag(false) and start to work with the message
	// it found. Else, if it did not find a message it waits/sleeps by calling
	// wait_for_work(ms); It is unclear how many ms to wait before waking up
	// or if a timeout should be used at all.
	void set_notify_flag(bool state) {
		_mm_sfence();
		notify_ = state;
		_mm_sfence();
	}
	
	/// Schedulers call notify() each time they push a message on a channel.
	/// The monitor call notify() if the database goes down.
#if defined(INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC) // Use Windows Events.
	void notify() {
		if (get_notify_flag() == false) {
			// No need to notify the scheduler because it is not waiting.
			return;
		}
		else {
			if (::SetEvent(work_)) {
				// Successfully notified the client.
				return;
			}
			else {
				// Error. Failed to notify the client.
				return;
			}
		}
	}
#else // !defined(INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC) // Use Boost.Interprocess.
	void notify() {
		if (get_notify_flag() == false) {
			// No need to notify, because no client thread is waiting,
			// or the channel has been returned or is about to be returned.
			return;
		}
		else {
			// Notify the waiting client that a message is available.
			boost::interprocess::scoped_lock
			<boost::interprocess::interprocess_mutex> lock(mutex_);
			
			set_predicate(true);
			lock.unlock();

			// In the scheduler interface we notify one. Here we notify all
			// because all client threads with the same client number need to
			// be awakened. One thread may be pushing to the in queue and
			// another thread may be popping from the out queue in a given
			// channel. In this model, a pair of threads work together on a
			// channel and both threads need the notification to start working.
			work_.notify_all();
		}
	}
#endif // defined(INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC) // Use Windows Events.
	
	// Setting predicate to true means the condition is met and the wait is
	// over. Threads that are waiting will not wait any more. Setting the
	// predicate to false means the condition is not met and threads waiting
	// will keep waiting.
	void set_predicate(bool s) {
		predicate_ = s;
	}
	
	bool do_work() const {
		// The mutex_ is always locked when entering here.
		return predicate_;
	}
	
	/// Client threads call wait_for_work() if they don't find any work to do,
	/// and want the database to wake them up if a message is pushed on any of a
	/// set of queues that the client thread(s) have allocated. All client
	/// threads using this client_interface can block on this condition, which
	/// allows a set of client threads to be notified that there is work to do
	/// on at least one of a set of channels.
	///
	/// If the database goes down, the monitor should detect this and will set
	/// the database_state in the common_client_interface to
	/// database_terminated_gracefully or database_terminated_unexpectedly,
	/// and then notify all clients on all channels in this database shared
	/// memory segment.
	///
	/// Client threads that wake up from a call to wait_for_work() must check
	/// the database_state variable. Clients should not check the database_state
	/// when working since it is a waste of time, unless the client will never
	/// wait/sleep, then it must periodically check the state of the database.
	///
	/// If the database goes down, a client will soon know since if a client
	/// don't get a response from the database, or the in queue gets full, or
	/// out queue gets empty in a channel, the client will block and if the
	/// database went down the monitor will see it and set the database_state
	/// variable to database_is_down and wake up all clients and then the
	/// clients will check the state.
	///
	/// It is a "timed" function that can fail.
	/**
	 * @param timeout_milliseconds The number of milliseconds to wait before
	 *		a timeout may occur.
	 * @return false if the call is returning because the time period specified
	 *		by timeout_milliseconds has elapsed, otherwise true.
	 */
#if defined(INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC) // Use Windows Events.
	void reset_work_event() {
		::ResetEvent(work_);
	}

	bool wait_for_work(std::size_t& work_event_index, const HANDLE* work_event,
	std::size_t number_of_work_events, bool reset = true, uint32_t
	timeout_milliseconds = INFINITE) {
		uint32_t event_code = ::WaitForMultipleObjects(number_of_work_events,
		work_event, false, timeout_milliseconds);

		if (event_code < static_cast<uint32_t>(number_of_work_events)) {
			// An event in the vector was set.
			if (reset) {
				// Reset the event.
				::ResetEvent(work_event[event_code]);
				//std::cout << "ResetEvent(" << work_event[event_code] << ")\n"; /// DEBUG
			}
			work_event_index = event_code;
			return true;
		}
		else switch (event_code) {
		case WAIT_TIMEOUT:
			//std::cout << "client_interface::wait_for_work() <multiple>: WAIT_TIMEOUT occurred. event_code = " << event_code << "\n"; /// DEBUG
			return false;
		case WAIT_FAILED:
			//std::cout << "client_interface::wait_for_work() <multiple>: WAIT_FAILED: OS err: " <<  GetLastError() << "\n"; /// DEBUG
			return false;
		default:
			//std::cout << "client_interface::wait_for_work() <multiple>: Unknown error occured: OS err: " <<  GetLastError() << "\n"; /// DEBUG
			return false;
		}
		return false;
	}

	bool wait_for_work(HANDLE work, uint32_t timeout_milliseconds = INFINITE) {
		switch (::WaitForSingleObject(work, timeout_milliseconds)) {
		case WAIT_OBJECT_0:
			// The client was notified that there is work to do.
			if (::ResetEvent(work)) {
				return true;
			}
			else {
				//std::cout << this << " client ResetEvent(" << work << ") <4> failed. Error" << ::GetLastError() << "\n"; /// DEBUG
				return true; // Anyway.
			}
			return true;
		case WAIT_TIMEOUT:
			std::cout << this << " <4> client is running (timeout)\n"; /// DEBUG
			return false;
		case WAIT_FAILED: // Windows system error code: 6 = The handle is invalid.
			return false;
		}
		return false;
	}
#else // !defined(INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC) // Use Boost.Interprocess.
	bool wait_for_work(uint32_t timeout_milliseconds) {
		// boost::get_system_time() also works.
		const boost::system_time timeout
		= boost::posix_time::microsec_clock::universal_time()
		+boost::posix_time::milliseconds(timeout_milliseconds);
		
		boost::interprocess::scoped_lock
		<boost::interprocess::interprocess_mutex> lock(mutex_, timeout);
		
		if (!lock.owns()) {
			// The timeout_milliseconds time period has elapsed.
			//std::cout << this << " client is running (timeout, failed to acquire lock)\n"; /// DEBUG
			return false;
		}
		
		//std::cout << this << " client is waiting...\n"; /// DEBUG
		
		// Wait until at least one message has been pushed into some channel,
		// or the timeout_milliseconds time period has elapsed.
		if (work_.timed_wait(lock, timeout,
		boost::bind(&client_interface<value_type, allocator_type>::do_work,
		this)) == true) {
			// The client was notified.
			//std::cout << this << " client is running (notified)\n"; /// DEBUG
			set_predicate(false);
			return true;
		}
		
		// The timeout_milliseconds time period has elapsed.
		//std::cout << this << " client is running (wait timeout)\n"; /// DEBUG
		return false;
	}
#endif // defined(INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC) // Use Windows Events.
	
	/// Set the owner_id when acquiring and releasing a client_interface.
	/**
	 * @param oid The owner_id.
	 */
	void set_owner_id(owner_id oid) {
		_mm_sfence();
		owner_id_ = oid;
		_mm_sfence();
	}
	
	/// Get the owner_id which indicates which client process that owns this
	/// client_interface. Used by the clean-up mechanism.
	/**
	 * @return The owner_id.
	 */
	owner_id& get_owner_id() {
		return owner_id_;
	}

	/// Set number of channels acquired by the client.
	/**
	 * @param n The number of channels allocated.
	 */
	void set_number_of_allocated_channels(uint32_t n) {
		_mm_sfence();
		allocated_channels_ = n;
		_mm_sfence();
	}
	
	/// Increment the number of channels acquired by the client by 1.
	/**
	 * @return The number of channels allocated.
	 */
	uint32_t increment_number_of_allocated_channels() {
		_InterlockedIncrement((LONG*) &allocated_channels_);
		return allocated_channels_;
	}
	
	/// Decrement the number of channels acquired by the client by 1.
	/**
	 * @return The number of channels allocated.
	 */
	uint32_t decrement_number_of_allocated_channels() {
		_InterlockedDecrement((LONG*) &allocated_channels_);
		return allocated_channels_;
	}
	
	/// Get the number of allocated channels. Used by the clean-up mechanism.
	/**
	 * @return The number of allocated channels.
	 */
	uint32_t allocated_channels() const {
		return allocated_channels_;
	}
	
	resource_map& get_resource_map() {
		return resource_map_;
	}
	
	void set_chunk_flag(std::size_t index) {
		resource_map_.set_chunk_flag(index);
	}
	
	void clear_chunk_flag(std::size_t index) {
		resource_map_.clear_chunk_flag(index);
	}
	
	void set_channel_flag(std::size_t scheduler_num, std::size_t channel_num) {
		resource_map_.set_channel_flag(scheduler_num, channel_num);
	}
	
	void clear_channel_flag(std::size_t scheduler_num, std::size_t channel_num)
	{
		resource_map_.clear_channel_flag(scheduler_num, channel_num);
	}
	
	/// TODO: Remove this debug function.
	/**
	 * @param n Is the channel number the caller want to know if
	 *		it is owned (true) or not owned (false). The information may be
	 *		incorrect once it is returned. Use for debug output.
	 */
	bool is_channel_owner(uint32_t n) const {
		return resource_map_.owns_channel(n);
	}

	bool is_chunk_owner(uint32_t n) const {
		return resource_map_.owns_chunk(n);
	}

#if defined(INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC) // Use Windows Events.
	HANDLE get_work_event() const {
		return work_;
	}

	/// Get the work notify name, used to open the event. In order to reduce the
	/// time taken to open the work_ event the name is cached. Otherwise the
	/// work notify name have to be formated before opening it.
	/**
	 * @return A const wchar_t pointer to the work notify name string in the
	 *		format: L"Local\<segment_name>_notify_client_<id>". For example:
	 *		L"Local\starcounter_PERSONAL_MYDB_64_notify_client_9".
	 */
	const wchar_t* work_notify_name() const {
		return work_notify_name_;
	}
#endif // defined(INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC) // Use Windows Events.
	
private:
#if defined(INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC) // Use Windows Events.
	HANDLE work_;
	char cache_line_pad_0_[CACHE_LINE_SIZE
	-sizeof(HANDLE) // work_
	];
#else // !defined(INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC) // Use Boost.Interprocess.
	boost::interprocess::interprocess_mutex mutex_;
	// Condition to wait when all of this clients channels out queues are empty.
	boost::interprocess::interprocess_condition work_;
	char cache_line_pad_0_[CACHE_LINE_SIZE
	-sizeof(boost::interprocess::interprocess_mutex) // mutex_
	-sizeof(boost::interprocess::interprocess_condition) // work_
	];
#endif // defined(INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC) // Use Windows Events.
	
	volatile bool notify_;
	char cache_line_pad_1_[CACHE_LINE_SIZE
	-sizeof(bool) // notify_
	];
	
	bool predicate_; // Is synchronized by mutex_.
	char cache_line_pad_2_[CACHE_LINE_SIZE
	-sizeof(bool) // predicate_
	];
	
	// The owner of this client_interface also owns resources marked in the
	// resource_map_.
	owner_id owner_id_;
	volatile uint32_t allocated_channels_;
	char cache_line_pad_3_[CACHE_LINE_SIZE
	-sizeof(owner_id) // owner_id_
	-sizeof(uint32_t) // allocated_channels_
	];
	
	resource_map resource_map_;

#if defined(INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC) // Use Windows Events.
	// In order to reduce the time taken to open the work_ event the name is
	// cached. Otherwise the name have to be formated before opening it.
	wchar_t work_notify_name_[segment_and_notify_name_size];
#endif // defined(INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC) // Use Windows Events.
};

typedef simple_shared_memory_allocator<channel_number>
shm_alloc_for_the_client_interfaces2;

typedef client_interface<channel_number, shm_alloc_for_the_client_interfaces2>
client_interface_type;

} // namespace core
} // namespace starcounter

//#include "impl/client_interface.hpp"

#endif // STARCOUNTER_CORE_CLIENT_INTERFACE_HPP
