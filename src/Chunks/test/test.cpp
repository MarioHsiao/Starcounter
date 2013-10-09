//
// test.cpp
// IPC test
//
// Copyright © 2006-2013 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//
// This IPC test is for the Windows platform.
//

#include <cstdint>
#include <iostream>
//#include <sccoreerr.h>
#include "test.hpp"

// Providing the arguments in a very hackish and uggly way - the user have to provide the arguments in the correct order.
// The correct way is to have the same functionality that the Boost.Program_options library provide. For example:
// >sc_ipc_test.exe --server-name=PERSONAL --database-name=db4 --timeout=10000 --workers=4 --num_schedulers_each_worker_connect_to=4
//
// But we are dropping Boost so we have to make such a lib ourself. No time for that now.
// Usage example with database named "db4" running under PERSONAL server.
// Running the IPC test for 10000 milliseconds:
//>sc_ipc_test.exe PERSONAL 4 10000 4 db4
int wmain(int argc, wchar_t* argv[], wchar_t* envp[])
try {
	// Start the interprocess_communication test application.
	starcounter::interprocess_communication::test app(argc, argv);
    std::wcout << "Starting " << app.workers() << " workers playing ping-pong with the database for "
    << app.timeout() / 1E3 << " s. . ." << std::endl;
	app.run();

    // The main thread sleeps while the workers run. . .
	::Sleep(app.timeout());
    
    std::wcout << "IPC test done, stopping all workers. . ." << std::endl;
    app.stop_all_workers();
    
    // Now all worker threads are joined. Show statistics, and exit.
    app.show_statistics();
    ::exit(EXIT_SUCCESS);
}
catch (starcounter::interprocess_communication::test_exception& e) {
	std::cout << "error: test_exception "
	<< "caught with error code " << e.error_code() << std::endl;
    ::exit(EXIT_FAILURE);
}
catch (starcounter::interprocess_communication::worker_exception& e) {
	std::cout << "error: worker_exception "
	<< "caught with error code " << e.error_code() << std::endl;
    ::exit(EXIT_FAILURE);
}
catch (starcounter::core::database_shared_memory_parameters_ptr_exception& e) {
	std::cout << "error: database_shared_memory_parameters_ptr_exception "
	<< "caught with error code " << e.error_code() << std::endl;
    ::exit(EXIT_FAILURE);
}
catch (starcounter::core::monitor_interface_ptr_exception& e) {
	std::cout << "error: monitor_interface_ptr_exception "
	<< "caught with error code " << e.error_code() << std::endl;
    ::exit(EXIT_FAILURE);
}
catch (starcounter::core::shared_interface_exception& e) {
	std::cout << "error: shared_interface_exception "
	<< "caught with error code " << e.error_code() << std::endl;
    ::exit(EXIT_FAILURE);
}
catch (boost::interprocess::interprocess_exception&) {
	std::cout << "error: boost::interprocess::interprocess_exception caught"
	<< std::endl;
    ::exit(EXIT_FAILURE);
}
catch (...) {
	// An unknown exception was caught.
	std::cout << "error: unknown exception caught" << std::endl;
    ::exit(EXIT_FAILURE);
}
