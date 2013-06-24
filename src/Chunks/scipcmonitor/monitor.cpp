//
// monitor.cpp
//
// Copyright © 2006-2011 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//
// This monitor is for the Windows platform.
//
//------------------------------------------------------------------------------
// The monitor watches over up to 64 database processes, and up to 256 client
// processes. These limits are compile time configurable.
//
// A database- or client process that wants to be monitored must first register
// its pid to the monitor and receive an owner_id. After this the process is
// being monitored.
//
// When a client process allocates a resource, such as a chunk or a channel in
// shared memory, the client uses its owner_id which will be stamped on the
// resource.
//
// A process that have registered, that intend to exit will unregistere itself
// after it have released all resources.
//
// If a process exit or crashes, the monitor eventually receives an OS event
// with the pid of the process that exit. The monitor thread that receives the
// event searches its pid register for a matching pid. If it finds one the
// clean-up program is run.
//
// The monitors clean-up thread scans resources (such as chunks and channels)
// for a matching pid and where it finds one it marks the resource for clean-up
// by setting the clean-up flag.
//
// If a client process exit, the owner_id is pushed to a cleanup queue in all
// scheduler interfaces in all managed shared memory segments (one per database
// process), and then the monitor notifies all schedulers in all database
// processes.
//
// The scheduler threads that 
//
// A] database monitor thread: then...
//
// B] client_monitor thread: sets the corresponding flag according to the
// owner_id, of the cleanup mask and wake up all schedulers. The schedulers will
// check the state of the cleanup mask (or a state variable if its "normal" or
// not), and if a scheduler see that cleanup must be done it will start doing
// this. Details about this is found below.
//
// If an event was received by a thread that watches over database processes then,
// the monitor knows
//
// A scheduler (and maybe a client) always have to check the header of a chunk
// to see if it is marked for cleanup. This is at least true in the scheduler's
// interfaces private in queue.
//
//------------------------------------------------------------------------------
// NOTES:
// A monitor is a service and is installed as a separate service in session 0.
// The monitor runs in the same user space as the server.
// Multiple monitors may run at the same time on the same machine.
// If a monitor dies, then all database- and client process that it monitors
// have to die as well.
// If the monitor exit then database- and client processes can not detect this.
//
//
//------------------------------------------------------------------------------
// create threads with boost. Then, since we need to use the Win32 API, we need
// HANDLE thread_handle = GetCurrentThread();
// and with this HANDLE we shall be able to:
// DWORD QueueUserAPC(PAPCFUNC pfnAPC, thread_handle, ULONG_PTR dwData);
//
// Don't forget CloseHandle

/// TODO: Remove includes that are not needed, when removed code below...

//#include <cstddef>
//#include <climits>
#include <iostream>
#include <vector>
#include <utility>
#include <cstdint>
#include <map>
#include <boost/unordered_map.hpp>
#include <boost/interprocess/sync/interprocess_mutex.hpp>
#include <boost/interprocess/sync/scoped_lock.hpp>
#include <boost/interprocess/sync/interprocess_condition.hpp>
#if defined(_MSC_VER)
# define WIN32_LEAN_AND_MEAN
# include <windows.h>
# undef WIN32_LEAN_AND_MEAN
#endif // defined(_MSC_VER)
#include "../common/pid_type.hpp"
#include "../common/owner_id.hpp"
#include "../common/config_param.hpp"
#include "../common/log.hpp"
#include "event.hpp"
#include "monitor.hpp"
#include "process_info.hpp"

// This is needed in order to set ScDebugPrivilege.
//#pragma comment(lib, "cmcfg32.lib")
#pragma comment(lib, "advapi32.lib")

// A server starts a monitor.exe before it starts any databases.
//
// The monitor process has int wmain(int argc, wchar_t* argv[], wchar_t* envp[])
// and not int main(int argc, char* argv[]).
//
// The server waits for about 5 seconds. Within this time period the server must
// have received an acknowledge from the monitor on standard input/output.
//
// If the monitor starts successfully, it acks with L"monitoring" (wchar_t) on
// standard output. After that the server can start databases as usual.
//
// If the monitor fails to start, the process will exit and the server receives
// EOF (end-of-file.) If the monitor cannot be started, the system cannot be
// started.

// Arguments to the monitor (wchar_t):
// First argument: server_name (L"PERSONAL" or L"SYSTEM", etc.)
//
// Second argument: Path to the dir where the monitor's
// "active_databases" file can be stored.
//

