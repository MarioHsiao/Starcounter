//
// record_header.hpp
//
// Copyright © 2006-2013 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//
// Revision: 8
//

#ifndef STARCOUNTER_CORE_TINY_TUPLE_RECORD_HEADER_HPP
#define STARCOUNTER_CORE_TINY_TUPLE_RECORD_HEADER_HPP

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
namespace record_header {

// RECORD HEADER (revision 8):
// +-------------------------------------------------+  <== Byte aligned
// | IS PRIMARY SLOT (1-bit)                         |
// +-------------------------------------------------+
// | HAS TAIL (1-bit)                                |
// +-------------------------------------------------+
// | IS DELETED (1-bit)                              |
// +-------------------------------------------------+
// | IS HIDDEN (1-bit)                               |
// +-------------------------------------------------+
// | HEADER SIZE (4-bit)                             |
// +-------------------------------------------------+  <== Byte aligned
// | GENERATION STAMP                                |  1 to 8 bytes
// +-------------------------------------------------+  <== Byte aligned

enum {
	// Flags
	is_primary_slot = 1 << 0,
	has_tail = 1 << 1,
	is_deleted = 1 << 2,
	is_hidden = 1 << 3
};

typedef uint64_t* pointer;

} // namespace record_header
} // namespace record
} // namespace tiny_tuple
} // namespace core
} // namespace starcounter

#include "impl/record_header.hpp"

#endif // STARCOUNTER_CORE_TINY_TUPLE_RECORD_HEADER_HPP
