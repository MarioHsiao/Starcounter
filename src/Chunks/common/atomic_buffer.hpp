//
// atomic_buffer.hpp
//
// Copyright © 2006-2013 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

// TODO: Test cache-line align

#ifndef STARCOUNTER_CORE_ATOMIC_BUFFER_HPP
#define STARCOUNTER_CORE_ATOMIC_BUFFER_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#include <cstdint>
#if defined(_MSC_VER) // Windows
# include <intrin.h>
#else
# error Compiler not supported.
#endif // (_MSC_VER)
#include <boost/call_traits.hpp>
#include <boost/bind.hpp>
#include "performance_counter.hpp"
#include "macro_definitions.hpp"

namespace starcounter {
namespace core {

// The implementation of atomic_buffer relies on the rules of x86 and x64 CPU
// memory reordering operations, and is broken on some other architectures.
#if defined(__x86_64__) || defined(__amd64__) || defined(_M_X64) \
|| defined(_M_AMD64) || defined(__i386__) || defined(_M_IX86)

// Template param N expresses the number of elements as 2^N.
// For 64 elements, N = 6; for 4096 elements, N = 12, etc.
template<typename T, int32_t N>
class atomic_buffer {
public:
    // Type definitions.
	typedef T value_type;
	typedef typename boost::call_traits<value_type>::param_type param_type;
    typedef T& reference;
    typedef const T& const_reference;
    typedef int32_t size_type;
	typedef std::size_t spin_count_type;
	
	// Construction/Destruction.
	
	/// Default constructor.
	/**
	 * @throws Nothing.
	 * @par Complexity
	 *      Constant.
	 */
	atomic_buffer()
	: head_(0), tail_(0) {}
	
	/// Push an item to the queue. Retry spin_count times.
	/**
	 * @param item The item of type T to be pushed.
	 * @param spin_count The number of times to re-try to push the item before
	 *		returning.
	 * @return true if the item was pushed to the queue, false otherwise.
	 * @throws Nothing.
	 */
	bool push_front(param_type item, spin_count_type spin_count = 0) {
		int32_t head = head_;
		int32_t next_head = next(head);

		do {
			if (next_head != tail_) {
				elems_[head] = item;
				// Guarantees that every preceding store is globally visible
				// before any subsequent store.
				_mm_sfence();
				head_ = next_head;
				_mm_sfence();
				
				// The item was pushed.
				#if defined (STARCOUNTER_CORE_ATOMIC_BUFFER_PERFORMANCE_COUNTERS)
				++pushed_counter();
				#endif // STARCOUNTER_CORE_ATOMIC_BUFFER_PERFORMANCE_COUNTERS
				return true;
			}
			
			// Inserting a pause instruction into the loop, telling the
			// processor to wait (do nothing) for the amount of time equivalent
			// to this memory access. On processors with Hyper-Threading
			// Technology, this respite enables the other thread to use all of
			// the resources on the physical processor and continue processing.
			// On processors that predate Hyper-Threading Technology, the pause
			// instruction is translated into a no-op, that is, a no-operation
			// instruction, which simply introduces a one instruction delay.
			_mm_pause();
		} while (spin_count--);
		
		// The item was not pushed.
		return false;
	}
	
	/// Pop an item from the queue. Retry spin_count times.
	/**
	 * @param item The item of type T to be popped.
	 * @param spin_count The number of times to re-try to pop the item before
	 *		returning.
	 * @return true if the item was popped from the queue, false otherwise.
	 * @throws Nothing.
	 */
	bool pop_back(value_type* item, spin_count_type spin_count = 0) {
		int32_t tail = tail_;

		do {
			if (tail != head_) {
				*item = elems_[tail];
				_mm_sfence();
				tail_ = next(tail);
				_mm_sfence();
				
				// The item was popped.
				#if defined (STARCOUNTER_CORE_ATOMIC_BUFFER_PERFORMANCE_COUNTERS)
				++popped_counter();
				#endif // STARCOUNTER_CORE_ATOMIC_BUFFER_PERFORMANCE_COUNTERS
				return true;
			}

			_mm_pause();
		} while (spin_count--);
		
		// The item was not popped.
		return false;
	}
	
	/// Try to push an item to the queue.
	/**
	 * @param item The item of type T to be pushed.
	 * @return true if the item was pushed to the queue, false otherwise.
	 * @throws Nothing.
	 */
	bool try_push_front(param_type item) {
		int32_t head = head_;
		int32_t next_head = next(head);

		if (next_head != tail_) {
			elems_[head] = item;
			// Guarantees that every preceding store is
			// globally visible before any subsequent store.
			_mm_sfence();
			head_ = next_head;
			_mm_sfence();

			// The item was pushed.
			#if defined (STARCOUNTER_CORE_ATOMIC_BUFFER_PERFORMANCE_COUNTERS)
			++pushed_counter();
			#endif // STARCOUNTER_CORE_ATOMIC_BUFFER_PERFORMANCE_COUNTERS
			return true;
		}

		// The item was not pushed.
		return false;
	}
	
