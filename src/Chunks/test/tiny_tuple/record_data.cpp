//
// record_data.cpp
//
// Copyright © 2006-2013 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#include "record_data.hpp"

VM_PAGE_ALIGN

// Test record data
uint8_t record_data[record_data_size] = {
	// Pretending that the RECORD HEADER is 3 bytes.
	0x00, 0x00, 0x00,

	//==========================================================================
	// DATA HEADER with one column value that is not defined:
	//
	//  3 3 2 2 2 2 2 2 2 2 2 2 1 1 1 1 1 1 1 1 1 1
	// .1 0 9 8 7 6 5 4.3 2 1 0 9 8 7 6.5 4 3 2 1 0 9 8.7 6 5 4 3 2 1 0.
	// +---------------+-------------+---------+-+-----+---------------+
	// |    next record|          PAD|   OFFSET|D|OFFSZ|        COLUMNS|
	// +---------------+-------------+---------+-+-----+---------------+
	// |               |x x x x x x x|0 0 0 1 1|0|0 0 0|0 0 0 0 0 0 0 1|
	// +-----------------------------+---------+-+-----+---------------+
	//
	// OFFSZ = OFFSET SIZE, expressing number of bits in each OFFSET in the
	// range 5..12 bits. So "0" = 5 and "7" = 12, by adding 5. The minimum
	// OFFSET SIZE is 5 bits.
	//
	// OFFSETS are relative to where the DATA HEADER begin and for a given
	// OFFSET, a corresponing DEFINED COLUMN VALUE start at the address DATA
	// HEADER +OFFSET.
	//
	// The last OFFSET (which is always stored) is not an offset to a DEFINED
	// COLUMN VALUE. Instead it is the size of the DATA HEADER including all
	// DEFINED COLUMN VALUES, and the next record begins at DATA HEADER +OFFSET
	// (the last OFFSET.)
	//
	// PAD = PAD BITS. x = Any value (not guaranteed to be 0.)
	
	///0x01, 0x30, 0x00

	//==========================================================================
	// DATA HEADER with eight column values that are not defined:
	//
	//  3 3 2 2 2 2 2 2 2 2 2 2 1 1 1 1 1 1 1 1 1 1
	// .1 0 9 8 7 6 5 4.3 2 1 0 9 8 7 6.5 4 3 2 1 0 9 8.7 6 5 4 3 2 1 0.
	// +---------------+---------+---------------+-----+---------------+
	// |    next record|   OFFSET|DEFINED COLUMNS|OFFSZ|        COLUMNS|
	// +---------------+---------+---------------+-----+---------------+
	// |               |0 0 0 1 1|0 0 0 0 0 0 0 0|0 0 0|0 0 0 0 1 0 0 0|
	// +---------------+---------+---------------+-----+---------------+
	//
	// There are no PAD bits here, since the next record is already byte
	// aligned.

	///0x08, 0x00, 0x18

	//==========================================================================
	// DATA HEADER with one DEFINED COLUMN VALUE:
	//
	//  3 3 2 2 2 2 2 2 2 2 2 2 1 1 1 1 1 1 1 1 1 1
	// .1 0 9 8 7 6 5 4.3 2 1 0 9 8 7 6.5 4 3 2 1 0 9 8.7 6 5 4 3 2 1 0.
	// +---------------+---+---------+---------+-+-----+---------------+
	// | DEFINED VALUES|PAD|   OFFSET|   OFFSET|D|OFFSZ|        COLUMNS|
	// +---------------+---+---------+---------+-+-----+---------------+
	// |n n n n n n n n|x x|0 0 1 0 0|0 0 0 1 1|1|0 0 0|0 0 0 0 0 0 0 1|
	// +---------------+---+---------+---------+-+-----+---------------+
	//
	// DEFINED VALUES = DEFINED COLUMN VALUES
	// n = First (and last) DEFINED COLUMN VALUE. It is 1 byte in size.

	0x01, 0x38, 0x08, 0xA2
};

uint8_t* get_pointer_to_record_data(uint64_t offset) {
	return record_data +offset;
}
