//
// initialize.hpp
// server
//
// Copyright © 2006-2011 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#ifndef STARCOUNTER_CORE_INITIALIZE_HPP
#define STARCOUNTER_CORE_INITIALIZE_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#include "../common/database_shared_memory_parameters.hpp"

namespace starcounter {
namespace core {

unsigned long initialize(
    const char* segment_name,
    const char* server_name,
    std::size_t schedulers,
    bool is_system,
    uint32_t chunks_total_number,
    uint8_t gateway_num_workers);

} // namespace core
} // namespace starcounter

#endif // STARCOUNTER_CORE_INITIALIZE_HPP
