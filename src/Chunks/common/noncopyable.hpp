//
// noncopyable.hpp
//
// Copyright © 2006-2013 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#ifndef STARCOUNTER_CORE_NONCOPYABLE_HPP
#define STARCOUNTER_CORE_NONCOPYABLE_HPP

namespace starcounter {
namespace core {

// Private copy constructor and copy assignment ensure classes derived from
// class noncopyable cannot be copied.

// Protection from unintended argument-dependent lookup (ADL.)
namespace noncopyable_ {

class noncopyable {
protected:
	noncopyable() {}
	~noncopyable() {}
private:
	// Emphasize the following members are private.
	noncopyable(const noncopyable&);
	const noncopyable& operator=(const noncopyable&);
};

} // namespace noncopyable_

typedef noncopyable_::noncopyable noncopyable;

} // namespace core
} // namespace starcounter

#endif // STARCOUNTER_CORE_NONCOPYABLE_HPP
