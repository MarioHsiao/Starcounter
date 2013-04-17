//
// test.cpp
// IPC test
//
// Copyright © 2006-2012 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//
// This IPC test is for the Windows platform.
//

#include <boost/interprocess/sync/interprocess_mutex.hpp>
#include <boost/interprocess/sync/scoped_lock.hpp>
#include <boost/interprocess/sync/interprocess_condition.hpp>
#include <boost/thread/thread.hpp>
#include <boost/thread/mutex.hpp>
#include <boost/thread/condition.hpp>
#include <boost/shared_ptr.hpp>
#include <boost/call_traits.hpp>
#include <boost/bind.hpp>
#include <boost/utility.hpp>
#include <boost/noncopyable.hpp>
#include <boost/scoped_ptr.hpp>
#include <sccoreerr.h>
#include "../common/shared_interface.hpp"
#include "../common/config_param.hpp"
#include "test.hpp"

// For testing tiny tuple
#include "tiny_tuple/test.hpp"
#include "tiny_tuple/tiny_tuple.hpp"
#include "tiny_tuple/record_data.hpp"

//extern void starcounter::core::tiny_tuple::benchmark();

int wmain(int argc, wchar_t* argv[], wchar_t* envp[])
try {
	///=========================================================================
	/// Tiny tuple test:
	///=========================================================================

	try {
		using namespace starcounter::core::tiny_tuple::record;

		// Pretend that the RECORD HEADER size is 3 bytes.
		uint32_t record_header_size = 3;

		// Get a pointer to the DATA HEADER in the record.
		data_header::pointer_type data_header_addr = data_header::pointer_type
		(get_pointer_to_record_data(record_header_size));

		std::cout << "DATA HEADER ADDRESS: " << data_header_addr << std::endl;

		// Read the COLUMNS value from the DATA HEADER.
		uint32_t columns = data_header::number_of_columns(data_header_addr);
		std::cout << "COLUMNS: " << columns << std::endl;

		// Read the OFFSET SIZE value from the DATA HEADER.
		uint32_t osize = data_header::offset_size(data_header_addr);
		std::cout << "OFFSET SIZE: " << osize << std::endl;

		defined_column_value::pointer_type value_ptr;
		data_header::index_type index = 0;
		defined_column_value::size_type sz = 0;

		// Get value to DEFINED COLUMN VALUE at index, or 0 if not defined.
		value_ptr = get_pointer_to_value(data_header_addr, index, &sz);
		
		std::cout << "DEFINED COLUMN VALUE POINTER: " << (void*) value_ptr << std::endl;
		std::cout << "DEFINED COLUMN VALUE SIZE: " << sz << std::endl;

		uint64_t value = *((uint64_t*) value_ptr);
		value &= (1ULL << (sz << 3)) -1;
		std::cout << "DEFINED COLUMN VALUE: " << value << std::endl;

		// Start the tiny_tuple_test application.
		//boost::scoped_ptr<starcounter::core::tiny_tuple::test> app
		//(new starcounter::core::tiny_tuple::test(argc, argv));

		//app->run();
	}
	catch (...) {
		// An unknown exception was caught.
		std::cout << "error: unknown exception caught" << std::endl;
	}
	return 0;

	///=========================================================================
	
	// Start the interprocess_communication test application.
	boost::scoped_ptr<starcounter::interprocess_communication::test> app
	(new starcounter::interprocess_communication::test(argc, argv));
	
	std::cout << "workers: " << starcounter::interprocess_communication::test::workers << std::endl;

	app->run(200 /* interval time milliseconds */,
	6000000 /* duration time milliseconds */);
	
	// Stop worker 0.
	//app->stop_worker(0);
	//app->stop_all_workers();
	
	Sleep(INFINITE);
	std::cout << "test: exit." << std::endl;
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
	std::cout << "error: shared_interface_exception caught" << std::endl;
}
catch (...) {
	// An unknown exception was caught.
	std::cout << "error: unknown exception caught" << std::endl;
}
