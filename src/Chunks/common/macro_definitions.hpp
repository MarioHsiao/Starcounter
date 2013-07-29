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

/// Defining IPC_MONITOR_USE_STARCOUNTER_CORE_THREADS means that the IPC monitor will use
/// starcounter::core::thread instead of boost::thread.
#define IPC_MONITOR_USE_STARCOUNTER_CORE_THREADS

/// Defining IPC_SEND_TO_SERVER_AND_WAIT_RESPONSE_TURN_OFF_NOTIFICATIONS
/// means this_client_interface.set_notify_flag(false); will be called in 13 places
/// in shared_interface::send_to_server_and_wait_response().
/// If this introduces a bug, disale/comment this macro:
#define IPC_SEND_TO_SERVER_AND_WAIT_RESPONSE_TURN_OFF_NOTIFICATIONS

///********************************************************************************************
/// Define IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL
/// to synchronize access to the shared_chunk_pool using a spinlock and Windows Events.
#define IPC_REPLACE_IPC_SYNC_IN_THE_SHARED_CHUNK_POOL

/// Define IPC_HANDLE_CHANNEL_IN_BUFFER_FULL
/// to activate the use of one overflow queue per channel, based on linked lists.
#define IPC_HANDLE_CHANNEL_IN_BUFFER_FULL

/// Define IPC_HANDLE_CHANNEL_OUT_BUFFER_FULL
/// to activate the use of one overflow queue per channel, based on linked lists.
#define IPC_HANDLE_CHANNEL_OUT_BUFFER_FULL

/// Define CONNECTIVITY_MONITOR_SHOW_ACTIVITY in order for the connectivity monitor
/// to show the activity in shared memory between database(s) and client(s).
/// It shows resource usage and activity in channels. Only used for debug, it shall
/// not be defined when pushing code.
//#define IPC_MONITOR_SHOW_ACTIVITY

/// Debug switch to see atomic_buffer performance counters used in
/// starcounter::core::channel. NOTE: This macro must be commented out before pushing
/// code so that performance counters are not used because it degrades performance a bit.
//#define STARCOUNTER_CORE_ATOMIC_BUFFER_PERFORMANCE_COUNTERS

/// Define IPC_MONITOR_INTERFACE_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC to use spinlocks to
/// synchronize access to the IPC monitors monitor_interface shared memory segment.
///NOT STARTED YET: #define IPC_MONITOR_INTERFACE_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC
//#if defined (IPC_MONITOR_INTERFACE_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
//#else // !defined (IPC_MONITOR_INTERFACE_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
//#endif // defined (IPC_MONITOR_INTERFACE_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)

///********************************************************************************************
/// Define IPC_CLIENT_NUMBER_POOL_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC
/// to use a robust spinlock and windows events to synchronize access to
/// client_number_pool.
#define IPC_CLIENT_NUMBER_POOL_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC

#if defined (IPC_CLIENT_NUMBER_POOL_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
#else // !defined (IPC_CLIENT_NUMBER_POOL_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
#endif // defined (IPC_CLIENT_NUMBER_POOL_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)

///********************************************************************************************
/// Define IPC_SCHEDULER_INTERFACE_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC
/// to use a robust spinlock and windows events to synchronize access to
/// scheduler_interface.

/// NOT OPTIONAL TO DISABLE THIS ANYMORE - TODO.
#define IPC_SCHEDULER_INTERFACE_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC

///********************************************************************************************
/// Defining IPC_MONITOR_RELEASES_CHUNKS_DURING_CLEAN_UP
/// means the IPC monitor do the release of chunks instead of the schedulers.
#define IPC_MONITOR_RELEASES_CHUNKS_DURING_CLEAN_UP

#if defined (IPC_MONITOR_RELEASES_CHUNKS_DURING_CLEAN_UP)
#else // !defined (IPC_MONITOR_RELEASES_CHUNKS_DURING_CLEAN_UP)
#endif // defined (IPC_MONITOR_RELEASES_CHUNKS_DURING_CLEAN_UP)

///********************************************************************************************

/// Comment macro IPC_OWNER_ID_IS_32_BIT to go back to the old 64-bit owner_id type.
#define IPC_OWNER_ID_IS_32_BIT

/// Define INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC to use Windows Event
/// synchronization in interprocess communication. Comment this out in order to use
/// Boost.Interprocess condition synchronization.
/// The plan is to test the implementation with Windows Events and when stable, remove
/// code using Boost.Interprocess condition variable and remove wrapping of the code
/// with this macro. Using Windows Events is not yet fully implemented.
/// While experimenting with this, don't define it when pushing code.
#define INTERPROCESS_COMMUNICATION_USE_WINDOWS_EVENTS_TO_SYNC

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
