//
// scheduler.hpp
//
// Copyright © 2006-2011 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#ifndef STARCOUNTER_CORE_SCHEDULER_HPP
#define STARCOUNTER_CORE_SCHEDULER_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#if 0
#include <cstddef>
#include <climits>
#include <boost/cstdint.hpp>
#include <boost/thread/thread.hpp>
#include <boost/shared_ptr.hpp>
#include <boost/call_traits.hpp>
#include <boost/bind.hpp>
#include <boost/utility.hpp>
#include <boost/noncopyable.hpp>
#define WIN32_LEAN_AND_MEAN
#include <intrin.h>
#undef WIN32_LEAN_AND_MEAN
#include "../common/bounded_buffer.hpp"
#include "../common/config_param.hpp"
#endif

// TODO: Handle interrupt that can be thrown in a thread. If not caught - abort().


namespace starcounter {
namespace core {

// The scheduler is implemented as a function here,
// since Blue uses a function instead of a functor.
void scheduler(std::size_t id);

#if 0 // A scheduler can be implemented as a functor instead of a function.
// A server (process) starts one or several schedulers.

// A scheduler is an object of a callable type.

// Objects of type boost::thread are not copyable.
template<typename Message>
class scheduler : private boost::noncopyable {
public:
	typedef bounded_buffer<Message> queue_type;

	enum state {
		stopped,
		stopping,
		starting,
		running
	};

	// channel_masks is malplaced and hard coded for 256-channels,
	// assuming we use 64-bit masks. It should be a variable that
	// is computed by the constructor.
	enum { channel_masks = 4 };

	scheduler()
	: state_(stopped), working_(true) {}

	~scheduler() {
		// must join the thread before destroying the scheduler.
	}

	// The server spawns a scheduler thread that calls this function,
	// after the constructor has been called?
	void operator()() {
		work();
	}

public:
	// Each scheduler has an in queue where any scheduler may push messages.
	// Currently it is not allocated in shared memory so only schedulers residing
	// in the same process can access it. This model need to be changed when we
	// want to support NUMA systems. Probably we want to put this queue in the
	// scheduler_interface.
	queue_type in;

private:
	volatile state state_;
	volatile bool working_;
	std::size_t channel_masks_;

	void work() {
		// This is placed in the scheduler interface:  uint64_t channel_mask[channel_masks_];
		channel_number ch;
		chunk_index the_chunk_index;
		
		while (state_ == running) {
			while (working_) {
				// Scan through all channels once and process messages if there is any.
				for (std::size_t n = 0; n < channel_masks_; ++n) {
					for (uint64_t mask = channel_mask[n]; mask; mask &= mask -1) {
						//ch = bit_scan_forward(mask); // TODO: Fix bit_operations.hpp
						(void) _BitScanForward64(&ch, mask); // Only for Windows now.
						ch += n << 6;
						
						// Check if there is a message in channel[ch].in and process it.
					
					}
				}
				// Checking my in queue. It costs about 140 ns, so do this only after some
				// time has passed or some number of channels have been checked.
				if (true) { // TODO: Fix the logic for deciding when to check the in queue.
					if (in.try_pop_back(&the_chunk_index)) {
						// Got a message on the in queue. Process it.
					}
				}
			}


			// No messages...going to sleep.
		}

		// The scheduler thread exit here. The server should join the thread.
	}

	// mask for my channels
	// I can handle streams on any of the available channels
	// and any number of channels.
	// I will just have a mask with one bit per channel that
	// marks which channels are mine (exclusively, absolutely
	// not shared with any other scheduler ever).
	// Channels I get is marked with 1 and channels I don't have,
	// or channels I stop working on is marked with 0.

	// Then I need a copy of the range of up to 64 channels I watch.
};

#endif // A scheduler can be implemented as a functor instead of a function.

} // namespace core
} // namespace starcounter

//#include "impl/scheduler.hpp"

#endif // STARCOUNTER_CORE_SCHEDULER_HPP
