//
// owner_id_value_type.h
//
// Copyright © 2006-2012 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#ifndef OWNER_ID_VALUE_TYPE_H
#define OWNER_ID_VALUE_TYPE_H

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#include <stdint.h>
#include "macro_definitions.hpp"

#if defined (IPC_OWNER_ID_IS_32_BIT)
typedef uint32_t owner_id_value_type;
#else // !defined (IPC_OWNER_ID_IS_32_BIT)
typedef uint64_t owner_id_value_type;
#endif // defined (IPC_OWNER_ID_IS_32_BIT)

#endif // OWNER_ID_VALUE_TYPE_H
