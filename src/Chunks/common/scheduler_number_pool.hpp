//
// scheduler_number_pool.hpp
//
// Copyright © 2006-2012 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//
// http://www.boost.org/doc/libs/1_46_1/libs/circular_buffer/doc/circular_buffer.html
// The example shows how the circular_buffer can be utilized as an underlying
// container of the bounded buffer.
//
// This is a modified version of scheduler_number_pool that uses spin locks and takes
// an allocator template parameter. Multiple consumer and producer threads are
// allowed. It is not directly based on atomics and memory order, it uses a
// boost::mutex to protect the queue.
//

#ifndef STARCOUNTER_CORE_SCHEDULER_NUMBER_POOL_HPP
#define STARCOUNTER_CORE_SCHEDULER_NUMBER_POOL_HPP

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
#include <boost/date_time/posix_time/posix_time_types.hpp> // No I/O just types.
#include <boost/date_time/microsec_time_clock.hpp>
#include <boost/thread/thread.hpp>
#include <boost/call_traits.hpp>
#include <boost/bind.hpp>
#if defined(_MSC_VER) // Windows
# include <intrin.h>
#else 
# error Compiler not supported.
#endif // (_MSC_VER)

namespace starcounter {
namespace core {

// The scheduler_number_pool is normally used in a producer-consumer mode when producer
// threads produce items and store them in the container and consumer threads
// remove these items and process them. The bounded buffer has to guarantee that
// producers do not insert items into the container when the container is full,
// that consumers do not try to remove items when the container is empty, and
// that each produced item is consumed by exactly one consumer.

// The scheduler_number_pool relies on Boost Threads and Boost Bind libraries and Boost
// call_traits utility.

// The push_front() method is called by the producer thread in order to insert a
// new item into the buffer. The method locks the mutex and waits until there is
// a space for the new item. (The mutex is unlocked during the waiting stage and
// has to be regained when the condition is met.) If there is a space in the
// buffer available, the execution continues and the method inserts the item at
// the end of the circular_buffer. Then it increments the number of unread items
// and unlocks the mutex (in case an exception is thrown before the mutex is
// unlocked, the mutex is unlocked automatically by the destructor of the
// scoped_lock). At last the method notifies one of the consumer threads waiting
// for a new item to be inserted into the buffer.

// The pop_back() method is called by the consumer thread in order to read the
// next item from the buffer. The method locks the mutex and waits until there
// is an unread item in the buffer. If there is at least one unread item, the
// method decrements the number of unread items and reads the next item from the
// circular_buffer. Then it unlocks the mutex and notifies one of the producer
// threads waiting for the buffer to free a space for the next item.

// The pop_back() method does not remove the item but the item is left in the
// circular_buffer which then replaces it with a new one (inserted by a producer)
// when the circular_buffer is full. This technique is more effective than removing
// the item explicitly by calling the pop_back() method of the circular_buffer.
// This claim is based on the assumption that an assignment (replacement) of a
// new item into an old one is more effective than a destruction (removal) of an
// old item and a consequent inplace construction (insertion) of a new item.

/// class scheduler_number_pool
/**
 * @param T The type of the elements stored in the scheduler_number_pool.
 * @par Type Requirements T
 *		The T has to be SGIAssignable (SGI STL defined combination of Assignable
 *		and CopyConstructible), and EqualityComparable and/or LessThanComparable
 *		if the scheduler_number_pool will be compared with another container.
 * @param Alloc The allocator type used for all internal memory management.
 * @par Type Requirements Alloc
 *		The Alloc has to meet the allocator requirements imposed by STL.
 * @par Default Alloc
 *		std::allocator<T>
 */
template<class T, class Alloc = std::allocator<T> >
class scheduler_number_pool {
public:
	// Basic types
	
	// The type of the underlying container used in the scheduler_number_pool.
	typedef boost::circular_buffer<T, Alloc> container_type;
	
	// The type of elements stored in the scheduler_number_pool.
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
	
	// The type of an allocator used in the scheduler_number_pool.
	//typedef Alloc allocator_type;
	typedef typename container_type::allocator_type allocator_type;
	
	// Helper types
	
	// A type representing the "best" way to pass the value_type to a method.
	typedef typename boost::call_traits<value_type>::param_type param_type;
	
	// A type representing the "best" way to return the value_type from a const method.
	typedef typename boost::call_traits<value_type>::param_type return_type;
	
	// Construction/Destruction.
	
