//
// initialize.cpp
// server
//
// Copyright � 2006-2011 Starcounter AB. All rights reserved.
// Starcounter� is a registered trademark of Starcounter AB.
//

#include <iostream> // debug
#include <cstddef>
#include <climits>
#include <boost/cstdint.hpp>
#include <boost/thread/thread.hpp>
#include <boost/thread/mutex.hpp>
#include <boost/thread/condition.hpp>
#include <boost/shared_ptr.hpp>
#include <boost/call_traits.hpp>
#include <boost/bind.hpp> 
#include <boost/utility.hpp>
#include <boost/noncopyable.hpp>
#define WIN32_LEAN_AND_MEAN
#include <intrin.h>
#undef WIN32_LEAN_AND_MEAN
#include "../common/bounded_buffer.hpp"
#include "../common/config_param.hpp"
#include "../common/circular_buffer.hpp"
#include "../common/bounded_buffer.hpp"
#include "../common/chunk.hpp"
#include "../common/shared_chunk_pool.hpp"
#include "../common/channel.hpp"
#include "../common/scheduler_channel.hpp"
#include "../common/common_scheduler_interface.hpp"
#include "../common/scheduler_interface.hpp"
#include "../common/common_client_interface.hpp"
#include "../common/client_interface.hpp"
#include "../common/client_number.hpp"
#include "../common/config_param.hpp"
#include "../common/spinlock.hpp"
#include "../common/macro_definitions.hpp"
#include "../common/interprocess.hpp"
#include "../common/name_definitions.hpp"
#include "initialize.hpp"

namespace {

const int _E_UNSPECIFIED = 999L;

} // namespace

