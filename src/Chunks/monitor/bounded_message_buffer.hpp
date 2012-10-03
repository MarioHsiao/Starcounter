//
// bounded_message_buffer.hpp
//
// Copyright © 2006-2011 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//
// The code is a modified version of the Boost bounded_buffer example:
// http://www.boost.org/doc/libs/1_37_0/libs/circular_buffer/doc/circular_buffer.html#boundedbuffer
//
//------------------------------------------------------------------------------
// We already have a bounded_buffer adopted for shared memory which is
// unsuitable for streaming messages from various threads within the monitor
// process. A log_thread outputs the monitor log messages to a file.
//

#ifndef STARCOUNTER_CORE_BOUNDED_MESSAGE_BUFFER_HPP
#define STARCOUNTER_CORE_BOUNDED_MESSAGE_BUFFER_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

// Use -DBOOST_CB_DISABLE_DEBUG or -DNDEBUG flags
#define BOOST_CB_DISABLE_DEBUG

#include <boost/circular_buffer.hpp>
#include <boost/thread/mutex.hpp>
#include <boost/thread/condition.hpp>
#include <boost/thread/thread.hpp>
#include <boost/bind.hpp>

namespace starcounter {
namespace core {

/// Class bounded_message_buffer.
/**
 * @param T The type of the elements stored in the bounded_message_buffer.
 *
 * @par Type Requirements T
 *      The T has to be SGIAssignable (SGI STL defined combination of Assignable
 *      and CopyConstructible), and EqualityComparable and/or LessThanComparable
 *      if the bounded_buffer will be compared with another container.
 */
template<class T>
class bounded_message_buffer {
public:
	typedef boost::circular_buffer<T> container_type;
	typedef typename container_type::size_type size_type;
	typedef typename container_type::value_type value_type;
	typedef typename boost::call_traits<value_type>::param_type param_type;

	explicit bounded_message_buffer(size_type capacity)
	: unread_(0), container_(capacity) {}

	void push_front(param_type item) {
		boost::mutex::scoped_lock lock(mutex_);
		not_full_.wait(lock, boost::bind(&bounded_message_buffer<value_type>
		::is_not_full, this));
		container_.push_front(item);
		++unread_;
		lock.unlock();
		not_empty_.notify_one();
	}

	void pop_back(value_type* item) {
		boost::mutex::scoped_lock lock(mutex_);
		not_empty_.wait(lock, boost::bind(&bounded_message_buffer<value_type>
		::is_not_empty, this));
		*item = container_[--unread_];
		lock.unlock();
		not_full_.notify_one();
	}
	
private:
	// Disabled copy constructor
	bounded_message_buffer(const bounded_message_buffer&);
	
	// Disabled assign operator
	bounded_message_buffer& operator=(const bounded_message_buffer&);

	bool is_not_empty() const {
		return unread_ > 0;
	}
	
	bool is_not_full() const {
		return unread_ < container_.capacity();
	}
	
	size_type unread_;
	container_type container_;
	boost::mutex mutex_;
	boost::condition not_empty_;
	boost::condition not_full_;
};

} // namespace core
} // namespace starcounter

//#include "impl/bounded_message_buffer.hpp"

#endif // STARCOUNTER_CORE_BOUNDED_MESSAGE_BUFFER_HPP
