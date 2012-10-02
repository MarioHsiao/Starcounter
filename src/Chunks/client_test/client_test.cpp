//
// client_test.cpp
// client_test
//
// Copyright © 2006-2012 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

// Use -DBOOST_CB_DISABLE_DEBUG or -DNDEBUG flags
#define BOOST_CB_DISABLE_DEBUG

#include <cstdlib>
#include <iostream>
#include <vector>
#include <string>
#include <utility>
#include <new>
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
#include "../common/monitor_interface.hpp"
#include "../common/shared_memory_manager.hpp"
#include "../common/database_shared_memory_parameters.hpp"
#include "../common/timeout.hpp"
#include "../common/config_param.hpp"
#include "../common/macro_definitions.hpp"
#include <sccoreerr.h>

#if defined(_MSC_VER)
# define WIN32_LEAN_AND_MEAN
# include <windows.h>
# undef WIN32_LEAN_AND_MEAN
#endif // defined(_MSC_VER)
#include "../monitor/event.hpp"
#include "client_port.hpp"

uint32_t initialize_port(void* port, const char* database_name, uint64_t
port_number);

// TODO: Remove when returning suitable error codes everywhere.
#define _E_UNSPECIFIED 999L

//typedef DWORD affinity_mask; /// TODO

// Usage: client_test
// <number of messages>
// <scheduler number>
// <number of channels>
// <database name>
// <spin count> (optional, default value is 100000)
/// TODO: Use Boost.Program_options
int main(int argc, char* argv[]) try {
	using namespace starcounter::core;
	std::cout << "This client pid: " << GetCurrentProcessId()
	<< " running on processor number " << GetCurrentProcessorNumber()
	<< std::endl;
	
	// All arguments must be provided.
	if (argc < 4) {
		std::cout << "Please enter 4 arguments:\n""<number of messages> "
		"<scheduler number> <number of channels> <database name>" << std::endl;
		return 0;
	}
	
	uint64_t iterations = std::atoi(argv[1]);
	scheduler_number the_scheduler_number = std::atoi(argv[2]);
	if (the_scheduler_number > SCHEDULERS -1) {
		the_scheduler_number = SCHEDULERS -1;
		std::cout
		<< "Scheduler number is out of range. Setting scheduler number to "
		<< the_scheduler_number << std::endl;
	}
	
	std::size_t channels_to_allocate = 1;
	channels_to_allocate = std::atoi(argv[3]);
	if (channels_to_allocate < 1) {
		channels_to_allocate = 1;
		std::cout << "Channels to allocate is out of range. "
		"Setting channel number to " << channels_to_allocate << std::endl;
	}
	
	char* database_name = argv[4];
	
	// Setting spin_count to the default, non scientific, value.
	std::size_t spin_count = 100000; /// TODO: Experiment with spin_count
	
	if (argc > 5) {
		spin_count = std::atoi(argv[5]);
	}
	std::cout << "Spin count: " << spin_count << std::endl;
	
	//--------------------------------------------------------------------------
	#if 0 /// Affinity
	affinity_mask process_affinity_mask[1] = { ~0 };
	affinity_mask system_affinity_mask[1] = { ~0 };
	
	if (!GetProcessAffinityMask(GetCurrentProcess(),
	PDWORD_PTR(process_affinity_mask), PDWORD_PTR(system_affinity_mask))) {
		std::cout << "Failed to acquire process affinity mask so mask is ~0"
		<< std::endl;
		*process_affinity_mask = ~0;
		*system_affinity_mask = ~0;
	}
	
	std::cout << "process_affinity_mask = " << *process_affinity_mask
	<< std::endl;
	std::cout << "system_affinity_mask = " << *system_affinity_mask
	<< std::endl;
	#endif /// Affinity
	
	/// Setting the affinity, since without it the performance is affected by
	/// which cores the threads happen to be running on...which is not ideal.
	/// Since it is most common now that Intel processors supporting HT have a
	/// pair of hardware threads sharing an L1-cache, I set the affinity of the
	/// server threads to be scheduled on odd numbered cores.
	DWORD_PTR affinity_mask = 1 << (the_scheduler_number * 2 +1);
	SetThreadAffinityMask(GetCurrentThread(), affinity_mask);
	SetThreadIdealProcessor(GetCurrentThread(), the_scheduler_number * 2 +1);
	Sleep(1); /// TODO: Is this neccessary?
	
	std::cout << "Setting thread ideal processor for this client thread("
	<< GetCurrentThreadId() << ") to " << the_scheduler_number * 2 +1
	<< std::endl;
	
	//affinity_mask = process_affinity_mask & 0xAAAAAAAAUL
	//& (1UL << (logical_cores_per_physical_core * );
	//SetProcessAffinityMask(GetCurrentProcess(), affinity_mask);
	//Sleep(1);
	
	//--------------------------------------------------------------------------
	/// This is kind how it is done in the real client, so I simulate it here.
	void* port_store = new char[sizeof(client_port)];
	uint64_t port_number = 0;
	uint32_t err = initialize_port(port_store, database_name, port_number);
	
	if (err) {
		std::cout << "error: initialize_port() returned " << err << std::endl;
		return err;
	}
	
	/// It is important to use a client_port* (instead of the lazy void*),
	/// because pointers to dynamic objects don't static_cast. In other words,
	/// if client_port is a dynamic object we have a bug here.
	client_port* the_port = static_cast<client_port*>(port_store);
	
	// Create a shared_interface to access all objects in shared memory.
	/// we have a client_port instead: shared_interface shared(segment_name);
	
	std::cout << "Opened the database shared memory segment: " << the_port->
	get_segment_name() << std::endl;
	
	// Each client (consisting of at least one client thread) acquire their own
	// resources: owner_id, client_number, channels and chunks, etc.
	
	std::cout << "client number: " << the_port->get_client_number()
	<< std::endl; /// debug
	
	//--------------------------------------------------------------------------
	// Get reference to this client's client interface. Schedulers reach it via
	// the channel but the client can reach it faster by having is's own
	// reference to it.
	///client_interface_type& this_client_interface = the_port->client_interface();
	///this_client_interface.set_id(my_client_number); /// TODO: remove - used for debug
	
	///=========================================================================
	/// Preparing a simple ping-pong test to simulate the real client behaviour.
	
	// Acquire a channel_number.
	channel_number channel = invalid_channel_number;
	if (!the_port->acquire_channel_number(&channel, the_scheduler_number)) {
		std::cout << "error: invalid_channel_number" << std::endl;
		return _E_UNSPECIFIED; /// TODO: Return a suitable error code.
	}
	
	std::cout << "channel: " << channel << std::endl;
	
	///-------------------------------------------------------------------------
	// Acquire a chunk and get a chunk_index referring to it (the_chunk_index.)
	//shared.acquire_chunk_index(&the_chunk_index);
	// In this example only the pre-allocated chunk (same chunk_index as the
	// channel number) is used, so:
	
	/// Other input paremeters.
	chunk_index request = channel;
	chunk_index response;
	uint32_t timeout = 120000; // milliseconds
	
	/// The error code returned.
	///uint32_t err;
	
	/// For statistics
	uint64_t ping_messages_sent = 0;
	uint64_t pong_messages_received = 0;
	///uint64_t signaled_counter = 0;
	///uint64_t timeout_counter = 0;
	
	// Get the channels pre-allocated chunk to write the request in it. The same
	// request_chunk is reused all the time in this client ping-pong test.
	chunk_type& request_chunk = the_port->chunk(channel);
	
	std::cout << "Running..." << std::endl; /// debug info
	
	// Start timing.
	boost::timer t;
	
	// Start of loop
	while (pong_messages_received < iterations) {
		// Write a request message into the request chunk. Simple example:
		/// TODO: check if this is in fact written to the same chunk?
		*((PACKED uint32_t*) &request_chunk[16]) = 0;
		*((PACKED uint32_t*) &request_chunk[20]) = 'PING';
		
		if ((err = the_port->send_to_server_and_wait_response(channel, request,
		response, spin_count, timeout)) == 0) {
			++ping_messages_sent; /// For statistics.
			// Get reference to the response chunk.
			chunk_type& response_chunk = the_port->chunk(response);
			
			// Read the message. Simple example:
			if (*((PACKED uint32_t*) &response_chunk[24]) == 'PONG') {
				++pong_messages_received; /// For statistics.
			}
			continue;
		}
		else {
			std::cout << "error " << err << " occurred in the loop. stopping."
			<< std::endl; /// debug info
			goto end_of_loop;
		}
	}
	
end_of_loop:
	// Get timestamp for elapsed time.
	double timestamp = t.elapsed();
	
	// Release the chunk(s).
	///shared.release_chunk_index(the_chunk_index);
	
	// Release the channel_number.
	the_port->release_channel_number(channel, the_scheduler_number);
	
	//--------------------------------------------------------------------------
	// Show statistics
	double messages_per_second = double(ping_messages_sent) / timestamp;
	double ns_per_messages = (1E9 * timestamp) / double(ping_messages_sent);
	
	///std::cout << "Waited " << signaled_counter +timeout_counter << " times"
	///<< std::endl;
	///std::cout << "The server signaled " << signaled_counter << " times"
	///<< std::endl;
	///std::cout << "Timeout occurred " << timeout_counter << " times"
	///<< std::endl;
	std::cout << int(ns_per_messages +.5) << " ns per message"
	<< std::endl;
	
	//--------------------------------------------------------------------------
	/// TODO: Use RAII.
	// Release resources: chunks, channels, client_number and owner_id, etc.
	
	// Release the client number.
	the_port->release_client_number();
	
	//--------------------------------------------------------------------------
	// Send unregistration request to the monitor and try to release the
	// owner_id.
	//
	// IMPORTANT NOTE: Before calling unregister_client_process(), it must be
	// guaranteed that no thread in this client process is able to use
	// the_owner_id anymore. It is best to have a mechanism to stop all threads
	// and join them, and only after that call unregister_client_process().
	//
	// RAII shall be used for all resource acquisition, except when
	// unregistereing. We only unregister after all resources that this process
	// have been released.
	
#if 1 /// TODO: FIX THIS ---------------------------------------------------------------
	// Get monitor_interface_ptr for monitor_interface_name.
	monitor_interface_ptr the_monitor_interface(the_port->
	get_monitor_interface_name().c_str());
	
	owner_id the_owner_id = the_port->get_owner_id();
	uint32_t error_code;

	if ((error_code = the_monitor_interface->unregister_client_process
	(the_port->get_pid(), the_owner_id, 10000)) == 0) {
		std::cout << "Unregistered with the monitor." << std::endl; /// debug info
	}
	else {
		std::cout << "Unregistering with the monitor failed." << std::endl; /// debug info
	}
#endif // FIX THIS
	/// TODO: RAII if possible. This may leak memory.
	delete port_store;
}
catch (starcounter::core::shared_interface_exception& e) {
	std::cerr << "client: shared_interface_exception: " << e.error_code()
	<< std::endl; /// debug info
	return e.error_code();
}
catch (boost::interprocess::interprocess_exception& e) {
	std::cerr << "client: interprocess_exception caught: " << e.what()
	<< std::endl;
	return 1; /// TODO return error code
}
catch (...) {
	std::cerr << "client: unknown exception caught" << std::endl;
	return 1; /// TODO return error code
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
	
	// Try to register this client process pid. Wait up to 10000 ms.
	owner_id the_owner_id;
	the_monitor_interface->register_client_process(pid, the_owner_id, 10000);
	
	// Check if the owner_id is valid.
	if (the_owner_id.get_owner_id() == owner_id::none) {
		// Failed to acquire an owner_id. Since a client needs a valid owner_id
		// in order to be able to acquire resources in shared memory, the client
		// cannot continue and has to exit.
		return 999L; // SCERRCACQUIREOWNERID;
	}
	
	// Threads in this process can now use the_owner_id to acquire resources.
	
	/// TODO: Change the API so that when acquire and release resources in
	/// shared memory we pass our owner_id.
	
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
	monitor_interface_name, pid, the_owner_id /*, port_number*/);
	
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
	return 4; /// TODO: Return a suitable error code.
}
catch (...) {
	// An unknown exception was caught.
	return 999L; /// TODO: Return a suitable error code.
}
