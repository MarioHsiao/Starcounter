//
// pid_type.hpp
//
// Copyright © 2006-2013 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#ifndef STARCOUNTER_CORE_PID_TYPE_HPP
#define STARCOUNTER_CORE_PID_TYPE_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#include <cstdint>
#include <cstddef>
#include <iostream>

#if defined(UNIX)
# include <sys/types.h>
# include <unistd.h>
#elif defined(_WIN32) || defined(_WIN64)
# define WIN32_LEAN_AND_MEAN
# include <windows.h>
# undef WIN32_LEAN_AND_MEAN
#else
# error Unsupported architecture.
#endif // defined(UNIX)

namespace starcounter {
namespace core {

/// Class pid_type represents the OS dependent process ID type.
class pid_type {
public:
	// Type definitions.
	#if defined(UNIX)
	typedef pid_t value_type;
	#elif defined(_WIN32) || defined(_WIN64)
	typedef DWORD value_type;
	#else
	# error Unsupported architecture.
	#endif // defined(UNIX)
	
	typedef value_type param_type;
	typedef value_type return_type;
	
	enum {
		// none indicates no processor id.
		no_pid = ~value_type(0)
	};
	
	// construction/destruction
	
	/// pid_type constructor.
	/**
	 * @param pid The value to assign. Default constructor assigns no_pid.
	 *
	 * @throws Nothing.
	 *
	 * @par Complexity
	 *      Constant.
	 */
	explicit pid_type(param_type pid = no_pid);
	
	// Let the compiler write copy constructor, copy assignment, and destructor.
	
	/// Assignment from param_type.
	/**
	 * @param pid The value to assign.
	 *
	 * @return A reference to this pid_type.
	 *
	 * @throws Nothing.
	 *
	 * @par Exception Safety
	 *      No-throw.
	 */
	pid_type& operator=(param_type pid);
	
	/// Assign in place.
	/**
	 * @param pid The value to assign.
	 *
	 * @return A reference to this pid_type.
	 *
	 * @throws Nothing.
	 *
	 * @par Exception Safety
	 *      No-throw.
	 */
	pid_type& assign(param_type pid);
	
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
	 * @param pid The value to assign.
	 *
	 * @throws Nothing.
	 *
	 * @par Exception Safety
	 *      No-throw.
	 */
	void set(param_type pid);
	
	/// Set the process ID of the current process.
	/**
	 * @return The value.
	 *
	 * @throws Nothing.
	 *
	 * @par Exception Safety
	 *      No-throw.
	 */
	return_type set_current();
	
private:
	value_type value_;
};

} // namespace core
} // namespace starcounter

#include "impl/pid_type.hpp"

#endif // STARCOUNTER_CORE_OWNER_ID_HPP
