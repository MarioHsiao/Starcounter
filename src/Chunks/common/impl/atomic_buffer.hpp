//
// impl/atomic_buffer.hpp
//
// Copyright © 2006-2011 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//
// Implementation of class atomic_buffer.
//

#ifndef STARCOUNTER_CORE_IMPL_ATOMIC_BUFFER_HPP
#define STARCOUNTER_CORE_IMPL_ATOMIC_BUFFER_HPP

// Implementation

namespace starcounter {
namespace core {

#if 0 /// TODO: Implementation

template<typename T, int32_t N>
inline atomic_buffer<T, N>::atomic_buffer()
: head_(0), tail_(0) {}



// Specialization
template<typename T>
inline atomic_buffer<T, 8>::atomic_buffer()
: head_(0), tail_(0) {}

template<class T, class Alloc>
inline typename atomic_buffer<T, Alloc>::size_type
atomic_buffer<T, Alloc>::size() const {
	return unread_;
}

template<class T, class Alloc>
inline typename atomic_buffer<T, Alloc>::size_type
atomic_buffer<T, Alloc>::capacity() const {
	return container_.capacity();
}

#if 0
template<class T, class Alloc>
inline bool atomic_buffer<T, Alloc>::empty() const {
	return unread_ == 0;
}

template<class T, class Alloc>
inline bool atomic_buffer<T, Alloc>::full() const {
	return unread_ == capacity();
}
#endif

template<class T, class Alloc>
inline void atomic_buffer<T, Alloc>::push_front(param_type item) {

}

template<class T, class Alloc>
inline void atomic_buffer<T, Alloc>::pop_back(value_type* item) {

}

template<class T, class Alloc>
inline bool atomic_buffer<T, Alloc>::try_push_front(param_type item) {

}

template<class T, class Alloc>
inline bool atomic_buffer<T, Alloc>::try_pop_back(value_type* item) {

}

template<class T, class Alloc>
inline std::size_t atomic_buffer<T, Alloc>::get_spin_count() const {
	return spin_count_;
}

template<class T, class Alloc>
inline void atomic_buffer<T, Alloc>::set_spin_count(std::size_t spin_count) {
	spin_count_ = spin_count;
}

template<class T, class Alloc>
inline bool atomic_buffer<T, Alloc>::is_not_empty() const {
	return unread_ > 0;
}

template<class T, class Alloc>
inline bool atomic_buffer<T, Alloc>::is_not_full() const {
	return unread_ < container_.capacity();
}

#endif /// TODO: Implementation

} // namespace core
} // namespace starcounter

#endif // STARCOUNTER_CORE_IMPL_ATOMIC_BUFFER_HPP
