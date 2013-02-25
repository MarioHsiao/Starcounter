//
// common_client_interface.hpp
//
// Copyright � 2006-2012 Starcounter AB. All rights reserved.
// Starcounter� is a registered trademark of Starcounter AB.
//

#ifndef STARCOUNTER_CORE_COMMON_CLIENT_INTERFACE_HPP
#define STARCOUNTER_CORE_COMMON_CLIENT_INTERFACE_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#include <cstddef>
#include <boost/cstdint.hpp>
#include <iostream> // debug
#include <memory>
#include <utility>
#define WIN32_LEAN_AND_MEAN
# include <windows.h>
# include <intrin.h>
// Declaring interlocked functions for use as intrinsics.
# pragma intrinsic (_InterlockedIncrement)
# pragma intrinsic (_InterlockedDecrement)
#undef WIN32_LEAN_AND_MEAN
#include <boost/interprocess/managed_shared_memory.hpp>
#include <boost/interprocess/allocators/allocator.hpp>
#include <boost/call_traits.hpp>
#include <boost/bind.hpp>
#include "../common/client_number.hpp"
#include "../common/client_number_pool.hpp"
#include "config_param.hpp"
#include "macro_definitions.hpp"

namespace starcounter {
namespace core {

/// class common_client_interface.
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
class common_client_interface {
public:
	// Basic types
	
#if defined (IPC_CLIENT_NUMBER_POOL_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
	// The type of queue for client_number.
	typedef client_number_pool<T, client_interface_bits> queue_type;

	// The type of elements stored in the client_number_pool.
	typedef typename queue_type::value_type value_type;
	
	// The size type. (An unsigned integral type that can represent any non-
	// negative value of the container's distance type.)
	typedef typename queue_type::size_type size_type;

#else // !defined (IPC_CLIENT_NUMBER_POOL_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
	// The type of queue for client_number.
	typedef client_number_pool<T, Alloc> queue_type;
	
	// The type of elements stored in the client_number_pool.
	typedef typename queue_type::value_type value_type;
	
	// A pointer to an element.
	typedef typename queue_type::pointer pointer;
	
	// A const pointer to the element.
	typedef typename queue_type::const_pointer const_pointer;
	
	// A reference to an element.
	typedef typename queue_type::reference reference;
	
	// A const reference to an element.
	typedef typename queue_type::const_reference const_reference;
	
	// The distance type. (A signed integral type used to represent the distance
	// between two iterators.)
	typedef typename queue_type::difference_type difference_type;
	
	// The size type. (An unsigned integral type that can represent any non-
	// negative value of the container's distance type.)
	typedef typename queue_type::size_type size_type;
	
	// The type of an allocator used in the client_number_pool.
	//typedef Alloc allocator_type;
	typedef typename queue_type::allocator_type allocator_type;
#endif // defined (IPC_CLIENT_NUMBER_POOL_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
	
	// Helper types
	
	// A type representing the "best" way to pass the value_type to a method.
	typedef typename boost::call_traits<value_type>::param_type param_type;
	
	// A type representing the "best" way to return the value_type from a const method.
	typedef typename boost::call_traits<value_type>::param_type return_type;
	
	enum state {
		normal,
		database_terminated_gracefully,
		database_terminated_unexpectedly
	};
	
	// Construction/Destruction.
	
	/// Create an empty client_number_pool with the specified capacity.
	/**
	 * @param buffer_capacity The maximum number of elements which can be stored
	 *		in the client_number_pool.
	 * @param alloc The allocator.
	 * @throws "An allocation error" if memory is exhausted (std::bad_alloc if
	 *		the standard allocator is used.)
	 * @par Complexity
	 *		Constant.
	 */
#if defined (IPC_CLIENT_NUMBER_POOL_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
	explicit common_client_interface(const char* segment_name)
	: client_number_pool_(segment_name),
	state_(normal),
	client_interfaces_to_clean_up_(0) {}
#else // !defined (IPC_CLIENT_NUMBER_POOL_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
	explicit common_client_interface(size_type buffer_capacity,
	const allocator_type& alloc = allocator_type())
	: client_number_pool_(buffer_capacity, alloc),
	state_(normal),
	client_interfaces_to_clean_up_(0) {}
#endif // defined (IPC_CLIENT_NUMBER_POOL_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
	
	queue_type& client_number_pool() {
		return client_number_pool_;
	}
	
