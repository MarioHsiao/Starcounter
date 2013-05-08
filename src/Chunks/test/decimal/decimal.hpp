//
// decimal.hpp
//
// Copyright © 2006-2013 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#ifndef STARCOUNTER_NUMERICS_DECIMAL_HPP
#define STARCOUNTER_NUMERICS_DECIMAL_HPP

namespace starcounter {
namespace numerics {

// starcounter::core::numerics::decimal in raw format:
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

// starcounter::core::numerics::decimal in unpacked/encoded format:
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

// starcounter::core::numerics::decimal in packed/compressed format:
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

class decimal {
public:
	typedef uint64_t value_type;

private:
	value_type value_;
};
#if 0
If the value of a CLR decimal doesn't fall within allowed range when converting to decimal then the conversion is to fail (exception thrown).

Basically you can not store any value CLR decimal in the database in a decimal field.

int64_t int2dec(int64_t v) { return v * 1E6; }

It is only stored in multiple parts in the packed and encoded format. Not in the raw format. Then it value is simply v/1000000.

The too one is the encoded format. The bottom one you invented.

The encoded format is an intermediate format to make comparison between packed decimals faster.

(Encoded = unpacked in spec)

#endif

// http://msdn.microsoft.com/en-us/library/system.decimal.aspx

} // namespace numerics
} // namespace starcounter

#include "impl/decimal.hpp"

#endif // STARCOUNTER_NUMERICS_DECIMAL_HPP
