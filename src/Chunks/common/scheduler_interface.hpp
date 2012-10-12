//
// scheduler_interface.hpp
//
// Copyright � 2006-2012 Starcounter AB. All rights reserved.
// Starcounter� is a registered trademark of Starcounter AB.
//

#ifndef STARCOUNTER_CORE_SCHEDULER_INTERFACE_HPP
#define STARCOUNTER_CORE_SCHEDULER_INTERFACE_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#include <cstddef>
#include <boost/cstdint.hpp>
#include <iostream> /// debug
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
# include <atlstr.h>
# include <intrin.h>
#undef WIN32_LEAN_AND_MEAN
#include "../common/channel.hpp"
#include "../common/channel_mask.hpp"
#include "../common/client_interface.hpp"
#include "../common/overflow_buffer.hpp"
#include "../common/chunk_pool.hpp"
#include "../common/macro_definitions.hpp"

namespace starcounter {
namespace core {

typedef uint32_t channel_scan_counter_type;

/// class scheduler_interface.
/**
 * @param T The type of the elements stored in the channel_number_queue.
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
template<class T, class T2,
class Alloc = std::allocator<T>, class Alloc2 = std::allocator<T2> >
class scheduler_interface {
public:
	// Basic types
	
	// The type of queue for channel_number.
	typedef bounded_buffer<T, Alloc> channel_number_queue_type;
	
	// The type of queue for chunk_pool.
	typedef chunk_pool<T2, Alloc2> chunk_pool_type;
	
	// The type of queue for overflow_pool.
	typedef overflow_buffer<T2, Alloc2> overflow_pool_type;
	
	// The type of an allocator used in the channel_number_queue.
	typedef typename channel_number_queue_type::allocator_type
	channel_number_queue_allocator_type;
	
	// The type of an allocator used in the chunk_pool.
	typedef typename chunk_pool_type::allocator_type
	chunk_pool_allocator_type;
	
	// The type of an allocator used in the overflow_buffer.
	typedef typename overflow_pool_type::allocator_type
	overflow_pool_allocator_type;
	
	enum {
		channel_scan_mask_words = 4 // 4 * 64-bit = 256 (channels)
	};
	
	// Construction/Destruction.
	
	/// Construct a scheduler_interface.
	/**
	 * @param channel_number_queue_capacity The maximum number of elements which
	 *		can be stored in the bounded_buffer.
	 * @param channel_number_queue_alloc The channel_number_queue allocator.
	 * @throws "An allocation error" if memory is exhausted (std::bad_alloc if
	 *		the standard allocator is used).
	 */
	explicit scheduler_interface(
	std::size_t channel_number_queue_capacity,
	std::size_t chunk_pool_capacity,
	std::size_t overflow_pool_capacity,
	const channel_number_queue_allocator_type& channel_number_queue_alloc
	= channel_number_queue_allocator_type(),
	const chunk_pool_allocator_type& chunk_pool_alloc
	= chunk_pool_allocator_type(),
	const overflow_pool_allocator_type& overflow_pool_alloc
	= overflow_pool_allocator_type(),
	const char* segment_name = 0,
	int32_t id = -1)
	: channel_number_(channel_number_queue_capacity,
	channel_number_queue_alloc),
	chunk_pool_(chunk_pool_capacity, chunk_pool_alloc),
	overflow_pool_(overflow_pool_capacity, overflow_pool_alloc),
	channel_scan_mask_(),
	channel_scan_counter_(0),
	notify_(false),
	predicate_(false),
	client_interface_(0) {
#if defined(CONNECTIVITY_USE_EVENTS_TO_SYNC)
		if (segment_name != 0) {
			char notify_name[segment_and_notify_name_size];
			wchar_t w_notify_name[segment_and_notify_name_size];
			std::size_t length;

			// Format: "Local\<segment_name>_notify_scheduler_<id>".
			if ((length = _snprintf_s(notify_name, _countof(notify_name),
			segment_and_notify_name_size -1 /* null */,
			"Local\\%s_notify_scheduler_%u", segment_name, id)) < 0) {
				return; // error
			}
			notify_name[length] = '\0';
			//std::cout << "notify_name: " << notify_name << "\n"; /// DEBUG

			/// TODO: Fix insecure
			if ((length = mbstowcs(w_notify_name, notify_name, segment_name_size)) < 0) {
				//std::cout << this
				//<< ": Failed to convert segment_name to multi-byte string. Error: "
				//<< GetLastError() << "\n";
				return; // throw exception
			}
			w_notify_name[length] = L'\0';

			if ((work_ = ::CreateEvent(NULL, TRUE, FALSE, w_notify_name)) == NULL) {
				// Failed to create event.
				//std::cout << this << ": Failed to create event with error: "
				//<< GetLastError() << "\n"; /// DEBUG
				return; // throw exception
			}
		}
		else {
			// TODO: Handle the error - no segment name. Throw an exception.
		}
#endif // defined(CONNECTIVITY_USE_EVENTS_TO_SYNC)
	}
	
#if defined(CONNECTIVITY_USE_EVENTS_TO_SYNC)
	~scheduler_interface() {
		::CloseHandle(work_);
	}
#endif // defined(CONNECTIVITY_USE_EVENTS_TO_SYNC)
	
