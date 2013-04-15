//
// tiny_tuple.hpp
//
// Copyright © 2006-2013 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//
// Revision: 8
//

#ifndef STARCOUNTER_CORE_TINY_TUPLE_HPP
#define STARCOUNTER_CORE_TINY_TUPLE_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#if defined(_MSC_VER) && (_MSC_VER >= 1700)
 #include <cstdint>
#else
# include <tr1/cstdint>
#endif // defined(_MSC_VER) && (_MSC_VER >= 1700)

#include "macro_definitions.hpp"

namespace starcounter {
namespace core {
namespace tiny_tuple {

// A tiny tuple record consists of three parts:
// RECORD HEADER
// DATA HEADER
// DEFINED COLUMN VALUES

// The layout is:

// RECORD HEADER (revision 8):
// +-------------------------------------------------+  <== Byte aligned
// | IS PRIMARY SLOT (1-bit)                         |
// +-------------------------------------------------+
// | HAS TAIL (1-bit)                                |
// +-------------------------------------------------+
// | IS DELETED (1-bit)                              |
// +-------------------------------------------------+
// | IS HIDDEN (1-bit)                               |
// +-------------------------------------------------+
// | HEADER SIZE (4-bit)                             |
// +-------------------------------------------------+  <== Byte aligned
// | GENERATION STAMP                                |  1 to 8 bytes
// +-------------------------------------------------+  <== Byte aligned
// : DATA HEADER. . .                                :


// DATA HEADER (revision 8):
// +-------------------------------------------------+  <== Byte aligned
// | COLUMNS (8-bit field)                           |  Number of columns (0-255) in the DEFINED COLUMN(S) field
// +-------------------------------------------------+  <== Byte aligned
// | OFFSET SIZE (3-bit field)                       |
// +-------------------------------------------------+
// | DEFINED COLUMN(S) (field with 0-255 flags)      |
// +-------------------------------------------------+
// | OFFSETS[N +1]:                                  |
// | +--------------------------------+              |
// | | OFFSET 0 (OFFSET SIZE bits)    |              |  Offset to first DEFINED COLUMN VALUE
// | +--------------------------------+              |
// | | OFFSET 1 (OFFSET SIZE bits)    |              |  Offset to second DEFINED COLUMN VALUE
// | +--------------------------------+              |
// |                •                                |
// |                •                                |
// |                •                                |
// | +--------------------------------+              |
// | | OFFSET N -1 (OFFSET SIZE bits) |              |  Offset to last DEFINED COLUMN VALUE
// | +--------------------------------+              |
// | | OFFSET N (OFFSET SIZE bits)    |              |  Offset to the next record
// | +--------------------------------+              |
// +-------------------------------------------------+
// | PAD (0-7 pad bits, not guaranteed to be "0")    |
// +-------------------------------------------------+  <== Byte aligned
// : DEFINED COLUMN VALUES:                          :


// DEFINED COLUMN VALUES (revision 8):
// +-------------------------------------------------+
// | +---------------------------+  <== Byte aligned |
// | | DEFINED COLUMN VALUE 0    |                   |  First DEFINED COLUMN VALUE
// | +---------------------------+  <== Byte aligned |
// | | DEFINED COLUMN VALUE 1    |                   |  Second DEFINED COLUMN VALUE
// | +---------------------------+                   |
// |             •                                   |
// |             •                                   |
// |             •                                   |
// | +---------------------------+  <== Byte aligned |
// | | DEFINED COLUMN VALUE N -1 |                   |  Last DEFINED COLUMN VALUE
// | +---------------------------+                   |
// +-------------------------------------------------+  <== Byte aligned
// : Next record. . .                                :





// Minimal DATA HEADER with one column value that is not defined require 24 bits
// and the record is thus minimum 24-bits. The first 64-bits will be read anyway:
//
//  3 3 2 2 2 2 2 2 2 2 2 2 1 1 1 1 1 1 1 1 1 1
// .1 0 9 8 7 6 5 4.3 2 1 0 9 8 7 6.5 4 3 2 1 0 9 8.7 6 5 4 3 2 1 0.
// +---------------+-------------+---------+-+-----+---------------+
// |    next record|          PAD|   OFFSET|D|OFFSZ|        COLUMNS|
// +---------------+-------------+---------+-+-----+---------------+
// |               |x x x x x x x|0 0 0 1 1|0|0 0 0|0 0 0 0 0 0 0 1|
// +-----------------------------+---------+-+-----+---------------+
//
// OFFSZ = OFFSET SIZE, expressing number of bits in each OFFSET in the range 5..12 bits.
// So "0" = 5 and "7" = 12, by adding 5. The minimum OFFSET SIZE is 5 bits.
//
// OFFSETS are relative to where the DATA HEADER begin and for a given OFFSET,
// a corresponing DEFINED COLUMN VALUE start at the address DATA HEADER +OFFSET.
//
// The last OFFSET (which is always stored) is not an offset to a DEFINED COLUMN
// VALUE. Instead it is the size of the DATA HEADER record including all DEFINED
// COLUMN VALUES, and the next record begins at DATA HEADER +OFFSET (the last
// OFFSET.)
//
// PAD = PAD BITS.
// x = Any value (not guaranteed to be 0.)
//
// DEFINED VALUES = DEFINED COLUMN VALUES
// n = First (and last) DEFINED COLUMN VALUE. It is 1 byte in size.
//


// Minimal DATA HEADER with one DEFINED COLUMN VALUE is 24 bits, and the record
// is minimum 32-bits. The first 64-bits will be read anyway:
//
//  3 3 2 2 2 2 2 2 2 2 2 2 1 1 1 1 1 1 1 1 1 1
// .1 0 9 8 7 6 5 4.3 2 1 0 9 8 7 6.5 4 3 2 1 0 9 8.7 6 5 4 3 2 1 0.
// +---------------+---+---------+---------+-+-----+---------------+
// | DEFINED VALUES|PAD|   OFFSET|   OFFSET|D|OFFSZ|        COLUMNS|
// +---------------+---+---------+---------+-+-----+---------------+
// |n n n n n n n n|x x|0 0 1 0 0|0 0 0 1 1|1|0 0 0|0 0 0 0 0 0 0 1|
// +---------------+---+---------+---------+-+-----+---------------+
//
// OFFSZ = OFFSET SIZE, expressing number of bits in each OFFSET in the range 5..12 bits.
// So "0" = 5 and "7" = 12, by adding 5. The minimum OFFSET SIZE is 5 bits.
//
// OFFSETS are relative to where the DATA HEADER begin and for a given OFFSET,
// a corresponing DEFINED COLUMN VALUE start at the address DATA HEADER +OFFSET.
//
// The last OFFSET (which is always stored) is not an offset to a DEFINED COLUMN
// VALUE. Instead it is the size of the DATA HEADER record including all DEFINED
// COLUMN VALUES, and the next record begins at DATA HEADER +OFFSET (the last
// OFFSET.)
//
// PAD = PAD BITS.
// x = Any value (not guaranteed to be 0.)
//
// DEFINED VALUES = DEFINED COLUMN VALUES
// n = First (and last) DEFINED COLUMN VALUE. It is 1 byte in size.
//

//  3 3 2 2 2 2 2 2 2 2 2 2 1 1 1 1 1 1 1 1 1 1
// .1 0 9 8 7 6 5 4.3 2 1 0 9 8 7 6.5 4 3 2 1 0 9 8.7 6 5 4 3 2 1 0.
// +---------------+---------+---------------+-----+---------------+
// |    next record|   OFFSET|DEFINED COLUMNS|OFFSZ|        COLUMNS|
// +---------------+---------+---------------+-----+---------------+
// |               |0 0 0 1 1|0 0 0 0 0 0 0 0|0 0 0|0 0 0 0 0 0 0 1|
// +---------------+---------+---------------+-----+---------------+

// Reading the byte aligned 32-bit word overlapping the OFFSET and the next
// OFFSET:
//
//  3 3 3 3 3 3 3 3 3 3 2 2 2 2 2 2 2 2 2 2 1 1 1 1 1 1 1 1 1 1
// .9 8 7 6 5 4 3 2.1 0 9 8 7 6 5 4.3 2 1 0 9 8 7 6.5 4 3 2 1 0 9 8.
// +---------------+---------------+---+---------+---------+-+-----+
// |               | DEFINED VALUES|PAD|   OFFSET|   OFFSET|D|OFFSZ|
// +---------------+---------------+---+---------+---------+-+-----+
// |               |n n n n n n n n|x x|0 0 1 0 0|0 0 0 1 1|1|0 0 0|
// +---------------+---------------+---+---------+---------+-+-----+


// Types.
typedef uint64_t* data_header_pointer;
typedef uint32_t index_type;
typedef uint32_t size_type;
typedef uint8_t* value_pointer;

enum {
	// Field sizes:
	columns_bits = 8,
	offset_size_bits = 3,
	data_header_static_field_size = columns_bits +offset_size_bits,
	
	// Masks:
	columns_mask = (1 << columns_bits) -1,
	offset_size_mask = (1 << offset_size_bits) -1,
	data_header_static_fields_mask = (1 << data_header_static_field_size) -1
};

/// get_pointer_to_value() gets a pointer to an value in the tuple, if
/// it is defined.
/**
 * @param data_header The address of the byte aligned DATA HEADER.
 * @param index The index of the value to retrieve. It is assumed that it is a
 *		valid index.
 * @param size A pointer to a size_type which upon return will contain
 *		the size (in bytes) of the value, if the value is defined. If the
 *		value is not defined, size is not changed.
 * @return A pointer to the value if it is defined, or 0 if not defined.
 */
//value_pointer get_pointer_to_value(data_header_pointer RESTRICT data_header,
//index_type index, size_type* RESTRICT size);
value_pointer get_pointer_to_value(data_header_pointer data_header,
index_type index, size_type* size);

} // namespace tiny_tuple
} // namespace core
} // namespace starcounter

#include "impl/tiny_tuple.hpp"

#endif // STARCOUNTER_CORE_TINY_TUPLE_HPP
