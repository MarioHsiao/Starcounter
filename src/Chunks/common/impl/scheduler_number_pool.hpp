//
// impl/scheduler_number_pool.hpp
//
// Copyright © 2006-2012 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//
//
// Implementation of class scheduler_number_pool.
// Only Microsoft Windows OS is supported for now.
//
// Multiple consumer and producer threads are allowed.
// Synchronized using smp::spinlock and Windows Events.
//

#ifndef STARCOUNTER_CORE_IMPL_SCHEDULER_NUMBER_POOL_HPP
#define STARCOUNTER_CORE_IMPL_SCHEDULER_NUMBER_POOL_HPP

// Implementation

namespace starcounter {
namespace core {

template<class T, class Alloc>
inline scheduler_number_pool<T, Alloc>::scheduler_number_pool
(size_type buffer_capacity, const allocator_type& alloc,
const char* segment_name, int32_t id)
: container_(buffer_capacity, alloc),
unread_(0),
waiting_consumers_(0L),
waiting_producers_(0L) {
	if (segment_name != 0) {
		char notify_name[segment_and_notify_name_size];
		std::size_t length;

		// Create the not_empty_notify_name_ and the not_empty_ event.
		
		// Format: "Local\<segment_name>_scheduler_number_pool_<id>_not_empty".
		if ((length = _snprintf_s(notify_name, _countof(notify_name),
		segment_and_notify_name_size -1 /* null */, "Local\\%s_"
		"scheduler_number_pool_%u_not_empty", segment_name, id)) < 0) {
			return; // Throw exception error_code.
		}
		notify_name[length] = '\0';

		/// TODO: Fix insecure
		if ((length = mbstowcs(not_empty_notify_name_, notify_name,
		segment_name_size)) < 0) {
			// Failed to convert notify_name to multi-byte string.
			return; // Throw exception error_code.
		}
		not_empty_notify_name_[length] = L'\0';
		
		if ((not_empty_ = ::CreateEvent(NULL, TRUE, FALSE,
		not_empty_notify_name_)) == NULL) {
			// Failed to create event.
			return; // Throw exception error_code.
		}

		// Create the not_full_notify_name_ and the not_full_ event.
		
		// Format: "Local\<segment_name>_scheduler_number_pool_<id>_not_full".
		if ((length = _snprintf_s(notify_name, _countof(notify_name),
		segment_and_notify_name_size -1 /* null */, "Local\\%s_"
		"scheduler_number_pool_%u_not_full", segment_name, id)) < 0) {
			return; // Throw exception error_code.
		}
		notify_name[length] = '\0';

		/// TODO: Fix insecure
		if ((length = mbstowcs(not_full_notify_name_, notify_name,
		segment_name_size)) < 0) {
			// Failed to convert notify_name to multi-byte string.
			return; // Throw exception error_code.
		}
		not_full_notify_name_[length] = L'\0';
		
		if ((not_full_ = ::CreateEvent(NULL, TRUE, FALSE,
		not_full_notify_name_)) == NULL) {
			// Failed to create event.
			return; // Throw exception error_code.
		}
	}
	else {
		// Error: No segment name. Throw exception error_code.
	}
}
//------------------------------------------------------------------------------

template<class T, class Alloc>
inline typename scheduler_number_pool<T, Alloc>::size_type
scheduler_number_pool<T, Alloc>::size() const {
	return unread_;
}

template<class T, class Alloc>
inline typename scheduler_number_pool<T, Alloc>::size_type
scheduler_number_pool<T, Alloc>::capacity() const {
	return container_.capacity();
}

template<class T, class Alloc>
inline bool scheduler_number_pool<T, Alloc>::empty() const {
	return unread_ == 0;
}

#if 0
template<class T, class Alloc>
inline bool scheduler_number_pool<T, Alloc>::full() const {
	return unread_ == capacity();
}
#endif


///=============================================================================
/// TODO: If a process terminates in the call to
/// push_front(), the waiting_producers_ counter may have
/// the wrong value and the IPC monitor shall correct it, by
/// subtracting the number of waiting_producers_ (those
/// threads that terminated.) One solution that comes to
/// mind is to use an array with waiting_producers_[clients]
/// The client_number is obtained by finding the
/// client_interface(s) with a matching owner_id of the
/// terminated process. Then the count in
/// waiting_producers_[X] is set to 0, where X is a set of
/// client numbers (those with a matching owner_id of the
/// terminated process.)
///=============================================================================
template<class T, class Alloc>
inline bool scheduler_number_pool<T,Alloc>::push_front(param_type item, uint32_t
spin_count, smp::spinlock::milliseconds timeout) {
	timeout.add_tick_count();
	unsigned int count = 0;
	
	// Spin at most spin_count number of times, and try to push the item...
	for (std::size_t s = 0; s < spin_count; ++s) {
		if (try_push_front(item)) {
			// The item was successfully pushed.
			return true;
		}

		if (++count != elapsed_time_check) {
			_mm_pause();
			continue;
		}

		if (timeout -timeout.tick_count() > 0) {
			SwitchToThread();
			count = 0;
			continue;
		}
		else {
			// Timeout.
			return false;
		}
	}

	// Failed to aquire the lock while spinning. If failing to aquire the lock
	// when trying again now, the thread waits until the queue is not full.
	// The timeout is used multiple times below, while time passes, so all
	// synchronization must be completed before the timeout has elapsed.
	{
		smp::spinlock::scoped_lock lock(spinlock(), /* locker_id_type, */
		timeout -timeout.tick_count()); /// P01 - TODO: SPINLOCK_ID

		if (!lock.owns()) {
			// The timeout_milliseconds time period has elapsed. Failed to push
			// the item.
			return false; // P08
		}

		if (is_not_full()) { // P02
			// The queue is not full so the item can be pushed.
			container_.push_front(item); // P03
			
			if (++unread_ == 1) { // P05
				// The queue was empty. Notify that the queue is not empty.
				::SetEvent(not_empty_);
			}
			lock.unlock(); // P06
			return true; // P07
		}
		else {
			// The queue is full, preparing to wait.
prepare_to_wait:
			_InterlockedExchangeAdd(&waiting_producers_, +1L); // P09
			lock.unlock(); // P10

			// Wait until the queue is not full, or timeout occurs.
			switch (::WaitForSingleObject(not_full_, timeout
			-timeout.tick_count())) { // P11
			case WAIT_OBJECT_0:
				// A notification that the queue is not full was received.
				_InterlockedExchangeAdd(&waiting_producers_, -1L); // P12

				lock.timed_lock(spinlock(), /* locker_id_type, */
				timeout -timeout.tick_count()); /// P13 - TODO: SPINLOCK_ID

				if (lock.owns()) {
					// Acquired the lock.
					if (waiting_producers() == 0) { // P14
						if (::ResetEvent(not_full_)) {
							std::cout << this << " <2> scheduler_number_pool::push_front(): Successfully reset the not_full_ event.\n";
						}
						else {
							std::cout << this << " <2> scheduler_number_pool::push_front(): Failed to reset the not_full_ event.\n";
						}
						
						if (is_not_full()) { // Same as P02
							// The queue is not full so the item can be pushed.
							container_.push_front(item); // Same as P03

							if (++unread_ == 1) { // Same as P05
								// The queue was empty. Notify that the queue is
								// not empty.
								::SetEvent(not_empty_);
							}
							lock.unlock(); // Same as P06
							return true; // Same as P07
						}
						else {
							// The queue is full.
							goto prepare_to_wait;
						}
					}
				}
				else {
					// The timeout time period has elapsed. Failed to push the
					// item.
					return false; // P17
				}

				// Control never passes this point.
				break;

			case WAIT_TIMEOUT:
				// A timeout occurred.
				_InterlockedExchangeAdd(&waiting_producers_, -1L); // P15
				return false; // P16

			case WAIT_FAILED:
				// An error occurred.
				return false;
			}
		}
	}
	
	// Timeout.
	return false;
}

template<class T, class Alloc>
inline bool scheduler_number_pool<T, Alloc>::pop_back(value_type* item, uint32_t
spin_count, uint32_t timeout_milliseconds) {
	// Spin at most spin_count number of times, and try to pop the item...
	do { /// ADDED BECAUSE OF CANCELING OUT CODE BELOW - REMOVE INFINITE LOOP
	///for (std::size_t s = 0; s < spin_count; ++s) {
		if (try_pop_back(item)) {
			// The item has been popped.
			return true;
		}
		_mm_pause();
	///}
	} while (true); /// ADDED BECAUSE OF CANCELING OUT CODE BELOW - REMOVE INFINITE LOOP

#if 0 /// Some of this code need to be changed...canceling out
	// Failed to aquire the lock while spinning. If failing to aquire the lock
	// when trying again now, the thread blocks.
	{
#if defined (IPC_SCHEDULER_INTERFACE_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
		smp::spinlock::scoped_lock lock(spinlock(), /* locker_id_type, */
		timeout_milliseconds); /// TODO: SPINLOCK_ID
#else // !defined (IPC_SCHEDULER_INTERFACE_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
		// The timeout is used multiple times below, while time passes, so all
		// synchronization must be completed before the timeout_milliseconds
		// time period has elapsed.
		const boost::system_time timeout = boost::posix_time::microsec_clock
		::universal_time() +boost::posix_time::milliseconds
		(timeout_milliseconds);

		boost::interprocess::scoped_lock
		<boost::interprocess::interprocess_mutex> lock(mutex_, timeout);
#endif // defined (IPC_SCHEDULER_INTERFACE_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
		
		if (!lock.owns()) {
			// The timeout_milliseconds time period has elapsed. Failed to pop
			// the item.
			return false;
		}

		// Wait until the queue is not empty, or timeout occurs.
		if (not_empty_.timed_wait(lock, timeout,
		boost::bind(&scheduler_number_pool<value_type, allocator_type>::is_not_empty,
		this)) == true) {
			// The queue is not empty so the item can be popped.
			*item = container_[--unread_];
			lock.unlock();
			not_full_.notify_one();

			// Successfully popped the item.
			return true;
		}
		else {
			// The timeout_milliseconds time period has elapsed. Failed to pop
			// the item.
			return false;
		}
	}
#endif /// Some of this code need to be changed...canceling out
}

template<class T, class Alloc>
inline bool scheduler_number_pool<T, Alloc>::try_push_front(param_type item) {
#if defined (IPC_SCHEDULER_INTERFACE_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
	smp::spinlock::scoped_lock lock(spinlock(), /* locker_id_type, */
	smp::spinlock::scoped_lock::try_to_lock_type()); /// TODO: SPINLOCK_ID
#else // !defined (IPC_SCHEDULER_INTERFACE_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
	boost::interprocess::scoped_lock<boost::interprocess::interprocess_mutex>
	lock(mutex_, boost::interprocess::try_to_lock);
#endif // defined (IPC_SCHEDULER_INTERFACE_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
	
	if (lock.owns()) {
		// If the buffer is full, we shall not block.
		if (unread_ == container_.capacity()) {
			// The buffer is full so we can't push the item.
			lock.unlock();
			return false;
		}
		
		container_.push_front(item);
		++unread_;
		lock.unlock();
#if defined (IPC_SCHEDULER_INTERFACE_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
		if (::SetEvent(not_empty_)) {
			// Successfully notified.
			std::cout << this << " <1> scheduler_number_pool::try_push_front(): Successfully notified.\n";
			return true;
		}
		else {
			// Error. Failed to notify.
			std::cout << this << " <1> scheduler_number_pool::try_push_front(): Failed to notify.\n";
			return true;
		}
#else // !defined (IPC_SCHEDULER_INTERFACE_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
		not_empty_.notify_one();
#endif // defined (IPC_SCHEDULER_INTERFACE_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
		
		// The item was pushed.
		return true;
	}

	// The lock was not aquired so the item was not pushed.
	return false;
}

template<class T, class Alloc>
inline bool scheduler_number_pool<T, Alloc>::try_pop_back(value_type* item) {
#if defined (IPC_SCHEDULER_INTERFACE_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
	smp::spinlock::scoped_lock lock(spinlock(), /* locker_id_type, */
	smp::spinlock::scoped_lock::try_to_lock_type()); /// TODO: SPINLOCK_ID
#else // !defined (IPC_SCHEDULER_INTERFACE_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
	boost::interprocess::scoped_lock<boost::interprocess::interprocess_mutex>
	lock(mutex_, boost::interprocess::try_to_lock);
#endif // defined (IPC_SCHEDULER_INTERFACE_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)

	if (lock.owns()) {
		// If the buffer is empty, we shall not block.
		if (unread_ == 0) {
			// The buffer is empty so there is no item to pop.
			lock.unlock();
			return false;
		}

		*item = container_[--unread_];
		lock.unlock();
#if defined (IPC_SCHEDULER_INTERFACE_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
		if (::SetEvent(not_full_)) {
			// Successfully notified.
			std::cout << this << " <1> scheduler_number_pool::try_pop_back(): Successfully notified.\n";
			return true;
		}
		else {
			// Error. Failed to notify.
			std::cout << this << " <1> scheduler_number_pool::try_pop_back(): Failed to notify.\n";
			return true;
		}
#else // !defined (IPC_SCHEDULER_INTERFACE_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
		not_full_.notify_one();
#endif // defined (IPC_SCHEDULER_INTERFACE_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)

		// The item was popped.
		return true;
	}
	// The lock was not aquired so the item was not popped.
	return false;
}

template<class T, class Alloc>
inline bool scheduler_number_pool<T, Alloc>::is_not_empty() const {
	return unread_ > 0;
}

template<class T, class Alloc>
inline bool scheduler_number_pool<T, Alloc>::is_not_full() const {
	return unread_ < container_.capacity();
}

} // namespace core
} // namespace starcounter

#endif // STARCOUNTER_CORE_IMPL_SCHEDULER_NUMBER_POOL_HPP
