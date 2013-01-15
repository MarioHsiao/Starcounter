//
// chunk.hpp
//
// Copyright © 2006-2012 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#ifndef STARCOUNTER_CORE_CHUNK_HPP
#define STARCOUNTER_CORE_CHUNK_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#include <cstddef>
#include <boost/cstdint.hpp>
#include <boost/array.hpp>
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <intrin.h>
#undef WIN32_LEAN_AND_MEAN
#include "../common/owner_id.hpp"
#include "../common/config_param.hpp"

namespace starcounter {
namespace core {

// chunk_index:
//  3 3 2 2 2 2 2 2 2 2 2 2 1 1 1 1 1 1 1 1 1 1
//  1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2 1 0
// +-------+-------------------------+-----------------------------+
// |0 0 0 0|                    chunk|                             |
// +-------+-------------------------+-----------------------------+
//
// Example for 256 MiByte chunk pool, each chunk is 32 KiByte.
// With the chunk field 15:31 one of 131072 chunks are selected.
// Within that chunk the user (client or scheduler) can access byte 0..32767.
//
//
// chunk_index when using the concepts of sub-chunks:
//  3 3 2 2 2 2 2 2 2 2 2 2 1 1 1 1 1 1 1 1 1 1
//  1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2 1 0
// +-------+-------------------------+---------+-------------------+
// |0 0 0 0|                    chunk|sub_chunk|                   |
// +-------+-------------------------+---------+-------------------+
//
// Example for 256 MiByte chunk pool, each chunk is 32 KiByte and sub-chunks are
// 1 KiByte (32 sub-chunks).
// With the chunk field 15:31 one of 131072 chunks are selected.
// With the sub-chunk field 10:14 one of 32 sub-chunks within that chunk is
// selected and then the user (client or scheduler) can access byte 0..1023.

/// TODO: Make a class.
typedef uint32_t chunk_index;


#if 0 // current chunk layout
Chunks are currently 4 KiByte.

byte 0..7 is no longer used:
+----------------------------------------------------------------------+
| 32-bit owner_id (0 = no_one, free chunk) - OBSOLETE (2012-03-26)!    |
| On 2012-03-26 the owner_id is no longer stored in the chunk.         |
| This means that byte 0..7 is not used. We shall later on move all    |
| data 8 bytes closer to byte 0, except the link - it shall be located |
| in the last 4 bytes of the chunk.                                    |
+----------------------------------------------------------------------+

byte 8..15 (shall be moved to byte 0..7):
+----------------------------------------------------------------------+
| 64-bit user_data                                                     |
+----------------------------------------------------------------------+

byte 16..19 (shall be moved to byte 8..11):
+----------------------------------------------------------------------+
| 32-bit request_size                                                  |
+----------------------------------------------------------------------+

byte 20..20 +request_size -1 (20 shall be 12)
+----------------------------------------------------------------------+
| Blast2 request message(s)                                            |
+----------------------------------------------------------------------+

byte 20 +request_size..23 +request_size
+----------------------------------------------------------------------+
| 32-bit response_size                                                 |
+----------------------------------------------------------------------+

+----------------------------------------------------------------------+
| Blast2 response message(s).                                          |
+----------------------------------------------------------------------+

byte 4088..4091:
+----------------------------------------------------------------------+
| 32-bit chunk_index next_link (-1 = LINK_TERMINATOR)                  |
+----------------------------------------------------------------------+

byte 4092..4095:
+----------------------------------------------------------------------+
| 32-bit chunk_index link (-1 = LINK_TERMINATOR)                       |
+----------------------------------------------------------------------+

#endif // current chunk layout



#if 0 // new chunk layout - not implemented yet
Chunks are currently 32 KiByte.

byte 0..7:
+----------------------------------------------------------------------+
| 64-bit user_data                                                     |
+----------------------------------------------------------------------+

byte 8..11:
+----------------------------------------------------------------------+
| 32-bit request_size                                                  |
+----------------------------------------------------------------------+

byte 12..4087:
+----------------------------------------------------------------------+
| Blast2 request message(s)                                            |
+----------------------------------------------------------------------+

if response(s):
+----------------------------------------------------------------------+
| 32-bit response_size                                                 |
+----------------------------------------------------------------------+
| Blast2 response message(s)                                           |
+----------------------------------------------------------------------+

byte 4088..4091:
+----------------------------------------------------------------------+
| 32-bit chunk_index next_link (-1 = LINK_TERMINATOR)                  |
+----------------------------------------------------------------------+

byte 4092..4095:
+----------------------------------------------------------------------+
| 32-bit chunk_index link (-1 = LINK_TERMINATOR)                       |
+----------------------------------------------------------------------+

#endif // new chunk layout - not implemented yet

#ifdef _M_X64
# pragma intrinsic (_InterlockedExchange64)
#endif

// boost::array is documented here:
// http://www.boost.org/doc/libs/1_46_1/doc/html/boost/array.html
// C:\boost\x86_64\include\boost-1_46_1\boost\array.hpp

typedef int16_t bmx_protocol_type;

template<class T, std::size_t N>
class chunk : public boost::array<T,N> {
public:
	// The kernel uses the user_data value for various things.
	typedef uint64_t user_data_type;
	
