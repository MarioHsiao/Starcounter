//
// channel_interface.hpp
//
// Copyright © 2006-2013 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#ifndef STARCOUNTER_CORE_CHANNEL_INTERFACE_HPP
#define STARCOUNTER_CORE_CHANNEL_INTERFACE_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#include <cstddef>
#include <cstdint>
#include "../common/channel_number.hpp"
#include "../common/macro_definitions.hpp"

namespace starcounter {
namespace core {

/// class channel_interface.
/**
 * @param T The type of the elements stored in the bounded_buffer.
 * @par Type Requirements T
 *		The T has to be SGIAssignable (SGI STL defined combination of Assignable
 *		and CopyConstructible), and EqualityComparable and/or LessThanComparable
 *		if the bounded_buffer will be compared with another container.
 * @param Alloc The allocator type used for all internal memory management.
 * @par Type Requirements Alloc
 *		The Alloc has to meet the allocator requirements imposed by STL.
 * @par Default Alloc
 *		std::allocator<T>
 */
template<class T, class Alloc = std::allocator<T> >
class channel_interface {
public:
	// Basic types
	typedef uint32_t channel_size_type;
	typedef typename T value_type;
	//typedef typename queue_type::value_type value_type;
	
	// The type of an allocator used.
	typedef Alloc allocator_type;
	
	// Construction/Destruction.
	
	/// Constructor.
	/**
	 * @param alloc The allocator.
	 */
	explicit channel_interface(const allocator_type& alloc = allocator_type(),
	channel_size_type channel_size = 0)
	: channel_size_(channel_size) {}
	
	/// Get the number of channels.
	/**
	 * @return The number of channels.
	 */
	channel_size_type channel_size() const {
		return channel_size_;
	}
	
private:
	channel_size_type channel_size_;
};

typedef simple_shared_memory_allocator<channel_number>
shm_alloc_for_the_channel_interface;

typedef channel_interface<channel_number, shm_alloc_for_the_channel_interface>
channel_interface_type;

} // namespace core
} // namespace starcounter

//#include "impl/channel_interface.hpp"

#endif // STARCOUNTER_CORE_CHANNEL_INTERFACE_HPP
