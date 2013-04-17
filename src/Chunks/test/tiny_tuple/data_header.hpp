//
// data_header.hpp
//
// Copyright © 2006-2013 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//
// Revision: 8
//

#ifndef STARCOUNTER_CORE_TINY_TUPLE_RECORD_DATA_HEADER_HPP
#define STARCOUNTER_CORE_TINY_TUPLE_RECORD_DATA_HEADER_HPP

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
namespace record {
namespace data_header {

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

typedef uint64_t* pointer_type;
typedef uint32_t columns_type;
typedef uint32_t offset_type;
typedef uint32_t index_type;

enum {
	// Field sizes:
	columns_bits = 8,
	offset_size_bits = 3,
	static_field_size = columns_bits +offset_size_bits,
	
	// Masks:
	columns_mask = (1 << columns_bits) -1,
	offset_size_mask = (1 << offset_size_bits) -1,
	static_fields_mask = (1 << static_field_size) -1
};

/// number_of_columns() returns number of columns value, 0 to 255.
/**
 * @param data_header The address of the byte aligned DATA HEADER.
 * @return The number of columns value, 0 to 255.
 */
FORCE_INLINE columns_type number_of_columns(pointer_type data_header);

/// offset_size() returns the offset size, 5 to 12.
/**
 * @param data_header The address of the byte aligned DATA HEADER.
 * @return The offset size, 5 to 12.
 */
FORCE_INLINE offset_type offset_size(pointer_type data_header);

} // namespace data_header
} // namespace record
} // namespace tiny_tuple
} // namespace core
} // namespace starcounter

#include "impl/data_header.hpp"

#endif // STARCOUNTER_CORE_TINY_TUPLE_RECORD_DATA_HEADER_HPP
