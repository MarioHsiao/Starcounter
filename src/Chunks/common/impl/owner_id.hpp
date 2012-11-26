//
// impl/owner_id.hpp
//
// Copyright © 2006-2012 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//
// Implementation of class owner_id.
//

#ifndef STARCOUNTER_CORE_IMPL_OWNER_ID_HPP
#define STARCOUNTER_CORE_IMPL_OWNER_ID_HPP

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
inline owner_id::owner_id(param_type n) {
	assign(n);
}

// Copy assignment for owner_id with volatile qualifier.
inline volatile owner_id& owner_id::operator=(const owner_id& a) volatile {
	_mm_mfence();
	value_ = a.value_;
	_mm_mfence();
	return *this;
}

// operator&=() with volatile qualifier.
inline volatile owner_id& owner_id::operator&=(const owner_id& a) volatile {
	_mm_mfence();
	value_ &= a.value_;
	_mm_mfence();
	return *this;
}

// Assignment from param_type.
inline owner_id& owner_id::operator=(param_type n) {
	return assign(n);
}

// Assign in place.
inline owner_id& owner_id::assign(param_type n) {
	_mm_mfence();
	value_ = n;
	_mm_mfence();
	return *this;
}

// Access to representation.

inline owner_id::return_type owner_id::get() const {
	return value_;
}

inline void owner_id::set(param_type n) {
	_mm_mfence();
	value_ = n;
	_mm_mfence();
}

inline owner_id::return_type owner_id::get_owner_id() const {
	// Mask bit 31 containing the clean-up flag. A zero will be shifted in from
	// the left side since value_ is unsigned.

	// TODO: Optimize. Which is faster?
	//return value_ & id_field;
	return (value_ << 1) >> 1;
}

inline owner_id::return_type owner_id::get_clean_up() const {
	// Mask bit 30:0 containing the owner_id field.
	return value_ >> 31;
}

inline void owner_id::mark_for_clean_up() {
	_mm_mfence();
	value_ |= 1ULL << 31;
	_mm_mfence();
}

inline bool owner_id::is_no_owner_id() const {
	return static_cast<bool>(((value_ << 1) >> 1) == none);
}

// Unary operators.

// Prefix increment.
inline owner_id& owner_id::operator++() {
	value_ += 1;
	return *this;
}

// Nonmember functions:

// Relational operators.
inline bool operator==(const owner_id& lhs, const owner_id& rhs) {
	return lhs.get() == rhs.get();
}

inline bool operator<(const owner_id& lhs, const owner_id& rhs) {
	return lhs.get() < rhs.get();
}

inline bool operator!=(const owner_id& lhs, const owner_id& rhs) {
	return !(lhs == rhs);
}

inline bool operator>(const owner_id& lhs, const owner_id& rhs) {
	return rhs < lhs;
}

inline bool operator<=(const owner_id& lhs, const owner_id& rhs) {
	return !(rhs < lhs);
}

inline bool operator>=(const owner_id& lhs, const owner_id& rhs) {
	return !(lhs < rhs);
}

// Input.
template<class CharT, class Traits>
inline std::basic_istream<CharT, Traits>& 
operator>>(std::basic_istream<CharT, Traits>& is, owner_id& u) {
	owner_id::value_type i;
	is >> i;
	u.set(n);
	return is;
}

// Output.
template<class CharT, class Traits>
inline std::basic_ostream<CharT, Traits>&
operator<<(std::basic_ostream<CharT, Traits>& os, const owner_id& u) {
	os << u.get();
	return os;
}

} // namespace core
} // namespace starcounter

#ifdef _MSC_VER
# pragma warning(pop)
#endif // _MSC_VER

#endif // STARCOUNTER_CORE_IMPL_OWNER_ID_HPP
