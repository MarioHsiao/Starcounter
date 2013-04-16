//
// random.hpp
//

#ifndef STARCOUNTER_CORE_RANDOM_HPP
#define STARCOUNTER_CORE_RANDOM_HPP

#include <tr1/cstdint>

namespace starcounter {
namespace core {

class random {
public:
	/// random() constructs the generator.
	/**
	 * @param seed The seed to initialize the generator with.
	 */
	explicit random(uint64_t seed);
	
	/// int64() returns a 64-bit pseudo random integer.
	/**
	 * @return a 64-bit pseudo random integer.
	 */
	uint64_t int64();
	
private:
	uint64_t v_;
};

} // namespace core
} // namespace starcounter

#include "impl/random.hpp"

#endif // STARCOUNTER_CORE_RANDOM_HPP
