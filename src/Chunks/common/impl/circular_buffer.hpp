//
// impl/circular_buffer.hpp
//
// Copyright © 2006-2012 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//
// http://www.boost.org/doc/libs/1_46_1/libs/circular_buffer/doc/circular_buffer.html
//
// This is a modified version of boost::circular_buffer, which it uses as the
// underlaying container, but has a reduced (simplified) interface.
//
// Thread-Safety
// The thread-safety of the circular_buffer is the same as the thread-safety of
// containers in most STL implementations. This means the circular_buffer is not
// thread-safe.
//

#ifndef STARCOUNTER_CORE_IMPL_CIRCULAR_BUFFER_HPP
#define STARCOUNTER_CORE_IMPL_CIRCULAR_BUFFER_HPP

// Implementation

namespace starcounter {
namespace core {

template<class T, class Alloc>
inline circular_buffer<T, Alloc>::circular_buffer(size_type buffer_capacity,
const allocator_type& alloc)
: container_(buffer_capacity, alloc),
spin_count_(0) {}

template<class T, class Alloc>
inline typename circular_buffer<T, Alloc>::size_type
circular_buffer<T, Alloc>::size() const {
	return container_.size();
}

template<class T, class Alloc>
inline typename circular_buffer<T, Alloc>::size_type
circular_buffer<T, Alloc>::capacity() const {
	return container_.capacity();
}

template<class T, class Alloc>
inline bool circular_buffer<T, Alloc>::empty() const {
	return container_.empty();
}

template<class T, class Alloc>
inline bool circular_buffer<T, Alloc>::full() const {
	return container_.full();
}

template<class T, class Alloc>
inline bool circular_buffer<T, Alloc>::push_front(param_type item) {
	if (is_not_full()) {
		container_.push_front(item);
		// The item was pushed.
		return true;
	}
	// The item was not pushed because the buffer is full.
	return false; 
}

template<class T, class Alloc>
inline bool circular_buffer<T, Alloc>::pop_front(value_type* item) {
	if (is_not_empty()) {
		*item = container_.front();
        container_.pop_front();
		// The item was popped.
		return true;
	}
	// The item was not popped because the buffer is empty.
	return false;
}

template<class T, class Alloc>
inline bool circular_buffer<T, Alloc>::is_not_empty() const {
	return !container_.empty();
}

template<class T, class Alloc>
inline bool circular_buffer<T, Alloc>::is_not_full() const {
	return !container_.full();
}

} // namespace core
} // namespace starcounter

#endif // STARCOUNTER_CORE_IMPL_BOUNDED_BUFFER_HPP
