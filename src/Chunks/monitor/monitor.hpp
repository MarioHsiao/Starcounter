//
// monitor.hpp
//
// Copyright © 2006-2012 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#ifndef STARCOUNTER_CORE_MONITOR_HPP
#define STARCOUNTER_CORE_MONITOR_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#include <map> /// TODO: map or unordered_map
#include <set>
#include <iostream>
#include <ios>
#include <fstream>
#include <string>
#include <sstream>
#include <algorithm>
#include <cstddef>
#include <cstdlib>
#include <memory> /// ?
#include <utility>
#include <stdexcept>
#include <boost/unordered_map.hpp> /// TODO: map or unordered_map
#include <boost/cstdint.hpp>
#include <boost/interprocess/managed_shared_memory.hpp>
#include <boost/interprocess/allocators/allocator.hpp>
#include <boost/interprocess/sync/interprocess_mutex.hpp>
#include <boost/interprocess/sync/scoped_lock.hpp>
#include <boost/interprocess/sync/interprocess_condition.hpp>
#include <boost/date_time.hpp> // maybe not neccessary?
#include <boost/date_time/posix_time/posix_time_types.hpp> // No I/O just types.
#include <boost/date_time/microsec_time_clock.hpp>
#include <boost/thread/thread.hpp>
#include <boost/thread/win32/thread_primitives.hpp>
#include <boost/shared_ptr.hpp>
#include <boost/call_traits.hpp>
#include <boost/bind.hpp>
#include <boost/lexical_cast.hpp>
#include <boost/timer.hpp>
#if defined(_MSC_VER)
# define WIN32_LEAN_AND_MEAN
# include <windows.h> /// TODO: thread_primitives.hpp might replace this include
//# include <intrin.h>
# undef WIN32_LEAN_AND_MEAN
#endif // (_MSC_VER)
#include "../common/pid_type.hpp"
#include "../common/owner_id.hpp"
#include "../common/macro_definitions.hpp"
#include "../common/interprocess.hpp"
#include "../common/log_message.hpp"
#include "../common/config_param.hpp"
#include "../common/monitor_interface.hpp"
#include "../common/shared_interface.hpp"
#include "../common/bounded_buffer.hpp"
#include "event.hpp"
#include "bounded_message_buffer.hpp"
#include "process_info.hpp"

/// TODO Figure which of these to include. Maybe required if monitor opens a
/// database process managed shared memory.
//#include "../common/circular_buffer.hpp"
//#include "../common/bounded_buffer.hpp"
//#include "../common/chunk.hpp"
//#include "../common/shared_chunk_pool.hpp"
//#include "../common/channel.hpp"
//#include "../common/scheduler_channel.hpp"
//#include "../common/common_scheduler_interface.hpp"
//#include "../common/scheduler_interface.hpp"
//#include "../common/common_client_interface.hpp"
//#include "../common/client_interface.hpp"
//#include "../common/client_number.hpp"
//#include "../common/config_param.hpp"
//#include "../common/macro_definitions.hpp"
//#include "../common/interprocess.hpp"
//#include "../common/name_definitions.hpp"
//#include "../common/monitor_interface.hpp"


//#define _E_UNSPECIFIED 999L
namespace {

enum {
	_E_UNSPECIFIED = 999L
};

} // namespace

namespace starcounter {
namespace core {

/// Exception class.
class bad_monitor : public std::runtime_error {
public:
	/**
	 * @param message The c-string formated message to be passed.
	 */
	explicit bad_monitor(const char* message)
	: std::runtime_error(message) {}
};

/// Class monitor.
/**
 * @throws monitor_exception when something can not be achieved.
 */
// Objects of type boost::thread are not copyable.
class monitor : private boost::noncopyable {
public:
	// We can backup this and other information on disk so that if the monitor
	// process crashes, it can be restared by Windows and try to recover by
	// reading the data from disk. TODO: Discuss this.
	typedef std::map<owner_id, process_info> process_register_type;
	/// TODO: What is required to be able to use unordered_map<>.
	//typedef std::vector<std::string> exited_processes_list_type;
	
	enum state {
		stopped,
		running
	};
	
	enum { /// TODO: rename these enums.
		// The maximum number of object handles per thread is in the range
		// 1..MAXIMUM_WAIT_OBJECTS. This parameter cannot be zero.
		events_per_group = MAXIMUM_WAIT_OBJECTS,
		
		// Up to (events_per_group * database_process_event_groups) database
		// processes can be monitored.
		database_process_event_groups = 1,
		
		// Up to (events_per_group * client_process_event_groups) client
		// processes can be monitored.
		client_process_event_groups = 4
	};
	
	enum {
		active_segments_buffer_capacity = 256
	};
	
	/// Construction of the monitor application.
	// Log messages are appended to the log file, it is never deleted.
	/**
	 * @param argc Argument count.
	 * @param argv Argument vector.
	 * @throws starcounter::core::bad_monitor if the monitor can not start.
	 */
	explicit monitor(int argc, wchar_t* argv[]);
	
