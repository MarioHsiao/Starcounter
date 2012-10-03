//
// macro_definitions.hpp
//
// Copyright © 2006-2012 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#ifndef STARCOUNTER_CORE_MACRO_DEFINITIONS_HPP
#define STARCOUNTER_CORE_MACRO_DEFINITIONS_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

/// Switch on current work in progress.
#define SPIN

// The SCHEDULERS macro is a bit malplaced but it works for now. It is only used
// in the test server and test client.
#define SCHEDULERS 2

// Prefix for database names.
#define DATABASE_NAME_PREFIX "starcounter"
#define W_DATABASE_NAME_PREFIX L"starcounter"

// Suffix for monitor_interfaces.
#define MONITOR_INTERFACE_SUFFIX "starcounter_monitor_interface"

// Default monitor log file name
#define DEFAULT_MONITOR_LOG_FILE_NAME "monitor.log"
#define W_DEFAULT_MONITOR_LOG_FILE_NAME L"monitor.log"

// Default monitor active database file name
#define DEFAULT_MONITOR_ACTIVE_DATABASES_FILE_NAME "active_databases"
#define W_DEFAULT_MONITOR_ACTIVE_DATABASES_FILE_NAME L"active_databases"

// What a slash look like on this operating system.
#if defined(UNIX)
# define SLASH '/'
# define W_SLASH L'/'
#elif defined(_WIN32) || defined(_WIN64)
# define SLASH '\\'
# define W_SLASH L'\\'
#else
# error Unsupported architecture.
#endif // defined(UNIX)

// Processor cache, vector register size, and vm page constants.
#define CACHE_LINE_SIZE 64
#define VM_PAGE_SIZE 4096
#define XMM_SIZE 16
#define YMM_SIZE 32

#if defined(__INTEL_COMPILER) || defined(_MSC_VER)
# define CACHE_LINE_ALIGN __declspec(align(CACHE_LINE_SIZE))
# define VM_PAGE_ALIGN __declspec(align(VM_PAGE_SIZE))
# define XMM_ALIGN __declspec(align(XMM_SIZE))
# define YMM_ALIGN __declspec(align(YMM_SIZE))
#elif defined(__GNUC__)
# define CACHE_LINE_ALIGN __attribute__ ((aligned (CACHE_LINE_SIZE)))
# define VM_PAGE_ALIGN __attribute__ ((aligned (VM_PAGE_SIZE)))
# define XMM_ALIGN __attribute__ ((aligned (XMM_SIZE)))
# define YMM_ALIGN __attribute__ ((aligned (YMM_SIZE)))
#endif // defined(__INTEL_COMPILER)

#if defined(__GNUC__)
# define FORCE_INLINE inline __attribute__((always_inline))
#elif defined(_MSC_VER)
# define FORCE_INLINE __forceinline
#else
# define FORCE_INLINE inline
#endif

#if defined(__ARMCC) // __arm
# define PACKED __packed
#else ! defined(__ARMCC)
# define PACKED
#endif // defined(__ARMCC)

// CXXFLAGS += -D__STDC_CONSTANT_MACROS
#ifndef INT64_C
#define INT64_C(c) (c##LL)
#define UINT64_C(c) (c##ULL)
#endif // INT64_C

#endif // STARCOUNTER_CORE_MACRO_DEFINITIONS_HPP
