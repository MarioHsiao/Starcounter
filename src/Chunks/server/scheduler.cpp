//
// scheduler.cpp
//
// Copyright © 2006-2013 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#include <iostream> // debug
#include <iomanip> // debug
#include <ios> // debug

#include <cstddef>
#include <climits>
#include <xmmintrin.h>
#include <boost/cstdint.hpp>
#include <boost/thread/thread.hpp>
#include <boost/thread/mutex.hpp>
#include <boost/thread/condition.hpp>
#include <boost/shared_ptr.hpp>
#include <boost/call_traits.hpp>
#include <boost/bind.hpp>
#include <boost/utility.hpp>
#include <boost/noncopyable.hpp>
#if defined(_MSC_VER)
# define WIN32_LEAN_AND_MEAN
# include <intrin.h>
# undef WIN32_LEAN_AND_MEAN
#endif // defined(_MSC_VER)
#include "../common/circular_buffer.hpp"
#include "../common/bounded_buffer.hpp"
#include "../common/chunk.hpp"
#include "../common/shared_chunk_pool.hpp"
#include "../common/channel.hpp"
#include "../common/common_scheduler_interface.hpp"
#include "../common/common_client_interface.hpp"
#include "../common/scheduler_channel.hpp"
#include "../common/scheduler_interface.hpp"
#include "../common/client_interface.hpp"
#include "../common/config_param.hpp"
#include "../common/macro_definitions.hpp"
#include "scheduler.hpp"
#include "../common/interprocess.hpp"
#include "../common/name_definitions.hpp"
#include "../common/database_shared_memory_parameters.hpp"
#include "../common/overflow_buffer.hpp"
#include "../common/config_param.hpp"
#include "../common/bit_operations.hpp"
#include "../common/owner_id_value_type.h"
#include "../common/monitor_interface.hpp"

#include <scerrres.h>

#define _E_UNSPECIFIED SCERRUNSPECIFIED
#define _E_INVALID_SERVER_NAME SCERRINVALIDSERVERNAME
#define _E_WAIT_TIMEOUT SCERRWAITTIMEOUT
#define _E_INPUT_QUEUE_FULL SCERRINPUTQUEUEFULL

typedef struct _sc_io_event
{
	unsigned long channel_index_;
	unsigned long chunk_index_;
} sc_io_event;

EXTERN_C unsigned long server_initialize_port(void *port_mem128, const char *name, unsigned long port_number, owner_id_value_type owner_id_value);
EXTERN_C unsigned long server_get_next_signal_or_task(void *port, unsigned int timeout_milliseconds, sc_io_event *pio_event);
EXTERN_C unsigned long server_get_next_signal(void *port, unsigned int timeout_milliseconds, unsigned long *pchunk_index);
EXTERN_C long server_has_task(void *port);
EXTERN_C unsigned long sc_try_receive_from_client(void *port, unsigned long channel_index, unsigned long *pchunk_index);
EXTERN_C unsigned long sc_send_to_client(void *port, unsigned long channel_index, unsigned long chunk_index);
EXTERN_C unsigned long server_send_task_to_scheduler(void *port, unsigned long port_number, unsigned long message);
EXTERN_C unsigned long server_send_signal_to_scheduler(void *port, unsigned long port_number, unsigned long message);
EXTERN_C unsigned long sc_acquire_shared_memory_chunk(void *port, unsigned long channel_index, unsigned long *pchunk_index);
EXTERN_C unsigned long sc_acquire_linked_shared_memory_chunks(void *port, unsigned long channel_index, unsigned long start_chunk_index, unsigned long needed_size);
EXTERN_C unsigned long sc_acquire_linked_shared_memory_chunks_counted(void *port, unsigned long channel_index, unsigned long start_chunk_index, unsigned long num_chunks);
EXTERN_C void *sc_get_shared_memory_chunk(void *port, unsigned long chunk_index);
EXTERN_C unsigned long sc_release_linked_shared_memory_chunks(void *port, unsigned long start_chunk_index);
EXTERN_C void sc_add_ref_to_channel(void *port, unsigned long channel_index);
EXTERN_C void sc_release_channel(void *port, unsigned long channel_index);

namespace starcounter {
namespace core {

extern shared_memory_object global_segment_shared_memory_object;
extern mapped_region global_mapped_region;

class server_port {
	typedef uint64_t mask_type;

	enum {
		mask_bit_size = sizeof(mask_type) * CHAR_BIT,
		channel_masks_ = (channels +mask_bit_size -1) / mask_bit_size
	};

	scheduler_channel_type *this_scheduler_task_channel_;
	scheduler_channel_type *this_scheduler_signal_channel_;
	
	// Base pointer to scheduler_interface.
	scheduler_interface_type* scheduler_interface_;
	
	// Pointer to this scheduler's interface.
	scheduler_interface_type *this_scheduler_interface_;
	
	common_scheduler_interface_type* common_scheduler_interface_;
	common_client_interface_type* common_client_interface_;
	channel_type *channel_;

	// Keep track of the next channel to check for incomming messages.
	channel_number next_channel_;

	scheduler_channel_type *scheduler_task_channel_;
	scheduler_channel_type *scheduler_signal_channel_;
	chunk_type *chunk_;
	shared_chunk_pool_type* shared_chunk_pool_;
	std::size_t id_;

	// TODO: Remove gotoxy() - used during debug.
	void gotoxy(int16_t x, int16_t y) {
		COORD coord;
		coord.X = x;
		coord.Y = y;
		SetConsoleCursorPosition(GetStdHandle(STD_OUTPUT_HANDLE), coord);
	}
	
	owner_id& get_owner_id() {
		return this_scheduler_interface_->get_owner_id();
	}

public:
	enum {
		// TODO: Experiment with this treshold, which decides when to acquire
		// linked chunks from the private chunk_pool or the shared_chunk_pool.
		a_bunch_of_chunks = 64
	};
	
	server_port()
	: next_channel_(0) {}
	
	unsigned long init(const char *name, std::size_t id, owner_id oid);
	unsigned long get_next_signal_or_task(unsigned int timeout_milliseconds, sc_io_event &the_io_event);
	unsigned long get_next_signal(unsigned int timeout_milliseconds, unsigned long *pchunk_index);
	long has_task();
	void send_to_client(unsigned long the_channel_index, chunk_index the_chunk_index);
	unsigned long try_receive_from_client(unsigned long the_channel_index, chunk_index &the_chunk_index);
	unsigned long send_task_to_scheduler(unsigned long port_number, chunk_index the_chunk_index);
	unsigned long send_signal_to_scheduler(unsigned long port_number, chunk_index the_chunk_index);
	void add_ref_to_channel(unsigned long the_channel_index);
	
	/// Release a reference to the channel. NOTE: This have nothing to do with
	/// actually releasing the channel so that any client can allocate it.
	/// A better name might be remove_ref_to_channel(channel_number);
	void release_channel(unsigned long the_channel_index);
	
	/// Scheduler's call do_release_channel() after they see that the
	/// channel is marked "to be released", and if the clean up flag is set also
	/// then all resources (chunks, channels, client_interface) the client had
	/// allocated are released. If the clean up flag is not set, then only the
	/// channel (including the chunks there in and in the overflow_pool) are
	/// released and made available to any client.
	/// NOTE: It would have been named release_channel() had not that been in
	/// use already.
	/**
	 * @param the_channel_number The channel_number to be released, including
	 *		all chunks in the channel or targeted to the channel.
	 */
	void do_release_channel(channel_number the_channel_number);
	void release_channel_marked_for_release(channel_number the_channel_index);

	std::size_t number_of_active_schedulers();
	
	//--------------------------------------------------------------------------
	/// open_ipc_monitor_cleanup_event() is called by the constructor.
	HANDLE& open_ipc_monitor_cleanup_event();

