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

#include "record_header.hpp"
#include "data_header.hpp"
#include "defined_column_value.hpp"
#include "macro_definitions.hpp"

namespace starcounter {
namespace core {
namespace tiny_tuple {
namespace record {

// A tiny tuple record consists of three parts:
// RECORD HEADER
// DATA HEADER
// DEFINED COLUMN VALUES

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
defined_column_value::pointer_type get_pointer_to_value(
data_header::pointer_type /* RESTRICT */ data_header,
data_header::index_type index,
defined_column_value::size_type* /* RESTRICT */ size);

// Inserts
// Updates

} // namespace record
} // namespace tiny_tuple
} // namespace core
} // namespace starcounter

#include "impl/tiny_tuple.hpp"

#endif // STARCOUNTER_CORE_TINY_TUPLE_HPP
