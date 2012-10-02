//
// main.cpp
// server_test
//
// Copyright © 2006-2012 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

// Use -DBOOST_CB_DISABLE_DEBUG or -DNDEBUG flags
#define BOOST_CB_DISABLE_DEBUG

// Andreas added this here to get it to compile, but it is of course malplaced.
#define _E_UNSPECIFIED 999L

#include <cstdlib>
#include <iostream>
#include <vector>
#include <string>
#include <utility>
#include <stdexcept>
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
#include "../common/chunk.hpp"
#include "../common/monitor_interface.hpp"
#include "../common/shared_memory_manager.hpp"
#include "../common/interprocess.hpp"
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

#define _E_WAIT_TIMEOUT 1012L

typedef struct _sc_io_event {
	unsigned long channel_index_;
	unsigned long chunk_index_;
} sc_io_event;

#ifdef __cplusplus
extern "C" {
#endif

// These functions have C linkage.
unsigned long sc_initialize_io_service(const char* name, unsigned long
port_count, bool is_system, unsigned int num_shm_chunks);

unsigned long sc_sizeof_port();

unsigned long sc_initialize_port(void *port, const char *name, unsigned long
port_number);

unsigned long sc_get_next_io_event(void *port,unsigned int timeout_milliseconds,
sc_io_event *pio_event);

unsigned long sc_send_to_client(void *port, unsigned long channel_index,
unsigned long chunk_index);

unsigned long sc_send_to_port(void *port, unsigned long port_number,
unsigned long message);

void *sc_get_shared_memory_chunk(void *port, unsigned long chunk_index);

#ifdef __cplusplus
}
#endif

#include <iostream>
#if defined(_MSC_VER)
# define WIN32_LEAN_AND_MEAN
# include <windows.h>
# undef WIN32_LEAN_AND_MEAN
#endif // defined(_MSC_VER)
#include <malloc.h>
#include <stdio.h>
#include "../common/macro_definitions.hpp"

/// TODO: Fix this hack. It is used to pass parameters to scheduler().
/// The correct fix is to make a class that holds all the code in this
/// file, similar to class monitor.
struct hack {
	int id;
	std::string segment_name;
};

namespace starcounter {
namespace core {

/// TODO: Fix this...making everything a class solves it.

/// Exception class.
class database_shared_memory_parameters_exception : public std::runtime_error { /// TODO: database
public:
	/**
	 * @param message The c-string formated message to be passed.
	 */
	explicit database_shared_memory_parameters_exception(const char* message) /// TODO: database
	: std::runtime_error(message) {}
};

} // namespace core
} // namespace starcounter