	/// Get a reference to the ipc_monitor_cleanup_event. It is opened by the
	/// constructor and closed by the destructor.
	HANDLE& ipc_monitor_cleanup_event();

	//--------------------------------------------------------------------------
	// This function is obsolete and shall be replaced with
	// acquire_linked_chunks() and release_linked_chunks().
	//void acquire_chunk_index(unsigned long& the_chunk_index); // OBSOLETE API
	
	/// Allocate linked chunks from the shared_chunk_pool. It is a "timed"
	/// function that can fail.
	/**
	 * @param head Will upon return contain the head of a linked chain of chunks
	 *		if successfuly acquired the requested size.
	 * @param size The number of bytes to allocate as 1..N linked chunks. The
	 *		chunks require some space for header data and this is taken into
	 *		account.
	 * @param client_interface_ptr A pointer to the client_interface where the
	 *		chunk is marked as owned.
	 * @param timeout_milliseconds The number of milliseconds to wait before a
	 *      timeout may occur.
	 * @return true if successfully acquired the linked chunks with the
	 *		requested amount of memory before the time period specified by
	 *		timeout_milliseconds has elapsed, otherwise false if not enough
	 *		space or the time period has elapsed.
	 */
	bool acquire_linked_chunks(chunk_index& head, std::size_t size,
	client_interface_type* client_interface_ptr, uint32_t timeout_milliseconds
	= 10000);

    /// Allocate linked chunks from the shared_chunk_pool. It is a "timed"
	/// function that can fail.
	/**
	 * @param head Will upon return contain the head of a linked chain of chunks
	 *		if successfuly acquired the requested size.
	 * @param num_chunks The number of chunks to allocate.
	 * @param client_interface_ptr A pointer to the client_interface where the
	 *		chunk is marked as owned.
	 * @param timeout_milliseconds The number of milliseconds to wait before a
	 *      timeout may occur.
	 * @return true if successfully acquired the linked chunks with the
	 *		requested amount of memory before the time period specified by
	 *		timeout_milliseconds has elapsed, otherwise false if not enough
	 *		space or the time period has elapsed.
	 */

	bool acquire_linked_chunks_counted(chunk_index& head, std::size_t num_chunks,
	client_interface_type* client_interface_ptr, uint32_t timeout_milliseconds
	= 10000);
	
	/// Scheduler release linked chunks.
	/// NOTE: After calling this, the message data in the linked chunks may be
	/// unreadable even if unsuccessfull when trying to release the linked
	/// chunks, because some chunks may have been released and thus the message
	/// data may be cut.
	/**
	 * @param head The head of the linked chunks, will upon return contain
	 *		chunk_type::LINK_TERMINATOR if successfully released all linked
	 *		chunks, otherwise it contains the chunk_index pointing to the head
	 *		of the linked chunks that are left.
	 * @param client_interface_ptr A pointer to the client_interface where the
	 *		chunk is marked as not owned.
	 * @param timeout_milliseconds The number of milliseconds to wait before a
	 *      timeout may occur.
	 * @return true if successfully released all linked chunks in which case
	 *		head is set to chunk_type::LINK_TERMINATOR, otherwise returns false
	 *		if failed to release all linked chunks, or the time period has
	 *		elapsed. NOTE: It is expected that release_linked_chunks() never
	 *		fail once it has locked the queue, so if it fails it should be due
	 *		to the time period has elapsed.
	 */
	bool release_linked_chunks(chunk_index& head, client_interface_type*
	client_interface_ptr, uint32_t timeout_milliseconds = 10000);
	
	//--------------------------------------------------------------------------
	/// Acquire N (unlinked) chunks from the shared_chunk_pool to a private
	/// chunk_pool.
	/**
	 * @param private_chunk_pool Reference to the private chunk_pool to which
	 *		chunks are allocated/moved. The chunks are not marked as owned by
	 *		any client.
	 * @param chunks_to_acquire The number of chunks to acquire.
	 * @param timeout_milliseconds The number of milliseconds to wait before a
	 *		timeout may occur while trying to lock the shared_chunk_pool.
	 * @return The number of acquired chunks. If the private_chunk_pool is empty
	 *		or becomes empty when acquiring chunks, the acquirement process is
	 *		stopped and the job is half done.
	 */
	template<typename U>
	std::size_t acquire_from_shared_to_private(U& private_chunk_pool,
	std::size_t chunks_to_acquire, uint32_t timeout_milliseconds = 10000);
	
	/// Release N (unlinked) chunks from a private chunk_pool to the
	/// shared_chunk_pool.
	/**
	 * @param private_chunk_pool Reference to the private chunk_pool from which
	 *		chunks are released.
	 * @param chunks_to_release The number of chunks to release.
	 * @param timeout_milliseconds The number of milliseconds to wait before a
	 *		timeout may occur while trying to lock the shared_chunk_pool.
	 * @return The number of released chunks.
	 */
	template<typename U>
	std::size_t release_from_private_to_shared(U& private_chunk_pool,
	std::size_t chunks_to_release, uint32_t timeout_milliseconds = 10000);
	
	//--------------------------------------------------------------------------
	unsigned long acquire_linked_chunk_indexes(unsigned long channel_number, unsigned long start_chunk, unsigned long needed_size);
    unsigned long acquire_linked_chunk_indexes_counted(unsigned long channel_number, unsigned long start_chunk, unsigned long num_chunks);
    unsigned long acquire_one_chunk(unsigned long channel_number, chunk_index* out_chunk_index);

    /// Releases linked chunks to a private chunk_pool and if there is a bunch there
    /// then to the shared_chunk_pool.
	/**
	 * @param start_chunk_index Index of the first chunk.
	 * @return 0 on success otherwise error.
	 */
    unsigned long release_linked_chunks(chunk_index start_chunk_index);
	
	//--------------------------------------------------------------------------
	/// client_release_linked_chunks() is used by the scheduler to do the clean
	/// up, releasing chunks of a client_interface. The pointer to the
	/// client_interface is obtained via a channel and must belong to a client
	/// that has terminated. This function is only used during clean up.
	/**
	 * @param client_interface_ptr A pointer to the client_interface where the
	 *		chunk is marked as owned.
	 * @param timeout_milliseconds The number of milliseconds to wait before a
	 *		timeout may occur. TODO: implement timeout_milliseconds!?
	 * @return false if failing to release the chunk_index. It can happen if the
	 *		lock of the queue was not obtained.
	 */
	bool release_clients_chunks(client_interface_type* client_interface_ptr,
	uint32_t timeout_milliseconds = 10000);
	
	//--------------------------------------------------------------------------
	/// Scheduler's call release_channel_number() after they see that the
	/// channel is marked "to be released."
	/**
	 * @param the_channel_number The channel_number to be released.
	 * @param scheduler_number Selects the scheduler where the channel_number
	 *		was allocated from.
	 * @param spin_count Spin at most spin_count number of times, and try to
	 *		release the item without blocking. If it fails, the calling thread
	 *		can be blocked for the time period timeout_milliseconds.
	 * @param timeout_milliseconds The number of milliseconds to wait before a
	 *		timeout may occur. TODO: implement timeout_milliseconds.
	 * @return false if the call is returning because the client_number pool is
	 *		full (impossible) or the time period specified by
	 *		timeout_milliseconds has elapsed, true otherwise.
	 */
	bool release_channel_number(channel_number the_channel_number,
	scheduler_number the_scheduler_number, uint32_t spin_count = 1000000,
	uint32_t timeout_milliseconds = 10000);
	
	//--------------------------------------------------------------------------
	/// Get reference to chunk[N].
	/**
	 * @param the_chunk_index The index of chunk[N].
	 * @return Reference to chunk[N].
	 */
	chunk_type& chunk(chunk_index the_chunk_index) const;
	
	//--------------------------------------------------------------------------
	chunk_type *get_chunk(chunk_index the_chunk_index)
	{
		return (chunk_ + the_chunk_index);
	}
	
