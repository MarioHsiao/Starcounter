//
// impl/decimal.hpp
//
// Copyright © 2006-2013 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//
// Implementation of decimal.
//

#ifndef STARCOUNTER_NUMERICS_IMPL_DECIMAL_HPP
#define STARCOUNTER_NUMERICS_IMPL_DECIMAL_HPP

namespace starcounter {
namespace numerics {
namespace clr {

inline decimal::decimal(int32_t low, int32_t middle, int32_t high, bool is_negative, uint8_t scale)
: low_(low), middle_(middle), high_(high),
scale_sign_((is_negative << 31) | (scale_sign_type(scale) << 16)) {}

inline void decimal::print() {
	std::cout << "CLR Decimal (hex): " << "0x" << std::hex << std::setw(8) << std::setfill('0')
	<< scale_sign_ << high_ << middle_ << low_ << '\n';

	uint128_t a(scale_sign_, high_, middle_, low_);
	
	std::cout << "CLR Decimal (bin): ";
	a.print_binary();
	std::cout << std::endl;
}

} // namespace clr
} // namespace numerics
} // namespace starcounter

#endif // STARCOUNTER_NUMERICS_IMPL_DECIMAL_HPP