	// The message size is 32-bit unsigned, same for request and response size.
	typedef uint32_t message_size_type;

	typedef chunk_index link_type;

	enum {
		// There are two links: The stream_link, and the next_link.
		// The links have no pad bytes inbetween.
		// link_size represents the space required for these two links.
		link_size = 2 * sizeof(link_type),
		bmx_handler_size = sizeof(bmx_protocol_type),

		// The last chunk in the chain of 1..N chunks is terminated by setting
		// the link to link_terminator.
		link_terminator = -1
	};

	enum {
		owner_id_begin = 0,
		user_data_begin = 8,
		bmx_protocol_begin = 16,
		request_size_begin = bmx_protocol_begin +bmx_handler_size,
		stream_link_begin = static_size -link_size,
		next_link_begin = static_size -(2 * link_size)
	};

	// header size is constant
	enum {
		// Think about padding.
		static_header_size =
		+sizeof(owner_id) // to be removed later
		+sizeof(user_data_type)
		+sizeof(message_size_type)
		+4 // Padding for 8-bytes alignment.
	};
	
	// data size is constant
	enum {
		static_data_size = static_size -static_header_size -link_size,
	};

	/// TODO: Follow the coding standard - avoid ALL_CAPITAL identifiers.
	/// Only macros shall use capital letters.
    enum {
        LINK_SIZE = link_size,
        BMX_HANDLER_SIZE = bmx_handler_size,
        LINK_TERMINATOR = link_terminator,
		STATIC_DATA_SIZE = static_data_size,
		STATIC_HEADER_SIZE = static_header_size,
		REQUEST_SIZE_BEGIN = request_size_begin
    };
	
	/// data_size() returns the number of bytes of the chunks data area. It does
	/// not take into consideration the amount of data in use.
	/**
	 * @return The number of bytes reserved for the data area of the chunk,
	 *		not including the header data.
	 */
	static size_type data_size() {
		return N -static_header_size;
	}
	
	/// Header operations (synchronized by using atomics)
	
	/// NOTE: Since the owner_id will be removed from the chunk, the header will
	/// be changed.
	
	/// set_owner_id() writes an owner_id to the chunk.
	/**
	 * @param oid The owner_id value to write to the chunk.
	 * @return A reference to this chunk.
	 */
	const chunk& set_owner_id(owner_id oid) {
		InterlockedExchange64((__int64*)(elems +owner_id_begin), oid.get());
		return *this;
	}
	
	/// get_owner_id() returns the owner_id of the chunk.
	/**
	 * @return The owner_id.
	 */
	owner_id get_owner_id() const {
		return *((owner_id*)(elems +owner_id_begin));
	}
	
	/// set_user_data() writes user_data value to the chunk.
	/**
	 * @param u The user_data value to write to the chunk.
	 * @return A reference to this chunk.
	 */
	const chunk& set_user_data(user_data_type u) {
		InterlockedExchange64((__int64*)(elems +user_data_begin), u);
		return *this;
	}
	
	/// get_user_data() returns the user_data value of the chunk.
	/**
	 * @return The owner_id.
	 */
	user_data_type get_user_data() const {
		return *((user_data_type*)(elems +user_data_begin));
	}

	/**
	 * @param b The protocol value to write to the chunk.
	 * @return A reference to this chunk.
	 */
	const chunk& set_bmx_protocol(bmx_protocol_type b) {
		(*(bmx_protocol_type*)(elems +bmx_protocol_begin)) = b;
		return *this;
	}
	
	/**
	 * @return The bmx protocol type.
	 */
	bmx_protocol_type get_bmx_protocol() const {
		return *((bmx_protocol_type*)(elems +bmx_protocol_begin));
	}
	