	//--------------------------------------------------------------------------
	// Get reference to channel[n].
	channel_type& channel(std::size_t n) const {
		return channel_[n];
	}
	
	//--------------------------------------------------------------------------
	/// This is for debug purpose only. It prints a list showing how the chunks
	/// beginning with the head are linked together. TODO: remove it.
	void show_linked_chunks(chunk_index head);
	
	//--------------------------------------------------------------------------
	/// Get reference to the common_client_interface.
	/**
	 * @return Reference to the common_client_interface.
	 */
	common_client_interface_type& common_client_interface() const {
		return *common_client_interface_;
	}

private:
	FORCE_INLINE unsigned long prepare_wait_or_wait_for_signal(unsigned long timeout_milliseconds)
	{
		// If the notify flag is not set, then this was the first scan not
		// finding any message to process since last time we had work to do.
		if (this_scheduler_interface_->get_notify_flag() == false) {
			// First scan - no messages found.
			if (timeout_milliseconds == 0) {
				return _E_WAIT_TIMEOUT;
			}
			
			// We're to block the thread. Set the notify flag, and then re-scan
			// again, just in case a client or a scheduler have pushed work to
			// this scheduler.
			this_scheduler_interface_->set_notify_flag(true);
			return 0;
		}
		else { /// TODO: Check if this code is correct and suitable for release.
			// Second scan completed and no messages found. The notify flag is
			// already set, so now this scheduler will wait specified ms for
			// work.
			bool signaled = this_scheduler_interface_->wait_for_work
			(timeout_milliseconds);
			
			/// I think this is a bug, turning off notifications should be done
			/// only if (signaled), not here.
			this_scheduler_interface_->set_notify_flag(false);
			
			if (signaled) {
				// This scheduler has been notified that there is work to do.
				// Turn off notifications.
				return 0;
			}
			// Not signaled it was a timeout.
			return _E_WAIT_TIMEOUT;
		}
	}
};


unsigned long server_port::init(const char* database_name, std::size_t id, owner_id oid) {
	try {
		if (!global_mapped_region.is_valid()) {
			return SCERRINVALIDGLOBALSEGMENTSHMOBJ;
		}
		
		id_ = id;
		simple_shared_memory_manager *pm = new (global_mapped_region.get_address())
		simple_shared_memory_manager;
		
		// Find the chunks.
		chunk_ = (chunk_type *)pm->find_named_block
		(starcounter_core_shared_memory_chunks_name);

		// Find the shared_chunk_pool.
		shared_chunk_pool_type* shared_chunk_pool = (shared_chunk_pool_type*)
		pm->find_named_block
		(starcounter_core_shared_memory_shared_chunk_pool_name);

		shared_chunk_pool_ = shared_chunk_pool;

		// Find the scheduler_channels.
		scheduler_task_channel_ = (scheduler_channel_type*) pm->find_named_block
		(starcounter_core_shared_memory_scheduler_task_channels_name);

		scheduler_signal_channel_ = (scheduler_channel_type*) pm->find_named_block
		(starcounter_core_shared_memory_scheduler_signal_channels_name);

		// Get this scheduler's scheduler_channel.
		this_scheduler_task_channel_ = scheduler_task_channel_ + id_;

		this_scheduler_signal_channel_ = scheduler_signal_channel_ + id_;

		// Find the common_scheduler_interface.
		common_scheduler_interface_ = (common_scheduler_interface_type*)
		pm->find_named_block
		(starcounter_core_shared_memory_common_scheduler_interface_name);
		
		common_scheduler_interface_->set_scheduler_number_flag(id);

		// Find the common_client_interface.
		common_client_interface_ = (common_client_interface_type*)
		pm->find_named_block
		(starcounter_core_shared_memory_common_client_interface_name);

		// Find the channels.
		channel_ = (channel_type*) pm->find_named_block
		(starcounter_core_shared_memory_channels_name);

		// Find the scheduler_interfaces.
		scheduler_interface_type* scheduler_interface =
		(scheduler_interface_type*) pm->find_named_block
		(starcounter_core_shared_memory_scheduler_interfaces_name);

		// Get base pointer to scheduler_interface.
		scheduler_interface_ = scheduler_interface;
		
		// Get this scheduler's scheduler_interface.
		this_scheduler_interface_ = scheduler_interface + id_;
		
		// Assign owner_id.
		this_scheduler_interface_->set_owner_id(oid);

		// Find the client_interface for this scheduler and store it in this
		// scheduler_interface. The value of the client_interface pointer is
		// relative to this scheduler's address space and that is the reason
		// that the scheduler stores its client_interface pointer in this
		// scheduler_interface, so that the client can find it and copy it,
		// plus the index of the client interface, to the channel, and then the
		// scheduler can use it via the channel.
		client_interface_type* client_interface = (client_interface_type*)
		pm->find_named_block
		(starcounter_core_shared_memory_client_interfaces_name);
		
		this_scheduler_interface_->set_client_interface(client_interface);

		//----------------------------------------------------------------------
		this_scheduler_interface_->set_notify_flag(false);
		this_scheduler_interface_->set_predicate(true);
		common_scheduler_interface_->set_scheduler_number_flag(id_);
	}
	catch (...) {
		return SCERRSERVERPORTUNKNOWNEXCEPTION;
	}
	return 0;
}

//------------------------------------------------------------------------------
#if 0 /// TODO: Test if this method results in a faster scan of the channels.
// Flags which words to scan, those channel_scan words that are not 0.
mask_type words_to_scan; /// TODO: Align on cache-line boundary.

// Flags for which of 4096 channels to scan.
mask_type channel_scan[64]; /// TODO: Align on cache-line boundary.

for (mask_type word_mask = words_to_scan; word_mask; word_mask &= word_mask -1) {
    std::size_t i = bit_scan_forward(word_mask);
    for (mask_type bit_mask = channel_scan[i]; bit_mask; bit_mask &= bit_mask -1) {
        std::size_t this_channel = bit_scan_forward(bit_mask);
        // Probe the channels in queue to see if there is a message...and process it.
    }
}

/// The fastest would be to use 64 channels:
for (mask_type channels_mask = get_channel_scan_mask();
channels_mask; channels_mask &= channels_mask -1) {
    std::size_t this_channel = bit_scan_forward(channels_mask);
    // Probe the channels in queue to see if there is a message...and process it.
}

#endif /// TODO: Test if this method results in a faster scan of the channels.
//------------------------------------------------------------------------------

unsigned long server_port::get_next_signal_or_task(unsigned int timeout_milliseconds,
sc_io_event& the_io_event) try {
	// The scheduler works with a chunk via the_chunk_index. Of course we may
	// work with any number of chunks at the same time, but here we only work
	// with one chunk at any given time.
	chunk_index the_chunk_index;
	
	while (true) {
		if (this_scheduler_signal_channel_->in.try_pop_back(&the_chunk_index) == true)
		{
			the_io_event.channel_index_ = invalid_channel_number;
			the_io_event.chunk_index_ = the_chunk_index;
			return 0;
		}

		// Check the in queue of this scheduler.
		if (this_scheduler_task_channel_->in.try_pop_back(&the_chunk_index) == true)
		{
			// Got an internal message from some scheduler.
			the_io_event.channel_index_ = invalid_channel_number;
			the_io_event.chunk_index_ = the_chunk_index;
			return 0;
		}

		if (!common_client_interface().client_interfaces_to_clean_up()) {
			// No clean up to do.
			goto check_next_channel;
		}
		else {
			/// At least one client_interface shall be cleaned up. Since the
			/// "attention" to various channels or schedulers can vary, the best
			/// seems to force a scan of all channels here.
			
			/// Scan through all channels of this scheduler and check if they
			/// are marked for release.
			
			for (channel_number mask_word_counter = 0;
			mask_word_counter < channel_masks_; ++mask_word_counter) {
				for (mask_type mask = this_scheduler_interface_
				->get_channel_mask_word(mask_word_counter);
				mask; mask &= mask -1) {
					channel_number this_channel = bit_scan_forward(mask);
					this_channel += mask_word_counter << 6;
					channel_type& the_channel = channel_[this_channel];
					
					// Check if the channel is marked for release, assuming not.
					if (!the_channel.is_to_be_released()) {
						continue; // check next...
					}
					else {
						// The channel has been marked for release. The release of
						// the channel will be done when there are no more server
						// references to the channel, i.e., there is tranquility in
						// the channel. The state of tranquility in the channel
						// remains for as long as the scheduler don't pop another
						// message from the in queue of this channel.
						if (the_channel.get_server_refs() != 0) {
							// No tranquility yet in the channel.
							continue;
						}
						else {
							// Tranquility in the channel. Releasing it.
							do_release_channel(this_channel);
						}
					}
				}
			}
		}

check_next_channel:
		for (channel_number mask_word_counter = next_channel_ >> 6;
		mask_word_counter < channel_masks_; ++mask_word_counter) {
			uint32_t prev = (next_channel_ & 63);
			for (mask_type mask = (this_scheduler_interface_
			->get_channel_mask_word(mask_word_counter) >> prev) << prev;
			mask; mask &= mask -1) {
				channel_number this_channel = bit_scan_forward(mask);
				this_channel += mask_word_counter << 6;
				// next_channel_ = (this_channel +1) % channels;
				next_channel_ = (this_channel +1) & (channels -1);
				channel_type& the_channel = channel_[this_channel];
				
				// Check if the channel is marked for release, assuming not.
				if (!the_channel.is_to_be_released()) {
					// An attempt to improve the flow of messages circulating in
					// the system is that the scheduler shall attempt to move
					// all items (if any) from the out overflow queue in the given
					// channel to the out queue - before trying to get a new
					// task from the in queue. The question is if this improves
					// or degrades the performance. Cancel out this code to see
					// if there is a difference. I'm not sure the quality of the
					// implemention is good enough either.
					while (!the_channel.out_overflow().empty()) {
						if (!the_channel.out.try_push_front(the_channel
						.out_overflow().front())) {
							// Failed to push the item. Not removing it from
							// the out_overflow queue.
							break;
						}
						
						// The item was successfully pushed to the out buffer.
						// Removing the item from the overflow queue.
						the_channel.out_overflow().pop_front();
						
						// Notify the client that the channel::out buffer might
						// not be empty.
						the_channel.client()->notify();
					}
					
					// Check if there is a message and process it.
					if (the_channel.in.try_pop_back(&the_chunk_index) == true) {
						// Prefetching selected cache-lines of the chunk.
						// How many cache-lines to prefetch in the beginning of
						// the chunk is unclear. TODO: Testing needed!
						char* chunk_addr = (char*) &chunk(the_chunk_index);
						_mm_prefetch(chunk_addr +CACHE_LINE_SIZE * 0, _MM_HINT_T0);
						_mm_prefetch(chunk_addr +CACHE_LINE_SIZE * 1, _MM_HINT_T0);
						_mm_prefetch(chunk_addr +CACHE_LINE_SIZE * 2, _MM_HINT_T0);
						_mm_prefetch(chunk_addr +CACHE_LINE_SIZE * 3, _MM_HINT_T0);
						
						// The last cache-line contains the link fields, which is
						// currently always checked. However, maybe checking the
						// link fields is not always necessary. TODO: Consider this optimization.
						_mm_prefetch(chunk_addr +chunk_size -CACHE_LINE_SIZE, _MM_HINT_T0);

						// Notify the client that the in queue is not full.
						the_channel.client()->notify();
						
						// Add a reference to for the caller. The caller will
						// release the channel request has been processed.
						the_channel.add_server_ref();
						the_io_event.channel_index_ = this_channel;
						the_io_event.chunk_index_ = the_chunk_index;

						// Successfully fetched a message from the given channel.
						return 0;
					}
				}
				else {
					// The channel has been marked for release. The release of
					// the channel will be done when there are no more server
					// references to the channel, i.e., there is tranquility in
					// the channel. The state of tranquility in the channel
					// remains for as long as the scheduler don't pop another
					// message from the in queue of this channel.
					if (the_channel.get_server_refs() != 0) {
						// No tranquility yet in the channel.
						continue;
					}
					else {
						// Tranquility in the channel. Releasing it.
						release_channel_marked_for_release(this_channel);
					}
				}
			}
			
			// A 64-bit mask word have been scanned, therefore add mask size 64.
			next_channel_ += 64;
			
			// Keep the mask word counter value (bit 7:6), and clear all other bits.
			next_channel_ &= 192; // ...011000000
		}
		
		// The scheduler has completed a scan of all its channels in queues.
		this_scheduler_interface_->increment_channel_scan_counter();
				
		// In the last scan we did not find any message to process in any of the
		// channels that this scheduler watches (according to the mask), or in
		// the in queue of this schedulers channel. Therefore this scheduler
		// prepares to wait for work. If the notify flag is not set, then this
		// was the first scan not finding any message to process since last time
		// we had work to do.
		unsigned long r = prepare_wait_or_wait_for_signal(timeout_milliseconds);
		if (r == 0) {
			continue;
		}
		return r;
	}
	// The scheduler thread exit here. The server should join the thread.
}
catch (boost::interprocess::interprocess_exception&) {
	return (unsigned long) -1;
}
catch (...) {
	return (unsigned long) -1;
}

unsigned long server_port::get_next_signal(unsigned int timeout_milliseconds, unsigned long *pchunk_index)
{
	unsigned long r;
	do
	{
		if (this_scheduler_signal_channel_->in.try_pop_back((chunk_index *)pchunk_index)) {
			return 0;
		}
		r = prepare_wait_or_wait_for_signal(timeout_milliseconds);
	}
	while (r == 0);
	return r;
}

long server_port::has_task() {
	if (this_scheduler_task_channel_->in.has_more()) return 1;

	for (channel_number n = 0; n < channel_masks_; ++n) {
		for (mask_type mask = this_scheduler_interface_
		->get_channel_mask_word(n); mask; mask &= mask -1) {
			uint32_t ch = bit_scan_forward(mask);
			ch += n << 6;
			if (channel_[ch].in.has_more()) return 1;
		}
	}
	return 0;
}

unsigned long server_port::try_receive_from_client(unsigned long
the_channel_index, chunk_index &the_chunk_index) {
	// We assume that, unless the client has crashed, the client still
	// references the channel.
	//
	// Should the client have crashed then the server does not return the
	// channel to the channel pool while this task holds a reference to the
	// channel.
	//
	// So the channel can always be considered valid.

	channel_type& the_channel = channel_[the_channel_index];

	if (the_channel.in.try_pop_back(&the_chunk_index)) {
		// Notify the client that the channel::in buffer might not be full.
		the_channel.client()->notify();
		return 0;
	}

	return _E_WAIT_TIMEOUT;
}

void server_port::send_to_client(unsigned long the_channel_index,
chunk_index the_chunk_index) {
	// We assume that, unless the client has crashed, the client still
	// references the channel.
	//
	// Should the client have crashed then the server does not return the
	// channel to the channel pool while this task holds a reference to the
	// channel.
	//
	// So the channel can always be considered valid.
	
	// reference used as shorthand
	channel_type& the_channel = channel_[the_channel_index];
	
	// If the channels out_overflow queue is empty (assumed), then try to push to
	// the channel::out buffer. If that succeeds (assumed), return. If it fails, the
	// item is pushed to the out_overflow queue.
	// If the out_overflow queue is not empty (assumed), the item is pushed to the out_overflow
	// queue and then try to move the whole out_overflow queue to the out queue.
	// If the out_overflow queue is empty (assumed), the item is pushed to the out_overflow

	//	If out is full, the client may be dead - check if marked for clean-up?
	//	With the new infinite out_overflow queue per channel, I decide not to care about checking this.
	
	if (the_channel.out_overflow().empty()) {
		if (the_channel.out.try_push_front(the_chunk_index)) {
			// Successfully pushed the response message to the channel::out buffer.
			// Notify the client that the channel::out buffer might not be empty.
			the_channel.client()->notify();
			return;
		}
		else {
			// The channels out buffer is full. The message is pushed to this channels out_overflow queue instead.
			the_channel.out_overflow().push_back(the_chunk_index);
			return;
		}
	}
	else {
		// The out_overflow queue is not empty so the message is first pushed to
		// the out_overflow queue, to preserve the order of production.
		the_channel.out_overflow().push_back(the_chunk_index);

		// Try to move all items from the out_overflow queue to the out buffer.
		while (!the_channel.out_overflow().empty()) {
			if (!the_channel.out.try_push_front(the_channel
			.out_overflow().front())) {
				// Failed to push the item. Not removing it from
				// the out_overflow queue.
				return;
			}

			// The item was successfully pushed to the out buffer.
			// Removing the item from the out_overflow queue.
			the_channel.out_overflow().pop_front();

			// Notify that the out queue is not empty.
			the_channel.client()->notify();
		}

		// Dont check if (!the_channel.client()->get_owner_id().get_clean_up()) {}
		return;
	}
	
	// call client_interface[the_channel.client_number()].notify();
}

unsigned long server_port::send_task_to_scheduler(unsigned long port_number,
chunk_index the_chunk_index) {
	scheduler_channel_type& the_channel = scheduler_task_channel_[port_number];
	if (the_channel.in.try_push_front(the_chunk_index)) {
		// Get the work HANDLE...
		HANDLE work = 0; /// TEST COMPILE
		scheduler_interface_[port_number].notify(work);
		return 0;
	}
	return _E_INPUT_QUEUE_FULL;
}

unsigned long server_port::send_signal_to_scheduler(unsigned long port_number,
chunk_index the_chunk_index) {
	scheduler_channel_type& the_channel = scheduler_signal_channel_[port_number];
	if (the_channel.in.try_push_front(the_chunk_index)) {
		HANDLE work = 0; /// TEST COMPILE
		scheduler_interface_[port_number].notify(work);
		return 0;
	}
	return _E_INPUT_QUEUE_FULL;
}

void server_port::add_ref_to_channel(unsigned long the_channel_index)
{
	channel_type &channel = channel_[the_channel_index];
	int32_t new_server_refs = channel.add_server_ref();
	_ASSERT(new_server_refs >= 1);
}

void server_port::release_channel(unsigned long the_channel_index)
{
	channel_type &channel = channel_[the_channel_index];
	int32_t new_server_refs = channel.release_server_ref();
	_ASSERT(new_server_refs >= 0);
	
	if (!new_server_refs)
	{
		//if (!channel.client()->get_owner_id().get_clean_up()) {
		if (!channel.is_to_be_released()) {
			// No clean up job to do.
			return;
		}
		else {
			// The channel shall be released. It can be done from here also
			// because the scheduler won't probe the channel again after this.
			// It works because a scheduler thread is co-operative and cannot
			// pop from a channels in queue at the same time as it is here, etc.
		}
	}
}

void server_port::do_release_channel(channel_number the_channel_index) {
	channel_type& channel = channel_[the_channel_index];
	int32_t server_refs = channel.get_server_refs();
	chunk_index the_chunk_index;
	
	// TODO: If the client process terminated without unregistering, clean up
	// must be done by the database process. There are no more references to
	// the channel on the server so it is safe to release the channel.
	// But first the in and out queues must be emptied and any chunk in them
	// must first be released.
	client_interface_type* client_interface_ptr = channel.client();
	client_number the_client_number = channel.get_client_number();
	
	smp::spinlock::scoped_lock lock(client_interface_ptr->spinlock());

	// Clean up job to be done: Empty the in and out queues of this
	// channel and decrement the number of owned channels counter in
	// this client_interface, and if the counter reaches 0, this
	// scheduler do the job of releasing the resources in this
	// client_interface. This can be changed so that another thread
	// do this job instead and this scheduler move on an do its normal
	// tasks. Its a question of optimization.
	
	// Move all chunk indices from the channels in and out queues, and
	// the overflow_pool, to the bit bucket. These are copies and the
	// same indices are marked in the resource_map.
	
	///=========================================================================
	/// Empty the channels in and out queues.
	///=========================================================================

	// Empty the channels in queue.
	for (std::size_t i = 0; i < channel_capacity; ++i) {
		if (channel.in.try_pop_back(&the_chunk_index) == true) {
			// the_chunk_index is not released here, it is done later when
			// releasing chunks via the resource map.
			continue;
		}
		else {
			// Only reason it would not work is that it is empty.
			break;
		}
	}
	
	// Empty the channels out queue.
	for (std::size_t i = 0; i < channel_capacity; ++i) {
		if (channel.out.try_pop_back(&the_chunk_index) == true) {
			// the_chunk_index is not released here, it is done later when
			// releasing chunks via the resource map.
			continue;
		}
		else {
			// Only reason it would not work is that it is empty.
			break;
		}
	}
	
	// Remove chunk indices from the out_overflow queue in this channel.
	while (!channel.out_overflow().empty()) {
		// Removing the item from the out_overflow queue.
		channel.out_overflow().pop_front();
	}
	
	///=========================================================================
	/// Releases the channel making it available for any client to allocate.
	///=========================================================================
	
	_mm_mfence();
	_mm_lfence(); // Synchronizes instruction stream.
	channel.clear_is_to_be_released(); // Do it higher up in here?

	// Get the scheduler_number the channel is to be released via.
	uint32_t the_scheduler_number = client_interface_ptr->get_resource_map()
	.get_scheduler_number_for_owned_channel_number(the_channel_index);

	// Mark the channel as not owned.
	client_interface_ptr->clear_channel_flag(the_scheduler_number,
	the_channel_index);

	// It is of paramount importance that the above is done before the
	// channel is released. The fence forces this.
	_mm_mfence();
	_mm_lfence(); // Synchronizes instruction stream.
	
	// Release the channel.
	release_channel_number(the_channel_index, the_scheduler_number);
	_mm_mfence();
	_mm_lfence(); // Synchronizes instruction stream.
	
	// The channel has been released and can now be allocated by any client.
	
	uint32_t channels_left
	= client_interface_ptr->decrement_number_of_allocated_channels();
	
	if (channels_left == 0) {
		if (client_interface_ptr->get_owner_id().get_clean_up()) {
			// Is the client_interface marked for clean up?
#if defined (IPC_MONITOR_RELEASES_CHUNKS_DURING_CLEAN_UP)
			///=================================================================
			/// Notify the IPC monitor to release all chunks in this
			/// client_interface, making them available for anyone to allocate.
            ///=================================================================
            int32_t database_cleanup_index = client_interface_ptr->get_database_cleanup_index();
 
            if (database_cleanup_index != -1) {
				try {
					// Get monitor_interface_ptr for monitor_interface_name.
					monitor_interface_ptr the_monitor_interface
					(common_scheduler_interface_->monitor_interface_name());

					the_monitor_interface->set_cleanup_flag(database_cleanup_index,
					ipc_monitor_cleanup_event());
				}
				catch (...) {
					// OK, what to do? Failed to open the monitor_interface, chunks will not be
					// recovered (and leak.)
					std::cout << "error: failed to open the monitor_interface, chunks will not be recovered (and leak.)" << std::endl;
				}
			}
			else {
				std::cout << "error: no database_cleanup_index set." << std::endl;
			}
			
#else // !defined (IPC_MONITOR_RELEASES_CHUNKS_DURING_CLEAN_UP)
			///=================================================================
			/// Release all chunks in this client_interface, making them
			/// available for anyone to allocate.
			///=================================================================
			
			// Search through the overflow_pool and for each chunk that is
			// marked in the resource_map of this client, remove it. Otherwise
			// put it back into the overflow_pool. Not sure if this is needed.
			// Maybe tranquility means there is no chunks left, because they
			// were thrown away already. Should be the case, verify!
			
			// Now there shall not exist any more chunk indices around, except
			// those in the resource_map. Release them.
			
			//std::size_t chunks_flagged = client_interface_ptr
			//->get_resource_map().count_chunk_flags_set();
			
			bool release_chunk_result = release_clients_chunks
			(client_interface_ptr, 10000 /* milliseconds */);
			
			/// BUG!? Why not clear the resource map here?

			// Release the client_interface[the_client_number].
			client_interface_ptr->set_owner_id(owner_id::none);
			
#if defined (IPC_CLIENT_NUMBER_POOL_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
			bool release_client_number_res =
			common_client_interface_->release_client_number(the_client_number,
			client_interface_ptr, get_owner_id());
#else // !defined (IPC_CLIENT_NUMBER_POOL_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
			bool release_client_number_res =
			common_client_interface_->release_client_number(the_client_number,
			client_interface_ptr);
#endif // defined (IPC_CLIENT_NUMBER_POOL_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
			
			common_client_interface_->decrement_client_interfaces_to_clean_up();
			// Clean up done for client_interface[the_client_number].
#endif // defined (IPC_MONITOR_RELEASES_CHUNKS_DURING_CLEAN_UP)
		}
	}
}

void server_port::release_channel_marked_for_release(channel_number the_channel_index) {
	channel_type& channel = channel_[the_channel_index];
	int32_t server_refs = channel.get_server_refs();
	chunk_index the_chunk_index;
	
	// TODO: If the client process terminated without unregistering, clean up
	// must be done by the database process. There are no more references to
	// the channel on the server so it is safe to release the channel.
	// But first the in and out queues must be emptied and any chunk in them
	// must first be released.
	client_interface_type* client_interface_ptr = channel.client();
	client_number the_client_number = channel.get_client_number();
	
	//==========================================================================
	// Check if the client_interface's owner_id cleanup flag is set, in which
	// case return instantly because the cleanup will release all resources then
	// No:

	if (!client_interface_ptr->get_owner_id().get_clean_up()) {
		smp::spinlock::scoped_lock lock(client_interface_ptr->spinlock());

		// Clean up job to be done: Empty the in and out queues of this
		// channel and decrement the number of owned channels counter in
		// this client_interface, and if the counter reaches 0, this
		// scheduler do the job of releasing the resources in this
		// client_interface. This can be changed so that another thread
		// do this job instead and this scheduler move on an do its normal
		// tasks. Its a question of optimization.
	
		// Move all chunk indices from the channels in and out queues, and
		// the overflow_pool, to the bit bucket. These are copies and the
		// same indices are marked in the resource_map.
	
		///=========================================================================
		/// Empty the channels in and out queues.
		///=========================================================================

		// Empty the channels in queue.
		for (std::size_t i = 0; i < channel_capacity; ++i) {
			if (channel.in.try_pop_back(&the_chunk_index) == true) {
				// the_chunk_index is not released here, it is done later when
				// releasing chunks via the resource map.
				continue;
			}
			else {
				// Only reason it would not work is that it is empty.
				break;
			}
		}
	
		// Empty the channels out queue.
		for (std::size_t i = 0; i < channel_capacity; ++i) {
			if (channel.out.try_pop_back(&the_chunk_index) == true) {
				// the_chunk_index is not released here, it is done later when
				// releasing chunks via the resource map.
				continue;
			}
			else {
				// Only reason it would not work is that it is empty.
				break;
			}
		}
	
		///=========================================================================
		/// Remove chunk indices from the overflow_pool targeted for this channel.
		///=========================================================================

		// Remove chunk indices from the out_overflow queue in this channel.
		while (!channel.out_overflow().empty()) {
			// Removing the item from the out_overflow queue.
			channel.out_overflow().pop_front();
		}

		/// TODO: If the in queue was not empty at the moment this function was
		/// called, this is a bug because the scheduler have popped a message since
		/// then. This must be made impossible. Verify this.

		///=========================================================================
		/// Releases the channel making it available for any client to allocate.
		///=========================================================================
		_mm_mfence();
		_mm_sfence();
		channel.clear_is_to_be_released(); // Do it higher up in here?
		_mm_mfence();
		_mm_sfence();

		// Get the scheduler_number the channel is to be released via.
		uint32_t the_scheduler_number = client_interface_ptr->get_resource_map()
		.get_scheduler_number_for_owned_channel_number(the_channel_index);
		
		// Mark the channel as not owned.
		client_interface_ptr->clear_channel_flag(the_scheduler_number,
		the_channel_index);
	
		// It is of paramount importance that the above is done before the
		// channel is released. The fence forces this.
		_mm_mfence();
		_mm_lfence(); // Synchronizes instruction stream.
	
		// Release the channel.
		release_channel_number(the_channel_index, the_scheduler_number);
		_mm_mfence();
		_mm_lfence(); // Synchronizes instruction stream.
	
		uint32_t channels_left
		= client_interface_ptr->decrement_number_of_allocated_channels();
		// The channel has been released and can now be allocated by any client.
	}
}

std::size_t server_port::number_of_active_schedulers() {
	return common_scheduler_interface_->number_of_active_schedulers();
}

//------------------------------------------------------------------------------
inline HANDLE& server_port::open_ipc_monitor_cleanup_event() {
#if 0
	// Not checking if the event is already open.
	if ((ipc_monitor_cleanup_event() = ::OpenEvent(SYNCHRONIZE | EVENT_MODIFY_STATE,
	FALSE, common_scheduler_interface->ipc_monitor_cleanup_event_name())) == NULL) {
		// Failed to open the event.
		std::cout << "server_port::open_scheduler_number_pool_not_full_event(): "
		"Failed to open event. OS error: " << GetLastError() << "\n"; /// DEBUG
		return ipc_monitor_cleanup_event() = 0; // throw exception
	}
#endif
	return ipc_monitor_cleanup_event();
}

inline HANDLE& server_port::ipc_monitor_cleanup_event() {
	return this_scheduler_interface_->ipc_monitor_cleanup_event();
}

//------------------------------------------------------------------------------
inline bool server_port::acquire_linked_chunks(chunk_index& head, std::size_t
size, client_interface_type* client_interface_ptr, uint32_t
timeout_milliseconds) {
	return shared_chunk_pool_->acquire_linked_chunks(chunk_, head, size,
	client_interface_ptr, timeout_milliseconds);
}

inline bool server_port::release_linked_chunks(chunk_index& head,
client_interface_type* client_interface_ptr, uint32_t timeout_milliseconds) {
	return shared_chunk_pool_->release_linked_chunks(chunk_, head,
	client_interface_ptr, timeout_milliseconds);
}

//------------------------------------------------------------------------------
template<typename U>
inline std::size_t server_port::acquire_from_shared_to_private(U&
private_chunk_pool, std::size_t chunks_to_acquire, uint32_t
timeout_milliseconds) {
	return shared_chunk_pool_->acquire_to_chunk_pool(private_chunk_pool,
	chunks_to_acquire, timeout_milliseconds);
}

template<typename U>
inline std::size_t server_port::release_from_private_to_shared(U&
private_chunk_pool, std::size_t chunks_to_release, uint32_t
timeout_milliseconds) {
	return shared_chunk_pool_->release_from_chunk_pool(private_chunk_pool,
	chunks_to_release, timeout_milliseconds);
}

//------------------------------------------------------------------------------

#if 0 // OBSOLETE API
void server_port::acquire_chunk_index(unsigned long& the_chunk_index)
{
	chunk_index* ci = (chunk_index*)&the_chunk_index;
	
	if (shared_chunk_pool_->pop_back(ci)) {
		// Terminate the link in the acquired chunk.
		chunk_[*ci].terminate_link();
	}
}
#endif // OBSOLETE API

unsigned long server_port::acquire_linked_chunk_indexes(unsigned long channel_number, unsigned long start_chunk_index, unsigned long needed_size)
{
	channel_type& the_channel = channel_[channel_number];
	//uint64_t the_owner_id = the_channel.get_owner_id().get_owner_id();
	lldiv_t div_value = div((int64_t)needed_size, (int64_t)chunk_type::static_data_size);
	uint32_t needed_chunks = div_value.quot;
	if (div_value.rem != 0) needed_chunks++;

	uint8_t* current_chunk = (uint8_t*)&chunk_[start_chunk_index];
	chunk_index head;
	
	if (needed_chunks < a_bunch_of_chunks) {
try_to_acquire_from_private_chunk_pool:
		// Try to acquire the linked chunks from the private chunk_pool.
		
		if (this_scheduler_interface_->chunk_pool().acquire_linked_chunks
		(&chunk(0), head, needed_size, the_channel.client()) == true) {
			// Successfully acquired the linked chunks from the private
			// chunk_pool. Link chunk[start_chunk_index] to head.
			chunk(start_chunk_index).set_link(head);
			return 0;
		}
		else {
			// Failed to acquire the linked chunks from the private chunk_pool.
			// Try to move some chunks from the shared_chunk_pool to the private
			// chunk_pool.
			while (this_scheduler_interface_->chunk_pool().size()
			< needed_chunks) {
				// Failed to move enough chunks. Retry, potentially blocking
				// this thread forever. TODO: Consider returning with an error
				// code, or bool to indicate success/failure.
				std::size_t chunks_to_move = needed_chunks;
				
				// Make sure at least a_bunch_of_chunks is moved.
				if (chunks_to_move < a_bunch_of_chunks) {
					chunks_to_move = a_bunch_of_chunks;
				}
				
				shared_chunk_pool_->acquire_to_chunk_pool
				(this_scheduler_interface_->chunk_pool(), chunks_to_move,
				10000 /* timeout ms */);
			}
			
			// Successfully moved enough chunks to the private chunk_pool.
			// Retry acquire the linked chunks from there.
			goto try_to_acquire_from_private_chunk_pool;
		}
	}
	else {
		// Try to acquire the linked chunks from the shared_chunk_pool.
		
		while (!shared_chunk_pool_->acquire_linked_chunks(&chunk(0), head,
		needed_size, the_channel.client(), 10000 /* timeout ms */)) {

            // NOTE: Returning error immediately if chunks can't be obtained.
            return SCERRACQUIRELINKEDCHUNKS;

			// Failed to acquire the linked chunks from the shared_chunk_pool.
			// Retry, potentially blocking this thread forever. TODO: Consider
			// returning with an error code, or bool to indicate success/
			// failure.
		}
		
		// Successfully acquired the linked chunks from the shared_chunk_pool.
		// Link chunk[start_chunk_index] to head.
		chunk(start_chunk_index).set_link(head);
		return 0;
	}
}

unsigned long server_port::acquire_one_chunk(unsigned long channel_number, chunk_index* out_chunk_index)
{
    channel_type& the_channel = channel_[channel_number];

try_to_acquire_from_private_chunk_pool:
    // Try to acquire the linked chunks from the private chunk_pool.

    if (this_scheduler_interface_->chunk_pool().acquire_linked_chunks_counted
        (&chunk(0), *out_chunk_index, 1, the_channel.client()) == true)
    {
        // Successfully acquired the linked chunks from the private chunk_pool.
        return 0;
    }
    else
    {
        // Failed to acquire the linked chunks from the private chunk_pool.
        // Try to move some chunks from the shared_chunk_pool to the private
        // chunk_pool.
        shared_chunk_pool_->acquire_to_chunk_pool(
            this_scheduler_interface_->chunk_pool(), a_bunch_of_chunks,
			10000 /* timeout ms */);

        // Successfully moved enough chunks to the private chunk_pool.
        // Retry acquire the linked chunks from there.
        goto try_to_acquire_from_private_chunk_pool;
    }
}

unsigned long server_port::acquire_linked_chunk_indexes_counted(unsigned long channel_number, unsigned long start_chunk_index, unsigned long num_chunks)
{
	channel_type& the_channel = channel_[channel_number];
	chunk_index head;
	
	if (num_chunks < a_bunch_of_chunks) {
try_to_acquire_from_private_chunk_pool:
		// Try to acquire the linked chunks from the private chunk_pool.
		
		if (this_scheduler_interface_->chunk_pool().acquire_linked_chunks_counted
		(&chunk(0), head, num_chunks, the_channel.client()) == true) {
			// Successfully acquired the linked chunks from the private
			// chunk_pool. Link chunk[start_chunk_index] to head.
			chunk(start_chunk_index).set_link(head);
			return 0;
		}
		else {
			// Failed to acquire the linked chunks from the private chunk_pool.
			// Try to move some chunks from the shared_chunk_pool to the private
			// chunk_pool.
			while (this_scheduler_interface_->chunk_pool().size()
			< num_chunks) {
				// Failed to move enough chunks. Retry, potentially blocking
				// this thread forever. TODO: Consider returning with an error
				// code, or bool to indicate success/failure.
				std::size_t chunks_to_move = num_chunks;
				
				// Make sure at least a_bunch_of_chunks is moved.
				if (chunks_to_move < a_bunch_of_chunks) {
					chunks_to_move = a_bunch_of_chunks;
				}
				
				shared_chunk_pool_->acquire_to_chunk_pool
				(this_scheduler_interface_->chunk_pool(), chunks_to_move,
				10000 /* timeout ms */);
			}
			
			// Successfully moved enough chunks to the private chunk_pool.
			// Retry acquire the linked chunks from there.
			goto try_to_acquire_from_private_chunk_pool;
		}
	}
	else {
		// Try to acquire the linked chunks from the shared_chunk_pool.
		
		while (!shared_chunk_pool_->acquire_linked_chunks_counted(&chunk(0), head,
		num_chunks, the_channel.client(), 10000 /* timeout ms */)) {

            // NOTE: Returning error immediately if chunks can't be obtained.
            return SCERRACQUIRELINKEDCHUNKS;

			// Failed to acquire the linked chunks from the shared_chunk_pool.
			// Retry, potentially blocking this thread forever. TODO: Consider
			// returning with an error code, or bool to indicate success/
			// failure.
		}
		
		// Successfully acquired the linked chunks from the shared_chunk_pool.
		// Link chunk[start_chunk_index] to head.
		chunk(start_chunk_index).set_link(head);
		return 0;
	}
}

unsigned long server_port::release_linked_chunks(chunk_index start_chunk_index)
{
    // First releasing to private chunk pool.
    if (!this_scheduler_interface_->chunk_pool().release_linked_chunks(&chunk(0), start_chunk_index))
        return SCERRUNSPECIFIED;

    // Checking if we have more chunks in private pool then needed.
    int32_t num_to_return = this_scheduler_interface_->chunk_pool().size() - a_bunch_of_chunks;
    if (num_to_return > 0)
    {
        // Checking that number of returned chunks is correct.
        if (num_to_return != release_from_private_to_shared(this_scheduler_interface_->chunk_pool(), num_to_return))
            return SCERRUNSPECIFIED;
    }

    return 0;
}

bool server_port::release_clients_chunks(client_interface_type*
client_interface_ptr, uint32_t timeout_milliseconds) {
	return shared_chunk_pool_->release_clients_chunks(client_interface_ptr,
	timeout_milliseconds);
}

bool server_port::release_channel_number(channel_number the_channel_number,
scheduler_number the_scheduler_number, uint32_t spin_count,
uint32_t timeout_milliseconds) {
	// Clear the channel number flag so that the scheduler stops probing it.
	scheduler_interface_[the_scheduler_number]
	.clear_channel_number_flag(the_channel_number);
	
	_mm_mfence(); // Neccessary?

	// Set the pointers and indexes to 0.
	channel_type& the_channel = channel_[the_channel_number];
	the_channel.set_scheduler_interface(0);
	the_channel.set_scheduler_number(-1);
	the_channel.set_client_interface_as_qword(0);
	the_channel.set_client_number(-1);

	_mm_mfence(); // Neccessary?
	
	// Release the_channel_number.
	scheduler_interface_[the_scheduler_number]
	.push_front_channel_number(the_channel_number, get_owner_id());
	
	return true; /// TODO: Timeout, return false when not successfull.
}

inline chunk_type& server_port::chunk(chunk_index the_chunk_index) const {
	return chunk_[the_chunk_index];
}

inline void server_port::show_linked_chunks(chunk_index head) {
	shared_chunk_pool_->show_linked_chunks(chunk_, head);
}

} // namespace core
} // namespace starcounter

