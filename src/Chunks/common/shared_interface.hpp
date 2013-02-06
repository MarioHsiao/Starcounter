//
// shared_interface.hpp
//
// Copyright � 2006-2012 Starcounter AB. All rights reserved.
// Starcounter� is a registered trademark of Starcounter AB.
// 

#ifndef STARCOUNTER_CORE_SHARED_INTERFACE_HPP
#define STARCOUNTER_CORE_SHARED_INTERFACE_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#include <cstddef>
#include <boost/cstdint.hpp>
#include <memory>
#include <string>
#include <utility>
#include <boost/interprocess/managed_shared_memory.hpp>
#include <boost/interprocess/allocators/allocator.hpp>
#include <boost/call_traits.hpp>
#include <boost/bind.hpp>
#include "../common/client_number.hpp"
#include "../common/circular_buffer.hpp"
#include "../common/bounded_buffer.hpp"
#include "../common/chunk.hpp"
#include "../common/chunk_pool.hpp"
#include "../common/shared_chunk_pool.hpp"
#include "../common/channel.hpp"
#include "../common/channel_number.hpp"
#include "../common/client_number.hpp"
#include "../common/common_scheduler_interface.hpp"
#include "../common/scheduler_interface.hpp"
#include "../common/common_client_interface.hpp"
#include "../common/client_interface.hpp"
#include "../common/scheduler_number.hpp"
#include "../common/owner_id.hpp"
#include "../common/pid_type.hpp"
#include "../common/config_param.hpp"
#include "../common/macro_definitions.hpp"
#include "../common/interprocess.hpp"
#include "../common/name_definitions.hpp"

#include <scerrres.h>

namespace starcounter {
namespace core {

// Exception class.
class shared_interface_exception {
public:
	typedef uint32_t error_code_type;
	
	explicit shared_interface_exception(uint32_t err)
	: err_(err) {}
	
	int error_code() const {
		return err_;
	}
	
private:
	error_code_type err_;
};

/// class shared_interface have all the API functions needed by a client to
/// access all objects in the shared memory segment of a database. A client need
/// one shared_interface per database it uses. A shared_interface cannot be used
/// by a scheduler, as original thought. Schedulers use a similar but different
/// interface - class server_port. However, clients should use shared_interface
/// instead of client_port (which is obsolete.)
class shared_interface {
public:
	/// Default constructor.
	shared_interface();

	/// Constructor.
	/**
	 * @param segment_name is used to open the database shared memory.
	 *		The segment_name has the format:
	 *		<DATABASE_NAME_PREFIX>_<DATABASE_NAME>_<SEQUENCE_NUMBER>
	 */
	explicit shared_interface(std::string segment_name, std::string
	monitor_interface_name, pid_type pid, owner_id oid = owner_id
	(owner_id::none));
	
#if defined(INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC) // Use Windows Events.
	/// Destructor.
	~shared_interface();
#endif // defined(INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC) // Use Windows Events.
	
	/// Initialize.
	/**
	 * @param segment_name is used to open the database shared memory.
	 *		The segment_name has the format:
	 *		<DATABASE_NAME_PREFIX>_<DATABASE_NAME>_<SEQUENCE_NUMBER>
	 */
	void init(std::string segment_name, std::string monitor_interface_name,
	pid_type pid, owner_id oid = owner_id(owner_id::none));
	
	// Resource acquisition/release paired functions.
	
	//--------------------------------------------------------------------------
	/// Client's call acquire_client_number() at initialization. Each client
	/// thread within a client process need one client_interface which is
	/// allocated by acquiring a client_number. When the client thread have
	/// acquired this, it owns client_interface[n], where n is the
	/// client_number. It is a "timed" function that can fail.
	/// TODO: Implement timeout_milliseconds!
	/**
	 * @param spin_count Spin at most spin_count number of times, and try to
	 *		acquire the item without blocking. If it fails, the calling thread
	 *		can be blocked for the time period timeout_milliseconds.
	 * @param timeout_milliseconds The number of milliseconds to wait before a
	 *		timeout may occur.
	 * @return false if failing to get a client_number before the time period
	 *		specified by timeout_milliseconds has elapsed, true otherwise.
	 */
	bool acquire_client_number(uint32_t spin_count = 1000000, uint32_t
	timeout_milliseconds = 10000);
	
