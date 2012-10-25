//
// spinlock.hpp
// For SMP synchronization on x86 and x86_64 architectures.
// Currently only Windows is supported.
//
// Copyright © 2006-2012 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#ifndef STARCOUNTER_CORE_SMP_SPINLOCK_HPP
#define STARCOUNTER_CORE_SMP_SPINLOCK_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1020)

#if defined(_WIN32) || defined(_WIN64)
# if ((defined (_M_IA64) || defined (_M_AMD64)) && !defined(NT_INTEREX))
#  define WIN32_LEAN_AND_MEAN
#  include <windows.h>
#  include <intrin.h>
#  pragma intrinsic (_InterlockedExchange)
#  undef WIN32_LEAN_AND_MEAN
# endif // ((defined (_M_IA64) || defined (_M_AMD64)) && !defined(NT_INTEREX))
#endif //  defined(_WIN32) || defined(_WIN64)

#include "../common/macro_definitions.hpp"

namespace starcounter {
namespace core {
namespace smp {

#if defined(_WIN32) || defined(_WIN64)
# if ((defined (_M_IA64) || defined (_M_AMD64)) && !defined(NT_INTEREX))

/// class spinlock can be used for SMP synchronization on x86 and x86_64
/// architectures.
class spinlock {
public:
	typedef volatile long int lock_t;
	// Consider typedef long int lock_t;

	enum {
		not_locked = 0,
		locked = 1
	};
	
	spinlock() {
		init();
	}

	~spinlock() {
		unlock();
	}

	void init() {
		lock_ = not_locked;
	}

	/// try_lock() tries to acquire the lock but will not spin.
	/**
	 * @return true if acquired the lock, otherwise false.
	 */
	ALWAYS_INLINE bool try_lock() {
		long int lock = _InterlockedExchange((LPLONG) &lock_, locked);
		_mm_mfence();
		return lock == not_locked;
	}

	/// lock() don't use a pause instruction in the spin loop while trying to
	/// acquire the lock. This can give better or worse performance compared to
	/// lock_with_pause() depending on the contention.
	/// NOTE: Spins forever until it acquires the lock, so this potentially
	/// deadlock the thread.
	ALWAYS_INLINE void lock() {
		do {
			if (_InterlockedExchange((LPLONG) &lock_, locked) == not_locked) {
				return;
			}
			while (lock_ != not_locked) {
				// No pause.
			}
		} while (true);
	}

	/// lock_with_pause() use a pause instruction in the spin loop while trying
	/// to acquire the lock. This can give better or worse performance compared
	/// to lock() depending on the contention.
	/// NOTE: Spins forever until it obtains the lock, so this potentially
	/// deadlock the thread.
	ALWAYS_INLINE void lock_with_pause() {
		do {
			if (_InterlockedExchange((LPLONG) &lock_, locked) == not_locked) {
				return;
			}
			while (lock_ != not_locked) {
				_mm_pause();
			}
		} while (true);
	}

	/// unlock() releases the lock.
	void unlock() {
		_mm_mfence(); // _mm_mfence() or _mm_sfence()?
		lock_ = not_locked;
		// if typedef long int lock_t:
		//*const_cast<long int volatile*>(lock_) = not_locked;
	}

	/// class scoped_lock locks the spinlock using lock(), and will unlock the
	/// spinlock when the object goes out of scope.
	class scoped_lock {
	public:
		/// Constructor acquires the lock using spinlock::lock().
		/**
		 * @param spinlock A reference to a spinlock.
		 */
		explicit scoped_lock(spinlock& spinlock)
		: spinlock_(spinlock) {
			spinlock.lock();
		}

		/// unlock() releases the lock using spinlock::unlock() before the
		/// object goes out of scope.
		void unlock() {
			spinlock_.unlock();
		}

		/// Destructor releases the lock using spinlock::unlock() when the
		/// object goes out of scope.
		~scoped_lock() {
			unlock();
		}

	private:
		scoped_lock(const scoped_lock&);
		scoped_lock& operator=(const scoped_lock&);
		spinlock& spinlock_;
	};

	/// class scoped_lock_with_pause locks the spinlock using
	/// spinlock::lock_with_pause(), and will unlock the spinlock when the
	/// object goes out of scope.
	class scoped_lock_with_pause {
	public:
		/// Constructor acquires the lock using spinlock::lock_with_pause().
		/**
		 * @param spinlock A reference to a spinlock.
		 */
		explicit scoped_lock_with_pause(spinlock& spinlock)
		: spinlock_(spinlock) {
			spinlock.lock();
		}

		/// unlock() releases the lock using spinlock::unlock() before the
		/// object goes out of scope.
		void unlock() {
			spinlock_.unlock();
		}

		/// Destructor releases the lock using spinlock::unlock() when the
		/// object goes out of scope.
		~scoped_lock_with_pause() {
			unlock();
		}

	private:
		scoped_lock_with_pause(const scoped_lock_with_pause&);
		scoped_lock_with_pause& operator=(const scoped_lock_with_pause&);
		spinlock& spinlock_;
	};

private:
	lock_t lock_;
};

# endif // ((defined (_M_IA64) || defined (_M_AMD64)) && !defined(NT_INTEREX))
#endif //  defined(_WIN32) || defined(_WIN64)

} // namespace smp
} // namespace core
} // namespace starcounter

//#include "impl/spinlock.hpp"

#endif // STARCOUNTER_CORE_SMP_SPINLOCK_HPP
