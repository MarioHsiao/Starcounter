//
// scheduler_mask.hpp
//
// Copyright © 2006-2013 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#ifndef STARCOUNTER_CORE_SCHEDULER_MASK_HPP
#define STARCOUNTER_CORE_SCHEDULER_MASK_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#include <cstdint>
#include <cstddef>
#include <iostream>
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#undef WIN32_LEAN_AND_MEAN
#include "../common/config_param.hpp"
#include "../server/scheduler.hpp"
#include "../common/bit_operations.hpp"

namespace starcounter {
namespace core {

/// class scheduler_mask.
template<std::size_t Schedulers>
class scheduler_mask {
public:
	typedef uint64_t value_type;
	
	enum {
		masks = (Schedulers +63) / 64
	};
	
	// Construction/Destruction.
	scheduler_mask() {
		for (std::size_t i = 0; i < masks; ++i) {
			scheduler_mask_[i] = 0ULL;
		}
	}
	
	value_type get_mask(std::size_t i) const {
		return scheduler_mask_[i];
	}
	
	void set_mask(std::size_t i, value_type mask) {
		scheduler_mask_[i] = mask;
	}
	
	void set_scheduler_number_flag(std::size_t scheduler_number) {
		// Atomically set scheduler number flag in scheduler_mask_.
		std::size_t scheduler_mask_index = scheduler_number >> 6;
		value_type mask = 1ULL << (scheduler_number & 0x3FULL);
		_InterlockedOr64((volatile __int64*)
		(&scheduler_mask_[scheduler_mask_index]), mask);
	}
	
	void clear_scheduler_number_flag(std::size_t scheduler_number) {
		// Atomically clear scheduler number flag in scheduler_mask_.
		std::size_t scheduler_mask_index = scheduler_number >> 6;
		value_type mask = ~(1ULL << (scheduler_number & 0x3FULL));
		_InterlockedAnd64((volatile __int64*)
		&scheduler_mask_[scheduler_mask_index], mask);
	}
	
	std::size_t scheduler_masks() const {
		return masks;
	}
	
	bool is_scheduler_active(std::size_t scheduler_number) {
		if (scheduler_number < max_number_of_schedulers) {
			// Test scheduler number flag in scheduler_mask_.
			std::size_t scheduler_mask_index = scheduler_number >> 6;
			return ((1ULL << (scheduler_number & 0x3FULL))
			& scheduler_mask_[scheduler_mask_index]) != 0;
		}
		return false;
	}
	
private:
	volatile value_type scheduler_mask_[masks];
	char pad0[CACHE_LINE_SIZE -(masks * sizeof(value_type))];
	volatile uint32_t number_of_schedulers_;
	char pad1[CACHE_LINE_SIZE -sizeof(uint32_t)];
};

} // namespace core
} // namespace starcounter

//#include "impl/scheduler_mask.hpp"

#endif // STARCOUNTER_CORE_SCHEDULER_MASK_HPP