_STATIC_ASSERT(sizeof(starcounter::core::server_port) <= 128);

unsigned long server_initialize_port(void *port_mem128, const char *name, unsigned
long port_number, owner_id_value_type owner_id_value) {
	using namespace starcounter::core;
	server_port *the_port = new (port_mem128) server_port();
	return the_port->init(name, port_number,
	starcounter::core::owner_id(owner_id_value));
}

unsigned long server_get_next_signal_or_task(void *port, unsigned int
timeout_milliseconds, sc_io_event *pio_event) {
	using namespace starcounter::core;
	server_port *the_port = (server_port *)port;
	return the_port->get_next_signal_or_task(timeout_milliseconds, *pio_event);
}

unsigned long server_get_next_signal(void *port, unsigned int
timeout_milliseconds, unsigned long *pchunk_index) {
	using namespace starcounter::core;
	server_port *the_port = (server_port *)port;
	return the_port->get_next_signal(timeout_milliseconds, pchunk_index);
}

long server_has_task(void *port) {
	using namespace starcounter::core;
	server_port *the_port = (server_port *)port;
	return the_port->has_task();
}

unsigned long sc_try_receive_from_client(void *port, unsigned long
channel_index, unsigned long *pchunk_index) {
	using namespace starcounter::core;
	server_port *the_port = (server_port *)port;
	chunk_index the_chunk_index;
	unsigned long r = the_port->try_receive_from_client(channel_index,
	the_chunk_index);
	
	*pchunk_index = the_chunk_index;
	return r;
}

