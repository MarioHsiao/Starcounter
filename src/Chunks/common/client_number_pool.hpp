//
// client_number_pool.hpp
//
// Copyright © 2006-2012 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#ifndef STARCOUNTER_CORE_CLIENT_NUMBER_POOL_HPP
#define STARCOUNTER_CORE_CLIENT_NUMBER_POOL_HPP

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

namespace starcounter {
namespace core {

/// class client_number_pool
/**
 * @param T The type of the elements stored in the client_number_pool.
 * @par Type Requirements T
 *		The T has to be SGIAssignable (SGI STL defined combination of Assignable
 *		and CopyConstructible), and EqualityComparable and/or LessThanComparable
 *		if the client_number_pool will be compared with another container.
 * @param Alloc The allocator type used for all internal memory management.
 * @par Type Requirements Alloc
 *		The Alloc has to meet the allocator requirements imposed by STL.
 * @par Default Alloc
 *		std::allocator<T>
 */
template<class T, class Alloc = std::allocator<T> >
class client_number_pool {
public:
	// Basic types
	
	// The type of the underlying container used in the client_number_pool.
	typedef boost::circular_buffer<T, Alloc> container_type;
	
	// The type of elements stored in the client_number_pool.
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
	
	// The type of an allocator used in the client_number_pool.
	//typedef Alloc allocator_type;
	typedef typename container_type::allocator_type allocator_type;
	
	// Helper types
	
	// A type representing the "best" way to pass the value_type to a method.
	typedef typename boost::call_traits<value_type>::param_type param_type;
	
	// A type representing the "best" way to return the value_type from a const
	// method.
	typedef typename boost::call_traits<value_type>::param_type return_type;
	
	// Construction/Destruction.
	
	/// Create an empty client_number_pool with the specified capacity.
	/**
	 * @param buffer_capacity The maximum number of elements which can be stored
	 *		in the client_number_pool.
	 * @param alloc The allocator.
	 * @throws "An allocation error" if memory is exhausted (std::bad_alloc if
	 *		the standard allocator is used).
	 * @par Complexity
	 *		Constant.
	 */
	explicit client_number_pool(size_type buffer_capacity, const allocator_type&
	alloc = allocator_type());
	
	// Size and capacity
	
	/// Get the number of elements currently stored in the client_number_pool.
	/**
	 * @return The number of elements stored in the client_number_pool.
	 *		This is the number of unread elements.
	 * @throws Nothing.
	 * @par Exception Safety
	 *		No-throw.
	 * @par Complexity
	 *		Constant (in the size of the client_number_pool).
	 */
	size_type size() const;
	
	/// Get the capacity of the client_number_pool.
	/**
	 * @return The maximum number of elements which can be stored in the
	 *		client_number_pool.
	 * @throws Nothing.
	 * @par Exception Safety
	 *		No-throw.
	 * @par Complexity
	 *		Constant (in the size of the client_number_pool).
	 */
	size_type capacity() const;
	
	/// Is the client_number_pool empty?
	/**
	 * @return true if there are no elements stored in the client_number_pool;
	 *		false otherwise.
	 * @throws Nothing.
	 * @par Exception Safety
	 *		No-throw.
	 * @par Complexity
	 *		Constant (in the size of the client_number_pool).
	 */
	bool empty() const;
	
	/// Is the client_number_pool full?
	/**
	 * @return true if the number of elements stored in the client_number_pool
	 *		equals the capacity of the client_number_pool; false otherwise.
	 * @throws Nothing.
	 * @par Exception Safety
	 *		No-throw.
	 * @par Complexity
	 *		Constant (in the size of the client_number_pool).
	 */
	bool full() const;
	
