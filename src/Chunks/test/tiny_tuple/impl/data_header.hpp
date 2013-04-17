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

FORCE_INLINE columns_type number_of_columns(pointer_type data_header) {
	return *((uint8_t*) data_header);
}

FORCE_INLINE offset_type offset_size(pointer_type data_header) {
	return (*((uint8_t*) data_header +1) & 7) +5;
}

} // namespace data_header
} // namespace record
} // namespace tiny_tuple
} // namespace core
} // namespace starcounter

#endif // STARCOUNTER_CORE_TINY_TUPLE_RECORD_IMPL_DATA_HEADER_HPP
