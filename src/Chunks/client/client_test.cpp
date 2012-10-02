//
// client_test.cpp
//
// Copyright © 2006-2011 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

// Use -DBOOST_CB_DISABLE_DEBUG or -DNDEBUG flags
#define BOOST_CB_DISABLE_DEBUG

#include <cstdlib>
#include <iostream>
#include <vector>
#include <string>
#include <utility>
#include <boost/cstdint.hpp>
#include <boost/interprocess/managed_shared_memory.hpp>
#include <boost/interprocess/allocators/allocator.hpp>
#include <boost/interprocess/exceptions.hpp>
#include <boost/timer.hpp>
#include <boost/lexical_cast.hpp>
#include "../common/circular_buffer.hpp"
#include "../common/bounded_buffer.hpp"
#include "../common/chunk.hpp"
//#include "../common/shared_chunk_pool.hpp"
//#include "../common/channel.hpp"
#include "../common/shared_interface.hpp"
#include "../common/scheduler_number.hpp"
#include "../common/client_number.hpp"
#include "../common/channel_number.hpp"
#include "../common/pid_type.hpp"
#include "../common/owner_id.hpp"
#include "../common/shared_memory_manager.hpp"
#include "../common/config_param.hpp"
#include "../common/macro_definitions.hpp"
#if defined(_MSC_VER)
# define WIN32_LEAN_AND_MEAN
# include <windows.h>
# undef WIN32_LEAN_AND_MEAN
#endif // defined(_MSC_VER)

// Don't know if this is needed but it is just for a test.
#define _E_WAIT_TIMEOUT 0x0001000CL

