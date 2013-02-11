//
// impl/shared_chunk_pool.hpp
//
// Copyright � 2006-2012 Starcounter AB. All rights reserved.
// Starcounter� is a registered trademark of Starcounter AB.
//
// Implementation of class shared_chunk_pool.
//

#ifndef STARCOUNTER_CORE_IMPL_SHARED_CHUNK_POOL_HPP
#define STARCOUNTER_CORE_IMPL_SHARED_CHUNK_POOL_HPP

// Implementation

namespace starcounter {
namespace core {

template<class T, class Alloc>
inline shared_chunk_pool<T, Alloc>::shared_chunk_pool(const char* segment_name,
size_type buffer_capacity, const allocator_type& alloc)
: container_(buffer_capacity, alloc), unread_(0) {
}

template<class T, class Alloc>
inline typename shared_chunk_pool<T, Alloc>::size_type
shared_chunk_pool<T, Alloc>::size() const {
	return unread_;
}

template<class T, class Alloc>
inline typename shared_chunk_pool<T, Alloc>::size_type
shared_chunk_pool<T, Alloc>::capacity() const {
	return container_.capacity();
}

#if 0
template<class T, class Alloc>
inline bool shared_chunk_pool<T, Alloc>::empty() const {
	return unread_ == 0;
}

template<class T, class Alloc>
inline bool shared_chunk_pool<T, Alloc>::full() const {
	return unread_ == capacity();
}
#endif

// For clients and schedulers.
#if defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)
template<class T, class Alloc>
inline bool shared_chunk_pool<T, Alloc>::acquire_linked_chunks(chunk_type*
chunk_base, chunk_index& head, std::size_t size, client_interface_type*
client_interface_ptr, smp::spinlock::milliseconds timeout) { /// "A"
	std::size_t chunks_to_acquire = (size +chunk_type::static_data_size -1)
	/ chunk_type::static_data_size;
	
	// No recovery is done here in IPC version 1.0 so not locking with owner_id.
	smp::spinlock::scoped_lock lock(spinlock(), timeout -timeout.tick_count());

	if (!lock.owns()) {
		// The timeout_milliseconds time period has elapsed. Failed to acquire
		// linked chunks.
		return false;
	}
	
	// Check if enough space is available, assuming it is.
	if (chunks_to_acquire <= this->size()) {
		// Enough space is available, start linking chunks together.
		chunk_index prev;
		chunk_index current;
		current = container_[--unread_];
		_mm_mfence(); // TODO: Figure if _mm_sfence() is enough.
		client_interface_ptr->set_chunk_flag(current);
		head = current;
		
		for (std::size_t i = 1; i < chunks_to_acquire; ++i) {
			prev = current;
			current = container_[--unread_];
			_mm_mfence(); // TODO: Figure if _mm_mfence() is enough/required.
			
			// This must never occur before the pop_back() above, because if
			// it occurs before pop_back() and the client process terminates
			// unexpectedly (crashes), then the clean up will be messed up
			// because of duplicates. Only way to avoid that would be if the
			// shared_chunk_pool used a bit map in conjunction with the queue,
			// to mark free chunks, because then duplicates could not appear
			// obviously.
			client_interface_ptr->set_chunk_flag(current);
			chunk_base[prev].set_link(current);
		}
		
		// Terminate the last chunk.
		chunk_base[current].terminate_link();
		
		// Notify that the queue is not full.
		lock.unlock();

		// Successfully acquired the chunks and linked them.
		return true;
    }
    else {
		// Not enough space available. Failed to acquire linked chunks.
		return false;
	}
}

#else // !defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)
template<class T, class Alloc>
inline bool shared_chunk_pool<T, Alloc>::acquire_linked_chunks(chunk_type*
chunk_base, chunk_index& head, std::size_t size, client_interface_type*
client_interface_ptr, uint32_t timeout_milliseconds) {
	std::size_t chunks_to_acquire = (size +chunk_type::static_data_size -1)
	/ chunk_type::static_data_size;
	
	// The timeout is used multiple times below, while time passes, so all
	// synchronization must be completed before the timeout_milliseconds
	// time period has elapsed.
	const boost::system_time timeout = boost::posix_time::microsec_clock
	::universal_time() +boost::posix_time::milliseconds(timeout_milliseconds);
	
	boost::interprocess::scoped_lock<boost::interprocess::interprocess_mutex>
	lock(mutex_, timeout);
	
	if (!lock.owns()) {
		// The timeout_milliseconds time period has elapsed. Failed to acquire
		// linked chunks.
		return false;
	}
	
	// Check if enough space is available, assuming it is.
	if (chunks_to_acquire <= this->size()) {
		// Enough space is available, start linking chunks together.
		chunk_index prev;
		chunk_index current;
		current = container_[--unread_];
		_mm_mfence(); // TODO: Figure if _mm_sfence() is enough.
		client_interface_ptr->set_chunk_flag(current);
		
		///Not chunk_base[head].set_link(current); which was a bug.
		head = current;
		
		for (std::size_t i = 1; i < chunks_to_acquire; ++i) {
			prev = current;
			current = container_[--unread_];
			
			_mm_mfence(); // TODO: Figure if _mm_mfence() is enough/required.
			
			// This must never occur before the pop_back() above, because if
			// it occurs before pop_back() and the client process terminates
			// unexpectedly (crashes), then the clean up will be messed up
			// because of duplicates. Only way to avoid that would be if the
			// shared_chunk_pool used a bit map in conjunction with the queue,
			// to mark free chunks, because then duplicates could not appear
			// obviously.
			client_interface_ptr->set_chunk_flag(current);
			chunk_base[prev].set_link(current);
		}
		
		// Terminate the last chunk.
		chunk_base[current].terminate_link();
		
		// Notify that the queue is not full.
		lock.unlock();
		not_full_condition_.notify_one();
		
		// Successfully acquired the chunks and linked them.
		return true;
    }
    else {
		// Not enough space available. Failed to acquire linked chunks.
		return false;
	}
}
#endif // defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)

//------------------------------------------------------------------------------
#if defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)
template<class T, class Alloc>
inline bool shared_chunk_pool<T, Alloc>::acquire_linked_chunks_counted(chunk_type*
chunk_base, chunk_index& head, std::size_t num_chunks_to_acquire, client_interface_type*
client_interface_ptr, smp::spinlock::milliseconds timeout) { /// "B"
	// No recovery is done here in IPC version 1.0 so not locking with owner_id.
	smp::spinlock::scoped_lock lock(spinlock(), timeout -timeout.tick_count());
	
	if (!lock.owns()) {
		// The timeout_milliseconds time period has elapsed. Failed to acquire
		// linked chunks.
		return false;
	}
	
	// Check if enough space is available, assuming it is.
	if (num_chunks_to_acquire <= this->size()) {
		// Enough space is available, start linking chunks together.
		chunk_index prev;
		chunk_index current;
		current = container_[--unread_];
		_mm_mfence(); // TODO: Figure if _mm_sfence() is enough.
		client_interface_ptr->set_chunk_flag(current);
		
		///Not chunk_base[head].set_link(current); which was a bug.
		head = current;
		
		for (std::size_t i = 1; i < num_chunks_to_acquire; ++i) {
			prev = current;
			current = container_[--unread_];
			
			_mm_mfence(); // TODO: Figure if _mm_mfence() is enough/required.
			
			// This must never occur before the pop_back() above, because if
			// it occurs before pop_back() and the client process terminates
			// unexpectedly (crashes), then the clean up will be messed up
			// because of duplicates. Only way to avoid that would be if the
			// shared_chunk_pool used a bit map in conjunction with the queue,
			// to mark free chunks, because then duplicates could not appear
			// obviously.
			client_interface_ptr->set_chunk_flag(current);
			chunk_base[prev].set_link(current);
		}
		
		// Terminate the last chunk.
		chunk_base[current].terminate_link();
		lock.unlock();

		// Successfully acquired the chunks and linked them.
		return true;
    }
    else {
		// Not enough space available. Failed to acquire linked chunks.
		return false;
	}
}

#else // !defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)
template<class T, class Alloc>
inline bool shared_chunk_pool<T, Alloc>::acquire_linked_chunks_counted(chunk_type*
chunk_base, chunk_index& head, std::size_t num_chunks_to_acquire, client_interface_type*
client_interface_ptr, uint32_t timeout_milliseconds) {
	// The timeout is used multiple times below, while time passes, so all
	// synchronization must be completed before the timeout_milliseconds
	// time period has elapsed.
	const boost::system_time timeout = boost::posix_time::microsec_clock
	::universal_time() +boost::posix_time::milliseconds(timeout_milliseconds);
	
	boost::interprocess::scoped_lock<boost::interprocess::interprocess_mutex>
	lock(mutex_, timeout);
	
	if (!lock.owns()) {
		// The timeout_milliseconds time period has elapsed. Failed to acquire
		// linked chunks.
		return false;
	}
	
	// Check if enough space is available, assuming it is.
	if (num_chunks_to_acquire <= this->size()) {
		// Enough space is available, start linking chunks together.
		chunk_index prev;
		chunk_index current;
		current = container_[--unread_];
		_mm_mfence(); // TODO: Figure if _mm_sfence() is enough.
		client_interface_ptr->set_chunk_flag(current);
		
		///Not chunk_base[head].set_link(current); which was a bug.
		head = current;
		
		for (std::size_t i = 1; i < num_chunks_to_acquire; ++i) {
			prev = current;
			current = container_[--unread_];
			
			_mm_mfence(); // TODO: Figure if _mm_mfence() is enough/required.
			
			// This must never occur before the pop_back() above, because if
			// it occurs before pop_back() and the client process terminates
			// unexpectedly (crashes), then the clean up will be messed up
			// because of duplicates. Only way to avoid that would be if the
			// shared_chunk_pool used a bit map in conjunction with the queue,
			// to mark free chunks, because then duplicates could not appear
			// obviously.
			client_interface_ptr->set_chunk_flag(current);
			chunk_base[prev].set_link(current);
		}
		
		// Terminate the last chunk.
		chunk_base[current].terminate_link();
		
		// Notify that the queue is not full.
		lock.unlock();
		not_full_condition_.notify_one();
		
		// Successfully acquired the chunks and linked them.
		return true;
    }
    else {
		// Not enough space available. Failed to acquire linked chunks.
		return false;
	}
}
#endif // defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)

//------------------------------------------------------------------------------
#if defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)
template<class T, class Alloc>
inline bool shared_chunk_pool<T, Alloc>::release_linked_chunks(chunk_type*
chunk_, chunk_index& head, client_interface_type* client_interface_ptr,
smp::spinlock::milliseconds timeout) { /// "C"
	// No recovery is done here in IPC version 1.0 so not locking with owner_id.
	smp::spinlock::scoped_lock lock(spinlock(), timeout -timeout.tick_count());
	
	if (!lock.owns()) {
		// The timeout_milliseconds time period has elapsed. Failed to release
		// the linked chunks.
		return false;
	}
	
	// Release linked chunks.
	chunk_index current = head;
	chunk_index this_chunks_link;
	
	do {
		this_chunks_link = chunk_[current].get_link();
		
		// Only chunks that are not pre-allocated in the channels are released.
		if (current >= channels) {
			// The queue is not full so the item can be pushed.
			container_.push_front(current);
			client_interface_ptr->clear_chunk_flag(current);
			++unread_;
			// Can access the chunk since the lock have not been released yet.
		}
		
		current = this_chunks_link;
	} while (this_chunks_link != chunk_type::link_terminator);
	
	head = current;
	lock.unlock();

	// Successfully released all linked chunks.
	return true;
}

#else // !defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)
template<class T, class Alloc>
inline bool shared_chunk_pool<T, Alloc>::release_linked_chunks(chunk_type*
chunk_, chunk_index& head, client_interface_type* client_interface_ptr, uint32_t
timeout_milliseconds) {
	// The timeout is used multiple times below, while time passes, so all
	// synchronization must be completed before the timeout_milliseconds
	// time period has elapsed.
	const boost::system_time timeout = boost::posix_time::microsec_clock
	::universal_time() +boost::posix_time::milliseconds(timeout_milliseconds);
	
	boost::interprocess::scoped_lock<boost::interprocess::interprocess_mutex>
	lock(mutex_, timeout);
	
	if (!lock.owns()) {
		// The timeout_milliseconds time period has elapsed. Failed to release
		// the linked chunks.
		return false;
	}
	
	// Release linked chunks.
	chunk_index current = head;
	chunk_index this_chunks_link;
	
	do {
		this_chunks_link = chunk_[current].get_link();
		
		// Only chunks that are not pre-allocated in the channels are released.
		if (current >= channels) {
			// The queue is not full so the item can be pushed.
			container_.push_front(current);
			client_interface_ptr->clear_chunk_flag(current);
			++unread_;
			// Can access the chunk since the lock have not been released yet.
		}
		
		current = this_chunks_link;
	} while (this_chunks_link != chunk_type::link_terminator);
	
	head = current;
	lock.unlock();
	not_empty_condition_.notify_one();
	
	// Successfully released all linked chunks.
	return true;
}
#endif // defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)

//------------------------------------------------------------------------------
// For clients:

//------------------------------------------------------------------------------
#if defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)
template<class T, class Alloc>
template<typename U>
inline std::size_t shared_chunk_pool<T, Alloc>::acquire_to_chunk_pool(U&
private_chunk_pool, std::size_t chunks_to_acquire, client_interface_type*
client_interface_ptr, smp::spinlock::milliseconds timeout) { /// "D"
	// No recovery is done here in IPC version 1.0 so not locking with owner_id.
	smp::spinlock::scoped_lock lock(spinlock(), timeout -timeout.tick_count());

	if (!lock.owns()) {
		// The timeout_milliseconds time period has elapsed. Failed to acquire
		// the lock, therefore no chunks could be acquired.
		return 0;
	}
	
	chunk_index current;
	std::size_t acquired;
	
	for (acquired = 0; acquired < chunks_to_acquire; ++acquired) {
		if (is_not_empty()) {
			// The shared_chunk_pool is not empty. Get a chunk_index.
			current = container_[--unread_];
			
			// If the process terminates here, this chunk is leaked and will not
			// be recovered. It is most important to be able to recover all
			// chunks in most cases. TODO: Fixed it later.
			
			// Make sure the CPU (and compiler) don't re-order instructions.
			_mm_mfence();
			
			if (private_chunk_pool.push_front(current)) {
				// Mark the chunk as owned by this client.
				client_interface_ptr->set_chunk_flag(current);
				
				// Havning reached this point the chunk can be recovered.
				
				_mm_mfence(); // Remove if uneccessary.
			}
			else {
				// The private_chunk_pool is full. Put the current chunk_index
				// back.
				container_[unread_++] = current;
				lock.unlock();
				return acquired;
			}
		}
		else {
			// The shared_chunk_pool is empty.
			break;
		}
	}
	
	// Successfully acquired chunks_to_acquire number of chunks.
	lock.unlock();
	return acquired;
}

#else // !defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)
template<class T, class Alloc>
template<typename U>
inline std::size_t shared_chunk_pool<T, Alloc>::acquire_to_chunk_pool(U&
private_chunk_pool, std::size_t chunks_to_acquire, client_interface_type*
client_interface_ptr, uint32_t timeout_milliseconds) {
	const boost::system_time timeout = boost::posix_time::microsec_clock
	::universal_time() +boost::posix_time::milliseconds(timeout_milliseconds);
	
	// The lock must be acquired before the timeout_milliseconds time period has
	// elapsed.
	boost::interprocess::scoped_lock<boost::interprocess::interprocess_mutex>
	lock(mutex_, timeout);
	
	if (!lock.owns()) {
		// The timeout_milliseconds time period has elapsed. Failed to acquire
		// the lock, therefore no chunks could be acquired.
		return 0;
	}
	
	chunk_index current;
	std::size_t acquired;
	
	for (acquired = 0; acquired < chunks_to_acquire; ++acquired) {
		if (is_not_empty()) {
			// The shared_chunk_pool is not empty. Get a chunk_index.
			current = container_[--unread_];
			
			// If the process terminates here, this chunk is leaked and will not
			// be recovered. It is most important to be able to recover all
			// chunks in most cases. TODO: Fixed it later.
			
			// Make sure the CPU (and compiler) don't re-order instructions.
			_mm_mfence();
			
			if (private_chunk_pool.push_front(current)) {
				// Mark the chunk as owned by this client.
				client_interface_ptr->set_chunk_flag(current);
				
				// Havning reached this point the chunk can be recovered.
				
				_mm_mfence(); // Remove if uneccessary.
			}
			else {
				// The private_chunk_pool is full. Put the current chunk_index
				// back.
				container_[unread_++] = current;
				lock.unlock();
				
				if (acquired != 0) {
					not_full_condition_.notify_one();
				}
				
				return acquired;
			}
		}
		else {
			// The shared_chunk_pool is empty.
			break;
		}
	}
	
	// Successfully acquired chunks_to_acquire number of chunks.
	lock.unlock();
	
	if (acquired != 0) {
		not_full_condition_.notify_one();
	}
	
	return acquired;
}
#endif // defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)

//------------------------------------------------------------------------------
#if defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)
template<class T, class Alloc>
template<typename U>
std::size_t shared_chunk_pool<T, Alloc>::release_from_chunk_pool(U&
private_chunk_pool, std::size_t chunks_to_release, client_interface_type*
client_interface_ptr, smp::spinlock::milliseconds timeout) { /// "E"
	// No recovery is done here in IPC version 1.0 so not locking with owner_id.
	smp::spinlock::scoped_lock lock(spinlock(), timeout -timeout.tick_count());
	
	if (!lock.owns()) {
		// The timeout_milliseconds time period has elapsed. Failed to acquire
		// the lock, therefore no chunks could be released.
		return 0;
	}
	
	chunk_index current;
	std::size_t released;
	
	for (released = 0; released < chunks_to_release; ++released) {
		if (private_chunk_pool.pop_back(&current)) {
			// Mark the chunk as not owned by this client.
			client_interface_ptr->clear_chunk_flag(current);
			
			// Make sure the CPU (and compiler) don't re-order instructions.
			_mm_mfence();
			
			// Push the current chunk_index.
			container_.push_front(current);
			++unread_;
		}
		else {
			// The private_chunk_pool is empty.
			break;
		}
	}
	
	lock.unlock();
	return released;
}

#else // !defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)
template<class T, class Alloc>
template<typename U>
std::size_t shared_chunk_pool<T, Alloc>::release_from_chunk_pool(U&
private_chunk_pool, std::size_t chunks_to_release, client_interface_type*
client_interface_ptr, uint32_t timeout_milliseconds) {
	const boost::system_time timeout = boost::posix_time::microsec_clock
	::universal_time() +boost::posix_time::milliseconds(timeout_milliseconds);
	
	// The lock must be acquired before the timeout_milliseconds time period has
	// elapsed.
	boost::interprocess::scoped_lock<boost::interprocess::interprocess_mutex>
	lock(mutex_, timeout);
	
	if (!lock.owns()) {
		// The timeout_milliseconds time period has elapsed. Failed to acquire
		// the lock, therefore no chunks could be released.
		return 0;
	}
	
	chunk_index current;
	std::size_t released;
	
	for (released = 0; released < chunks_to_release; ++released) {
		if (private_chunk_pool.pop_back(&current)) {
			// Mark the chunk as not owned by this client.
			client_interface_ptr->clear_chunk_flag(current);
			
			// Make sure the CPU (and compiler) don't re-order instructions.
			_mm_mfence();
			
			// Push the current chunk_index.
			container_.push_front(current);
			++unread_;
		}
		else {
			// The private_chunk_pool is empty.
			break;
		}
	}
	
	lock.unlock();
	
	if (released != 0) {
		not_empty_condition_.notify_one();
	}

	return released;
}
#endif // defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)

//------------------------------------------------------------------------------
// For schedulers:

//------------------------------------------------------------------------------
#if defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)
template<class T, class Alloc>
template<typename U>
inline std::size_t shared_chunk_pool<T, Alloc>::acquire_to_chunk_pool(U&
private_chunk_pool, std::size_t chunks_to_acquire,
smp::spinlock::milliseconds timeout) { /// "F"
	// No recovery is done here in IPC version 1.0 so not locking with owner_id.
	smp::spinlock::scoped_lock lock(spinlock(), timeout -timeout.tick_count());
	
	if (!lock.owns()) {
		// The timeout_milliseconds time period has elapsed. Failed to acquire
		// the lock, therefore no chunks could be acquired.
		return 0;
	}
	
	chunk_index current;
	std::size_t acquired;
	
	for (acquired = 0; acquired < chunks_to_acquire; ++acquired) {
		if (is_not_empty()) {
			// The shared_chunk_pool is not empty. Get a chunk_index.
			current = container_[--unread_];
			
			// If the process terminates here, this chunk is leaked and will not
			// be recovered. It is most important to be able to recover all
			// chunks in most cases. TODO: Fixed it later.
			
			// Make sure the CPU (and compiler) don't re-order instructions.
			_mm_mfence();
			
			if (private_chunk_pool.push_front(current)) {
				// Havning reached this point the chunk can be recovered.
				_mm_mfence(); // Remove if uneccessary.
			}
			else {
				// The private_chunk_pool is full. Put the current chunk_index
				// back.
				container_[unread_++] = current;
				lock.unlock();
				return acquired;
			}
		}
		else {
			// The shared_chunk_pool is empty.
			break;
		}
	}
	
	// Successfully acquired chunks_to_acquire number of chunks.
	lock.unlock();
	return acquired;
}

#else // !defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)
template<class T, class Alloc>
template<typename U>
inline std::size_t shared_chunk_pool<T, Alloc>::acquire_to_chunk_pool(U&
private_chunk_pool, std::size_t chunks_to_acquire, uint32_t
timeout_milliseconds) {
	const boost::system_time timeout = boost::posix_time::microsec_clock
	::universal_time() +boost::posix_time::milliseconds(timeout_milliseconds);
	
	// The lock must be acquired before the timeout_milliseconds time period has
	// elapsed.
	boost::interprocess::scoped_lock<boost::interprocess::interprocess_mutex>
	lock(mutex_, timeout);
	
	if (!lock.owns()) {
		// The timeout_milliseconds time period has elapsed. Failed to acquire
		// the lock, therefore no chunks could be acquired.
		return 0;
	}
	
	chunk_index current;
	std::size_t acquired;
	
	for (acquired = 0; acquired < chunks_to_acquire; ++acquired) {
		if (is_not_empty()) {
			// The shared_chunk_pool is not empty. Get a chunk_index.
			current = container_[--unread_];
			
			// If the process terminates here, this chunk is leaked and will not
			// be recovered. It is most important to be able to recover all
			// chunks in most cases. TODO: Fixed it later.
			
			// Make sure the CPU (and compiler) don't re-order instructions.
			_mm_mfence();
			
			if (private_chunk_pool.push_front(current)) {
				// Havning reached this point the chunk can be recovered.
				
				_mm_mfence(); // Remove if uneccessary.
			}
			else {
				// The private_chunk_pool is full. Put the current chunk_index
				// back.
				container_[unread_++] = current;
				lock.unlock();
				
				if (acquired != 0) {
					not_full_condition_.notify_one();
				}
				
				return acquired;
			}
		}
		else {
			// The shared_chunk_pool is empty.
			break;
		}
	}
	
	// Successfully acquired chunks_to_acquire number of chunks.
	lock.unlock();
	
	if (acquired != 0) {
		not_full_condition_.notify_one();
	}
	
	return acquired;
}
#endif // defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)

//------------------------------------------------------------------------------
#if defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)
template<class T, class Alloc>
template<typename U>
std::size_t shared_chunk_pool<T, Alloc>::release_from_chunk_pool(U&
private_chunk_pool, std::size_t chunks_to_release,
smp::spinlock::milliseconds timeout) { /// "G"
	// No recovery is done here in IPC version 1.0 so not locking with owner_id.
	smp::spinlock::scoped_lock lock(spinlock(), timeout -timeout.tick_count());
	
	if (!lock.owns()) {
		// The timeout_milliseconds time period has elapsed. Failed to acquire
		// the lock, therefore no chunks could be released.
		return 0;
	}
	
	chunk_index current;
	std::size_t released;
	
	for (released = 0; released < chunks_to_release; ++released) {
		if (private_chunk_pool.pop_back(&current)) {
			// Make sure the CPU (and compiler) don't re-order instructions.
			_mm_mfence();
			
			// Push the current chunk_index.
			container_.push_front(current);
			_mm_mfence();
			++unread_;
			_mm_mfence(); // Remove if uneccessary.
		}
		else {
			// The private_chunk_pool is empty.
			break;
		}
	}
	
	lock.unlock();
	return released;
}

#else // !defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)
template<class T, class Alloc>
template<typename U>
std::size_t shared_chunk_pool<T, Alloc>::release_from_chunk_pool(U&
private_chunk_pool, std::size_t chunks_to_release, uint32_t
timeout_milliseconds) {
	const boost::system_time timeout = boost::posix_time::microsec_clock
	::universal_time() +boost::posix_time::milliseconds(timeout_milliseconds);
	
	// The lock must be acquired before the timeout_milliseconds time period has
	// elapsed.
	boost::interprocess::scoped_lock<boost::interprocess::interprocess_mutex>
	lock(mutex_, timeout);
	
	if (!lock.owns()) {
		// The timeout_milliseconds time period has elapsed. Failed to acquire
		// the lock, therefore no chunks could be released.
		return 0;
	}
	
	chunk_index current;
	std::size_t released;
	
	for (released = 0; released < chunks_to_release; ++released) {
		if (private_chunk_pool.pop_back(&current)) {
			// Make sure the CPU (and compiler) don't re-order instructions.
			_mm_mfence();
			
			// Push the current chunk_index.
			container_.push_front(current);
			_mm_mfence();
			++unread_;
			_mm_mfence(); // Remove if uneccessary.
		}
		else {
			// The private_chunk_pool is empty.
			break;
		}
	}
	
	lock.unlock();
	
	if (released != 0) {
		not_empty_condition_.notify_one();
	}

	return released;
}

#endif // defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)

//------------------------------------------------------------------------------
// For schedulers (and monitor), doing clean up:

//------------------------------------------------------------------------------
#if defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)
template<class T, class Alloc>
bool shared_chunk_pool<T, Alloc>::release_clients_chunks(client_interface_type*
client_interface_ptr, smp::spinlock::milliseconds timeout) { /// "H"
	// No recovery is done here in IPC version 1.0 so not locking with owner_id.
	smp::spinlock::scoped_lock lock(spinlock(), timeout -timeout.tick_count());
	
	if (!lock.owns()) {
		// The timeout_milliseconds time period has elapsed. Failed to acquire
		// the lock, therefore no chunks could be released.
		return false;
	}
	
	chunk_index current;
	resource_map::mask_type mask;
	std::size_t released = 0;
	
	for (std::size_t i = 0; i < resource_map::chunks_mask_size; ++i) {
		for (mask = client_interface_ptr->get_resource_map()
		.get_owned_chunks_mask(i); mask; mask &= mask -1) {
			current = bit_scan_forward(mask);
			current += i << 5;
			
			// Push the current chunk_index.
			container_.push_front(current);
			_mm_mfence();
			++unread_;
			++released;
		}
	}
	
	lock.unlock();
	
	// Clear the resource_map. The channels was already cleared, by the order of
	// releasing resources.
	client_interface_ptr->get_resource_map().clear();
	
	// Successfully released all chunks.
	return true;
}

#else // !defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)
template<class T, class Alloc>
bool shared_chunk_pool<T, Alloc>::release_clients_chunks(client_interface_type*
client_interface_ptr, uint32_t timeout_milliseconds) {
	const boost::system_time timeout = boost::posix_time::microsec_clock
	::universal_time() +boost::posix_time::milliseconds(timeout_milliseconds);
	
	// The lock must be acquired before the timeout_milliseconds time period has
	// elapsed.
	boost::interprocess::scoped_lock<boost::interprocess::interprocess_mutex>
	lock(mutex_, timeout);
	
	if (!lock.owns()) {
		// The timeout_milliseconds time period has elapsed. Failed to acquire
		// the lock, therefore no chunks could be released.
		return false;
	}
	
	chunk_index current;
	resource_map::mask_type mask;
	std::size_t released = 0;
	
	for (std::size_t i = 0; i < resource_map::chunks_mask_size; ++i) {
		for (mask = client_interface_ptr->get_resource_map()
		.get_owned_chunks_mask(i); mask; mask &= mask -1) {
			current = bit_scan_forward(mask);
			current += i << 5;
			
			// Push the current chunk_index.
			container_.push_front(current);
			_mm_mfence();
			++unread_;
			++released;
		}
	}
	
	lock.unlock();
	
	// Clear the resource_map. The channels was already cleared, by the order of
	// releasing resources.
	client_interface_ptr->get_resource_map().clear();
	
	if (released != 0) {
		not_empty_condition_.notify_one();
	}

	// Successfully released all chunks.
	return true;
}
#endif // defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)

//------------------------------------------------------------------------------
#if defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)
template<class T, class Alloc>
void shared_chunk_pool<T, Alloc>::show_linked_chunks(chunk_type* chunk_,
chunk_index head) {
	smp::spinlock::milliseconds timeout(1000);

	// No recovery is done here in IPC version 1.0 so not locking with owner_id.
	smp::spinlock::scoped_lock lock(spinlock(), timeout -timeout.tick_count());

	if (!lock.owns()) {
		// The timeout_milliseconds time period has elapsed. Failed to acquire
		// linked chunks.
		return;
	}
	
	for (chunk_index current = head; current != chunk_type::link_terminator;
	current = chunk_[current].get_link()) {
		chunk_index next = chunk_[current].get_link();
		std::cout << "chunk[" << current << "] links to chunk[" << next << "]" << std::endl;
	}
	std::cout << "end" << std::endl;
}

#else // !defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)
template<class T, class Alloc>
void shared_chunk_pool<T, Alloc>::show_linked_chunks(chunk_type* chunk_,
chunk_index head) {
	// The timeout is used multiple times below, while time passes, so all
	// synchronization must be completed before the timeout_milliseconds
	// time period has elapsed.
	const boost::system_time timeout = boost::posix_time::microsec_clock
	::universal_time() +boost::posix_time::milliseconds(1000);
	
	boost::interprocess::scoped_lock<boost::interprocess::interprocess_mutex>
	lock(mutex_, timeout);
	
	if (!lock.owns()) {
		// The timeout_milliseconds time period has elapsed. Failed to acquire
		// linked chunks.
		return;
	}
	
	for (chunk_index current = head; current != chunk_type::link_terminator;
	current = chunk_[current].get_link()) {
		chunk_index next = chunk_[current].get_link();
		std::cout << "chunk[" << current << "] links to chunk[" << next << "]" << std::endl;
	}
	std::cout << "end" << std::endl;
}
#endif // defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)

//------------------------------------------------------------------------------
#if defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)
template<class T, class Alloc>
inline bool shared_chunk_pool<T, Alloc>::push_front(param_type item, uint32_t
spin_count, smp::spinlock::milliseconds timeout) { /// "I"
	// Spin at most spin_count number of times, and try to push the item...
	for (std::size_t s = 0; s < spin_count; ++s) {
		if (try_push_front(item)) {
			// The item was successfully pushed.
			return true;
		}
	}
	
	// Failed to aquire the lock while spinning. If failing to aquire the lock
	// when trying again now, the thread waits until the queue is not full.
	{
		// The timeout is used multiple times below, while time passes, so all
		// synchronization must be completed before the timeout time period has
		// elapsed.

		// No recovery is done here in IPC version 1.0 so not locking with owner_id.
		smp::spinlock::scoped_lock lock(spinlock(), timeout -timeout.tick_count());
		
		if (!lock.owns()) {
			// The timeout time period has elapsed. Failed to push the item.
			return false;
		}
		
		// The queue is not full because it has enough capacity to hold all
		// items, so the item can be pushed.
		container_.push_front(item);
		++unread_;
		lock.unlock();

		// Successfully pushed the item.
		return true;
	}
	return false;
}

#else // !defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)
template<class T, class Alloc>
inline bool shared_chunk_pool<T, Alloc>::push_front(param_type item, uint32_t
spin_count, uint32_t timeout_milliseconds) {
	// Spin at most spin_count number of times, and try to push the item...
	for (std::size_t s = 0; s < spin_count; ++s) {
		if (try_push_front(item)) {
			// The item was successfully pushed.
			return true;
		}
	}
	
	// Failed to aquire the lock while spinning. If failing to aquire the lock
	// when trying again now, the thread waits until the queue is not full.
	{
		// The timeout is used multiple times below, while time passes, so all
		// synchronization must be completed before the timeout_milliseconds
		// time period has elapsed.
		const boost::system_time timeout = boost::posix_time::microsec_clock
		::universal_time() +boost::posix_time::milliseconds
		(timeout_milliseconds);
		
		boost::interprocess::scoped_lock
		<boost::interprocess::interprocess_mutex> lock(mutex_, timeout);
		
		if (!lock.owns()) {
			// The timeout_milliseconds time period has elapsed. Failed to push
			// the item.
			return false;
		}
		
		// Wait until the queue is not full, or timeout occurs.
		if (not_full_condition_.timed_wait(lock, timeout,
		boost::bind(&shared_chunk_pool<value_type, allocator_type>::is_not_full,
		this)) == true) {
			// The queue is not full so the item can be pushed.
			container_.push_front(item);
			++unread_;
			lock.unlock();
			not_empty_condition_.notify_one();
			// Successfully popped the item.
			return true;
		}
		else {
			// The timeout_milliseconds time period has elapsed. Failed to push
			// the item.
			return false;
		}
	}
}
#endif // defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)

//------------------------------------------------------------------------------
#if defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)
// No pop_back()
#else // !defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)
template<class T, class Alloc>
inline bool shared_chunk_pool<T, Alloc>::pop_back(value_type* item, uint32_t
spin_count, uint32_t timeout_milliseconds) {
	// Spin at most spin_count number of times, and try to pop the item...
	for (std::size_t s = 0; s < spin_count; ++s) {
		if (try_pop_back(item)) {
			// The item has been popped.
			return true;
		}
	}
	
	// Failed to aquire the lock while spinning. If failing to aquire the lock
	// when trying again now, the thread blocks.
	{
		// The timeout is used multiple times below, while time passes, so all
		// synchronization must be completed before the timeout_milliseconds
		// time period has elapsed.
		const boost::system_time timeout = boost::posix_time::microsec_clock
		::universal_time() +boost::posix_time::milliseconds
		(timeout_milliseconds);

		boost::interprocess::scoped_lock
		<boost::interprocess::interprocess_mutex> lock(mutex_, timeout);
		
		if (!lock.owns()) {
			// The timeout_milliseconds time period has elapsed. Failed to pop
			// the item.
			return false;
		}
		
		// Wait until the queue is not empty, or timeout occurs.
		if (not_empty_condition_.timed_wait(lock, timeout,
		boost::bind(&shared_chunk_pool<value_type, allocator_type>::is_not_empty,
		this)) == true) {
			// The queue is not empty so the item can be popped.
			*item = container_[--unread_];
			lock.unlock();
			not_full_condition_.notify_one();
			// Successfully popped the item.
			return true;
		}
		else {
			// The timeout_milliseconds time period has elapsed. Failed to pop
			// the item.
			return false;
		}
	}
}
#endif // defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)

//------------------------------------------------------------------------------
#if defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)
template<class T, class Alloc>
inline bool shared_chunk_pool<T, Alloc>::try_push_front(param_type item) { /// "K"
	smp::spinlock::scoped_lock lock(spinlock(),
	smp::spinlock::scoped_lock::try_to_lock_type());
	
	if (lock.owns()) {
		// If the buffer is full, we shall not block.
		if (unread_ == container_.capacity()) {
			// The buffer is full so we can't push the item.
			lock.unlock();
			return false;
		}
		
		container_.push_front(item);
		++unread_;
		lock.unlock();
		
		// The item was pushed.
		return true;
	}
	// The lock was not aquired so the item was not pushed.
	return false;
}

#else // !defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)
template<class T, class Alloc>
inline bool shared_chunk_pool<T, Alloc>::try_push_front(param_type item) {
	boost::interprocess::scoped_lock<boost::interprocess::interprocess_mutex>
	lock(mutex_, boost::interprocess::try_to_lock);
	
	if (lock.owns()) {
		// If the buffer is full, we shall not block.
		if (unread_ == container_.capacity()) {
			// The buffer is full so we can't push the item.
			lock.unlock();
			return false;
		}
		
		container_.push_front(item);
		++unread_;
		lock.unlock();
		not_empty_condition_.notify_one();
		// The item was pushed.
		return true;
	}
	// The lock was not aquired so the item was not pushed.
	return false;
}
#endif // defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)

//------------------------------------------------------------------------------
#if defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)
/// "L"
#else // !defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)
template<class T, class Alloc>
inline bool shared_chunk_pool<T, Alloc>::try_pop_back(value_type* item) {
	boost::interprocess::scoped_lock<boost::interprocess::interprocess_mutex>
	lock(mutex_, boost::interprocess::try_to_lock);

	if (lock.owns()) {
		// If the buffer is empty, we shall not block.
		if (unread_ == 0) {
			// The buffer is empty so there is no item to pop.
			lock.unlock();
			return false;
		}

		*item = container_[--unread_];
		lock.unlock();
		not_full_condition_.notify_one();
		// The item was popped.
		return true;
	}
	// The lock was not aquired so the item was not popped.
	return false;
}
#endif // defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)

template<class T, class Alloc>
inline bool shared_chunk_pool<T, Alloc>::is_not_empty() const {
	return unread_ > 0;
}

template<class T, class Alloc>
inline bool shared_chunk_pool<T, Alloc>::is_not_full() const {
	return unread_ < container_.capacity();
}

//------------------------------------------------------------------------------
#if defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)
template<class T, class Alloc>
inline const wchar_t* shared_chunk_pool<T, Alloc>::not_empty_notify_name() const {
	return not_empty_notify_name_;
}

template<class T, class Alloc>
inline const wchar_t* shared_chunk_pool<T, Alloc>::not_full_notify_name() const {
	return not_full_notify_name_;
}

template<class T, class Alloc>
inline bool shared_chunk_pool<T, Alloc>::if_locked_with_id_recover_and_unlock
(smp::spinlock::locker_id_type id) {
	if (spinlock().is_locked_with_id(id)) {
		// Recover...NOT! We accept a resource leak in IPC version 1.0.
		// TODO: Fix recovery of chunks in IPC version 2.0.
		spinlock().unlock();
		return true;
	}

	// The shared_chunk_pool was not locked with id. No recovery to be done here.
	return false;
}
#endif // defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)

} // namespace core
} // namespace starcounter

#endif // STARCOUNTER_CORE_IMPL_SHARED_CHUNK_POOL_HPP
