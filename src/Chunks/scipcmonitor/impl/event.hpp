//
// impl/event.hpp
//
// Copyright © 2006-2011 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//
// Implementation of class event.
//

#ifndef STARCOUNTER_CORE_IMPL_EVENT_HPP
#define STARCOUNTER_CORE_IMPL_EVENT_HPP

// Implementation

namespace starcounter {
namespace core {

event::event() {
	value_ = NULL;
}

event::event(LPSECURITY_ATTRIBUTES attributes, BOOL manual_reset, BOOL
initial_state, LPCTSTR name) {
	if ((value_ = CreateEvent(attributes, manual_reset, initial_state, name))
	== NULL) {
		throw event_error();
	}
}

event::event(LPSECURITY_ATTRIBUTES attributes, LPCTSTR name, DWORD flags,
DWORD desired_access) {
	if ((value_ = CreateEventEx(attributes, name, flags, desired_access))
	== NULL) {
		throw event_error();
	}
}

event::~event() {
	close();
}

event& event::operator=(const event& e) {
	if (this != &e) {
		if (value_ != NULL) {
			CloseHandle(value_);
		}
		value_ = e.value_;
	}
	return *this;
}

inline event& event::operator=(param_type n) {
	return assign(n);
}

inline event& event::assign(param_type n) {
	if (value_ != NULL) {
		CloseHandle(value_);
	}
	value_ = n;
	return *this;
}

void event::close() {
	if (value_ != NULL) {
		CloseHandle(value_);
	}
	value_ = NULL;
}

event::value_type event::get() const {
	return value_;
}

event::operator value_type() const {
	return value_;
}

// input
template<class CharT, class Traits>
inline std::basic_istream<CharT, Traits>& 
operator>>(std::basic_istream<CharT, Traits>& is, event& u) {
	event::value_type value;
	is >> value;
	u = event(value);
	return is;
}

// output
template<class CharT, class Traits>
inline std::basic_ostream<CharT, Traits>&
operator<<(std::basic_ostream<CharT, Traits>& os, const event& u) {
	os << u.get();
	return os;
}

} // namespace core
} // namespace starcounter

#endif // STARCOUNTER_CORE_IMPL_EVENT_HPP
