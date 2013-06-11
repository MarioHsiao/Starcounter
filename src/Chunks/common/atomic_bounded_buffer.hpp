//
// atomic_bounded_buffer.hpp
//
// Copyright © 2006-2013 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

// TODO: Test cache-line align

#ifndef STARCOUNTER_CORE_ATOMIC_BOUNDED_BUFFER_HPP
#define STARCOUNTER_CORE_ATOMIC_BOUNDED_BUFFER_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#include <cstdint>

#if defined(_MSC_VER) // Windows
# include <intrin.h>
// Declaring interlocked functions for use as intrinsics.
# pragma intrinsic (_InterlockedIncrement)
# pragma intrinsic (_InterlockedDecrement)
#else 
# error Compiler not supported.
#endif // (_MSC_VER)

#include <boost/thread/mutex.hpp>
#include <boost/thread/condition.hpp>
#include <boost/thread/thread.hpp>
#include <boost/call_traits.hpp>
#include <boost/bind.hpp>
#include "macro_definitions.hpp"

namespace starcounter {
namespace core {

// Template param N expresses the number of elements as 2^N.
// For 64 elements, N = 6; for 4096 elements, N = 12, and so on.
// There are template specializations for N = 8 and N = 16, so if
// changing code in the template it may need to be changed in the
// specializations also.
template<typename T, int32_t N>
class atomic_bounded_buffer {
public:
    // type definitions
	typedef T value_type;
	typedef typename boost::call_traits<value_type>::param_type param_type;
    typedef T& reference;
    typedef const T& const_reference;
    typedef int32_t size_type;
	
	atomic_bounded_buffer()
	: head_(0), tail_(0), unread_(0) {}

	void push_front(param_type item) {
		while (!try_push_front(item)) {
			// TODO: spin for a while and then block.

			// The loop variable cannot change faster than the memory bus can
			// update it. Hence, there is no benefit to pre-execute the loop
			// faster than the time needed for a memory refresh. By inserting a
			// pause instruction into a loop, the programmer tells the processor
			// to wait (literally to do nothing) for the amount of time equivalent
			// to this memory access. On processors with Hyper-Threading Technology,
			// this respite enables the other thread to use all of the resources
			// on the physical processor and continue processing.
			// On processors that predate Hyper-Threading Technology, the pause
			// instruction is translated into a no-op, that is, a no-operation
			// instruction, which simply introduces a one instruction delay.
			_mm_pause();
		}
	}

	void pop_back(value_type* item) {
		while (!try_pop_back(item)) {
			// TODO: spin for a while and then block.
			_mm_pause();
		}
	}

	bool try_push_front(param_type item) {
		boost::interprocess::scoped_lock
		<boost::interprocess::interprocess_mutex> lock(mutex_);

		// Not using any synchronization to indicate that the queue is not full.
		int32_t head = head_;
		int32_t next_head = next(head);

		if (next_head != tail_) {
			elems_[head] = item;
			// Guarantees that every preceding store is
			// globally visible before any subsequent store.
			_mm_sfence();
			head_ = next_head;
			_mm_sfence();
			_InterlockedIncrement(&unread_);
			return true;
		}
		return false;
	}

	bool try_pop_back(value_type* item) {
		int32_t tail = tail_;
		if (tail != head_) {
			*item = elems_[tail];
			_mm_sfence();
			tail_ = next(tail);
			_mm_sfence();
			_InterlockedDecrement(&unread_);
			_mm_sfence();
			// Not using any synchronization to indicate that the queue is not full.
			return true;
		}
		return false;
	}

	size_type size() const {
		return unread_;
	}

	static size_type capacity() {
		return 1 << N;
	}

	bool is_not_full() const {
		return unread_ < capacity();
	}

private:
	int32_t next(int32_t current) const {
		return (current +1) & ((1 << N) -1);
	}
	
	T elems_[1 << N]; // Fixed-size array of elements of type T.
	volatile size_type head_;
	char pad_0_[CACHE_LINE_SIZE -sizeof(size_type)];
	volatile size_type tail_;
	char pad_1_[CACHE_LINE_SIZE -sizeof(size_type)];
	/*volatile*/ long int unread_;
	char pad_2_[CACHE_LINE_SIZE -sizeof(size_type)];

	// Mutex to protect access to the queue
	boost::interprocess::interprocess_mutex mutex_;
};

// Template specialization for 256 elements, N = 8. This case should improve
// performance since we don't need to use masking, because an uint8_t is used.
// However, I could not measure any performance improvement.
template<typename T>
class atomic_bounded_buffer<T, 8> {
public:
    // type definitions
	typedef T value_type;
	typedef typename boost::call_traits<value_type>::param_type param_type;
    typedef T& reference;
    typedef const T& const_reference;
    typedef int32_t size_type;
	
	explicit atomic_bounded_buffer()
	: head_(0), tail_(0), unread_(0) {}

