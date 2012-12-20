//
// event.hpp
//
// Copyright © 2006-2011 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#ifndef STARCOUNTER_CORE_EVENT_HPP
#define STARCOUNTER_CORE_EVENT_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#include <cstddef>
#include <iostream>
#include <boost/call_traits.hpp>
#include <boost/cstdint.hpp>
#include <boost/utility.hpp>
#include <boost/thread/win32/thread_primitives.hpp>

namespace starcounter {
namespace core {

/// class event.
/// TODO: Consider using thread_primitives.hpp as an improvement if possible.
class event {
public:
	// Type definitions.
	typedef boost::detail::win32::handle value_type;
	typedef boost::detail::win32::handle* iterator;
	typedef const boost::detail::win32::handle* const_iterator;
	typedef boost::detail::win32::handle& reference;
	typedef const boost::detail::win32::handle& const_reference;
	
	// Helper types.
	
	// A type representing the "best" way to pass the value_type to a method.
	typedef boost::call_traits<value_type>::param_type param_type;
	
	// A type representing the "best" way to return the value_type from a const
	// method.
	typedef boost::call_traits<value_type>::param_type return_type;
	
	// Construction/destruction.
	
	class event_error {};
	
	/// The default constructor initializes the event to NULL.
	event();
	
	/// Creates or opens a named or unnamed event object.
	/// The Windows API documentation for the arguments to CreateEvent(), which
	/// this constructor uses, are repeated below.
	/**
	 * @param attributes A pointer to a SECURITY_ATTRIBUTES structure. If this
	 *      parameter is NULL, the handle cannot be inherited by child
	 *      processes. The lpSecurityDescriptor member of the structure
	 *      specifies a security descriptor for the new event. If
	 *      attributes is NULL, the event gets a default security descriptor.
	 *      The ACLs in the default security descriptor for an event come from
	 *      the primary or impersonation token of the creator.
	 *
	 * @param manual_reset If this parameter is TRUE, the function creates a
	 *      manual-reset event object, which requires the use of the ResetEvent
	 *      function to set the event state to nonsignaled. If this parameter is
	 *      FALSE, the function creates an auto-reset event object, and system
	 *      automatically resets the event state to nonsignaled after a single
	 *      waiting thread has been released.
	 *
	 * @param initial_state If this parameter is TRUE, the initial state of the
	 *      event object is signaled; otherwise, it is nonsignaled.
	 *
	 * @param name The name of the event object. The name is limited to MAX_PATH
	 *      characters. Name comparison is case sensitive. If name matches the
	 *      name of an existing named event object, this function requests the
	 *      EVENT_ALL_ACCESS access right. In this case, the manual_reset and
	 *      initial_state parameters are ignored because they have already been
	 *      set by the creating process. If the attributes parameter is not
	 *      NULL, it determines whether the handle can be inherited, but its
	 *      security-descriptor member is ignored. If name is NULL, the event
	 *      object is created without a name. If name matches the name of
	 *      another kind of object in the same namespace (such as an existing
	 *      semaphore, mutex, waitable timer, job, or file-mapping object), the
	 *      function fails and the GetLastError function returns
	 *      ERROR_INVALID_HANDLE. This occurs because these objects share the
	 *      same namespace.
	 *
	 * @throws event_error.
	 */
	explicit event(LPSECURITY_ATTRIBUTES attributes, BOOL manual_reset, BOOL
	initial_state, LPCTSTR name);
	
	/// Creates or opens a named or unnamed event object.
	/// The Windows API documentation for the arguments to CreateEventEx(),
	/// which this constructor uses, are repeated below.
	/**
	 * @param attributes A pointer to a SECURITY_ATTRIBUTES structure. If this
	 *      parameter is NULL, the handle cannot be inherited by child
	 *      processes. The lpSecurityDescriptor member of the structure
	 *      specifies a security descriptor for the new event. If
	 *      attributes is NULL, the event gets a default security descriptor.
	 *      The ACLs in the default security descriptor for an event come from
	 *      the primary or impersonation token of the creator.
	 *
	 * @param name The name of the event object. The name is limited to MAX_PATH
	 *      characters. Name comparison is case sensitive. If name matches the
	 *      name of an existing named event object, this function requests the
	 *      EVENT_ALL_ACCESS access right. In this case, the manual_reset and
	 *      initial_state parameters are ignored because they have already been
	 *      set by the creating process. If the attributes parameter is not
	 *      NULL, it determines whether the handle can be inherited, but its
	 *      security-descriptor member is ignored. If name is NULL, the event
	 *      object is created without a name. If name matches the name of
	 *      another kind of object in the same namespace (such as an existing
	 *      semaphore, mutex, waitable timer, job, or file-mapping object), the
	 *      function fails and the GetLastError function returns
	 *      ERROR_INVALID_HANDLE. This occurs because these objects share the
	 *      same namespace.
	 *
	 * @param flags This parameter can be one or more of the following values:
	 *      CREATE_EVENT_INITIAL_SET The initial state of the event object is
	 *      signaled; otherwise, it is nonsignaled.
	 *
	 *      CREATE_EVENT_MANUAL_RESET The event must be manually reset using the
	 *      ResetEvent function. Any number of waiting threads, or threads that
	 *      subsequently begin wait operations for the specified event object,
	 *      can be released while the object's state is signaled. If this flag
	 *      is not specified, the system automatically resets the event after
	 *      releasing a single waiting thread.
	 *
	 * @param desired_access The access mask for the event object. For a list of
	 *      access rights, see the Windows API documentation for Synchronization
	 *      Object Security and Access Rights.
	 *
	 * @throws event_error.
	 */
	explicit event(LPSECURITY_ATTRIBUTES attributes, LPCTSTR name, DWORD flags,
	DWORD desired_access);
	
	// Let the compiler write the copy constructor.
	
	/// The destructor closes the handle.
	/**
	 * @throws Nothing.
	 *
	 * @par Exception Safety
	 *      No-throw.
	 */
	~event();
	
	/// Copy assignment.
	/**
	 * @return A reference to the object.
	 *
	 * @throws Nothing.
	 *
	 * @par Exception Safety
	 *      No-throw.
	 */
	// NOTE: It will close the handle (if it is not NULL) before assignment.
	event& operator=(const event&);
	
	/// Assignment from param_type.
	/**
	 * @param n The value to assign.
	 *
	 * @return A reference to this event.
	 *
	 * @throws Nothing.
	 *
	 * @par Exception Safety
	 *      No-throw.
	 */
	// NOTE: It will close the handle (if it is not NULL) before assignment.
	event& operator=(param_type n);
	
	/// Assign in place.
	/**
	 * @param n The value to assign.
	 *
	 * @return A reference to this event.
	 *
	 * @throws Nothing.
	 *
	 * @par Exception Safety
	 *      No-throw.
	 */
	// NOTE: It will close the handle (if it is not NULL) before assignment.
	event& assign(param_type n);
	
	// access to representation
	
	/// Close the handle and sets it to NULL.
	/**
	 * @throws Nothing.
	 *
	 * @par Exception Safety
	 *      No-throw.
	 */
	void close();
	
	/// get value.
	/**
	 * @throws Nothing.
	 *
	 * @par Exception Safety
	 *      No-throw.
	 */
	value_type get() const;
	
	/// Reading (implemented by a conversion operator) is trivial.
	/**
	 * @throws Nothing.
	 *
	 * @par Exception Safety
	 *      No-throw.
	 */
	operator value_type() const;
	
private:
	value_type value_;
};

} // namespace core
} // namespace starcounter

#include "impl/event.hpp"

#endif // STARCOUNTER_CORE_EVENT_HPP