int wmain(int argc, wchar_t* argv[], wchar_t* envp[])
try {
	using namespace starcounter::core;

	std::wcout << L"Starcounter Interprocess Communication (IPC) monitor" << std::endl;
	
	// Start the monitor application.
	monitor app(argc, argv);
	app.run();
	
	// Now the monitor is running. Send ack to the server that the monitor is
	// monitoring.
	std::wcout << L"Monitoring Starcounter processes." << std::endl;
	std::wcout.flush();
	
	/// TODO: A mechanism for shutdown is not completed. I'm not so sure that
	/// killing the monitor process is the best way, but will probably work ok.
	// Waiting here forever until killed.
	Sleep(INFINITE);
}
catch (const starcounter::core::ipc_monitor_exception& e) {
	std::wcout << L"Andreas. You need to provide better feedback when started using command line." << std::endl;
	return e.error_code();
}
catch (const starcounter::log_exception& e) {
	std::wcout << L"Error: starcounter::log_exception caught: "
	<< "Failed to open a Starcounter log for IPC monitor logging.\n"
	<< L"SCERR" << e.error_code() << "\n"
	<< L"This error has therefore not been logged." << std::endl;
	return e.error_code();
}
catch (...) {
	std::wcout << L"Unknown exception" << std::endl;
	std::wcout << L"Error code is SCERR" << SCERRIPCMONITORUNKNOWNEXCEPTION << std::endl;
	return SCERRIPCMONITORUNKNOWNEXCEPTION;
}

#if 0 /// TODO move this code to the monitor threads
// client_event() is reentrant and waits for a group of client processes.
/*static*/ DWORD WINAPI client_event(VOID* args) {
	std::size_t group = reinterpret_cast<std::size_t>(args);
	DWORD exit_code = 0;
	
	// Before the thread have any handles to wait for:
	// No need to check the return value. It is WAIT_IO_COMPLETION due to one or
	// more I/O completion callback functions. The thread that called SleepEx()
	// is the same thread that called the extended I/O function.
	while (true) {
		std::cout << group << " SleepEx" << std::endl;
		DWORD r = SleepEx(INFINITE, TRUE);
		std::cout << group << " SleepEx returned " << r << std::endl;
	}
	//--------------------------------------------------------------------------
	std::cout << "client_group_event() group " << group << " now exit." << std::endl;
	ExitThread(exit_code);

	//--------------------------------------------------------------------------
	// Terminating a thread does not necessarily remove the thread object from
	// the operating system. A thread object is deleted when the last handle to
	// the thread is closed. ExitThread followed by a CloseHandle is the
	// graceful way to shut down a thread. To immediately stop a thread, call
	// TerminateThread. Windows automatically calls ExitThread when a thread
	// ends its function. The exit_code can later be retrieved using
	// GetExitCodeThread()
}


	
	using namespace starcounter::core; // after try moves catch it is in scope.
	
	const DWORD client_handles = 1;
	HANDLE client_handle[client_handles];

	//--------------------------------------------------------------------------
	for (uint64_t i = 0; i < client_handles; ++i) {
		client_handle[i] = CreateThread(NULL, 0, client_event, (VOID*) i, 0, NULL);
		if (client_handle[i] == NULL) {
			DWORD err = GetLastError();
			std::cout << "main: CreateThread() failed with the error " << err
			<< std::endl;
			return 1; /// TODO: Fix the error handling.
		}
	}

	//WaitForMultipleObjectsEx(client_handles, client_handle, FALSE, INFINITE, TRUE);
	//DWORD the_event;
	DWORD the_event = WaitForMultipleObjectsEx(client_handles, client_handle,
	FALSE, INFINITE, TRUE);	
	
	while (true) {
		std::cout << "Waiting for objects..." << std::endl;
		the_event = WaitForMultipleObjectsEx(client_handles,
		client_handle, FALSE, INFINITE, TRUE);
		std::cout << "Done waiting for objects, the_event = "
		<< the_event << std::endl;

		if (the_event < client_handles) {
			// One of the clients have an event.
			std::cout << "client = " << the_event << std::endl;
			break;
		}
		else switch (the_event) {
		case WAIT_IO_COMPLETION:
			// The wait was ended by one or more user-mode asynchronous
			// procedure calls (APC) queued to the thread.
			std::cout << "APC" << std::endl;
			break;
		case WAIT_FAILED: {
				DWORD err = GetLastError();
				std::cout << "failed with the error " << err << std::endl;
				//...5 = access denied
				return 0;
				break;
			}
		case ERROR_INVALID_PARAMETER:
			std::cout << "INVALID" << std::endl;
			// It outputs 6 = ERROR_INVALID_HANDLE...
			break;
		default:
			std::cout << "UNKNOWN" << std::endl;
			// unknown event code
			break;
		}
	}
	std::cout << "exit" << std::endl;
	CloseHandle(client_handle);

	//--------------------------------------------------------------------------
	//HANDLE register_client_pid_event = CreateEvent(NULL, FALSE, FALSE, NULL);
	//HANDLE unregister_client_pid_event = CreateEvent(NULL, FALSE, FALSE, NULL);
	//HANDLE register_server_pid_event = CreateEvent(NULL, FALSE, FALSE, NULL);
	//HANDLE unregister_server_pid_event = CreateEvent(NULL, FALSE, FALSE, NULL);
	//HANDLE shutdown_event = CreateEvent(NULL, FALSE, FALSE, NULL);

	// index:	HANDLE:
	// -------------------------------------------------
	// 0		server_group[0] (server process 0..63)
	// 1		client_group[0] (client process 0..63)
	// 2		client_group[1] (client process 64..127)
	// 3		client_group[2] (client process 128..191)
	// 4		client_group[3] (client process 192..255)
	// 5		register_client_pid_event
	// 6		unregister_client_pid_event
	// 7		register_server_pid_event
	// 8		unregister_server_pid_event
	// 9		shutdown_event
	//HANDLE event[64];
	std::size_t client_process_event_groups = 4;
	std::size_t database_process_event_groups = 1;
	std::size_t control_and_update_event_groups = 1;

