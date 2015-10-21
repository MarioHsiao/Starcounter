//
// server.cpp
//
// Copyright © 2006-2011 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#pragma warning(disable:4267)
#pragma warning(disable:4996)

#include <cstdlib>
#include <iostream>
#include <string>
#include <boost/cstdint.hpp>
#include <boost/bind.hpp>
#include <boost/thread/thread.hpp>
#include <boost/interprocess/managed_shared_memory.hpp>
#include <boost/interprocess/allocators/allocator.hpp>
#include <boost/interprocess/exceptions.hpp>
#include <boost/shared_ptr.hpp>
#include <boost/call_traits.hpp>
#include <boost/bind.hpp>
#include <boost/utility.hpp>
#include <boost/noncopyable.hpp>
#include <boost/timer.hpp>

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <intrin.h>
#undef WIN32_LEAN_AND_MEAN

#include "../common/circular_buffer.hpp"
#include "../common/bounded_buffer.hpp"
#include "../common/chunk.hpp"
#include "../common/shared_chunk_pool.hpp"
#include "../common/channel.hpp"
#include "../common/scheduler_interface.hpp"
#include "../common/config_param.hpp"
#include "../common/macro_definitions.hpp"
#include "initialize.hpp"
#include "scheduler.hpp"

#if 0
#include <boost/thread.hpp>
#include <boost/bind.hpp>
#include <boost/shared_ptr.hpp>
#include <vector>
#endif

#include "server.hpp"

namespace starcounter {
namespace core {

#if 0
// The server is implemented as a function here, but
// consider implementing it as a functor instead.
void server() {
	boost::thread_group the_scheduler_threads;
	
	for (std::size_t i = 0; i < schedulers; ++i) {
		the_scheduler_threads.create_thread(boost::bind(&starcounter::core::scheduler, i));
	}

	std::cout << "server: ready to process messages..." << std::endl;
	the_scheduler_threads.join_all();
	std::cout << "server: exit." << std::endl;
}
#endif

#if 0 // Idéas.
server::server(const std::string& netfile_config, std::size_t thread_pool_size) {
	//...
}

void server::run() {
	// Create a pool of threads to run all of the io_services.
	std::vector<boost::shared_ptr<boost::thread> > threads;
	for (std::size_t i = 0; i < thread_pool_size_; ++i) {
		boost::shared_ptr<boost::thread> thread(new boost::thread(boost::bind(
		&boost::asio::io_service::run, &io_service_)));

		threads.push_back(thread);
	}

	// Wait for all threads in the pool to exit.
	for (std::size_t i = 0; i < threads.size(); ++i) {
		threads[i]->join();
	}
}

void server::stop() {
	//...
}
#endif // Idéas.

} // namespace core
} // namespace starcounter
