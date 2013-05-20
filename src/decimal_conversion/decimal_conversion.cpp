//
// decimal_conversion.cpp
//
// Copyright © 2006-2013 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#include <cstdint>
#include "uint128_t.hpp"
#include <sccoredb.h>
#include <sccoredbg.h>
#include <record\impl\dec_decode.hpp>
#include <record\impl\dec_encode.hpp>

typedef __dataValueFlags data_value_flags_type;

// These kernel functions have C++ linkage.
extern int64_t decode_dec(int64_t encoded_value);
extern int64_t encode_dec(int64_t value);

extern "C" { // These kernel functions have C linkage.

data_value_flags_type sccoredb_get_decimal(uint64_t record_id,
uint64_t record_addr, uint32_t column_index, int64_t* pvalue);

uint32_t sccoredb_put_decimal(uint64_t record_id, uint64_t record_addr,
uint32_t column_index, int64_t value);

}

namespace {

const int64_t x6_decimal_max = +4398046511103999999LL;
const int64_t x6_decimal_min = -4398046511103999999LL;

} // namespace

namespace {

// Exponents from 1e1 to 1e28 expressed as 128-bit constants.
const starcounter::numerics::uint128_t _1e1(0x0000000000000000ULL, 0x000000000000000AULL);
const starcounter::numerics::uint128_t _1e2(0x0000000000000000ULL, 0x0000000000000064ULL);
const starcounter::numerics::uint128_t _1e3(0x0000000000000000ULL, 0x00000000000003E8ULL);
const starcounter::numerics::uint128_t _1e4(0x0000000000000000ULL, 0x0000000000002710ULL);
const starcounter::numerics::uint128_t _1e5(0x0000000000000000ULL, 0x00000000000186A0ULL);
const starcounter::numerics::uint128_t _1e6(0x0000000000000000ULL, 0x00000000000F4240ULL);
const starcounter::numerics::uint128_t _1e7(0x0000000000000000ULL, 0x0000000000989680ULL);
const starcounter::numerics::uint128_t _1e8(0x0000000000000000ULL, 0x0000000005f5e100ULL);
const starcounter::numerics::uint128_t _1e9(0x0000000000000000ULL, 0x000000003B9ACA00ULL);
const starcounter::numerics::uint128_t _1e10(0x0000000000000000ULL, 0x00000002540BE400ULL);
const starcounter::numerics::uint128_t _1e11(0x0000000000000000ULL, 0x000000174876E800ULL);
const starcounter::numerics::uint128_t _1e12(0x0000000000000000ULL, 0x000000E8D4A51000ULL);
const starcounter::numerics::uint128_t _1e13(0x0000000000000000ULL, 0x000009184E72A000ULL);
const starcounter::numerics::uint128_t _1e14(0x0000000000000000ULL, 0x00005AF3107A4000ULL);
const starcounter::numerics::uint128_t _1e15(0x0000000000000000ULL, 0x00038D7EA4C68000ULL);
const starcounter::numerics::uint128_t _1e16(0x0000000000000000ULL, 0x002386F26FC10000ULL);
const starcounter::numerics::uint128_t _1e17(0x0000000000000000ULL, 0x016345785D8A0000ULL);
const starcounter::numerics::uint128_t _1e18(0x0000000000000000ULL, 0x0DE0B6B3A7640000ULL);
const starcounter::numerics::uint128_t _1e19(0x0000000000000000ULL, 0x8AC7230489E80000ULL);
const starcounter::numerics::uint128_t _1e20(0x0000000000000005ULL, 0x6BC75E2D63100000ULL);
const starcounter::numerics::uint128_t _1e21(0x0000000000000036ULL, 0x35C9ADC5DEA00000ULL);
const starcounter::numerics::uint128_t _1e22(0x000000000000021EULL, 0x19E0C9BAB2400000ULL);
const starcounter::numerics::uint128_t _1e23(0x000000000000152DULL, 0x02C7E14AF6800000ULL);
const starcounter::numerics::uint128_t _1e24(0x000000000000D3C2ULL, 0x1BCECCEDA1000000ULL);
const starcounter::numerics::uint128_t _1e25(0x0000000000084595ULL, 0x161401484A000000ULL);
const starcounter::numerics::uint128_t _1e26(0x000000000052B7D2ULL, 0xDCC80CD2E4000000ULL);
const starcounter::numerics::uint128_t _1e27(0x00000000033B2E3CULL, 0x9FD0803CE8000000ULL);
const starcounter::numerics::uint128_t _1e28(0x00000000204FCE5EULL, 0x3E25026110000000ULL);

} // namespace

