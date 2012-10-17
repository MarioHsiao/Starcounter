//
// worker.hpp
// interprocess_communication/test
//
// Copyright � 2006-2012 Starcounter AB. All rights reserved.
// Starcounter� is a registered trademark of Starcounter AB.
//

#ifndef STARCOUNTER_INTERPROCESS_COMMUNICATION_WORKER_HPP
#define STARCOUNTER_INTERPROCESS_COMMUNICATION_WORKER_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#include <iostream>
#include <ios>
#include <string>
#include <sstream>
#include <algorithm>
#include <cstddef>
#include <cstdlib>
#include <memory>
#include <utility>
#include <stdexcept>
#include <boost/cstdint.hpp>
#include <boost/interprocess/managed_shared_memory.hpp>
#include <boost/interprocess/allocators/allocator.hpp>
#include <boost/interprocess/sync/interprocess_mutex.hpp>
#include <boost/interprocess/sync/scoped_lock.hpp>
#include <boost/interprocess/sync/interprocess_condition.hpp>
#include <boost/date_time.hpp>
#include <boost/date_time/posix_time/posix_time_types.hpp> // No I/O just types.
#include <boost/date_time/microsec_time_clock.hpp>
#include <boost/thread/thread.hpp>
#include <boost/thread/win32/thread_primitives.hpp>
#include <boost/call_traits.hpp>
#include <boost/bind.hpp>
#include <boost/lexical_cast.hpp>
#include <boost/timer.hpp>
#if defined(_MSC_VER)
# define WIN32_LEAN_AND_MEAN
# include <windows.h> /// TODO: thread_primitives.hpp might replace this include
# include <intrin.h>
# undef WIN32_LEAN_AND_MEAN
#endif // (_MSC_VER)
#include "../common/bit_operations.hpp"
#include "../common/macro_definitions.hpp"
#include "../common/config_param.hpp"
#include "../common/shared_interface.hpp"
#include "../common/database_shared_memory_parameters.hpp"
#include "../common/monitor_interface.hpp"
#include "../common/channel_mask.hpp"
#include "../common/channel_mask.hpp"
#include "../../Chunks/bmx/bmx.hpp"
#include "../../Chunks/bmx/chunk_helper.h"
#include "../common/chunk_pool.hpp"
#include "random.hpp"

namespace starcounter {
namespace interprocess_communication {

using namespace starcounter::core;

/// Exception class.
class worker_exception {
public:
	explicit worker_exception(uint32_t err)
	: err_(err) {}
	
	uint32_t error_code() const {
		return err_;
	}
	
private:
	uint32_t err_;
};

/// Class worker.
/**
 * @throws worker_exception when something can not be achieved.
 */
// Objects of type boost::thread are not copyable.
class worker : private boost::noncopyable {
public:
	friend class test;
	
	typedef uint32_t channel_chunk;
	
	enum state {
		stopped,
		running
	};
	
	enum {
		// The maximum number of chunks that a worker is allowed to have in its
		// private chunk_pool_ and overflow_pool_ combined. The worker will keep
		// this amount of chunks if it
		// can, but not more, so if it finds that its private chunk_pool_ has
		// more chunks than max_chunks after it has pushed chunk(s) to it, then
		// it will move chunks_to_move number of chunks from its private
		// chunk_pool_ to the shared_chunk_pool.
		max_chunks = 512, // at least min_chunks +chunks_to_move.
		
		// The worker will try to keep at least min_chunks amount of chunks in
		// its private chunk_pool, so that it does not have to move chunks from
		// the shared_chunk_pool to its private chunk_pool_ too often, which
		// would otherwise severely degrade performace.
		min_chunks = 64, // at least chunks_to_move
		
		a_bunch_of_chunks = 64
	};
	
	/// Construction.
	/**
	 * @throws starcounter::interprocess_communication::worker_exception if failing to start.
	 */
	worker();
	
	/// Destruction of the worker.
	// It waits for worker thread to finish.
	~worker();
	
	void initialize(const char* database_name);

	/// Start the worker.
	void start();
	
	/// Get/set state of the worker.
	state get_state();
	void set_state(state s);
	
	/// Is the worker running?
	bool is_running();

	/// This is the method that this worker's thread call at start. It contains
	/// the worker loop. Here is where the work is done.
	void work();
	void join();
	worker& set_worker_number(std::size_t n);
	worker& set_active_schedulers(std::size_t n);
	worker& set_shared_interface();
	worker& set_segment_name(const std::string& segment_name);
	std::string get_segment_name() const;
	worker& set_monitor_interface_name(const std::string& monitor_interface_name);
	std::string get_monitor_interface_name() const;
	worker& set_pid(const pid_type pid);
	pid_type get_pid() const;
	worker& set_owner_id(const owner_id oid);
	owner_id get_owner_id() const;

	/// The worker must call release_all_resources() before terminating its
	/// thread. Otherwise resources it may have allocated will be leaked.
	void release_all_resources();

	// For debug. TODO: Remove it.
	void show_linked_chunks(chunk_type* chunk_base, chunk_index head);
	
	// Help functions to work with the overflow_pool.

private:
	chunk_pool<chunk_index> chunk_pool_;
	chunk_pool<channel_chunk> overflow_pool_;
	boost::thread thread_;
	boost::detail::win32::handle thread_handle_;
	boost::mutex mutex_;
	std::string monitor_interface_name_;
	std::string segment_name_;
	pid_type pid_;
	owner_id owner_id_;
	
	// The state of this worker.
	volatile state state_;
	
	// BUG: Each worker has a copy of the test's shared_interface. The pointers to
	// various objects in shared memory are copied, but each worker will
	// initialize its own client_number.
	// FIX: Each worker shall create its own shared_interface, not get a copy
	// of the tests shared_interface.
	shared_interface shared_;
	std::size_t worker_id_;
	
	// At start the worker knows how many schedulers that are active, and
	// assumes that they are scheduler_interface[0..active_schedulers_ -1].
	std::size_t num_active_schedulers_;
	
	// A mask marking which of all channels this worker have acquired.
	////channel_mask<channels> channel_mask_;
	
	// An array of indexes to channels, unordered.
	channel_number channel_[channels];
	
	// Number of channel indexes stored in the channel_ array.
	std::size_t num_channels_;
	
	// A random number generator is used to simulate messages arriving on the
	// TCP/IP stack. The message will contain a number n that is to be used:
	// channel_[n], which gives the number to the channel where the message
	// is to be sent. A blast ping message will be sent during simulation.
	random_generator random_generator_;
	
	// Number of acquired chunks.
	uint64_t acquired_chunks_;
	
	// For statistics - to be removed.
	boost::timer timer_;
	double timestamp_;
	uint64_t scan_counter_;
	uint64_t empty_chunk_pool_counter_;
};

} // namespace interprocess_communication
} // namespace starcounter

#include "impl/worker.hpp"

#endif // STARCOUNTER_INTERPROCESS_COMMUNICATION_WORKER_HPP
