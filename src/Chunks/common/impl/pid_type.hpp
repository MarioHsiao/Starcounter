//
// impl/pid_type.hpp
//
// Copyright © 2006-2011 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//
// Implementation of class pid_type.
//

#ifndef STARCOUNTER_CORE_IMPL_PID_TYPE_HPP
#define STARCOUNTER_CORE_IMPL_PID_TYPE_HPP

// Implementation

namespace starcounter {
namespace core {

// member functions:

// constructor
inline pid_type::pid_type(param_type pid) {
	assign(pid);
}

// assignment from param_type
inline pid_type& pid_type::operator=(param_type pid) {
	return assign(pid);
}

// assign in place
inline pid_type& pid_type::assign(param_type pid) {
	value_ = pid;
	return *this;
}

// access to representation

inline pid_type::return_type pid_type::get() const {
	return value_;
}

inline void pid_type::set(param_type pid) {
	value_ = pid;
}

inline pid_type::operator value_type() const {
	return value_;
}

inline pid_type::return_type pid_type::set_current() {
	#if defined(UNIX)
	pid_ = getpid();
	#elif defined(_WIN32) || defined(_WIN64)
	value_ = GetProcessId(GetCurrentProcess());
	#else
	# error Unsupported architecture.
	#endif // defined(UNIX)
	return value_;
}

// nonmember functions:

// relational operators
inline bool operator==(const pid_type& lhs, const pid_type& rhs) {
	return lhs.get() == rhs.get();
}

inline bool operator<(const pid_type& lhs, const pid_type& rhs) {
	return lhs.get() < rhs.get();
}

inline bool operator!=(const pid_type& lhs, const pid_type& rhs) {
	return !(lhs == rhs);
}

inline bool operator>(const pid_type& lhs, const pid_type& rhs) {
	return rhs < lhs;
}

inline bool operator<=(const pid_type& lhs, const pid_type& rhs) {
	return !(rhs < lhs);
}

inline bool operator>=(const pid_type& lhs, const pid_type& rhs) {
	return !(lhs < rhs);
}

// input
template<class CharT, class Traits>
inline std::basic_istream<CharT, Traits>& 
operator>>(std::basic_istream<CharT, Traits>& is, pid_type& u) {
	pid_type::value_type i;
	is >> i;
	u.set(n);
	return is;
}

// output
template<class CharT, class Traits>
inline std::basic_ostream<CharT, Traits>&
operator<<(std::basic_ostream<CharT, Traits>& os, const pid_type& u) {
	os << u.get();
	return os;
}

} // namespace core
} // namespace starcounter

#endif // STARCOUNTER_CORE_IMPL_PID_TYPE_HPP