	void pop_back_channel_number(channel_number* the_channel_number) {
		channel_number_.pop_back(the_channel_number);
	}
	
	void push_front_channel_number(channel_number the_channel_number) {
		// Release the_channel_number to this channel_number_ queue.
		channel_number_.push_front(the_channel_number);
	}
	
	/// Access to this schedulers channel_number queue. It is not synchronized,
	/// and can only be used by schedulers. It works without synchronization
	/// since scheduler threads are co-operative, so there is an indirect sync.
	channel_number_queue_type& channel_number_queue() {
		return channel_number_;
	}
	
	/// Access to this schedulers private chunk_pool. It is not synchronized,
	/// and can only be used by schedulers. It works without synchronization
	/// since scheduler threads are co-operative, so there is an indirect sync.
	chunk_pool_type& chunk_pool() {
		return chunk_pool_;
	}
	
	/// Access to this schedulers private overflow_pool. It is not synchronized,
	/// and can only be used by schedulers. It works without synchronization
	/// since scheduler threads are co-operative, so there is an indirect sync.
	overflow_pool_type& overflow_pool() {
		return overflow_pool_;
	}
	
	void set_channel_number_flag(channel_number the_channel_number) {
		channel_scan_mask_.set_channel_number_flag(the_channel_number);
	}
	
	void clear_channel_number_flag(channel_number the_channel_number) {
		channel_scan_mask_.clear_channel_number_flag(the_channel_number);
	}
	
	uint64_t get_channel_mask_word(std::size_t n) const {
		return channel_scan_mask_.get_channel_mask_word(n);
	}
	
	/// TODO: Remove this debug function.
	/**
	 * @param n Is the channel number the caller want to know if
	 *		it is owned (true) or not owned (false). The information may be
	 *		incorrect once it is returned. Use for debug output.
	 */
	bool is_channel_owner(std::size_t n) const {
		return channel_scan_mask_.get_channel_number_flag(n);
	}

	/// Get spin count.
	/**
	 * @return The spin_count.
	 */
	std::size_t get_spin_count() const {
		return channel_number_.get_spin_count();
	}
	
	//--------------------------------------------------------------------------
	// Each time a scan of a set of channels have been completed, regardless of
	// the number of channels that have been scanned, the scheduler calls
	// increment_channel_scan_counter(). The id�a is that when a client wants to
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
	
	//the scheduler have seen the update of the channel_scan_mask_ and the client
	// can then release the channel. However, the scheduler may take a
	// relatively long time to complete a scan. We may enter a periodic sleep
	// of 1 ms and poll the get_channel_scan_counter() once every 1 ms.
	channel_scan_counter_type get_channel_scan_counter() const {
		return channel_scan_counter_;
	}
	
	// A scheduler calls increment_channel_scan_counter() each time it has
	// completed a scan of all channels in the channel_scan_mask_.
	void increment_channel_scan_counter() {
		channel_scan_counter_type next = channel_scan_counter_ +1;
		_mm_mfence();
		channel_scan_counter_ = next;
		_mm_mfence();
	}
	
	bool get_notify_flag() const {
		return notify_;
	}
	
	void set_notify_flag(bool s) {
		_mm_mfence();
		notify_ = s;
		_mm_mfence();
	}
	
	//--------------------------------------------------------------------------
	// Clients call notify() each time they push a message on a channel, or mark
	// the channel for release, in order to wake up the scheduler it
	// communicates with, if it is waiting.
#if defined(CONNECTIVITY_USE_EVENTS_TO_SYNC)
	// Use Windows Events to synchronize.
	void notify(HANDLE work) {
		if (false /*get_notify_flag() == false*/) { /// DEBUG TEST - FORCE NOTIFICATION
			// No need to notify the scheduler because it is not waiting.
			return;
		}
		else {
			if (!::SetEvent(work)) {
				//std::cout << this << ": SetEvent(" << work_ << ") <2> failed with the error: "
				//<< ::GetLastError() << "\n"; /// DEBUG
			}
		}
	}
#else // !defined(CONNECTIVITY_USE_EVENTS_TO_SYNC)
	// Use Boost.Interprocess to synchronize.
	void notify() {
		if (get_notify_flag() == false) {
			// No need to notify the scheduler because it is not waiting.
			return;
		}
		else {
			boost::interprocess::scoped_lock
			<boost::interprocess::interprocess_mutex> lock(mutex_);
			
			// Need to notify the scheduler that a message has been pushed into
			// some queue. The scheduler is probably waiting.
			set_predicate(true);
			lock.unlock();
			work_.notify_one(); // In the client interface we notify all.
		}
	}
#endif // defined(CONNECTIVITY_USE_EVENTS_TO_SYNC)
	
