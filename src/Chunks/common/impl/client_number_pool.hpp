//
// impl/client_number_pool.hpp
//
// Copyright © 2006-2012 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//
// Implementation of class client_number_pool.
//

#ifndef STARCOUNTER_CORE_IMPL_CLIENT_NUMBER_POOL_HPP
#define STARCOUNTER_CORE_IMPL_CLIENT_NUMBER_POOL_HPP

// Implementation

namespace starcounter {
namespace core {

#if defined (IPC_CLIENT_NUMBER_POOL_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
///=============================================================================
template<class T, std::size_t N>
inline client_number_pool<T, N>::client_number_pool
(const char* segment_name)
: size_(0) {
	clear_buffer();
	clear_mask();

    if (segment_name != 0) {
		char notify_name[segment_and_notify_name_size];
		std::size_t length;

		// Create the not_empty_notify_name_ and the not_empty_ event.
		
		// Format: "Local\<segment_name>_client_number_pool_not_empty".
		if ((length = _snprintf_s(notify_name, _countof(notify_name),
		segment_and_notify_name_size -1 /* null */, "Local\\%s_"
		"client_number_pool_not_empty", segment_name)) < 0) {
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
		
		// Format: "Local\<segment_name>_client_number_pool_not_full".
		if ((length = _snprintf_s(notify_name, _countof(notify_name),
		segment_and_notify_name_size -1 /* null */, "Local\\%s_"
		"client_number_pool_not_full", segment_name)) < 0) {
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

template<class T, std::size_t N>
inline typename client_number_pool<T, N>::size_type
client_number_pool<T, N>::size() const {
	return size_;
}

template<class T, std::size_t N>
inline typename client_number_pool<T, N>::size_type
client_number_pool<T, N>::capacity() const {
	return buffer_capacity;
}

template<class T, std::size_t N>
inline bool client_number_pool<T, N>::empty() const {
	return size_ == 0;
}

template<class T, std::size_t N>
inline bool client_number_pool<T, N>::full() const {
	return size_ == capacity();
}

template<class T, std::size_t N>
inline bool client_number_pool<T, N>::insert(value_type item,
client_interface_type* client_interface_base, owner_id id,
smp::spinlock::milliseconds timeout) {
	smp::spinlock::scoped_lock lock(spinlock(), id.get(),
	timeout -timeout.tick_count());

	if (lock.owns()) {
		if (item < capacity()) {
			if (elem_[item] == 0) {
				elem_[item] = 1;

				std::size_t i = item >> 6;
				std::size_t bit = item & 63;
				mask_[i] |= 1ULL << bit;

				// Successfully inserted.
				return true;
			}
		}
	}

	// Not inserted.
	return false;
}

template<class T, std::size_t N>
inline bool client_number_pool<T, N>::erase(value_type item,
client_interface_type* client_interface_base, owner_id id,
smp::spinlock::milliseconds timeout) {
	smp::spinlock::scoped_lock lock(spinlock(), id.get(),
	timeout -timeout.tick_count());

	if (lock.owns()) {
		if (item < capacity()) {
			if (elem_[item] == 1) {
				elem_[item] = 0;
				std::size_t i = item >> 6;
				std::size_t bit = item & 63;
				mask_[i] &= ~(1 << bit);

				// Successfully erased.
				return true;
			}
		}
	}
	
	// Not erased.
	return false;
}

template<class T, std::size_t N>
inline bool client_number_pool<T, N>::acquire(value_type* item,
client_interface_type* client_interface_base, owner_id id,
smp::spinlock::milliseconds timeout) {
	smp::spinlock::scoped_lock lock(spinlock(), id.get(),
	timeout -timeout.tick_count());
	
	if (lock.owns()) {
		for (std::size_t i = 0; i < masks; ++i) {
			for (mask_type mask = mask_[i]; mask; mask &= mask -1) {
				std::size_t bit = bit_scan_forward(mask);
				std::size_t n = (i << 6) +bit;

				if (elem_[n] == 1) {
					// 0 = not an entry (place holder.)
					// 1 = a free entry.
					// > 1 = an allocated entry.
					elem_[n] = id.get();
					mask_[i] = mask & ~(1 << bit);
                    ++size_;
					client_interface_base[n].set_owner_id(id);
					*item = n;

					// Successfully acquired.
					return true;
				}
				else {
					// The mask is not up to date. Should be impossible.
				}
			}
		}
	}

	// Not acquired.
	return false;
}

template<class T, std::size_t N>
inline bool client_number_pool<T, N>::release(value_type item,
client_interface_type* client_interface_base, owner_id id,
smp::spinlock::milliseconds timeout) {
	smp::spinlock::scoped_lock lock(spinlock(), id.get(),
	timeout -timeout.tick_count());
	
	if (lock.owns()) {
		if (item < capacity()) {
			elem_[item] = 1;
			client_interface_base[item].set_owner_id(owner_id::none);
			std::size_t i = item >> 6;
			std::size_t bit = item & 63;
			mask_[i] |= 1ULL << bit;
            --size_;

			// Successfully released.
			return true;
		}
	}

	// Not released.
	return false;
}

template<class T, std::size_t N>
inline void client_number_pool<T, N>::adjust_mask() {
	clear_mask();
	
	for (size_type i = 0; i < buffer_capacity; ++i) {
		if (elem_[i] == 1) {
			mask_[i >> 6] |= 1ULL << (i & 63);
		}
	}
}

template<class T, std::size_t N>
inline void client_number_pool<T, N>::adjust_size() {
	size_type sum = 0;
	
	// Sum up the population count of the masks.
	for (std::size_t i = 0; i < masks; ++i) {
		sum += population_count(mask_[i]);
	}

	// Assign the sum to the size_.
	size_ = sum;
}

template<class T, std::size_t N>
inline bool client_number_pool<T, N>::if_locked_with_id_recover_and_unlock
(smp::spinlock::locker_id_type id) {
	if (spinlock().is_locked_with_id(id)) {
		// Release elements marked with id.
		for (size_type i = 0; i < buffer_capacity; ++i) {
			if (elem_[i] != id) {
				continue;
			}
			else {
				// The element is marked with id. Mark it as free.
				elem_[i] = 1;
			}
		}

		adjust_mask();
		adjust_size();
		spinlock().unlock();
		return true;
	}

	// The client_number_pool was not locked with id. No recovery to be done here.
	return false;
}

template<class T, std::size_t N>
inline bool client_number_pool<T, N>::is_not_empty() const {
	return size() > 0;
}

template<class T, std::size_t N>
inline bool client_number_pool<T, N>::is_not_full() const {
	return size() < capacity();
}

///=============================================================================
#else // !defined (IPC_CLIENT_NUMBER_POOL_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
template<class T, class Alloc>
inline client_number_pool<T, Alloc>::client_number_pool(size_type
buffer_capacity, const allocator_type& alloc)
: container_(buffer_capacity, alloc), unread_(0) {}

template<class T, class Alloc>
inline typename client_number_pool<T, Alloc>::size_type
client_number_pool<T, Alloc>::size() const {
	return unread_;
}

template<class T, class Alloc>
inline typename client_number_pool<T, Alloc>::size_type
client_number_pool<T, Alloc>::capacity() const {
	return container_.capacity();
}

template<class T, class Alloc>
inline bool client_number_pool<T, Alloc>::empty() const {
	return unread_ == 0;
}

template<class T, class Alloc>
inline bool client_number_pool<T, Alloc>::full() const {
	return unread_ == capacity();
}

template<class T, class Alloc>
inline bool client_number_pool<T, Alloc>::push_front(param_type item,
client_interface_type* client_interface_base, uint32_t spin_count, uint32_t
timeout_milliseconds) {
	// Spin at most spin_count number of times, and try to push the item...
	for (std::size_t s = 0; s < spin_count; ++s) {
		if (try_push_front(item, client_interface_base)) {
			// The item was successfully pushed.
			return true;
		}
	}

	// Failed to aquire the lock while spinning. If failing to aquire the lock
	// when trying again now, the thread waits until the queue is not full.
	{
		// The timeout is used multiple times below, while time passes, so all
		// synchronization must be completed before the timeout_milliseconds
		// time period has elapsed.
		const boost::system_time timeout = boost::posix_time::microsec_clock
		::universal_time() +boost::posix_time::milliseconds
		(timeout_milliseconds);

		boost::interprocess::scoped_lock
		<boost::interprocess::interprocess_mutex> lock(mutex_, timeout);

		if (!lock.owns()) {
			// The timeout_milliseconds time period has elapsed. Failed to push
			// the item.
			return false;
		}

		// Wait until the queue is not full, or timeout occurs.
		if (not_full_.timed_wait(lock, timeout,
		boost::bind(&client_number_pool<value_type,
		allocator_type>::is_not_full, this)) == true) {
			// The queue is not full so the item can be pushed.
			container_.push_front(item);
			++unread_;
			lock.unlock();
			not_empty_.notify_one();
			// Successfully popped the item.
			return true;
		}
		else {
			// The timeout_milliseconds time period has elapsed. Failed to push
			// the item.
			return false;
		}
	}
}

template<class T, class Alloc>
inline bool client_number_pool<T, Alloc>::pop_back(value_type* item,
client_interface_type* client_interface_base, owner_id oid, uint32_t spin_count,
uint32_t timeout_milliseconds) {
	// Spin at most spin_count number of times, and try to pop the item...
	for (std::size_t s = 0; s < spin_count; ++s) {
		if (try_pop_back(item, client_interface_base, oid)) {
			// The item has been popped.
			return true;
		}
	}
	
	// Failed to aquire the lock while spinning. If failing to aquire the lock
	// when trying again now, the thread blocks.
	{
		// The timeout is used multiple times below, while time passes, so all
		// synchronization must be completed before the timeout_milliseconds
		// time period has elapsed.
		const boost::system_time timeout = boost::posix_time::microsec_clock
		::universal_time() +boost::posix_time::milliseconds
		(timeout_milliseconds);

		boost::interprocess::scoped_lock
		<boost::interprocess::interprocess_mutex> lock(mutex_, timeout);
		
		if (!lock.owns()) {
			// The timeout_milliseconds time period has elapsed. Failed to pop
			// the item.
			return false;
		}

		// Wait until the queue is not empty, or timeout occurs.
		if (not_empty_.timed_wait(lock, timeout,
		boost::bind(&client_number_pool<value_type, allocator_type>
		::is_not_empty, this)) == true) {
			// The queue is not empty so the item can be popped.
			value_type temp_item;
			temp_item = container_[unread_ -1];
			client_interface_base[temp_item].set_owner_id(oid);
			*item = temp_item;
			--unread_;
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
}

template<class T, class Alloc>
inline bool client_number_pool<T, Alloc>::try_push_front(param_type item,
client_interface_type* client_interface_base) {
	boost::interprocess::scoped_lock<boost::interprocess::interprocess_mutex>
	lock(mutex_, boost::interprocess::try_to_lock);
	
	if (lock.owns()) {
		// If the buffer is full, we shall not block.
		if (unread_ == container_.capacity()) {
			// The buffer is full so we can't push the item.
			lock.unlock();
			return false;
		}
		
		client_interface_base[item].set_owner_id(owner_id::none);
		
		// A fence is needed to ensure that the owner_id is set to none before
		// pushing the client_number to the client_number_pool. If the client
		// process terminates here, the client_number is leaked.
		_mm_sfence();
		container_.push_front(item);
		++unread_;
		lock.unlock();
		not_empty_.notify_one();
		
		// The item was pushed.
		return true;
	}
	
	// The lock was not aquired so the item was not pushed.
	return false;
}

template<class T, class Alloc>
inline bool client_number_pool<T, Alloc>::try_pop_back(value_type* item,
client_interface_type* client_interface_base, owner_id oid) {
	boost::interprocess::scoped_lock<boost::interprocess::interprocess_mutex>
	lock(mutex_, boost::interprocess::try_to_lock);

	if (lock.owns()) {
		// If the buffer is empty, we shall not block.
		if (unread_ == 0) {
			// The buffer is empty so there is no item to pop.
			lock.unlock();
			return false;
		}
		value_type temp_item;
		temp_item = container_[unread_ -1];
		client_interface_base[temp_item].set_owner_id(oid);
		*item = temp_item;
		--unread_;
		lock.unlock();
		not_full_.notify_one();
		// The item was popped.
		return true;
	}
	// The lock was not aquired so the item was not popped.
	return false;
}

template<class T, class Alloc>
inline std::size_t client_number_pool<T, Alloc>
::release_all_client_numbers_with_owner_id(owner_id oid, uint32_t
timeout_milliseconds) {
	const boost::system_time timeout = boost::posix_time::microsec_clock
	::universal_time() +boost::posix_time::milliseconds
	(timeout_milliseconds);
	
	boost::interprocess::scoped_lock
	<boost::interprocess::interprocess_mutex> lock(mutex_, timeout);
	
	if (lock.owns()) {
		std::size_t released_client_numbers = 0;
		
		for (value_type i = 0; i < buffer_capacity; ++i) {
			if (owner_id_[i] == oid) {
				container_.push_front(i);
				++unread_;
				//.second = owner_id::none;
				++released_client_numbers;
			}
		}
		
		if (released_client_numbers > 0) {
			not_empty_.notify_one();
		}
		
		return released_client_numbers;
	}
	else {
		// The timeout_milliseconds time period has elapsed and the operation
		// has failed. Could not see if there are any client_number(s) to be
		// released since the lock wasn't acquired.
		return -1;
	}
}

template<class T, class Alloc>
inline bool client_number_pool<T, Alloc>::is_not_empty() const {
	return unread_ > 0;
}

template<class T, class Alloc>
inline bool client_number_pool<T, Alloc>::is_not_full() const {
	return unread_ < container_.capacity();
}
///=============================================================================
#endif // defined (IPC_CLIENT_NUMBER_POOL_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)

} // namespace core
} // namespace starcounter

#endif // STARCOUNTER_CORE_IMPL_CLIENT_NUMBER_POOL_HPP
