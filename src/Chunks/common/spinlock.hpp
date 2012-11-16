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
//extern "C" unsigned long long __stdcall GetTickCount64();
#  include <intrin.h>
#  pragma intrinsic (_InterlockedExchange)
#  pragma intrinsic (_InterlockedCompareExchange)
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
	// Consider typedef long int lock_t;
	typedef volatile long int lock_t;
	typedef std::uint32_t locker_id_type;
	typedef uint64_t milliseconds_type;
	
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

	/// This version of lock will lock with the locked value (1) and should only
	/// be used when the spinlock is not placed in shared memory and only shared
	/// between threads in the same process. If the spinlock is placed in a
	/// shared memory segment synchronizing threads in different processes, then
	/// use lock(locker_id_type) instead.
	/// lock() don't use a pause instruction in the spin loop while trying to
	/// acquire the lock. This can give better or worse performance compared to
	/// lock_with_pause() depending on the contention.
	/// NOTE: Spins forever until it acquires the lock, so this potentially
	/// deadlock the thread. Use timed_lock() to reduce this risk.
	ALWAYS_INLINE bool lock() {
		do {
			if (_InterlockedExchange((LPLONG) &lock_, locked) == not_locked) {
				return true; // The original version returned void.
			}
			while (lock_ != not_locked) {
				// No pause.
			}
		} while (true);
	}

	/// This version of lock will lock with the locker_id value, which must not
	/// be 0 (not verified.) This version can be used when the spinlock is
	/// placed in a shared memory segment, shared between threads in different
	/// processes. By supplying the locker_id (owner_id of the process) the
	/// spinlock becomes robust since it can be unlocked by another process
	/// (the IPC monitor) in case the thread holding the lock terminates.
	/// lock() don't use a pause instruction in the spin loop while trying to
	/// acquire the lock. This can give better or worse performance compared to
	/// lock_with_pause() depending on the contention.
	/// NOTE: Spins forever until it acquires the lock, so this potentially
	/// deadlock the thread. Use timed_lock() to reduce this risk.
	/**
	 * @param locker_id The uint32_t value to is used as id for locking the
	 *		spinlock. It must not be not_locked (0). This is not verified.
	 * @return true if the thread acquires the lock. The thread will not return
	 *		otherwise.
	 */
	ALWAYS_INLINE bool lock(locker_id_type locker_id) {
		do {
			if (_InterlockedCompareExchange((LPLONG) &lock_, locker_id,
			not_locked) == not_locked) {
				return true;
			}
			while (lock_ != not_locked) {
				std::cout << "lock(" << locker_id << "): did not get the lock\n"; Sleep(500);
				// No pause.
			}
			std::cout << "lock(" << locker_id << "): trying again to get the lock\n";
		} while (true);
	}

	ALWAYS_INLINE locker_id_type get_lock_value() const {
		return lock_;
	}

	ALWAYS_INLINE bool is_locked() const {
		return lock_ != not_locked;
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
		// Not long int lock = _InterlockedExchange((LPLONG) &lock_, locked);
		long int lock = _InterlockedCompareExchange((LPLONG) &lock_, locker_id,
		not_locked);
		
		_mm_mfence();
		return lock == not_locked;
	}

	/// timed_lock() don't use a pause instruction in the spin loop while trying to
	/// acquire the lock. This can give better or worse performance compared to
	/// timed_lock_with_pause() depending on the contention.
	/// Spins until it acquires the lock or a timeout occurs.
	/**
	 * @param timeout is absolute milliseconds timeout.
	 *		NOTE: abs_timeout is an absolute time code.
	 */
	ALWAYS_INLINE bool timed_lock(milliseconds_type abs_timeout) {
		// As an optimization, first try to acquire the lock without calling
		// GetTickCount64().
		if (_InterlockedExchange((LPLONG) &lock_, locked) == not_locked) {
			return true;
		}
		
		//abs_timeout += GetTickCount64(); // Correct - but does not compile.
		abs_timeout += GetTickCount(); // Wrong - wraps after 2^32 ms.
		unsigned int count = 0;

		do {
			if (_InterlockedExchange((LPLONG) &lock_, locked) == not_locked) {
				return true;
			}

			while (lock_ != not_locked) {
				if (++count != elapsed_time_check) {
					// No pause.
					continue;
				}

				//if (abs_timeout > GetTickCount64()) { // Correct - but does not compile.
				if (abs_timeout > GetTickCount()) { // Wrong - wraps after 2^32 ms.
					SwitchToThread();
					count = 0;
					continue;
				}
				else {
					// A timeout occurred.
					return false;
				}
			}
		} while (true);
	}

	/// timed_lock_with_pause() use a pause instruction in the spin loop while trying to
	/// acquire the lock. This can give better or worse performance compared to
	/// timed_lock() depending on the contention.
	/// Spins until it acquires the lock or a timeout occurs.
	/**
	 * @param timeout is absolute milliseconds timeout.
	 *		NOTE: abs_timeout is an absolute time code.
	 */
	ALWAYS_INLINE bool timed_lock_with_pause(milliseconds_type abs_timeout) {
		// As an optimization, first try to acquire the lock without calling
		// GetTickCount64().
		if (_InterlockedExchange((LPLONG) &lock_, locked) == not_locked) {
			return true;
		}
		
		//abs_timeout += GetTickCount64(); // Correct - but does not compile.
		abs_timeout += GetTickCount(); // Wrong - wraps after 2^32 ms.
		unsigned int count = 0;

		do {
			if (_InterlockedExchange((LPLONG) &lock_, locked) == not_locked) {
				return true;
			}

			while (lock_ != not_locked) {
				if (++count != elapsed_time_check) {
					_mm_pause();
					continue;
				}

				//if (abs_timeout > GetTickCount64()) { // Correct - but does not compile.
				if (abs_timeout > GetTickCount()) { // Wrong - wraps after 2^32 ms.
					SwitchToThread();
					count = 0;
					continue;
				}
				else {
					// A timeout occurred.
					return false;
				}
			}
		} while (true);
	}

