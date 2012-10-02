//
// resource_map.hpp
//
// Copyright © 2006-2012 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#ifndef STARCOUNTER_CORE_RESOURCE_MAP_HPP
#define STARCOUNTER_CORE_RESOURCE_MAP_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#include <cstddef>
#include <climits>
#if defined(_MSC_VER)
# define WIN32_LEAN_AND_MEAN
# include <intrin.h>
# undef WIN32_LEAN_AND_MEAN
#endif // (_MSC_VER)
#include <mmintrin.h>
#include <boost/cstdint.hpp>
#include "../common/chunk.hpp"
#include "../common/owner_id.hpp"
#include "../common/bit_operations.hpp"
#include "../common/config_param.hpp"

namespace starcounter {
namespace core {

/// client_interface[s] have one resource_map each.

class resource_map {
public:
	typedef uint32_t volatile mask_type;
	
	enum {
		shift_bits_in_mask_type = 5,
		bits_in_mask_type = sizeof(mask_type) * CHAR_BIT,
		chunks_mask_size = chunks_total_number_max / bits_in_mask_type,
		channels_mask_size = channels / bits_in_mask_type
	};
	
	// Constructor.
	resource_map() {
		for (std::size_t i = 0; i < channels_mask_size; ++i) {
			owned_channels_[i] = 0UL;
		}
		
		for (std::size_t i = 0; i < chunks_mask_size; ++i) {
			owned_chunks_[i] = 0UL;
		}
	}
	
	void set_chunk_flag(std::size_t index) {
		_interlockedbittestandset((volatile long int*)
		&owned_chunks_[index >> 5], index & 31);
	}
	
	void clear_chunk_flag(std::size_t index) {
		_interlockedbittestandreset((volatile long int*)
		&owned_chunks_[index >> 5], index & 31);
	}
	
	std::size_t count_chunk_flags_set() {
		std::size_t count = 0;
		
		for (std::size_t i = 0; i < chunks_mask_size; ++i) {
			count += population_count(owned_chunks_[i]);
		}
		
		return count;
	}
	
	void set_channel_flag(std::size_t scheduler_num, std::size_t channel_num) {
		scheduler_number_for_owned_channel_[channel_num] = scheduler_num;
		
		// Force the order. This is important because the stored scheduler
		// number must be valid in case of a crash, so that the thread doing the
		// clean up releases the channel_number to the right scheduler_interface
		// channel_number queue.
		_mm_sfence();
		
		_interlockedbittestandset((volatile long int*)
		&owned_channels_[channel_num >> 5], channel_num & 31);
	}
	
	void clear_channel_flag(std::size_t scheduler_num, std::size_t channel_num)
	{
		std::size_t index = scheduler_num << channel_bits | channel_num;
		_interlockedbittestandreset((volatile long int*)
		&owned_channels_[index >> 5], index & 31);
	}
	
	/// Get the scheduler_number for the corresponding owned channel_number.
	/**
	 * @return The scheduler_number of the channel_number. It is only valid if
	 *		the channel is marked as owned. The caller must be aware of this.
	 */
	uint32_t get_scheduler_number_for_owned_channel_number(
	channel_number channel_num) {
		return scheduler_number_for_owned_channel_[channel_num];
	}
	
	resource_map& clear() {
		for (std::size_t i = 0; i < channels_mask_size; ++i) {
			owned_channels_[i] = 0UL;
		}
		
		for (std::size_t i = 0; i < chunks_mask_size; ++i) {
			owned_chunks_[i] = 0UL;
		}
		
		return *this;
	}
	
	//--------------------------------------------------------------------------
	/// Get a reference to the owned_chunks_[mask_word_index].
	/**
	 * @return A reference to the owned_chunks_[mask_word_index].
	 */
	mask_type& get_owned_chunks_mask(std::size_t mask_word_index) {
		return owned_chunks_[mask_word_index];
	}
	