{
	// The client_process_id is communicated over shared memory.
	std::cout << "client_process_id = ";
	DWORD client_process_id;
	std::cin >> client_process_id;

	HANDLE client_handle[MAXIMUM_WAIT_OBJECTS];
	DWORD client_handles = 0;
	//--------------------------------------------------------------------------
	if ((client_handle[0] = OpenProcess(SYNCHRONIZE, FALSE, client_process_id))
	== NULL) {
		DWORD err = GetLastError();
		std::cout << "OpenProcess failed with the error: " << err << std::endl;
		return 1;
	}
	++client_handles;
	
	for (uint64_t i = 0; i < client_handles; ++i) {
		std::cout << "client_handle[" << i << "] = "
		<< client_handle[i] << std::endl;
	}
	
	DWORD the_event;
	
	while (true) {
		std::cout << "Waiting for objects..." << std::endl;
		the_event = WaitForMultipleObjectsEx(client_handles,
		client_handle, FALSE, INFINITE, TRUE);
		std::cout << "Done waiting for objects, the_event = "
		<< the_event << std::endl;
		
		if (the_event < client_handles) {
			// One of the clients have an event.
			std::cout << "client = " << the_event << std::endl;
			break;
		}
		else switch (the_event) {
		case WAIT_IO_COMPLETION:
			// The wait was ended by one or more user-mode asynchronous
			// procedure calls (APC) queued to the thread.
			std::cout << "APC" << std::endl;
			break;
		case WAIT_FAILED: {
				DWORD err = GetLastError();
				std::cout << "failed with the error " << err << std::endl;
				//...5 = access denied
				return 0;
				break;
			}
		case ERROR_INVALID_PARAMETER:
			std::cout << "INVALID" << std::endl;
			// It outputs 6 = ERROR_INVALID_HANDLE...
			break;
		default:
			std::cout << "UNKNOWN" << std::endl;
			// unknown event code
			break;
		}
	}
	std::cout << "exit" << std::endl;
	CloseHandle(client_handle);
}
	return 0;
	//--------------------------------------------------------------------------
	typedef boost::unordered_map<pid_type, owner_id> pid_register_type;
	pid_register_type pid_register;
	for (uint64_t i = 0; i < 512; ++i) {
		pid_register[pid_type(i +1000)] = owner_id(i); // insert
	}
	std::cout << "pid:\t\t\towner_id:" << std::endl;
	// Print the pid register
	for (pid_register_type::iterator pos = pid_register.begin();
	pos != pid_register.end(); ++pos) {
		std::cout << pos->first << "\t" << pos->second << std::endl;
	}
	pid_register_type::iterator it = pid_register.find(pid_type(11));
	if (it == pid_register.end()) {
		std::cout << "not found" << std::endl;
		return 0;
	}
	std::cout << it->first << "\t" << it->second << std::endl;
	pid_register.erase(it);
	pid_register.erase(pid_register.find(pid_type(1001)));
	std::cout << pid_register.find(pid_type(12))->second << std::endl;
	return 0;
	
	//--------------------------------------------------------------------------
	std::cout << "enter pid: ";
	HANDLE client_process_id;
	std::cin >> client_process_id;
	HANDLE object_handle[1];
	object_handle[0] = client_process_id;
	std::cout << "object_handle[0] is: " << object_handle[0] << std::endl;
	//DWORD the_event;
	
	while (true) {
		std::cout << "now waiting..." << std::endl;
		the_event = WaitForMultipleObjectsEx(1, object_handle, TRUE,
		INFINITE, TRUE);
		std::cout << "done waiting. the_event = " << the_event << std::endl;

		if (the_event < 4 /*threads*/) {
			// One of the 4 threads have an event. Which thread is it?
			std::cout << "the_event = " << the_event << std::endl;
			break;
		}
		else switch (the_event) {
		case WAIT_IO_COMPLETION:
			// The wait was ended by one or more user-mode asynchronous
			// procedure calls (APC) queued to the thread.
			std::cout << "APC" << std::endl;
			break;
		case WAIT_FAILED: {
				DWORD err = GetLastError();
				std::cout << "failed with the error " << err << std::endl;
				//...
				break;
			}
		case ERROR_INVALID_PARAMETER:
			std::cout << "INVALID" << std::endl;
			// It outputs 6 = ERROR_INVALID_HANDLE...
			break;
		default:
			std::cout << "UNKNOWN" << std::endl;
			// unknown event code
			break;
		}
	}
	std::cout << "exit" << std::endl;
	return 0;

