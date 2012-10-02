//
// timeout.hpp
//
// Copyright © 2006-2011 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#ifndef STARCOUNTER_CORE_TIMEOUT_HPP
#define STARCOUNTER_CORE_TIMEOUT_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#include <iostream>
#include <cstddef>
#include <boost/call_traits.hpp>
#include <boost/cstdint.hpp>

namespace starcounter {
namespace core {

/// TODO: I considered using Boost.Date_Time, but it is so heavy and I only need
/// this very simple representation of a timeout value, including the infinite
/// constant. But I might be wrong so this is something to think about.

/// Class timeout holds a value in the range 0..2^^32 -2, including the
/// constant timeout::infinite to represent "forever." It doesn't specify
/// whether the value is in seconds, milli seconds, micro seconds or nano
/// seconds, etc. It could also represent minutes, hours, days, weeks,
/// months, years, etc. It doesn't include the actual timeout mechanism, it just
/// holds the value and it is suitable to be passed as arguments.
class timeout {
public:
	// type definitions
	typedef uint32_t value_type;
	
	// helper types
	
	// A type representing the "best" way to pass the value_type to a method.
	typedef boost::call_traits<value_type>::param_type param_type;
	
	// A type representing the "best" way to return the value_type from a const
	// method.
	typedef boost::call_traits<value_type>::param_type return_type;
	
	enum {
		infinite = ~0
	};
	
	// construction/destruction
	
	/// timeout constructor.
	/**
	 * @param t The value to assign. Default constructor assigns 0.
	 *
	 * @throws Nothing.
	 *
	 * @par Complexity
	 *      Constant.
	 */
	explicit timeout(param_type t = 0);
	
	// Let the compiler write copy constructor, copy assignment, and destructor.
	
	/// Assignment from param_type.
	/**
	 * @param t The value to assign.
	 *
	 * @return A reference to this timeout.
	 *
	 * @throws Nothing.
	 *
	 * @par Exception Safety
	 *      No-throw.
	 */
	timeout& operator=(param_type t);
	
	/// Assign in place.
	/**
	 * @param t The value to assign.
	 *
	 * @return A reference to this timeout.
	 *
	 * @throws Nothing.
	 *
	 * @par Exception Safety
	 *      No-throw.
	 */
	timeout& assign(param_type t);
	
	// access to representation
	
	/// TODO: Figure if this is dangerous!
	operator value_type() const;
	
	/// Get the value.
	/**
	 * @return The value.
	 *
	 * @throws Nothing.
	 *
	 * @par Exception Safety
	 *      No-throw.
	 */
	return_type get() const;
	
	/// Set the value.
	/**
	 * @param t The value to assign.
	 *
	 * @throws Nothing.
	 *
	 * @par Exception Safety
	 *      No-throw.
	 */
	void set(param_type t);
	
	/// Test if the value represents infinite time.
	/**
	 * @return true if the value represents infinite time, false otherwise.
	 *
	 * @throws Nothing.
	 *
	 * @par Exception Safety
	 *      No-throw.
	 */
	bool is_infinite() const;
	
private:
	value_type value_;
};

} // namespace core
} // namespace starcounter

#include "impl/timeout.hpp"

#endif // STARCOUNTER_CORE_TIMEOUT_HPP
