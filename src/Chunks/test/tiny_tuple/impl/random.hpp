//
// impl/random.hpp
//

#ifndef STARCOUNTER_CORE_IMPL_RANDOM_HPP
#define STARCOUNTER_CORE_IMPL_RANDOM_HPP

namespace starcounter {
namespace core {

inline random::random(uint64_t seed)
: v_(4101842887655102017LL) {
	v_ ^= seed;
	v_ = int64();
}
	
inline uint64_t random::int64() {
	v_ ^= v_ >> 21;
	v_ ^= v_ << 35;
	v_ ^= v_ >> 4;
	return v_ * 2685821657736338717LL;
}

} // namespace core
} // namespace starcounter

#endif // STARCOUNTER_CORE_IMPL_RANDOM_HPP
