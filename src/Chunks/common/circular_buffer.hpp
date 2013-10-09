//
// circular_buffer.hpp
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

#ifndef STARCOUNTER_CORE_CIRCULAR_BUFFER_HPP
#define STARCOUNTER_CORE_CIRCULAR_BUFFER_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

// Use -DBOOST_CB_DISABLE_DEBUG or -DNDEBUG flags
#define BOOST_CB_DISABLE_DEBUG

#include <cstddef>
#include <memory>
#include <boost/circular_buffer.hpp>
#include <boost/interprocess/sync/interprocess_mutex.hpp>
#include <boost/interprocess/sync/scoped_lock.hpp>
#include <boost/interprocess/sync/interprocess_condition.hpp>
#include <boost/thread/thread.hpp>
#include <boost/call_traits.hpp>
#include <boost/bind.hpp>

namespace starcounter {
namespace core {

// The circular_buffer relies on Boost Threads and Boost Bind libraries and Boost
// call_traits utility.

// The push_front() method is called in order to insert a new item into the buffer.
// If there is a space in the buffer available, the execution continues and the
// method inserts the item at the end of the circular_buffer. Then it increments
// the number of unread items.

// The pop_back() method is called in order to read the next item from the buffer.
// If there is at least one unread item, the method decrements the number of unread
// items and reads the next item from the circular_buffer.

// The pop_back() method does not remove the item but the item is left in the
// circular_buffer which then replaces it with a new one (inserted by a producer)
// when the circular_buffer is full. This technique is more effective than removing
// the item explicitly by calling the pop_back() method of the circular_buffer.
// This claim is based on the assumption that an assignment (replacement) of a
// new item into an old one is more effective than a destruction (removal) of an
// old item and a consequent inplace construction (insertion) of a new item.

/// class circular_buffer
/**
 * @param T The type of the elements stored in the circular_buffer.
 * @par Type Requirements T
 *      The T has to be SGIAssignable (SGI STL defined combination of Assignable
 *      and CopyConstructible), and EqualityComparable and/or LessThanComparable
 *      if the circular_buffer will be compared with another container.
 * @param Alloc The allocator type used for all internal memory management.
 * @par Type Requirements Alloc
 *      The Alloc has to meet the allocator requirements imposed by STL.
 * @par Default Alloc
 *      std::allocator<T>
 */
template<class T, class Alloc = std::allocator<T> >
class circular_buffer {
public:
	// Basic types
	
	// The type of the underlying container used in the circular_buffer.
	typedef boost::circular_buffer<T, Alloc> container_type;
	
	// The type of elements stored in the circular_buffer.
	typedef typename container_type::value_type value_type;
	
	// A pointer to an element.
	typedef typename container_type::pointer pointer;
	
	// A const pointer to the element.
	typedef typename container_type::const_pointer const_pointer;
	
	// A reference to an element.
	typedef typename container_type::reference reference;
	
	// A const reference to an element.
	typedef typename container_type::const_reference const_reference;
	
	// The distance type. (A signed integral type used to represent the distance
	// between two iterators.)
	typedef typename container_type::difference_type difference_type;
	
	// The size type. (An unsigned integral type that can represent any non-
	// negative value of the container's distance type.)
	typedef typename container_type::size_type size_type;

	// Capacity type.
	typedef typename container_type::capacity_type capacity_type;

	// The type of an allocator used in the circular_buffer.
	//typedef Alloc allocator_type;
	typedef typename container_type::allocator_type allocator_type;
	
	// Helper types
	
	// A type representing the "best" way to pass the value_type to a method.
	typedef typename boost::call_traits<value_type>::param_type param_type;
	
	// A type representing the "best" way to return the value_type from a const method.
	typedef typename boost::call_traits<value_type>::param_type return_type;
	
	// Construction/Destruction.
	
	/// Create an empty circular_buffer with the specified capacity.
	/**
	 * @param buffer_capacity The maximum number of elements which can be stored
	 *      in the circular_buffer.
	 * @param alloc The allocator.
	 * @throws "An allocation error" if memory is exhausted (std::bad_alloc if
	 *      the standard allocator is used).
	 * @par Complexity
	 *      Constant.
	 */
	explicit circular_buffer(size_type buffer_capacity = 0, const allocator_type&
	alloc = allocator_type());
	
	// Size and capacity
	
	/// Get the number of elements currently stored in the circular_buffer.
	/**
	 * @return The number of elements stored in the circular_buffer.
	 * @throws Nothing.
	 * @par Exception Safety
	 *      No-throw.
	 * @par Complexity
	 *      Constant (in the size of the circular_buffer).
	 */
	size_type size() const;

	/// Sets the new capacity of the circular buffer.
	void set_capacity(capacity_type new_capacity) {
		container_.set_capacity(new_capacity);
	}

	/// Get the capacity of the circular_buffer.
	/**
	 * @return The maximum number of elements which can be stored in the
	 *      circular_buffer.
	 * @throws Nothing.
	 * @par Exception Safety
	 *      No-throw.
	 * @par Complexity
	 *      Constant (in the size of the circular_buffer).
	 */
	size_type capacity() const;
	
	/// Is the circular_buffer empty?
	/**
	 * @return true if there are no elements stored in the circular_buffer;
	 *      false otherwise.
	 * @throws Nothing.
	 * @par Exception Safety
	 *      No-throw.
	 * @par Complexity
	 *      Constant (in the size of the circular_buffer).
	 */
	bool empty() const;
	
	/// Is the circular_buffer full?
	/**
	 * @return true if the number of elements stored in the circular_buffer
	 *      equals the capacity of the circular_buffer; false otherwise.
	 * @throws Nothing.
	 * @par Exception Safety
	 *      No-throw.
	 * @par Complexity
	 *      Constant (in the size of the circular_buffer).
	 */
	bool full() const;
	
	/// Push item to the front of the queue.
	/**
	 * @param item The item to be pushed to the front of the queue. The
	 *      param_type represents the "best" way to pass a parameter of type
	 *      value_type to a method
	 * @return true if the item was pushed, false if the buffer was full.
	 */
	bool push_front(param_type item);
	
	/// Pop item from the front of the queue.
	/**
	 * @param item The item to be popped from the front of the queue.
	 * @return true if the item was popped, false if the buffer was empty.
	 */
	bool pop_front(value_type* item);
	
private:
	circular_buffer(const circular_buffer&);
	circular_buffer& operator=(const circular_buffer&);
	
	bool try_push_front(param_type item);
	bool try_pop_back(value_type* item);
	
	bool is_not_empty() const;
	bool is_not_full() const;
	
	size_type unread_;
	std::size_t spin_count_;
	container_type container_;
};

} // namespace core
} // namespace starcounter

#include "impl/circular_buffer.hpp"

#endif // STARCOUNTER_CORE_CIRCULAR_BUFFER_HPP
