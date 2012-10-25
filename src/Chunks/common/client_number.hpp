//
// client_number.hpp
//
// Copyright © 2006-2012 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//
// Within each client process, any number of clients may be running with the
// limitation of max_number_of_clients (that may exist at the same time.)
// A client acquires a client_number to allocate a position. The client number
// is used to access this client's interface, etc.
//

#ifndef STARCOUNTER_CORE_CLIENT_NUMBER_HPP
#define STARCOUNTER_CORE_CLIENT_NUMBER_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#include <cstddef>

namespace starcounter {
namespace core {

//typedef std::size_t client_number;
typedef uint32_t client_number;

const client_number no_client_number = -1;

} // namespace core
} // namespace starcounter

#endif // STARCOUNTER_CORE_CLIENT_NUMBER_HPP
