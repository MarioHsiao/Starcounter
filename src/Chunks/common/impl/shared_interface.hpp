//
// impl/shared_interface.hpp
//
// Copyright © 2006-2012 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//
// Implementation of class shared_interface.
//

#ifndef STARCOUNTER_CORE_IMPL_SHARED_INTERFACE_HPP
#define STARCOUNTER_CORE_IMPL_SHARED_INTERFACE_HPP

// Implementation

namespace starcounter {
namespace core {

inline shared_interface::shared_interface()
: segment_name_(),
monitor_interface_name_(),
chunk_(),
shared_chunk_pool_(),
common_scheduler_interface_(),
scheduler_interface_(),
common_client_interface_(),
client_interface_(),
channel_(),
client_number_(no_client_number),
owner_id_(0),
pid_(0) {}

inline shared_interface::shared_interface(std::string segment_name, std::string
monitor_interface_name, pid_type pid, owner_id oid)
: segment_name_(segment_name),
monitor_interface_name_(monitor_interface_name),
chunk_(),
shared_chunk_pool_(),
common_scheduler_interface_(),
scheduler_interface_(),
common_client_interface_(),
client_interface_(),
channel_(),
client_number_(no_client_number),
owner_id_(oid),
pid_(pid) {
	init(segment_name, monitor_interface_name, pid, oid);
}

inline void shared_interface::init(std::string segment_name, std::string
monitor_interface_name, pid_type pid, owner_id oid) {
	segment_name_ = segment_name;
	monitor_interface_name_ = monitor_interface_name;
	owner_id_ = oid;
	pid_ = pid;
	
	// Open the managed segment with the segment_name, which has the format:
	// <DATABASE_NAME_PREFIX>_<DATABASE_NAME>_<SEQUENCE_NUMBER>.
	segment_.init_open(segment_name.c_str());
	
	// Check if it is valid.
	if (segment_.is_valid()) {
		mapped_region_.init(segment_);
		if (mapped_region_.is_valid()) {
			// Found the shared memory segment. Initialize pointers.
			init();
		}
	}
	else {
		// Invalid segment. The shared memory segment probably don't exist.
		throw shared_interface_exception(999L); /// TODO: return a suitable error code
	}
}

inline std::string shared_interface::get_segment_name() const {
	return segment_name_;
}

inline std::string shared_interface::get_monitor_interface_name() const {
	return monitor_interface_name_;
}

//------------------------------------------------------------------------------
inline bool shared_interface::acquire_client_number(uint32_t spin_count,
uint32_t timeout_milliseconds) {
	common_client_interface_type::value_type v;
	bool r = common_client_interface_->acquire_client_number(&v,
	client_interface_, owner_id_, spin_count, timeout_milliseconds);
	client_number_ = v;
	return r;
}

inline bool shared_interface::release_client_number(uint32_t spin_count,
uint32_t timeout_milliseconds) {
	if ((common_client_interface_->release_client_number(client_number_,
	client_interface_, spin_count, timeout_milliseconds)) == true) {
		client_number_ = no_client_number;
		// Successfully released the client_number.
		return true;
	}
	else {
		// Failed to release the client_number, probably because a timeout
		// occurred. If a client fails to release a shared resource, the client
		// shall probably not unregister with the monitor, and instead rely on
		// that the monitor and schedulers clean up properly after the client.
		// It is probably the best strategy.

		// Flag that not all resources have been released can be tested later
		// when it is time to unregister with the monitor. TODO: Implement this:
		//failed_to_release_a_resource = true;
		return false;
	}
}

//------------------------------------------------------------------------------
inline bool shared_interface::acquire_channel(channel_number*
the_channel_number, scheduler_number the_scheduler_number, uint32_t spin_count,
uint32_t timeout_milliseconds) {
	channel_number temp_channel_number;
	
	// Pop a channel number.
	scheduler_interface_[the_scheduler_number]
	.pop_back_channel_number(&temp_channel_number);
	
	// Mark this channel as owned by this client.
	client_interface().set_channel_flag(the_scheduler_number,
	temp_channel_number);
	
	// The number of owned channels counter is incremented.
	client_interface().increment_number_of_allocated_channels();
	
	// Set index to the scheduler_interface.
	channel_[temp_channel_number].set_scheduler_number(the_scheduler_number);
	
	// Set pointer to the scheduler_interface.
	channel_[temp_channel_number].set_scheduler_interface
	(&scheduler_interface_[the_scheduler_number]);
	
	// Set index to the client_interface.
	channel_[temp_channel_number].set_client_number(client_number_);
	
	// Set pointer to the client_interface.
	channel_[temp_channel_number].set_client_interface_as_qword
	(scheduler_interface_[the_scheduler_number].get_client_interface_as_qword()
	+(client_number_ * sizeof(client_interface_type)));
	
	// Set the channel number flag after having set pointers to the
	// scheduler_interface and the client_interface.
	scheduler_interface_[the_scheduler_number]
	.set_channel_number_flag(temp_channel_number);
	
	*the_channel_number = temp_channel_number;
	
	return true; /// TODO: Timeout doesn't work yet.
}

inline void shared_interface::release_channel(channel_number the_channel_number)
{
	scheduler_interface_type* the_scheduler
	= channel_[the_channel_number].scheduler();
	
	// Force the load of the pointer to the scheduler before it is released.
	_mm_lfence();
	
	/// Mark the channel to be released.
	channel_[the_channel_number].set_to_be_released();
	
	/// Notify the scheduler. It may happen that the scheduler has already
	/// released the channel now, in which case the notification do no harm
	/// because it notifies an existing scheduler. Once a valid pointer to a
	/// scheduler have been obtained, it can always be used since a scheduler
	/// can not quit.
	the_scheduler->notify();
}

//------------------------------------------------------------------------------
							// OBSOLETE FUNCTIONS. TODO: Replace them with new API functions.
							#if 0
							/// This function is obsolete and replaced by acquire_linked_chunks().
							inline bool shared_interface::acquire_chunk_index(chunk_index* the_chunk_index,
							uint32_t spin_count, uint32_t timeout_milliseconds) {
								// Wrapping the new API here, so that clean up works if this obsolete API is used.
								return shared_chunk_pool_->acquire_linked_chunks(chunk_, *the_chunk_index, 1,
								client_interface_, timeout_milliseconds);
							}
							///// This function is obsolete and replaced by release_linked_chunks().
							inline bool shared_interface::release_chunk_index(chunk_index the_chunk_index,
							uint32_t spin_count, uint32_t timeout_milliseconds) {
								// Wrapping the new API here, so that clean up works if this obsolete API is used.
								return shared_chunk_pool_->release_linked_chunks(chunk_, the_chunk_index,
								client_interface_, timeout_milliseconds);
							}
							#endif

//------------------------------------------------------------------------------
																				/// THESE ARE OBSOLETE SINCE SCHEDULERS USE server_port, not shared_interface.