	/// Client's call release_client_number() when they want to release the
	/// client_number.
	/**
	 * @param spin_count Spin at most spin_count number of times, and try to
	 *		acquire the item without blocking. If it fails, the calling thread
	 *		can be blocked for the time period timeout_milliseconds.
	 * @param timeout_milliseconds The number of milliseconds to wait before a
	 *      timeout may occur. TODO: Implement timeout.
	 * @return false if failing to get a client_number before the time period
	 *		specified by timeout_milliseconds has elapsed, true otherwise.
	 */
	bool release_client_number(uint32_t spin_count = 1000000, uint32_t
	timeout_milliseconds = 10000);
	
	//--------------------------------------------------------------------------
	/// With acquire_channel() a client can allocate many, even all, channels.
	/// It is a "timed" function that can fail.
	/**
	 * @param the_channel_number Pointer to a channel_number variable where the
	 *		channel_number will be stored if successfully allocated the channel.
	 * @param the_scheduler_number Selects the scheduler to communicate with.
	 *		The scheduler_number is selected to match the affinity of the client
	 *		so that the scheduler and client likely share the same physical
	 *		core. This naturally only works of affinity is set for the threads.
	 * @param spin_count Spin at most spin_count number of times, and try to
	 *		acquire the item without blocking. If it fails, the calling thread
	 *		can be blocked for the time period timeout_milliseconds.
	 * @param timeout_milliseconds The number of milliseconds to wait before a
	 *      timeout may occur. TODO: implement timeout_milliseconds.
	 * @return false if the call is returning because the client_number pool is
	 *      empty or the time period specified by timeout_milliseconds has
	 *      elapsed, true otherwise.
	 */
	bool acquire_channel(channel_number* the_channel_number, scheduler_number
	the_scheduler_number, uint32_t spin_count = 1000000, uint32_t
	timeout_milliseconds = 10000);
	
	/// A client can call release_channel() to release a channel it owns,
	/// at any time - even when the channel is not in the tranquility state,
	/// i.e., it has not received all responses from the scheduler. The channel
	/// is marked to be released by the scheduler so the channel might not have
	/// been released when returning from this call. The scheduler will
	/// eventually see that the channel is to be released and will do that after
	/// it has released the chunks in the in and out queues of the channel, if
	/// any. This function doesn't block the client thread.
	/**
	 * @param the_channel_number The channel_number to be released.
	 */
	void release_channel(channel_number the_channel_number);
	
	//--------------------------------------------------------------------------
	/// Allocate linked chunks from the shared_chunk_pool. It is a "timed"
	/// function that can fail.
	/**
	 * @param head Will upon return contain the head of a linked chain of chunks
	 *		if successfuly acquired the requested size.
	 * @param size The number of bytes to allocate as 1..N linked chunks. The
	 *		chunks require some space for header data and this is taken into
	 *		account.
	 * @param timeout_milliseconds The number of milliseconds to wait before a
	 *      timeout may occur.
	 * @return true if successfully acquired the linked chunks with the
	 *		requested amount of memory before the time period specified by
	 *		timeout_milliseconds has elapsed, otherwise false if not enough
	 *		space or the time period has elapsed.
	 */
	// TODO: After having removed acquire_linked_chunks(), and added it to
	// server_port, rename this to:
	// acquire_linked_chunks_from_shared_chunk_pool().
	bool client_acquire_linked_chunks(chunk_index& head, std::size_t size,
	uint32_t timeout_milliseconds); /// "A"

	/// Allocate linked chunks from the shared_chunk_pool. It is a "timed"
	/// function that can fail.
	/**
	 * @param head Will upon return contain the head of a linked chain of chunks
	 *		if successfuly acquired the requested size.
	 * @param num_chunks The number of chunks to allocate.
	 * @param timeout_milliseconds The number of milliseconds to wait before a
	 *      timeout may occur.
	 * @return true if successfully acquired the linked chunks with the
	 *		requested amount of memory before the time period specified by
	 *		timeout_milliseconds has elapsed, otherwise false if not enough
	 *		space or the time period has elapsed.
	 */
	// TODO: After having removed acquire_linked_chunks(), and added it to
	// server_port, rename this to:
	// acquire_linked_chunks_from_shared_chunk_pool().
	bool client_acquire_linked_chunks_counted(chunk_index& head, std::size_t num_chunks,
	uint32_t timeout_milliseconds = 10000); /// "B"
	
