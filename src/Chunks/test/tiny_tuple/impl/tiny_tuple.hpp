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
		return 0;
	}

word_offset_2: {
		// The 64-bit word at offset 2 from data_header has been loaded into
		// column_field_with_flag. data_header[0] and data_header[1] also
		// overlapps the DEFINED COLUMNS (0-255 flags field) and need to be
		// loaded.
		return 0;
	}

word_offset_3: {
		// The 64-bit word at offset 3 from data_header has been loaded into
		// column_field_with_flag. data_header[0], data_header[1] and
		// data_header[2] also overlapps the DEFINED COLUMNS (0-255 flags
		// field) and need to be loaded.
		return 0;
	}

word_offset_4: {
		// The 64-bit word at offset 4 from data_header has been loaded into
		// column_field_with_flag. data_header[0], data_header[1],
		// data_header[2] and data_header[3] also overlapps the DEFINED
		// COLUMNS (0-255 flags field) and need to be loaded.
		return 0;
	}

#if 0
	uint32_t v = *((uint32_t*)((uint8_t*) data_header +offset));
	return (uint8_t*) p +offset;
	
	// The COLUMNS and OFFSET SIZE are compactly stored in bit 10:0 at the
	// beginning of the DATA HEADER, in little-endian format:
	//
	//  1
	//  0 9 8 7 6 5 4 3 2 1 0
	// +-----+---------------+
	// |OFFSZ|        COLUMNS|
	// +-----+---------------+
	// OFFSZ = OFFSET SIZE
	//
	// Reading the first 64-bits of the DATA HEADER. It contains a part or the
	// whole DEFINED COLUMNS field, depending on number of COLUMNS.
	register uint64_t r0 = *((uint64_t* RESTRICT) data_header);

	//--------------------------------------------------------------------------
	// Extracting the COLUMNS value from r0. The OFFSET SIZE and the DATA HEADER
	// SIZE fields might not be needed before returning, so it makes sense to
	// rearrange these fields so that COLUMNS are in 7:0, to avoid the shift.
	register uint64_t columns = (r0 >> offset_size_bits +data_header_size_bits)
	& columns_mask;

	//--------------------------------------------------------------------------
	// The method used here is to construct a jump-table and switch on
	// column_words which is the number of 64-bit words that overlay the DEFINED
	// COLUMNS (field with 0-255 column flags), including the preceeding fields
	// in bit 19:0.
	register uint64_t column_words
	= (index +data_header_size_bits +offset_size_bits +columns_bits +63) >> 6;

	// It would be best to extract the defined_columns flag first and create a
	// mask of it and apply that to column_words, so that if the flag is 0, then
	// column_words is 0, and in case 0 return 0.

	


	// TODO: Verify that the compiler creates a jump-table. If not, this method
	// should not be used. The code must be branchless.
	switch (column_words) {
	//case 0:
		// extract the column_flag at index, shift it to bit 0, then
		// column_words &= -column_flag; will yield 0 when the column value is
		// not defined. So here we return 0.
		// Won't happen because this function will not be called on a 0-tuple.
		//break;
	case 1:
		// The DEFINED DEFINED COLUMNS is 1-44 bits and fit into r0.
		// Clear bits from index onwards.
		uint64_t defined_columns_field = r0 & data_header_static_fields_mask;
		defined_columns_field &= (1ULL << index) -1;
		uint32_t defined_columns = population_count(defined_columns_field);
		break;
	default:
		UNREACHABLE;
	}
	//--------------------------------------------------------------------------
	// In the DEFINED DEFINED COLUMNS (field with 0-255 column flags), a '0'
	// denotes "default value", and a '1' denotes "defined value". This field
	// starts on bit 20 up to bit 274 at most. This value expresses the
	// order of the tuple. 0-tuples won't exist. An ordered tuple is a sequence
	// of elements.
	//
	// Current architectures can do the POPCNT operation on a 64-bit word, so
	// multiple POPCNT operations may need to be performed depending on number
	// of elements in the tuple and the index of the element to access.
	//
//------------------------------------------------------------------------------
	// Compute offset_index. Mask and POPCNT, etc. I have several idéas.
	uint32_t offset_index = 0; // Test with 0 for now.
	// If the flag is set, continue and load the defined value, otherwise return
	// 0 instantly.
//------------------------------------------------------------------------------
	// After the DEFINED COLUMNS follows the OFFSET[S], at bit columns
	// +20 and its range is at most 12-bits. The code will load
	uint32_t offset_bit_start = 20 +columns
	+(offset_index * offset_size);

	// Now the OFFSET value and the OFFSET value after it is loaded into a 64-
	// bit word laying over the OFFSETs. The OFFSETs are not byte aligned so
	// when the word is loaded the OFFSETs may be up to 7 bits from LSB.
	// Both OFFSETs must fit in the remaining 57 bits, so each OFFSET can be
	// up to 28-bits. Current limit is set to 20-bits. If tiny tuple
	// need to support larger than 28-bit OFFSETs then another 64-bit word
	// must be loaded, etc.
	uint64_t offset = *((uint64_t*)(data_header +(offset_bit_start >> 3)));
	offset >>= offset_bit_start & 7;
	uint64_t next_offset = offset >> offset_size;
	uint64_t offset_mask = (1 << offset_size) -1;
	offset &= offset_mask;
	next_offset &= offset_mask;

	// Now we have the two offsets. Set the size of the element:
	*size = next_offset -offset;

	// Another variant returns a 64-bit element value and the size is not needed.

	// BUG! This is incorrect because the first OFFSET is element[1] not [0].
	// So this needs to be fixed. The last OFFSET 
	// PowerPC has instructions to shift and mask. Do Intel have it? I think no.

//------------------------------------------------------------------------------
	// Set the size of the value by computing the distance to the next element.

	// Finally the pointer to
	value_pointer ep = 0;

	std::cout << "data_header_size: " << data_header_size << "\n"
	<< "offset_size: " << offset_size << "\n"
	<< "number_of_columns: " << number_of_columns << "\n";

	// If 

	// The column value is not defined. Returning 0 to indicate "use the
	// default value."
	return ep;
#endif
	return 0;
}

} // namespace record
} // namespace tiny_tuple
} // namespace core
} // namespace starcounter

#endif // STARCOUNTER_CORE_TINY_TUPLE_IMPL_RECORD_HPP
