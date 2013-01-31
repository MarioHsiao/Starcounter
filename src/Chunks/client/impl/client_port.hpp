//
// impl/client_port.hpp
// client
//
// Copyright © 2006-2012 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//
// Implementation of class client_port.
//

#ifndef STARCOUNTER_CORE_IMPL_CLIENT_PORT_HPP
#define STARCOUNTER_CORE_IMPL_CLIENT_PORT_HPP

// Implementation

namespace starcounter {
namespace core {

inline client_port::client_port(const char* segment_name, const char*
monitor_interface_name, pid_type pid, owner_id oid)
: shared_interface(segment_name, monitor_interface_name, pid, oid)
{}

inline uint32_t client_port::send_to_server_and_wait_response(uint32_t channel,
uint32_t request, uint32_t& response, uint32_t spin, uint32_t timeout) {
	return shared_interface::send_to_server_and_wait_response(channel, request,
	response, spin, timeout);
}

inline std::string client_port::get_segment_name() const {
	return shared_interface::get_segment_name();
}

inline chunk_index client_port::wait_for_response(uint32_t the_channel_index) {
	chunk_index the_chunk_index;
	shared_interface::channel(the_channel_index).out.pop_back(&the_chunk_index);
	return the_chunk_index;
}

inline chunk_index client_port::acquire_chunk() {
	chunk_index index;
	/// TODO: Handle timeout and errors.
	while (!shared_interface::client_acquire_linked_chunks(index, 1)) { // New API
		// until a chunk_index is acquired...
	}

	return index;
}

inline channel_number client_port::acquire_channel(scheduler_number
the_scheduler_number) {
	channel_number the_channel_number = invalid_channel_number;
	
	/// TODO: Handle errors.
	
	if (!shared_interface::acquire_channel(&the_channel_number,
	the_scheduler_number)) {
		//...
	}
	
	return the_channel_number;
}

inline void client_port::release_channel(channel_number the_channel_number) {
	shared_interface::release_channel(the_channel_number);
}

inline uint32_t client_port::get_number_of_active_schedulers() {
	return shared_interface::common_scheduler_interface()
	.number_of_active_schedulers();
}

inline void client_port::release_chunk(chunk_index the_chunk_index) {
	shared_interface::client_release_linked_chunks(the_chunk_index); // for now...name change -client
}

inline void client_port::release_client() {
	shared_interface::release_client_number();
}

inline chunk_type& client_port::get_chunk(chunk_index the_chunk_index) {
	return shared_interface::chunk(the_chunk_index);
}

} // namespace core
} // namespace starcounter

#endif // STARCOUNTER_CORE_IMPL_CLIENT_PORT_HPP
