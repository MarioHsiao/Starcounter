//
// uint128_t.hpp
//
// Copyright © 2006-2013 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#ifndef STARCOUNTER_NUMERICS_UINT128_T_HPP
#define STARCOUNTER_NUMERICS_UINT128_T_HPP

#include "macro_definitions.hpp"

namespace starcounter {
namespace numerics {

/// Exception class when conversion from CLR decimal to Starcounter X6 decimal
/// cannot be done because data would be lost.
class remainder_not_zero {};

class uint128_t {
public:
	// Type definitions.
	typedef uint64_t low_type;
	typedef uint64_t high_type;
	
	// Constructors.
	uint128_t();
	//uint128_t(const uint64_t low);
	uint128_t(const int64_t low);
	uint128_t(const uint64_t high, const uint64_t low);
	
	//uint128_t& operator=(const uint128_t& rhs);
	
	// Assignment from param_type.
	//uint128_t& operator=(const uint64_t low);
	uint128_t& operator=(const int64_t low);
	uint128_t& assign(uint64_t high, uint64_t low);
	
	// Access to representation.
	const uint64_t low() const;
	const uint64_t high() const;
	
	// Arithmetic assignment operators.
	uint128_t& operator+=(const uint128_t& addend);
	uint128_t& operator-=(const uint128_t& subtrahend);
	uint128_t& operator*=(const uint128_t& multiplier);
	uint128_t& operator/=(const uint128_t& divisor);
	uint128_t& operator%=(const uint128_t& rhs);
	uint128_t& operator^=(const uint128_t& rhs);
	uint128_t& operator&=(const uint128_t& rhs);
	uint128_t& operator|=(const uint128_t& rhs);
	uint128_t& operator>>=(const uint128_t& rhs);
	uint128_t& operator<<=(const uint128_t& rhs);
	
	uint128_t& operator+=(uint64_t addend);
	uint128_t& operator-=(uint64_t subtrahend);
	uint128_t& operator*=(uint64_t multiplier);
	uint128_t& operator/=(uint64_t divisor);
	uint128_t& operator%=(uint64_t i);
	uint128_t& operator^=(uint64_t i);
	uint128_t& operator&=(uint64_t i);
	uint128_t& operator|=(uint64_t i);
	uint128_t& operator>>=(uint64_t shift);
	uint128_t& operator<<=(uint64_t shift);
	
	// Special function that don't belong in uint128_t, used when doing
	// CLR decimal to Starcounter X6 decimal conversion.
	// These functions will throw the exception remainder_not_zero if the
	// remainder is not zero, meaning there will be data loss when dividing.
	uint128_t& divide_and_throw_if_remainder_not_zero(const uint128_t& divisor);
	
	// Unary operators.
	
	// Increment and decrement.
	const uint128_t& operator++();
	const uint128_t operator++(int);
	const uint128_t& operator--();
	const uint128_t operator--(int);
	
	// Plus and minus.
	const uint128_t& operator+() const;
	uint128_t operator-() const;
	
	// Bitwise one's complement.
	uint128_t operator~() const;
	
	// Logical not.
	bool operator!() const;
	
	friend extern const uint128_t operator+(const uint128_t& augend, const uint128_t& addend);
	friend extern const uint128_t operator-(const uint128_t& augend, const uint128_t& addend);
	friend extern const uint128_t operator*(const uint128_t& lhs, const uint128_t& rhs);
	friend extern const uint128_t operator/(const uint128_t& lhs, const uint128_t& rhs);
	friend extern const uint128_t operator>>(const uint128_t& lhs, const uint128_t& rhs);
	friend extern const uint128_t operator<<(const uint128_t& lhs, const uint128_t& rhs);
	
	friend extern bool operator==(const uint128_t& lhs, const uint128_t& rhs);
	friend extern bool operator<(const uint128_t& lhs, const uint128_t& rhs);
	friend extern bool operator!=(const uint128_t& lhs, const uint128_t& rhs);
	friend extern bool operator>(const uint128_t& lhs, const uint128_t& rhs);
	friend extern bool operator<=(const uint128_t& lhs, const uint128_t& rhs);
	friend extern bool operator>=(const uint128_t& lhs, const uint128_t& rhs);
	
	const uint128_t& print_binary() const;
	
	template<class CharT, class Traits>
	friend extern std::basic_ostream<CharT, Traits>& operator<<
	(std::basic_ostream<CharT, Traits>& os, uint128_t u);
	
private:
#if defined(STARCOUNTER_LITTLE_ENDIAN_ORDER)
	low_type low_;
	high_type high_;
#elif defined(STARCOUNTER_BIG_ENDIAN_ORDER)
	high_type high_;
	low_type low_;
#else
# error Endian order not specified.
#endif
};

} // namespace numerics
} // namespace starcounter

#include "impl/uint128_t.hpp"

#endif // STARCOUNTER_NUMERICS_UINT128_T_HPP
