//
// scheduler_channel.hpp
//
// Copyright © 2006-2013 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#ifndef STARCOUNTER_CORE_SCHEDULER_CHANNEL_HPP
#define STARCOUNTER_CORE_SCHEDULER_CHANNEL_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#include <cstddef>
#include <cstdint>
#include "../common/noncopyable.hpp"
#include "../common/chunk.hpp"
#include "../common/atomic_bounded_buffer.hpp"
#include "../common/config_param.hpp"

namespace starcounter {
namespace core {

template<class T, class Alloc = std::allocator<T> >
class scheduler_channel : private noncopyable {
public:
	// The type of queues used in the scheduler_channel.
	typedef bounded_buffer<T, Alloc> queue_type;
	
	// The type of elements stored in the scheduler_channel.
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

	// The type of allocator used in the scheduler_channel.
	typedef typename queue_type::allocator_type allocator_type;

	// Helper types

	// A type representing the "best" way to pass the value_type to a method.
	typedef typename boost::call_traits<value_type>::param_type param_type;
	
	// A type representing the "best" way to return the value_type from a const method.
	typedef typename boost::call_traits<value_type>::param_type return_type;

	// Construction/Destruction.

	explicit scheduler_channel(size_type capacity, const allocator_type& alloc
	= allocator_type())
	: in() {}

public:
	atomic_bounded_buffer<T, 8> in; // 1 << 8 (256) elements.
};

#if 0
// Define an STL compatible allocator of chunk_indexes that allocates from the
// managed_shared_memory. This allocator will allow placing containers in the segment.
typedef boost::interprocess::allocator<chunk_index, boost::interprocess
::managed_shared_memory::segment_manager> shm_alloc_for_the_scheduler_channels;

// Alias a scheduler_channel that uses the previous STL-like allocator so that it
// allocates its values from the segment.
typedef scheduler_channel<chunk_index, shm_alloc_for_the_scheduler_channels>
scheduler_channel_type;
#endif

typedef simple_shared_memory_allocator<chunk_index> shm_alloc_for_the_channels2;
typedef scheduler_channel<chunk_index, shm_alloc_for_the_channels2> scheduler_channel_type;

} // namespace core
} // namespace starcounter

#endif // STARCOUNTER_CORE_SCHEDULER_CHANNEL_HPP
