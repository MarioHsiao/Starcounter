//
// shared_memory_manager.hpp
//
// Copyright © 2006-2011 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#ifndef STARCOUNTER_CORE_SHARED_MEMORY_MANAGER_HPP
#define STARCOUNTER_CORE_SHARED_MEMORY_MANAGER_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#include <cstddef>
#include <memory>
#include <boost/circular_buffer.hpp>
#include <boost/interprocess/sync/interprocess_mutex.hpp>
#include <boost/interprocess/sync/interprocess_condition.hpp>
#include <boost/interprocess/sync/scoped_lock.hpp>
#include <boost/interprocess/shared_memory_object.hpp>
#include <boost/interprocess/mapped_region.hpp>
#include <boost/thread/thread.hpp>
#include <boost/call_traits.hpp>
#include <boost/bind.hpp>
#include "macro_definitions.hpp"

namespace starcounter {
namespace core {

class shared_memory_manager {
public:
	shared_memory_manager()
	: is_initialized_(false) {}
	
	// All processes that intend to use the shared memory must first call this.
	void wait_until_shared_memory_is_initialized() {
		boost::interprocess::scoped_lock<boost::interprocess
		::interprocess_mutex> lock(mutex_);

		initialized_.wait(lock, boost::bind(&shared_memory_manager
		::is_initialized, this));
	}

	// When the shared memory is initialized, the initializer calls this:
	void notify_all_that_shared_memory_is_initialized() {
		boost::interprocess::scoped_lock<boost::interprocess
		::interprocess_mutex> lock(mutex_);

		is_initialized_ = true;
		lock.unlock();
		initialized_.notify_all();
	}

private:
	bool is_initialized() const {
		return is_initialized_;
	}

#if defined (IPC_SHARED_MEMORY_MANAGER_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
	boost::interprocess::interprocess_mutex mutex_;
	boost::interprocess::interprocess_condition initialized_;
	bool is_initialized_;
	
#else // !defined (IPC_SHARED_MEMORY_MANAGER_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
	boost::interprocess::interprocess_mutex mutex_;
	boost::interprocess::interprocess_condition initialized_;
	bool is_initialized_;
#endif // defined (IPC_SHARED_MEMORY_MANAGER_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
};

} // namespace core
} // namespace starcounter

//#include "impl/shared_memory_manager.hpp"

#endif // STARCOUNTER_CORE_SHARED_MEMORY_MANAGER_HPP
