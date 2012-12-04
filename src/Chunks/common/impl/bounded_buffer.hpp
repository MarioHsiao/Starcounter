//
// impl/bounded_buffer.hpp
//
// Copyright © 2006-2012 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//
// http://www.boost.org/doc/libs/1_46_1/libs/circular_buffer/doc/circular_buffer.html
// The example shows how the circular_buffer can be utilized as an underlying
// container of the bounded buffer.
//
// This is a modified version of bounded_buffer that uses spin locks and takes
// an allocator template parameter. Multiple consumer and producer threads are
// allowed. It is not directly based on atomics and memory order, it uses a
// boost::mutex to protect the queue.
//
// Implementation of class bounded_buffer.
//

#ifndef STARCOUNTER_CORE_IMPL_BOUNDED_BUFFER_HPP
#define STARCOUNTER_CORE_IMPL_BOUNDED_BUFFER_HPP

// Implementation

namespace starcounter {
namespace core {

template<class T, class Alloc>
inline bounded_buffer<T, Alloc>::bounded_buffer(size_type buffer_capacity,
const allocator_type& alloc)
: container_(buffer_capacity, alloc), unread_(0), spin_count_(0) {}

template<class T, class Alloc>
inline typename bounded_buffer<T, Alloc>::size_type
bounded_buffer<T, Alloc>::size() const {
	return unread_;
}

template<class T, class Alloc>
inline typename bounded_buffer<T, Alloc>::size_type
bounded_buffer<T, Alloc>::capacity() const {
	return container_.capacity();
}

#if 0
template<class T, class Alloc>
inline bool bounded_buffer<T, Alloc>::empty() const {
	return unread_ == 0;
}

template<class T, class Alloc>
inline bool bounded_buffer<T, Alloc>::full() const {
	return unread_ == capacity();
}
#endif

template<class T, class Alloc>
inline bool bounded_buffer<T, Alloc>::push_front(param_type item, uint32_t
spin_count, uint32_t timeout_milliseconds) {
	// Spin at most spin_count number of times, and try to push the item...
	for (std::size_t s = 0; s < spin_count_; ++s) {
		if (try_push_front(item)) {
			// The item was successfully pushed.
			return true;
		}
		_mm_pause();
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
		boost::bind(&bounded_buffer<value_type, allocator_type>::is_not_full,
		this)) == true) {
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
inline bool bounded_buffer<T, Alloc>::pop_back(value_type* item, uint32_t
spin_count, uint32_t timeout_milliseconds) {
	// Spin at most spin_count number of times, and try to pop the item...
	for (std::size_t s = 0; s < spin_count_; ++s) {
		if (try_pop_back(item)) {
			// The item has been popped.
			return true;
		}
		_mm_pause();
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
		boost::bind(&bounded_buffer<value_type, allocator_type>::is_not_empty,
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
}

template<class T, class Alloc>
inline bool bounded_buffer<T, Alloc>::try_push_front(param_type item) {
	boost::interprocess::scoped_lock<boost::interprocess::interprocess_mutex>
	lock(mutex_, boost::interprocess::try_to_lock);
	
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
		not_empty_.notify_one();

		// The item was pushed.
		return true;
	}

	// The lock was not aquired so the item was not pushed.
	return false;
}

template<class T, class Alloc>
inline bool bounded_buffer<T, Alloc>::try_pop_back(value_type* item) {
	boost::interprocess::scoped_lock<boost::interprocess::interprocess_mutex>
	lock(mutex_, boost::interprocess::try_to_lock);

	if (lock.owns()) {
		// If the buffer is empty, we shall not block.
		if (unread_ == 0) {
			// The buffer is empty so there is no item to pop.
			lock.unlock();
			return false;
		}

		*item = container_[--unread_];
		lock.unlock();
		not_full_.notify_one();
		// The item was popped.
		return true;
	}
	// The lock was not aquired so the item was not popped.
	return false;
}

template<class T, class Alloc>
inline bool bounded_buffer<T, Alloc>::is_not_empty() const {
	return unread_ > 0;
}

template<class T, class Alloc>
inline bool bounded_buffer<T, Alloc>::is_not_full() const {
	return unread_ < container_.capacity();
}

} // namespace core
} // namespace starcounter

#endif // STARCOUNTER_CORE_IMPL_BOUNDED_BUFFER_HPP
