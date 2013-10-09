//
// chunk_pool_list.hpp
//
// Copyright © 2006-2013 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//
// Thread-Safety
// The thread-safety of the chunk_pool_list is the same as the thread-safety of
// containers in most STL implementations. This means the chunk_pool_list is not
// thread-safe.
//

#ifndef STARCOUNTER_CORE_CHUNK_POOL_LIST_HPP
#define STARCOUNTER_CORE_CHUNK_POOL_LIST_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#include <cstddef>
#include "../common/chunk.hpp"

namespace starcounter {
namespace core {

// The list is based on linking chunks that are stored in the the chunks themselves.
template<typename T>
class chunk_pool_list {
public:
	// Type definitions.
	typedef T value_type;
	typedef value_type& reference;
	typedef const value_type& const_reference;
	typedef size_t size_type;

	enum {
		link_terminator = chunk_type::link_terminator
	};
	
	chunk_pool_list()
	: front_(link_terminator),
	back_(link_terminator),
	size_(0),
	chunk_(0) {}
	
	/// set_chunk_ptr() stores the address of the first chunk in an array of
	/// chunks, so that operations on the chunk_pool_list can be done. The
	/// address is relative to the thread operating on the chunk_pool_list.
	/**
	 * @param p The address of chunk[0], in the array of chunks.
	 */
	void set_chunk_ptr(chunk_type* p);
	
	/// size() returns the number of elements in the list. 
	/**
	 * @return The number of elements in the list. 
	 */
	size_type size() const;
	
	/// empty() checks whether the queue is empty.
	/**
	 * @return true if the queue is empty, false otherwise.
	 */
	bool empty() const;
	
	/// Removes all elements from the list.
	void clear();
	
	/// front() returns a reference to the first element in the chunk_pool_list.
	/// Calling front() on an empty container is undefined.
	/**
	 * @return A reference to the first element in the chunk_pool_list.
	 */
	reference front();
	
	/// front() returns a const reference to the first element in the chunk_pool_list.
	/// Calling front() on an empty container is undefined.
	/**
	 * @return A const reference to the first element in the chunk_pool_list.
	 */
	/* constexpr */ const_reference front() const;
	
	/// pop_front() removes the first element of the queue. 
	void pop_front();
	
	/// push_front() prepends the given element value to the beginning of the queue.
	/**
	 * @param n A reference to the link that will point to the popped chunk
	 *		if returning true.
	 */
	void push_front(value_type n);
	
	// Returns a reference to chunk[n] relative to the process address space.
	chunk_type& chunk(chunk_index n);
	
	/// Acquire linked chunks.
	/**
	 * @param head Will upon return contain the head of a linked chain of chunks
	 *		if successfuly acquired the requested size.
	 * @param chunks_to_acquire The number of chunks to allocate as 1..N linked chunks.
	 * @return true if successfully acquired the linked chunks, otherwise false.
	 */
	bool acquire_linked_chunks(chunk_index& head, size_type chunks_to_acquire);
	
	/// Release linked chunks.
	/**
	 * @param head The head of the linked chunks, will upon return be set
	 *		to chunk_type::link_terminator.
	 */
	void release_linked_chunks(chunk_index& head);
	
private:
	value_type front_;
	value_type back_;
	size_type size_;
	
	// chunk_ is a pointer relative to the process address space.
	chunk_type* chunk_;
};

} // namespace core
} // namespace starcounter

#include "impl/chunk_pool_list.hpp"

#endif // STARCOUNTER_CORE_CHUNK_POOL_LIST_HPP
