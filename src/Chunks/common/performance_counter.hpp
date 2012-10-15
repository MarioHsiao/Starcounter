//
// performance_counter.hpp
//
// Copyright © 2006-2012 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#ifndef STARCOUNTER_CORE_PERFORMANCE_COUNTER_HPP
#define STARCOUNTER_CORE_PERFORMANCE_COUNTER_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#include <iostream>
#include <cstddef>
#include <boost/call_traits.hpp>
#include <boost/cstdint.hpp>

namespace starcounter {
namespace core {

/// Class performance_counter has a counter that can be incremented and
/// decremented by 1 with an atomic operation.

class performance_counter {
public:
	// Type definitions.
	typedef int64_t value_type;
	typedef int64_t* iterator;
	typedef const int64_t* const_iterator;
	typedef int64_t& reference;
	typedef const int64_t& const_reference;
	
	// Helper types.
	
	// A type representing the "best" way to pass the value_type to a method.
	typedef boost::call_traits<value_type>::param_type param_type;
	
	// A type representing the "best" way to return the value_type from a const
	// method.
	typedef boost::call_traits<volatile value_type>::param_type return_type;
	
	// Construction/destruction.
	
	/// Constructor.
	/**
	 * @param n The value to assign.
	 * @throws Nothing.
	 * @par Complexity
	 *		Constant.
	 */
	performance_counter(param_type n = 0);
	
	// Let the compiler write copy constructor, copy assignment, and destructor.
	
	/// Copy assignment for performance_counter with volatile qualifier.
	volatile performance_counter& operator=(const performance_counter& a)
	volatile;
	
	/// Assignment from param_type.
	/**
	 * @param n The value to assign.
	 * @return A reference to this performance_counter.
	 * @throws Nothing.
	 * @par Exception Safety
	 *		No-throw.
	 */
	performance_counter& operator=(param_type n);
	
	/// Assign in place.
	/**
	 * @param n The value to assign.
	 * @return A reference to this performance_counter.
	 * @throws Nothing.
	 * @par Exception Safety
	 *		No-throw.
	 */
	performance_counter& assign(param_type n);
	
	// access to representation
	
	/// Get the value.
	/**
	 * @return The value.
	 * @throws Nothing.
	 * @par Exception Safety
	 *		No-throw.
	 */
	return_type get() const;
	
	/// Set the value.
	/**
	 * @param n The value to assign.
	 * @throws Nothing.
	 * @par Exception Safety
	 *		No-throw.
	 */
	void set(param_type n);
	
	// unary operators
	
	// increment
	
    /// operator++ prefix increment.
 	/**
	 * @return A reference to this performance_counter.
	 * @throws Nothing.
	 * @par Exception Safety
	 *		No-throw.
	 */
	performance_counter& operator++();
	
    /// Increment. Same as operator++ prefix increment.
 	/**
	 * @return A reference to this performance_counter.
	 * @throws Nothing.
	 * @par Exception Safety
	 *		No-throw.
	 */
	performance_counter& increment();
	
	// decrement
	
    /// operator-- prefix decrement.
 	/**
	 * @return A reference to this performance_counter.
	 * @throws Nothing.
	 * @par Exception Safety
	 *		No-throw.
	 */
	performance_counter& operator--();
	
    /// Decrement. Same as operator-- prefix decrement.
 	/**
	 * @return A reference to this performance_counter.
	 * @throws Nothing.
	 * @par Exception Safety
	 *		No-throw.
	 */
	performance_counter& decrement();
	
private:
	volatile value_type value_;
};

} // namespace core
} // namespace starcounter

#include "impl/performance_counter.hpp"

#endif // STARCOUNTER_CORE_PERFORMANCE_COUNTER_HPP
