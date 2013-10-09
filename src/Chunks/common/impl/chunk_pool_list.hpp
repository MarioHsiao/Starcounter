//
// impl/chunk_pool_list.hpp
//
// Copyright © 2006-2013 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//
// Thread-Safety
// The thread-safety of the chunk_pool_list is the same as the thread-safety of
// containers in most STL implementations. This means the chunk_pool is not
// thread-safe.
//

#ifndef STARCOUNTER_CORE_IMPL_CHUNK_POOL_LIST_HPP
#define STARCOUNTER_CORE_IMPL_CHUNK_POOL_LIST_HPP

// Implementation

namespace starcounter {
namespace core {

template<class T>
inline void chunk_pool_list<T>::set_chunk_ptr(chunk_type* p) {
	chunk_ = p;
}

template<class T>
inline typename chunk_pool_list<T>::size_type
chunk_pool_list<T>::size() const {
	return size_;
}

template<class T>
inline bool chunk_pool_list<T>::empty() const {
	//return front_ == link_terminator;
	return size_ == 0;
}

template<class T>
inline void chunk_pool_list<T>::clear() {
	while (!empty()) {
		value_type temp_front = chunk(front()).get_next();
		chunk(front()).terminate_next();
		front_ = temp_front;
	}
	
	front_ = link_terminator;
	back_ = link_terminator;
	size_ = 0;
}

template<class T>
inline typename chunk_pool_list<T>::reference
chunk_pool_list<T>::front() {
	return front_;
}

template<class T>
inline /* constexpr */ typename chunk_pool_list<T>::const_reference
chunk_pool_list<T>::front() const {
	return front_;
}

template<class T>
inline void chunk_pool_list<T>::pop_front() {
	if (!empty()) {
		value_type temp_front = chunk(front()).get_next();
		chunk(front()).terminate_next();
		front_ = temp_front;
		--size_;
	}
}

template<class T>
inline void chunk_pool_list<T>::push_front(value_type n) {
	if (!empty()) {
		chunk(n).set_next(front_);
		front_ = n;
	}
	else {
		front_ = n;
		back_ = n;
		chunk(n).set_next(link_terminator);
	}
	
	++size_;
}

template<class T>
inline chunk_type& chunk_pool_list<T>::chunk(chunk_index n) {
	return chunk_[n];
}

template<class T>
inline bool chunk_pool_list<T>::acquire_linked_chunks(chunk_index& head,
size_type chunks_to_acquire) {
	// Check if enough space is available, assuming it is.
	if (chunks_to_acquire <= size()) {
		// Enough space is available, start linking chunks together.
		chunk_index prev;
		chunk_index current = front();
		pop_front();
		head = current;
		
		for (size_type i = 1; i < chunks_to_acquire; ++i) {
			prev = current;
			
			// Get the next chunk.
			current = front();
			pop_front();
			chunk(prev).set_link(current);
		}
		
		// Terminate the last chunk.
		chunk(current).terminate_link();
		
		// Successfully acquired the chunks and linked them.
		return true;
	}
	else {
		// Not enough space available. Failed to acquire linked chunks.
		return false;
	}
}

template<class T>
inline void chunk_pool_list<T>::release_linked_chunks(chunk_index& head) {
	chunk_index next;
	
	while (head != chunk_type::link_terminator) {
		next = chunk(head).get_link();
		push_front(head);
		head = next;
	}
}

} // namespace core
} // namespace starcounter

#endif // STARCOUNTER_CORE_IMPL_CHUNK_POOL_LIST_HPP
