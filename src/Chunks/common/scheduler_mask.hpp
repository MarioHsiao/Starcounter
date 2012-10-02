//
// scheduler_mask.hpp
//
// Copyright © 2006-2011 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#ifndef STARCOUNTER_CORE_SCHEDULER_MASK_HPP
#define STARCOUNTER_CORE_SCHEDULER_MASK_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#include <iostream>
#include <cstddef>
#include <boost/cstdint.hpp>
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#undef WIN32_LEAN_AND_MEAN
#include "../common/config_param.hpp"
#include "../server/scheduler.hpp"
#include "../common/bit_operations.hpp"

namespace starcounter {
namespace core {

#if defined(_MSC_VER) // Windows
# if defined(_M_X64) || (_M_AMD64) // LLP64 machine 
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
	
	uint64_t get_mask(std::size_t i) const {
		return scheduler_mask_[i];
	}
	
	void set_mask(std::size_t i, uint64_t mask) {
		scheduler_mask_[i] = mask;
	}
	
	void set_scheduler_number_flag(std::size_t scheduler_number) {
		// Atomically set scheduler number flag in scheduler_mask_.
		std::size_t scheduler_mask_index = scheduler_number >> 6;
		uint64_t mask = 1ULL << (scheduler_number & 0x3FULL);
		_InterlockedOr64((volatile __int64*)
		(&scheduler_mask_[scheduler_mask_index]), mask);
	}
	
	void clear_scheduler_number_flag(std::size_t scheduler_number) {
		// Atomically clear scheduler number flag in scheduler_mask_.
		std::size_t scheduler_mask_index = scheduler_number >> 6;
		uint64_t mask = ~(1ULL << (scheduler_number & 0x3FULL));
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

# elif defined(_M_IX86) // ILP32 machine 
/// class scheduler_mask.
template<std::size_t Schedulers>
class scheduler_mask {
public:
	typedef uint32_t value_type;
	
	enum {
		masks = (Schedulers +31) / 32
	};
	
	// Construction/Destruction.
	scheduler_mask() {
		for (std::size_t i = 0; i < masks; ++i) {
			scheduler_mask_[i] = 0UL;
		}
	}
	
	uint32_t get_mask(std::size_t i) const {
		return scheduler_mask_[i];
	}
	
	void set_mask(std::size_t i, uint32_t mask) {
		scheduler_mask_[i] = mask;
	}
	
	void set_scheduler_number_flag(std::size_t scheduler_number) {
		// Atomically set scheduler number flag in scheduler_mask_.
		std::size_t scheduler_mask_index = scheduler_number >> 5;
		uint32_t mask = 1UL << (scheduler_number & 0x1FUL);
		_InterlockedOr(&scheduler_mask_[scheduler_mask_index], mask);
	}
	
	void clear_scheduler_number_flag(std::size_t scheduler_number) {
		// Atomically clear scheduler number flag in scheduler_mask_.
		std::size_t scheduler_mask_index = scheduler_number >> 5;
		uint32_t mask = ~(1UL << (scheduler_number & 0x1FUL));
		_InterlockedAnd(&scheduler_mask_[scheduler_mask_index], mask);
	}
	
	std::size_t scheduler_masks() const {
		return masks;
	}
	
	bool is_scheduler_active(std::size_t scheduler_number) {
		if (scheduler_number < max_number_of_schedulers) {
			// Test scheduler number flag in scheduler_mask_.
			std::size_t scheduler_mask_index = scheduler_number >> 5;
			return ((1UL << (scheduler_number & 0x1FUL))
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

# endif // (_M_X64) || (_M_AMD64)
#else 
# error Compiler not supported.
#endif // (_MSC_VER)

} // namespace core
} // namespace starcounter

//#include "impl/scheduler_mask.hpp"

#endif // STARCOUNTER_CORE_SCHEDULER_MASK_HPP