	/// Try to pop an item from the queue.
	/**
	 * @param item The item of type T to be popped.
	 * @return true if the item was popped from the queue, false otherwise.
	 * @throws Nothing.
	 */
	bool try_pop_back(value_type* item) {
		int32_t tail = tail_;

		if (tail != head_) {
			*item = elems_[tail];
			_mm_sfence();
			tail_ = next(tail);
			_mm_sfence();

			// The item was popped.
			#if defined (STARCOUNTER_CORE_ATOMIC_BUFFER_PERFORMANCE_COUNTERS)
			++popped_counter();
			#endif // STARCOUNTER_CORE_ATOMIC_BUFFER_PERFORMANCE_COUNTERS
			return true;
		}

		// The item was not popped.
		return false;
	}

	bool has_more() {
		return tail_ != head_;
	}
	
	// Size is constant.
	static size_type size() {
		return 1 << N;
	}
	
	static size_type max_size() {
		return 1 << N;
	}
	
	enum {
		static_size = 1 << N
	};
	
	#if defined (STARCOUNTER_CORE_ATOMIC_BUFFER_PERFORMANCE_COUNTERS)
	/// TODO: Remove performance counters, only used for debug.
	/// Performance counters counts how many objects have been pushed and popped
	/// in this atomic_buffer.
	performance_counter& pushed_counter() {
		return pushed_counter_;
	}
	
	performance_counter& popped_counter() {
		return popped_counter_;
	}
	#endif // STARCOUNTER_CORE_ATOMIC_BUFFER_PERFORMANCE_COUNTERS

private:
	int32_t next(int32_t current) const {
		return (current +1) & ((1 << N) -1);
	}
	
	T elems_[1 << N]; // Fixed-size array of elements of type T.
	volatile int32_t head_;
	char pad_cache_line_0_[CACHE_LINE_SIZE
	-sizeof(int32_t) // head_
	];

	volatile int32_t tail_;
	char pad_cache_line_1_[CACHE_LINE_SIZE
	-sizeof(int32_t) // tail_
	];

	#if defined (STARCOUNTER_CORE_ATOMIC_BUFFER_PERFORMANCE_COUNTERS)
	/// Performance counters counts how many objects have been pushed and popped
	/// in this atomic_buffer.
	performance_counter pushed_counter_;
	char pad_cache_line_2_[CACHE_LINE_SIZE
	-sizeof(performance_counter) // pushed_counter_
	];

	performance_counter popped_counter_;
	char pad_cache_line_3_[CACHE_LINE_SIZE
	-sizeof(performance_counter) // popped_counter_
	];

	#endif // STARCOUNTER_CORE_ATOMIC_BUFFER_PERFORMANCE_COUNTERS
};

// Template specialization for 256 elements, N = 8. This case should improve
// performance since we don't need to use masking, because an uint8_t is used.
// However, I could not measure any performance improvement.
template<typename T>
class atomic_buffer<T, 8> {
public:
     // Type definitions.
	typedef T value_type;
	typedef typename boost::call_traits<value_type>::param_type param_type;
    typedef T& reference;
    typedef const T& const_reference;
    typedef int32_t size_type;
	typedef std::size_t spin_count_type;
	
	// Construction/Destruction.
	
	/// Default constructor.
	/**
	 * @throws Nothing.
	 * @par Complexity
	 *		Constant.
	 */
	atomic_buffer()
	: head_(0), tail_(0) {}
	
	/// Push an item to the queue. Retry spin_count times.
	/**
	 * @param item The item of type T to be pushed.
	 * @param spin_count The number of times to re-try to push the item before
	 *		returning.
	 * @return true if the item was pushed to the queue, false otherwise.
	 * @throws Nothing.
	 */
	bool push_front(param_type item, spin_count_type spin_count = 0) {
		int32_t head = head_;
		int32_t next_head = next(head);

		do {
			if (next_head != tail_) {
				elems_[head] = item;
				// Guarantees that every preceding store is
				// globally visible before any subsequent store.
				_mm_sfence();
				head_ = next_head;
				_mm_sfence();
				
				// The item was pushed.
				#if defined (STARCOUNTER_CORE_ATOMIC_BUFFER_PERFORMANCE_COUNTERS)
				++pushed_counter();
				#endif // STARCOUNTER_CORE_ATOMIC_BUFFER_PERFORMANCE_COUNTERS
				return true;
			}
			
			// Inserting a pause instruction into the loop, telling the
			// processor to wait (do nothing) for the amount of time
			// equivalent to this memory access. On processors with Hyper-
			// Threading Technology, this respite enables the other thread to
			// use all of the resources on the physical processor and continue
			// processing. On processors that predate Hyper-Threading
			// Technology, the pause instruction is translated into a no-op,
			// that is, a no-operation instruction, which simply introduces a
			// one instruction delay.
			_mm_pause();
		} while (spin_count--);
		
		// The item was not pushed.
		return false;
	}
	