	void push_front(param_type item) {
		while (!try_push_front(item)) {
			// TODO: spin for a while and then block.
			_mm_pause();
		}
	}

	void pop_back(value_type* item) {
		while (!try_pop_back(item)) {
			// TODO: spin for a while and then block.
			_mm_pause();
		}
	}

	bool try_push_front(param_type item) {
		boost::interprocess::scoped_lock
		<boost::interprocess::interprocess_mutex> lock(mutex_);

		// Not using any synchronization to indicate that the queue is not full.
		int32_t head = head_;
		int32_t next_head = next(head);
		
		if (next_head != tail_) {
			elems_[head] = item;
			// Guarantees that every preceding store is
			// globally visible before any subsequent store.
			_mm_sfence();
			head_ = next_head;
			_mm_sfence();
			_InterlockedIncrement(&unread_);
			return true;
		}
		return false;
	}

	bool try_pop_back(value_type* item) {
		int32_t tail = tail_;
		if (tail != head_) {
			*item = elems_[tail];
			_mm_sfence();
			tail_ = next(tail);
			_mm_sfence();
			_InterlockedDecrement(&unread_);
			_mm_sfence();
			// Not using any synchronization to indicate that the queue is not full.
			return true;
		}
		return false;
	}

	bool has_more() {
		return (tail_ != head_);
	}

	size_type size() const {
		return unread_;
	}

	static size_type capacity() {
		return 1 << 8;
	}

	bool is_not_full() const {
		return unread_ < capacity();
	}

private:
	uint8_t next(uint8_t current) const {
		return current +1;
	}
	
	T elems_[1 << 8]; // Fixed-size array of elements of type T.

	volatile size_type head_;
	char pad_0_[CACHE_LINE_SIZE -sizeof(size_type)];
	volatile size_type tail_;
	char pad_1_[CACHE_LINE_SIZE -sizeof(size_type)];
	/*volatile*/ long int unread_;
	char pad_2_[CACHE_LINE_SIZE -sizeof(size_type)];
	
	// Mutex to protect access to the queue
	boost::interprocess::interprocess_mutex mutex_;
};

// Template specialization for 65536 elements, N = 16.
// This case should improve performance since we don't need to use masking, because
// an uint16_t is used. However, I could not measure any performance improvement.
template<typename T>
class atomic_bounded_buffer<T, 16> {
public:
    // type definitions
	typedef T value_type;
	typedef typename boost::call_traits<value_type>::param_type param_type;
    typedef T& reference;
    typedef const T& const_reference;
    typedef int32_t size_type;
	
	explicit atomic_bounded_buffer()
	: head_(0), tail_(0), unread_(0) {}

	void push_front(param_type item) {
		while (!try_push_front(item)) {
			// TODO: spin for a while and then block.
			_mm_pause();
		}
	}

	void pop_back(value_type* item) {
		while (!try_pop_back(item)) {
			// TODO: spin for a while and then block.
			_mm_pause();
		}
	}

	bool try_push_front(param_type item) {
		boost::interprocess::scoped_lock
		<boost::interprocess::interprocess_mutex> lock(mutex_);

		// Not using any synchronization to indicate that the queue is not full.
		int32_t head = head_;
		int32_t next_head = next(head);

		if (next_head != tail_) {
			elems_[head] = item;
			// Guarantees that every preceding store is
			// globally visible before any subsequent store.
			_mm_sfence();
			head_ = next_head;
			_mm_sfence();
			_InterlockedIncrement(&unread_);
			return true;
		}
		return false;
	}

	bool try_pop_back(value_type* item) {
		int32_t tail = tail_;
		if (tail != head_) {
			*item = elems_[tail];
			_mm_sfence();
			tail_ = next(tail);
			_mm_sfence();
			_InterlockedDecrement(&unread_);
			_mm_sfence();
			// Not using any synchronization to indicate that the queue is not full.
			return true;
		}
		return false;
	}

	size_type size() const {
		return unread_;
	}

	static size_type capacity() {
		return 1 << 16;
	}

	bool is_not_full() const {
		return unread_ < capacity();
	}

private:
	uint16_t next(uint16_t current) const {
		return current +1;
	}
	
	T elems_[1 << 16]; // Fixed-size array of elements of type T.
	volatile size_type head_;
	char pad_0_[CACHE_LINE_SIZE -sizeof(size_type)];
	volatile size_type tail_;
	char pad_1_[CACHE_LINE_SIZE -sizeof(size_type)];
	/*volatile*/ long int unread_;
	char pad_2_[CACHE_LINE_SIZE -sizeof(size_type)];
	
	// Mutex to protect access to the queue
	boost::interprocess::interprocess_mutex mutex_;
};

} // namespace core
} // namespace starcounter

//#include "impl/atomic_bounded_buffer.hpp"

#endif // STARCOUNTER_CORE_ATOMIC_BOUNDED_BUFFER_HPP
