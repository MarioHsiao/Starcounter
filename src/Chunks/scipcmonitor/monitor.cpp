//
// monitor.cpp
//
// Copyright © 2006-2013 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//
// This IPC monitor is for the Windows platform.
//

// The IPC monitor watches over up to 64 database processes, and up to 256
// client processes. These limits are compile time configurable.
//
// A database- or client process that wants to be monitored must first register
// its pid to the IPC monitor and receive an owner_id. After this the process is
// being monitored.
//
// When a client process allocates a resource, such as a chunk or a channel in
// shared memory, the client uses its owner_id which will be linked to the
// resource.
//
// A process that have registered, that intend to exit may unregistere itself
// after it have released all resources.
//
// If a process exit or crashes, the IPC monitor eventually do cleanup and
// restores all resources, such as memory chunks, channels and
// client_interfaces.
//
// There must be exactly one IPC monitor running in the same user space for each
// server. If an IPC monitor terminates unexcpectedly, then all database- and
// client process that it monitors have to terminate as well.
// If an IPC monitor exit then database- and client processes can not detect this.

#include <iostream>
//#include <boost/interprocess/sync/interprocess_mutex.hpp>
//#include <boost/interprocess/sync/scoped_lock.hpp>
#if defined(_MSC_VER)
# define WIN32_LEAN_AND_MEAN
# include <windows.h>
# undef WIN32_LEAN_AND_MEAN
#endif // defined(_MSC_VER)
#include "../common/pid_type.hpp"
#include "../common/config_param.hpp"
#include "../common/log.hpp"
#include "monitor.hpp"

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