	/// Destruction of the monitor.
	// It waits for all threads to finnish and closes the monitor log file.
	~monitor();
	
	//--------------------------------------------------------------------------
	/// The wait_for_database_process_event() will wait for up to 64 database
	/// process events per group.
	// This function is reentrant and is called by one thread for each group.
	/**
	 * @param group Is the group (0..N) of server process events to watch. Each
	 *		group has up to 64 process events.
	 */
	void wait_for_database_process_event(std::size_t group);
	
	//--------------------------------------------------------------------------
	/// The wait_for_client_process_event() will wait for up to 64 client
	/// process events per group.
	// This function is reentrant and is called by one thread for each group.
	// Example: If monitoring up to 256 clients, there are 4 groups with up to
	// 64 client processes in each group and 4 threads (one per group) calls
	// this function. An event is eventually received when a client process
	// exit, or crash.
	/**
	 * @param group Is the group (0..N) of client process events to watch. Each
	 *		group has up to 64 client process events.
	 */
	void wait_for_client_process_event(std::size_t group);
	
	//--------------------------------------------------------------------------
	/// Register a database process for surveillance by the monitor.
	/// It is a "timed" function that can fail.
	// TODO: Implement timeout_milliseconds!
	/**
	 * @param pid Is the process id of the registering process.
	 * @param oid Is the owner_id that the registering process receives.
	 *		Upon failure, the owner_id is set to owner_id::none.
	 * @param timeout_milliseconds The number of milliseconds to wait before
	 *		a timeout may occur. NOT IMPLEMENTED YET!
	 * @return false if the call is returning because the registration of the
	 *		database process failed, or the time period specified by
	 *		timeout_milliseconds has elapsed, true otherwise.
	 */
	bool register_database_process(pid_type pid, owner_id& oid, unsigned int
	timeout_milliseconds);
	
	//--------------------------------------------------------------------------
	/// Unregister a database process, when preparing for a graceful shutdown of
	/// the system. It is a "timed" function that can fail.
	// TODO: Implement timeout_milliseconds!
	/**
	 * @param pid Is the process id of the unregistering process.
	 * @param oid Is the owner_id that the unregistering process receives.
	 *		If successful, the owner_id is set to owner_id::none.
	 * @param timeout_milliseconds The number of milliseconds to wait before
	 *		a timeout may occur. NOT IMPLEMENTED YET!
	 * @return false if the call is returning because the unregistration of the
	 *		database process failed, or the time period specified by
	 *		timeout_milliseconds has elapsed, true otherwise.
	 */
	bool unregister_database_process(pid_type pid, owner_id& oid, unsigned int
	timeout_milliseconds);
#if 0
	bool register_client_process(pid_type pid, owner_id& oid, unsigned int
	timeout_milliseconds);
	
	bool unregister_client_process(pid_type pid, owner_id& oid, unsigned int
	timeout_milliseconds);
#endif
	
#if 0 // idea
	// Methods for updating the event_register
	void insert_into_event_register(pid_type, owner_id) {
		boost::mutex::scoped_lock lock(event_register_mutex_);
		//...
	}
#endif
	
	/// Start the monitor threads.
	void run();
	
	/// Stop the monitor threads.
	void stop();
	
	/// Remove database process event.
	void remove_database_process_event(process_info::handle_type e);
	void remove_database_process_event(std::size_t group, uint32_t event_code);
	
	/// Remove client process event.
	void remove_client_process_event(process_info::handle_type e);
	void remove_client_process_event(std::size_t group, uint32_t event_code);

	/// Print pid register requires the pid register to be locked for a
	/// relatively long time so this is only used for debug.
	void print_event_register();
	
	/// Print pid exited processes requires the pid register to be locked for a
	/// relatively long time so this is only used for debug.
	//void print_exited_processes();
	
	/// The apc_function() calls this so that we can access member variables
	/// without having to make getters and setters for all.
	//void do_registration();
	
	/// Extract the database name from the segment name. The format of the
	/// segment name is:
	/// <DATABASE_NAME_PREFIX>_<DATABASE_NAME>_<SEQUENCE_NUMBER>
	/// and the sub string <DATABASE_NAME> is returned.
	/**
	 * @param segment_name The segment name, which is the name of the shared
	 *		memory segment of the database, used for connectivity.
	 * @return A std::string containing the database name.
	 */
	std::string segment_name_to_database_name(const std::string& segment_name);
	
	// The caller must lock the active_databases_mutex_.
	void print_active_databases();
	
	void set_active_databases_updated_flag(bool state) {
		active_databases_updated_flag_ = state;
	}
	
	bool active_databases_updated_flag() const {
		return active_databases_updated_flag_;
	}
	
