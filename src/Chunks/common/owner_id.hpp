//
// owner_id.hpp
//
// Copyright � 2006-2012 Starcounter AB. All rights reserved.
// Starcounter� is a registered trademark of Starcounter AB.
//

#ifndef STARCOUNTER_CORE_OWNER_ID_HPP
#define STARCOUNTER_CORE_OWNER_ID_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#include <iostream>
#include <cstddef>
#include <boost/call_traits.hpp>
#include <boost/cstdint.hpp>

namespace starcounter {
namespace core {

/// Class owner_id has a 64-bit word (value_), where 62:0 is the owner_id field,
/// and bit 63 is a flag "c" indicating if the resource that has this owner_id
/// needs clean-up or not.

// NOTE: The range of the owner_id field need to be large enough to minimize the
// chance to exceed the range during a session (while the monitor is running).
//
// owner_id:
//  6 6 6 6 5 5 5 5 5 5 5 5 5 5 4 4 4 4 4 4 4 4 4 4 3 3 3 3 3 3 3 3
//  3 2 1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2
// +-+-------------------------------------------------------------+
// |c|                                                             :
// +-+-------------------------------------------------------------+
//
//  3 3 2 2 2 2 2 2 2 2 2 2 1 1 1 1 1 1 1 1 1 1
//  1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2 1 0
// +---------------------------------------------------------------+
// :                                                             id|
// +---------------------------------------------------------------+
//
// Bit 0:62 for id. The id field is so large that it never have to be re-cycled.
//
// Bit 63 is the clean-up flag.
//
//******************************************************************************
// New idea, not implemented yet, is:
//
// owner_id:
//  3 3 2 2 2 2 2 2 2 2 2 2 1 1 1 1 1 1 1 1 1 1
//  1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2 1 0
// +-+-----------------+-------------------------------------------+
// |c|               id|                                      index|
// +-+-----------------+-------------------------------------------+
//
// Bit 0:21 (22 bits) for an index to any kind of resource. Currently the
// largest index is 13 bits, for the chunks. But we are discussing how to handle
// the gateway client, that requires "millions" of small (128 Byte) chunks. In
// this case the index need to be in the range 20 bits, or so. With 22 bits we
// can address 4194304 chunks, but if we decide to use more than one pool, some
// of these index bits must be used to select which pool the chunk is in.
//
// Bit 22:30 (9 bits) for id allows 511 unique IDs to be "active", so in total
// up to 511 database and client processes can be monitored simultaneously.
// Today the limit is 256 client processes and 64 database processes.
//
// The monitor re-cycles the ID values. It will have a table with occupied and
// free IDs.
//
// Bit 31 is the clean-up flag. I'm not sure that it need to be stored in the
// owner_id, but it is OK that it is stored there.
//
// If we need more bits for the index part, we can shrink the id field because
// when the gateway client runs, no other client runs. Therefore a single bit
// (or maybe no bit at all..if thinking about it) is needed for the id field.
//
// owner_id:
//  3 3 2 2 2 2 2 2 2 2 2 2 1 1 1 1 1 1 1 1 1 1
//  1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2 1 0
// +-+-+-----------------------------------------------------------+
// |c|i|                                                      index|
// | |d|                                                           |
// +-+-+-----------------------------------------------------------+
//
// This "shrink" can be achieved without recompilation simply because we can
// let the type of client decide which method to use in the owner_id class, to
// read/write the individual fields.
//
//******************************************************************************

class owner_id {
public:
	// Type definitions.
	typedef uint64_t value_type;
	typedef uint64_t* iterator;
	typedef const uint64_t* const_iterator;
	typedef uint64_t& reference;
	typedef const uint64_t& const_reference;
	
	// Helper types.
	
	// A type representing the "best" way to pass the value_type to a method.
	typedef boost::call_traits<value_type>::param_type param_type;
	
	// A type representing the "best" way to return the value_type from a const
	// method.
	typedef boost::call_traits<volatile value_type>::param_type return_type;
	
	enum {
		// none indicates that the resource has no owner-id. Both the owner_id
		// field and the clean-up flag is zero.
		none = 0
	};
	
	// Construction/destruction.
	
	/// Constructor.
	/**
	 * @param n The value to assign.
	 *
	 * @throws Nothing.
	 *
	 * @par Complexity
	 *      Constant.
	 */
	owner_id(param_type n = owner_id::none);
	
	//owner_id(const owner_id& oid)
	//: value_(oid.value_) {}

	// Let the compiler write copy constructor, copy assignment, and destructor.

	/// Copy assignment for owner_id with volatile qualifier.
	volatile owner_id& operator=(const owner_id& a) volatile;

	/// Assignment from param_type.
	/**
	 * @param n The value to assign.
	 *
	 * @return A reference to this owner_id.
	 *
	 * @throws Nothing.
	 *
	 * @par Exception Safety
	 *      No-throw.
	 */
	owner_id& operator=(param_type n);
	
	/// Assign in place.
	/**
	 * @param n The value to assign.
	 *
	 * @return A reference to this owner_id.
	 *
	 * @throws Nothing.
	 *
	 * @par Exception Safety
	 *      No-throw.
	 */
	owner_id& assign(param_type n);
	
	// access to representation
	
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
	 * @param n The value to assign.
	 *
	 * @throws Nothing.
	 *
	 * @par Exception Safety
	 *      No-throw.
	 */
	void set(param_type n);
	
	/// Get the owner_id value.
	/**
	 * @return The owner_id value.
	 *
	 * @throws Nothing.
	 *
	 * @par Exception Safety
	 *      No-throw.
	 */
	return_type get_owner_id() const;
	
	/// Get clean_up flag in bit 0.
	/**
	 * @return The clean_up flag, in MSB.
	 *
	 * @throws Nothing.
	 *
	 * @par Exception Safety
	 *      No-throw.
	 */
	return_type get_clean_up() const;
	
	/// Mark for clean-up.
	/**
	 * @throws Nothing.
	 *
	 * @par Exception Safety
	 *      No-throw.
	 */
	void mark_for_clean_up();
	
	/// Test if this owner_id is not an owner id (owner_id::none.)
	/**
	 * @return true if this owner_id is not an owner id (owner_id::none.)
	 *
	 * @throws Nothing.
	 *
	 * @par Exception Safety
	 *      No-throw.
	 */
	bool is_no_owner_id() const;
	
	// unary operators
	
	// increment
	
    /// operator++ prefix increment.
 	/**
	 * @return A reference to this owner_id.
	 *
	 * @throws Nothing.
	 *
	 * @par Exception Safety
	 *      No-throw.
	 */
	owner_id& operator++();
	
private:
	volatile value_type value_;
};

} // namespace core
} // namespace starcounter

#include "impl/owner_id.hpp"

#endif // STARCOUNTER_CORE_OWNER_ID_HPP
