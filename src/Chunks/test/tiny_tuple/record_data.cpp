//
// record_data.cpp
//
// Copyright © 2006-2013 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#include "record_data.hpp"

//namespace {

VM_PAGE_ALIGN

// Test record data
uint8_t record_data[record_data_size] = {
	0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
};

uint8_t* get_pointer_to_record_data(uint64_t offset) {
	return record_data +offset;
}

//} // namespace
