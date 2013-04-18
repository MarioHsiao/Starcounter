//
// bit_operations.hpp
//
// Copyright © 2006-2013 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#ifndef STARCOUNTER_CORE_BIT_OPERATIONS_HPP
#define STARCOUNTER_CORE_BIT_OPERATIONS_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#include <cstdint>

#if defined(_MSC_VER)
# define WIN32_LEAN_AND_MEAN
# include <intrin.h>
# undef WIN32_LEAN_AND_MEAN
#endif // defined(_MSC_VER)

#if defined(USE_POPCNT) && defined(_MSC_VER) && defined(__INTEL_COMPILER)
# include <nmmintrin.h> // Intel header for _mm_popcnt_u64() intrinsic
#endif // defined(USE_POPCNT) && defined(_MSC_VER) && defined(__INTEL_COMPILER)

#include "macro_definitions.hpp"

namespace starcounter {
namespace core {

///=============================================================================
///	Operating System:			Unix	Unix	Windows	Windows
///	Vector instructions:		SSE2	SSE4.2	SSE2	SSE4.2
/// ----------------------------------------------------------------------------
/// bit_scan_forward(uint32_t)	YES		YES		YES		YES
/// bit_scan_forward(uint64_t)	YES		YES		YES		YES
/// bit_scan_reverse(uint32_t)	YES		YES		YES		YES
/// bit_scan_reverse(uint64_t)	YES		YES		YES		YES
/// population_count(uint32_t)	YES		YES		YES		YES
/// population_count(uint64_t)	YES		YES		YES		YES
///
/// NOTE: Unix = Linux or OS X. No other Unix or Unix like OS are supported.
/// SSE4.2 allows using the POPCNT instruction, and will be used if the macro
/// USE_POPCNT is defined.
///=============================================================================

#if defined(__INTEL_COMPILER) || defined(__GNUC__)

/// bit_scan_forward(uint32_t) finds the least significant (LSB) nonzero bit in
/// the 32-bit nonzero word parameter.
/// Uses inline assembly (for 32-bit architectures).
/**
 * @param word Nonzero 32-bit word to scan.
 * @precondition word != 0.
 * @return index (0..31) of least significant one bit.
 */
static FORCE_INLINE std::size_t bit_scan_forward(uint32_t word) {
	uint32_t index;
	assert(word != 0);
	__asm__("bsf %1, %0": "=r" (index): "rm" (word));
//	asm ("bsf %1, %0\n": "=&r" (index): "r" (word));
	return static_cast<std::size_t>(index);
}

/// bit_scan_reverse(uint32_t) finds the most significant (MSB) nonzero bit in
/// the 32-bit nonzero word.
/// Uses inline assembly (for 32-bit architectures).
/**
 * @param word Nonzero 32-bit word to scan.
 * @precondition word != 0.
 * @return index (0..31) of most significant one bit.
 */
static FORCE_INLINE std::size_t bit_scan_reverse(uint32_t word) {
	uint32_t index;
	assert(word != 0);
	__asm__("bsr %1, %0": "=r" (index): "rm" (word));
//	asm ("bsr %1, %0\n": "=&r" (index): "r" (word));
	return static_cast<std::size_t>(index);
}

#if defined(_M_X64) || (_M_AMD64) // LLP64 machine

/// bit_scan_forward(uint64_t) finds the least significant (LSB) nonzero bit in
/// the 64-bit nonzero word parameter.
/// Uses inline assembly (for 64-bit architectures).
/**
 * @param word Nonzero 64-bit word to scan.
 * @precondition word != 0.
 * @return index (0..63) of least significant one bit.
 */
static FORCE_INLINE std::size_t bit_scan_forward(uint64_t word) {
	uint64_t index;
	assert(word != 0);
	__asm__("bsfq %1, %0": "=r" (index): "rm" (word));
//	asm ("bsfq %1, %0\n": "=&r" (index): "r" (word));
	return static_cast<std::size_t>(index);
}

/// bit_scan_reverse(uint64_t) finds the most significant (MSB) nonzero bit in
/// the 64-bit nonzero word.
/// Uses inline assembly (for 64-bit architectures).
/**
 * @param word Nonzero 64-bit word to scan.
 * @precondition word != 0.
 * @return index (0..63) of most significant one bit.
 */
static FORCE_INLINE std::size_t bit_scan_reverse(uint64_t word) {
	uint64_t index;
	assert(word != 0);
	__asm__("bsrq %1, %0": "=r" (index): "rm" (word));
//	asm ("bsrq %1, %0\n": "=&r" (index): "r" (word));
	return static_cast<std::size_t>(index);
}

#endif // defined(_M_X64) || (_M_AMD64) // LLP64 machine

#elif defined(_MSC_VER)

/// bit_scan_forward(uint32_t) finds the least significant (LSB) nonzero bit in
/// the 32-bit nonzero word.
static FORCE_INLINE std::size_t bit_scan_forward(uint32_t word) {
	unsigned long int index;
	assert(word != 0);
	(void) _BitScanForward(&index, word);
	return static_cast<std::size_t>(index);
}

/// bit_scan_reverse(uint32_t) finds the most significant (MSB) nonzero bit in
/// the 32-bit nonzero word.
static FORCE_INLINE std::size_t bit_scan_reverse(uint32_t word) {
	unsigned long int index;
	assert(word != 0);
	(void) _BitScanReverse(&index, word);
	return static_cast<std::size_t>(index);
}

#if defined(_M_X64) || (_M_AMD64) // LLP64 machine

/// bit_scan_forward(uint64_t) finds the least significant (LSB) nonzero bit in
/// the 64-bit nonzero word.
static FORCE_INLINE std::size_t bit_scan_forward(uint64_t word) {
	unsigned long int index;
	assert(word != 0);
	(void) _BitScanForward64(&index, word);
	return static_cast<std::size_t>(index);
}

/// bit_scan_reverse(uint64_t) finds the most significant (MSB) nonzero bit in
/// the 64-bit nonzero word.
static FORCE_INLINE std::size_t bit_scan_reverse(uint64_t word) {
	unsigned long int index;
	assert(word != 0);
	(void) _BitScanReverse64(&index, word);
	return static_cast<std::size_t>(index);
}

#endif // defined(_M_X64) || (_M_AMD64) // LLP64 machine
#endif // defined(__INTEL_COMPILER) || defined(__GNUC__)

///=============================================================================
/// Population count counts the number of bits set to 1 in the 64-bit argument
/// and return the count.
///=============================================================================

// population_count() will use the POPCNT (SSE4.2) instruction if the macro
// USE_POPCNT is defined (in the makefile.) If USE_POPCNT is not defined,
// population_count() will use instructions supported by older hardware.

#if defined (USE_POPCNT)
static FORCE_INLINE int population_count(uint32_t w) {
# if defined(_MSC_VER) && defined(__INTEL_COMPILER)
	return _mm_popcnt_u32(w);
# elif defined(_MSC_VER)
	return (int) __popcnt(w);
# else // Clang and GCC
	__asm__("popcnt %1, %0" : "=r" (b) : "r" (w));
	return w;
# endif // defined(_MSC_VER) && defined(__INTEL_COMPILER)
}

static FORCE_INLINE int population_count(uint64_t w) {
# if defined(_MSC_VER) && defined(__INTEL_COMPILER)
	return _mm_popcnt_u64(w);
# elif defined(_MSC_VER)
	return (int) __popcnt64(w);
# else // Clang and GCC
	__asm__("popcnt %1, %0" : "=r" (b) : "r" (w));
	return w;
# endif // defined(_MSC_VER) && defined(__INTEL_COMPILER)
}

#else // !defined (USE_POPCNT)
static FORCE_INLINE int population_count(uint32_t w) {
	w -= (w >> 1) & 0x55555555;
	w = (w & 0x33333333) +((w >> 2) & 0x33333333);
	w = (w +(w >> 4)) & 0x0F0F0F0F;
	return (w * 0x01010101) >> 24;
}

static FORCE_INLINE int population_count(uint64_t w) {
	w -= (w >> 1) & 0x5555555555555555ULL;
	w = ((w >> 2) & 0x3333333333333333ULL) +(w & 0x3333333333333333ULL);
	w = ((w >> 4) +w) & 0x0F0F0F0F0F0F0F0FULL;
	return (w * 0x0101010101010101ULL) >> 56;
}

#endif // defined (USE_POPCNT)

} // namespace core
} // namespace starcounter

#endif // STARCOUNTER_CORE_BIT_OPERATIONS_HPP
