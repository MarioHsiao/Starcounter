//
// channel.hpp
//
// Copyright © 2006-2012 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#ifndef STARCOUNTER_CORE_CHANNEL_HPP
#define STARCOUNTER_CORE_CHANNEL_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#include <cstddef>
#include <boost/cstdint.hpp>
#include <boost/noncopyable.hpp>
#include "../common/chunk.hpp"
#include "../common/atomic_buffer.hpp"
#include "../common/owner_id.hpp"
#include "../common/scheduler_number.hpp"
#include "../common/client_number.hpp"
#include "../common/scheduler_interface.hpp"
#include "../common/client_interface.hpp"
#include "../common/config_param.hpp"
#include "../common/macro_definitions.hpp"

namespace starcounter {
namespace core {

template<class T, class Alloc = std::allocator<T> >
class channel : private boost::noncopyable {
public:
	// The type of queues used in the channel.
	typedef starcounter::core::bounded_buffer<T, Alloc> queue_type;
	
	// The type of elements stored in the channel.
	typedef typename queue_type::value_type value_type;
	
	// A pointer to an element.
	typedef typename queue_type::pointer pointer;
	
	// A const pointer to the element.
	typedef typename queue_type::const_pointer const_pointer;
	
	// A reference to an element.
	typedef typename queue_type::reference reference;
	
	// A const reference to an element.
	typedef typename queue_type::const_reference const_reference;
	
	// The distance type. (A signed integral type used to represent the distance
	// between two iterators.)
	typedef typename queue_type::difference_type difference_type;
	
	// The size type. (An unsigned integral type that can represent any non-
	// negative value of the container's distance type.)
	typedef typename queue_type::size_type size_type;
	
	// The type of allocator used in the channel.
	typedef typename queue_type::allocator_type allocator_type;
	
	// Helper types
	
	// A type representing the "best" way to pass the value_type to a method.
	typedef typename boost::call_traits<value_type>::param_type param_type;
	
	// A type representing the "best" way to return the value_type from a const
	// method.
	typedef typename boost::call_traits<value_type>::param_type return_type;

	// Construction/Destruction.
	
	explicit channel(size_type capacity, const allocator_type& alloc
	= allocator_type())
	: in(),
	out(),
	scheduler_interface_(0),
	client_interface_(0),
	server_refs_(0),
	is_to_be_released_(false)
#if defined (IPC_HANDLE_CHANNEL_OUT_BUFFER_FULL)
	, overflow_()
#endif // defined (IPC_HANDLE_CHANNEL_OUT_BUFFER_FULL)
	{}
	
	void set_scheduler_interface(scheduler_interface_type* i) {
		scheduler_interface_ = i;
	}
	
	void set_client_interface_as_qword(uint64_t i) {
		client_interface_ = i;
	}
	
	/// Get the scheduler interface. Only works for the client process that
	/// allocated the channel, other processes will get an access violation.
	scheduler_interface_type* scheduler() const {
		return scheduler_interface_;
	}
	
	/// Get the client interface.
	client_interface_type* client() const {
		return reinterpret_cast<client_interface_type*>(client_interface_);
	}
	
	/// Set the scheduler_number.
	void set_scheduler_number(scheduler_number n) {
		scheduler_number_ = n;
	}
	
	/// Get the scheduler_number.
	scheduler_number get_scheduler_number() const {
		return scheduler_number_;
	}
	
	/// Set the client_number.
	void set_client_number(client_number n) {
		client_number_ = n;
	}
	
	/// Get the client_number.
	client_number get_client_number() const {
		return client_number_;
	}
	
	int32_t add_server_ref() {
		return ++server_refs_;
	}
	
	int32_t release_server_ref() {
		return --server_refs_;
	}
	
	int32_t get_server_refs() {
		_mm_mfence();
		return server_refs_;
	}
	
	void set_to_be_released() {
		_mm_sfence();
		is_to_be_released_ = true;
		_mm_sfence();
	}
	
	void clear_is_to_be_released() {
		_mm_sfence();
		is_to_be_released_ = false;
		_mm_sfence();
	}
	
	bool is_to_be_released() const {
		return is_to_be_released_;
	}

public:
	atomic_buffer<T, channel_capacity_bits> in; // 1 << 8 (256) elements.
	atomic_buffer<T, channel_capacity_bits> out; // 1 << 8 (256) elements.
	
	//--------------------------------------------------------------------------
	// PAGE_ALIGN is working better if only the in and out queues are in the
	// channel and everything else that belongs to the channel is put in a
	// separate interface, because the in and out queues will occupy exactly 1
	// KiB each. But this also requires that the in and out queues are aligned
	// on 1 KiB boundary, and preferably that channel[0]'s in queue is aligned
	// on a vm page. This is an important optimization but the priority is to
	// get everything to work properly first so I will not spend time on vm page
	// alignment now.
	//--------------------------------------------------------------------------
	
#if defined (IPC_HANDLE_CHANNEL_OUT_BUFFER_FULL)
	//--------------------------------------------------------------------------
	// The queue here is synchronized enough by the operation of the
	// co-operative scheduler threads. Its purpose is to replaces the other
	// overflow buffer (in scheduler_interface[s]).
	
	class queue {
	public:
		typedef chunk_type::link_type link_type;

		enum {
			link_terminator = chunk_type::link_terminator
		};

		queue()
		: front_(link_terminator),
		back_(link_terminator),
		chunk_(0) {}

