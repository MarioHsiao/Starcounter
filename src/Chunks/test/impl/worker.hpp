//
// impl/worker.hpp
// interprocess_communication/test
//
// Copyright © 2006-2012 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//
// Implementation of class worker.
//

#ifndef STARCOUNTER_INTERPROCESS_COMMUNICATION_IMPL_WORKER_HPP
#define STARCOUNTER_INTERPROCESS_COMMUNICATION_IMPL_WORKER_HPP

// Implementation

namespace starcounter {
namespace interprocess_communication {

using namespace starcounter::core;

worker::worker()
: state_(stopped),
shared_(),
worker_id_(0),
num_active_schedulers_(0),
chunk_pool_(chunks_total_number_max),
overflow_pool_(chunks_total_number_max),
num_channels_(0),
random_generator_(0 /*seed*/),
acquired_chunks_(0) {}

worker::~worker() {
	join();
	/// TODO: Release resources.
	state_ = stopped;
}

/// Start the worker.
void worker::start() {
	///=========================================================================
	/// Initialize
	///=========================================================================
	
	if (!shared().acquire_client_number()) {
		// Failed to acquire client number.
		throw worker_exception(4000);
	}
	
	// Acquire a channel_number for each scheduler.
	for (scheduler_number i = 0; i < num_active_schedulers_; ++i) {
		channel_[i] = invalid_channel_number;

		if (!shared().acquire_channel(&channel_[i], i /*scheduler_number*/)) {
			std::cout << " worker[" << worker_id_ << "] error: "
			"invalid channel number." << std::endl;
		}
		
		++num_channels_;
	}
	
	channel_[num_channels_] = invalid_channel_number;
	
	///=========================================================================
	/// Start worker thread
	///=========================================================================

	thread_ = boost::thread(&worker::work, this); 
	thread_handle_ = thread_.native_handle();	
}

inline worker::state worker::get_state() {
	return state_;
}

inline void worker::set_state(state s) {
	InterlockedExchange((LONG*) &state_, s);
}

inline bool worker::is_running() {
	return get_state() == running;
}

void worker::work()
try {
	chunk_index request_chunk_index;
	chunk_index response_chunk_index;
	uint32_t scan_out_buffers = 0;

	// worked indicates if this worker did some work during the last iteration,
	// scanning the set of channels to push or pop chunks, etc.
	bool worked = false;
	uint64_t scan_counter = scan_counter_preset;

	///=========================================================================
	/// Worker loop
	///=========================================================================
	
	set_state(running);
	
	while (is_running()) {
		///=====================================================================
		/// For each channel, try to send a BMX ping message.
		///=====================================================================

		for (std::size_t n = 0; n < num_channels_; ++n) {
			///=================================================================
			/// Check if there are any chunks in the overflow_pool_ and try to
			/// push those chunks first.
			///=================================================================
			
			if (overflow_pool_.empty()) {
				goto acquire_chunk_from_private_chunk_pool;
			}
			else {
				///=============================================================
				/// Try to push all messages currently in the overflow_pool_.
				///=============================================================
				
				uint32_t chunk_index_and_channel; // This type must be uint32_t.
				std::size_t current_overflow_size = overflow_pool_.size();
				
				// Try to empty the overflow buffer, but only those elements
				// that are currently in the buffer. Those that fail to be
				// pushed are put back in the buffer and those are attempted to
				// be pushed the next time around.
				for (std::size_t i = 0; i < current_overflow_size; ++i) {
					overflow_pool_.pop_back(&chunk_index_and_channel);
					request_chunk_index = chunk_index_and_channel & 0xFFFFFFUL;
					uint32_t ch = (chunk_index_and_channel >> 24) & 0xFFUL;
					
					// Try to push the request_chunk_index via channel ch.
					
					channel_type& the_channel = shared().channel(ch);

					if (the_channel.in.try_push_front(request_chunk_index)
					== true) {
						// Successfully pushed the chunk_index. Notify the
						// scheduler.
						worked = true;
#if defined(INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC) // Use Windows Events.
						the_channel.scheduler()->notify(shared()
						.scheduler_work_event(the_channel
						.get_scheduler_number()));
#else // !defined(INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC) // Use Boost.Interprocess.
						the_channel.scheduler()->notify();
#endif // defined(INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC) // Use Windows Events.
					}
					else {
						// Could not push the request message to the channels in
						// queue - push it back to the overflow_pool_ instead.
						overflow_pool_.push_front(channel_[n] << 24
						| request_chunk_index);
					}
				}
			}
			
acquire_chunk_from_private_chunk_pool:
			///=================================================================
			/// Try to get a chunk from the private chunk_pool_.
			///=================================================================
			
			if (chunk_pool_.acquire_linked_chunks(&shared().chunk(0),
			request_chunk_index, 1) == true) {
				///=============================================================
				/// Got a chunk. Writing a BMX ping message into it.
				///=============================================================
				
				// Constructing the BMX Ping chunk.
		        shared_memory_chunk* smc = static_cast<shared_memory_chunk*>
				(&shared().chunk(request_chunk_index));
				
				sc_bmx_construct_ping(0, smc);
				
				// Reference used as shorthand.
				channel_type& the_channel = shared().channel(channel_[n]);
				
				// Send the request to the database.
				if (the_channel.in.try_push_front(request_chunk_index) == true)
				{
					// Successfully pushed the chunk_index. Notify the
					// scheduler.
					worked = true;
#if defined(INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC) // Use Windows Events.
					the_channel.scheduler()->notify(shared()
					.scheduler_work_event(the_channel.get_scheduler_number()));
#else // !defined(INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC) // Use Boost.Interprocess.
					the_channel.scheduler()->notify();
#endif // defined(INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC) // Use Windows Events.
				}
				else {
					// Could not push the request to the channels in
					// queue - push it to the overflow_pool_ instead.
					overflow_pool_.push_front(channel_[n] << 24
					| request_chunk_index);
				}
			}
			else {
				// The private chunk_pool_ is empty.
				
				// If this worker have not allocated max_chunks, acquire
				// chunks_to_move chunks from the shared_chunk_pool and move
				// them to the private chunk_pool_.
				if (acquired_chunks_ < worker::max_chunks) {
					acquired_chunks_ += shared().acquire_from_shared_to_private
					(chunk_pool_, a_bunch_of_chunks,
					&shared().client_interface(), 1000);
					
					std::size_t chunks_flagged = shared().client_interface()
					.get_resource_map().count_chunk_flags_set();
					
					// If the worker has some chunks, retry:
					if (acquired_chunks_) {
						goto acquire_chunk_from_private_chunk_pool;
					}
				}
				else {
					// The worker have allocated max_chunks to its private
					// chunk_pool_ and must do something else.
					
					// Continue probing channels for response messages. This is
					// just an example of what the worker can do when waiting.
					break;
					
					// The database schedulers process chunks and send them
					// back so the worker can re-use them after having processed
					// the response messages.
				}
			}
		}

		///=====================================================================
		/// Round-robin check all channels and see if there are any response
		/// messages and process them.
		///=====================================================================

		//if ((++scan_out_buffers & 127) == 0) { // Slow down scanning of out buffers
			for (std::size_t n = 0; n < num_channels_; ++n) {
				channel_type& the_channel = shared().channel(channel_[n]);
				// Check if there is a message and process it.
				if (the_channel.out.try_pop_back(&response_chunk_index) == true) {
					// A message on channel ch was received. Notify the database
					// that the out queue in this channel is not full.
					worked = true;
	#if defined(INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC) // Use Windows Events.
					/// No reason, nobody waits!
					//the_channel.scheduler()->notify(shared().scheduler_work_event
					//(the_channel.get_scheduler_number()));
	#else // !defined(INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC) // Use Boost.Interprocess.
					//the_channel.scheduler()->notify();
	#endif // defined(INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC) // Use Windows Events.
					
					///=============================================================
					/// Handle all responses in this chunk.
					///=============================================================
					//shared().chunk(response_chunk_index).set_next(0); /// DEBUG: Set next link to 0.

					shared_memory_chunk* smc = (shared_memory_chunk*) &(shared()
					.chunk(response_chunk_index));
					
					uint64_t ping_data;
					uint32_t error_code = sc_bmx_parse_pong(smc, &ping_data);
					
					if (error_code == 0) {
						// Successfully processed the response.
					}
					else {
						// An error occurred.
						std::cout << "worker[" << worker_id_ << "] error: "
						"handle_responses() failed with the error code "
						<< error_code << std::endl;
					}
					
					// Release the chunk.
					chunk_pool_.release_linked_chunks(&shared().chunk(0),
					response_chunk_index);
					if (chunk_pool_.size() <= max_chunks) {
						continue;
					}
					else {
						// The chunk_pool_ has more chunks than allowed, time to
						// release some chunks.
						std::size_t chunks_to_move = chunk_pool_.size() -max_chunks;
						
						if (chunks_to_move < a_bunch_of_chunks) {
							chunks_to_move = a_bunch_of_chunks;
						}
					
						acquired_chunks_ -= shared().release_from_private_to_shared
						(chunk_pool_, chunks_to_move, &shared().client_interface(),
						1000);
					}
				}
			}
		//} // Slow down scanning of out buffers
		
		// Check if this worker wait for work. Assuming not.
		if (worked) {
			// Did some work (pushing or popping, etc.) during last scan.
			scan_counter = scan_counter_preset;
			worked = false;
			continue;
		}
		else {
			// Did not find work to do during last scan.
			if (scan_counter > 1) {
				--scan_counter;
				_mm_pause();
				continue;
			}
			else if (scan_counter == 1) {
				// Preparing to wait.
				shared().client_interface().set_notify_flag(true);
				// Therefore need to scan one more time, just in case a
				// scheduler read the notify flag as false just before it was
				// set, and pushed or popped on any channel without notifying.
				--scan_counter;
				_mm_pause();
				continue;
			}
			else if (scan_counter == 0) {
				// Nothing was pushed or popped for scan_count_reset number of
				// iterations. This thread will now wait for any scheduler to
				// push or pop on any of this worker's channels.
#if defined(INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC) // Use Windows Events.
				if (shared().client_interface().wait_for_work
				(shared().client_work_event(), wait_for_work_milli_seconds)
				== true) {
					// This worker was notified there is work to do.
					shared().client_interface().set_notify_flag(false);
					scan_counter = scan_counter_preset;
				}
				else {
					// A timeout occurred.
					shared().client_interface().set_notify_flag(false);
					scan_counter = scan_counter_preset; // or 1?
				}
#else // !defined(INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC) // Use Boost.Interprocess.
				if (shared().client_interface().wait_for_work
				(wait_for_work_milli_seconds) == true) {
					// This worker was notified there is work to do.
					shared().client_interface().set_notify_flag(false);
					scan_counter = scan_counter_preset;
				}
				else {
					// A timeout occurred.
					shared().client_interface().set_notify_flag(false);
					scan_counter = scan_counter_preset; // or 1?
				}
#endif // defined(INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC) // Use Windows Events.
			}
		}
	}
	
	// Call this before exit is called for the thread.
	release_all_resources();
	
	/// Exit thread.
	return;
}
catch (starcounter::interprocess_communication::worker_exception& e) {
	std::cout << " worker[" << worker_id_ << "] error: worker exception "
	<< "caught with error code " << e.error_code() << std::endl;
}

void worker::join() {
	thread_.join();
}

inline worker& worker::set_worker_number(std::size_t n) {
	worker_id_ = n;
	return *this;
}

inline worker& worker::set_active_schedulers(std::size_t n) {
	num_active_schedulers_ = n;
	return *this;
}

inline worker& worker::set_shared_interface() {
	_mm_mfence();
	shared().init(segment_name_, monitor_interface_name_, pid_, owner_id_);
	_mm_mfence();
	return *this;
}

inline worker& worker::set_segment_name(const std::string& segment_name) {
	segment_name_ = segment_name;
	return *this;
}

inline std::string worker::get_segment_name() const {
	return segment_name_;
}

inline worker& worker::set_monitor_interface_name(const std::string& monitor_interface_name) {
	monitor_interface_name_ = monitor_interface_name;
	return *this;
}

inline std::string worker::get_monitor_interface_name() const {
	return monitor_interface_name_;
}

inline worker& worker::set_pid(const pid_type pid) {
	pid_ = pid;
	return *this;
}

inline pid_type worker::get_pid() const {
	return pid_;
}

inline worker& worker::set_owner_id(const owner_id oid) {
	owner_id_ = oid;
	return *this;
}

inline owner_id worker::get_owner_id() const {
	return owner_id_;
}

inline void worker::show_linked_chunks(chunk_type* chunk_base, chunk_index head)
{
	for (chunk_index current = head; current != chunk_type::link_terminator;
	current = chunk_base[current].get_link()) {
		chunk_index next = chunk_base[current].get_link();
		std::cout << "chunk[" << current << "] links to chunk[" << next
		<< "]" << std::endl;
	}
	std::cout << "end" << std::endl;
}

#if 0 // NOT COMPLETE
inline void worker::release_all_resources() {
	///-------------------------------------------------------------------------
	for (std::size_t n = 0; n < num_channels_; ++n) {
		channel_type& the_channel = shared().channel(channel_[n]);
		the_channel.set_to_be_released();
		the_channel.scheduler()->notify();
	}
	///-------------------------------------------------------------------------
	shared().common_client_interface().increment_client_interfaces_to_clean_up();
	// I think it is important that the increment above is done before
	// marking for clean up below. Not sure about the order, the monitor does it
	// in this order.
	///-------------------------------------------------------------------------
	_mm_mfence();
	shared().client_interface().get_owner_id().mark_for_clean_up();
}
#endif // NOT COMPLETE

inline void worker::release_all_resources() {
	return; // Let the IPC monitor start the cleanup instead.

	shared().common_client_interface().increment_client_interfaces_to_clean_up();
	
	// I think it is important that the increment above is done before marking
	// for clean up below.
	_mm_mfence();
	
	// Mark the client_interface for clean up.
	shared().client_interface().get_owner_id().mark_for_clean_up();
	
	// For each of the channels the client thread own, try to notify the
	// scheduler(s) via those channels.
	
	// For each mask word, bitscan to find the channel indices.
	for (uint32_t ch_index = 0; ch_index < resource_map::channels_mask_size;
	++ch_index) {
		for (resource_map::mask_type mask = shared().client_interface()
		.get_resource_map().get_owned_channels_mask(ch_index); mask;
		mask &= mask -1) {
			uint32_t ch = bit_scan_forward(mask);
			ch += ch_index << resource_map::shift_bits_in_mask_type;
			channel_type& the_channel = shared().channel(ch);
			scheduler_number the_scheduler_number = the_channel
			.get_scheduler_number();
			
			/// TODO: Check if the_scheduler_number is out of range!!!
			
			scheduler_interface_type* scheduler_interface_ptr = &shared()
			.scheduler_interface(the_scheduler_number);
			
			// A fence is needed so that all accesses to the channel is
			// completed when marking it to be released.
			_mm_mfence();
			
			// Mark channel to be released. After this the channel cannot be
			// accessed.
			the_channel.set_to_be_released();
			
			if (scheduler_interface_ptr) {
				// The scheduler may be waiting so notify it.
				if ((scheduler_interface_ptr->notify_scheduler_to_do_clean_up
				(shared().scheduler_work_event(the_channel.get_scheduler_number())))
				== true) {
					// Succeessfully notified the scheduler on this channel.
				}
				else { /// REMOVE THIS DEBUG TEST
					//std::cout << " notify_scheduler_to_do_clean_up() "
					//"failed in worker::release_all_resources().\n"; /// DEBUG
				}
			}
		}
	}
}

} // namespace interprocess_communication
} // namespace starcounter

#endif // STARCOUNTER_INTERPROCESS_COMMUNICATION_IMPL_WORKER_HPP
