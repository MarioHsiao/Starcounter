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

int wmain(int argc, wchar_t* argv[], wchar_t* envp[])
try {
	// Start the interprocess_communication test application.
	starcounter::interprocess_communication::test app(argc, argv);
	app.run(200 /* interval time milliseconds */);
	Sleep(INFINITE);
}
catch (starcounter::interprocess_communication::test_exception& e) {
	std::cout << "error: test_exception "
	<< "caught with error code " << e.error_code() << std::endl;
}
catch (starcounter::interprocess_communication::worker_exception& e) {
	std::cout << "error: worker_exception "
	<< "caught with error code " << e.error_code() << std::endl;
}
catch (starcounter::core::database_shared_memory_parameters_ptr_exception& e) {
	std::cout << "error: database_shared_memory_parameters_ptr_exception "
	<< "caught with error code " << e.error_code() << std::endl;
}
catch (starcounter::core::monitor_interface_ptr_exception& e) {
	std::cout << "error: monitor_interface_ptr_exception "
	<< "caught with error code " << e.error_code() << std::endl;
}
catch (starcounter::core::shared_interface_exception& e) {
	std::cout << "error: shared_interface_exception "
	<< "caught with error code " << e.error_code() << std::endl;
}
catch (boost::interprocess::interprocess_exception&) {
	std::cout << "error: boost::interprocess::interprocess_exception caught"
	<< std::endl;
}
catch (...) {
	// An unknown exception was caught.
	std::cout << "error: unknown exception caught" << std::endl;
}