#if 0 /// cancel out this code in order to check in the code
	DWORD object_handles = 0; // Keep track of number of object handles.
	DWORD timeout_milliseconds = 8; // What is a good value for the timeout?
	DWORD result;
	DWORD error;

	// When a process registers, ++object_handles; then call WaitForMultipleObjects().

	// Must wait for the first client to register before entering the loop.

	while (true) {
		//----------------------------------------------------------------------
		result = WaitForMultipleObjects(object_handles, object_handle, FALSE,
		timeout_milliseconds);
		//----------------------------------------------------------------------
		switch (result) {
		case WAIT_OBJECT_0:
		case WAIT_ABANDONED_0:
		case WAIT_TIMEOUT:
			// The time-out interval elapsed and the conditions specified by the
			// bWaitAll parameter is not satisfied.
			
			/// Check if a client is waiting for registration!

			// clients GetProcessId() and register it, and receives an owner_id.

			// Constructor
			//object_handle[object_handles++] OpenProcess(DWORD dwDesiredAccess,
			//BOOL bInheritHandle, DWORD dwProcessId);
			//++object_handles;
			break;
		
		case WAIT_FAILED:
			// WaitForMultipleObjects() has failed.
			error = GetLastError();
			// How shall the error be handled?
			std::cerr << "monitor: an error occurred: " << error << std::endl;
			break;
		default:
			std::cerr << "monitor: unknown error occurred" << std::endl;
		}
		//----------------------------------------------------------------------
	}

	// Time to quit
	// Close all opened handles
	for (uint64_t i = 0; i < object_handles; ++i) {
		if (!CloseHandle(object_handle[i])) {
			error = GetLastError();
			// How shall the error be handled?
			std::cerr << "monitor: an error occurred: " << error << std::endl;
		}
	}
	////////////
	HANDLE hThread;
	DWORD i, dwEvent, dwThreadID;
	// Create two event objects
	for (i = 0; i < 2; i++) {
		object_handle[i] = CreateEvent(
		NULL,   // default security attributes
		FALSE,  // auto-reset event object
		FALSE,  // initial state is nonsignaled
		NULL);  // unnamed object
		
		if (object_handle[i] == NULL) {
			std::cout << "CreateEvent error: " << GetLastError() << std::endl;
			ExitProcess(0);
		}
	}
	
	// Create a thread
	hThread = CreateThread( 
	NULL,         // default security attributes
	0,            // default stack size
	(LPTHREAD_START_ROUTINE) ThreadProc, 
	NULL,         // no thread function arguments
	0,            // default creation flags
	&dwThreadID); // receive thread identifier

	if( hThread == NULL ) {
		printf("CreateThread error: %d\n", GetLastError());
		return 1;
	}

	QueueUserAPC(func, hThread, data);
	
	// Wait for the thread to signal one of the event objects

	dwEvent = WaitForMultipleObjects( 
	2,           // number of objects in array
	object_handle,     // array of objects
	FALSE,       // wait for any object
	5000);       // five-second wait

	// The return value indicates which event is signaled

	switch (dwEvent) {
	// object_handle[0] was signaled
	case WAIT_OBJECT_0 + 0: 
		// TODO: Perform tasks required by this event
		printf("First event was signaled.\n");
		break;
	// object_handle[1] was signaled
	case WAIT_OBJECT_0 + 1: 
		// TODO: Perform tasks required by this event
		printf("Second event was signaled.\n");
		break;
	case WAIT_TIMEOUT:
		printf("Wait timed out.\n");
		break;
	// Return value is invalid.
	default: 
		printf("Wait error: %d\n", GetLastError()); 
		ExitProcess(0); 
	}
	// Close event handles
	for (i = 0; i < 2; ++i) {
		CloseHandle(object_handle[i]);
	}
