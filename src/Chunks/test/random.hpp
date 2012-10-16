//
// random.hpp
// Network Gateway
//

#ifndef STARCOUNTER_INTERPROCESS_COMMUNICATION_RANDOM_HPP
#define STARCOUNTER_INTERPROCESS_COMMUNICATION_RANDOM_HPP

#include <cstdint>

namespace starcounter {
namespace interprocess_communication {

// Implementation of recommended random generator for everyday use. The period is
// ≈ 1.8 * 10^19. It should not be used by an application that makes more than
// ~ 10^12 calls.
class random_generator {
public:
	/// Default constructor.
	/**
	 * @throws Nothing.
	 *
	 * @par Complexity
	 *      Constant.
	 */
	explicit random_generator(uint64_t seed)
	: v_(4101842887655102017LL) {
		v_ ^= seed;
		v_ = int64();
	}
	
	/// Get 64-bit random integer.
	/**
	 * @return 64-bit random integer.
	 *
	 * @throws Nothing.
	 *
	 * @par Complexity
	 *      Constant.
	 */
	uint64_t int64() {
		v_ ^= v_ >> 21;
		v_ ^= v_ << 35;
		v_ ^= v_ >> 4;
		return v_ * 2685821657736338717LL;
	}
	
	/// Get 32-bit random integer.
	/**
	 * @return 32-bit random integer.
	 *
	 * @throws Nothing.
	 *
	 * @par Complexity
	 *      Constant.
	 */
	uint32_t int32() {
		return static_cast<uint32_t>(int64());
	}
	
	/// Get 16-bit random integer.
	/**
	 * @return 16-bit random integer.
	 *
	 * @throws Nothing.
	 *
	 * @par Complexity
	 *      Constant.
	 */
	uint16_t int16() {
		return static_cast<uint16_t>(int64());
	}
	
	/// Get 8-bit random integer.
	/**
	 * @return 8-bit random integer.
	 *
	 * @throws Nothing.
	 *
	 * @par Complexity
	 *      Constant.
	 */
	uint8_t int8() {
		if (byte_count_--) {
			return static_cast<uint8_t>(byte_register_ >>= 8);
		}
		byte_register_ = int64();
		byte_count_ = 7;
		return static_cast<uint8_t>(byte_register_);
	}
	
	/// Get random double-precision floating value in the range 0 to 1.
	/**
	 * @return double-precision floating value in the range 0 to 1.
	 *
	 * @throws Nothing.
	 *
	 * @par Complexity
	 *      Constant.
	 */
	double doub() {
		return 5.42101086242752217E-20 * int64();
	}
	
private:
	uint64_t v_;
	uint64_t byte_register_;
	int32_t byte_count_;
};

// Implemention of high-quality random hash of an integer into several numeric
// types.
class random_hash {
public:
	/// Get hash of key as a 64-bit integer.
	/**
	 * @param key The 64-bit key to hash.
	 *
	 * @return Hash of key as a 64-bit integer.
	 *
	 * @throws Nothing.
	 *
	 * @par Complexity
	 *      Constant.
	 */
	uint64_t int64(uint64_t key) {
		uint64_t v = key * 3935559000370003845LL +2691343689449507681LL;
		v ^= v >> 21;
		v ^= v << 37;
		v ^= v >> 4;
		v *= 4768777513237032717LL;
		v ^= v << 20;
		v ^= v >> 41;
		v ^= v << 5;
		return v;
	}
	
	/// Get hash of key as a 32-bit integer.
	/**
	 * @param key The 64-bit key to hash.
	 *
	 * @return Hash of key as a 32-bit integer.
	 *
	 * @throws Nothing.
	 *
	 * @par Complexity
	 *      Constant.
	 */
	uint32_t int32(uint64_t key) {
		return static_cast<uint32_t>(int64(key));
	}
	
	/// Get hash of key as a 16-bit integer.
	/**
	 * @param key The 64-bit key to hash.
	 *
	 * @return Hash of key as a 16-bit integer.
	 *
	 * @throws Nothing.
	 *
	 * @par Complexity
	 *      Constant.
	 */
	uint32_t int16(uint64_t key) {
		return static_cast<uint16_t>(int64(key));
	}
	
	/// Get hash of key as a 8-bit integer.
	/**
	 * @param key The 64-bit key to hash.
	 *
	 * @return Hash of key as a 8-bit integer.
	 *
	 * @throws Nothing.
	 *
	 * @par Complexity
	 *      Constant.
	 */
	uint8_t int8(uint64_t key) {
		return static_cast<uint8_t>(int64(key));
	}
	
	/// Get hash of key as a double-precision floating value between 0 to 1.
	/**
	 * @return Hash of key as a double-precision floating value in the range 0
	 *      to 1.
	 *
	 * @throws Nothing.
	 *
	 * @par Complexity
	 *      Constant.
	 */
	double doub(uint64_t key) {
		return 5.42101086242752217E-20 * int64(key);
	}
};

} // namespace interprocess_communication
} // namespace starcounter

#endif // STARCOUNTER_INTERPROCESS_COMMUNICATION_RANDOM_HPP
