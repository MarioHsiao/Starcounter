//
// impl/monitor_interface.hpp
//
// Copyright © 2006-2012 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//
// Implementation of class monitor_interface.
//

#ifndef STARCOUNTER_CORE_IMPL_MONITOR_INTERFACE_HPP
#define STARCOUNTER_CORE_IMPL_MONITOR_INTERFACE_HPP

// Implementation

namespace starcounter {
namespace core {

inline monitor_interface::in::in()
: data_available_(false) {}

inline monitor_interface::out::out()
: data_available_(false) {}

#if 0 /// TODO: Figure how to bind this.
inline bool monitor_interface::in::is_data_available() const {
	return data_available_;
}

inline bool monitor_interface::out::is_data_available() const {
	return data_available_;
}
#endif /// TODO: Figure how to bind this.

inline monitor_interface::monitor_interface()
: in_(), out_(), is_ready_flag_(false) {}

inline void monitor_interface::wait_until_ready() {
	boost::interprocess::scoped_lock<boost::interprocess
	::interprocess_mutex> lock(ready_mutex_);
	is_ready_.wait(lock, boost::bind(&monitor_interface
	::is_ready, this));
}

inline void monitor_interface::is_ready_notify_all() {
	boost::interprocess::scoped_lock<boost::interprocess
	::interprocess_mutex> lock(ready_mutex_);
	is_ready_flag_ = true;
	lock.unlock();
	is_ready_.notify_all();
}

inline bool monitor_interface::is_ready() const {
	return is_ready_flag_;
}

inline void monitor_interface::set_in_data_available_state(bool state) {
	in_.data_available_ = state;
}

inline void monitor_interface::set_out_data_available_state(bool state) {
	out_.data_available_ = state;
}

/// TODO: Remove when I have figured how to bind.
inline bool monitor_interface::in_is_data_available() const {
	return in_.data_available_;
}

/// TODO: Remove when I have figured how to bind.
inline bool monitor_interface::out_is_data_available() const {
	return out_.data_available_;
}

inline uint32_t monitor_interface::register_database_process(pid_type pid, std::string
segment_name, owner_id& oid, uint32_t timeout_milliseconds) {
	// The timeout is used multiple times below, while time passes, so all
	// synchronization must be completed before the timeout_milliseconds time
	// period has elapsed.
	const boost::system_time timeout = boost::posix_time::microsec_clock
	::universal_time() +boost::posix_time::milliseconds(timeout_milliseconds);
	
	// The calling database thread must acquire the monitor_interface_lock, to
	// ensure that only one thread except the monitor thread gets access to the
	// monitor_interface. The monitor never locks the monitor_interface_lock.
	boost::interprocess::scoped_lock<boost::interprocess::interprocess_mutex>
	monitor_interface_lock(monitor_interface_mutex_, timeout);
	
	if (!monitor_interface_lock.owns()) {
		// The timeout_milliseconds time period has elapsed. Failed to register
		// the database process.
		oid = owner_id::none;
		return SCERRDBACQUIREOWNERIDTIMEOUT;
	}
	
	{
		// This lock synchronizes access to the monitor_interface between this
		// calling database thread and the monitor wait_for_registration thread.
		boost::interprocess::scoped_lock<boost::interprocess
		::interprocess_mutex> lock(in_.mutex_, timeout);
		
		if (!lock.owns()) {
			// The timeout_milliseconds time period has elapsed. Failed to
			// register the database process.
			oid = owner_id::none;
			return SCERRDBACQUIREOWNERIDTIMEOUT;
		}
		
		set_pid(pid);
		set_segment_name(segment_name);
		set_process_type(database_process);
		set_operation(registration_request);
		set_in_data_available_state(true);
	}
	
	// This thread is holding the monitor_interface_lock so no other thread can
	// access in_ and out_. Notify the monitor wait_for_registration thread.
	in_.data_is_available_.notify_one();
	
	{
		// This lock synchronizes access to the monitor_interface between this
		// calling database thread and the monitor thread.
		boost::interprocess::scoped_lock<boost::interprocess
		::interprocess_mutex> lock(out_.mutex_, timeout);
		
		if (!lock.owns()) {
			// The timeout_milliseconds time period has elapsed. Failed to
			// register the database process.
			oid = owner_id::none;
			return SCERRDBACQUIREOWNERIDTIMEOUT;
		}
		
		// Wait until out data is available, or timeout occurs.
		if (out_.data_is_available_.timed_wait(lock, timeout,
		boost::bind(&monitor_interface::out_is_data_available, this)) == true) {
			// Now out data is available.
			oid = get_owner_id();
			set_out_data_available_state(false);
		}
		else {
			// The timeout_milliseconds time period has elapsed. Failed to
			// register the database process.
			oid = owner_id::none;
			/// TODO: Here is a bug, because:
			set_out_data_available_state(false);
			/// is not properly synchronized, since out_.mutex_ is not locked!
			/// Setting it to false anyway decreases the probablility of the
			/// bug to appear. Eventually the state is set to false, but if
			/// another thread registers while the state is still true,
			/// that thread will get bad registration data. A bad bug.
			return SCERRDBACQUIREOWNERIDTIMEOUT;
		}
	}
	
	// Check if the owner_id is invalid.
	if (get_owner_id() == owner_id::none) {
		// Failed to acquire an owner_id.
		return SCERRDBACQUIREOWNERID;
	}
	
	// Successfully registered the database process.
	return 0;
}

inline uint32_t monitor_interface::register_client_process(pid_type pid, owner_id& oid,
uint32_t timeout_milliseconds) {
	// The timeout is used multiple times below, while time passes, so all
	// synchronization must be completed before the timeout_milliseconds time
	// period has elapsed.
	const boost::system_time timeout = boost::posix_time::microsec_clock
	::universal_time() +boost::posix_time::milliseconds(timeout_milliseconds);
	
	// The calling client thread must acquire the monitor_interface_lock, to
	// ensure that only one thread except the monitor thread gets access to the
	// monitor_interface. The monitor never locks the monitor_interface_lock.
	boost::interprocess::scoped_lock<boost::interprocess::interprocess_mutex>
	monitor_interface_lock(monitor_interface_mutex_, timeout);

	if (!monitor_interface_lock.owns()) {
		// The timeout_milliseconds time period has elapsed. Failed to register
		// the client process.
		oid = owner_id::none;
		return SCERRCACQUIREOWNERIDTIMEOUT;
	}
	
	{
		// This lock synchronizes access to the monitor_interface between this
		// calling client thread and the monitor thread.
		boost::interprocess::scoped_lock<boost::interprocess
		::interprocess_mutex> lock(in_.mutex_, timeout);

		if (!lock.owns()) {
			// A timeout occurred. Failed to register the client process.
			oid = owner_id::none;
			return SCERRCACQUIREOWNERIDTIMEOUT2;
		}

		set_pid(pid);
		set_segment_name("");
		set_process_type(client_process);
		set_operation(registration_request);
		set_in_data_available_state(true);
	}

	// This thread is holding the monitor_interface_lock so no other process can
	// access in_ and out_. Notify the monitor wait_for_registration thread.
	in_.data_is_available_.notify_one();
	
	{
		// This lock synchronizes access to the monitor_interface between this
		// calling client thread and the monitor thread.
		boost::interprocess::scoped_lock<boost::interprocess
		::interprocess_mutex> lock(out_.mutex_, timeout);
		
		if (!lock.owns()) {
			// The timeout_milliseconds time period has elapsed. Failed to
			// register the client process.
			oid = owner_id::none;
			return SCERRCACQUIREOWNERIDTIMEOUT;
		}

		// Wait until out data is available, or timeout occurs.
		if (out_.data_is_available_.timed_wait(lock, timeout,
		boost::bind(&monitor_interface::out_is_data_available, this)) == true) {
			// Now out data is available.
			oid = get_owner_id();
			set_out_data_available_state(false);
		}
		else {
			// The timeout_milliseconds time period has elapsed. Failed to
			// register the client process.
			oid = owner_id::none;
			/// TODO: Here is a bug, because:
			set_out_data_available_state(false);
			/// is not properly synchronized, since out_.mutex_ is not locked!
			/// Setting it to false anyway decreases the probablility of the
			/// bug to appear. Eventually the state is set to false, but if
			/// another thread registers while the state is still true,
			/// that thread will get bad registration data. A bad bug.
			return SCERRCACQUIREOWNERIDTIMEOUT;
		}
	}

	// Check if the owner_id is invalid.
	if (get_owner_id() == owner_id::none) {
		// Failed to acquire an owner_id.
		return SCERRCACQUIREOWNERID;
	}
	
	// Successfully registered the client process.
	return 0;
}

inline uint32_t monitor_interface::unregister_database_process(pid_type pid, owner_id&
oid, uint32_t timeout_milliseconds) {
	// The timeout is used multiple times below, while time passes, so all
	// synchronization must be completed before the timeout_milliseconds time
	// period has elapsed.
	const boost::system_time timeout = boost::posix_time::microsec_clock
	::universal_time() +boost::posix_time::milliseconds(timeout_milliseconds);

	// The calling database thread must acquire the monitor_interface_lock, to
	// ensure that only one thread except the monitor thread gets access to the
	// monitor_interface. The monitor never locks the monitor_interface_lock.
	boost::interprocess::scoped_lock<boost::interprocess::interprocess_mutex>
	monitor_interface_lock(monitor_interface_mutex_, timeout);

	if (!monitor_interface_lock.owns()) {
		// The timeout_milliseconds time period has elapsed. Failed to
		// unregister the database process.
		oid = owner_id::none;
		return SCERRDBRELEASEOWNERIDTIMEOUT;
	}

	{
		// This lock synchronizes access to the monitor_interface between this
		// calling database thread and the monitor thread.
		boost::interprocess::scoped_lock<boost::interprocess
		::interprocess_mutex> lock(in_.mutex_, timeout);

		if (!lock.owns()) {
			// A timeout occurred. Failed to unregister the database process.
			oid = owner_id::none;
			return SCERRDBRELEASEOWNERIDTIMEOUT;
		}

		set_pid(pid);
		set_owner_id(oid);
		set_process_type(database_process);
		set_operation(unregistration_request);
		set_in_data_available_state(true);
	}

	// This thread is holding the monitor_interface_lock so no other process can
	// access in_ and out_. Notify the monitor wait_for_registration thread.
	in_.data_is_available_.notify_one();

	{
		// This lock synchronizes access to the monitor_interface between this
		// calling database thread and the monitor thread.
		boost::interprocess::scoped_lock<boost::interprocess
		::interprocess_mutex> lock(out_.mutex_, timeout);
		
		if (!lock.owns()) {
			// The timeout_milliseconds time period has elapsed. Failed to
			// unregister the database process.
			oid = owner_id::none;
			return SCERRDBRELEASEOWNERIDTIMEOUT;
		}

		// Wait until out data is available, or timeout occurs.
		if (out_.data_is_available_.timed_wait(lock, timeout,
		boost::bind(&monitor_interface::out_is_data_available, this)) == true) {
			// Now out data is available.
			oid = get_owner_id();
			set_out_data_available_state(false);
		}
		else {
			// The timeout_milliseconds time period has elapsed. Failed to
			// unregister the database process.
			oid = owner_id::none;
			/// TODO: Here is a bug, because:
			set_out_data_available_state(false);
			/// is not properly synchronized, since out_.mutex_ is not locked!
			/// Setting it to false anyway decreases the probablility of the
			/// bug to appear. Eventually the state is set to false, but if
			/// another thread registers while the state is still true,
			/// that thread will get bad registration data. A bad bug.
			return SCERRDBRELEASEOWNERIDTIMEOUT;
		}
	}

	// Check that the owner_id is invalid.
	if (get_owner_id() != owner_id::none) {
		// Failed to release the owner_id.
		return SCERRDBRELEASEOWNERID;
	}
	
	// Successfully unregistered the database process.
	return 0;
}

inline uint32_t monitor_interface::unregister_client_process(pid_type pid, owner_id&
oid, uint32_t timeout_milliseconds) {
	// The timeout is used multiple times below, while time passes, so all
	// synchronization must be completed before the timeout_milliseconds time
	// period has elapsed.
	const boost::system_time timeout = boost::posix_time::microsec_clock
	::universal_time() +boost::posix_time::milliseconds(timeout_milliseconds);

	// The calling client thread must acquire the monitor_interface_lock, to
	// ensure that only one thread except the monitor thread gets access to the
	// monitor_interface. The monitor never locks the monitor_interface_lock.
	boost::interprocess::scoped_lock<boost::interprocess::interprocess_mutex>
	monitor_interface_lock(monitor_interface_mutex_, timeout);

	if (!monitor_interface_lock.owns()) {
		// The timeout_milliseconds time period has elapsed. Failed to
		// unregister the client process.
		oid = owner_id::none;
		return SCERRCRELEASEOWNERIDTIMEOUT;
	}

	{
		// This lock synchronizes access to the monitor_interface between this
		// calling client thread and the monitor thread.
		boost::interprocess::scoped_lock<boost::interprocess
		::interprocess_mutex> lock(in_.mutex_, timeout);

		if (!lock.owns()) {
			// A timeout occurred. Failed to unregister the client process.
			oid = owner_id::none;
			return SCERRCRELEASEOWNERIDTIMEOUT;
		}

		set_pid(pid);
		set_owner_id(oid);
		set_process_type(client_process);
		set_operation(unregistration_request);
		set_in_data_available_state(true);
	}

	// This thread is holding the monitor_interface_lock so no other process can
	// access in_ and out_. Notify the monitor wait_for_registration thread.
	in_.data_is_available_.notify_one();
	
	{
		// This lock synchronizes access to the monitor_interface between this
		// calling client thread and the monitor thread.
		boost::interprocess::scoped_lock<boost::interprocess
		::interprocess_mutex> lock(out_.mutex_, timeout);

		if (!lock.owns()) {
			// The timeout_milliseconds time period has elapsed. Failed to
			// unregister the client process.
			oid = owner_id::none;
			return SCERRCRELEASEOWNERIDTIMEOUT;
		}

		// Wait until out data is available, or timeout occurs.
		if (out_.data_is_available_.timed_wait(lock, timeout,
		boost::bind(&monitor_interface::out_is_data_available, this)) == true) {
			// Now out data is available.
			oid = get_owner_id();
			set_out_data_available_state(false);
		}
		else {
			// The timeout_milliseconds time period has elapsed. Failed to
			// unregister the client process.
			oid = owner_id::none;
			/// TODO: Here is a bug, because:
			set_out_data_available_state(false);
			/// is not properly synchronized, since out_.mutex_ is not locked!
			/// Setting it to false anyway decreases the probablility of the
			/// bug to appear. Eventually the state is set to false, but if
			/// another thread registers while the state is still true,
			/// that thread will get bad registration data. A bad bug.
			return SCERRCRELEASEOWNERIDTIMEOUT;
		}
	}
	
	// Check that the owner_id is invalid.
	if (get_owner_id() != owner_id::none) {
		// Failed to release the owner_id.
		return SCERRCRELEASEOWNERID;
	}
	
	// Successfully unregistered the client process.
	return 0;
}

inline void monitor_interface::wait_for_registration() {
	// The monitor starts the registrar thread which calls this function.
	// The registrar waits for instance- and client processes to register and
	// unregister. When the process that registers have stored in data, it
	// notifies this thread.
	boost::interprocess::scoped_lock
	<boost::interprocess::interprocess_mutex> lock(in_.mutex_);
	
	in_.data_is_available_.wait(lock, boost::bind(&monitor_interface
	::in_is_data_available, this));
	
	// The calling monitor thread knows that in data is available upon return.
	set_in_data_available_state(false);
}

inline void monitor_interface::set_pid(pid_type pid) {
	in_.pid_ = pid;
}

inline pid_type monitor_interface::get_pid() const {
	return in_.pid_;
}

inline void monitor_interface::set_segment_name(std::string segment_name) {
	// Copy segment_name to c-string in_.segment_name_, and 0 terminate.
	std::size_t length = segment_name.copy(in_.segment_name_,
	segment_name_size);
	
	in_.segment_name_[length] = '\0';
}

inline std::string monitor_interface::get_segment_name() const {
	return std::string(in_.segment_name_);
}

inline void monitor_interface::set_process_type(monitor_interface::process_type
pt) {
	in_.process_type_ = pt;
}

inline monitor_interface::process_type monitor_interface::get_process_type()
const {
	return in_.process_type_;
}
	
inline void monitor_interface::set_operation(monitor_interface::operation op) {
	in_.operation_ = op;
}

inline monitor_interface::operation monitor_interface::get_operation() const {
	return in_.operation_;
}

inline void monitor_interface::set_owner_id(owner_id oid) {
	out_.owner_id_ = oid;
}

inline owner_id monitor_interface::get_owner_id() const {
	return out_.owner_id_;
}

// output operator for monitor_interface::process_type
template<class CharT, class Traits>
inline std::basic_ostream<CharT, Traits>&
operator<<(std::basic_ostream<CharT, Traits>& os,
const monitor_interface::process_type& u) {
	std::string name;
	switch (u) {
	case monitor_interface::database_process:
		name = "database";
		break;
	case monitor_interface::client_process:
		name = "client";
		break;
	default:
		name = "?";
	}
	os << name;
	return os;
}

} // namespace core
} // namespace starcounter

#endif // STARCOUNTER_CORE_IMPL_MONITOR_INTERFACE_HPP