unsigned long sc_send_to_client(void *port, unsigned long channel_index,
unsigned long chunk_index) {
	//printf("%s\n", "sc_send_to_client"); /// debug
	using namespace starcounter::core;
	server_port *the_port = (server_port *)port;
	the_port->send_to_client(channel_index, chunk_index);
	
	// Notify the client.
	client_interface_type* the_client;
	
	if ((the_client = the_port->channel(channel_index).client()) != 0) {
		// The client may have set the channels client_ pointer to 0, but
		// the_client is a valid pointer (right? at least it can not be 0).
		// Another client may have acquired the channel and that this client is
		// now notified - that's ok.
		the_client->notify();
	}

	return 0;
}

unsigned long server_send_task_to_scheduler(void *port, unsigned long
port_number, unsigned long message) {
	using namespace starcounter::core;
	server_port *the_port = (server_port *)port;
	return the_port->send_task_to_scheduler(port_number, message);
}

unsigned long server_send_signal_to_scheduler(void *port, unsigned long
port_number, unsigned long message) {
	using namespace starcounter::core;
	server_port *the_port = (server_port *)port;
	return the_port->send_signal_to_scheduler(port_number, message);
}

void sc_add_ref_to_channel(void *port, unsigned long the_channel_index)
{
	using namespace starcounter::core;
	server_port *the_port = (server_port *)port;
	the_port->add_ref_to_channel(the_channel_index);
}

