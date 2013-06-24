//
// posix_thread.hpp
//
// Copyright © 2006-2013 Starcounter AB. All rights reserved.
// StarcounterÆ is a registered trademark of Starcounter AB.
//

#ifndef STARCOUNTER_CORE_THREAD_POSIX_THREAD_HPP
#define STARCOUNTER_CORE_THREAD_POSIX_THREAD_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#include <pthread.h>
#include "../noncopyable.hpp"

namespace starcounter {
namespace core {

class posix_thread : private noncopyable {
public:
	typedef ::pthread_t native_handle_type;
	typedef void* (*start_routine_type)(void*);
	
	posix_thread()
	: thread_(),
	joined_(false) {}
	
	~posix_thread() {
		if (thread_ != 0) {
			std::terminate();
		}
	}
	
	int create(start_routine_type routine, void* arg) {
		return !pthread_create(&thread_, (const pthread_attr_t*) 0, routine, arg);
	}
	
	void join() {
		if (!joined_) {
			std::cout << "::pthread_join()" << std::endl;
			::pthread_join(thread_, 0);
			joined_ = true;
			std::cout << "did join!" << std::endl;
		}
	}
	
	native_handle_type native_handle() {
		return thread_;
	}
	
private:
	native_handle_type thread_;
	bool joined_;
};

} // namespace core
} // namespace starcounter

#endif // STARCOUNTER_CORE_THREAD_POSIX_THREAD_HPP