#endif /// cancel out this code in order to check in the code
}

#if 0 // example from: http://suacommunity.com/dictionary/waitpid-entry.php
pidcnt = 0, pidsiz = 0;
HANDLE* pidarray;

// To be called when a new process is created to keep track of it
BOOL store_pid(HANDLE h) {
	if (pidcnt == pidsiz) {
		pidsiz += 8;
		pidarray = realloc(pidarray, pidsiz * sizeof(HANDLE));
	}
	pid_array[pid_cnt++] = h;
}

// Return the handle of a process that has completed
HANDLE our_waitpid() {
	DWORD rez;
	HANDLE h;
	rez = WaitForMultipleObjects(pidcnt, pidarray, FALSE, 0);
	h = pidarray[rez];
	memmove(&pidarray[rez], &pidarray[rez +1], (pidsiz -rez) * sizeof(HANDLE));
	pidcnt--;
	return h;
}
#endif // example from: http://suacommunity.com/dictionary/waitpid-entry.php

#endif /// TODO move this code to the monitor threads


#if 0
starcounter_server (service or admin)
=====================================
1	Start the starcounter_process_monitor. Communicate over a pipe.

2	Wait for ack from the starcounter_process_monitor.

2.1	If the starcounter_process_monitor is ready it replies with "ready",
	and the starcounter_server can start one or several starcounter database
	manager(s).

2.2	If the starcounter_process_monitor failed (because it could not delete all
	shared memory segments with the prefix name starcounter_* under
	C:\Users\All Users\boost_interprocess), the process exit with an error code.
	In this case the starcounter_server gets EOF on the pipe when the
	starcounter_process_monitor terminates and this ack represnts falire and the
	system can not be started.


starcounter_process_monitor
===========================
1	Delete all shared memory segments with the prefix name starcounter_* under
	C:\Users\All Users\boost_interprocess
	
1.1	If task of deleting all starcounter_* was successfull, ack "ready" to the
	starcounter_server.

1.2	If the operation failed, terminate the process with an error code. In this
	case the system can not be started.

2	Do the main tasks (2.1, 2.2 and 2.3) in parallel:

2.1	Wait for client and server processes to register their pid and return a
	session-unique owner_id if successfull.

2.2	Wait for client and server processes to unregister their pid and return
	owner_id::none if successfull.

2.3	Detect when processes exit and search the private pid-register. If the pid
	is found, the process crashed.

2.3.1	If it was a client process that crashed, inform all schedulers to
		clean-up and free all resources that the client owned.

2.3.2	If it was a starcounter_database_manager process that crashed, inform
		all clients that were using that database that the server has crashed.


a starcounter_database_manager
==============================
1	Open or create starcounter_<db_name>_0.

1.1	If create, the current_file_number variable in the file is set to 0.
	Check if any starcounter_<db_name>_* exists. If so, throw an error meaning
	that the database can not be started. Shall the process terminate?

1.2	If open, read current_file_number and create a shared memory segment named
	starcounter_<db_name>_<current_file_number +1>.
	So if current_file_number is 0, the name is starcounter_<db_name>_1, etc.
	Initialize the shared memory segment.

2	Increment the current_file_number variable and write it to
	starcounter_<db_name>_0.

3	Register this server pid and get an owner_id from the monitor.

NOTE: When writing the current_file_number variable, the _InterlockedExchange()
is used for synchronization.


Client
======
1	Open starcounter_<db_name>_0.

1.1	If failure, throw exception("bad filename").

1.2	If successfull, read the current_file_number variable.

1.2.1	If it is 0, the database doesn't exist. Throw exception("database does
		not exist").

1.2.2	If it is > 0, open the shared memory segment
		starcounter_<db_name>_<current_file_number>. If failing to open the
		shared memory segment, throw exception("failed to open the shared memory
		segment starcounter_<db_name>_<current_file_number>").

2	Register this client pid and get an owner_id from the monitor.

#endif
