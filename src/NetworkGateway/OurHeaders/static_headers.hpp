#pragma once
#ifndef STATIC_HEADERS_HPP
#define STATIC_HEADERS_HPP

// Standard headers.
#include <iostream>
#include <sstream>
#include <fstream>
#include <vector>
#include <iterator>
#include <algorithm>
#include <iomanip>
#include <limits>
#include <list>
#include <cstdint>

// Windows headers.
#define WIN32_LEAN_AND_MEAN
#include <winsock2.h>
#include <mswsock.h>
#include <mmsystem.h>
#include <strsafe.h>
#undef WIN32_LEAN_AND_MEAN

#include <conio.h>
//#include <strsafe.h>
#include <wtypes.h>

// BOOST headers include.
#include <boost/cstdint.hpp>
#include <boost/algorithm/string/case_conv.hpp>
#include <boost/interprocess/managed_shared_memory.hpp>
#include <boost/interprocess/allocators/allocator.hpp>
#include <boost/interprocess/sync/interprocess_mutex.hpp>
#include <boost/interprocess/sync/scoped_lock.hpp>
#include <boost/interprocess/sync/interprocess_condition.hpp>
#include <boost/date_time.hpp>
#include <boost/date_time/posix_time/posix_time_types.hpp>
#include <boost/date_time/microsec_time_clock.hpp>
#include <boost/thread/thread.hpp>
#include <boost/thread/win32/thread_primitives.hpp>
#include <boost/call_traits.hpp>
#include <boost/bind.hpp>
#include <boost/lexical_cast.hpp>
#include <boost/timer.hpp>
#include <boost/iostreams/stream.hpp>
#include <boost/iostreams/tee.hpp>

// Connectivity headers include.
#include "common/macro_definitions.hpp"
#include "common/config_param.hpp"
#include "common/shared_interface.hpp"
#include "common/database_shared_memory_parameters.hpp"
#include "common/monitor_interface.hpp"
#include "common/circular_buffer.hpp"
#include "common/bounded_buffer.hpp"
#include "common/chunk.hpp"
#include "common/shared_chunk_pool.hpp"
#include "common/chunk_pool.hpp"
#include "common/channel.hpp"
#include "common/scheduler_channel.hpp"
#include "common/common_scheduler_interface.hpp"
#include "common/scheduler_interface.hpp"
#include "common/common_client_interface.hpp"
#include "common/client_interface.hpp"
#include "common/client_number.hpp"
#include "common/macro_definitions.hpp"
#include "common/interprocess.hpp"
#include "common/name_definitions.hpp"

// BMX/Blast2 include.
#include "../Chunks/bmx/bmx.hpp"
#include "../Chunks/bmx/chunk_helper.h"

// Internal foreign headers.
#include "http_parser.h"
#include "rapidxml.hpp"
#include "cdecode.h"
#include "cencode.h"
#include "sha-1.h"

// Internal includes.
#include "utilities.hpp"
#include "profiler.hpp"

#endif // STATIC_HEADERS_HPP