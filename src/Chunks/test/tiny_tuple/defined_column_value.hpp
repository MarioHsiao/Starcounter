//
// defined_column_value.hpp
//
// Copyright © 2006-2013 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//
// Revision: 8
//

#ifndef STARCOUNTER_CORE_TINY_TUPLE_RECORD_DEFINED_COLUMN_VALUE_HPP
#define STARCOUNTER_CORE_TINY_TUPLE_RECORD_DEFINED_COLUMN_VALUE_HPP

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
#include "macro_definitions.hpp"

namespace starcounter {
namespace core {
namespace tiny_tuple {
namespace record {
namespace defined_column_value {

// DEFINED COLUMN VALUES (revision 8):
// +-------------------------------------------------+
// | +---------------------------+  <== Byte aligned |
// | | DEFINED COLUMN VALUE 0    |                   |  First DEFINED COLUMN VALUE
// | +---------------------------+  <== Byte aligned |
// | | DEFINED COLUMN VALUE 1    |                   |  Second DEFINED COLUMN VALUE
// | +---------------------------+                   |
// |             •                                   |
// |             •                                   |
// |             •                                   |
// | +---------------------------+  <== Byte aligned |
// | | DEFINED COLUMN VALUE N -1 |                   |  Last DEFINED COLUMN VALUE
// | +---------------------------+                   |
// +-------------------------------------------------+  <== Byte aligned


typedef uint8_t* pointer_type;
typedef uint32_t size_type;

} // namespace defined_column_value
} // namespace record
} // namespace tiny_tuple
} // namespace core
} // namespace starcounter

#include "impl/defined_column_value.hpp"

#endif // STARCOUNTER_CORE_TINY_TUPLE_RECORD_DEFINED_COLUMN_VALUE_HPP
