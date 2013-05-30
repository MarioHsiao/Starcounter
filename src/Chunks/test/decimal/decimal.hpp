//
// decimal.hpp
//
// Copyright © 2006-2013 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#ifndef STARCOUNTER_NUMERICS_DECIMAL_HPP
#define STARCOUNTER_NUMERICS_DECIMAL_HPP

#include <cstdint>
#include "uint128_t.hpp"

namespace starcounter {
namespace numerics {
namespace clr {

// starcounter::numerics::decimal in raw format:
//
//  6 6 6 6 5 5 5 5 5 5 5 5 5 5 4 4 4 4 4 4 4 4 4 4 3 3 3 3 3 3 3 3
// .3 2 1 0 9 8 7 6.5 4 3 2 1 0 9 8.7 6 5 4 3 2 1 0.9 8 7 6 5 4 3 2.
// +-+-------------------------------------------------------------+
// |S|                                                      INTEGER:
// +-+-------------------------------------------------------------+
// Bit 63 = sign
//
//  3 3 2 2 2 2 2 2 2 2 2 2 1 1 1 1 1 1 1 1 1 1
// .1 0 9 8 7 6 5 4.3 2 1 0 9 8 7 6.5 4 3 2 1 0 9 8.7 6 5 4 3 2 1 0.
// +---------------------------------------------------------------+
// :                                                        INTEGER|
// +---------------------------------------------------------------+
// Bit 62:0 INTEGER

// starcounter::numerics::decimal in unpacked/encoded format:
//
//  6 6 6 6 5 5 5 5 5 5 5 5 5 5 4 4 4 4 4 4 4 4 4 4 3 3 3 3 3 3 3 3
// .3 2 1 0 9 8 7 6.5 4 3 2 1 0 9 8.7 6 5 4 3 2 1 0.9 8 7 6 5 4 3 2.
// +-+-------------------------------------------------------------+
// |S|                                                      INTEGER:
// +-+-------------------------------------------------------------+
// Bit 63 = sign
//
//  3 3 2 2 2 2 2 2 2 2 2 2 1 1 1 1 1 1 1 1 1 1
// .1 0 9 8 7 6 5 4.3 2 1 0 9 8 7 6.5 4 3 2 1 0 9 8.7 6 5 4 3 2 1 0.
// +---------------------+-------------+---------------------------+
// :              INTEGER|   MS DECIMAL|                 LS DECIMAL|
// +---------------------+-------------+---------------------------+
// Bit 62:21 INTEGER
// Bit 20:14 MS DECIMAL (the two most significant digits)
// Bit 13:0 LS DECIMAL (the four least significant digits)
//
// NOTE: This is an intermediate state between the raw format and
// the packed/compressed, for the purpose of comparing quickly.

// starcounter::numerics::decimal in packed/compressed format:
//
//  6 6 6 6 5 5 5 5 5 5 5 5 5 5 4 4 4 4 4 4 4 4 4 4 3 3 3 3 3 3 3 3
// .3 2 1 0 9 8 7 6.5 4 3 2 1 0 9 8.7 6 5 4 3 2 1 0.9 8 7 6 5 4 3 2.
// +---------------------------------------------------------------+
// |                                                        INTEGER:
// +---------------------------------------------------------------+
// NOTE: Managed code will never see the packed/compressed format.
//
//  3 3 2 2 2 2 2 2 2 2 2 2 1 1 1 1 1 1 1 1 1 1
// .1 0 9 8 7 6 5 4.3 2 1 0 9 8 7 6.5 4 3 2 1 0 9 8.7 6 5 4 3 2 1 0.
// +---------------+-------------+---------------------------+---+-+
// :        INTEGER|   MS DECIMAL|                 LS DECIMAL|SCL|S|
// +---------------+-------------+---------------------------+---+-+
// Bit 63:24 INTEGER
// Bit 23:17 MS DECIMAL (the two most significant digits)
// Bit 13:0 LS DECIMAL (the four least significant digits)
// Bit 2:1 (SCL) = SCALE
// Bit 0 (S) = SIGN


// The binary representation of a CLR Decimal number consists of a 1-bit sign, a
// 96-bit integer number, and a scaling factor used to divide the integer number
// and specify what portion of it is a decimal fraction. The scaling factor is
// implicitly the number 10, raised to an exponent ranging from 0 to 28.

// The return value is a four-element array of 32-bit signed integers.

// The first, second, and third elements of the returned array contain the low,
// middle, and high 32 bits of the 96-bit integer number.

// The fourth element of the returned array contains the scale factor and sign. It
// consists of the following parts:

// Bits 0 to 15, the lower word, are unused and must be zero.

// Bits 16 to 23 must contain an exponent between 0 and 28, which indicates the
// power of 10 to divide the integer number.

// Bits 24 to 30 are unused and must be zero.

// Bit 31 contains the sign: 0 mean positive, and 1 means negative.

// Note that the bit representation differentiates between negative and positive
// zero. These values are treated as being equal in all operations.

//  1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1
//  2 2 2 2 2 2 2 2 1 1 1 1 1 1 1 1 1 1 0 0 0 0 0 0 0 0 0 0 9 9 9 9
//  7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6
// +-+-------------+---------------+-------------------------------+
// |S|0 0 0 0 0 0 0|       EXPONENT|0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0|
// +-+-------------+---------------+-------------------------------+
// S = sign
// EXPONENT is in the range 0 to 28 (seems like only bit 116:112 is used of the dedicated 119:112 field to the exponent.)
// The scaling factor = 10^EXPONENT, which the integer part is divided by.

//  9 9 9 9 9 9 8 8 8 8 8 8 8 8 8 8 7 7 7 7 7 7 7 7 7 7 6 6 6 6 6 6
//  5 4 3 2 1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4
// +---------------------------------------------------------------+
// |                                            INTEGER BITS 95:64 |
// +---------------------------------------------------------------+

//  6 6 6 6 5 5 5 5 5 5 5 5 5 5 4 4 4 4 4 4 4 4 4 4 3 3 3 3 3 3 3 3
//  3 2 1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2
// +---------------------------------------------------------------+
// |                                            INTEGER BITS 63:32 |
// +---------------------------------------------------------------+

//  3 3 2 2 2 2 2 2 2 2 2 2 1 1 1 1 1 1 1 1 1 1
//  1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2 1 0
// +---------------------------------------------------------------+
// |                                             INTEGER BITS 31:0 |
// +---------------------------------------------------------------+

class decimal : public uint128_t {
public:
	typedef int32_t low_type;
	typedef int32_t middle_type;
	typedef int32_t high_type;
	typedef int32_t scale_sign_type;

	/// decimal constructor (int32_t, int32_t, int32_t, bool, uint8_t)
	/**
	 * @param low Bit 31:0 of a 96-bit integer.
	 * @param middle Bit 63:32 of a 96-bit integer.
	 * @param high Bit 95:64 of a 96-bit integer.
	 * @param is_negative Is true to indicate a negative number,
	 *		false to indicate a positive number.
	 * @param scale A power of 10 ranging from 0 to 28.
	 */
	decimal(int32_t low = 0, int32_t middle = 0, int32_t high = 0,
	bool is_negative = false, uint8_t scale = 0);
	
	void print();

private:
	low_type low_;
	middle_type middle_;
	high_type high_;
	scale_sign_type scale_sign_;
};

} // namespace clr
} // namespace numerics
} // namespace starcounter

#include "impl/decimal.hpp"

#endif // STARCOUNTER_NUMERICS_DECIMAL_HPP