	/// The monitor sets the state to database_terminated_unexpectedly if it
	/// detects that the database process exit without having unregistered. This
	/// indicates a database crash.
	/**
	 * @param s The state of the database: normal,
	 *		database_terminated_gracefully or database_terminated_unexpectedly.
	 * @throws Nothing.
	 * @par Exception Safety
	 *		No-throw.
	 */
	void database_state(state s) {
		_mm_sfence();
		state_ = s;
		_mm_sfence();
	}
	
	/// Clients check the database state. See client_interface.
	/**
	 * @return The state of the database: normal or database_terminated_unexpectedly.
	 * @throws Nothing.
	 * @par Exception Safety
	 *		No-throw.
	 */
	state database_state() const {
		return state_;
	}
	
	/// Get number of client interfaces to clean up.
	/**
	 * @return The number of client interfaces to clean up.
	 */
	uint32_t client_interfaces_to_clean_up() const {
		return client_interfaces_to_clean_up_;
	}
	
	/// The monitor increments this counter for each client_interface it finds
	/// to belong to a terminated client process.
	/**
	 * @return The number of client interfaces to clean up.
	 */
	uint32_t increment_client_interfaces_to_clean_up() {
		return _InterlockedIncrement(&client_interfaces_to_clean_up_);
	}
	
	/// Schedulers decrements this counter for each client_interface it releases
	/// which indicates complete recovery of all resources via that
	/// client_interface: chunks and channels.
	/**
	 * @return The number of client interfaces to clean up.
	 */
	uint32_t decrement_client_interfaces_to_clean_up() {
		return _InterlockedDecrement(&client_interfaces_to_clean_up_);
	}
	
#if defined (IPC_CLIENT_NUMBER_POOL_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
	/// Clients acquire a client_number, which allocates
	/// client_interface[client_number].
	bool acquire_client_number(value_type* n, client_interface_type*
	base_client_interface, owner_id oid, uint32_t spin_count = 1000000,
	uint32_t timeout_milliseconds = 10000) {
		return client_number_pool_.acquire(n, base_client_interface, oid,
		smp::spinlock::milliseconds(timeout_milliseconds));
	}
	
	/// Clients release its client_number, which releases
	/// client_interface[client_number].
	bool release_client_number(value_type n, client_interface_type*
	base_client_interface, owner_id oid, uint32_t spin_count = 1000000,
	uint32_t timeout_milliseconds = 10000) {
		return client_number_pool_.release(n, base_client_interface, oid,
		smp::spinlock::milliseconds(timeout_milliseconds));
	}

	bool insert_client_number(value_type n, client_interface_type*
	base_client_interface, owner_id oid, uint32_t spin_count = 1000000,
	uint32_t timeout_milliseconds = 10000) {
		return client_number_pool_.insert(n, base_client_interface, oid,
		smp::spinlock::milliseconds(timeout_milliseconds));
	}

#else // !defined (IPC_CLIENT_NUMBER_POOL_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
	/// Clients acquire a client_number, which allocates
	/// client_interface[client_number].
	bool acquire_client_number(value_type* n, client_interface_type*
	base_client_interface, owner_id oid, uint32_t spin_count = 1000000,
	uint32_t timeout_milliseconds = 10000) {
		return client_number_pool_.pop_back(n, base_client_interface, oid,
		spin_count, timeout_milliseconds);
	}
	
	/// Clients release its client_number, which releases
	/// client_interface[client_number].
	bool release_client_number(value_type n, client_interface_type*
	base_client_interface, uint32_t spin_count = 1000000,
	uint32_t timeout_milliseconds = 10000) {
		return client_number_pool_.push_front(n, base_client_interface,
		spin_count, timeout_milliseconds);
	}
#endif // defined (IPC_CLIENT_NUMBER_POOL_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
	
private:
	// A pool with client numbers, which is used to acquire the
	// client_interface[client_number].
	queue_type client_number_pool_;
	char cache_line_pad_0_[CACHE_LINE_SIZE
	-(sizeof(queue_type) % CACHE_LINE_SIZE) // client_number_pool_
	];
	
	// Database state.
	volatile state state_;
	
	// Number of client interfaces to clean up.
	volatile uint32_t client_interfaces_to_clean_up_;
	
	char cache_line_pad_1_[CACHE_LINE_SIZE
	-sizeof(state) // state_
	-sizeof(uint32_t) // client_interfaces_to_clean_up_
	];
};

typedef simple_shared_memory_allocator<client_number>
shm_alloc_for_the_common_client_interface2;

typedef common_client_interface
<client_number, shm_alloc_for_the_common_client_interface2>
common_client_interface_type;

} // namespace core
} // namespace starcounter

#endif // STARCOUNTER_CORE_COMMON_CLIENT_INTERFACE_HPP
