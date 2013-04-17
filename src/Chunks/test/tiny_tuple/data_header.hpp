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
typedef uint32_t offset_distance_type;
typedef uint32_t offset_size_type;
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

/// get_columns() returns number of COLUMNS value, 0 to 255.
/**
 * @param data_header The address of the byte aligned DATA HEADER.
 * @return The number of columns value, 0 to 255.
 */
FORCE_INLINE columns_type get_columns(pointer_type data_header);

/// set_columns() sets number of COLUMNS value, 0 to 255.
/**
 * @param data_header The address of the byte aligned DATA HEADER.
 * @param columns The COLUMN value to be written, 0 to 255. Other bits in the
 *      DATA HEADER are preserved.
 */
FORCE_INLINE void set_columns(pointer_type data_header, columns_type columns);

/// offset_size() returns the OFFSET size, 5 to 12.
/**
 * @param data_header The address of the byte aligned DATA HEADER.
 * @return The offset size, 5 to 12.
 */
FORCE_INLINE offset_type get_offset_size(pointer_type data_header);

/// set_offset_size() sets the OFFSET value, 5 to 12.
/**
 * @param data_header The address of the byte aligned DATA HEADER.
 * @param offset The OFFSET value to be written, 5 to 12. Other bits in the
 *      DATA HEADER are preserved.
 */
FORCE_INLINE void set_offset_size(pointer_type data_header, offset_type offset);

/// get_defined_column_flag() returns state of the column flag at index.
/**
 * @param data_header The address of the byte aligned DATA HEADER.
 * @param index The index of the DEFINED COLUMN FLAG.
 * @return The state of the column flag at index, false = not defined and
 *      true = defined.
 */
FORCE_INLINE bool get_defined_column_flag(pointer_type data_header, index_type index);

/// set_defined_column_flag() sets the state of the column flag at index.
/**
 * @param data_header The address of the byte aligned DATA HEADER.
 * @param index The index of the DEFINED COLUMN FLAG.
 * @param state The state to assign to the DEFINED COLUMN FLAG at index.
 */
FORCE_INLINE void set_defined_column_flag(pointer_type data_header, index_type index, bool state);

/// get_distance_to_offset() returns the distance of OFFSET at index, in number of bits
/// from the beginning of the DATA HEADER. This utility function is used by set_offset().
/**
 * @param data_header The address of the byte aligned DATA HEADER.
 * @param index The index of the OFFSET.
 * @param columns The number of COLUMNS in the DATA HEADER.
 * @param offset_size The number of bits (5 to 12) in each OFFSET.
 * @return The distance of OFFSET at index, in number of bits from the beginning
 *      of the DATA HEADER.
 */
FORCE_INLINE offset_distance_type get_distance_to_offset(pointer_type data_header, index_type index,
columns_type columns, offset_type offset_size);

/// get_offset() returns the OFFSET value at index, which is relative to the beginning of the DATA HEADER.
/**
 * @param data_header The address of the byte aligned DATA HEADER.
 * @param index The index of the OFFSET.
 * @return Tthe OFFSET value at index, which is relative to the beginning of the DATA HEADER.
 */
FORCE_INLINE offset_type get_offset(pointer_type data_header, index_type index);

} // namespace data_header
} // namespace record
} // namespace tiny_tuple
} // namespace core
} // namespace starcounter

#include "impl/data_header.hpp"

#endif // STARCOUNTER_CORE_TINY_TUPLE_RECORD_DATA_HEADER_HPP
