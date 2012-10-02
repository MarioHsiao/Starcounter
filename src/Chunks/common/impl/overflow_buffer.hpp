//
// impl/overflow_buffer.hpp
//
// Copyright © 2006-2012 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//
// http://www.boost.org/doc/libs/1_46_1/libs/overflow_buffer/doc/overflow_buffer.html
//
// This is a modified version of boost::overflow_buffer, which it uses as the
// underlaying container, but has a reduced (simplified) interface.
//
// Thread-Safety
// The thread-safety of the overflow_buffer is the same as the thread-safety of
// containers in most STL implementations. This means the overflow_buffer is not
// thread-safe.
//

#ifndef STARCOUNTER_CORE_IMPL_OVERFLOW_BUFFER_HPP
#define STARCOUNTER_CORE_IMPL_OVERFLOW_BUFFER_HPP

// Implementation

namespace starcounter {
namespace core {

template<class T, class Alloc>
inline overflow_buffer<T, Alloc>::overflow_buffer(size_type buffer_capacity,
const allocator_type& alloc)
: container_(buffer_capacity, alloc), unread_(0), spin_count_(0) {}

template<class T, class Alloc>
inline typename overflow_buffer<T, Alloc>::size_type
overflow_buffer<T, Alloc>::size() const {
	return unread_;
}

template<class T, class Alloc>
inline typename overflow_buffer<T, Alloc>::size_type
overflow_buffer<T, Alloc>::capacity() const {
	return container_.capacity();
}

template<class T, class Alloc>
inline bool overflow_buffer<T, Alloc>::empty() const {
	return unread_ == 0;
}

template<class T, class Alloc>
inline bool overflow_buffer<T, Alloc>::full() const {
	return unread_ == capacity();
}

template<class T, class Alloc>
inline bool overflow_buffer<T, Alloc>::push_front(param_type item) {
	// Overflow buffers can never be full, since it's capacity should be set to
	// the max number of items that exists. However, the check is done anyway,
	// since it is not expected that this method is called often.
	if (is_not_full()) {
		container_.push_front(item);
		++unread_;
		// The item was pushed.
		return true;
	}
	// The item was not pushed because the buffer is full.
	return false;
}

template<class T, class Alloc>
inline bool overflow_buffer<T, Alloc>::pop_back(value_type* item) {
	// Overflow buffers are expected to be empty most of the time.
	// This is in contrast to circular_buffer which assumes that the queue is
	// not empty most of the time.
	if (empty()) {
		// The item was not popped because the buffer is empty.
		return false;
	}
	
	*item = container_[--unread_];
	// The item was popped.
	return true;
}

template<class T, class Alloc>
inline bool overflow_buffer<T, Alloc>::is_not_empty() const {
	return unread_ > 0;
}

template<class T, class Alloc>
inline bool overflow_buffer<T, Alloc>::is_not_full() const {
	return unread_ < container_.capacity();
}

} // namespace core
} // namespace starcounter

#endif // STARCOUNTER_CORE_IMPL_OVERFLOW_BUFFER_HPP