	//--------------------------------------------------------------------------
	// The monitor call try_to_notify_scheduler_to_do_clean_up() if a client
	// process has crashed, in order to wake up the scheduler if it is waiting.
	// A scheduler that is woken up is required to see if the channel is marked
	// for clean-up. Maybe this is not at all required, it may be irrelevent.
#if defined(CONNECTIVITY_USE_EVENTS_TO_SYNC)
	// Use Windows Events to synchronize.
	bool try_to_notify_scheduler_to_do_clean_up(uint32_t timeout_milliseconds) {
		if (!::SetEvent(work_)) {
			//std::cout << this << ": SetEvent(" << work_ << ") <3> failed with the error: "
			//<< ::GetLastError() << "\n"; /// DEBUG
			return false;
		}
		return true;
	}
#else // !defined(CONNECTIVITY_USE_EVENTS_TO_SYNC)
	// Use Boost.Interprocess to synchronize.
	bool try_to_notify_scheduler_to_do_clean_up(uint32_t timeout_milliseconds) {
		const boost::system_time timeout = boost::posix_time::microsec_clock
		::universal_time() +boost::posix_time::milliseconds
		(timeout_milliseconds);
		
		boost::interprocess::scoped_lock
		<boost::interprocess::interprocess_mutex> lock(mutex_, timeout);
		
		if (lock.owns()) {
			// Need to notify the scheduler to clean-up.
			set_predicate(true);
			lock.unlock();
			work_.notify_one(); // In the client interface we notify all.
			return true;
		}
		else {
			// The timeout_milliseconds time period has elapsed and the
			// operation has failed. Could not notify the scheduler on this
			// channel.
			return false;
		}
	}
#endif // defined(CONNECTIVITY_USE_EVENTS_TO_SYNC)
	
	// Setting predicate to true means the condition is met
	// and the wait is over, the thread waiting will not wait any more.
	// Setting the predicate to false means the condition is not met
	// and the thread waiting will keep waiting.
	// Access to the predicate must be synchronized.
	void set_predicate(bool state) {
		predicate_ = state;
	}
	
	bool do_work() const {
		return predicate_;
	}
	
	/// Scheduler's call wait_for_work() if they don't find any work to do.
	/// It is a "timed" function that can fail.
	/**
	 * @param timeout_milliseconds The number of milliseconds to wait before
	 *		a timeout may occur.
	 * @return false if the call is returning because the time period specified
	 *		by timeout_milliseconds has elapsed, true otherwise.
	 */
#if defined(CONNECTIVITY_USE_EVENTS_TO_SYNC)
	// Use Windows Events to synchronize.
	bool wait_for_work(unsigned int timeout_milliseconds) {
		//std::cout << this << " scheduler is waiting...\n"; /// DEBUG
		switch (::WaitForSingleObject(work_, timeout_milliseconds)) {
		case WAIT_OBJECT_0:
			// The scheduler was notified that there is work to do.
			//std::cout << this << " scheduler is running (NOTIFIED)\n"; /// DEBUG
			
			if (::ResetEvent(work_)) {
				//std::cout << "::ResetEvent(" << work_ << ")\n"; /// DEBUG
				return true;
			}
			else {
				//std::cout << this << " scheduler ResetEvent() failed. Error" << ::GetLastError() << "\n"; /// DEBUG
				return true; // Anyway.
			}
			return true;
		case WAIT_TIMEOUT:
			//std::cout << this << " scheduler is running (timeout)\n"; /// DEBUG
			return false;
		case WAIT_FAILED: // Windows system error code: 6 = The handle is invalid.
			//std::cout << this << " scheduler WaitForSingleObject() failed. Error" << ::GetLastError() << "\n"; /// DEBUG
			return false;
		}
	}
#else // !defined(CONNECTIVITY_USE_EVENTS_TO_SYNC)
	// Use Boost.Interprocess to synchronize.
	bool wait_for_work(unsigned int timeout_milliseconds) {
		// boost::get_system_time() also works.
		const boost::system_time timeout
		= boost::posix_time::microsec_clock::universal_time()
		+boost::posix_time::milliseconds(timeout_milliseconds);
		
		boost::interprocess::scoped_lock
		<boost::interprocess::interprocess_mutex> lock(mutex_);
		
		if (!lock.owns()) {
			// The timeout_milliseconds time period has elapsed.
			//std::cout << this << " scheduler is running (timeout no lock)\n"; /// DEBUG
			return false;
		}
		
		//std::cout << this << " scheduler is waiting...\n"; /// DEBUG

		// Wait until at least one message has been pushed into some channel,
		// or the timeout_milliseconds time period has elapsed.
		if (work_.timed_wait(lock, timeout,
		boost::bind(&scheduler_interface_type::do_work, this)) == true) {
			// The scheduler was notified that there is work to do.
			//std::cout << this << " scheduler is running (notified)\n"; /// DEBUG
			set_predicate(false);
			return true;
		}
		
		// The timeout_milliseconds time period has elapsed.
		// Shall the predicate be set to false on timeout? I think not.
		//std::cout << this << " scheduler is running (timeout)\n"; /// DEBUG
		return false;
	}
#endif // defined(CONNECTIVITY_USE_EVENTS_TO_SYNC)
	
