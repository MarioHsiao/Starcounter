//
// impl/test.hpp
// Tiny tuple test
//
// Copyright © 2006-2013 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//
// Implementation of class test.
//

#ifndef STARCOUNTER_CORE_TINY_TUPLE_IMPL_TEST_HPP
#define STARCOUNTER_CORE_TINY_TUPLE_IMPL_TEST_HPP

// Implementation

namespace starcounter {
namespace core {
namespace tiny_tuple {

inline test::test(int argc, wchar_t* argv[])
: ptr_() {
	std::cout << "test::test()" << std::endl;
}

inline void test::run() {
	std::cout << "test::run()" << std::endl;
}

} // namespace tiny_tuple
} // namespace core
} // namespace starcounter

#endif // STARCOUNTER_CORE_TINY_TUPLE_IMPL_TEST_HPP