		/// set_chunk_ptr() stores the address of the first chunk in an array of chunks,
		/// so that operations on the queue can be done. The address is relative to the thread
		/// operating on the queue, which is a scheduler thread. A client thread cannot
		/// use this queue because the address would be incorrect.
		/**
		 * @param p The address of chunk[0], in the array of chunks.
		 */
		void set_chunk_ptr(chunk_type* p) {
			chunk_ = p;
		}

		bool empty() const {
			return front_ == link_terminator;
		}
		
		/// not_empty() returns true if the queue is not empty, false if the queue is empty.
		/**
		 * @return true if the queue is not empty, false if the queue is empty.
		 */
		bool not_empty() const {
			return front_ != link_terminator;
		}

		/// front() returns a reference to the first element in the queue.
		/**
		 * @return A reference to the first element in the queue.
		 */
		link_type& front() {
			return front_;
		}
		
		/// front() returns a const reference to the first element in the queue.
		/**
		 * @return A const reference to the first element in the queue.
		 */
		const link_type& front() const {
			return front_;
		}

		/// back() returns a reference to the last element in the queue.
		/**
		 * @return A reference to the last element in the queue.
		 */
		link_type& back() {
			return back_;
		}

		/// back() returns a const reference to the last element in the queue.
		/**
		 * @return A const reference to the last element in the queue.
		 */
		const link_type& back() const {
			return back_;
		}

		/// push_back() will push_back chunk(n) at the end of the queue.
		/**
		 * @param n Link to the chunk that shall be pushed into the overflow
		 *		queue.
		 */
		void push(link_type n) {
			if (not_empty()) {
				chunk(back()).set_next(n);
				back_ = n;
			}
			else {
				front_ = n;
				back_ = n;
				//chunk(back()).set_next(link_terminator); // the caller should do this.
			}
		}

		/// pop_front() will pop chunk(n) at the front of the queue.
		/**
		 * @param n A reference to the link that will point to the popped chunk
		 *		if returning true.
		 * @return true if successfully popped an item from the queue,
		 *		false otherwise.
		 */
		void pop() {
			if (not_empty()) {
				link_type temp_front = chunk(front()).get_next();
				chunk(front()).terminate_next();
				front_ = temp_front;
			}

			// The queue is empty, nothing to pop.
		}

		/// print() show the list as a tree. This is only used for debug.
		/// However, when using this, synchronization is needed so that
		/// scheduler's are not modifying the queue while it is printed.
		void print() const {
			for (link_type next = front_; next != link_terminator; next = chunk(back()).extended()) {
				std::cout << "(";
				for (link_type extended = front_; extended != link_terminator; extended = chunk(back()).extended()) {
					std::cout << i << ", ";
				}
				std::cout << "),\n";
			}
		}

	private:
		// Returns a reference to chunk[n] relative to the scheduler process
		// address space, so only the scheduler process that manages this
		// channel can use it.
		chunk_type& chunk(chunk_index n) {
			return chunk_[n];
		}

		link_type front_;
		link_type back_;

		// chunk_ is a pointer relative to the scheduler process address space, so
		// only the scheduler process that manages this channel can use it.
		chunk_type* chunk_;
	};
	
	queue& overflow() {
		return overflow_;
	}

	const queue& overflow() const {
		return overflow_;
	}
	
	// During initialization of the channel, the chunk pointer
	// is set relative to the scheduler process address space, so
	// only the scheduler process that manages this channel can use it.
	void set_chunk_ptr(chunk_type* p) {
		overflow().set_chunk_ptr(p);
	}
#endif // defined (IPC_HANDLE_CHANNEL_OUT_BUFFER_FULL)

private:
	// Here we are already aligned on a cache-line. The owner_id,
	// client_interface_ and scheduler_interface_ are written only when
	// initializing. Otherwise they are read-only, so they shall share the same
	// cache-line.
	
	// scheduler_interface_ is a pointer relative to the client process address
	// space, so only the client process that owns this channel can use it.
	scheduler_interface_type* scheduler_interface_;
	
	// client_interface_ is an uint64_t that holds a pointer value, relative
	// to the database process address space, so only the database process that
	// owns this channel can use it.
	uint64_t client_interface_; // client_interface_type*
	
	// Only read from and written to on the server side. Used to keep track of
	// when a channel can be released if the client terminates unexpectedly.
	int32_t server_refs_;
	
	// Indexes to interfaces.
	scheduler_number scheduler_number_;
	client_number client_number_;
	
	// Flag to indicate that the client no longer uses the channel and the
	// scheduler shall empty the in and out queues and release the channel.
	volatile bool is_to_be_released_;
	
	char cache_line_pad_0_[CACHE_LINE_SIZE -(
	+sizeof(scheduler_interface_type*) // scheduler_interface_
	+sizeof(uint64_t) // client_interface_
	+sizeof(int32_t) // server_refs_
	+sizeof(scheduler_number) // scheduler_number_
	+sizeof(client_number) // client_number_
	+sizeof(bool) // is_to_be_released_
	) % CACHE_LINE_SIZE];

#if defined (IPC_HANDLE_CHANNEL_OUT_BUFFER_FULL)
	queue overflow_;

	char cache_line_pad_1_[CACHE_LINE_SIZE -(
	+sizeof(queue) // overflow_
	) % CACHE_LINE_SIZE];
#endif // defined (IPC_HANDLE_CHANNEL_OUT_BUFFER_FULL)
};

typedef simple_shared_memory_allocator<chunk_index> shm_alloc_for_the_channels2;
typedef channel<chunk_index, shm_alloc_for_the_channels2> channel_type;

} // namespace core
} // namespace starcounter

#endif // STARCOUNTER_CORE_CHANNEL_HPP
