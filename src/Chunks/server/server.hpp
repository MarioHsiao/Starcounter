//
// server.hpp
//
// Copyright © 2006-2011 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//
// http://www.boost.org/doc/libs/1_46_1/doc/html/thread/thread_management.html
//

#ifndef STARCOUNTER_CORE_SERVER_HPP
#define STARCOUNTER_CORE_SERVER_HPP

#include <cstddef>
#include <iostream> // debug
#include <boost/cstdint.hpp>
#include <boost/utility.hpp>
#include "scheduler.hpp"

#if 0 // interruption points
boost::thread::join()
boost::thread::timed_join()
boost::condition_variable::wait()
boost::condition_variable::timed_wait()
boost::condition_variable_any::wait()
boost::condition_variable_any::timed_wait()
boost::thread::sleep()
boost::this_thread::sleep()
boost::this_thread::interruption_point()
#endif // interruption points

namespace starcounter {
namespace core {

//void server();

#if 0 // Later perhaps, for NUMA.
class server : private boost::noncopyable {
public:
	explicit server(const std::string& netfile_config, std::size_t thread_pool_size)
	: thread_pool_size_(thread_pool_size) {}

	// If not already created, this server creates the shared memory segment.
	// Otherwise it just opens it and finds all objects.
	// Theoretically more than one server process may be started.
	// Make sure that the shared memory is initialized by
	// the first started server only and any other server will just
	// do the same as a client, open and find the names.
	
	// A scheduler starts one thread that scans the channels that is observed
	// has one threads working.
	// It should be number of logical threads per core.
	// Normally one scheduler per physical core is created.

	// Run the server. This is just a simple test but is unsuitable because
	// we want to specify the affinity for each scheduler to match that of the
	// client each scheduler communicates with.
	void start_scheduler(std::size_t scheduler_index) {
		// set the state of all schedulers to startíng.
		// Spawn scheduler threads.
		for (std::size_t i = 0; i < thread_pool_size_; ++i) {
			scheduler_threads_.create_thread(scheduler());
		}
	}

	// Stop the server.
	void stop_scheduler(std::size_t scheduler_index) {
		// Tell all scheduler threads to stop and hope they get the message.
		// So first we need to set the state of this process schedulers to stopping.
		scheduler_threads_.join_all();
	}
	
private:
	boost::thread_group scheduler_threads_;
	std::size_t thread_pool_size_;
};
#endif // Later perhaps, for NUMA.

} // namespace core
} // namespace starcounter

//#include "impl/server.hpp"

#endif // STARCOUNTER_CORE_SERVER_HPP
