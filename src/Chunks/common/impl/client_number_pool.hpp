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
client_interface_type* base_client_interface, owner_id oid, uint32_t spin_count,
uint32_t timeout_milliseconds) {
	// Spin at most spin_count number of times, and try to pop the item...
	for (std::size_t s = 0; s < spin_count; ++s) {
		if (try_pop_back(item, base_client_interface, oid)) {
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
			base_client_interface[temp_item].set_owner_id(oid);
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

} // namespace core
} // namespace starcounter

#endif // STARCOUNTER_CORE_IMPL_CLIENT_NUMBER_POOL_HPP