/// Reading a decimal requires type conversion from Starcounter X6 decimal in
/// unpacked/encoded format to CLR decimal.
/**
 * @param record_id The ID of the record.
 * @param record_addr The address of the record.
 * @param column_index The column index.
 * @param decimal_part_ptr A pointer to the first element of int32_t[4],
 *		where the CLR Decimal value will be composed.
 * @return Data value flags.
 */
data_value_flags_type convert_x6_decimal_to_clr_decimal(uint64_t record_id,
uint64_t record_addr, int32_t column_index, int32_t* decimal_part_ptr) {
	int64_t encoded_value;

	data_value_flags_type flags = sccoredb_get_decimal
	(record_id, record_addr, column_index, &encoded_value);

	int64_t raw_value = decode_dec(encoded_value);
	*((uint64_t*) decimal_part_ptr) = raw_value & 0x7FFFFFFFFFFFFFFFULL;
	*((uint64_t*) decimal_part_ptr +1) = raw_value & 0x8000000000000000ULL
	| 0x0006000000000000ULL;
	
	return flags;
}

/// Writing a decimal requires type conversion from CLR decimal to Starcounter
/// X6 decimal in unpacked/encoded format.
/**
 * @param record_id The ID of the record.
 * @param record_addr The address of the record.
 * @param column_index The column index.
 * @param low Bit 31:0 of the 96-bit value.
 * @param middle Bit 63:32 of the 96-bit value.
 * @param high Bit 95:64 of the 96-bit value.
 * @param scale_sign Contains the scale (exponent) and the sign flag.
 * @return An error code.
 */