#if 0
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
#endif
	
	/// unlock() releases the lock.
	void unlock() {
		_mm_mfence(); // _mm_mfence() or _mm_sfence()?
		lock_ = not_locked;
		// if typedef long int lock_t:
		//*const_cast<long int volatile*>(lock_) = not_locked;
	}

#if 0 /// Ideas from Boost Interprocess API
	class scoped_lock {
	public:
		scoped_lock();
		scoped_lock(mutex_type&, const boost::posix_time::ptime&);
		~scoped_lock();

		void lock();
		bool try_lock();
		bool timed_lock(const boost::posix_time::ptime&);
		void unlock();
		bool owns() const;
	};
#endif /// Ideas from Boost Interprocess API
	
	/// class scoped_lock locks the spinlock using lock(), and will unlock the
	/// spinlock when the object goes out of scope.
	class scoped_lock {
	public:
		/// Constructor acquires the lock using spinlock::lock().
		/**
		 * @param spinlock A reference to a spinlock.
		 */
		explicit scoped_lock(spinlock& spinlock)
		: spinlock_(spinlock), locked_(spinlock.lock()) {}

#if 0
		/// Effects: m.try_lock(). 
		//!Postconditions: mutex() == &m. owns() == the return value of the
		//!   m.try_lock() executed within the constructor.
		//!Notes: The constructor will take ownership of the mutex if it can do
		//!   so without waiting. If the mutex_type does not support try_lock,
		//!   this constructor will fail at compile time if instantiated, but otherwise
		//!   have no effect.
		explicit scoped_lock(spinlock& spinlock, try_to_lock_type)
		: spinlock_(spinlock), locked_(spinlock_.try_lock()) {}
#endif
#if 0
		bool try_lock() {
			if(!locked_) {
				return locked_ = spinlock_.try_lock();
			}
			else {
				throw lock_exception();
			}
		}
#endif
//------------------------------------------------------------------------------
		/// Constructor acquires the lock using spinlock.timed_lock(abs_time). 
		//!Postconditions: mutex() == &m. owns() == the return value of the
		//!   m.timed_lock(abs_time) executed within the constructor.
		//!Notes: The constructor will take ownership of the mutex if it can do
		//!   it until abs_time is reached. Whether or not this constructor
		//!   handles recursive locking depends upon the mutex. If the mutex_type
		//!   does not support try_lock, this constructor will fail at compile
		//!   time if instantiated, but otherwise have no effect.
		//explicit scoped_lock(spinlock& spinlock, const boost::posix_time::ptime& abs_time)
		//: spinlock_(spinlock), locked_(spinlock_->timed_lock(abs_time)) {}

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

//------------------------------------------------------------------------------
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

		/// owns() return true if this scoped_lock has acquired the referenced
		/// mutex.
		/**
		 * @return true if this scoped_lock has acquired the referenced mutex.
		 */
		bool owns() const {
			return locked_;
		}

	private:
		scoped_lock_with_pause(const scoped_lock_with_pause&);
		scoped_lock_with_pause& operator=(const scoped_lock_with_pause&);
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