	uint64_t get_client_interface_as_qword() {
		boost::interprocess::scoped_lock
		<boost::interprocess::interprocess_mutex> lock(client_interface_mutex_);
		return client_interface_;
	}
	
	//--------------------------------------------------------------------------
	// Only one thread ever accesses this - the scheduler thread calls this once
	// to set the client_interface pointer. After that it is not changed for as
	// long as the scheduler owns this scheduler_interface. Therefore, no
	// synchronization is needed. Such assumptions are dangerous and must be
	// thought through, so in the mean time I use synchronization anyway.
	void set_client_interface(client_interface_type* p) {
		boost::interprocess::scoped_lock
		<boost::interprocess::interprocess_mutex> lock(client_interface_mutex_);
		client_interface_ = reinterpret_cast<uint64_t>(p);
	}
	
private:
	// Condition to wait when the all of this scheduler's channels in queues,
	// and the scheduler channels in queue are empty.
#if defined(CONNECTIVITY_USE_EVENTS_TO_SYNC)
	// Use Windows Events to synchronize.
	HANDLE work_;
#else // !defined(CONNECTIVITY_USE_EVENTS_TO_SYNC)
	// Use Boost.Interprocess to synchronize.
	boost::interprocess::interprocess_condition work_;
	boost::interprocess::interprocess_mutex mutex_;
#endif // defined(CONNECTIVITY_USE_EVENTS_TO_SYNC)
	
	// Sync access to client_interface - probably not needed.
	boost::interprocess::interprocess_mutex client_interface_mutex_;
	channel_number_queue_type channel_number_;
	chunk_pool_type chunk_pool_;
	overflow_pool_type overflow_pool_;
	char cache_line_pad_0_[CACHE_LINE_SIZE]; // Not needed if on another cache line.
	channel_mask<channels> channel_scan_mask_;
	char cache_line_pad_1_[CACHE_LINE_SIZE];
	volatile channel_scan_counter_type channel_scan_counter_;
	char cache_line_pad_2_[CACHE_LINE_SIZE -sizeof(channel_scan_counter_type)];
	volatile bool notify_;
	char cache_line_pad_3_[CACHE_LINE_SIZE -sizeof(bool)];
	volatile bool predicate_;
	char cache_line_pad_4_[CACHE_LINE_SIZE -sizeof(bool)];
	
	// The scheduler that have allocated this interface must store a pointer to
	// the client_interface here, which will be relative to its own address
	// space. This must be done before the client have a chance to access this
	// scheduler_interface. The client will copy this pointer to the channel
	// when a client allocates a channel.
	//
	// We store the pointer as uint64_t to provide compatibility between 64-bit
	// server and 32-bit client.
	uint64_t client_interface_; // client_interface_type*
	char cache_line_pad_5_[CACHE_LINE_SIZE -sizeof(uint64_t)];
};

typedef starcounter::core::simple_shared_memory_allocator<channel_number>
shm_alloc_for_the_scheduler_interfaces2;

typedef starcounter::core::simple_shared_memory_allocator<chunk_index>
shm_alloc_for_the_scheduler_interfaces2b;

typedef scheduler_interface<channel_number, chunk_index,
shm_alloc_for_the_scheduler_interfaces2,
shm_alloc_for_the_scheduler_interfaces2b>
scheduler_interface_type;

} // namespace core
} // namespace starcounter

//#include "impl/scheduler_interface.hpp"

#endif // STARCOUNTER_CORE_SCHEDULER_INTERFACE_HPP
