//
// common_scheduler_interface.hpp
//
// Copyright © 2006-2012 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#ifndef STARCOUNTER_CORE_COMMON_SCHEDULER_INTERFACE_HPP
#define STARCOUNTER_CORE_COMMON_SCHEDULER_INTERFACE_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#include <cstddef>
#include <boost/cstdint.hpp>
#include <memory>
#include <boost/interprocess/managed_shared_memory.hpp>
#include <boost/interprocess/allocators/allocator.hpp>
#include <boost/call_traits.hpp>
#include <boost/bind.hpp>

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <intrin.h>
#undef WIN32_LEAN_AND_MEAN
#include "../common/scheduler_mask.hpp"
#include "../common/bit_operations.hpp"

namespace starcounter {
namespace core {

/// class common_scheduler_interface.
/**
 * @param T Is not used.
 * @param Alloc The allocator type used for all internal memory management.
 * @par Type Requirements Alloc
 *		The Alloc has to meet the allocator requirements imposed by STL.
 * @par Default Alloc
 *		std::allocator<T>
 */
template<class T, class Alloc = std::allocator<T> >
class common_scheduler_interface {
public:
	// Basic types
	
	typedef typename uint64_t value_type; // uint64_t is the mask type.
	typedef scheduler_mask<max_number_of_schedulers> scheduler_mask_type;
	
	// The type of an allocator used.
	typedef Alloc allocator_type;
	
	// Helper types
	
	// A type representing the "best" way to pass the value_type to a method.
	typedef typename boost::call_traits<value_type>::param_type param_type;
	
	// A type representing the "best" way to return the value_type from a const
	// method.
	typedef typename boost::call_traits<value_type>::param_type return_type;
	
	enum state {
		normal,
		at_least_one_client_is_down /// TODO: Think about it.
	};
	
	// Construction/Destruction.
	
	/**
	 * @param alloc The allocator.
	 * @throws "An allocation error" if memory is exhausted (std::bad_alloc if
	 *		the standard allocator is used).
	 * @par Complexity
	 *		Constant.
	 */
	explicit common_scheduler_interface(const allocator_type& alloc
	= allocator_type())
	: active_schedulers_mask_(), state_(normal) {}
	
	/// TODO: Think about multiple clients.
	void clients_state(state s) {
		_mm_mfence();
		state_ = s;
		_mm_mfence();
	}
	
	/// TODO: Think about multiple clients.
	state clients_state() const {
		return state_;
	}
	
	bool is_scheduler_active(std::size_t index) {
		return active_schedulers_mask_.is_scheduler_active(index);
	}
	
	void set_scheduler_number_flag(std::size_t ch) {
		active_schedulers_mask_.set_scheduler_number_flag(ch);
	}
	
	void clear_scheduler_number_flag(std::size_t ch) {
		active_schedulers_mask_.clear_scheduler_number_flag(ch);
	}
	
	uint64_t get_scheduler_mask(std::size_t ch) const {
		return active_schedulers_mask_.get_scheduler_mask(ch);
	}
	
	std::size_t number_of_active_schedulers() const {
		std::size_t count = 0;
		
		for (std::size_t i = 0; i < scheduler_mask_type::masks; ++i) {
			count += population_count(active_schedulers_mask_.get_mask(i));
		}
		
		return count;
	}
	
private:
	scheduler_mask_type active_schedulers_mask_;
	char cache_line_pad_0_[CACHE_LINE_SIZE
	-(sizeof(scheduler_mask_type) % CACHE_LINE_SIZE) // active_schedulers_mask_
	];
	
	volatile state state_;
	char cache_line_pad_1_[CACHE_LINE_SIZE
	-sizeof(state) // state_
	];
};

typedef starcounter::core::simple_shared_memory_allocator<std::size_t>
shm_alloc_for_the_common_scheduler_interface2;

typedef common_scheduler_interface
<std::size_t, shm_alloc_for_the_common_scheduler_interface2>
common_scheduler_interface_type;

} // namespace core
} // namespace starcounter

#endif // STARCOUNTER_CORE_COMMON_SCHEDULER_INTERFACE_HPP
