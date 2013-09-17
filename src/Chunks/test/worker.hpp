//
// worker.hpp
// interprocess_communication/test
//
// Copyright © 2006-2013 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#ifndef STARCOUNTER_INTERPROCESS_COMMUNICATION_WORKER_HPP
#define STARCOUNTER_INTERPROCESS_COMMUNICATION_WORKER_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#include <cstdint>
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
#include <boost/bind.hpp>
#include <boost/lexical_cast.hpp>
#if defined(_MSC_VER)
# define WIN32_LEAN_AND_MEAN
# include <windows.h>
# include <intrin.h>
# undef WIN32_LEAN_AND_MEAN
#endif // (_MSC_VER)
#include "../common/noncopyable.hpp"
#include "../common/thread.hpp"
#include "../common/bit_operations.hpp"
#include "../common/macro_definitions.hpp"
#include "../common/config_param.hpp"
#include "../common/shared_interface.hpp"
#include "../common/database_shared_memory_parameters.hpp"
#include "../common/monitor_interface.hpp"
#include "../common/chunk.hpp"
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
// Objects of type thread are not copyable.
class worker : private noncopyable {
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
		max_chunks = 4096, // at least min_chunks +chunks_to_move.
		
		// The worker will try to keep at least min_chunks amount of chunks in
		// its private chunk_pool, so that it does not have to move chunks from
		// the shared_chunk_pool to its private chunk_pool_ too often, which
		// would otherwise severely degrade performace.
		min_chunks = 64, // at least chunks_to_move
		
