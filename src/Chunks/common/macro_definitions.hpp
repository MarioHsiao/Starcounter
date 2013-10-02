//
// macro_definitions.hpp
//
// Copyright © 2006-2013 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#ifndef STARCOUNTER_CORE_MACRO_DEFINITIONS_HPP
#define STARCOUNTER_CORE_MACRO_DEFINITIONS_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

/// Define the macro IPC_VERSION_2_0 in order to get the changes:
/// -------------------------------------------------------------
/// - No cleanup of resources (chunks, channels and client_interfaces), in the database's
///   shared memory segment.
/// - Grouped channels: One channel per worker-scheduler pair, and each worker have its own
///   set of channels in the range 0 to N -1, where N is number of schedulers.
///   A worker that want to communicate with scheduler S do so on channel(S).
/// - Allocation of channels is not done. Once the Network Gateway, or IPC test, connects
///   to the shared memory, each worker can start communicating with the schedulers.
/// - Schedulers scan all channels. No need to tell the schedulers to start scanning.
/// - The IPC Monitor is obsolete. I don't know if it will be running, but the Network Gateway
///   and the IPC test will not register. They will use owner_id 2 for now.
/// - No cleanup will be done by the IPC monitor (since the Network Gateway and IPC test
///   will not register, no cleanup is triggered.)
/// - Databases will not be notified by the IPC monitor that the Network Gateway or
///   IPC test process terminated. But the server kills all databases anyway if the
///   Network Gateway (and that should apply to the IPC test also) terminates.
///
/// If IPC_VERSION_2_0 is not defined, then it is IPC version 1.0

//#define IPC_VERSION_2_0

#if defined (IPC_VERSION_2_0)
#endif // defined (IPC_VERSION_2_0)

#if defined (IPC_VERSION_2_0)
#else // !defined (IPC_VERSION_2_0)
#endif // defined (IPC_VERSION_2_0)

///********************************************************************************************
/// Define CONNECTIVITY_MONITOR_SHOW_ACTIVITY in order for the connectivity monitor
/// to show the activity in shared memory between database(s) and client(s).
/// It shows resource usage and activity in channels. Only used for debug, it shall
/// not be defined when pushing code.
//#define IPC_MONITOR_SHOW_ACTIVITY

/// Define IPC_MONITOR_INTERFACE_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC to use spinlocks to
/// synchronize access to the IPC monitors monitor_interface shared memory segment.
///NOT STARTED YET: #define IPC_MONITOR_INTERFACE_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC
//#if defined (IPC_MONITOR_INTERFACE_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
//#else // !defined (IPC_MONITOR_INTERFACE_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
//#endif // defined (IPC_MONITOR_INTERFACE_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)

///********************************************************************************************
/// Comment macro IPC_OWNER_ID_IS_32_BIT to go back to the old 64-bit owner_id type.
#define IPC_OWNER_ID_IS_32_BIT

// Prefix for database names.
#define DATABASE_NAME_PREFIX "starcounter"
#define W_DATABASE_NAME_PREFIX L"starcounter"

// Suffix for monitor_interfaces.
#define MONITOR_INTERFACE_SUFFIX "starcounter_monitor_interface"

// Suffix for monitor_interfaces.
#define IPC_MONITOR_CLEANUP_EVENT "ipc_monitor_cleanup_event"
#define W_IPC_MONITOR_CLEANUP_EVENT L"ipc_monitor_cleanup_event"

// Suffix for monitor_interfaces.
#define ACTIVE_DATABASES_UPDATED_EVENT "active_databases_updated_event"
#define W_ACTIVE_DATABASES_UPDATED_EVENT L"active_databases_updated_event"

// Default monitor directory name
#define DEFAULT_MONITOR_DIR_NAME "ipc_monitor"
#define W_DEFAULT_MONITOR_DIR_NAME L"ipc_monitor"

// Default monitor active databases file name
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
#endif // defined(__INTEL_COMPILER) || defined(_MSC_VER)

#if defined(__GNUC__)
# define ALWAYS_INLINE inline __attribute__((always_inline))
#elif defined(_MSC_VER)
# define ALWAYS_INLINE __forceinline
#else
# define ALWAYS_INLINE inline
#endif

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

#if defined(_MSC_VER)
# define DLL_IMPORT __declspec(dllimport)
#endif // defined(_MSC_VER)

#define USE_POPCNT

// Restrict is a compiler directive that helps avoid load-hit-store stalls.
//
// One pointer is said to alias another pointer when both refer to the same
// location or object.
//
// Restrict on a pointer promises the compiler that it has no aliases: nothing
// else in the function points to that same data. Thus the compiler knows that
// if it writes data to a pointer, it doesn’t need to read it back into a
// register later on because nothing else could have written to that address.
//
// Without restrict, the compiler is forced to read data from every pointer
// every time it is used, because another pointer may have aliased x.
//
// It bears repeating that restrict is a promise you make to your compiler:
// "I promise that the pointer declared along with the restrict qualifier is not
// aliased. I certify that writes through this pointer will not affect the
// values read through any other pointer available in the same context which is
// also declared as restricted."
//
// If you break your promise, you can get incorrect results.
//
// A restrict-qualified pointer can grant access to a non-restrict pointer.
// However, two restrict-qualified pointers are trivially non-aliasing.
//
// Restrict enables SIMD optimizations.

#if defined(__clang__)
// Try #include <config.h> if it doesn't work.
# define RESTRICT __restrict
#elif defined(__GNUC__)
# define RESTRICT __restrict__
#elif defined(_MSC_VER)
// Compile with: /LD
# define RESTRICT __restrict
#else // The compiler does not support RESTRICT.
# define RESTRICT
#endif // defined(__clang__)

// The UNREACHABLE macro tell the optimizer that the default cannot be reached.
// The optimizer can take advantage of this to produce better code.
#if defined(__clang__)
# if __has_builtin(__builtin_unreachable)
/// TODO: Add -Wunreachable-code to Clang Makefile.
#  define UNREACHABLE __builtin_unreachable()
# else
#  error __builtin_unreachable() is not available.
# endif // __has_builtin(__builtin_unreachable)
#elif defined(_MSC_VER)
# if (_MSC_VER >= 1500)
#  define UNREACHABLE __assume(0)
# else // !(_MSC_VER >= 1500
#  error Compiler not supported.
# endif // (_MSC_VER >= 1500)
#else
# error Compiler not supported.
#endif // defined(__clang__)

#endif // STARCOUNTER_CORE_MACRO_DEFINITIONS_HPP
