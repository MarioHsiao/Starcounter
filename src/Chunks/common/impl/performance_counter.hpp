//
// impl/performance_counter.hpp
//
// Copyright © 2006-2012 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//
// Implementation of class performance_counter.
//

#ifndef STARCOUNTER_CORE_IMPL_PERFORMANCE_COUNTER_HPP
#define STARCOUNTER_CORE_IMPL_PERFORMANCE_COUNTER_HPP

#ifdef _MSC_VER
# pragma warning(push)
// Returning address of local variable or temporary.
# pragma warning(disable: 4172)
#endif // _MSC_VER

// Implementation

namespace starcounter {
namespace core {

// Member functions:

// Constructor.
inline performance_counter::performance_counter(param_type n) {
	assign(n);
}

// Copy assignment for performance_counter with volatile qualifier.
inline volatile performance_counter& performance_counter::operator=(const
performance_counter& a) volatile {
	_mm_mfence();
	value_ = a.value_;
	_mm_mfence();
	return *this;
}

// Assignment from param_type.
inline performance_counter& performance_counter::operator=(param_type n) {
	return assign(n);
}

// Assign in place.
inline performance_counter& performance_counter::assign(param_type n) {
	_mm_mfence();
	value_ = n;
	_mm_mfence();
	return *this;
}

// Access to representation.

inline performance_counter::return_type performance_counter::get() const {
	return value_;
}

inline void performance_counter::set(param_type n) {
	_mm_mfence();
	value_ = n;
	_mm_mfence();
}

// Unary operators.

inline performance_counter& performance_counter::operator++() {
	InterlockedIncrement64(&value_);
	return *this;
}

inline performance_counter& performance_counter::increment() {
	InterlockedIncrement64(&value_);
	return *this;
}

inline performance_counter& performance_counter::operator--() {
	InterlockedDecrement64(&value_);
	return *this;
}

inline performance_counter& performance_counter::decrement() {
	InterlockedDecrement64(&value_);
	return *this;
}

// Nonmember functions:

// Relational operators.
inline bool operator==(const performance_counter& lhs, const
performance_counter& rhs) {
	return lhs.get() == rhs.get();
}

inline bool operator<(const performance_counter& lhs, const
performance_counter& rhs) {
	return lhs.get() < rhs.get();
}

inline bool operator!=(const performance_counter& lhs, const
performance_counter& rhs) {
	return !(lhs == rhs);
}

inline bool operator>(const performance_counter& lhs, const
performance_counter& rhs) {
	return rhs < lhs;
}

inline bool operator<=(const performance_counter& lhs, const
performance_counter& rhs) {
	return !(rhs < lhs);
}

inline bool operator>=(const performance_counter& lhs, const
performance_counter& rhs) {
	return !(lhs < rhs);
}

// Input.
template<class CharT, class Traits>
inline std::basic_istream<CharT, Traits>& 
operator>>(std::basic_istream<CharT, Traits>& is, performance_counter& u) {
	performance_counter::value_type i;
	is >> i;
	u.set(n);
	return is;
}

// Output.
template<class CharT, class Traits>
inline std::basic_ostream<CharT, Traits>&
operator<<(std::basic_ostream<CharT, Traits>& os, const performance_counter& u)
{
	os << u.get();
	return os;
}

} // namespace core
} // namespace starcounter

#ifdef _MSC_VER
# pragma warning(pop)
#endif // _MSC_VER

#endif // STARCOUNTER_CORE_IMPL_PERFORMANCE_COUNTER_HPP
