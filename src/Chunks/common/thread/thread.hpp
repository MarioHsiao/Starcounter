//
// thread.hpp
//
// Copyright © 2006-2013 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#ifndef STARCOUNTER_CORE_THREAD_THREAD_HPP
#define STARCOUNTER_CORE_THREAD_THREAD_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#if !defined(_WIN32) && !defined(_WIN64) // Unix
# if defined(_POSIX_THREADS) && (_POSIX_THREADS +0 >= 0)
#  include "posix_thread.hpp"
#  define STARCOUNTER_CORE_HAS_PTHREADS
# endif // defined(_POSIX_THREADS) && (_POSIX_THREADS +0 >= 0)
#elif defined(_MSC_VER) // Windows
# include "windows_thread.hpp"
# define STARCOUNTER_CORE_HAS_WINTHREADS
#else
# error "Starcounter core threads unavailable on this platform"
#endif // !defined(_WIN32) && !defined(_WIN64)

namespace starcounter {
namespace core {

#if defined(STARCOUNTER_CORE_HAS_PTHREADS)
typedef posix_thread thread;
#elif defined(STARCOUNTER_CORE_HAS_WINTHREADS)
typedef windows_thread thread;
#endif

} // namespace core
} // namespace starcounter

#endif // STARCOUNTER_CORE_THREAD_THREAD_HPP
