//
// windows_thread.hpp
//
// Copyright © 2006-2013 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#ifndef STARCOUNTER_CORE_THREAD_WINDOWS_THREAD_HPP
#define STARCOUNTER_CORE_THREAD_WINDOWS_THREAD_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#undef WIN32_LEAN_AND_MEAN
#include "../noncopyable.hpp"

namespace starcounter {
namespace core {

class windows_thread : private noncopyable {
public:
	typedef ::HANDLE native_handle_type;
	typedef ::LPTHREAD_START_ROUTINE start_routine_type;
	
	windows_thread()
	: thread_(),
	joined_(false) {}
	
	~windows_thread() {
		if (thread_ != 0) {
			std::terminate();
		}
	}
	
	int create(start_routine_type routine, void* arg) {
		return (thread_ = ::CreateThread(NULL, 0, routine, arg, 0, NULL)) != NULL;
	}
	
	void join() {
		if (!joined_) {
			::WaitForSingleObject(thread_, INFINITE);
			::CloseHandle(thread_);
			joined_ = true;
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

#endif // STARCOUNTER_CORE_THREAD_WINDOWS_THREAD_HPP