		a_bunch_of_chunks = 64
	};
	
	enum {
		// The number of times to scan through all channels trying to push or
		// pop before waiting, in case did not push or pop for spin_count_reset
		// number of times. Need to experiment with this value.
		// NOTE: scan_counter_preset must be > 0.
		scan_counter_preset = 1 << 10,
		
		// The thread can give up waiting after wait_for_work_milli_seconds, but
		// if not, set it to INFINITE.
		wait_for_work_milli_seconds = INFINITE
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
	static void work(worker*);
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
	
	// For debug.
	void show_linked_chunks(chunk_type* chunk_base, chunk_index head);
	
	std::size_t id() const {
		return worker_id_;
	}
	
	const shared_interface& shared() const {
		return shared_;
	}
	
	shared_interface& shared() {
		return shared_;
	}
	
	// Help functions to work with the overflow_pool.
	std::size_t num_channels() const {
		return num_channels_;
	}
	
	channel_number channel(std::size_t i) {
		return channel_[i];
	}
	
	//chunk_pool<chunk_index>& get_chunk_pool() {
	//	return chunk_pool_;
	//}
	
	uint64_t& acquired_chunks() {
		return acquired_chunks_;
	}
	
    /// Return number of pushed items.
    /**
     * @return Number of pushed items.
     */
    uint64_t pushed() const {
        return pushed_;
    }
	
    /// Return number of popped items.
    /**
     * @return Number of popped items.
     */
    uint64_t popped() const {
        return popped_;
    }
	
	template<class T>
	class chunk_pool_list;
	
	typedef chunk_pool_list<chunk_type::link_type> chunk_pool_list_type;

	/// Get reference to chunk_pool(n) where n is the channel number.
	/**
	 * @return A reference to chunk_pool(n).
	 */
	chunk_pool_list_type& get_chunk_pool_list(channel_number n) {
		return chunk_pool_list_[n];
	}
	
#if 0
	/// Get const reference to chunk_pool(n) where n is the channel number.
	/**
	 * @return A const reference to chunk_pool(n).
	 */
	const chunk_pool_list& get_chunk_pool_queue(channel_number n) const {
		return chunk_pool_queue_[n];
	}
#endif
	
public:
	// chunk_pool_list is not thread safe. The list is based on linking chunks
	// that are stored in the the chunks themselves.
	template<typename T>
	class chunk_pool_list { // TODO: Rename it to chunk_pool.
	public:
		// Type definitions.
		typedef T value_type;
		typedef value_type& reference;
		typedef const value_type& const_reference;
		
		enum {
			link_terminator = chunk_type::link_terminator
		};
		
		chunk_pool_list()
		: front_(link_terminator),
		back_(link_terminator),
		chunk_(0) {}
		
		/// set_chunk_ptr() stores the address of the first chunk in an array of
		/// chunks, so that operations on the chunk_pool_list can be done. The
		/// address is relative to the worker thread operating on the chunk_pool_list.
		/**
		 * @param p The address of chunk[0], in the array of chunks.
		 */
		void set_chunk_ptr(chunk_type* p) {
			chunk_ = p;
		}
		
		/// empty() checks whether the queue is empty 
		/**
		 * @return true if the queue is empty, false otherwise.
		 */
		bool empty() const {
			return front_ == link_terminator;
		}
		
		/// Removes all elements from the list.
		void clear() {
			while (!empty()) {
				value_type temp_front = chunk(front()).get_next();
				chunk(front()).terminate_next();
				front_ = temp_front;
			}

			front_ = link_terminator;
			back_ = link_terminator;
		}

		/// front() returns a reference to the first element in the chunk_pool_list.
		/// Calling front() on an empty container is undefined.
		/**
		 * @return A reference to the first element in the chunk_pool_list.
		 */
		reference front() {
			return front_;
		}
		
		/// front() returns a const reference to the first element in the chunk_pool_list.
		/// Calling front() on an empty container is undefined.
		/**
		 * @return A const reference to the first element in the chunk_pool_list.
		 */
		/* constexpr */ const_reference front() const {
			return front_;
		}
		
		/// pop_front() removes the first element of the queue. 
		void pop_front() {
			if (!empty()) {
				value_type temp_front = chunk(front()).get_next();
				chunk(front()).terminate_next();
				front_ = temp_front;
			}
		}
		
		/// push_front() prepends the given element value to the beginning of the queue.
		/**
		 * @param n A reference to the link that will point to the popped chunk
		 *		if returning true.
		 */
		void push_front(value_type n) {
			if (!empty()) {
				chunk(n).set_next(front_);
				front_ = n;
			}
			else {
				front_ = n;
				back_ = n;
				chunk(n).set_next(link_terminator);
			}
		}
		
		// Returns a reference to chunk[n] relative to the worker process
		// address space.
		chunk_type& chunk(chunk_index n) {
			return chunk_[n];
		}
		
	private:
		value_type front_;
		value_type back_;
		
		// chunk_ is a pointer relative to the process address space.
		chunk_type* chunk_;
	};
	
private:
	// There is one chunk_pool_list per scheduler so that the number
	// of chunks that can end up in a given channels overflow queue are limited to
	// the number of chunks in the corresponding loop of flow between each worker
	// and scheduler pair, and those loops are isolated from each other.
	// This hinders the chunk starvation that occurred when using only one private
	// chunk pool per worker (shared between that worker and all schedulers it
	// communicates with.)
	// NOTE: Since now the links in each chunks are always going to be touched
	// it is most probablyt best to put those links at the beginning of the chunk
	// rather than at the end, to avoid always touching the last cache line in the
	// chunk. TODO: Issue #993.
	chunk_pool_list_type chunk_pool_list_[max_number_of_schedulers];

	//starcounter::core::chunk_pool<chunk_index> chunk_pool_; // OLD CHUNK POOL TO BE REPLACED!

	thread thread_;
	thread::native_handle_type thread_handle_;
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
	
    // For statistics:
    uint64_t pushed_;
    uint64_t popped_;

	// Avoid false sharing. TODO: Calculate the number of bytes to pad with.
	uint8_t pad_0_[CACHE_LINE_SIZE];
};

} // namespace interprocess_communication
} // namespace starcounter

#include "impl/worker.hpp"

#endif // STARCOUNTER_INTERPROCESS_COMMUNICATION_WORKER_HPP
