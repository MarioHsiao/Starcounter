//
// config_param.hpp
//
// Copyright © 2006-2012 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#ifndef STARCOUNTER_CORE_CONFIG_PARAM_HPP
#define STARCOUNTER_CORE_CONFIG_PARAM_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#include <cstdlib>
#include "macro_definitions.hpp"
#include "../../Starcounter.Internal/Constants/MixedCodeConstants.cs"

namespace starcounter {
namespace core {

// Read some of these parameters from a config file at startup.
// Use default values if fail to read the config file (or not present).
// The values here are not scientific.
// TODO: Mark which parameters that are configurable when initializing the
// Starcounter DB.

// The size of each chunk.
const std::size_t chunk_size = MixedCodeConstants::SHM_CHUNK_SIZE; // 4K chunks.

// One chunk per channel is the minimum because a default chunk is allocated for
// each channel. Currently channel_bits + chunks_total_number_max must not
// exceed 32, because chunk_index is 32-bit and sometimes both the chunk_index
// and the channel shared the same 32-bit word. This means that
// chunks_total_number_max can not be more than 1 << 24 when channel_bits = 8.
const std::size_t chunks_total_number_max = 1 << 20;

// The number of channels.
const std::size_t channel_bits = 8;
const std::size_t channels = 1 << channel_bits;

// The number of client_interfaces.
const std::size_t client_interface_bits = 8;
const std::size_t client_interfaces = 1 << client_interface_bits;

// The capacity of each channels in and out queues.
// This parameter is currently hard coded in the channel class itself
// to use 8 = 256 elements.
const std::size_t channel_capacity_bits = 8;
const std::size_t channel_capacity = 1 << channel_capacity_bits;

// The max number of databases that can exist (per IPC monitor).
const std::size_t max_number_of_databases = 64;

// The max number of schedulers that can exist (per NUMA node).
const std::size_t max_number_of_schedulers = 31;

// The max number of clients that can exist (maybe per NUMA node).
const std::size_t max_number_of_clients = 256;

// The size of the array to hold the server name, including terminating null.
const std::size_t server_name_size = 32;

// The size of the array to hold the database name, including terminating null.
const std::size_t database_name_size = 32;

// The size of the array to hold the segment name, including terminating null.
const std::size_t segment_name_size = 64;

// The size of the array to hold the segment name and notify name,
// including terminating null. The format is:
// "Local\<segment_name>_notify_scheduler_<N>", and
// "Local\<segment_name>_notify_client_<N>".
// where N is number in the range 0..30 for schedulers and 0..255 for clients.
// For example: "Local\starcounter_PERSONAL_LOADANDLATENCY_64_notify_scheduler_0"
const std::size_t segment_and_notify_name_size = 96;

// The size of the array to hold the server name and ipc monitor cleanup event name,
// including terminating null. The format is:
// "Local\<server_name>_ipc_monitor_cleanup_event"
const std::size_t ipc_monitor_cleanup_event_name_size
= server_name_size -1 /* null */ +sizeof("Local\\") -1 /* null */ 
+sizeof(IPC_MONITOR_CLEANUP_EVENT);

// The size of the array to hold the server name and active databases updated event name,
// including terminating null. The format is:
// "Local\<server_name>_ipc_monitor_active_databases_updated_event"
const std::size_t active_databases_updated_event_name_size
= server_name_size -1 /* null */ +sizeof("Local\\") -1 /* null */ 
+sizeof(ACTIVE_DATABASES_UPDATED_EVENT);

// The size of the array to hold the interface name.
const std::size_t interface_name_size = 64;

// The size of buffer to hold file name, including terminating null.
const std::size_t maximum_path_length = 768;

// The size of buffer to hold file name, including terminating null.
const std::size_t maximum_file_name_length = 256;

// The size of buffer to hold path +file name, including terminating null.
const std::size_t maximum_path_and_file_name_length
= maximum_path_length +maximum_file_name_length;

} // namespace core
} // namespace starcounter

#endif // STARCOUNTER_CORE_CONFIG_PARAM_HPP
