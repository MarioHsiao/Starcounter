//
// test.hpp
// Tiny tuple test
//
// Copyright © 2006-2013 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#ifndef STARCOUNTER_CORE_TINY_TUPLE_TEST_HPP
#define STARCOUNTER_CORE_TINY_TUPLE_TEST_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#include <iostream>
#if defined(_MSC_VER) && (_MSC_VER >= 1700)
 #include <cstdint>
#else
# include <tr1/cstdint>
#endif // defined(_MSC_VER) && (_MSC_VER >= 1700)

#include "tiny_tuple.hpp"

namespace starcounter {
namespace core {
namespace tiny_tuple {

/// Exception class.
class test_exception {
public:
	typedef uint32_t error_code_type;
	
	explicit test_exception(error_code_type err)
	: err_(err) {}
	
	error_code_type error_code() const {
		return err_;
	}
	
private:
	error_code_type err_;
};

/// Class test.
/**
 * @throws test_exception when something can not be achieved.
 */
class test {
public:
	/// Construction of the test application.
	/**
	 * @param argc Argument count.
	 * @param argv Argument vector.
	 * @throws starcounter::core::test_exception if the test fails to start.
	 */
	explicit test(int argc, wchar_t* argv[]);

	/// Start the test.
	void run();

private:
	record::data_header::pointer ptr_;
};

} // namespace tiny_tuple
} // namespace core
} // namespace starcounter

#include "impl/test.hpp"

#endif // STARCOUNTER_CORE_TINY_TUPLE_TEST_HPP