namespace starcounter {
namespace core {

/// TODO: The use of static to indicate "local to translation unit" is
/// deprecated in C++. Use unnamed namespaces instead.
static shared_memory_object global_segment_shared_memory_object;

//typedef DWORD affinity_mask; // experimenting

unsigned long initialize(const char* segment_name, const char* server_name, std::size_t schedulers, bool
is_system, uint32_t chunks_total_number) try {
	using namespace starcounter::core;

	//--------------------------------------------------------------------------
	// if (strlen(segment_name) > 31) return;
	
	//--------------------------------------------------------------------------
	// Set affinity but not here. main.cpp has scheduler() where the affinity
	// for the calling thread can be set.
	#if 0 // experimenting
	affinity_mask process_affinity_mask[1] = {0};
	affinity_mask system_affinity_mask[1] = {0};
	
	if (!GetProcessAffinityMask(GetCurrentProcess(),
	PDWORD_PTR(process_affinity_mask), PDWORD_PTR(system_affinity_mask))) {
		*process_affinity_mask = ~0;
		*system_affinity_mask = ~0;
	}
	
	//SetProcessAffinityMask(GetCurrentProcess(), 0x55555555UL);
	//Sleep(1);
	SetProcessAffinityMask(GetCurrentProcess(), 0x55555555UL);
	Sleep(1);
	#endif // experimenting
	
	/// Setting the affinity, since without it the performance is affected by
	/// which cores the threads happen to be running on...which is not ideal.
	/// Since it is most common now that Intel processors supporting HT have a
	/// pair of hardware threads sharing an L1-cache, I set the affinity of the
	/// server threads to be scheduled on even numbered cores.
	//SetProcessAffinityMask(GetCurrentProcess(), 0x55555555UL); /// mask was 1
	//Sleep(1);
	
	//--------------------------------------------------------------------------
	// Compute the memory required for all objects in shared memory.
	std::size_t shared_memory_segment_size =
	
	// chunk[s]
	+sizeof(chunk_type) * chunks_total_number
	
	// shared_chunk_pool
	+sizeof(shared_chunk_pool_type)
	+sizeof(chunk_index) * chunks_total_number
	
	// channel[s]
	+sizeof(channel_type) * channels
	
	// common_scheduler_interface
	+sizeof(common_scheduler_interface_type)
	
	// common_client_interface
	+sizeof(common_client_interface_type)
	+sizeof(client_number) * max_number_of_clients
	
	// scheduler_interface[s]
	+sizeof(scheduler_interface_type) * max_number_of_schedulers
	
	// scheduler_interface[s] channel_number_
	+sizeof(channel_number) * channels * max_number_of_schedulers
	
	// scheduler_interface[s] overflow_pool_
	+sizeof(chunk_index) * chunks_total_number * max_number_of_schedulers
	
	// client_interface[s]
	+sizeof(client_interface_type) * max_number_of_clients
	
	// scheduler_task_channel[s]
	+sizeof(scheduler_channel_type) * max_number_of_schedulers;

	// scheduler_signal_channel[s]
	+sizeof(scheduler_channel_type) * max_number_of_schedulers;
	
	//--------------------------------------------------------------------------
	
	// Create a new segment with given name and size.
	global_segment_shared_memory_object.init_create(segment_name, 
	shared_memory_segment_size, is_system);
	
	if (!global_segment_shared_memory_object.is_valid()) {
		return _E_UNSPECIFIED;
	}
	
	mapped_region segment_region(global_segment_shared_memory_object);
	
	simple_shared_memory_manager* psegment_manager
	= new(segment_region.get_address()) simple_shared_memory_manager;
	psegment_manager->reset(shared_memory_segment_size);
	
	//--------------------------------------------------------------------------
	// Construct the chunk array in shared memory.
	void* p = psegment_manager->create_named_block
	(starcounter_core_shared_memory_chunks_name, sizeof(chunk_type) * chunks_total_number);
	
	chunk_type* chunk = static_cast<chunk_type*>(p);
	for (std::size_t i = 0; i < chunks_total_number; ++i) {
		new(chunk +i) chunk_type;
	}
	
	//--------------------------------------------------------------------------
	// Construct the shared_chunk_pool in shared memory.
	
	// Initialize shared memory STL-compatible allocator.
	const shm_alloc_for_the_shared_chunk_pool2 shared_chunk_pool_alloc_inst
	(segment_region.get_address());
	
	p = psegment_manager->create_named_block
	(starcounter_core_shared_memory_shared_chunk_pool_name,
	sizeof(shared_chunk_pool_type));

	shared_chunk_pool_type* shared_chunk_pool = new(p) shared_chunk_pool_type
	(chunks_total_number, shared_chunk_pool_alloc_inst /*, segment_name*/);
	
	// Initialize the shared_chunk_pool by pushing in chunk_indexes.
	// These chunk_indexes represents free chunks.

	// Chunks from 0..chunks -1 are put in the shared_chunk_pool.
	for (chunk_index i = 0; i < chunks_total_number; ++i) {
		shared_chunk_pool->push_front(i);
	}
	
	//--------------------------------------------------------------------------
	// Construct the common_scheduler_interface in shared memory.
	
	const shm_alloc_for_the_common_scheduler_interface2
	common_scheduler_interface_alloc_inst(segment_region.get_address());
	
	p = psegment_manager->create_named_block
	(starcounter_core_shared_memory_common_scheduler_interface_name,
	sizeof(common_scheduler_interface_type));
	
	common_scheduler_interface_type* common_scheduler_interface = new(p)
	common_scheduler_interface_type(server_name,
	common_scheduler_interface_alloc_inst);
	
	//--------------------------------------------------------------------------
	// Construct the scheduler_interface array in shared memory.
	
	// Initialize shared memory STL-compatible allocator.
#if defined (IPC_SCHEDULER_INTERFACE_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
	const shm_alloc_for_the_scheduler_interfaces2
	scheduler_interface_alloc_inst(segment_region.get_address()); /// remove?
#else // !defined (IPC_SCHEDULER_INTERFACE_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
	const shm_alloc_for_the_scheduler_interfaces2
	scheduler_interface_alloc_inst(segment_region.get_address());
#endif // defined (IPC_SCHEDULER_INTERFACE_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
	
	const shm_alloc_for_the_scheduler_interfaces2b
	scheduler_interface_alloc_inst2(segment_region.get_address());
	
	// Allocate the scheduler_interface array.
	
	p = psegment_manager->create_named_block
	(starcounter_core_shared_memory_scheduler_interfaces_name,
	sizeof(scheduler_interface_type) * schedulers);
	
	scheduler_interface_type* scheduler_interface
	= (scheduler_interface_type*) p;
	
	for (std::size_t i = 0; i < schedulers; ++i) {
		new(scheduler_interface +i) scheduler_interface_type(
		channels,
		chunks_total_number,
		chunks_total_number,
#if defined (IPC_SCHEDULER_INTERFACE_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
#else // !defined (IPC_SCHEDULER_INTERFACE_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
		scheduler_interface_alloc_inst,
#endif // defined (IPC_SCHEDULER_INTERFACE_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
		scheduler_interface_alloc_inst2,
		scheduler_interface_alloc_inst2,
		segment_name,
		i);
	}

	// Initialize the scheduler_interface by pushing in channel_number(s).
	// These channel_number(s) represents free channels. For now, the
	// channels are more or less evenly distributed among the schedulers.
	for (std::size_t n = 0; n < channels; ++n) {
#if defined (IPC_SCHEDULER_INTERFACE_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
		scheduler_interface[n % schedulers].insert(n, 1 /* id */,
		smp::spinlock::milliseconds(10000));
#else // !defined (IPC_SCHEDULER_INTERFACE_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
		scheduler_interface[n % schedulers].push_front_channel_number(n, 1 /* id */);
#endif // defined (IPC_SCHEDULER_INTERFACE_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
	}
	
	//--------------------------------------------------------------------------
	// Construct the client_interface array in shared memory.
	
	// Initialize shared memory STL-compatible allocator.
	const shm_alloc_for_the_client_interfaces2
	client_interface_alloc_inst(segment_region.get_address());
	
	// Allocate the client_interface array.
	p = psegment_manager->create_named_block
	(starcounter_core_shared_memory_client_interfaces_name,
	sizeof(client_interface_type) * max_number_of_clients);
	
	client_interface_type* client_interface = (client_interface_type*) p;
	
	for (std::size_t i = 0; i < max_number_of_clients; ++i) {
		new(client_interface +i) client_interface_type
		(client_interface_alloc_inst, segment_name, i);
	}
	
	//--------------------------------------------------------------------------
	// Construct the common_client_interface.
	
	// Initialize shared memory STL-compatible allocator.
	const shm_alloc_for_the_common_client_interface2
	common_client_interface_alloc_inst(segment_region.get_address());
	
	// Allocate the common_client_interface.
	p = psegment_manager->create_named_block
	(starcounter_core_shared_memory_common_client_interface_name,
	sizeof(common_client_interface_type));
	
#if defined (IPC_CLIENT_NUMBER_POOL_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
	common_client_interface_type* common_client_interface = new(p)
	common_client_interface_type(segment_name);
#else // !defined (IPC_CLIENT_NUMBER_POOL_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
	common_client_interface_type* common_client_interface = new(p)
	common_client_interface_type(max_number_of_clients,
	common_client_interface_alloc_inst);
#endif // defined (IPC_CLIENT_NUMBER_POOL_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
	
	// Initialize the client_number_pool queue with client numbers.
	for (client_number n = 0; n < max_number_of_clients; ++n) {
#if defined (IPC_CLIENT_NUMBER_POOL_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
		common_client_interface->insert_client_number(n, client_interface, owner_id(1));
#else // !defined (IPC_CLIENT_NUMBER_POOL_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
		common_client_interface->release_client_number(n, client_interface);
#endif // defined (IPC_CLIENT_NUMBER_POOL_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
	}
	
	//--------------------------------------------------------------------------
	// Construct the channel array in shared memory.
	
	// Initialize shared memory STL-compatible allocator.
	const shm_alloc_for_the_channels2 channels_alloc_inst
	(segment_region.get_address());
	
	// Allocate the client_interface array.
	p = psegment_manager->create_named_block
	(starcounter_core_shared_memory_channels_name, sizeof(channel_type)
	* channels);
	
	channel_type* channel = (channel_type*) p;
	for (std::size_t i = 0; i < channels; ++i) {
		new(channel +i) channel_type(channel_capacity, channels_alloc_inst);
	}
	
	// Initialize shared memory STL-compatible allocator.
	const shm_alloc_for_the_channels2
	scheduler_channels_alloc_inst(segment_region.get_address());
	
	//--------------------------------------------------------------------------
	// Construct an array of scheduler_channels in shared memory.
	p = psegment_manager->create_named_block
	(starcounter_core_shared_memory_scheduler_task_channels_name,
	sizeof(scheduler_channel_type) * schedulers);

	scheduler_channel_type* scheduler_task_channel = (scheduler_channel_type*) p;

	p = psegment_manager->create_named_block
	(starcounter_core_shared_memory_scheduler_signal_channels_name,
	sizeof(scheduler_channel_type) * schedulers);

	scheduler_channel_type* scheduler_signal_channel = (scheduler_channel_type*) p;

	for (std::size_t i = 0; i < schedulers; ++i) {
		new(scheduler_task_channel +i) scheduler_channel_type(channel_capacity,
		scheduler_channels_alloc_inst);

		new(scheduler_signal_channel +i) scheduler_channel_type(
		channel_capacity, scheduler_channels_alloc_inst);
	}

	return 0;
}
catch (...) {
	//std::cerr << "initialize: unknown exception thrown" << std::endl;
	return _E_UNSPECIFIED;
}

} // namespace core
} // namespace starcounter
