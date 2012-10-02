//
// client_port.hpp
// client
//
// Copyright © 2006-2012 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#ifndef STARCOUNTER_CORE_PORT_HPP
#define STARCOUNTER_CORE_PORT_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#include <iostream> // debug
#include <cstddef>
#include <climits>
#include <boost/cstdint.hpp>
#include <boost/thread/thread.hpp>
#include <boost/thread/mutex.hpp>
#include <boost/thread/condition.hpp>
#include <boost/shared_ptr.hpp>
#include <boost/call_traits.hpp>
#include <boost/bind.hpp>
#include <boost/utility.hpp>
#include <boost/noncopyable.hpp>
#include <boost/shared_ptr.hpp>
#define WIN32_LEAN_AND_MEAN
#include <intrin.h>
#undef WIN32_LEAN_AND_MEAN
#include "../common/circular_buffer.hpp"
#include "../common/bounded_buffer.hpp"
#include "../common/chunk.hpp"
#include "../common/shared_chunk_pool.hpp"
#include "../common/channel.hpp"
#include "../common/scheduler_number.hpp"
#include "../common/shared_interface.hpp"
#include "../common/shared_memory_manager.hpp"
#include "../common/database_shared_memory_parameters.hpp"
#include "../common/monitor_interface.hpp"

/// TODO: Remove these once suitable error codes have been defined.
#define _E_WAIT_TIMEOUT 1012L
#define _E_UNSPECIFIED 999L

namespace starcounter  {
namespace core  {

class client_port : public shared_interface {
public:
	/// Constructor.
	/**
	 * @param segment_name is used to open the database shared memory. The
	 *		segment name has the format:
	 *		<DATABASE_NAME_PREFIX>_<DATABASE_NAME>_<SEQUENCE_NUMBER>
	 *
	 * @param monitor_interface_name is used to open and close the
	 *		monitor_interface. The monitor_interface_name has the format:
	 *		<SERVER_NAME>_<MONITOR_INTERFACE_SUFFIX>
	 *
	 * @param oid Is the owner_id shared by all client threads in this client
	 *		process.
	 *
	 * @param pid Is the process id of this client process.
	 */
	client_port(const char* segment_name, const char* monitor_interface_name,
	pid_type pid, owner_id oid);
	
	/// send_to_server_and_wait_response() send a request and wait for a
	/// response from the database. It is a "timed" function that can fail.
	/**
	 * @param channel The channel which the communication is done.
	 *
	 * @param request The request chunk_index.
	 *
	 * @param response Reference to the response chunk_index.
	 *
	 * @param spin The number of times to re-try pushing to the in queue or
	 *		popping from the out queue, before eventually blocking.
	 *
	 * @param timeout The number of milliseconds to wait before a timeout may
	 *		occur, in case the database doesn't respond.
	 *
	 * @return An error code.
	 */
	uint32_t send_to_server_and_wait_response(uint32_t channel,
	uint32_t request, uint32_t& response, uint32_t spin, uint32_t timeout);
	
	chunk_index wait_for_response(uint32_t the_channel_index);
	
							/// acquire_chunk() used an obsolete API in shared_interface. Now it uses
							/// the new API, but acquire_chunk() is obsolete because it is inefficient
							/// to acquire just one chunk. So this function should be removed.
							chunk_index acquire_chunk();
	
	channel_number acquire_channel(scheduler_number the_scheduler_number);
	void release_channel(channel_number the_channel_number);
	
	uint32_t get_number_of_active_schedulers();
	
	void release_chunk(chunk_index the_chunk_index);
	
	void release_client();
	
	chunk_type& get_chunk(chunk_index the_chunk_index);
	
	std::string get_segment_name() const;
};

} // namespace core
} // namespace starcounter

#include "impl/client_port.hpp"

#endif // STARCOUNTER_CORE_PORT_HPP