	/// Release linked chunks to the shared_chunk_pool. It is a "timed" function
	/// that can fail. NOTE: It may be the case that not all chunks were
	/// released and thus the message data may be cut.
	/**
	 * @param head The head of the linked chunks, will upon return contain
	 *		chunk_type::LINK_TERMINATOR if successfully released all linked
	 *		chunks, otherwise it contains the chunk_index pointing to the head
	 *		of the linked chunks that are left.
	 * @param timeout_milliseconds The number of milliseconds to wait before a
	 *      timeout may occur.
	 * @return true if successfully released all linked chunks in which case
	 *		head is set to chunk_type::LINK_TERMINATOR, otherwise returns false
	 *		if failed to release all linked chunks, or the time period has
	 *		elapsed.
	 */
	// TODO: After having removed release_linked_chunks(), and added it to
	// server_port, rename this to:
	// release_linked_chunks_to_shared_chunk_pool().
	bool client_release_linked_chunks(chunk_index& head, uint32_t
	timeout_milliseconds = 10000); /// "C"
	
	//--------------------------------------------------------------------------
	/// Acquire N (unlinked) chunks from the shared_chunk_pool to a private
	/// chunk_pool.
	/**
	 * @param private_chunk_pool Reference to the private chunk_pool to which
	 *		chunks are allocated/moved. All chunks are marked as owned by the
	 *		calling client.
	 * @param chunks_to_acquire The number of chunks to acquire.
	 * @param client_interface_ptr A pointer to the client_interface where the
	 *		chunk will be marked as owned by the client.
	 * @param timeout_milliseconds The number of milliseconds to wait before a
	 *		timeout may occur while trying to lock the shared_chunk_pool.
	 * @return The number of acquired chunks. If the private_chunk_pool is empty
	 *		or becomes empty when acquiring chunks, the acquirement process is
	 *		stopped and the job is half done.
	 */
	template<typename U>
	std::size_t acquire_from_shared_to_private(U& private_chunk_pool,
	std::size_t chunks_to_acquire, client_interface_type* client_interface_ptr,
	uint32_t timeout_milliseconds);
	
	/// Release N (unlinked) chunks from a private chunk_pool to the
	/// shared_chunk_pool.
	/**
	 * @param private_chunk_pool Reference to the private chunk_pool from which
	 *		chunks are released.
	 * @param chunks_to_release The number of chunks to release.
	 * @param client_interface_ptr A pointer to the client_interface where the
	 *		chunk will be marked as not owned by the client.
	 * @param timeout_milliseconds The number of milliseconds to wait before a
	 *		timeout may occur while trying to lock the shared_chunk_pool.
	 * @return The number of released chunks.
	 */
	template<typename U>
	std::size_t release_from_private_to_shared(U& private_chunk_pool,
	std::size_t chunks_to_release, client_interface_type* client_interface_ptr,
	uint32_t timeout_milliseconds = 10000);
	
	//--------------------------------------------------------------------------
	/// This is for debug purpose only. It prints a list showing how the chunks
	/// beginning with the head are linked together. TODO: remove it.
	void show_linked_chunks(chunk_index head);
	
	/// Get reference to the shared_chunk_pool.
	shared_chunk_pool_type& shared_chunk_pool() const;
	
	/// Get reference to chunk[n] via chunk_index.
	/**
	 * @param n The chunk_index.
	 * @return reference to chunk[n].
	 */
	chunk_type& chunk(std::size_t n) const;
	
	/// Get the chunk_index of a chunk via a poiner to somewhere in the chunk.
	/// Note: The pointer can point to anywhere within the chunk of any chunk.
	/// If the pointer points elsewhere, the chunk_index will be out of range.
	/// This works as long as the thread calling this function is in the same
	/// process of the thread that produced the pointer. It does not work
	/// between different processes. Therefore chunk indices are used.
	/**
	 * @param Ptr A pointer of any kind.
	 * @param ptr Points to somewhere within the chunk of any chunk.
	 * @return chunk_index of the chunk.
	 */
	template<typename Ptr>
	chunk_index get_index_from_pointer(Ptr ptr) const;
	
	/// Get reference to the common_scheduler_interface.
	/**
	 * @return reference to the common_scheduler_interface.
	 */
	common_scheduler_interface_type& common_scheduler_interface() const;
	
