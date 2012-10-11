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

namespace starcounter {
namespace core {

// Read some of these parameters from a config file at startup.
// Use default values if fail to read the config file (or not present).
// The values here are not scientific.
// TODO: Mark which parameters that are configurable when initializing the
// Starcounter DB.

// The size of each chunk.
const std::size_t chunk_size = 1 << 12; // 4K chunks.

// One chunk per channel is the minimum because a default chunk is allocated for
// each channel.
const std::size_t chunks_total_number_max = 1 << 16;

// The number of channels.
const std::size_t channel_bits = 8;
const std::size_t channels = 1 << channel_bits;

// The capacity of each channels in and out queues.
// This parameter is currently hard coded in the channel class itself
// to use 8 = 256 elements.
const std::size_t channel_capacity = 256;

// The max number of schedulers that can exist (per NUMA node).
const std::size_t max_number_of_schedulers = 31;

// The max number of clients that can exist (maybe per NUMA node).
const std::size_t max_number_of_clients = 256;

// The default capacity of the bounded_message_buffer in the monitor is
// important. Without any deeper thought, I set it to:
const std::size_t bounded_message_buffer_capacity = 1 << 12;
// If a thread need to push a log message and it is full, the thread blocks
// until the log_thread extracts a message, and the log_thread may block while
// waiting for messages to be written to disk. Therefore it is best if the
// capacity is large enough so that threads pushing messages are unlikely to
// block.

// The size of the array to hold the server name, including terminating null.
const std::size_t server_name_size = 32;

// The size of the array to hold the database name, including terminating null.
const std::size_t database_name_size = 32;

// The size of the array to hold the segment name, including terminating null.
const std::size_t segment_name_size = 64;

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
