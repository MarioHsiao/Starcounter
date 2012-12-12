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
#include <bitset>

// Windows headers.
#define WIN32_LEAN_AND_MEAN
#include <winsock2.h>
#include <mswsock.h>
#include <mmsystem.h>
#include <strsafe.h>
#undef WIN32_LEAN_AND_MEAN

#include <conio.h>
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

// Internal foreign headers.
#include "../../HTTP/HttpParser/ThirdPartyHeaders/http_parser.h"
#include <rapidxml.hpp>
#include <cdecode.h>
#include <cencode.h>
#include <sha-1.h>

#endif // STATIC_HEADERS_HPP