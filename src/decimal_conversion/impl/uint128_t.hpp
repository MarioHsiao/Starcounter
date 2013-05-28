//
// impl/uint128_t.hpp
//
// Copyright © 2006-2013 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//
// Implementation of class uint128_t.
//

#ifndef STARCOUNTER_NUMERICS_IMPL_UINT128_T_HPP
#define STARCOUNTER_NUMERICS_IMPL_UINT128_T_HPP

namespace starcounter {
namespace numerics {

inline uint128_t::uint128_t()
: low_(0), high_(0) {}

//inline uint128_t::uint128_t(const uint64_t low)
//: high_(0), low_(low) {}

inline uint128_t::uint128_t(const int64_t low)
: high_(-(low < 0)), low_(low) {}

inline uint128_t::uint128_t(const uint64_t high, const uint64_t low)
: high_(high), low_(low) {}

inline uint128_t::uint128_t(const uint32_t bit_127_to_96, const uint32_t bit_95_to_64,
const uint32_t bit_63_to_32, const uint32_t bit_31_to_0)
: high_((uint64_t(bit_127_to_96) << 32) | uint64_t(bit_95_to_64)),
low_((uint64_t(bit_63_to_32) << 32) | uint64_t(bit_31_to_0)) {}

//inline uint128_t& uint128_t::operator=(const uint128_t& rhs) {
//	high_ = rhs.high_;
//	low_ = rhs.low_;
//	return *this;
//}

//inline uint128_t& uint128_t::operator=(const uint64_t low) {
//	return assign(0, low);
//}

inline uint128_t& uint128_t::operator=(const int64_t low) {
	return assign(-(low < 0), low);
}

inline uint128_t& uint128_t::assign(const uint64_t high, const uint64_t low) {
	low_ = low;
	high_ = high;
	return *this;
}

const uint64_t uint128_t::low() const {
	return low_;
}

const uint64_t uint128_t::high() const {
	return high_;
}

// Arithmetic assignment operators:

inline uint128_t& uint128_t::operator+=(const uint128_t& addend) {
	high_ += addend.high_ +((low_ +addend.low_) < low_);
	low_ += addend.low_;
	return *this;
}

inline uint128_t& uint128_t::operator-=(const uint128_t& subtrahend) {
	high_ -= subtrahend.high_;
	high_ -= ((low_ -subtrahend.low_) > low_);
	low_ -= subtrahend.low_;
	return *this;
}

#if 0 // TODO: Vectorise multiplication with something like:
#include <emmintrin.h>
inline __m128i vec_mul_128(const __m128i& a, const __m128i& b) {
	__m128i xmm0 = _mm_mul_epu32(a, b);
	__m128i xmm1 = _mm_mul_epu32(_mm_srli_si128(a, 4), _mm_srli_si128(b, 4));
	
	// Shuffle the results to 63:0 and pack.
	return _mm_unpacklo_epi32(_mm_shuffle_epi32(xmm0, _MM_SHUFFLE(0, 0, 2, 0)),
	_mm_shuffle_epi32(xmm1, _MM_SHUFFLE(0, 0, 2, 0)));
}
#endif

inline uint128_t& uint128_t::operator*=(const uint128_t& multiplier) {
	uint64_t z[4] = {
		high_ >> 32,
		high_ & 0xFFFFFFFFULL,
		low_ >> 32,
		low_ & 0xFFFFFFFFULL
	};
	
	uint64_t y[4] = {
		multiplier.high() >> 32,
		multiplier.high() & 0xFFFFFFFFULL,
		multiplier.low() >> 32,
		multiplier.low() & 0xFFFFFFFFULL
	};
	
	uint64_t x[4][4];
	
	x[0][0] = y[0] * z[3];
	x[0][1] = y[1] * z[3];
	x[0][2] = y[2] * z[3];
	x[0][3] = y[3] * z[3];
	//x[1][0] = y[0] * z[2];
	x[1][1] = y[1] * z[2];
	x[1][2] = y[2] * z[2];
	x[1][3] = y[3] * z[2];
	//x[2][0] = y[0] * z[1];
	//x[2][1] = y[1] * z[1];
	x[2][2] = y[2] * z[1];
	x[2][3] = y[3] * z[1];
	//x[3][0] = y[0] * z[0];
	//x[3][1] = y[1] * z[0];
	//x[3][2] = y[2] * z[0];
	x[3][3] = y[3] * z[0];
	
	uint64_t a = x[0][3] & 0xFFFFFFFFULL;
	
	uint64_t b = (x[0][2] & 0xFFFFFFFFULL)
	+(x[0][3] >> 32)
	+(x[1][3] & 0xFFFFFFFFULL);
	
	uint64_t c = (x[0][1] & 0xFFFFFFFFULL)
	+(x[0][2] >> 32)
	+(x[1][2] & 0xFFFFFFFFULL)
	+(x[1][3] >> 32)
	+(x[2][3] & 0xFFFFFFFFULL);
	
	uint64_t d = (x[0][0] & 0xFFFFFFFFULL)
	+(x[0][1] >> 32)
	+(x[1][1] & 0xFFFFFFFFULL)
	+(x[1][2] >> 32)
	+(x[2][2] & 0xFFFFFFFFULL)
	+(x[2][3] >> 32)
	+(x[3][3] & 0xFFFFFFFFULL);
	
	uint128_t result(d << 32, 0);
	result += uint128_t(b >> 32, b << 32);
	result += uint128_t(c, 0);
	result += uint128_t(a);
	low_ = result.low();
	high_ = result.high();
	return *this;
}

// Make special version that throws an exception if n is not zero!
inline uint128_t& uint128_t::operator/=(const uint128_t& divisor) {
	// Simplify divisors that are powers of 2.
	uint32_t s = 0;
	uint128_t d(divisor);
	
	while ((d.low() & 1) == 0) {
		d >>= 1;
		++s;
	}
	
	if (d == 1) {
		*this >>= s;
		return *this;
	}
	
	uint128_t n(*this);
	uint128_t quotient = 0;
	
	while (n >= divisor) {
		uint128_t d(divisor);
		uint128_t t(1);
		
		while ((n >> 1) > d) {
			d <<= 1;
			t <<= 1;
		}
		
		n -= d;
		quotient += t;
	}
	
	low_ = quotient.low();
	high_ = quotient.high();
	return *this;
}

inline uint128_t uint128_t::divide_and_get_remainder(const uint128_t& divisor) {
	// Simplify divisors that are powers of 2.
	uint32_t s = 0;
	uint128_t d(divisor);
	
	while ((d.low() & 1) == 0) {
		d >>= 1;
		++s;
	}
	
	if (d == 1) {
		*this >>= s;
		return *this;
	}
	
	uint128_t n(*this);
	uint128_t quotient = 0;
	
	while (n >= divisor) {
		uint128_t d(divisor);
		uint128_t t(1);
		
		while ((n >> 1) > d) {
			d <<= 1;
			t <<= 1;
		}
		
		n -= d;
		quotient += t;
	}
	
	low_ = quotient.low();
	high_ = quotient.high();
	return n;
}

inline uint128_t& uint128_t::operator%=(const uint128_t& rhs) {
	return *this -= rhs * (*this / rhs);
}

inline uint128_t& uint128_t::operator^=(const uint128_t& rhs) {
	high_ ^= rhs.high_;
	low_ ^= rhs.low_;
	return *this;
}

inline uint128_t& uint128_t::operator&=(const uint128_t& rhs) {
	high_ &= rhs.high_;
	low_ &= rhs.low_;
	return *this;
}

inline uint128_t& uint128_t::operator|=(const uint128_t& rhs) {
	high_ |= rhs.high_;
	low_ |= rhs.low_;
	return *this;
}

inline uint128_t& uint128_t::operator>>=(const uint128_t& rhs) {
	if (rhs.high()) {
		low_ = 0;
		high_ = 0;
		return *this;
	}
	
	uint64_t shift = rhs.low();
	
	if (shift >= 128) {
		low_ = 0;
		high_ = 0;
		return *this;
	}
	else if (shift == 64) {
		low_ = high_;
		high_ = 0;
		return *this;
	}
	else if (shift == 0) {
		return *this;
	}
	else if (shift < 64) {
		return *this = uint128_t(high_ >> shift,
		(high_ << (64 -shift)) | (low_ >> shift));
	}
	else if ((128 > shift) && (shift > 64)) {
		return *this = uint128_t(0, (high_ >> (shift -64)));
	}
	else {
		return *this = uint128_t(uint64_t(0));
	}
	return *this;
}

inline uint128_t& uint128_t::operator<<=(const uint128_t& rhs) {
	if (rhs.high()) {
		low_ = 0;
		high_ = 0;
		return *this;
	}
	
	uint64_t shift = rhs.low();
	
	if (shift >= 128) {
		low_ = 0;
		high_ = 0;
		return *this;
	}
	else if (shift == 64) {
		high_ = low_;
		low_ = 0;
		return *this;
	}
	else if (shift == 0) {
		return *this;
	}
	else if (shift < 64) {
		return *this = uint128_t((high_ << shift) | (low_ >> (64 -shift)),
		low_ << shift);
	}
	else if ((128 > shift) && (shift > 64)) {
		return *this = uint128_t(low_ << (shift -64), 0);
	}
	else {
		return *this = uint128_t(uint64_t(0));
	}
	return *this;
}

inline uint128_t& uint128_t::operator+=(uint64_t addend) {
	high_ += (low_ +addend) < low_;
	low_ += addend;
	return *this;
}

inline uint128_t& uint128_t::operator-=(uint64_t subtrahend) {
	high_ -= (low_ -subtrahend) > low_;
	low_ -= subtrahend;
	return *this;
}

inline uint128_t& uint128_t::operator*=(uint64_t multiplier) {
	return *this *= uint128_t(multiplier);
}

inline uint128_t& uint128_t::operator/=(uint64_t divisor) {
	return *this /= uint128_t(divisor);
}

inline uint128_t& uint128_t::operator%=(uint64_t i) {
	return *this %= uint128_t(i);
}

//inline uint128_t& uint128_t::operator%=(uint64_t i);

inline uint128_t& uint128_t::operator^=(uint64_t i) {
	return *this ^= uint128_t(i);
}

inline uint128_t& uint128_t::operator&=(uint64_t i) {
	return *this &= uint128_t(i);
}

inline uint128_t& uint128_t::operator|=(uint64_t i) {
	return *this |= uint128_t(i);
}

inline uint128_t& uint128_t::operator>>=(uint64_t shift) {
	if (shift >= 128) {
		low_ = 0;
		high_ = 0;
		return *this;
	}
	else if (shift == 64) {
		low_ = high_;
		high_ = 0;
		return *this;
	}
	else if (shift == 0) {
		return *this;
	}
	else if (shift < 64) {
		return *this = uint128_t(high_ >> shift,
		(high_ << (64 -shift)) | (low_ >> shift));
	}
	else if ((128 > shift) && (shift > 64)) {
		return *this = uint128_t(0, (high_ >> (shift -64)));
	}
	else {
		return *this = uint128_t(uint64_t(0));
	}
	return *this;
}
	
inline uint128_t& uint128_t::operator<<=(uint64_t shift) {
	if (shift >= 128) {
		low_ = 0;
		high_ = 0;
		return *this;
	}
	else if (shift == 64) {
		high_ = low_;
		low_ = 0;
		return *this;
	}
	else if (shift == 0) {
		return *this;
	}
	else if (shift < 64) {
		return *this = uint128_t((high_ << shift) | (low_ >> (64 -shift)),
		low_ << shift);
	}
	else if ((128 > shift) && (shift > 64)) {
		return *this = uint128_t(low_ << (shift -64), 0);
	}
	else {
		return *this = uint128_t(uint64_t(0));
	}
	return *this;
}

// Unary operators.

inline const uint128_t& uint128_t::operator++() {
	*this += 1;
	return *this;
}

inline const uint128_t uint128_t::operator++(int) {
	uint128_t old_value(*this);
	++*this;
	return old_value;
}

inline const uint128_t& uint128_t::operator--() {
	*this -= 1;
	return *this;
}

inline const uint128_t uint128_t::operator--(int) {
	uint128_t old_value(*this);
	--*this;
	return old_value;
}

inline const uint128_t& uint128_t::operator+() const {
	return *this;
}

inline uint128_t uint128_t::operator-() const {
	//return ~uint128_t(*this) +1;
	return ~*this += 1;
}

inline uint128_t uint128_t::operator~() const {
	return uint128_t(~high(), ~low());
}

inline bool uint128_t::operator!() const {
#pragma warning (disable: 4800)
	return !bool(low() | high());
#pragma warning (default: 4800)
}

const uint128_t& uint128_t::print_binary() const {
	for (int i = 0; i < 64; ++i) {
		char c = high_ & (1ULL << (63 -i)) ? '1' : '0';
		std::cout << c;
	}

	for (int i = 0; i < 64; ++i) {
		char c = low_ & (1ULL << (63 -i)) ? '1' : '0';
		std::cout << c;
	}

	return *this;
}

// Nonmember functions:

// Arithmetic binary operators

// operator+ implemented in terms of operator+=
const uint128_t operator+(const uint128_t& augend, const uint128_t& addend) {
	return uint128_t(augend) += addend;
}

const uint128_t operator+(const uint128_t& augend, uint64_t addend) {
	return uint128_t(augend) += addend;
}

const uint128_t operator+(uint64_t augend, const uint128_t& addend) {
	return uint128_t(augend) += addend;
}

// operator- implemented in terms of operator-=
const uint128_t operator-(const uint128_t& minuend, const uint128_t& subtrahend) {
	return uint128_t(minuend) -= subtrahend;
}

const uint128_t operator-(const uint128_t& minuend, uint64_t subtrahend) {
	return uint128_t(minuend) -= subtrahend;
}

const uint128_t operator-(uint64_t minuend, const uint128_t& subtrahend) {
	return uint128_t(minuend) -= subtrahend;
}

// operator* implemented in terms of operator*=
const uint128_t operator*(const uint128_t& multiplicand, const uint128_t& multipler) {
	return uint128_t(multiplicand) *= multipler;
}

const uint128_t operator*(const uint128_t& multiplicand, uint64_t multipler) {
	return uint128_t(multiplicand) *= multipler;
}

const uint128_t operator*(uint64_t multiplicand, const uint128_t& multipler) {
	return uint128_t(multiplicand) *= multipler;
}

// operator/ implemented in terms of operator/=
const uint128_t operator/(const uint128_t& dividend, const uint128_t& divisor) {
	return uint128_t(dividend) /= divisor;
}

const uint128_t operator/(const uint128_t& dividend, uint64_t divisor) {
	return uint128_t(dividend) /= divisor;
}

const uint128_t operator/(uint64_t dividend, const uint128_t& divisor) {
	return uint128_t(dividend) /= divisor;
}

// operator% implemented in terms of operator%=
const uint128_t operator%(const uint128_t& rhs, const uint128_t& lhs) {
	return uint128_t(rhs) %= lhs;
}

const uint128_t operator%(const uint128_t& rhs, uint64_t lhs) {
	return uint128_t(rhs) %= lhs;
}

const uint128_t operator%(uint64_t rhs, const uint128_t& lhs) {
	return uint128_t(rhs) %= lhs;
}

#if 0
template<typename T>
uint128_t operator%(T rhs){
	return *this % uint128_t(rhs);
}

uint128_t operator%(uint128_t rhs){
	return *this - (rhs * (*this / rhs));
}

template<typename T>
uint128_t operator%=(T rhs){
	*this = *this % uint128_t(rhs);
	return *this;
}

uint128_t operator%=(uint128_t rhs){
	*this = *this % rhs;
	return *this;
}

#endif

// operator>> implemented in terms of operator>>=
const uint128_t operator>>(const uint128_t& lhs, const uint128_t& rhs) {
	return uint128_t(lhs) >>= rhs;
}

const uint128_t operator>>(const uint128_t& lhs, uint64_t rhs) {
	return uint128_t(lhs) >>= rhs;
}

const uint128_t operator>>(uint64_t lhs, const uint128_t& rhs) {
	return uint128_t(lhs) >>= rhs;
}

// operator<< implemented in terms of operator<<=
const uint128_t operator<<(const uint128_t& lhs, const uint128_t& rhs) {
	return uint128_t(lhs) <<= rhs;
}

const uint128_t operator<<(const uint128_t& lhs, uint64_t rhs) {
	return uint128_t(lhs) <<= rhs;
}

const uint128_t operator<<(uint64_t lhs, const uint128_t& rhs) {
	return uint128_t(lhs) <<= rhs;
}

// Comparison operators.

inline bool operator==(const uint128_t& lhs, const uint128_t& rhs) {
	return lhs.low() == rhs.low() && lhs.high() == rhs.high();
}

inline bool operator<(const uint128_t& lhs, const uint128_t& rhs) {
	return lhs.high() == rhs.high() ?
	lhs.low() < rhs.low() : lhs.high() < rhs.high();
}

inline bool operator!=(const uint128_t& lhs, const uint128_t& rhs) {
	return !(lhs == rhs);
}

inline bool operator>(const uint128_t& lhs, const uint128_t& rhs) {
	return rhs < lhs;
}

inline bool operator<=(const uint128_t& lhs, const uint128_t& rhs) {
	return !(rhs < lhs);
}

inline bool operator>=(const uint128_t& lhs, const uint128_t& rhs) {
	return !(lhs < rhs);
}

// Output
template<class CharT, class Traits>
inline std::basic_ostream<CharT, Traits>& operator<<
(std::basic_ostream<CharT, Traits>& os, uint128_t u) {
	std::basic_string<CharT, Traits> s = "";
	
	if (u != 0) {
		int base = 10;
		
		if (os.flags() & os.hex) {
			base = 16;
		}
		else if (os.flags() & os.oct) {
			base = 8;
		}
		
		do {
			s = (u % base).low()["0123456789ABCDEF"] +s;
			u /= base;
		} while (u > 0);
	}
	else {
		s = "0";
	}
	
	return os << s;
}

} // namespace numerics
} // namespace starcounter

#endif // STARCOUNTER_NUMERICS_IMPL_UINT128_T_HPP