void sc_release_channel(void *port, unsigned long the_channel_index)
{
	using namespace starcounter::core;
	server_port *the_port = (server_port *)port;
	the_port->release_channel(the_channel_index);
}

#if 0
unsigned long sc_acquire_shared_memory_chunk(void *port, unsigned long channel_id, unsigned long *pchunk_index)
{
	using namespace starcounter::core;

	server_port* the_port = (server_port*)port;
	
	//the_port->acquire_chunk_index(*pchunk_index); // Obsolete API.

	// acquire_chunk_index() is an obsolete API. Use:
	//
	//	bool result = the_port->acquire_linked_chunks(chunk_index& head,
	//	std::size_t size, client_interface_type* client_interface_ptr,
	//	uint32_t timeout_milliseconds = 10000);
	//
	// if the number of chunks to allocate are, say, 64 or more. Experiment with
	// a suitable treshold.
	//
	// If need to allocate less chunks than the treshold (64, etc.), then
	// instead allocate the chunks from the private chunk_pool found in this
	// scheduler's scheduler_interface, which is much faster:
	//
	//	bool result =
	//	this_scheduler_interface_->chunk_pool().acquire_linked_chunks
	//	(&chunk(0), head, 65536 /*bytes*/, the_channel.client())
	//
	// If the private chunk_pool is empty, move chunks from the
	// shared_chunk_pool to the private chunk_pool with:
	//
	//	// Acquiring from shared_chunk_pool to private chunk_pool.
	//	shared_chunk_pool_->acquire_to_chunk_pool(this_scheduler_interface_->
	//	chunk_pool(), 256, 1000);
	//
	// See example how to: acquire_to_chunk_pool(), release_from_chunk_pool(),
	// acquire_linked_chunks(), and release_linked_chunks()
	// in server_port::send_to_client().
	
	// reference used as shorthand
	channel_type& the_channel = the_port->channel(channel_id);
	
	chunk_index head;

	// IMPORTANT NOTE: The caller must know which channel these chunks are
	// targeted for, because the chunks are marked as owned by the client that
	// owns the channel the chunks are targeted for. If channel_id is not the
	// channel that these chunks will be sent to, then that is a severe bug. 
	while (!the_port->acquire_linked_chunks(head, 1, the_channel.client())) {
		// Wait forever until acquiring a chunk, simulating the old code.
		// An alternative is to set the fourth argument timeout_milliseconds and
		// give up if failed to acquire the chunk(s).
	}
	
	*pchunk_index = head;
	return 0;
}
#endif