	bool insert_database_name(const std::string& database_name);
	bool erase_database_name(const std::string& database_name);
	
private:
	/// The registration thread calls this.
	void registrar();
	
	static void __stdcall apc_function(boost::detail::win32::ulong_ptr arg);
	
	/// Get a new owner_id.
	owner_id get_new_owner_id();
	
	/// Open all databases shared memory, for each, scan through all chunks and
	/// look for the oid. Set cleanup flag in those owner_id's matching.
	/**
	 * @param oid The owner_id of the crashed client process.
	 * @return Number of chunks marked for cleanup.
	 */
	std::size_t find_chunks_and_mark_them_for_cleanup(owner_id oid);
	
	/// Write log message.
	/**
	 * @param message The message to be written.
	 *		NOTE: The string that is passed must either be private to the thread
	 *		that writes the message or be synchronized if shared between threads
	 *		which is not recommended. Best is to reserve capacity for a private
	 *		message string for each thread that writes to the log, and reuse it.
	 */
	void write_log_message(std::string message);
	
	/// Read log message.
	// The monitor log thread writes messages to the monitor log file.
	void read_log_message();
	
	bool monitor_log_file_is_open() const {
		return monitor_log_file_.is_open();
	}
	
	/// Write active databases.
	void update_active_databases_file();
	
	/// Watch resources, dor debug purpose only.
	void watch_resources();
	
	// The monitor initializes the monitor_interface_shared_memory_object.
	shared_memory_object monitor_interface_;
	mapped_region monitor_interface_region_;
	monitor_interface* the_monitor_interface_;
	
	// The state of the monitor.
	state state_; /// TODO: implement shutdown.
	
	/// TODO Maybe put this in a nested struct. Saved time not doing it.
	// The register_mutex_ is locked whenever a thread need to update any of the
	// variables below: the process_register_, the owner_id_counter_.
	boost::mutex register_mutex_;
	
	// The process register.
	process_register_type process_register_;
	
	// The owner_id counter is incremented by 1 each time an owner_id is taken.
	owner_id owner_id_counter_;
	
	// The exited processes list.
	//exited_processes_list_type exited_processes_list_;
	//boost::mutex exited_processes_list_mutex_;
	
	// The monitors log mechanism.
	bounded_message_buffer<log_message> bounded_message_buffer_;
	std::ofstream monitor_log_file_;
	boost::mutex log_file_mutex_;
	boost::condition log_file_is_open_;
	
	// The list of registered databases is updated when databases register and
	// unregister, or terminates.
	std::ofstream monitor_active_databases_file_;
	boost::mutex active_databases_mutex_;
	boost::condition active_databases_updated_;
	bool active_databases_updated_flag_;
	
	struct database_process_group {
		boost::thread thread_;
		boost::detail::win32::handle thread_handle_;
		std::vector<boost::detail::win32::handle> event_;
	} database_process_group_[database_process_event_groups];
	
	struct client_process_group {
		boost::thread thread_;
		boost::detail::win32::handle thread_handle_;
		std::vector<boost::detail::win32::handle> event_;
	} client_process_group_[client_process_event_groups];
	
	// The internal_chron_ is used to timestamp log messages. The constructor
	// starts the timer at 0 and then it is running all the time during program
	// execution.
	boost::timer internal_chron_;
	
	// The name of the server that started this monitor.
	std::string server_name_;
	
	// Path to the dir where the monitor's log file can be stored.
	std::string monitor_log_dir_path_;
	
	// The monitor's log file name.
	std::string log_file_name_;
	
	// Set with names of active databases.
	std::set<std::string> active_databases_;
	
	// The monitor's active databases file name.
	std::string active_databases_file_name_;
	
	bounded_buffer<std::string> active_segments_update_;
	
	//--------------------------------------------------------------------------
	// Threads (also in database_process_group and client_process_group)
	
	// The registrar thread waits for database- and client processes to register
	// and unregister. It is notified when there is data available in the
	// monitor interface and it will read the data from there and communicate
	// the information to some thread in the
	// database_process_event_thread_group_ or the
	// client_process_event_thread_group_. Then it waits for that thread to
	// complete the wait_for_registration
	boost::thread registrar_;
	
	// The log thread waits for log_messages to arrive in the
	// bounded_message_buffer_, and will write the messages to the log file.
	boost::thread log_thread_;
	
	// The active databases file updater thread waits for a notification from
	// any thread that updates the register, and will write a list of active
	// databases to the file:
	// %UserProfile%\AppData\Local\Starcounter\active_databases
	boost::thread active_databases_file_updater_thread_;
	
	// The resources watching thread is used for debug, verifying that resources
	// are recovered. It will keep an eye of all registered databases shared
	// memory segments resources. Number of free: chunks, channels, and
	// client_interfaces.
	boost::thread resources_watching_thread_;
};

} // namespace core
} // namespace starcounter

#include "impl/monitor.hpp"

#endif // STARCOUNTER_CORE_MONITOR_HPP