static DWORD WINAPI scheduler(VOID* arg) {
	using namespace starcounter::core;
	hack* pa = reinterpret_cast<hack*>(arg);
	int id = pa->id;
	sc_io_event io_event;
	void* chunk; // old - works fine
	//chunk_type* chunk; // work in progress
	owner_id oid;
	//uint64_t user_data;
	//uint32_t request_size;
	//uint32_t response_size;
	
	/// TODO: Affinity.
	DWORD_PTR mask = 1 << (id * 2);
	SetThreadAffinityMask(GetCurrentThread(), mask);
	SetThreadIdealProcessor(GetCurrentThread(), id);
	
	void* port = malloc(sc_sizeof_port());
	unsigned long dr = sc_initialize_port(port, pa->segment_name.c_str(), id);
	dr = 0;
	while (true) {
		if (dr == 0) {
			for (int i = 0; i < 3000000; ++i) {
				dr = sc_get_next_io_event(port, 0, &io_event);
				if (dr != _E_WAIT_TIMEOUT) {
					break;
				}
			}
		}
		if (dr == _E_WAIT_TIMEOUT) {
			// tell the scheduler wait for 128 ms before timeout
			dr = sc_get_next_io_event(port, 128, &io_event);
		}
		if (dr == 0) {
			if (io_event.channel_index_ != (unsigned long) -1) {
				#if 1 // old - worked fine
				chunk = sc_get_shared_memory_chunk(port, io_event.chunk_index_);
				*((unsigned long *)(((char *)chunk) + 16)) = 0; // Request length.
				*((unsigned long *)(((char *)chunk) + 20)) = 4; // Response length.
				*((unsigned long *)(((char *)chunk) + 24)) = 'PONG';
				dr = sc_send_to_client(port, io_event.channel_index_,
				io_event.chunk_index_);
				#endif // old - worked fine
				
				#if 0 // work in progress
				chunk = static_cast<chunk_type*>(sc_get_shared_memory_chunk
				(port, io_event.chunk_index_));
				
				/// TODO: Write a chunk_header class with functions to help
				/// writing data to a chunk that can be used for anything.
				/// Even a simple example looks messy and is error prone.
				/// Also, we need to support scattered chunks and then its a
				/// no brainer to decide that this class is needed because
				/// it will be very messy code otherwise. The performance in
				/// time and space will be the same when using a class.
				
				// This simple example is messy, tedious and error prone but can
				// be less so.
				
				// Read the owner_id from byte 0..7. This must be done because
				// we are required to check if the cleanup-flag is set in which
				// case we have to free this chunk instead of working with it.
				// The alternative is to check the cleanup flag before pushing
				// the message to a queue. I don't know yet what is best.
				/// TODO: Figure which is best...
				
				oid = *((owner_id*) chunk +0);
				
				if (!oid.get_clean_up()) {
					// Read the user_data from byte 8..15
					// No need for it here. user_data = *((uint64_t*) chunk +8);
					
					// Read the request_size from byte 16..19
					request_size = *((uint32_t*) chunk +16);
					
					// Write a simple PONG response message. What's this?
					*((uint32_t*) chunk +sizeof(owner_id) +sizeof(uint64_t)
					+sizeof(uint32_t) +request_size +sizeof(uint32_t)) = 'PONG';
					
					// Write the response_size. What's this?
					*((uint32_t*) chunk +sizeof(owner_id) +sizeof(uint64_t)
					+sizeof(uint32_t) +request_size) = sizeof('PONG');
					
					dr = sc_send_to_client(port, io_event.channel_index_,
					io_event.chunk_index_);
				}
				else {
					// The cleanup flag is set. Free the chunk.
					/// TODO: Free the chunk...
				}
				#endif // work in progress
			}
			else {
				//std::cout << "server: Internal message received ("
				//<< io_event.chunk_index_ << ")" << std::endl;
			}
			continue;
		}
		if (dr == _E_WAIT_TIMEOUT) {
			//std::cout << "Timeout" << std::endl;
			continue;
		}
		std::cout << "server: error" << std::endl;
		break;
	}
	return 0;
}