unsigned long sc_acquire_shared_memory_chunk(void *port, unsigned long channel_index, unsigned long *pchunk_index)
{
    using namespace starcounter::core;

    server_port* the_port = (server_port*)port;

    return the_port->acquire_one_chunk(channel_index, (chunk_index*)pchunk_index);
}

unsigned long sc_acquire_linked_shared_memory_chunks(void *port, unsigned long channel_index, unsigned long start_chunk_index, unsigned long needed_size)
{
	using namespace starcounter::core;
	
	server_port* the_port = (server_port*)port;
	
	return the_port->acquire_linked_chunk_indexes(channel_index, start_chunk_index, needed_size);
}

unsigned long sc_acquire_linked_shared_memory_chunks_counted(void *port, unsigned long channel_index, unsigned long start_chunk_index, unsigned long num_chunks)
{
    using namespace starcounter::core;

    server_port* the_port = (server_port*)port;

    return the_port->acquire_linked_chunk_indexes_counted(channel_index, start_chunk_index, num_chunks);
}

void *sc_get_shared_memory_chunk(void *port, unsigned long chunk_index)
{
	using namespace starcounter::core;

	server_port *the_port = (server_port *)port;

	return the_port->get_chunk(chunk_index);
}

unsigned long sc_release_linked_shared_memory_chunks(void *port, unsigned long start_chunk_index)
{
    using namespace starcounter::core;

    server_port* the_port = (server_port*)port;

    return the_port->release_linked_chunks(start_chunk_index);
}
