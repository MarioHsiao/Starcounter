//
// impl/tiny_tuple.hpp
//
// Copyright © 2006-2013 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//
// Implementation of tiny tuple.
// Revision: 8
//

#ifndef STARCOUNTER_CORE_TINY_TUPLE_IMPL_RECORD_HPP
#define STARCOUNTER_CORE_TINY_TUPLE_IMPL_RECORD_HPP

namespace starcounter {
namespace core {
namespace tiny_tuple {
namespace record {

defined_column_value::pointer_type get_pointer_to_value(
data_header::pointer_type /* RESTRICT */ data_header,
data_header::index_type index,
defined_column_value::size_type* /* RESTRICT */ size) {
	// word_offset contains the DEFINED COLUMN flag, among other things.
	uint64_t word_offset = (index +data_header::static_field_size) >> 6;
	
	// Loading a word that most likely is not cached. This will take around
	// 167 cycles, depending on the hardware.
	uint64_t column_field_with_flag = data_header[word_offset];
	uint64_t bit = (index +data_header::static_field_size) & 63;
	
	switch ((column_field_with_flag >> bit) & 1) {
	case 0:
		// Returning 0 to indicate that the value is not defined. The caller can
		// get the default value via the schema. size is not modified.
		return 0;
		
	case 1:
		// The value is defined.
		switch (word_offset) {
		case 0:
			goto word_offset_0;
		
		case 1:
			goto word_offset_1;
		
		case 2:
			goto word_offset_2;
		
		case 3:
			goto word_offset_3;
		
		case 4:
			goto word_offset_4;
		
		default:
			UNREACHABLE;
		}
		break;
		
	default:
		UNREACHABLE;
	}
	
word_offset_0: {
		// The 64-bit word at offset 0 from data_header has been loaded into
		// column_field_with_flag. No other words are overlapping the
		// DEFINED COLUMNS (0-255 flags field).

		// Copy the COLUMNS and OFFSET SIZE before getting rid of those fields in this word.
		uint32_t columns = column_field_with_flag & data_header::columns_mask;
		uint32_t offset_size = ((column_field_with_flag >> data_header::columns_bits) & data_header::offset_size_mask) +5;

		// Before doing population_count(), clear bits 63:index in column_field_with_flag.
		column_field_with_flag &= (1ULL << bit) -1;

		// Get rid of the OFFSET SIZE and COLUMNS.
		column_field_with_flag >>= data_header::static_field_size;

		uint32_t number_of_defined_columns_before_index = population_count(column_field_with_flag);
		
		// Calculate which bit the OFFSET begins relative to where the data_header begins.
		uint32_t offset_start = data_header::static_field_size +columns +(number_of_defined_columns_before_index * offset_size);

		// Load the OFFSET and the next OFFSET from the byte aligned 32-bit word overlapping the two offsets.
		uint32_t offset = *((uint32_t*)((uint8_t*) data_header +(offset_start >> 3)));
		offset >>= offset_start & 7;
		uint32_t next_offset = offset >> offset_size;
		uint32_t offset_mask = (1 << offset_size) -1;
		offset &= offset_mask;
		next_offset &= offset_mask;
		
		// Now we have the two offsets. Set the size of the DEFINED COLUMN VALUE.
		*size = next_offset -offset;

		// Return a pointer to the DEFINED COLUMN VALUE.
		return (uint8_t*) data_header +offset;
	}

word_offset_1: {
		// The 64-bit word at offset 1 from data_header has been loaded into
		// column_field_with_flag. data_header[0] also overlapps the
		// DEFINED COLUMNS (0-255 flags field) and need to be loaded.

		// Copy the COLUMNS and OFFSET SIZE before getting rid of those fields in this word.
		uint32_t columns = column_field_with_flag & data_header::columns_mask;
		uint32_t offset_size = ((column_field_with_flag >> data_header::columns_bits) & data_header::offset_size_mask) +5;

		// Before doing population_count(), clear bits 63:index in column_field_with_flag.
		column_field_with_flag &= (1ULL << bit) -1;

		// Get rid of the OFFSET SIZE and COLUMNS.
		column_field_with_flag >>= data_header::static_field_size;

		uint32_t number_of_defined_columns_before_index = population_count(column_field_with_flag);
		
		// Load word at offset 0.
		uint64_t word_0 = data_header[0];

		// Get rid of the OFFSET SIZE and COLUMNS.
		word_0 >>= data_header::static_field_size;
		number_of_defined_columns_before_index += population_count(word_0);

		// Calculate which bit the OFFSET begins relative to where the data_header begins.
		uint32_t offset_start = data_header::static_field_size +columns +(number_of_defined_columns_before_index * offset_size);

		// Load the OFFSET and the next OFFSET from the byte aligned 32-bit word overlapping the two offsets.
		uint32_t offset = *((uint32_t*)((uint8_t*) data_header +(offset_start >> 3)));
		offset >>= offset_start & 7;
		uint32_t next_offset = offset >> offset_size;
		uint32_t offset_mask = (1 << offset_size) -1;
		offset &= offset_mask;
		next_offset &= offset_mask;
		
		// Now we have the two offsets. Set the size of the DEFINED COLUMN VALUE.
		*size = next_offset -offset;

		// Return a pointer to the DEFINED COLUMN VALUE.
		return (uint8_t*) data_header +offset;
	}

word_offset_2: {
		// The 64-bit word at offset 2 from data_header has been loaded into
		// column_field_with_flag. data_header[0] and data_header[1] also
		// overlapps the DEFINED COLUMNS (0-255 flags field) and need to be
		// loaded.

		// Copy the COLUMNS and OFFSET SIZE before getting rid of those fields in this word.
		uint32_t columns = column_field_with_flag & data_header::columns_mask;
		uint32_t offset_size = ((column_field_with_flag >> data_header::columns_bits) & data_header::offset_size_mask) +5;

		// Before doing population_count(), clear bits 63:index in column_field_with_flag.
		column_field_with_flag &= (1ULL << bit) -1;

		// Get rid of the OFFSET SIZE and COLUMNS.
		column_field_with_flag >>= data_header::static_field_size;

		uint32_t number_of_defined_columns_before_index = population_count(column_field_with_flag);
		
		// Load word at offset 0.
		uint64_t word_0 = data_header[0];

		// Get rid of the OFFSET SIZE and COLUMNS.
		word_0 >>= data_header::static_field_size;
		number_of_defined_columns_before_index += population_count(word_0);

		// Load word at offset 1.
		uint64_t word_1 = data_header[1];

		// Get rid of the OFFSET SIZE and COLUMNS.
		number_of_defined_columns_before_index += population_count(word_1);

		// Calculate which bit the OFFSET begins relative to where the data_header begins.
		uint32_t offset_start = data_header::static_field_size +columns +(number_of_defined_columns_before_index * offset_size);

		// Load the OFFSET and the next OFFSET from the byte aligned 32-bit word overlapping the two offsets.
		uint32_t offset = *((uint32_t*)((uint8_t*) data_header +(offset_start >> 3)));
		offset >>= offset_start & 7;
		uint32_t next_offset = offset >> offset_size;
		uint32_t offset_mask = (1 << offset_size) -1;
		offset &= offset_mask;
		next_offset &= offset_mask;
		
		// Now we have the two offsets. Set the size of the DEFINED COLUMN VALUE.
		*size = next_offset -offset;

		// Return a pointer to the DEFINED COLUMN VALUE.
		return (uint8_t*) data_header +offset;
	}

word_offset_3: {
		// The 64-bit word at offset 3 from data_header has been loaded into
		// column_field_with_flag. data_header[0], data_header[1] and
		// data_header[2] also overlapps the DEFINED COLUMNS (0-255 flags
		// field) and need to be loaded.

		// Copy the COLUMNS and OFFSET SIZE before getting rid of those fields in this word.
		uint32_t columns = column_field_with_flag & data_header::columns_mask;
		uint32_t offset_size = ((column_field_with_flag >> data_header::columns_bits) & data_header::offset_size_mask) +5;

		// Before doing population_count(), clear bits 63:index in column_field_with_flag.
		column_field_with_flag &= (1ULL << bit) -1;

		// Get rid of the OFFSET SIZE and COLUMNS.
		column_field_with_flag >>= data_header::static_field_size;

		uint32_t number_of_defined_columns_before_index = population_count(column_field_with_flag);
		
		// Load word at offset 0.
		uint64_t word_0 = data_header[0];

		// Get rid of the OFFSET SIZE and COLUMNS.
		word_0 >>= data_header::static_field_size;
		number_of_defined_columns_before_index += population_count(word_0);

		// Load word at offset 1.
		uint64_t word_1 = data_header[1];

		// Get rid of the OFFSET SIZE and COLUMNS.
		number_of_defined_columns_before_index += population_count(word_1);

		// Load word at offset 2.
		uint64_t word_2 = data_header[2];

		number_of_defined_columns_before_index += population_count(word_2);

		// Calculate which bit the OFFSET begins relative to where the data_header begins.
		uint32_t offset_start = data_header::static_field_size +columns +(number_of_defined_columns_before_index * offset_size);

		// Load the OFFSET and the next OFFSET from the byte aligned 32-bit word overlapping the two offsets.
		uint32_t offset = *((uint32_t*)((uint8_t*) data_header +(offset_start >> 3)));
		offset >>= offset_start & 7;
		uint32_t next_offset = offset >> offset_size;
		uint32_t offset_mask = (1 << offset_size) -1;
		offset &= offset_mask;
		next_offset &= offset_mask;
		
		// Now we have the two offsets. Set the size of the DEFINED COLUMN VALUE.
		*size = next_offset -offset;

		// Return a pointer to the DEFINED COLUMN VALUE.
		return (uint8_t*) data_header +offset;
	}

word_offset_4: {
		// The 64-bit word at offset 4 from data_header has been loaded into
		// column_field_with_flag. data_header[0], data_header[1],
		// data_header[2] and data_header[3] also overlapps the DEFINED
		// COLUMNS (0-255 flags field) and need to be loaded.

		// Copy the COLUMNS and OFFSET SIZE before getting rid of those fields in this word.
		uint32_t columns = column_field_with_flag & data_header::columns_mask;
		uint32_t offset_size = ((column_field_with_flag >> data_header::columns_bits) & data_header::offset_size_mask) +5;

		// Before doing population_count(), clear bits 63:index in column_field_with_flag.
		column_field_with_flag &= (1ULL << bit) -1;

		// Get rid of the OFFSET SIZE and COLUMNS.
		column_field_with_flag >>= data_header::static_field_size;

		uint32_t number_of_defined_columns_before_index = population_count(column_field_with_flag);
		
		// Load word at offset 0.
		uint64_t word_0 = data_header[0];

		// Get rid of the OFFSET SIZE and COLUMNS.
		word_0 >>= data_header::static_field_size;
		number_of_defined_columns_before_index += population_count(word_0);

		// Load word at offset 1.
		uint64_t word_1 = data_header[1];

		// Get rid of the OFFSET SIZE and COLUMNS.
		number_of_defined_columns_before_index += population_count(word_1);

		// Load word at offset 2.
		uint64_t word_2 = data_header[2];
		number_of_defined_columns_before_index += population_count(word_2);

		// Load word at offset 3.
		uint64_t word_3 = data_header[3];
		number_of_defined_columns_before_index += population_count(word_3);

		// Calculate which bit the OFFSET begins relative to where the data_header begins.
		uint32_t offset_start = data_header::static_field_size +columns +(number_of_defined_columns_before_index * offset_size);

		// Load the OFFSET and the next OFFSET from the byte aligned 32-bit word overlapping the two offsets.
		uint32_t offset = *((uint32_t*)((uint8_t*) data_header +(offset_start >> 3)));
		offset >>= offset_start & 7;
		uint32_t next_offset = offset >> offset_size;
		uint32_t offset_mask = (1 << offset_size) -1;
		offset &= offset_mask;
		next_offset &= offset_mask;
		
		// Now we have the two offsets. Set the size of the DEFINED COLUMN VALUE.
		*size = next_offset -offset;

		// Return a pointer to the DEFINED COLUMN VALUE.
		return (uint8_t*) data_header +offset;
	}
}

} // namespace record
} // namespace tiny_tuple
} // namespace core
} // namespace starcounter

#endif // STARCOUNTER_CORE_TINY_TUPLE_IMPL_RECORD_HPP
