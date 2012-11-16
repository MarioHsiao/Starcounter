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

#include <cstdint>

#if defined(_WIN32) || defined(_WIN64)
# if ((defined (_M_IA64) || defined (_M_AMD64)) && !defined(NT_INTEREX))
#  define WIN32_LEAN_AND_MEAN
#  include <windows.h>
#  include <intrin.h>
#  pragma intrinsic (_InterlockedExchange)
#  pragma intrinsic (_InterlockedCompareExchange)
#  undef WIN32_LEAN_AND_MEAN
# endif // ((defined (_M_IA64) || defined (_M_AMD64)) && !defined(NT_INTEREX))
#endif //  defined(_WIN32) || defined(_WIN64)

#include "../common/macro_definitions.hpp"

#if defined(_MSC_VER)
DLL_IMPORT extern uint64_t __stdcall GetTickCount64();
#endif // defined(_MSC_VER)

namespace starcounter {
namespace core {
namespace smp {

#if defined(_WIN32) || defined(_WIN64)
# if ((defined (_M_IA64) || defined (_M_AMD64)) && !defined(NT_INTEREX))

/// class spinlock can be used for SMP synchronization on x86 and x86_64
/// architectures.
class spinlock {
public:
	// Consider typedef long int lock_t;
	typedef volatile long int lock_t;
	typedef std::uint32_t locker_id_type;
	typedef uint64_t milliseconds;

	enum {
		not_locked = 0,
		locked = 1
	};
	
	enum {
		elapsed_time_check = 1024
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

#if 0 // Make it a class if milliseconds can be passed in instead of locker_id_type
	class milliseconds {
	public:
		typedef uint64_t value_type;

		milliseconds(value_type abs_time)
		: abs_time_(abs_time) {}

		operator value_type() const {
			return abs_time_;
		}

	private:
		value_type abs_time_;
	};
#endif // Make it a class if milliseconds can be passed in instead of locker_id_type
	
	/// lock() tries to lock with the locked value (1). Calling lock() means the
	/// caller is anonymous, so the spinlock is not used in robust mode.
	/// Use lock(locker_id_type) if need to use the spinlock in robust mode.
	///
	/// Do not mix calls to lock() or timed_lock(milliseconds) with
	/// lock(locker_id_type) or timed_lock(locker_id_type, milliseconds), on the
	/// same spinlock. That would lead to undefined behavior.
	///
	/// lock() spins forever until it acquires the lock, so this potentially
	/// deadlock the thread. lock() is faster than lock(locker_id_type).
	/**
	 * @return true if the lock is acquired. Otherwise the thread will not
	 *		return.
	 */
	ALWAYS_INLINE bool lock() {
		// Check if someone else is using the spinlock in robust mode.
		_ASSERT(lock_ == locked || lock_ == not_locked);

		do {
			if (_InterlockedExchange((LPLONG) &lock_, locked) == not_locked) {
				return true;
			}
			while (lock_ != not_locked) {
				_mm_pause();
			}
		} while (true);
	}