	/// Get reference to scheduler_interface[n].
	/**
	 * @param n The scheduler_number.
	 * @return reference to scheduler_interface[n].
	 */
	scheduler_interface_type& scheduler_interface(std::size_t n) const;
	
	/// Get reference to the common_client_interface.
	/**
	 * @return reference to the common_client_interface.
	 */
	common_client_interface_type& common_client_interface() const;
	
	/// Get reference to client_interface[n].
	/**
	 * @param n The client_number.
	 * @return reference to client_interface[n].
	 */
	client_interface_type& client_interface(std::size_t n) const;
	
	/// Get reference to the client_interface with this objects client_number.
	/**
	 * @return reference to the client_interface with this objects
	 *		client_number.
	 */
	client_interface_type& client_interface() const;
	
	/// Get reference to channel[n].
	/**
	 * @param n The channel_number.
	 * @return reference to channel[n].
	 */
	channel_type& channel(std::size_t n) const;
	
	/// Set database state. This is used by the monitor when it detects that
	/// the database process exit without having unregistered.
	/**
	 * @param s The state of the database, which can be normal,
	 *		database_terminated_gracefully or
	 *		database_terminated_unexpectedly.
	 */
	void database_state(common_client_interface_type::state s);
	
	/// Get database state. This is used by the client threads.
	/**
	 * @return The state of the database, which can be normal or
	 *      database_is_down.
	 */
	common_client_interface_type::state database_state() const;
	
	/// Get number of active schedulers.
	/**
	 * @return The number of active schedulers.
	 */
	/// This function is from class client_port.
	uint32_t number_of_active_schedulers() {
		return common_scheduler_interface().number_of_active_schedulers();
	}
	
	/// send_to_server_and_wait_response() send a request and wait for a
	/// response from the database. It is a "timed" function that can fail.
	/**
	 * @param channel The channel which the communication is done.
	 * @param request The request chunk_index.
	 * @param response Reference to the response chunk_index.
	 * @param spin The number of times to re-try pushing to the in queue or
	 *		popping from the out queue, before eventually blocking.
	 * @param timeout The number of milliseconds to wait before a timeout may
	 *		occur, in case the database doesn't respond. If the database
	 *		terminates, then clients waiting to push or pop on a queue will be
	 *		notified more or less instantly (within a millisecond or so), by
	 *		the monitor.
	 * @return en error code.
	 */
	uint32_t send_to_server_and_wait_response(uint32_t channel, uint32_t
	request, uint32_t& response, uint32_t spin, uint32_t timeout);
	
	/// Get the segment_name in the format
	/// <DATABASE_NAME_PREFIX>_<DATABASE_NAME>_<SEQUENCE_NUMBER>
	std::string get_segment_name() const;
	
	/// Get the segment_name in the format
	/// <SERVER_NAME>_<MONITOR_INTERFACE_SUFFIX>
	std::string get_monitor_interface_name() const;
	
	/// Get the owner_id.
	owner_id get_owner_id() const;
	
	/// Get pid.
	pid_type get_pid() const;
	
	/// Get the client_number.
	client_number get_client_number() const;
	
#if defined(INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC) // Use Windows Events.
	//--------------------------------------------------------------------------
	/// Only clients: Open client work event and return a reference to it.
	/// NOTE: Get a client_number first, because this function will use what
	/// get_client_number() returns.
	/// Before attempting to open the event, this function sets the event to 0
	/// regardless of if the event is already open or not.
	/**
	 * @param i The clients's number, related to client number i. Normally use
	 *		get_client_number() to pass the value of i.
	 * @return A reference to the client work event, or 0 if failed to open.
	 */ 
	HANDLE& open_client_work_event(std::size_t i);

	/// Close client work event. It does not actually close the event, the
	/// database do that before terminating, instead it sets the event to 0.
	void close_client_work_event();

	/// Get a reference to the client work event.
	/**
	 * @return A reference to the client work event.
	 */ 
	HANDLE& client_work_event();
	
	/// Get a const reference to the client work event.
	/**
	 * @param A const reference to the client work event.
	 */ 
	const HANDLE& client_work_event() const;

