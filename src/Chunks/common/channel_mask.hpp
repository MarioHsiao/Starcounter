//
// channel_mask.hpp
//
// Copyright © 2006-2013 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#ifndef STARCOUNTER_CORE_CHANNEL_MASK_HPP
#define STARCOUNTER_CORE_CHANNEL_MASK_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#ifdef _MSC_VER
# pragma warning(push) // Save warning levels.
// Disable some silly and noisy warning from MSVC compiler
# pragma warning(disable: 4800) // TODO!
#endif // _MSC_VER

#include <iostream>
#include <cstddef>
#include <climits>
#include <cstdint>
#define WIN32_LEAN_AND_MEAN
# include <windows.h>
# include <intrin.h>
// Declaring interlocked functions for use as intrinsics.
# pragma intrinsic (_InterlockedAnd64)
# pragma intrinsic (_InterlockedOr64)
#undef WIN32_LEAN_AND_MEAN
#include "bit_operations.hpp"
#include "channel_number.hpp"
#include "macro_definitions.hpp"

namespace starcounter {
namespace core {

/// class channel_mask.
/**
 * @param Channels The number of channels.
 *		Use channels defined in config_param.hpp.
 */
template<std::size_t Channels>
class channel_mask {
public:
	typedef uint64_t mask_type;
	
	enum {
		mask_bits = sizeof(mask_type) * CHAR_BIT
	};
	
	// Construction/Destruction.
	channel_mask() {
		for (std::size_t i = 0; i < channel_masks(); ++i) {
			channel_mask_[i] = 0;
		}
	}
	
	mask_type get_channel_mask_word(std::size_t n) const {
		// No fence is needed when reading it.
		return channel_mask_[n];
	}
	
	bool get_channel_number_flag(channel_number ch) const {
		// Get channel number flag in channel_mask_.
		return bool(channel_mask_[ch >> 6] & (1ULL << (ch & 63)));
	}
	
	void set_channel_number_flag(channel_number ch) {
		// Set channel number flag in channel_mask_.
		std::size_t channel_mask_index = ch >> 6;
		mask_type mask = 1ULL << (ch & 0x3FULL);
		_InterlockedOr64((volatile __int64*)
		&channel_mask_[channel_mask_index], mask);
	}
	
	void clear_channel_number_flag(channel_number ch) {
		// Clear channel number flag in channel_mask_.
		std::size_t channel_mask_index = ch >> 6;
		uint64_t mask = ~(1ULL << (ch & 0x3FULL));
		_InterlockedAnd64((volatile __int64*)
		&channel_mask_[channel_mask_index], mask);
	}
	
	std::size_t channel_masks() const {
		return sizeof channel_mask_ / sizeof(mask_type);
	}
	
private:
	volatile mask_type channel_mask_[(Channels +mask_bits -1) / mask_bits];
	// It's a bit messy to compute "32" here so to save time I hard code it. If
	// the number of channels are changed it will not yield otimal performance.
	char cache_line_pad_0_[CACHE_LINE_SIZE -32]; // 32 * 8 = 256, one flag per channel.
};

} // namespace core
} // namespace starcounter

//#include "impl/channel_mask.hpp"

#ifdef _MSC_VER
# pragma warning(pop) // Restore warning levels.
#endif // _MSC_VER

#endif // STARCOUNTER_CORE_CHANNEL_MASK_HPP