	/// Push item to the front of the queue. The thread blocks until the item
	/// has been successully pushed.
	/**
	 * @param item The item to be pushed to the front of the queue.
	 *		param_type represents the "best" way to pass a parameter of type
	 *		value_type to a method.
	 * @param client_interface_base A pointer to client_interface[0].
	 * @param spin_count The number of times to try locking (without blocking)
	 *		before waiting for the acquisition of the lock. It is affected by
	 *		the type of processor and the clock rate, etc.
	 * @param timeout_milliseconds The number of milliseconds to wait before a
	 *		timeout may occur.
	 * @return false if failing to push the item before the time period
	 *		specified by timeout_milliseconds has elapsed, true otherwise.
	 */
	bool push_front(param_type item, client_interface_type*
	client_interface_base, uint32_t spin_count = 1000000, uint32_t
	timeout_milliseconds = 10000);
	
	/// Pop item from the back of the queue. The thread blocks until the item
	/// has been successully popped.
	/**
	 * @param item The item to be popped from the back of the queue.
	 * @param client_interface_base A pointer to client_interface[0].
	 * @param spin_count The number of times to try locking (without blocking)
	 *		before waiting for the acquisition of the lock. It is affected by
	 *		the type of processor and the clock rate, etc.
	 * @param timeout_milliseconds The number of milliseconds to wait before a
	 *		timeout may occur.
	 * @return false if failing to pop the item before the time period specified
	 *		by timeout_milliseconds has elapsed, true otherwise.
	 */
	bool pop_back(value_type* item, client_interface_type*
	client_interface_base, owner_id oid, uint32_t spin_count = 1000000, uint32_t
	timeout_milliseconds = 10000);
	
	/// Try to push the item to the front of the queue. Returns without
	/// blocking.
	/**
	 * @param item The item to be pushed to the front of the queue.
	 *		param_type represents the "best" way to pass a parameter of type
	 *		value_type to a method.
	 * @param oid The owner_id of the calling process is stored internally so
	 *		that if the calling process crashes while doing operations, the
	 *		resource (client_number) can be recovered later on.
	 * @return false if failing when trying to push the item, true otherwise.
	 */
	bool try_push_front(param_type item, client_interface_type*
	client_interface_base);
	
	/// Try to pop the item from the back of the queue. Returns without
	/// blocking.
	/**
	 * @param item The item to be popped from the back of the queue.
	 * @param oid The owner_id of the calling process is stored so that if the
	 *		calling process crashes while doing operations, the client_number
	 *		can be recovered later on.
	 * @param client_interface_base A pointer to the first client_interface.
	 * @return false if failing to pop the item, true otherwise.
	 */
	bool try_pop_back(value_type* item, client_interface_type*
	client_interface_base, owner_id oid);
	
	/// This function is used when a client process have crashed and resources
	/// need to be restored. All client_number(s) that have the owner_id will be
	/// released.
	/**
	 * @param oid The owner_id of the process that may have allocated is stored
	 *		internally so that if the calling process crashes while doing
	 *		operations, the client_number(s) can be recovered later on.
	 * @param timeout_milliseconds The number of milliseconds to wait before a
	 *		timeout may occur.
	 * @return number of client_numbers released, or -1 if the operation failed
	 *		due to failing to lock the queue. In this case it is unknown if
	 *		there are any client_numbers to be released.
	 */
	std::size_t release_all_client_numbers_with_owner_id(owner_id oid, uint32_t
	timeout_milliseconds);
	
private:
	client_number_pool(const client_number_pool&);
	client_number_pool& operator=(const client_number_pool&);
	
	bool is_not_empty() const;
	bool is_not_full() const;
	
	size_type unread_;
	container_type container_;
	
	// Process-shared anonymous synchronization:
	
	// Mutex to protect access to the queue
	boost::interprocess::interprocess_mutex mutex_;
	
	// Condition to wait when the queue is not empty
	boost::interprocess::interprocess_condition not_empty_;
	
	// Condition to wait when the queue is not full
	boost::interprocess::interprocess_condition not_full_;
};

} // namespace core
} // namespace starcounter

#include "impl/client_number_pool.hpp"

#endif // STARCOUNTER_CORE_CLIENT_NUMBER_POOL_HPP