	//--------------------------------------------------------------------------
	/// Open scheduler work event (i) and return a reference to it.
	/// NOTE: Before attempting to open the event, sets the event to 0
	/// regardless of if the event is already open or not.
	/**
	 * @param i The scheduler's number, related to scheduler number i.
	 * @return A reference to scheduler work event (i). NULL if failed to open.
	 */ 
	HANDLE& open_scheduler_work_event(std::size_t i);

	/// Close scheduler work event (i). It does not actually close the event,
	/// the database do that before terminating, instead it sets the event to 0.
	/**
	 * @param i The scheduler's number, related to scheduler number i.
	 */ 
	void close_scheduler_work_event(std::size_t i);

	/// Get a reference to scheduler work event (i).
	/**
	 * @param i The scheduler's number, related to
	 *		scheduler_interface[i].
	 * @return A reference to scheduler work event (i).
	 */ 
	HANDLE& scheduler_work_event(std::size_t i);
	
	/// Get a const reference to scheduler work event (i).
	/**
	 * @param i The scheduler's number, related to
	 *		scheduler_interface[i].
	 * @param A const reference to scheduler work event (i).
	 */ 
	const HANDLE& scheduler_work_event(std::size_t i) const;

#if defined (IPC_SCHEDULER_INTERFACE_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
	/// Open scheduler_number_pool_not_empty event (i) and return a reference to it.
	/// NOTE: Before attempting to open the event, sets the event to 0
	/// regardless of if the event is already open or not.
	/**
	 * @param i The scheduler's number, related to scheduler number i.
	 * @return A reference to scheduler_number_pool_not_empty event (i). NULL if failed to open.
	 */ 
	HANDLE& open_scheduler_number_pool_not_empty_event(std::size_t i);

	/// Close scheduler_number_pool_not_empty event (i). It does not actually close the event,
	/// the database do that before terminating, instead it sets the event to 0.
	/**
	 * @param i The scheduler's number, related to scheduler number i.
	 */ 
	void close_scheduler_number_pool_not_empty_event(std::size_t i);

	/// Open scheduler_number_pool_not_full event (i) and return a reference to it.
	/// NOTE: Before attempting to open the event, sets the event to 0
	/// regardless of if the event is already open or not.
	/**
	 * @param i The scheduler's number, related to scheduler number i.
	 * @return A reference to scheduler_number_pool_not_full event (i). NULL if failed to open.
	 */ 
	HANDLE& open_scheduler_number_pool_not_full_event(std::size_t i);

	/// Close scheduler_number_pool_not_full event (i). It does not actually close the event,
	/// the database do that before terminating, instead it sets the event to 0.
	/**
	 * @param i The scheduler's number, related to scheduler number i.
	 */ 
	void close_scheduler_number_pool_not_full_event(std::size_t i);

	/// Get a reference to scheduler_number_pool_not_empty event (i).
	/**
	 * @param i The scheduler's number, related to
	 *		scheduler_interface[i].
	 * @return A reference to scheduler_number_pool_not_empty event (i).
	 */ 
	HANDLE& scheduler_number_pool_not_empty_event(std::size_t i);
	
	/// Get a const reference to scheduler_number_pool_not_empty event (i).
	/**
	 * @param i The scheduler's number, related to
	 *		scheduler_interface[i].
	 * @param A const reference to scheduler_number_pool_not_empty event (i).
	 */ 
	const HANDLE& scheduler_number_pool_not_empty_event(std::size_t i) const;

	/// Get a reference to scheduler_number_pool_not_full event (i).
	/**
	 * @param i The scheduler's number, related to
	 *		scheduler_interface[i].
	 * @return A reference to scheduler_number_pool_not_full event (i).
	 */ 
	HANDLE& scheduler_number_pool_not_full_event(std::size_t i);
	
	/// Get a const reference to scheduler_number_pool_not_full event (i).
	/**
	 * @param i The scheduler's number, related to
	 *		scheduler_interface[i].
	 * @param A const reference to scheduler_number_pool_not_full event (i).
	 */ 
	const HANDLE& scheduler_number_pool_not_full_event(std::size_t i) const;
#endif // defined (IPC_SCHEDULER_INTERFACE_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)

#endif // defined(INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC) // Use Windows Events.
	
#if defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)
	/// Open shared_chunk_pool_not_full event and return a reference to it.
	/// NOTE: Before attempting to open the event, sets the event to 0
	/// regardless of if the event is already open or not.
	/**
	 * @return A reference to shared_chunk_pool_not_full event. NULL if failed to open.
	 */ 
	HANDLE& open_shared_chunk_pool_not_full_event();
	
