//
// macro_definitions.hpp
//

#ifndef MACRO_DEFINITIONS_HPP
#define MACRO_DEFINITIONS_HPP

#define STARCOUNTER_LITTLE_ENDIAN_ORDER

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

#endif // MACRO_DEFINITIONS_HPP