uint32_t convert_clr_decimal_to_x6_decimal(uint64_t record_id, uint64_t record_addr, int32_t column_index,
int32_t low, int32_t middle, int32_t high, int32_t scale_sign) {
	using namespace starcounter::numerics;

	// Constructing a decimal as a 128-bit value with the integer part of the CLR Decimal, bit 95:0.
	// The value is treated as positive, so testing that it is not > x6_decimal_max is the same as
	// testing that the value is not < x6_decimal_min when the CLR Decimal holds a negative value.
	uint128_t decimal(0, high, middle, low);
	bool range_error;
	
	// The exponent (scale) value is 0 to 28. Change to 6 decimals if not already 6 decimals.
	switch ((scale_sign >> 16) & 255) {
	case 0: goto multiply_by_1e6;
	case 1: goto multiply_by_1e5;
	case 2: goto multiply_by_1e4;
	case 3: goto multiply_by_1e3;
	case 4: goto multiply_by_1e2;
	case 5: goto multiply_by_1e1;
	case 6: goto already_6_decimals;
	case 7: goto divide_by_1e1;
	case 8: goto divide_by_1e2;
	case 9: goto divide_by_1e3;
	case 10: goto divide_by_1e4;
	case 11: goto divide_by_1e5;
	case 12: goto divide_by_1e6;
	case 13: goto divide_by_1e7;
	case 14: goto divide_by_1e8;
	case 15: goto divide_by_1e9;
	case 16: goto divide_by_1e10;
	case 17: goto divide_by_1e11;
	case 18: goto divide_by_1e12;
	case 19: goto divide_by_1e13;
	case 20: goto divide_by_1e14;
	case 21: goto divide_by_1e15;
	case 22: goto divide_by_1e16;
	case 23: goto divide_by_1e17;
	case 24: goto divide_by_1e18;
	case 25: goto divide_by_1e19;
	case 26: goto divide_by_1e20;
	case 27: goto divide_by_1e21;
	case 28: goto divide_by_1e22;
	default: UNREACHABLE;
	}
multiply_by_1e6:
	decimal *= _1e6;
	range_error = decimal > x6_decimal_max;
	goto write_decimal;
multiply_by_1e5:
	decimal *= _1e5;
	range_error = decimal > x6_decimal_max;
	goto write_decimal;
multiply_by_1e4:
	decimal *= _1e4;
	range_error = decimal > x6_decimal_max;
	goto write_decimal;
multiply_by_1e3:
	decimal *= _1e3;
	range_error = decimal > x6_decimal_max;
	goto write_decimal;
multiply_by_1e2:
	decimal *= _1e2;
	range_error = decimal > x6_decimal_max;
	goto write_decimal;
multiply_by_1e1:
	decimal *= _1e1;
	range_error = decimal > x6_decimal_max;
	goto write_decimal;
already_6_decimals:
	range_error = decimal > x6_decimal_max;
	goto write_decimal;
divide_by_1e1:
	range_error = decimal.divide_and_get_remainder(_1e1) != 0 | decimal > x6_decimal_max;
	goto write_decimal;
divide_by_1e2:
	range_error = decimal.divide_and_get_remainder(_1e2) != 0 | decimal > x6_decimal_max;
	goto write_decimal;
divide_by_1e3:
	range_error = decimal.divide_and_get_remainder(_1e3) != 0 | decimal > x6_decimal_max;
	goto write_decimal;
divide_by_1e4:
	range_error = decimal.divide_and_get_remainder(_1e4) != 0 | decimal > x6_decimal_max;
	goto write_decimal;
divide_by_1e5:
	range_error = decimal.divide_and_get_remainder(_1e5) != 0 | decimal > x6_decimal_max;
	goto write_decimal;
divide_by_1e6:
	range_error = decimal.divide_and_get_remainder(_1e6) != 0 | decimal > x6_decimal_max;
	goto write_decimal;
divide_by_1e7:
	range_error = decimal.divide_and_get_remainder(_1e7) != 0 | decimal > x6_decimal_max;
	goto write_decimal;
divide_by_1e8:
	range_error = decimal.divide_and_get_remainder(_1e8) != 0 | decimal > x6_decimal_max;
	goto write_decimal;
divide_by_1e9:
	range_error = decimal.divide_and_get_remainder(_1e9) != 0 | decimal > x6_decimal_max;
	goto write_decimal;
divide_by_1e10:
	range_error = decimal.divide_and_get_remainder(_1e10) != 0 | decimal > x6_decimal_max;
	goto write_decimal;
divide_by_1e11:
	range_error = decimal.divide_and_get_remainder(_1e11) != 0 | decimal > x6_decimal_max;
	goto write_decimal;
divide_by_1e12:
	range_error = decimal.divide_and_get_remainder(_1e12) != 0 | decimal > x6_decimal_max;
	goto write_decimal;
divide_by_1e13:
	range_error = decimal.divide_and_get_remainder(_1e13) != 0 | decimal > x6_decimal_max;
	goto write_decimal;
divide_by_1e14:
	range_error = decimal.divide_and_get_remainder(_1e14) != 0 | decimal > x6_decimal_max;
	goto write_decimal;
divide_by_1e15:
	range_error = decimal.divide_and_get_remainder(_1e15) != 0 | decimal > x6_decimal_max;
	goto write_decimal;
divide_by_1e16:
	range_error = decimal.divide_and_get_remainder(_1e16) != 0 | decimal > x6_decimal_max;
	goto write_decimal;
divide_by_1e17:
	range_error = decimal.divide_and_get_remainder(_1e17) != 0 | decimal > x6_decimal_max;
	goto write_decimal;
divide_by_1e18:
	range_error = decimal.divide_and_get_remainder(_1e18) != 0 | decimal > x6_decimal_max;
	goto write_decimal;
divide_by_1e19:
	range_error = decimal.divide_and_get_remainder(_1e19) != 0 | decimal > x6_decimal_max;
	goto write_decimal;
divide_by_1e20:
	range_error = decimal.divide_and_get_remainder(_1e20) != 0 | decimal > x6_decimal_max;
	goto write_decimal;
divide_by_1e21:
	range_error = decimal.divide_and_get_remainder(_1e21) != 0 | decimal > x6_decimal_max;
	goto write_decimal;
divide_by_1e22:
	range_error = decimal.divide_and_get_remainder(_1e22) != 0 | decimal > x6_decimal_max;
write_decimal:
	// TODO: Test if < x6_decimal_min

	if (range_error == false) {
		// The value fits in a X6 decimal.
		int64_t raw_value = decimal.low() | (uint64_t(scale_sign) >> 31) << 63;
		return sccoredb_put_decimal(record_id, record_addr, column_index, encode_dec(raw_value));
	}
	else {
		// The value doesn't fit in a X6 decimal.
		/// TODO: Error code for range error.
		return 999L; // SCERRCONVERTDECIMALRANGEERROR
	}
}