// Client.
int main(int argc, char* argv[]) try {
	using namespace starcounter::core;
	
	//--------------------------------------------------------------------------
	// The client must register itself to the monitor and obtain an owner_id.
	// GetProcessId(), register it, receive owner_id.
	
	//--------------------------------------------------------------------------
	//SetProcessAffinityMask(GetCurrentProcess(), 2);
	//Sleep(1);
	
	//-------------------------------------------------------------------------->>>
	// Wait until shared memory is initialized.
	while (true) {
		try {
			// Open the shared memory object with the "shared memory initialized flag."
			boost::interprocess::shared_memory_object shm_obj
			(boost::interprocess::open_only,
			"starcounter_shared_memory_initialized_indicator",
			boost::interprocess::read_write);

			// Map the whole shared memory in this process.
			boost::interprocess::mapped_region region(shm_obj,
			boost::interprocess::read_write);

			// Obtain a pointer to the shared structure.
			shared_memory_manager* shm_mgr
			= static_cast<shared_memory_manager*>(region.get_address());

			// Wait for shared memory to be initialized.
			std::cout << "Waiting for shared memory to be initialized..."
			<< std::endl;
			shm_mgr->wait_until_shared_memory_is_initialized();
			std::cout << "Shared memory is initialized." << std::endl;
			break;
		}
		catch (boost::interprocess::interprocess_exception& e) {
			std::cerr << e.what() << std::endl;
			 // Wait a moment.
			Sleep(100);
		}
		catch (...) {
			std::cerr << "Unknown exception caught" << std::endl;
			throw;
		}
	};

	// Shared memory is initialized so now we continue.

	//--------------------------------------------------------------------------
	// Create a shared_interface to access all objects in shared memory.
	const char* segment_name
	= "test_starcounter_core_shared_memory_segment";
	//= "DBSERVER1_starcounter_core_shared_memory_segment";
	shared_interface shared(segment_name);
	///shared.show(); /// debug

	//--------------------------------------------------------------------------
	// Register the client process id with the monitor process and receive
	// an owner_id from the monitor.
	// class client_manager /client_app if it is useful. Watch out for
	// loosing a register or indirection level increases...slower access!?
	//owner_id my_owner_id =

	// The client must register itself to the monitor and obtain an owner_id.
	// GetProcessId(), register it, receive owner_id.
	//HANDLE WINAPI GetCurrentProcess(void);
	//DWORD pid = WINAPI GetProcessId(__in  HANDLE Process);
	DWORD pid = GetProcessId(GetCurrentProcess());
	std::cout << "my pid = " << pid << std::endl;
	//-------------------------------------------------------------------------->>>
	// Acquire resources: owner_id, client_number, channels and chunks, etc.

	// Acquire a client_number.
	client_number my_client_number;

	if (!shared.acquire_client_number(&my_client_number)) {
		std::cout << "Did not acquire a client_number. "
		"Handle the timeout, try another queue, etc." << std::endl;
	}
	
	std::cout << "my client number is: " << my_client_number
	<< std::endl; /// debug

	//--------------------------------------------------------------------------
	// Get reference to this client's client interface. Schedulers reach it
	// via the channel but the client can reach it faster by having is's own
	// reference to it.
	client_interface_type& this_client_interface
	= shared.client_interface(my_client_number);
	///std::cout << "this_client_interface = "
	///<< (void*) &this_client_interface << std::endl;

	//--------------------------------------------------------------------------
	// Set scheduler_number - think about affinity...
	
	/// TODO: Set scheduler_number with correct affiniy.
	//DWORD_PTR mask = 1 << (the_scheduler_number * 2 +1);
	//SetThreadAffinityMask(GetCurrentThread(), mask);
	
	//the_scheduler_number = GetCurrentProcessorNumber() / 2;
	
	/// This is a test:
	scheduler_number the_scheduler_number = 0;
	if (argc > 2) {
		// Override default test value with the argument.
		the_scheduler_number = std::atoi(argv[2]); // Second argument.

		if (the_scheduler_number > SCHEDULERS -1) {
			the_scheduler_number = SCHEDULERS -1;
			std::cout << "Scheduler number is out of range. "
			"Setting scheduler number to " << the_scheduler_number << std::endl;
		}
	}

	//--------------------------------------------------------------------------
	// Acquire 1..N channel_number(s).
	
	// The channel_numbers vector holds this client's channel numbers.
	std::vector<channel_number> channel_numbers;
	channel_numbers.reserve(channels);
	std::size_t channels_to_allocate = 1;
	if (argc > 3) {
		// Override default test value with the argument.
		channels_to_allocate = std::atoi(argv[3]); // Third argument.
		if (channels_to_allocate < 1) {
			channels_to_allocate = 1;
			std::cout << "Channels to allocate is out of range. "
			"Setting channel number to " << channels_to_allocate << std::endl;
		}
	}

	// This is an example. We can acquire and append a new channel_number
	// to the channel_numbers vector by other means.
	for (std::size_t n = 0; n < channels_to_allocate; ++n) {
		channel_number the_channel_number = invalid_channel_number;
		if (!shared.acquire_channel_number(&the_channel_number,
		the_scheduler_number, my_client_number)) {
			// Did not acquire a channel_number. Handle the timeout, etc.
			// We can change the_scheduler_number and try again, etc.
		}
		// Append the_channel_number.
		channel_numbers.push_back(the_channel_number);
	}
	std::cout << "acquired " << channel_numbers.size() << " channels"
	<< std::endl; /// debug
	
	// If we release a channel n, then after that it must no longer exist
	// in the channel_numbers vector. Copy the last element of the vector
	// over the channel number to be released, and then shrink the vector
	// size by 1 element. If we want we can also sort the vector in order
	// to perform a linear scan rather than jumping around on channels.

	//--------------------------------------------------------------------------
	// Creating a private pool. It's private to this client process and may
	// be used by a single thread only. The circular_buffer is not thread
	// safe. If we want more than one thread in this client process to
	// access a private_chunk_pool (in which case we need to create more
	// than one private_chunk_pool of a kind that is suitable [single thread
	// can use the fast circular_buffer, while two threads can use an
	// atomic_buffer, and finally if more than one thread need to push or
	// pop, we use a bounded_buffer]), we need to use a
	// bounded_buffer<chunk_index>.

	// WARNING: Not thread-safe, for single thread only.
	circular_buffer<chunk_index> private_chunk_pool(chunks);

	//--------------------------------------------------------------------------
	// Acquire 1..N chunks from the shared_chunk_pool and store them in the
	// private_chunk_pool. In this example we get two chunks per channel we
	// have allocated.
	std::size_t number_of_chunks = 2 * channel_numbers.size();
	for (std::size_t i = 0; i < number_of_chunks; ++i) {
		chunk_index ci;
		// Wait until we get a free chunk from the shared_chunk_pool...
		while (!shared.acquire_chunk_index(&ci /*, timeout milliseconds goes here */)) {
			// We didn't get a chunk.
			// Timeout or empty queue?
		}
		// Store it in our private pool.
		private_chunk_pool.push_front(ci);
	}
	std::cout << "acquired " << number_of_chunks << " chunks"
	<< std::endl; /// debug

	//--------------------------------------------------------------------------
	uint64_t iterations = 1000000000; // Default test value.
	if (argc > 1) {
		// Override default test value with the argument.
		iterations = std::atoi(argv[1]);
	}
	std::cout << "iterations = " << iterations << std::endl;
	
	uint64_t ping_messages_sent = 0;
	uint64_t pong_messages_received = 0;
	uint64_t empty_queues_since_last_message = 0;

	// Keep one chunk_index in a register (instead of an array, which
	// requires a memory access.)
	chunk_index the_chunk_index;

	/// not here! private_chunk_pool.pop_back(&the_chunk_index);
	
	// Timeout for client waiting on message(s) to arrive on out queues on
	// any channel.
	unsigned int timeout_milliseconds = 250; /// TESTING!
	
	//--------------------------------------------------------------------------
	boost::timer t; /// Start timing - for statistics

	// The example here is to demonstrate the API calls and that it works.
	
	//--------------------------------------------------------------------------
	// For a start, push a message on each channel so that the scheduler(s)
	// have something to work with.
	for (std::size_t ch = 0; ch < channel_numbers.size(); ++ch) {
		// Reference to the channel used as a shorthand.
		channel_type& the_channel = shared.channel(channel_numbers[ch]);

		// Get a chunk_index from our private chunk pool.
		private_chunk_pool.pop_back(&the_chunk_index);

		// Get the chunk.
		chunk_type& the_chunk = shared.chunk(the_chunk_index);

		// Write a message to the channel. Very simple example here:
		*((PACKED uint32_t*) &the_chunk[16]) = 0;
		*((PACKED uint32_t*) &the_chunk[20]) = 'PING';
		++ping_messages_sent; /// For statistics and debug.

		// Push the message on the channel.
		the_channel.in.push_front(the_chunk_index);

		// Notify the scheduler.
		the_channel.scheduler()->notify();
		//std::cout << "the_channel.scheduler() = "
		//<< (void*) the_channel.scheduler() << std::endl; /// debug
	}

	unsigned int state;

	//--------------------------------------------------------------------------
	// Now we start the main processing loop, scanning all queues round-
	// robin and if we pop a message - we push a message.
	while (pong_messages_received < iterations) {
		// Check the state every round-robin scan of a set of channels.
		// The state is either 0 (normal) or else, maybe the server crashed. 
		if ((state = shared.common_client_interface().get_database_state())
		== 0) {
			// Scan all channels. If receiving a message, send a message
			// right back. (This is just an example.)
			for (std::size_t ch = 0; ch < channel_numbers.size(); ++ch) {
				channel_type& the_channel
				= shared.channel(channel_numbers[ch]);

				// Non-blocking check if there is a message and process it.
				if (the_channel.out.try_pop_back(&the_chunk_index) == true) {
					empty_queues_since_last_message = 0;

					// Get the chunk.
					chunk_type& the_chunk = shared.chunk(the_chunk_index);

					// Read the message. Simple example:
					if (*((PACKED uint32_t*) &the_chunk[24]) == 'PONG') {
						++pong_messages_received; /// For debug.
					}
					
					// Write a message to the channel. Simple example:
					*((PACKED uint32_t*) &the_chunk[16]) = 0;
					*((PACKED uint32_t*) &the_chunk[20]) = 'PING';
					++ping_messages_sent; /// For debug.

					// Push the message on the channel.
					the_channel.in.push_front(the_chunk_index);

					// Notify the scheduler.
					the_channel.scheduler()->notify();
				}
				else {
					// There was no message, 
					++empty_queues_since_last_message;
				}
			}

			//------------------------------------------------------------------
			// If have not found any message after max_empty_queues_since_last_-
			// message tries, prepare to wait for a message to arrive.
			if (empty_queues_since_last_message
			>= max_empty_queues_since_last_message) {
				// Check if the notifications are currently turned off.
				if (this_client_interface.get_notify_flag() == false) {
					// Turn on notifications, then scan one more time.
					///std::cout << "Turning on notifications" << std::endl;
					this_client_interface.set_notify_flag(true);
				}
				else {
					// Notifications were turned on and a second scan was done,
					// but no message were found in any queue, so now this
					// client will wait for work.
					bool signaled = this_client_interface.wait_for_work
					(timeout_milliseconds);

					if (signaled == true) {
						// Signaled to wake up (not a timeout.)
						// Turn off notifications.
						this_client_interface.set_notify_flag(false);
						// Check database_state.
						// Check if there is any messages to process?
					}
					else {
						// Timeout. What to do now?
						// Check database_state?
						// Check if there is any messages to process?
					}
				}
			}
		}
		else {
			// The state is not normal - it is not 0.
			switch (state) {
			case 1:
				// The server crashed.
				std::cout << "Server crash detected!" << std::endl;
				break;
			default:
				std::cout << "Unknown state in client loop!" << std::endl;
				break;
			}
		}
	}

	// Get timestamp for elapsed time.
	double timestamp = t.elapsed();

	//--------------------------------------------------------------------------
	// Show statistics
	double messages_per_second = double(ping_messages_sent) / timestamp;
	double ns_per_messages = (1E9 * timestamp) / double(ping_messages_sent);

	std::cout << ping_messages_sent << " ping messages sent in "
	<< timestamp << " s" << std::endl;
	std::cout << messages_per_second  << " messages per s "
	<< std::endl;
	std::cout << ns_per_messages << " ns per message"
	<< std::endl;
	std::cout << "ping-pong = " << pong_messages_received << std::endl;

	//--------------------------------------------------------------------------
	// Release resources: chunks, channels, client_number and owner_id, etc.

	// Release the chunk(s).
	for (std::size_t i = 0; i < number_of_chunks; ++i) {
		private_chunk_pool.pop_back(&the_chunk_index);
		shared.release_chunk_index(the_chunk_index);
	}

	// Release the channel(s).
	for (std::size_t n = 0; n < channel_numbers.size(); ++n) {
		shared.release_channel_number(channel_numbers[n],
		the_scheduler_number, my_client_number);
	}

	// Release my_client_number.
	shared.release_client_number(my_client_number);

	// TODO: Release my_owner_id.
}
catch (shared_interface::shared_interface_exception&) {
	std::cerr << "client: shared_interface_exception caught" << std::endl;
	return 1;
}
catch (boost::interprocess::interprocess_exception& e) {
	std::cerr << "client: interprocess_exception caught: "
	<< e.what() << std::endl;
	return 1;
}
catch (...) {
	std::cerr << "client: unknown exception caught" << std::endl;
	return 1;
}