	/// lock(locker_id_type) tries to lock with the locker_id value, which
	/// should be a unique id number among all processes that accesses the
	/// spinlock. Thereby the spinlock is in robust mode.
	///
	/// A robust spinlock means that it can be unlocked in a safe way, if a
	/// process terminates and leaves the spinlock in the locked state. 
	/// Use lock() if not need to use the spinlock in robust mode.
	///
	/// Do not mix calls to lock() or timed_lock(milliseconds) with
	/// lock(locker_id_type) or timed_lock(locker_id_type, milliseconds), on the
	/// same spinlock. That would lead to undefined behavior.
	///
	/// lock(locker_id_type) spins forever until it acquires the lock, so this
	/// potentially deadlock the thread. lock(locker_id_type) is slower than
	/// lock().
	/**
	 * @param locker_id The value to is used as an id for locking the spinlock.
	 *		It must not be not_locked (0).
	 * @return true if the lock is acquired. Otherwise the thread will not
	 *		return.
	 */
	ALWAYS_INLINE bool lock(locker_id_type locker_id) {
		_ASSERT(locker_id != not_locked);

		do {
			if (_InterlockedCompareExchange((LPLONG) &lock_, locker_id,
			not_locked) == not_locked) {
				return true;
			}
			while (lock_ != not_locked) {
				_mm_pause();
			}
		} while (true);
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

	/// try_lock(locker_id_type) tries to acquire the lock but will not spin.
	/**
	 * @return true if acquired the lock, otherwise false.
	 */
	ALWAYS_INLINE bool try_lock(locker_id_type locker_id) {
		long int lock = _InterlockedCompareExchange((LPLONG) &lock_, locker_id,
		not_locked);
		
		_mm_mfence();
		return lock == not_locked;
	}

	/// timed_lock() spins until it acquires the lock or a timeout occurs.
	/**
	 * @param abs_timeout Is an absolute time code.
	 * @return true if acquired the lock, or false if a timeout occurs.
	 */
	ALWAYS_INLINE bool timed_lock(milliseconds abs_timeout) {
		if (_InterlockedExchange((LPLONG) &lock_, locked) == not_locked) {
			// The lock is acquired.
			return true;
		}
		
		abs_timeout += GetTickCount64();
		unsigned int count = 0;

		do {
			if (_InterlockedExchange((LPLONG) &lock_, locked) == not_locked) {
				// The lock is acquired.
				return true;
			}

			while (lock_ != not_locked) {
				if (++count != elapsed_time_check) {
					_mm_pause();
					continue;
				}

				if (abs_timeout > GetTickCount64()) {
					SwitchToThread();
					count = 0;
					continue;
				}
				else {
					// The lock is not acquired. A timeout occurred.
					return false;
				}
			}
		} while (true);
	}

	/// unlock() releases the lock.
	void unlock() {
		_mm_mfence();
		lock_ = not_locked;
		// if typedef long int lock_t:
		//*const_cast<long int volatile*>(lock_) = not_locked;
	}

	/// unlock_if() releases the lock if and only if locked with the locker_id.
	/// This is used by the IPC monitor to force unlocking of a spinlock if
	/// a terminated process left it in a locked state with the locker_id.
	bool unlock_if(locker_id_type locker_id) {
		if (locker_id == lock_) {
			_mm_mfence();
			lock_ = not_locked;
			// if typedef long int lock_t:
			//*const_cast<long int volatile*>(lock_) = not_locked;
			
			// The lock was locked with locker_id. The lock was ulocked.
			return true;
		}

		// The lock was not locked with locker_id. The lock was not ulocked.
		return false;
	}

	ALWAYS_INLINE locker_id_type get_lock_value() const {
		_mm_mfence();
		return lock_;
	}

	ALWAYS_INLINE bool is_locked() const {
		_mm_mfence();
		return lock_ != not_locked;
	}
	
	/// class scoped_lock locks the spinlock using lock(), and will unlock the
	/// spinlock when the object goes out of scope.
	class scoped_lock {
	public:
        // Exception class.
		class lock_exception {};

        // Tag type to express using try_lock().
	    class try_to_lock_type {};

		/// Constructor tries to acquire the lock using spinlock::lock().
		/// The thread will not return otherwise so this potentially deadlock
		/// the thread.
		/**
		 * @param spinlock A reference to a spinlock.
		 */
		explicit scoped_lock(spinlock& spinlock)
		: spinlock_(spinlock), locked_(spinlock.lock()) {}

		/// Constructor tries to acquire the lock using spinlock::try_lock().
		/// If the lock was acquired, calling owns() returns true, false otherwise.
		/**
		 * @param spinlock A reference to a spinlock.
		 * @param try_to_lock_type A tag to express using try_lock().
		 */
		explicit scoped_lock(spinlock& spinlock, try_to_lock_type)
		: spinlock_(spinlock), locked_(spinlock_.try_lock()) {}

		/// Constructor tries to acquire the lock using spinlock::timed_lock().
		/// If the lock was acquired, calling owns() returns true, false otherwise.
		/**
		 * @param spinlock A reference to a spinlock.
		 * @param abs_timeout An absolute time code expressing the timeout in
         *      milliseconds.
		 */
        explicit scoped_lock(spinlock& spinlock, milliseconds abs_timeout)
        : spinlock_(spinlock), locked_(spinlock_.timed_lock(abs_timeout)) {}
		
		/// try_lock() tries to acquire the lock without spinning.
		/// If the lock was acquired, calling owns() returns true, false otherwise.
		/**
		 * @return true if the lock was acquired, false otherwise.
		 * @throws lock_exception if already locked.
		 */
        bool try_lock() {
			if (!locked_) {
				return locked_ = spinlock_.try_lock();
			}
			else {
				throw lock_exception();
			}
		}

		/// unlock() releases the lock using spinlock::unlock() before the
		/// object goes out of scope.
		void unlock() {
			spinlock_.unlock();
			locked_ = false;
		}

		/// Destructor releases the lock using spinlock::unlock() when the
		/// object goes out of scope.
		~scoped_lock() {
			unlock();
		}

		/// owns() return true if this scoped_lock has acquired the referenced
		/// mutex.
		/**
		 * @return true if this scoped_lock has acquired the referenced mutex.
		 */
		bool owns() const {
			return locked_;
		}

	private:
		scoped_lock(const scoped_lock&);
		scoped_lock& operator=(const scoped_lock&);
		spinlock& spinlock_;
		bool locked_;
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