uint32_t clr_decimal_to_encoded_x6_decimal(int32_t* decimal_part_ptr, int64_t* encoded_x6_decimal_ptr) {
	using namespace starcounter::numerics;
	int32_t scale_sign = decimal_part_ptr[3];

	// Constructing a decimal as a 128-bit value with the integer part of the CLR Decimal, bit 95:0.
	// The value is treated as positive, so testing that it is not > x6_decimal_max is the same as
	// testing that the value is not < x6_decimal_min when the CLR Decimal holds a negative value.
	uint128_t decimal(scale_sign, decimal_part_ptr[2], decimal_part_ptr[1], decimal_part_ptr[0]);
	bool range_error;
	
	// The exponent (scale) value is 0 to 28. Change to 6 decimals if not already 6 decimals.
	switch ((scale_sign >> 16) & 255) {
	case 0: goto multiply_by_1e6;
	case 1: goto multiply_by_1e5;
	case 2: goto multiply_by_1e4;
	case 3: goto multiply_by_1e3;
	case 4: goto multiply_by_1e2;
	case 5: goto multiply_by_1e1;
	case 6: goto already_6_decimals;
	case 7: goto divide_by_1e1;
	case 8: goto divide_by_1e2;
	case 9: goto divide_by_1e3;
	case 10: goto divide_by_1e4;
	case 11: goto divide_by_1e5;
	case 12: goto divide_by_1e6;
	case 13: goto divide_by_1e7;
	case 14: goto divide_by_1e8;
	case 15: goto divide_by_1e9;
	case 16: goto divide_by_1e10;
	case 17: goto divide_by_1e11;
	case 18: goto divide_by_1e12;
	case 19: goto divide_by_1e13;
	case 20: goto divide_by_1e14;
	case 21: goto divide_by_1e15;
	case 22: goto divide_by_1e16;
	case 23: goto divide_by_1e17;
	case 24: goto divide_by_1e18;
	case 25: goto divide_by_1e19;
	case 26: goto divide_by_1e20;
	case 27: goto divide_by_1e21;
	case 28: goto divide_by_1e22;
	default: UNREACHABLE;
	}
multiply_by_1e6:
	decimal *= _1e6;
	range_error = decimal > x6_decimal_max;
	goto write_decimal;
multiply_by_1e5:
	decimal *= _1e5;
	range_error = decimal > x6_decimal_max;
	goto write_decimal;
multiply_by_1e4:
	decimal *= _1e4;
	range_error = decimal > x6_decimal_max;
	goto write_decimal;
multiply_by_1e3:
	decimal *= _1e3;
	range_error = decimal > x6_decimal_max;
	goto write_decimal;
multiply_by_1e2:
	decimal *= _1e2;
	range_error = decimal > x6_decimal_max;
	goto write_decimal;
multiply_by_1e1:
	decimal *= _1e1;
	range_error = decimal > x6_decimal_max;
	goto write_decimal;
already_6_decimals:
	range_error = decimal > x6_decimal_max;
	goto write_decimal;
divide_by_1e1:
	range_error = decimal.divide_and_get_remainder(_1e1) != 0 | decimal > x6_decimal_max;
	goto write_decimal;
divide_by_1e2:
	range_error = decimal.divide_and_get_remainder(_1e2) != 0 | decimal > x6_decimal_max;
	goto write_decimal;
divide_by_1e3:
	range_error = decimal.divide_and_get_remainder(_1e3) != 0 | decimal > x6_decimal_max;
	goto write_decimal;
divide_by_1e4:
	range_error = decimal.divide_and_get_remainder(_1e4) != 0 | decimal > x6_decimal_max;
	goto write_decimal;
divide_by_1e5:
	range_error = decimal.divide_and_get_remainder(_1e5) != 0 | decimal > x6_decimal_max;
	goto write_decimal;
divide_by_1e6:
	range_error = decimal.divide_and_get_remainder(_1e6) != 0 | decimal > x6_decimal_max;
	goto write_decimal;
divide_by_1e7:
	range_error = decimal.divide_and_get_remainder(_1e7) != 0 | decimal > x6_decimal_max;
	goto write_decimal;
divide_by_1e8:
	range_error = decimal.divide_and_get_remainder(_1e8) != 0 | decimal > x6_decimal_max;
	goto write_decimal;
divide_by_1e9:
	range_error = decimal.divide_and_get_remainder(_1e9) != 0 | decimal > x6_decimal_max;
	goto write_decimal;
divide_by_1e10:
	range_error = decimal.divide_and_get_remainder(_1e10) != 0 | decimal > x6_decimal_max;
	goto write_decimal;
divide_by_1e11:
	range_error = decimal.divide_and_get_remainder(_1e11) != 0 | decimal > x6_decimal_max;
	goto write_decimal;
divide_by_1e12:
	range_error = decimal.divide_and_get_remainder(_1e12) != 0 | decimal > x6_decimal_max;
	goto write_decimal;
divide_by_1e13:
	range_error = decimal.divide_and_get_remainder(_1e13) != 0 | decimal > x6_decimal_max;
	goto write_decimal;
divide_by_1e14:
	range_error = decimal.divide_and_get_remainder(_1e14) != 0 | decimal > x6_decimal_max;
	goto write_decimal;
divide_by_1e15:
	range_error = decimal.divide_and_get_remainder(_1e15) != 0 | decimal > x6_decimal_max;
	goto write_decimal;
divide_by_1e16:
	range_error = decimal.divide_and_get_remainder(_1e16) != 0 | decimal > x6_decimal_max;
	goto write_decimal;
divide_by_1e17:
	range_error = decimal.divide_and_get_remainder(_1e17) != 0 | decimal > x6_decimal_max;
	goto write_decimal;
divide_by_1e18:
	range_error = decimal.divide_and_get_remainder(_1e18) != 0 | decimal > x6_decimal_max;
	goto write_decimal;
divide_by_1e19:
	range_error = decimal.divide_and_get_remainder(_1e19) != 0 | decimal > x6_decimal_max;
	goto write_decimal;
divide_by_1e20:
	range_error = decimal.divide_and_get_remainder(_1e20) != 0 | decimal > x6_decimal_max;
	goto write_decimal;
divide_by_1e21:
	range_error = decimal.divide_and_get_remainder(_1e21) != 0 | decimal > x6_decimal_max;
	goto write_decimal;
divide_by_1e22:
	range_error = decimal.divide_and_get_remainder(_1e22) != 0 | decimal > x6_decimal_max;
write_decimal:
	// TODO: Test if < x6_decimal_min

	if (range_error == false) {
		// The value fits in a X6 decimal.
		int64_t raw_value = decimal.low() | (uint64_t(scale_sign) >> 31) << 63;
		*encoded_x6_decimal_ptr = encode_dec(raw_value);
		return 0;
	}
	else {
		// The value doesn't fit in a X6 decimal.
		/// TODO: Error code for range error.
		return 999L; // SCERRCONVERTDECIMALRANGEERROR
	}
}