																				// TODO: Move to server_port.
																				#if 0
																				inline bool shared_interface::release_linked_chunks(chunk_index& head, uint32_t
																				timeout_milliseconds) {
																					return shared_chunk_pool_->release_linked_chunks(chunk_, head,
																					timeout_milliseconds);
																				}
																				#endif

//------------------------------------------------------------------------------
// TODO: Rename to acquire_linked_chunks()
inline bool shared_interface::client_acquire_linked_chunks(chunk_index& head,
std::size_t size, uint32_t timeout_milliseconds) {
	return shared_chunk_pool_->acquire_linked_chunks(chunk_, head, size,
	client_interface_, timeout_milliseconds);
}

// TODO: Rename to release_linked_chunks()
inline bool shared_interface::client_release_linked_chunks(chunk_index& head,
uint32_t timeout_milliseconds) {
	return shared_chunk_pool_->release_linked_chunks(chunk_, head,
	client_interface_, timeout_milliseconds);
}

//------------------------------------------------------------------------------
template<typename U>
inline std::size_t shared_interface::acquire_from_shared_to_private(U&
private_chunk_pool, std::size_t chunks_to_acquire, client_interface_type*
client_interface_ptr, uint32_t timeout_milliseconds) {
	return shared_chunk_pool_->acquire_to_chunk_pool(private_chunk_pool,
	chunks_to_acquire, client_interface_ptr, timeout_milliseconds);
}

template<typename U>
inline std::size_t shared_interface::release_from_private_to_shared(U&
private_chunk_pool, std::size_t chunks_to_release, client_interface_type*
client_interface_ptr, uint32_t timeout_milliseconds) {
	return shared_chunk_pool_->release_from_chunk_pool(private_chunk_pool,
	chunks_to_release, client_interface_ptr, timeout_milliseconds);
}

//------------------------------------------------------------------------------
inline void shared_interface::show_linked_chunks(chunk_index head) {
	shared_chunk_pool_->show_linked_chunks(chunk_, head);
}

inline shared_chunk_pool_type& shared_interface::shared_chunk_pool() const {
	return *shared_chunk_pool_;
}

inline chunk_type& shared_interface::chunk(std::size_t n) const {
	return chunk_[n];
}

template<typename Ptr>
inline chunk_index shared_interface::get_index_from_pointer(Ptr ptr) const {
	return std::size_t(((char*) ptr) -((char*) chunk_)) / chunk_type::size();
}

inline common_scheduler_interface_type&
shared_interface::common_scheduler_interface() const {
	return *common_scheduler_interface_;
}

inline scheduler_interface_type&
shared_interface::scheduler_interface(std::size_t n) const {
	return scheduler_interface_[n];
}

inline common_client_interface_type&
shared_interface::common_client_interface() const {
	return *common_client_interface_;
}

inline client_interface_type&
shared_interface::client_interface(std::size_t n) const {
	return client_interface_[n];
}

inline client_interface_type& shared_interface::client_interface() const {
	return client_interface_[client_number_];
}

inline channel_type& shared_interface::channel(std::size_t n) const {
	return channel_[n];
}

inline void
shared_interface::database_state(common_client_interface_type::state s) {
	common_client_interface_->database_state(s);
}

inline common_client_interface_type::state
shared_interface::database_state() const {
	return common_client_interface_->database_state();
}

inline void shared_interface::init() {
	simple_shared_memory_manager* pm = new(mapped_region_.get_address())
	simple_shared_memory_manager;
	
	// Try to find the shared memory objects.
	
	// Find the chunks.
	chunk_ = (chunk_type*) pm->find_named_block
	(starcounter_core_shared_memory_chunks_name);
	
	if (!chunk_) {
		// error: could not find the chunks
		throw shared_interface_exception(999L); /// TODO: return suitable error code
	}
	
	// Find the shared_chunk_pool.
	shared_chunk_pool_ = (shared_chunk_pool_type*) pm->find_named_block
	(starcounter_core_shared_memory_shared_chunk_pool_name);
	
	if (!shared_chunk_pool_) {
		// error: could not find the shared_chunk_pool
		throw shared_interface_exception(999L); /// TODO: return suitable error code
	}
	
	// Find the common_scheduler_interface.
	common_scheduler_interface_ = (common_scheduler_interface_type*)
	pm->find_named_block
	(starcounter_core_shared_memory_common_scheduler_interface_name);
	
	if (!common_scheduler_interface_) {
		// error: could not find the common_scheduler_interface
		throw shared_interface_exception(999L); /// TODO: return suitable error code
	}
	
	// Find the scheduler_interfaces.
	scheduler_interface_ = (scheduler_interface_type*) pm->find_named_block
	(starcounter_core_shared_memory_scheduler_interfaces_name);
	
	if (!scheduler_interface_) {
		// Did not find the scheduler_interfaces.
		throw shared_interface_exception(999L); /// TODO: return suitable error code
	}
	
	// Find the common_client_interface.
	common_client_interface_ = (common_client_interface_type*)
	pm->find_named_block
	(starcounter_core_shared_memory_common_client_interface_name);
	
	if (!common_client_interface_) {
		// Did not find the common_client_interface.
		throw shared_interface_exception(999L); /// TODO: return suitable error code
	}
	
	// Find the client_interfaces.
	client_interface_ = (client_interface_type*) pm->find_named_block
	(starcounter_core_shared_memory_client_interfaces_name);
	
	if (!client_interface_) {
		// Did not find the client_interfaces.
		throw shared_interface_exception(999L); /// TODO: return suitable error code
	}
	
	// Find the channels.
	channel_ = (channel_type*)
	pm->find_named_block(starcounter_core_shared_memory_channels_name);
	
	if (!channel_) {
		// error: could not find the channels
		throw shared_interface_exception(999L); /// TODO: return suitable error code
	}
}

inline pid_type shared_interface::get_pid() const {
	return pid_;
}

inline owner_id shared_interface::get_owner_id() const {
	return owner_id_;
}

inline client_number shared_interface::get_client_number() const {
	return client_number_;
}

inline uint32_t shared_interface::send_to_server_and_wait_response(uint32_t ch,
uint32_t request, uint32_t& response, uint32_t spin, uint32_t timeout) {
	// Get a reference to the channel.
	channel_type& the_channel = channel(ch);
	
	// Before starting to spin, check the state of the database. Assuming the
	// state is normal.
	if (common_client_interface().database_state()
	== common_client_interface_type::normal) {
push_request_message_with_spin: /// The notify flag could be true...
		// Push the message to the channels in queue, retry spin_count times.
		if (the_channel.in.push_front(request, spin) == true) {
			// Successfully pushed the chunk_index. Notify the scheduler.
			the_channel.scheduler()->notify();
		}
		else {
			// Could not push the request message to the channels in queue while
			// spinning. Preparing to wait. . .
			client_interface_type& this_client_interface
			= client_interface(client_number_);
			
			this_client_interface.set_notify_flag(true);
			
			while (the_channel.in.try_push_front(request) == false) {
				// Check the state of the database.
				switch (common_client_interface().database_state()) {
				case common_client_interface_type::normal:
					// The server state is normal. Wait until the request
					// message can be pushed. . .the in queue is full.
					if (this_client_interface.wait_for_work(timeout)) {
						// The scheduler or monitor notified the client.
						///this_client_interface.set_notify_flag(false);
						goto push_request_message_with_spin;
					}
					else {
						// Timeout.
						return SCERRCTIMEOUTPUSHREQUESTMESSAGE;
					}
					break;
				case common_client_interface_type
				::database_terminated_gracefully:
					return SCERRDBTERMINATEDGRACEFULLY;
				case common_client_interface_type
				::database_terminated_unexpectedly:
					return SCERRDBTERMINATEDUNEXPECTEDLY;
				default: // Unknown server state.
					return SCERRUNKNOWNDBSTATE;
				}
			}
			
			// Successfully pushed the request message on the channels in queue.
			this_client_interface.set_notify_flag(false);
		}
		
pop_response_message_with_spin: /// The notify flag could be true
		// Pop a response message from the channels out queue, retry spin_count
		// times.
		if (the_channel.out.pop_back(&response, spin) == true) {
			// Successfully popped response. Notify the scheduler.
			the_channel.scheduler()->notify();
		}
		else {
			client_interface_type& this_client_interface
			= client_interface(client_number_);
			
			// Could not pop a response message from the channels out queue
			// while spinning. Preparing to wait. . .
			this_client_interface.set_notify_flag(true);
			
			while (the_channel.out.try_pop_back(&response) == false) {
				// Check the state of the database.
				switch (common_client_interface().database_state()) {
				case common_client_interface_type::normal:
					// The server state is normal. Wait until a response message
					// can be popped. . .the out queue is empty.
					if (this_client_interface.wait_for_work(timeout)) {
						// The scheduler or monitor notified the client.
						///this_client_interface.set_notify_flag(false); /// was commented
						goto pop_response_message_with_spin;
					}
					else {
						// Timeout.
						return SCERRCTIMEOUTPOPRESPONSEMESSAGE;
					}
					break;
				case common_client_interface_type
				::database_terminated_gracefully:
					return SCERRDBTERMINATEDGRACEFULLY;
				case common_client_interface_type
				::database_terminated_unexpectedly:
					return SCERRDBTERMINATEDUNEXPECTEDLY;
				default: // Unknown database state.
					return SCERRUNKNOWNDBSTATE;
				}
			}
			// Successfully popped the request message from the channels out
			// queue.
			this_client_interface.set_notify_flag(false);
		}
	}
	else {
		// The database state is not normal. Check the state of the database.
		switch (common_client_interface().database_state()) {
		case common_client_interface_type::normal:
			/// Not expected. The state was not normal, but now all of a sudden
			/// it is normal again. Return error code or go on? I just go on
			/// for now but TODO: return error code.
			goto push_request_message_with_spin;
		case common_client_interface_type
		::database_terminated_gracefully:
			return SCERRDBTERMINATEDGRACEFULLY;
		case common_client_interface_type
		::database_terminated_unexpectedly:
			return SCERRDBTERMINATEDUNEXPECTEDLY;
		default: // Unknown database state.
			return SCERRUNKNOWNDBSTATE;
		}
	}
	
	// Successfully pushed and popped.
	/// The notify flag could be true, but it should be false! This hurts performance a lot.
	return 0;
}