	/// Get a const reference to the owned_chunks_[mask_word_index].
	/**
	 * @return A const reference to the owned_chunks_[mask_word_index].
	 */
	const mask_type& get_owned_chunks_mask(std::size_t mask_word_index) const
	{
		return owned_chunks_[mask_word_index];
	}
	
	//--------------------------------------------------------------------------
	/// Get a reference to the owned_channels_[mask_word_index].
	/**
	 * @return A reference to the owned_channels_[mask_word_index].
	 */
	mask_type& get_owned_channels_mask(std::size_t mask_word_index) {
		return owned_channels_[mask_word_index];
	}
	
	/// Get a const reference to the owned_channels_[mask_word_index].
	/**
	 * @return A const reference to the owned_channels_[mask_word_index].
	 */
	const mask_type& get_owned_channels_mask(std::size_t mask_word_index) const
	{
		return owned_channels_[mask_word_index];
	}
	
	//--------------------------------------------------------------------------
	// arithmetic assignment operators
	resource_map& operator^=(const resource_map& rhs);
	resource_map& operator&=(const resource_map& rhs);
	resource_map& operator|=(const resource_map& rhs);
	
private:
	// The mask has one flag for each chunk. If the owner of this resource_map
	// (marked with the owner_id_) owns a chunk, the corresponding bit-index in
	// the owned_chunks_ mask is set, otherwise it is cleared.
	mask_type owned_chunks_[chunks_mask_size];
	
	// The mask has one flag for each channel. If the owner of this resource_map
	// (marked with the owner_id_) owns a channel, the corresponding bit-index
	// in the owned_channels_ mask is set, otherwise it is cleared.
	mask_type owned_channels_[channels_mask_size];
	
	// If the a flag in the owned_channels_ mask is set, then the corresponding
	// scheduler_number_for_owned_channel_[channel] gives which scheduler_number
	// the channel_channel_number was aquired from. No initialization is needed.
	uint32_t scheduler_number_for_owned_channel_[channels];
};

// member functions:

// arithmetic assignment operators
inline resource_map& resource_map::operator^=(const resource_map& rhs) {
	for (std::size_t i = 0; i < chunks_mask_size; ++i) {
		owned_chunks_[i] ^= rhs.owned_chunks_[i];
	}
	
	for (std::size_t i = 0; i < channels_mask_size; ++i) {
		owned_channels_[i] ^= rhs.owned_channels_[i];
	}
	
	return *this;
}

inline resource_map& resource_map::operator&=(const resource_map& rhs) {
	for (std::size_t i = 0; i < chunks_mask_size; ++i) {
		owned_chunks_[i] &= rhs.owned_chunks_[i];
	}
	
	for (std::size_t i = 0; i < channels_mask_size; ++i) {
		owned_channels_[i] &= rhs.owned_channels_[i];
	}
	
	return *this;
}

inline resource_map& resource_map::operator|=(const resource_map& rhs) {
	for (std::size_t i = 0; i < chunks_mask_size; ++i) {
		owned_chunks_[i] |= rhs.owned_chunks_[i];
	}
	
	for (std::size_t i = 0; i < channels_mask_size; ++i) {
		owned_channels_[i] |= rhs.owned_channels_[i];
	}
	
	return *this;
}

// nonmember functions:

// arithmetic binary operators

// operator^ implemented in terms of operator^=
inline const resource_map operator^(const resource_map& lhs,
const resource_map& rhs) {
	return resource_map(lhs) ^= rhs;
}

// operator& implemented in terms of operator&=
inline const resource_map operator&(const resource_map& lhs,
const resource_map& rhs) {
	return resource_map(lhs) &= rhs;
}

// operator| implemented in terms of operator|=
inline const resource_map operator|(const resource_map& lhs,
const resource_map& rhs) {
	return resource_map(lhs) |= rhs;
}

} // namespace core
} // namespace starcounter

#endif // STARCOUNTER_CORE_RESOURCE_MAP_HPP
