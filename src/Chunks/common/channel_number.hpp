//
// channel_number.hpp
//
// Copyright © 2006-2012 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#ifndef STARCOUNTER_CORE_CHANNEL_NUMBER_HPP
#define STARCOUNTER_CORE_CHANNEL_NUMBER_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#include <cstddef>

namespace starcounter {
namespace core {

typedef uint32_t channel_number;

const channel_number invalid_channel_number = -1;

} // namespace core
} // namespace starcounter

#endif // STARCOUNTER_CORE_CHANNEL_NUMBER_HPP
