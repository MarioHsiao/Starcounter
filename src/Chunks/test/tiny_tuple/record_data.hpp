//
// record_data.hpp
//
// Copyright © 2006-2013 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#ifndef RECORD_DATA_HPP
#define RECORD_DATA_HPP

#include <cstdint>
#include "macro_definitions.hpp"

enum {
	record_data_size = 1 << 12
};

VM_PAGE_ALIGN

extern uint8_t record_data[record_data_size];

extern uint8_t* get_pointer_to_record_data(uint64_t offset);

#endif // RECORD_DATA_HPP
