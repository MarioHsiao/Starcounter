//
// timeout.hpp
//
// Copyright © 2006-2013 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#ifndef STARCOUNTER_CORE_TIMEOUT_HPP
#define STARCOUNTER_CORE_TIMEOUT_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#include <cstdint>
#include <cstddef>
#include <iostream>

namespace starcounter {
namespace core {

/// Class timeout holds a value in the range 0..2^32 -2, including the
/// constant timeout::infinite to represent "forever." It doesn't specify
/// whether the value is in seconds, milliseconds, microseconds or nano-
/// seconds, etc. It could also represent minutes, hours, days, weeks,
/// months, years, etc. It doesn't include the actual timeout mechanism,
/// it just holds the value and it is suitable to be passed as arguments.
class timeout {
public:
	// Type definitions.
	typedef uint32_t value_type;
	typedef value_type param_type;
	typedef value_type return_type;
	
	enum {
		infinite = -1
	};
	
	// construction/destruction
	
	/// timeout constructor.
	/**
	 * @param t The value to assign. Default constructor assigns 0.
	 * @throws Nothing.
	 * @par Complexity
	 *      Constant.
	 */
	explicit timeout(param_type t = 0);
	
	// Let the compiler write copy constructor, copy assignment, and destructor.
	
	/// Assignment from param_type.
	/**
	 * @param t The value to assign.
	 * @return A reference to this timeout.
	 * @throws Nothing.
	 * @par Exception Safety
	 *      No-throw.
	 */
	timeout& operator=(param_type t);
	
	/// Assign in place.
	/**
	 * @param t The value to assign.
	 * @return A reference to this timeout.
	 * @throws Nothing.
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
	 * @throws Nothing.
	 * @par Exception Safety
	 *      No-throw.
	 */
	return_type get() const;
	
	/// Set the value.
	/**
	 * @param t The value to assign.
	 * @throws Nothing.
	 * @par Exception Safety
	 *      No-throw.
	 */
	void set(param_type t);
	
	/// Test if the value represents infinite time.
	/**
	 * @return true if the value represents infinite time, false otherwise.
	 * @throws Nothing.
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