	/// Close shared_chunk_pool_not_full event. It does not actually close the event,
	/// the database do that before terminating, instead it sets the event to 0.
	void close_shared_chunk_pool_not_full_event();

	/// Open shared_chunk_pool_not_empty event and return a reference to it.
	/// NOTE: Before attempting to open the event, sets the event to 0
	/// regardless of if the event is already open or not.
	/**
	 * @return A reference to shared_chunk_pool_not_empty event. NULL if failed to open.
	 */ 
	HANDLE& open_shared_chunk_pool_not_empty_event();

	/// Close shared_chunk_pool_not_empty event. It does not actually close the event,
	/// the database do that before terminating, instead it sets the event to 0.
	void close_shared_chunk_pool_not_empty_event();

	/// Get a reference to shared_chunk_pool_not_full event.
	/**
	 * @return A reference to shared_chunk_pool_not_full event.
	 */ 
	HANDLE& shared_chunk_pool_not_full_event();
	
	/// Get a const reference to shared_chunk_pool_not_full event.
	/**
	 * @param A const reference to shared_chunk_pool_not_full event.
	 */ 
	const HANDLE& shared_chunk_pool_not_full_event() const;

	/// Get a reference to shared_chunk_pool_not_empty event.
	/**
	 * @return A reference to shared_chunk_pool_not_empty event.
	 */ 
	HANDLE& shared_chunk_pool_not_empty_event();
	
	/// Get a const reference to shared_chunk_pool_not_empty event.
	/**
	 * @param A const reference to shared_chunk_pool_not_empty event.
	 */ 
	const HANDLE& shared_chunk_pool_not_empty_event() const;
#endif // defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)

private:
	// Specify what it throws.
	void init();
	
	/// TODO: Think through which ones are most frequently used together, and
	/// put them close to one another so that they share the same cache-line.
	
	chunk_type* chunk_;
	channel_type* channel_;
	shared_chunk_pool_type* shared_chunk_pool_;
	common_scheduler_interface_type* common_scheduler_interface_;
	scheduler_interface_type* scheduler_interface_;
	common_client_interface_type* common_client_interface_;
	
	// client_interface_ points to client_interface[0] with respect to this
	// process address space. This pointer is never changed.
	client_interface_type* client_interface_;
	
	owner_id owner_id_;
	pid_type pid_;
	std::string segment_name_;
	std::string monitor_interface_name_;
	shared_memory_object segment_;
	mapped_region mapped_region_;
	
#if defined(INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC) // Use Windows Events.
	// Each client need to have all events (HANDLEs) already opened and ready to be used.
	HANDLE client_work_;

	// To notify any scheduler. In this array of HANDLEs, only those that correspond to
	// an active scheduler will be opened/closed.
	HANDLE scheduler_work_[max_number_of_schedulers];

#if defined (IPC_SCHEDULER_INTERFACE_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
	// To notify that scheduler_number_pool_not_empty_[n]. In this array of HANDLEs,
	// only those that correspond to an active scheduler will be opened/closed.
	HANDLE scheduler_number_pool_not_empty_[max_number_of_schedulers];

	// To notify that scheduler_number_pool_not_full_[n]. In this array of HANDLEs,
	// only those that correspond to an active scheduler will be opened/closed.
	HANDLE scheduler_number_pool_not_full_[max_number_of_schedulers];
#endif // defined (IPC_SCHEDULER_INTERFACE_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)

#endif // defined(INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC) // Use Windows Events.
	
#if defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)
	// Event used by the scheduler to wait when the queue is not full.
	// Client's have to open the event and pass it in as an argument.
	HANDLE shared_chunk_pool_not_full_;
	
	// Event used by the scheduler to wait when the queue is not empty.
	// Client's have to open the event and pass it in as an argument.
	HANDLE shared_chunk_pool_not_empty_;
#endif // defined (IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL)
	
protected:
	/// TODO: Replace direct accesses to client_number_ with get_client_number()
	client_number client_number_;
};

} // namespace core
} // namespace starcounter

#include "impl/shared_interface.hpp"

#endif // STARCOUNTER_CORE_SHARED_INTERFACE_HPP
