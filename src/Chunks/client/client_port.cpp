//
// client_port.cpp
// client
//
// Copyright © 2006-2012 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//
#include "client_port.hpp"
#include "../common/database_shared_memory_parameters.hpp"
#include <scerrres.h>

//#define _E_WAIT_TIMEOUT 1012L

#ifdef __cplusplus
extern "C" {
#endif

// These functions have C linkage.
uint32_t sizeof_port();
uint32_t initialize_port(void* port, const char *name, uint64_t port_number);
void release_port(void* port);

uint32_t post_request(void* port, uint32_t channel, uint32_t request,
uint32_t& response, uint32_t spin, uint32_t timeout);

uint32_t allocate_shared_memory_chunk(void* port);
uint32_t allocate_shared_memory_channel(void* port, uint32_t scheduler_number);
uint8_t* get_shared_memory_chunk(void* port, uint32_t chunk_index);
void release_client(void* port);
void release_channel(void* port, uint32_t channel_index, uint32_t
scheduler_number);
uint32_t release_linked_chunks(void* port, uint32_t head_chunk_index);

void release_chunk(void* port, uint32_t the_chunk_index);
uint32_t get_number_of_active_schedulers(void* port);

#ifdef __cplusplus
}
#endif

/// TODO: Error handling.

uint32_t sizeof_port()
{
	return sizeof(starcounter::core::client_port);
}

uint32_t initialize_port(void* port, const char* database_name, uint64_t
port_number) try {
	///=========================================================================
	/// Open the database shared memory parameters.
	///=========================================================================
	using namespace starcounter::core;
	
	// Construct the string containing the name of the database shared memory
	// parameters shared memory object.
	
	// Name of the shared_memory_object containing the sequence_number to be
	// appended to the name of the shared memory segment. The format is
	// <DATABASE_NAME_PREFIX>_<DATABASE_NAME>_0.
	char database_shared_memory_parameters_name[sizeof(DATABASE_NAME_PREFIX)
	+segment_name_size +4 /* two delimiters, '0', and null */];
	std::size_t length;
	
	// Construct the database_shared_memory_parameters_name. The format is
	// <DATABASE_NAME_PREFIX>_<DATABASE_NAME>_0
	if ((length = _snprintf_s(database_shared_memory_parameters_name,
	_countof(database_shared_memory_parameters_name),
	sizeof(database_shared_memory_parameters_name) -1 /* null */, "%s_%s_0",
	DATABASE_NAME_PREFIX, database_name)) < 0) {
		// Failed to construct the database_shared_memory_parameters_name.
		return SCERRCCONSTRUCTDBSHMPARAMNAME;
	}
	
	database_shared_memory_parameters_name[length] = '\0';
	
	// Open the database shared memory parameters file and obtains a pointer to
	// the shared structure.
	
	database_shared_memory_parameters_ptr db_shm_params
	(database_shared_memory_parameters_name);
	
	///=========================================================================
	/// Register with the monitor.
	///=========================================================================
	
	char monitor_interface_name[segment_name_size
	+sizeof(MONITOR_INTERFACE_SUFFIX) +2 /* delimiter and null */];
	
	// Construct the server_name. The format is
	// <SERVER_NAME>_<MONITOR_INTERFACE_SUFFIX>.
	if ((length = _snprintf_s(monitor_interface_name,
	_countof(monitor_interface_name), sizeof(monitor_interface_name), "%s_%s",
	db_shm_params->get_server_name(), MONITOR_INTERFACE_SUFFIX)) < 0) {
		// Buffer overflow.
		return SCERRCCONSTRMONITORINTERFACENAME;
	}
	
	monitor_interface_name[length] = '\0';
	
	// Get monitor_interface_ptr for monitor_interface_name.
	monitor_interface_ptr the_monitor_interface(monitor_interface_name);
	
	//--------------------------------------------------------------------------
	// Send registration request to the monitor and try to acquire an owner_id.
	// Without an owner_id we can not proceed and have to exit.
	
	// Get process id and store it in the monitor_interface.
	pid_type pid;
	pid.set_current();
	owner_id the_owner_id;
	uint32_t error_code;
	
	// Try to register this client process pid. Wait up to 10000 ms.
	if ((error_code = the_monitor_interface->register_client_process(pid,
	the_owner_id, 10000)) != 0) {
		// Failed to register this client process pid.
		return error_code;
	}
	
	// Threads in this process can now acquire resources.
	
	///=========================================================================
	/// Open the database shared memory segment.
	///=========================================================================
	
	if (db_shm_params->get_sequence_number() == 0) {
		// Cannot open the database shared memory segment, because it is not
		// initialized yet.
		return SCERRCLIENTOPENDBSHMSEGMENT;
	}
	
	// The database shared memory segment is initialized.
	
	//--------------------------------------------------------------------------
	// Name of the database shared memory segment.
	char segment_name[sizeof(DATABASE_NAME_PREFIX) +segment_name_size
	+13 /* two delimiters, '4294967295', and null */];
	
	// Construct the name of the database shared memory segment. The format is
	// <DATABASE_NAME_PREFIX>_<DATABASE_NAME>_<SEQUENCE_NUMBER>
	if ((length = _snprintf_s(segment_name, _countof(segment_name),
	sizeof(segment_name) -1 /* null */, "%s_%s_%u", DATABASE_NAME_PREFIX,
	database_name, db_shm_params->get_sequence_number())) < 0) {
		return SCERRCCONSTRDBSHMSEGMENTNAME;
	}
	
	segment_name[length] = '\0';
	
	///=========================================================================
	/// Construct a client_port at port.
	///=========================================================================
	
	client_port* the_port = new (port) client_port(segment_name,
	monitor_interface_name, pid, the_owner_id);
	
	//--------------------------------------------------------------------------
	/// This works, but the caller of initialize_port() could/should do it
	/// instead. TODO: Discuss this with Christian.
	if (!the_port->acquire_client_number()) {
		// Did not acquire a client_number. Handle the timeout, etc.
		throw shared_interface_exception(SCERRCLIENTACQUIRECLIENTNUMBER);
	}
	
	return 0;
}
catch (starcounter::core::database_shared_memory_parameters_ptr_exception& e) {
	// Map the error code in order to help finding where the error occurred.
	switch (e.error_code()) {
	case SCERROPENDBSHMPARAMETERS:
		return SCERRCOPENDBSHMPARAMETERS;
	
	case SCERRMAPDBSHMPARAMETERSINSHM:
		return SCERRCMAPDBSHMPARAMETERSINSHM;
	
	default:
		return e.error_code();
	}
	return e.error_code();
}
catch (starcounter::core::monitor_interface_ptr_exception& e) {
	return e.error_code();
}
catch (starcounter::core::shared_interface_exception& e) {
	return e.error_code();
}
catch (boost::interprocess::interprocess_exception&) {
	return SCERRBOOSTIPCEXECPTION;
}
catch (...) {
	// An unknown exception was caught.
	return SCERRCLIENTPORTUNKNOWNEXCEPTION;
}