																				#if 0
																				uint32_t shared_interface::try_send(uint32_t ch, uint32_t request,
																				uint32_t spin) {
																					// Get a reference to the channel.
																					channel_type& the_channel = channel(ch);
																					
																				push_request_message_with_spin:
																					// Push the message to the channels in queue, retry spin_count times.
																					if (the_channel.in.push_front(request, spin) == true) {
																						// Successfully pushed the chunk_index. Notify the scheduler.
																						the_channel.scheduler()->notify();
																					}
																					else {
																						// Could not push the request message to the channels in queue while
																						// spinning. Return failure code.
																						return SCERRCHANNELINFULL;
																					}
																					
																					// Successfully pushed.
																					return 0;
																				}
																				#endif

																				#if 0
																				uint32_t shared_interface::send_request_to_server(uint32_t ch, uint32_t request,
																				uint32_t spin) {
																					// Get a reference to the channel.
																					channel_type& the_channel = channel(ch);
																					
																					// Before starting to spin, check the state of the database. Asuming the
																					// state is normal.
																					if (common_client_interface().database_state()
																					== common_client_interface_type::normal) {
																				push_request_message_with_spin: /// The notify flag could be true...
																						// Push the message to the channels in queue, retry spin_count times.
																						if (the_channel.in.push_front(request, spin) == true) {
																							// Successfully pushed the chunk_index. Notify the scheduler.
																							the_channel.scheduler()->notify();
																						}
																						else {
																							// Could not push the request message to the channels in queue while
																							// spinning. Return failure code.
																							return SCERRCHANNELINFULL;
																						}
																					}
																					else {
																						// The database state is not normal. Check the state of the database.
																						switch (common_client_interface().database_state()) {
																						case common_client_interface_type::normal:
																							/// Not expected. The state was not normal, but now all of a sudden
																							/// it is normal again. Return error code or go on? I just go on
																							/// for now but TODO: return error code.
																							goto push_request_message_with_spin;
																						case common_client_interface_type
																						::database_terminated_gracefully:
																							return SCERRDBTERMINATEDGRACEFULLY;
																						case common_client_interface_type
																						::database_terminated_unexpectedly:
																							return SCERRDBTERMINATEDUNEXPECTEDLY;
																						default: // Unknown database state.
																							return SCERRUNKNOWNDBSTATE;
																						}
																					}
																					
																					// Successfully pushed.
																					/// The notify flag could be true, but it should be false! This hurts performance a lot.
																					return 0;
																				}
																				#endif

} // namespace core
} // namespace starcounter

#endif // STARCOUNTER_CORE_IMPL_SHARED_INTERFACE_HPP