	/// Create an empty scheduler_number_pool with the specified capacity.
	/**
	 * @param buffer_capacity The maximum number of elements which can be stored
	 *		in the scheduler_number_pool.
	 * @param alloc The allocator.
	 * @throws "An allocation error" if memory is exhausted (std::bad_alloc if
	 *		the standard allocator is used).
	 * @par Complexity
	 *		Constant.
	 */
	explicit scheduler_number_pool(size_type buffer_capacity, const allocator_type&
	alloc = allocator_type());
	
	// Size and capacity
	
	/// Get the number of elements currently stored in the scheduler_number_pool.
	/**
	 * @return The number of elements stored in the scheduler_number_pool.
	 *		This is the number of unread elements.
	 * @throws Nothing.
	 * @par Exception Safety
	 *		No-throw.
	 * @par Complexity
	 *		Constant (in the size of the scheduler_number_pool).
	 */
	size_type size() const;
	
	/// Get the capacity of the scheduler_number_pool.
	/**
	 * @return The maximum number of elements which can be stored in the
	 *		scheduler_number_pool.
	 * @throws Nothing.
	 * @par Exception Safety
	 *		No-throw.
	 * @par Complexity
	 *		Constant (in the size of the scheduler_number_pool).
	 */
	size_type capacity() const;
	
	/// Is the scheduler_number_pool empty?
	/**
	 * @return true if there are no elements stored in the scheduler_number_pool;
	 *		false otherwise.
	 * @throws Nothing.
	 * @par Exception Safety
	 *		No-throw.
	 * @par Complexity
	 *		Constant (in the size of the scheduler_number_pool).
	 */
	//bool empty() const;
	
	/// Is the scheduler_number_pool full?
	/**
	 * @return true if the number of elements stored in the scheduler_number_pool
	 *		equals the capacity of the scheduler_number_pool; false otherwise.
	 * @throws Nothing.
	 * @par Exception Safety
	 *		No-throw.
	 * @par Complexity
	 *		Constant (in the size of the scheduler_number_pool).
	 */
	//bool full() const;
	
	/// Push item to the front of the queue.
	/**
	 * @param item The item to be pushed to the front of the queue.
	 *		param_type represents the "best" way to pass a parameter of type
	 *		value_type to a method.
	 * @param spin_count The number of times to try locking (without blocking)
	 *		before waiting for the acquisition of the lock. It is affected by
	 *		the type of processor and the clock rate, etc.
	 * @param timeout_milliseconds The number of milliseconds to wait before a
	 *		timeout may occur.
	 * @return false if failing to push the item before the time period
	 *		specified by timeout_milliseconds has elapsed, true otherwise.
	 */
	bool push_front(param_type item, uint32_t spin_count = 1000000, uint32_t
	timeout_milliseconds = 10000);
	
	/// Pop item from the back of the queue.
	/**
	 * @param item The item to be popped from the back of the queue.
	 * @param spin_count The number of times to try locking (without blocking)
	 *		before waiting for the acquisition of the lock. It is affected by
	 *		the type of processor and the clock rate, etc.
	 * @param timeout_milliseconds The number of milliseconds to wait before a
	 *		timeout may occur.
	 * @return false if failing to pop the item before the time period specified
	 *		by timeout_milliseconds has elapsed, true otherwise.
	 */
	bool pop_back(value_type* item, uint32_t spin_count = 1000000, uint32_t
	timeout_milliseconds = 10000);
	
	bool try_push_front(param_type item);
	bool try_pop_back(value_type* item);
	
private:
	scheduler_number_pool(const scheduler_number_pool&);
	scheduler_number_pool& operator=(const scheduler_number_pool&);

	bool is_not_empty() const;
	bool is_not_full() const;
	
	size_type unread_;
	std::size_t spin_count_;
	container_type container_;
	
	// Process-shared synchronization:
	
	// SMP spinlock to protect access to the queue.
	//smp::spinlock mutex_;
	
	// Event to wait when the queue is not empty.
	HANDLE not_empty_;
	
	// Event to wait when the queue is not full.
	HANDLE not_full_;
//------------------------------------------------------------------------------
	// In order to reduce the time taken to open the not_empty_ and not_full_
	// events the names are cached. Otherwise the names have to be formated
	// before opening them.
	wchar_t not_empty_notify_name_[segment_and_notify_name_size];
	wchar_t not_full_notify_name_[segment_and_notify_name_size];
};

} // namespace core
} // namespace starcounter

#include "impl/scheduler_number_pool.hpp"

#endif // STARCOUNTER_CORE_SCHEDULER_NUMBER_POOL_HPP