	/// Pop an item from the queue. Retry spin_count times.
	/**
	 * @param item The item of type T to be popped.
	 * @param spin_count The number of times to re-try to pop the item before
	 *		returning.
	 * @return true if the item was popped from the queue, false otherwise.
	 * @throws Nothing.
	 */
	bool pop_back(value_type* item, spin_count_type spin_count = 0) {
		int32_t tail = tail_;

		do {
			if (tail != head_) {
				*item = elems_[tail];
				_mm_sfence();
				tail_ = next(tail);
				_mm_sfence();
				
				// The item was popped.
				#if defined (STARCOUNTER_CORE_ATOMIC_BUFFER_PERFORMANCE_COUNTERS)
				++popped_counter();
				#endif // STARCOUNTER_CORE_ATOMIC_BUFFER_PERFORMANCE_COUNTERS
				return true;
			}

			_mm_pause();
		} while (spin_count--);
		
		// The item was not popped.
		return false;
	}
	
	/// Try to push an item to the queue.
	/**
	 * @param item The item of type T to be pushed.
	 * @return true if the item was pushed to the queue, false otherwise.
	 * @throws Nothing.
	 */
	bool try_push_front(param_type item) {
		int32_t head = head_;
		int32_t next_head = next(head);

		if (next_head != tail_) {
			elems_[head] = item;
			// Guarantees that every preceding store is
			// globally visible before any subsequent store.
			_mm_sfence();
			head_ = next_head;
			_mm_sfence();
			
			// The item was pushed.
			#if defined (STARCOUNTER_CORE_ATOMIC_BUFFER_PERFORMANCE_COUNTERS)
			++pushed_counter();
			#endif // STARCOUNTER_CORE_ATOMIC_BUFFER_PERFORMANCE_COUNTERS
			return true;
		}

		// The item was not pushed.
		return false;
	}
	
	/// Try to pop an item from the queue.
	/**
	 * @param item The item of type T to be popped.
	 * @return true if the item was popped from the queue, false otherwise.
	 * @throws Nothing.
	 */
	bool try_pop_back(value_type* item) {
		int32_t tail = tail_;

		if (tail != head_) {
			*item = elems_[tail];
			_mm_sfence();
			tail_ = next(tail);
			_mm_sfence();
			
			// The item was popped.
			#if defined (STARCOUNTER_CORE_ATOMIC_BUFFER_PERFORMANCE_COUNTERS)
			++popped_counter();
			#endif // STARCOUNTER_CORE_ATOMIC_BUFFER_PERFORMANCE_COUNTERS
			return true;
		}

		// The item was not popped.
		return false;
	}

	bool has_more() {
		return tail_ != head_;
	}

	// Size is constant.
	static size_type size() {
		return 1 << 8;
	}

	static size_type max_size() {
		return 1 << 8;
	}
	
	enum {
		static_size = 1 << 8
	};

	#if defined (STARCOUNTER_CORE_ATOMIC_BUFFER_PERFORMANCE_COUNTERS)
	/// TODO: Remove performance counters, only used for debug.
	/// Performance counters counts how many objects have been pushed and popped
	/// in this atomic_buffer.
	performance_counter& pushed_counter() {
		return pushed_counter_;
	}
	
	performance_counter& popped_counter() {
		return popped_counter_;
	}
	#endif // STARCOUNTER_CORE_ATOMIC_BUFFER_PERFORMANCE_COUNTERS
	
private:
	uint8_t next(uint8_t current) const {
		return current +1;
	}
	
	T elems_[1 << 8]; // Fixed-size array of elements of type T.
	volatile int32_t head_;
	char pad_cache_line_0_[CACHE_LINE_SIZE
	-sizeof(int32_t) // head_
	];

	volatile int32_t tail_;
	char pad_cache_line_1_[CACHE_LINE_SIZE
	-sizeof(int32_t) // tail_
	];

	#if defined (STARCOUNTER_CORE_ATOMIC_BUFFER_PERFORMANCE_COUNTERS)
	/// Performance counters counts how many objects have been pushed and popped
	/// in this atomic_buffer.
	performance_counter pushed_counter_;
	char pad_cache_line_2_[CACHE_LINE_SIZE
	-sizeof(performance_counter) // pushed_counter_
	];

	performance_counter popped_counter_;
	char pad_cache_line_3_[CACHE_LINE_SIZE
	-sizeof(performance_counter) // popped_counter_
	];

	#endif // STARCOUNTER_CORE_ATOMIC_BUFFER_PERFORMANCE_COUNTERS
};

#endif // defined(__x86_64__) || defined (__amd64__) || defined (_M_X64) \
// || defined(_M_AMD64) || defined(__i386__) || defined (_M_IX86)

} // namespace core
} // namespace starcounter

#include "impl/atomic_buffer.hpp"

#endif // STARCOUNTER_CORE_ATOMIC_BUFFER_HPP