	/// set_request_size() writes the request size value to the chunk.
	/**
	 * @param sz The request size value to write to the chunk.
	 * @return A reference to this chunk.
	 */
	const chunk& set_request_size(message_size_type sz) {
		InterlockedExchange((LONG*)(elems +request_size_begin), sz);
		return *this;
	}
	
	/// get_request_size() returns the request size value of the chunk.
	/**
	 * @return The request size.
	 */
	message_size_type get_request_size() const {
		return *((message_size_type*)(elems +request_size_begin));
	}
	
	/// set_link() writes a link value to the chunk.
	/**
	 * @param next The link value to write, that refers to the next chunk.
	 * @return A reference to this chunk.
	 */
	const chunk& set_link(link_type next) {
		InterlockedExchange((LONG*)(elems +stream_link_begin), next);
		return *this;
	}
	
	/// get_link() returns the link value of the chunk.
	/**
	 * @return The link value of the chunk.
	 */
	chunk_index get_link() const {
		return *((chunk_index*)(elems +stream_link_begin));
	}
	
	/// terminate_link() terminates the link of the chunk.
	/**
	 * @return A reference to this chunk.
	 */
	chunk& terminate_link() {
		InterlockedExchange((LONG*)(elems +stream_link_begin),
		link_terminator);
		return *this;
	}
	
	/// is_terminated() test if the link is terminated.
	/**
	 * @return true if the link value of the chunk is LINK_TERMINATOR, false
	 *		otherwise.
	 */
	bool is_terminated() const {
		return *((chunk_index*)(elems +stream_link_begin)) == link_terminator;
	}

	///-------------------------------------------------------------------------
	/// NOTE: Operations on next link is done only by a scheduler so no
	/// synchronization is needed.

	/// set_next() writes the next_ link value to the chunk.
	/**
	 * @param next The link value to write, that refers to the next chunk.
	 * @return A reference to this chunk.
	 */
	chunk& set_next(link_type next) {
		*((chunk_index*)(elems +next_link_begin)) = next;
		return *this;
	}
	
	/// get_next_link() returns the next_link value of the chunk.
	/**
	 * @return The link value of the chunk.
	 */
	chunk_index get_next() const {
		return *((chunk_index*)(elems +next_link_begin));
	}
	
	/// terminate_next_link() terminates the next_link of the chunk.
	/**
	 * @return A reference to this chunk.
	 */
	const chunk& terminate_next() {
		*((chunk_index*)(elems +next_link_begin)) = link_terminator;
		return *this;
	}
	
	/// is_next_terminated() test if the next_link is terminated.
	/**
	 * @return true if the next_link value of the chunk is link_terminator,
	 *		false otherwise.
	 */
	bool is_next_terminated() const {
		return *((chunk_index*)(elems +next_link_begin)) == link_terminator;
	}
	
	///-------------------------------------------------------------------------

	// Idéas:
	// overload iterators and such, so that they start after the header and
	// end at the link, etc.
	// 
	// Helper functions to read/write all built-in integer types: 8-, 16-, 32-
	// and 64-bit values, in host order, in big-endian, and in little-endian
	// order.
	//
	// Read/write sequence of N bytes (a char array), and a c-style string.
	//
	// It may also be nice to be able to write float and double for
	// some reason, but best is a conversion function from float to uint32_t and
	// from double to uint64_t, although this only works on systems with
	// sizeof(float) == sizeof(uint32_t) and sizeof(double) == sizeof(uint64_t).
};

typedef chunk<uint8_t, chunk_size> chunk_type;

#if 0 // helper functions for serialization over network
inline void host_to_big_endian(uint16_t&);
inline void host_to_big_endian(uint32_t&);
inline void host_to_big_endian(uint64_t&);
inline void host_to_little_endian(uint16_t&);
inline void host_to_little_endian(uint32_t&);
inline void host_to_little_endian(uint64_t&);
inline void big_endian_to_host(uint16_t&);
inline void big_endian_to_host(uint32_t&);
inline void big_endian_to_host(uint64_t&);
inline void little_endian_to_host(uint16_t&);
inline void little_endian_to_host(uint32_t&);
inline void little_endian_to_host(uint64_t&);
#endif // helper functions for serialization over network

} // namespace core
} // namespace starcounter

// Bmx handler type.
typedef starcounter::core::bmx_protocol_type BMX_HANDLER_TYPE;

#endif // STARCOUNTER_CORE_CHUNK_HPP
