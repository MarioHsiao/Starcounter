//
// impl/timeout.hpp
//
// Copyright © 2006-2011 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//
// Implementation of class timeout.
//

#ifndef STARCOUNTER_CORE_IMPL_TIMEOUT_HPP
#define STARCOUNTER_CORE_IMPL_TIMEOUT_HPP

// Implementation

namespace starcounter {
namespace core {

// member functions:

// constructor
inline timeout::timeout(param_type t) {
	assign(t);
}

// assignment from param_type
inline timeout& timeout::operator=(param_type t) {
	return assign(t);
}

// assign in place
inline timeout& timeout::assign(param_type t) {
	value_ = t;
	return *this;
}

// access to representation

inline timeout::return_type timeout::get() const {
	return value_;
}

inline void timeout::set(param_type t) {
	value_ = t;
}

inline timeout::operator value_type() const {
	return value_;
}

inline bool timeout::is_infinite() const {
	return value_ == infinite;
}

// nonmember functions:

// relational operators
inline bool operator==(const timeout& lhs, const timeout& rhs) {
	return lhs.get() == rhs.get();
}

inline bool operator<(const timeout& lhs, const timeout& rhs) {
	return lhs.get() < rhs.get();
}

inline bool operator!=(const timeout& lhs, const timeout& rhs) {
	return !(lhs == rhs);
}

inline bool operator>(const timeout& lhs, const timeout& rhs) {
	return rhs < lhs;
}

inline bool operator<=(const timeout& lhs, const timeout& rhs) {
	return !(rhs < lhs);
}

inline bool operator>=(const timeout& lhs, const timeout& rhs) {
	return !(lhs < rhs);
}

// input
template<class CharT, class Traits>
inline std::basic_istream<CharT, Traits>& 
operator>>(std::basic_istream<CharT, Traits>& is, timeout& u) {
	timeout::value_type i;
	is >> i;
	u.set(n);
	return is;
}

// output
template<class CharT, class Traits>
inline std::basic_ostream<CharT, Traits>&
operator<<(std::basic_ostream<CharT, Traits>& os, const timeout& u) {
	os << u.get();
	return os;
}

} // namespace core
} // namespace starcounter

#endif // STARCOUNTER_CORE_IMPL_TIMEOUT_HPP