/// TODO: Use Boost.Program_options.
int main(int argc, char* argv[]) try {
	using namespace starcounter::core;
	
	// The database name must be provided as the first argument.
	if (argc < 4) {
		std::cout << "usage: server_test <database name> <path>"
		<< std::endl;
		exit(EXIT_FAILURE);
	}
	
	char* server_name = argv[1];
	char* database_name = argv[2];
	char* path_to_parameter_file = argv[3];
	bool is_system = false;
	
	{
		std::string server_name_ = std::string(server_name);

		if (server_name_ == "SYSTEM") {
			is_system = true;
		}
	}
	
	///=========================================================================
	/// Open or create the database shared memory parameter file. This is done
	/// before registering with the monitor because the name of the database
	/// shared memory segment also need to be registered.
	///=========================================================================
	
	// Name of the shared memory object containing the sequence_number to be
	// appended to the name of the shared memory segment.
	// The format is <DATABASE_NAME_PREFIX>_<DATABASE_NAME>_0.
	char database_shared_memory_parameters_name[sizeof(DATABASE_NAME_PREFIX)
	+segment_name_size +4 /* two delimiters, '0', and null */];
	std::size_t length;
	
	// Construct the database_shared_memory_parameters_name. Format:
	// <DATABASE_NAME_PREFIX>_<DATABASE_NAME>_0.
	if ((length = _snprintf_s(database_shared_memory_parameters_name,
	_countof(database_shared_memory_parameters_name),
	sizeof(database_shared_memory_parameters_name) -1 /* null */, "%s_%s_0",
	DATABASE_NAME_PREFIX, database_name)) < 0) {
		std::cout << "database: failed to construct the database_shared_memory_parameters_name" << std::endl; /// debug info
		return _E_UNSPECIFIED; /// TODO: Return a suitable error code.
	}
	database_shared_memory_parameters_name[length] = '\0';
	
	//--------------------------------------------------------------------------
	// Name of the shared memory segment.
	char segment_name[sizeof(DATABASE_NAME_PREFIX)
	+segment_name_size +16 /* two delimiters, sequence_number, and null */];
	
	// Open the shared memory object with the database_shared_memory_parameters.
	// It might not exist, in which case it will be created instead.
	shared_memory_object database_shared_memory_parameters_shared_memory_object;
	database_shared_memory_parameters_shared_memory_object.init_open
	(database_shared_memory_parameters_name);
	
	if (!database_shared_memory_parameters_shared_memory_object.is_valid()) {
		// It failed because it doesn't exist. Create it instead.
		database_shared_memory_parameters_shared_memory_object.init_create
		(database_shared_memory_parameters_name,
		sizeof(database_shared_memory_parameters), false,
		shared_memory_object::file_mapped, path_to_parameter_file);
		
		if (!database_shared_memory_parameters_shared_memory_object.is_valid())
		{
			throw database_shared_memory_parameters_exception
			("failed to create database shared memory parameters");
		}
	}
	
	// The database shared memory parameters is now open.
	
	//--------------------------------------------------------------------------
	// Open the database shared memory parameter file and obtains a pointer to
	// the shared structure.
	
	/// This must be an error, here we shall do the same thing as in setup.cpp,
	/// which is to open or create (not just open):
	//database_shared_memory_parameters_ptr db_shm_params
	//(database_shared_memory_parameters_name);
	
	database_shared_memory_parameters_ptr db_shm_params
	(database_shared_memory_parameters_name, is_system,
	shared_memory_object::file_mapped, path_to_parameter_file /* db_data_dir_path */);
	
	//--------------------------------------------------------------------------
	// Store the server_name so that clients can get it.
	db_shm_params->set_server_name(server_name);
	
	//--------------------------------------------------------------------------
	// Get the sequence number and add that to the segment name.
	database_shared_memory_parameters::sequence_number_type sequence_number
	= db_shm_params->get_sequence_number();
	
	// Increment the sequence number. After the database shared memory segment
	// is constructed, set the new sequence number in the database shared memory
	// parameters to make the new segment the primary one.
	
	//--------------------------------------------------------------------------
	// segment_name was here, had to lift it outside the body.
	
	// Construct the segment_name with the appended sequence_number. Format:
	// <DATABASE_NAME_PREFIX>_<DATABASE_NAME>_<N>, where N is the
	// sequence_number read from the database_shared_memory_parameters +1.
	if ((length = _snprintf_s(segment_name, _countof(segment_name),
	sizeof(segment_name) -1 /* null */, "%s_%s_%u",
	DATABASE_NAME_PREFIX, database_name, sequence_number +1)) < 0) {
		return _E_UNSPECIFIED; /// TODO: Return a suitable error code.
	}
	segment_name[length] = '\0';
	
	///=========================================================================
	/// 2. Register with the monitor. This is done before creating the database
	/// shared memory segment in order to be able to clean up if the database
	/// goes down.
	///=========================================================================
	char monitor_interface_name[segment_name_size];
	
	// Concatenate the server_name with "_starcounter_monitor_interface".
	if ((length = _snprintf_s(monitor_interface_name,
	_countof(monitor_interface_name), sizeof(monitor_interface_name) -1
	/* null */, "%s_%s", server_name, MONITOR_INTERFACE_SUFFIX)) < 0) {
		return _E_UNSPECIFIED; /// TODO: Return a suitable error code.
	}
	monitor_interface_name[length] = '\0';
	
	// Try to open the monitor interface shared memory object.
	shared_memory_object monitor_interface_shared_memory_object
	(monitor_interface_name, shared_memory_object::open());
	
	monitor_interface* the_monitor_interface = 0;
	
	// Map the whole shared memory in this process.
	mapped_region monitor_interface_mapped_region
	(monitor_interface_shared_memory_object);
	
	if (monitor_interface_shared_memory_object.is_valid()) {
		if (monitor_interface_mapped_region.is_valid()) {
			// Obtain a pointer to the shared structure.
			the_monitor_interface = static_cast<monitor_interface*>
			(monitor_interface_mapped_region.get_address());
		}
		else {
			// failed to map the monitor interface in shared memory.
			return _E_UNSPECIFIED; /// TODO: Return a suitable error code.
		}
	}
	else {
		std::cout << "Error: Can not start. Please start the monitor first!"
		<< std::endl;
		// error: this database failed to open the monitors shared_interface
		// shared memory object. The server shall have started the monitor.
		return _E_UNSPECIFIED; /// TODO: Return a suitable error code.
	}
	
	// The monitor_interface shared memory object is open.
	
	//--------------------------------------------------------------------------
	// Send registration request to the monitor and try to acquire an
	// owner_id. Without an owner_id we can not proceed and have to exit.
	
	// GetProcessId(), register it, receive owner_id.
	
	// Get process id.
	pid_type pid;
	
	// Store this process ID.
	pid.set_current();
	
	// This is the owner_id used when acquire resources. However, a database
	// must use the owner_id of the client (found in the channel), if the
	// client is responsible for releasing the resource, which is normally
	// the case.
	owner_id the_owner_id;
	uint32_t error_code;

	// Try to register this database process pid. Wait up to 10000 ms.
	if ((error_code = the_monitor_interface->register_database_process(pid,
	segment_name, the_owner_id, 10000)) != 0) {
		// Failed to successfully register with the monitor and is thus not
		// monitored. The database cannot continue and has to exit.
		return error_code;
	}
	
	// Threads in this process can now use the_owner_id to acquire
	// resources.
	
	/// TODO: Change the API so that when acquire and release resources in
	/// shared memory we pass our owner_id.
	
	//--------------------------------------------------------------------------
	/// TODO: When does the database unregister with the monitor? Never?
	//--------------------------------------------------------------------------
	
	///=========================================================================
	/// 3. Create the database shared memory. After this clients can connect.
	///=========================================================================
	int dr;
    // NOTE: Initializing shared memory with 4096 chunks.
	if ((dr = sc_initialize_io_service(segment_name, SCHEDULERS, is_system, 4096))
	!= 0) {
		std::cout << "error: " << dr << std::endl;
		return dr;
	}
	
	// Now the database shared memory segment is constructed and it is time
	// to increment the sequence number.
	db_shm_params->increment_sequence_number();
	
	//--------------------------------------------------------------------------
	std::cout << "Database initialized with segment name: "
	<< segment_name << std::endl;
	
	HANDLE thread_handles[SCHEDULERS];
	hack a[SCHEDULERS];
	
	for (int i = 0; i < SCHEDULERS; ++i) {
		a[i].segment_name = segment_name;
		a[i].id = i;
		thread_handles[i] = CreateThread(NULL, 0, scheduler, &a[i], 0, NULL);
	}
	
	WaitForMultipleObjects(SCHEDULERS, thread_handles, TRUE, INFINITE);
	return 0;
}
catch (starcounter::core::database_shared_memory_parameters_exception& e) {
	std::cerr << "database_shared_memory_parameters_exception caught: "
	<< e.what() << std::endl;
	return 2; /// TODO return error code
}
catch (boost::interprocess::interprocess_exception& e) {
	std::cerr << "database: interprocess_exception caught: "
	<< e.what() << std::endl;
	return 1; /// TODO return error code
}
catch (...) {
	std::cerr << "database: unknown exception caught" << std::endl;
	return 1; /// TODO return error code
}
