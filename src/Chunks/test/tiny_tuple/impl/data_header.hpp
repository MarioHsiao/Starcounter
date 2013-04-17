//
// impl/data_header.hpp
//
// Copyright © 2006-2013 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//
// Implementation of data header.
// Revision: 8
//

#ifndef STARCOUNTER_CORE_TINY_TUPLE_RECORD_IMPL_DATA_HEADER_HPP
#define STARCOUNTER_CORE_TINY_TUPLE_RECORD_IMPL_DATA_HEADER_HPP

namespace starcounter {
namespace core {
namespace tiny_tuple {
namespace record {
namespace data_header {

FORCE_INLINE columns_type get_columns(pointer_type data_header) {
	return *((uint8_t*) data_header);
}

FORCE_INLINE void set_columns(pointer_type data_header, columns_type columns) {
	*((uint8_t*) data_header) = columns;
}

FORCE_INLINE offset_type get_offset_size(pointer_type data_header) {
	return (*((uint8_t*) data_header +1) & 7) +5;
}

FORCE_INLINE void set_offset_size(pointer_type data_header, offset_type offset) {
	// Read the value from bit 15:8.
	uint8_t value = *((uint8_t*) data_header +1);
	
	// Clear the OFFSET part.
	value &= ~offset_size_mask;
	
	// Encode the OFFSET range 5..12 to 0..7; then bitwise-or it into the value.
	value |= (offset -5) & 7;
	
	// Write the OFFSET.
	*((uint8_t*) data_header +1) = value;
}

FORCE_INLINE bool get_defined_column_flag(pointer_type data_header, index_type index) {
	uint32_t word = (index +static_field_size) >> 3;
	uint32_t bit = (index +static_field_size) & 7;
	return *((uint8_t*) data_header +word) >> bit;
}

FORCE_INLINE void set_defined_column_flag(pointer_type data_header, index_type index, bool state) {
	uint32_t word = (index +static_field_size) >> 3;
	uint8_t value = *((uint8_t*) data_header +word);
	uint32_t bit = (index +static_field_size) & 7;
	value &= ~(1 << bit);
 	value |= state << bit;
	
	// Write the DEFINED COLUMN flag.
	*((uint8_t*) data_header +word) = value;
}

FORCE_INLINE offset_distance_type get_distance_to_offset(pointer_type data_header, index_type index,
columns_type columns, offset_type offset_size) {
    return static_field_size +columns +index * offset_size;
}

FORCE_INLINE offset_type get_offset(pointer_type data_header, index_type index) {
    columns_type columns = get_columns(data_header);
    offset_size_type offset_size = get_offset_size(data_header);

    // Calculate distance to OFFSET at index.
    offset_distance_type offset_start = static_field_size +columns +index * offset_size;
	
    // Load the OFFSET from the byte aligned 32-bit word overlapping the OFFSET.
	uint32_t offset = *((uint32_t*)((uint8_t*) data_header +(offset_start >> 3)));
	offset >>= offset_start & 7;
    return offset & ((1 << offset_size) -1);
}

FORCE_INLINE void set_offset(pointer_type data_header, index_type index, offset_type offset) {
    columns_type columns = get_columns(data_header);
    offset_size_type offset_size = get_offset_size(data_header);

    // Calculate distance to OFFSET at index.
    offset_distance_type offset_start = static_field_size +columns +index * offset_size;
	
    // Load the OFFSET from the byte aligned 32-bit word overlapping the OFFSET.
	uint32_t old_offset = *((uint32_t*)((uint8_t*) data_header +(offset_start >> 3)));
    
    // Position the offset.
    offset <<= offset_start & 7;

    // Clear bits where the offset will be inserted.
    old_offset &= ~(((1 << offset_size) -1) << (offset_start & 7));
    old_offset |= offset;
    *((uint32_t*)((uint8_t*) data_header +(offset_start >> 3))) = offset;
}

FORCE_INLINE size_type size(pointer_type data_header) {
	// The data header size is stored in the first OFFSET.
    columns_type columns = get_columns(data_header);
    offset_size_type offset_size = get_offset_size(data_header);

    // Calculate distance to the first OFFSET.
    offset_distance_type first_offset_start = static_field_size +columns;
	
    // Load the first OFFSET from the byte aligned 32-bit word overlapping the OFFSET.
	uint32_t first_offset = *((uint32_t*)((uint8_t*) data_header +(first_offset_start >> 3)));
	first_offset >>= first_offset_start & 7;
    return first_offset & ((1 << offset_size) -1);
}

} // namespace data_header
} // namespace record
} // namespace tiny_tuple
} // namespace core
} // namespace starcounter

#endif // STARCOUNTER_CORE_TINY_TUPLE_RECORD_IMPL_DATA_HEADER_HPP