void release_port(void* port)
{
using namespace starcounter::core;
	client_port* the_port;
	the_port = (client_port*)port;
	the_port->~client_port();
}

void release_client(void* port)
{
using namespace starcounter::core;
	client_port* the_port;
	the_port = (client_port*)port;
	the_port->release_client();
}

void release_channel(void* port, uint32_t the_channel_index, uint32_t the_scheduler_number)
{
	(void) the_scheduler_number; // TODO: Remove it from the call.
	using namespace starcounter::core;
	client_port* the_port;
	the_port = (client_port*)port;
	the_port->release_channel(the_channel_index);
}

void release_chunk(void* port, uint32_t the_chunk_index)
{
using namespace starcounter::core;
	client_port* the_port;
	the_port = (client_port*)port;
	the_port->release_chunk(the_chunk_index);
}

/// post_request() is used to send a request and wait for a response from the
/// database. It is a "timed" function that can fail.
/**
 * @param port An interface to access the databases shared memory segment.
 *
 * @param channel The channel on which the communication is done.
 *
 * @param request The request chunk_index.
 *
 * @param response Reference to the response chunk_index.
 *
 * @param spin The number of times to re-try pushing to the in queue or
 *      popping from the out queue, without blocking.
 *
 * @param timeout The number of milliseconds to wait before a timeout may occur,
 *      in case the database doesn't respond.
 *
 * @return en error code.
 */
uint32_t post_request(void* port, uint32_t channel, uint32_t request,
uint32_t& response, uint32_t spin, uint32_t timeout) {
	// Send request to server and wait for a response.
	using namespace starcounter::core;
	return static_cast<client_port*>(port)->send_to_server_and_wait_response
	(channel, request, response, spin, timeout);
}

uint32_t allocate_shared_memory_chunk(void* port)
{
using namespace starcounter::core;
	client_port* the_port = (client_port*)port;
	return the_port->acquire_chunk();
}

uint32_t allocate_shared_memory_channel(void* port, uint32_t the_scheduler_number)
{
	using namespace starcounter::core;
	client_port* the_port = (client_port*)port;
	return the_port->acquire_channel((scheduler_number)the_scheduler_number);
}

uint8_t* get_shared_memory_chunk(void* port, uint32_t chunk_index)
{
using namespace starcounter::core;
	client_port* the_port = (client_port*)port;
	chunk_type& the_chunk = the_port->get_chunk(chunk_index);
	return (uint8_t*)&the_chunk[0];
}

uint32_t get_number_of_active_schedulers(void* port)
{
	using namespace starcounter::core;
	client_port* the_port = (client_port*)port;
	return the_port->get_number_of_active_schedulers();
}

uint32_t release_linked_chunks(void* port, uint32_t head_chunk_index)
{
	using namespace starcounter::core;
	client_port* the_port = (client_port*)port;
	return the_port->client_release_linked_chunks(head_chunk_index, 1000);
}