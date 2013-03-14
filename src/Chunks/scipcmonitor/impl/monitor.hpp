//
// impl/monitor.hpp
//
// Copyright © 2006-2012 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//
// Implementation of class monitor.
//

#ifndef STARCOUNTER_CORE_IMPL_MONITOR_HPP
#define STARCOUNTER_CORE_IMPL_MONITOR_HPP

// Implementation

namespace starcounter {
namespace core {

monitor::monitor(int argc, wchar_t* argv[])
: ipc_monitor_cleanup_event_(),
monitor_interface_(),
active_segments_update_(active_segments_buffer_capacity),
active_databases_updated_flag_(false),
registrar_(),
cleanup_(),
active_databases_file_updater_thread_(),
#if defined (IPC_MONITOR_SHOW_ACTIVITY)
resources_watching_thread_(),
#endif // defined (IPC_MONITOR_SHOW_ACTIVITY)
owner_id_counter_(1),
owner_id_(ipc_monitor_owner_id) {
	/// TODO: Use Boost.Program_options.
	/// ScErrCreateMonitorInterface is reserved in errorcodes.xml for later use.
	
	// Disable synchronization with stdio before any other I/O operation.
	//std::ios::sync_with_stdio(false);

	// wcstombs() output to temp_buf and then it is copied to a string.
	/// TODO: Try to improve this code, by copying directly to string.
	char temp_buf[maximum_path_and_file_name_length];
	
	// The is_system flag is set to true if the first argument is "SYSTEM",
	// otherwise it is false. If it is true, privilege SeDebugPrivilege is set.
	bool is_system = false;
	
	// Number of characters in the multibyte string after being converted.
	std::size_t length;
	
	if (argc > 2) {
		// Get the first argument, server_name, and convert it from wide-
		// character string to multibyte string.
		length = std::wcstombs(temp_buf, argv[1],
		maximum_path_and_file_name_length -1);
		server_name_ = std::string(temp_buf);
		
		if (server_name_ == "SYSTEM") {
			is_system = true;
		}
		
		// Get the second argument, monitor_dir_path, and convert it from
		// wide character string to multibyte string.
		length = std::wcstombs(temp_buf, argv[2], maximum_path_and_file_name_length -1);
		temp_buf[length++] = SLASH;
		temp_buf[length++] = '\0';
		database_output_dir_name_ = std::wstring(argv[2]);

		// Open the log.
		log().open(server_name_.c_str(), "scipcmonitor", database_output_dir_name_.c_str());

		monitor_dir_path_ = std::wstring(argv[2]) +W_DEFAULT_MONITOR_DIR_NAME +W_SLASH;
		active_databases_file_path_ = std::wstring(argv[2]) +W_SLASH +W_DEFAULT_MONITOR_DIR_NAME +W_SLASH;
		
		// Trying to create monitor directory.
		if ((!CreateDirectory(active_databases_file_path_.c_str(), NULL))
		&& (ERROR_ALREADY_EXISTS != GetLastError())) {
			log().error(SCERRCANTCREATEIPCMONITORDIR);
			throw ipc_monitor_exception(SCERRCANTCREATEIPCMONITORDIR);
		}

		// Constructing path to active databases directory.
		std::wstring w_active_databases_dir_path = active_databases_file_path_
		+W_DEFAULT_MONITOR_ACTIVE_DATABASES_FILE_NAME;

		// Trying to create active databases directory.
		if ((!CreateDirectory(w_active_databases_dir_path.c_str(), NULL))
		&& (ERROR_ALREADY_EXISTS != GetLastError())) {
			log().error(SCERRIPCMONITORCREATEACTIVEDBDIR);
			throw ipc_monitor_exception(SCERRIPCMONITORCREATEACTIVEDBDIR);
		}
	}
	else {
		// The first argument (name of the server that started this monitor),
		// must be provided.
		log().error(SCERRIPCMONITORREQUIREDARGUMENTS);
		throw ipc_monitor_exception(SCERRIPCMONITORREQUIREDARGUMENTS);
	}
	
	// Constructing the full path to active databases file.
	active_databases_file_path_ = active_databases_file_path_
	+W_DEFAULT_MONITOR_ACTIVE_DATABASES_FILE_NAME +W_SLASH
	+W_DEFAULT_MONITOR_ACTIVE_DATABASES_FILE_NAME;

	// Checking if old active databases file already exists and deleting it.
	if (GetFileAttributes(active_databases_file_path_.c_str())
	!= INVALID_FILE_ATTRIBUTES) {
		if (!DeleteFile(active_databases_file_path_.c_str())) {
			log().error(SCERRIPCMONITORDELACTIVEDBFILE);
			throw ipc_monitor_exception(SCERRIPCMONITORDELACTIVEDBFILE);
		}
	}

	//--------------------------------------------------------------------------
	// Initialize the monitor_interface shared memory object.
	std::string monitor_interface_shared_memory_object_name;
	monitor_interface_shared_memory_object_name.reserve(128);
	
	// Arguments to the monitor (wchar_t):
	// First argument: server_name (L"PERSONAL" or L"SYSTEM", etc.)
	//
	// Second argument: ipc_monitor_dir_path.
	//
	// Third argument (is optional): The ipc_monitor_file_name. If not provided,
	// then the default monitor log file name is used:
	// <server_name>_"monitor.log", so the default name will either be
	// L"PERSONAL_monitor.log", or L"SYSTEM_monitor.log".
	monitor_interface_shared_memory_object_name = server_name_ +"_"
	+MONITOR_INTERFACE_SUFFIX;
	
	//--------------------------------------------------------------------------
	// Construct the ipc_monitor_cleanup_event_name.
	char ipc_monitor_cleanup_event_name[ipc_monitor_cleanup_event_name_size];

	// Format: "Local\<server_name>_ipc_monitor_cleanup_event".
	// Example: "Local\PERSONAL_ipc_monitor_cleanup_event"
	if ((length = _snprintf_s(ipc_monitor_cleanup_event_name, _countof
	(ipc_monitor_cleanup_event_name), ipc_monitor_cleanup_event_name_size
	-1 /* null */, "Local\\%s_ipc_monitor_cleanup_event", server_name_.c_str()))
	< 0) {
		log().error(SCERRFORMATIPCMONITORCLEANUPEV);
		throw ipc_monitor_exception(SCERRFORMATIPCMONITORCLEANUPEV);
	}
	ipc_monitor_cleanup_event_name[length] = '\0';

	wchar_t w_ipc_monitor_cleanup_event_name[ipc_monitor_cleanup_event_name_size];

	/// TODO: Fix insecure
	if ((length = mbstowcs(w_ipc_monitor_cleanup_event_name,
	ipc_monitor_cleanup_event_name, segment_name_size)) < 0) {
		// Failed to convert ipc_monitor_cleanup_event_name to multi-byte string.
		log().error(SCERRCONVERTIPCMONCLEANUPEVMBS);
		throw ipc_monitor_exception(SCERRCONVERTIPCMONCLEANUPEVMBS);
	}
	w_ipc_monitor_cleanup_event_name[length] = L'\0';

	// Create the ipc_monitor_cleanup_event_ to be used when waiting for a
	// database to finnish doing its part of the cleanup.
	if ((ipc_monitor_cleanup_event_ = ::CreateEvent(NULL, TRUE, FALSE,
	w_ipc_monitor_cleanup_event_name)) == NULL) {
		// Failed to create event.
		log().error(SCERRCREATEIPCMONITORCLEANUPEV);
		throw ipc_monitor_exception(SCERRCREATEIPCMONITORCLEANUPEV);
	}

	//--------------------------------------------------------------------------
	// Construct the active_databases_updated_event_name.
	char active_databases_updated_event_name[active_databases_updated_event_name_size];

	// Format: "Local\<server_name>_active_databases_updated_event".
	// Example: "Local\PERSONAL_active_databases_updated_event"
	if ((length = _snprintf_s(active_databases_updated_event_name, _countof
	(active_databases_updated_event_name), active_databases_updated_event_name_size
	-1 /* null */, "Local\\%s_"ACTIVE_DATABASES_UPDATED_EVENT, server_name_.c_str()))
	< 0) {
		log().error(SCERRIPCMFORMATACTIVEDBUPDATEDEV);
		throw ipc_monitor_exception(SCERRIPCMFORMATACTIVEDBUPDATEDEV);
	}
	active_databases_updated_event_name[length] = '\0';

	wchar_t w_active_databases_updated_event_name[active_databases_updated_event_name_size];

	/// TODO: Fix insecure
	if ((length = mbstowcs(w_active_databases_updated_event_name,
	active_databases_updated_event_name, segment_name_size)) < 0) {
		// Failed to convert active_databases_updated_event_name to multi-byte string.
		log().error(SCERRIPCMCONVACTIVEDBUPDATEDEVMB);
		throw ipc_monitor_exception(SCERRIPCMCONVACTIVEDBUPDATEDEVMB);
	}
	w_active_databases_updated_event_name[length] = L'\0';
	HANDLE active_databases_updated_event;

	// Create the active_databases_updated_event_ to be used when the active
	// databases set is updated.
	if ((active_databases_updated_event = ::CreateEvent(NULL, TRUE, FALSE,
	w_active_databases_updated_event_name)) == NULL) {
		// Failed to create event.
		log().error(SCERRCREATEACTIVEDBUPDATEDEV);
		throw ipc_monitor_exception(SCERRCREATEACTIVEDBUPDATEDEV);
	}

	//--------------------------------------------------------------------------
	// Check if the monitor_interface with the name
	// monitor_interface_shared_memory_object_name already exist. That indicates
	// that a monitor is already running and have been started by this server.
	
	// Try to open the monitor interface shared memory object.
	monitor_interface_.init_open
	(monitor_interface_shared_memory_object_name.c_str());
	
	/// Map the whole shared memory in this process.
	///mapped_region monitor_interface_region_(monitor_interface_);
	
	if (monitor_interface_.is_valid()) {
		// The monitor_interface shared memory object is already open.
		// In this case, ack to the server that a monitor is already monitoring.
		std::wcout << L"monitoring" << std::endl;
		std::wcout.flush();
		
		// Must wait for a while to increase the probability that what has
		// been written on standard output has been received by the server.
		// Waiting here for 5000 ms before exit, hopefully enough.
		Sleep(5000);
		
		// Exit because another monitor is already doing the job.
		exit(EXIT_SUCCESS);
	}
	
	//--------------------------------------------------------------------------
	// Create a shared memory object to hold the monitor_interface.
	monitor_interface_.init_create(monitor_interface_shared_memory_object_name
	.c_str(), sizeof(monitor_interface), is_system);
	
	if (!monitor_interface_.is_valid()) {
		log().error(SCERRINVALIDIPCMONINTERFACSHMOBJ);
		throw ipc_monitor_exception(SCERRINVALIDIPCMONINTERFACSHMOBJ);
	}
	
	// Map the whole shared memory in this process.
	monitor_interface_region_.init(monitor_interface_);
	
	if (!monitor_interface_region_.is_valid()) {
		log().error(SCERRINVALIDIPCMONINTERFACMAPREG);
		throw ipc_monitor_exception(SCERRINVALIDIPCMONINTERFACMAPREG);
	}
	
	// Get the address of the mapped region and construct the shared memory
	// object in shared memory.
	the_monitor_interface_ = new(monitor_interface_region_.get_address())
	monitor_interface;
	
	// Notify all waiting threads in other processes that the monitor_interface
	// is ready to be used.

	the_monitor_interface_->active_database_set()
	.set_active_databases_set_update_event(active_databases_updated_event);
	_mm_mfence();
	the_monitor_interface_->is_ready_notify_all();
	
	//--------------------------------------------------------------------------
	// For each database and client event groups, create the vectors containing
	// database- and client process events. Since a vector may be moved while
	// reserving capacity, this is done before creating the threads that access
	// the vectors. The capacity of the vector will never be exceeded so the
	// vectors never have to reallocate their internal memory.
	for (std::size_t i = 0; i < database_process_event_groups; ++i) {
		database_process_group_[i].event_.reserve(events_per_group);
	}
	
	for (std::size_t i = 0; i < client_process_event_groups; ++i) {
		client_process_group_[i].event_.reserve(events_per_group);
	}

	//--------------------------------------------------------------------------
	if (is_system) {
		// Try to set the privilege SeDebugPrivilege, so that the monitor can
		// call OpenProcess() on a process started as a different user, in
		// another session.
		HANDLE access_token;
		uint32_t err = 0;
		
		if (OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES,
		&access_token) == 0) {
			err = GetLastError();
			CloseHandle(access_token);
			log().error(SCERRIPCMONITOROPENPROCESSTOKEN);
			throw ipc_monitor_exception(SCERRIPCMONITOROPENPROCESSTOKEN);
		}
		
		LUID luid;
		
		if (LookupPrivilegeValue(NULL, L"SeDebugPrivilege", &luid) == 0) {
			err = GetLastError();
			CloseHandle(access_token);
			log().error(SCERRIPCMONLOOKUPPRIVILEGEVALUE);
			throw ipc_monitor_exception(SCERRIPCMONLOOKUPPRIVILEGEVALUE);
		}
		
		TOKEN_PRIVILEGES tp;
		tp.PrivilegeCount = 1;
		tp.Privileges[0].Luid = luid;
		tp.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;
		
		// Enable SeDebugPrivilege.
		if (!AdjustTokenPrivileges(access_token, FALSE, &tp,
		sizeof(TOKEN_PRIVILEGES), (PTOKEN_PRIVILEGES) NULL, (PDWORD) NULL)) {
			err = GetLastError();
			CloseHandle(access_token);
			log().error(SCERRIPCMONADJUSTTOKENPRIVILEGES);
			throw ipc_monitor_exception(SCERRIPCMONADJUSTTOKENPRIVILEGES);
		}
		
		if (GetLastError() == ERROR_NOT_ALL_ASSIGNED) {
			CloseHandle(access_token);
			log().error(SCERRIPCMONSETSEDEBUGPRIVILEGE);
			throw ipc_monitor_exception(SCERRIPCMONSETSEDEBUGPRIVILEGE);
		}
		CloseHandle(access_token);
	}
}

monitor::~monitor() {
	/// TODO: Send some interrupt and set flag to terminate and hope it works.

	// Join threads.
	for (std::size_t i = 0; i < database_process_event_groups; ++i) {
		database_process_group_[i].thread_.join();
	}
	
	for (std::size_t i = 0; i < client_process_event_groups; ++i) {
		client_process_group_[i].thread_.join();
	}
	
	registrar_.join();
	cleanup_.join();
	active_databases_file_updater_thread_.join();

#if defined (IPC_MONITOR_SHOW_ACTIVITY)
	resources_watching_thread_.join();
#endif // defined (IPC_MONITOR_SHOW_ACTIVITY)
}

void monitor::run() {
	// Monitor objects manage their own threads and since the constructor have
	// initialized member variables it is safe to start the threads.
	
	// Start a group of threads monitoring database process event.
	for (std::size_t i = 0; i < database_process_event_groups; ++i) {
		database_process_group_[i].thread_ = boost::thread(boost::bind(&monitor
		::wait_for_database_process_event, this, i));
		
		// Store the native handle of the thread. It is used in the call to
		// QueueUserAPC() by the registrar_ thread.
		database_process_group_[i].thread_handle_
		= database_process_group_[i].thread_.native_handle();
	}
	
	// Start a group of threads monitoring client process event.
	for (std::size_t i = 0; i < client_process_event_groups; ++i) {
		client_process_group_[i].thread_ = boost::thread(boost::bind(&monitor
		::wait_for_client_process_event, this, i));
		
		// Store the native handle of the thread. It is used in the call to
		// QueueUserAPC() by the registrar_ thread.
		client_process_group_[i].thread_handle_
		= client_process_group_[i].thread_.native_handle();
	}
	
	// Start the registrar thread. This must be done after the
	// database_process_group_[s] and client_process_group_[s] native_handle(s)
	// for those threads have been stored, since it is used in the call to
	// QueueUserAPC() by the registrar_ thread.
	registrar_ = boost::thread(boost::bind(&monitor::registrar, this));

	// Start the clean up thread.
	cleanup_ = boost::thread(boost::bind(&monitor::cleanup, this));
	
	// Start the active databases thread.
	active_databases_file_updater_thread_ = boost::thread(boost::bind
	(&monitor::update_active_databases_file, this));
	
#if defined (IPC_MONITOR_SHOW_ACTIVITY)
	// Start the resources watching thread.
	resources_watching_thread_ = boost::thread(boost::bind
	(&monitor::watch_resources, this));
#endif // defined (IPC_MONITOR_SHOW_ACTIVITY)
}

void monitor::wait_for_database_process_event(std::size_t group) {
	// The event code returned from WaitForMultipleObjectsEx() and SleepEx().
	uint32_t event_code = 0;
	HANDLE the_event;
	
	/// TODO: termination
	while (true) {
		if (!database_process_group_[group].event_.empty()) {
			// Wait for database process events, or for an APC.
			event_code = WaitForMultipleObjectsEx(
			database_process_group_[group].event_.size(),
			event::const_iterator(&database_process_group_[group].event_[0]),
			false, INFINITE, true);
		}
		else {
			// No process exit_event(s) to wait for, just wait for an APC.
			event_code = SleepEx(INFINITE, true);
		}
		
		/// If the vector was updated in the APC call it is either the same
		/// or N less elements down to 0...so, think about that.
		if (event_code < database_process_group_[group].event_.size()) {
			// A registered database process exit. Get the exit_event.
			boost::detail::win32::handle exit_event
			= database_process_group_[group].event_[event_code];
			
			// Search for database process exit_event in the process_register_.
			for (process_register_type::iterator process_register_it
			= process_register_.begin(); process_register_it
			!= process_register_.end(); ++process_register_it) {
				if (process_register_it->second.get_handle() == exit_event) {
					switch (process_register_it->second.get_process_type()) {
					case monitor_interface::database_process: /// It must be!
						// A registrered database process terminated:
						// All clients that think they are still connected to
						// the database process need to know that it is down.
						// Try to set the state to
						// database_terminated_unexpectedly, and wake up clients
						// who are blocked on any channel.
						
						try {
							process_register_type::iterator
							process_register_it_2;
							
							// Search for the owner_id of the terminated
							// database process in the process_register_.
							// TODO: process_register_it_2 == process_register_it
							// so why find it? Remove 
							
							if ((process_register_it_2 = process_register_.find
							(process_register_it->first))
							!= process_register_.end()) {
								// Found the owner_id. Open the database
								// shared memory segment that this database
								// process had created.

								// Erase the segment name from the cleanup_task table.
								the_monitor_interface_->erase_segment_name
								(process_register_it_2->second.get_segment_name().c_str(),
								ipc_monitor_cleanup_event_);

								try {
									// Try open the segment.
									shared_interface shared
									(process_register_it_2
									->second.get_segment_name(), std::string(),
									pid_type(pid_type::no_pid));
									
									// If failed to open the segment, a
									// shared_interface_exception is thrown.
									// It is likely that no client were using
									// the segment, meaning there is no client
									// to inform so in this case we only log
									// what happened, where we catch the
									// exception.
									
									// Set the state to
									// database_terminated_unexpectedly.
									shared.database_state
									(common_client_interface_type
									::database_terminated_unexpectedly);
									
									// Notify waiting clients on all channels.
									for (std::size_t n = 0; n < channels; ++n) {
										shared.client_interface(n).notify();
									}
									
									/// TODO: Figure when to remove the event.

									// Try to erase database name from
									// active_databases_, and notify the
									// active_databases_file_updater_thread_.
									
									if (!erase_database_name
									(segment_name_to_database_name
									(process_register_it_2->second
									.get_segment_name()))) {
										// Failed to erase the database name.
										log().error(SCERRIPCMONITORERASEDATABASENAME);
									}
									
									// Remove from the process_register.
									process_register_.erase
									(process_register_it_2);
								}
								catch (shared_interface_exception&) {
									/// Remove event and database process info
									/// from the process_register.
									
									// Try to erase database name from
									// active_databases_, and notify the
									// active_databases_file_updater_thread_.
									if (!erase_database_name
									(segment_name_to_database_name
									(process_register_it_2->second
									.get_segment_name()))) {
										// Failed to erase the database name.
										log().error(SCERRIPCMONITORERASEDATABASENAME);
									}
									
									// Remove from the process_register.
									process_register_.erase
									(process_register_it_2);
								}
							}
							else {
								// Database segment name not found.
								// No clients could be informed
								// that the database is down.
							}
						}
						catch (boost::interprocess::interprocess_exception&) {
							log().error(SCERRBOOSTINTERPROCESSEXCEPTION);
						}
						catch (...) {
							log().error(SCERRTRYOPENSEGMENTUNKNOWNEXCEPT);
						}
						break;
					case monitor_interface::client_process: /// It can't be!
						log().error(SCERRGOTCLIENTPROCESSTYPENOTDB);
						break;
					default: /// Impossible!
						// Unknown proess type exit. Cosmic X-ray corrupted RAM?
						log().error(SCERRGOTUNKNOWNPROCESSTYPENOTDB);
						break;
					}
					// Found the exit_event, stop searching.
					break;
				}
			}
			remove_database_process_event(group, event_code);
		}
		else {
			switch (event_code) {
			case WAIT_IO_COMPLETION: {
					// The wait was ended by one or more user-mode asynchronous
					// procedure calls (APC) queued to this thread. The
					// apc_function() was called and returned instantly.
					switch (the_monitor_interface_->get_operation()) {
					case monitor_interface::registration_request: {
							// Store info about the registering database
							// process, if not already registered.
							/// TODO: Handle the case if the pid exists already.
							{
								/// TODO: Try to optimize and hold this mutex
								/// for the shortest time possible, as usual.
								boost::mutex::scoped_lock register_lock
								(register_mutex_);
								
								// Check if this pid already exist in the
								// event_register:
								for (process_register_type::iterator pos
								= process_register_.begin(); pos
								!= process_register_.end(); ++pos) {
									if (pos->second.get_pid()
									== the_monitor_interface_->get_pid()) {
										/// TODO: Log this? pid exists...not handled yet!
										/// Is this a problem? The key is
										/// owner_id, not pid.
									}
								}
								
								// The pid does not exist in the event register.
								// Register the process.
								if ((the_event = ::OpenProcess(SYNCHRONIZE, FALSE,
								the_monitor_interface_->get_pid())) == NULL) {
									// OpenProcess() failed.
									log().error(SCERROPENPROCESSFAILEDREGDBPROC);
									break;
								}
								
								// Get a new unique owner_id.
								owner_id new_owner_id = get_new_owner_id();
								
								// Insert database process info.
								process_register_[new_owner_id] =
								process_info(the_event,
								the_monitor_interface_->get_process_type(),
								the_monitor_interface_->get_pid(),
								the_monitor_interface_->get_segment_name());
								
								// Store the event to be monitored.
								database_process_group_[group].event_.push_back
								(the_event);
								
								// Set out data in the monitor_interface.
								the_monitor_interface_
								->set_owner_id(new_owner_id);
								
								active_segments_update_.push_front
								(the_monitor_interface_->get_segment_name());
								
								// Try to insert database name into
								// active_databases_, and notify the
								// active_databases_file_updater_thread_.
								if (!insert_database_name
								(segment_name_to_database_name
								(the_monitor_interface_->get_segment_name()))) {
									// Failed to insert database name into
									// active_databases_.
									log().error(SCERRIPCMONINSERTDBNAMEACTIVEDBR);
								}
								
								the_monitor_interface_
								->set_out_data_available_state(true);
							}
							
							// Notify the registering database process that out
							// data is available.
							the_monitor_interface_
							->out_data_is_available_notify_one();
						}
						break;
					case monitor_interface::unregistration_request: {
							// A database process unregisters.
							// Remove unregistering database process from the
							// process_register.
							{
								/// TODO: Try to optimize and hold this mutex
								/// for the shortest time possible, as usual.
								boost::mutex::scoped_lock register_lock
								(register_mutex_);
								
								// Find the process info to remove by searching
								// for the key - the owner_id.
								for (process_register_type::iterator pos
								= process_register_.begin(); pos
								!= process_register_.end(); ++pos) {
									if (pos->second.get_pid()
									== the_monitor_interface_->get_pid()
									&& pos->first
									== the_monitor_interface_->get_owner_id()) {
										// The pid exists and the owner_id
										// matches. Remove it from the register.
										
										// Try to erase database name from
										// active_databases_, and notify the
										// active_databases_file_updater_thread_
										if (!erase_database_name
										(segment_name_to_database_name
										(pos->second.get_segment_name()))) {
											// Failed to erase database name.
											log().error(SCERRIPCMONITORERASEDATABASENAME);
										}
										
										// Remove from the process_register_.
										process_register_.erase(pos);
										break;
									}
								}
								
								the_monitor_interface_
								->set_out_data_available_state(true);
							}
							
							// Notify the unregistering database process that
							// out data is available.
							the_monitor_interface_
							->out_data_is_available_notify_one();
						}
						break;
					}
				}
				break;
			case WAIT_FAILED:
				break;
			case ERROR_INVALID_PARAMETER:
				break;
			default:
				break;
			}
		}
	}
}

void monitor::wait_for_client_process_event(std::size_t group) {
	// The event code returned from WaitForMultipleObjectsEx() and SleepEx().
	uint32_t event_code = 0;
	HANDLE the_event;
	
	/// TODO: termination
	while (true) {
		if (!client_process_group_[group].event_.empty()) {
			// Wait for client process events, or for an APC.
			event_code = WaitForMultipleObjectsEx(
			client_process_group_[group].event_.size(),
			event::const_iterator(&client_process_group_[group].event_[0]),
			false, INFINITE, true);
		}
		else {
			// No process exit_event(s) to wait for, just wait for an APC.
			event_code = SleepEx(INFINITE, true);
		}
		
		/// If the vector was updated in the APC call it is either the same
		/// or N less elements down to 0...so, think about that.
		if (event_code < client_process_group_[group].event_.size()) {
			// A registered client process exit. Get the exit_event.
			boost::detail::win32::handle exit_event
			= client_process_group_[group].event_[event_code];
			
			// Search for client process exit_event in the process_register_.
			for (process_register_type::iterator process_register_it
			= process_register_.begin(); process_register_it
			!= process_register_.end(); ++process_register_it) {
				owner_id owner_id_of_terminated_process(owner_id::none);
				
				if (process_register_it->second.get_handle() == exit_event) {
					switch (process_register_it->second.get_process_type()) {
					case monitor_interface::client_process: /// It must be!
						owner_id_of_terminated_process
						= process_register_it->first;
						
						// A registrered client process terminated:
						// All database processes scheduler's, in all databases
						// need to be notified (send notification on each
						// channel instead of the client).
						//
						// This thread shall open all shared memory segments
						// that have been registered. Then scan all resources
						// (chunks and channels) for the owner_id of the
						// terminated client and set the clean up flag on all
						// those.
						// -----------------------------------------------------
						// NOTE: When checking out resources, first check that
						// the resource is not marked for clean up!!!
						// -----------------------------------------------------
						// Also set the state to clean up in all schedulers.
						// When done, notify all schedulers that may be waiting.
						// They will wake-up and check the state. If its normal
						// they do normal things, but since it is "cleanup" they
						// will clean up resources. All threads must check the
						// state before waiting.
						// ...
						
						// Client process with pid
						// process_register_it->second.get_pid()
						// terminated.
						
						try {
							// For each registered segment_name, open the shared
							// memory segment, do the clean up job until
							// completely finnished, and then open the next
							// shared memory segment and so on. Only a limited
							// number of shared memory segments can be mapped
							// in the process at the same time. Doing only one
							// each time but later on it can be optimized to
							// map more than one at the same time. However,
							// mapping all is very dangerous because we can be
							// out of memory and the system goes down.
							
							/// TODO: Try to optimize and hold this mutex
							/// for the shortest time possible, as usual.
							boost::mutex::scoped_lock register_lock(register_mutex_);

							// Search for all segment names in the register.
							for (process_register_type::iterator
							process_register_it_2 = process_register_.begin();
							process_register_it_2 != process_register_.end();
							++process_register_it_2) { /// Iterating through process_register, trying to find databases
								if (process_register_it_2
								->second.get_segment_name().empty()) {
									// Not a database process.
									continue;
								}
								
								// Found a database shared memory segment.

								// Insert the segment name into the cleanup_task array
								// and get the index of it.
								int32_t cleanup_task_index = the_monitor_interface_
								->insert_segment_name(process_register_it_2->second.get_segment_name().c_str());

								// TODO: Error handling.
								if (cleanup_task_index == -1) {
									// Could not insert segment name.
									log().error(SCERRIPCMONITORINSERTSEGMENTNAME);
								}

								shared_interface shared(process_register_it_2
								->second.get_segment_name(), std::string(),
								pid_type(pid_type::no_pid));
								
								// If failed to open the segment, a
								// shared_interface_exception is thrown.
								// It is likely that no client were using the
								// segment, meaning there is no client to inform
								// so in this case we only log what happened,
								// where we catch the exception.
								
								// Does the destructor close it? TODO: Check!<<<<<<<<<<<<<<<<<<<<<<<<
								
								std::size_t channels_to_recover = 0;
								
								// For each client_interface, find the ones that
								// have the owner_id_of_terminated_process.
								for (std::size_t n = 0; n < max_number_of_clients; ++n) {
									if (shared.client_interface(n).get_owner_id()
									== owner_id_of_terminated_process) {
										client_interface_type* client_interface_ptr
										= &shared.client_interface(n);
										
										_mm_mfence();

										///=====================================
										/// Unlock all robust spinlocks that may
										/// have been left in a locked state by
										/// the terminated client process.
										///=====================================

										// Unlock the scheduler_interface[s]
										// channel number queue.

#if defined (IPC_SCHEDULER_INTERFACE_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
										for (std::size_t si = 0; si < shared.common_scheduler_interface()
										.number_of_active_schedulers(); ++si) {
											if (shared.scheduler_interface(si).channel_number()
											.if_locked_with_id_recover_and_unlock
											(owner_id_of_terminated_process.get()) == true) {
												//std::cout << "Unlocked scheduler_interface(" << si
												//<< ") with owner_id_of_terminated_process = "
												//< owner_id_of_terminated_process.get() << std::endl;
											}
											//else {
											//	std::cout << "scheduler_interface(" << si << ") is not locked with id of terminated_process." << std::endl;
											//}
										}
#endif // defined (IPC_SCHEDULER_INTERFACE_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
										
										if (client_interface_ptr) {
											//common_client_interface_ptr->increment_client_interfaces_to_clean_up();
											shared.common_client_interface().increment_client_interfaces_to_clean_up();
											client_interface_ptr->database_cleanup_index() = cleanup_task_index;
											////std::cout << "monitor::wait_for_client_process_event(): " << client_interface_ptr << "->database_cleanup_index() = " << cleanup_task_index << std::endl;
											// I think it is important that the increment above is done before
											// marking for clean up below.
											_mm_mfence();
											_mm_lfence(); // serializes instructions

											// Set the owner_id's clean up flag, thereby
											// indirectly marking all resources (chunks
											// and channels) that the client process
											// owned, for clean up.
											client_interface_ptr->get_owner_id().mark_for_clean_up();
										}

										// For each of the channels the client
										// owned, notify the scheduler.

										// For each mask word, bitscan to find the channels owned by the terminated client.
										for (uint32_t ch_index = 0; ch_index < resource_map::channels_mask_size; ++ch_index) {
											for (resource_map::mask_type mask = client_interface_ptr->get_resource_map()
											.get_owned_channels_mask(ch_index); mask; mask &= mask -1) {
												uint32_t ch = bit_scan_forward(mask);
												ch += ch_index << resource_map::shift_bits_in_mask_type;
												channel_type& the_channel = shared.channel(ch);
												scheduler_number the_scheduler_number = the_channel.get_scheduler_number();
												scheduler_interface_type* scheduler_interface_ptr = 0;

												if (the_scheduler_number != -1) {
													scheduler_interface_ptr = &shared.scheduler_interface(the_scheduler_number);
												}

												HANDLE notify_scheduler_to_do_clean_up_event
												= shared.scheduler_work_event(the_channel.get_scheduler_number());

												// A fence is needed so that all accesses to the channel is
												// completed when marking it to be released.
												_mm_mfence();
												_mm_lfence(); // serializes instructions
												
												// Mark channel to be released.
												// After this the channel cannot
												// be accessed by the monitor.
												the_channel.set_to_be_released();
												++channels_to_recover;
												
												// The scheduler may be waiting. Notify the scheduler that probes this channel.
												if (scheduler_interface_ptr) {
													if ((scheduler_interface_ptr->notify_scheduler_to_do_clean_up
													(notify_scheduler_to_do_clean_up_event)) == true) {
														// Succeessfully notified the scheduler on this channel.
													}
													else {
														// Failed to notify the scheduler on this channel.
														log().error(SCERRIPCMONFAILEDNOTIFYSCHEDULER);
													}
												}
												else {
													// Scheduler did clean up on this channel.
												}
											}
										}
									}
								}
							}
						}
						catch (shared_interface_exception&) {
							// Failed to open the database shared memory segment and
							// is unable to notify the clients.
							log().error(SCERROPENDBSHMSEGTONOTIFYCLIENTS);
						}
						catch (boost::interprocess::interprocess_exception&) {
							log().error(SCERRBOOSTINTERPROCESSEXCEPTION);
						}
						catch (...) {
							log().error(SCERRIPCMONOPENDBSHMSEGUNKNOWNEX);
						}
						
						// Remove from the process_register here?
						process_register_.erase(process_register_it);
						break;

					case monitor_interface::database_process: /// It can't be!
						// A registrered database process exit (crashed):
						// All clients that think they are still connected to
						// the terminated database process need to know...set
						// the flag to indicate this unnormal state, and wake up
						// all involved clients.
						// ...
						break;
					
					default: /// Impossible!
						// Unknown proess type exit. Cosmic X-ray corrupted RAM?
						log().error(SCERRIPCMONUNKNOWNPROESSTYPEEXIT);
						break;
					} /// Getting iterator to process register.
					break;
				} /// Found event == exit event
			} /// Search for client process exit_event in the process_register_.
			remove_client_process_event(group, event_code);
		}
		else {
			switch (event_code) {
			case WAIT_IO_COMPLETION: {
					// The wait was ended by one or more user-mode asynchronous
					// procedure calls (APC) queued to this thread. The
					// apc_function() was called and returned instantly.
					switch (the_monitor_interface_->get_operation()) {
					case monitor_interface::registration_request: {
							// Store info about the registering client process,
							// if not already registered.
							/// TODO: Handle the case if the pid exists already.
							{
								/// TODO: Try to optimize and hold this mutex
								/// for the shortest time possible, as usual.
								boost::mutex::scoped_lock register_lock
								(register_mutex_);
								
								// Check if this pid already exist in the
								// event_register:
								for (process_register_type::iterator pos
								= process_register_.begin(); pos
								!= process_register_.end(); ++pos) {
									if (pos->second.get_pid()
									== the_monitor_interface_->get_pid()) {
										/// TODO: Log this? pid exists...not handled yet!
										/// Is this a problem? The key is
										/// owner_id, not pid.
									}
								}
								
								// The pid does not exist in the event register.
								// Register the client process.
								if ((the_event = OpenProcess(SYNCHRONIZE, FALSE,
								the_monitor_interface_->get_pid())) == NULL) {
									// OpenProcess() failed.
									log().error(SCERROPENPROCESSFAILEDREGCLIPROC);
									break;
								}
								
								// Get a new unique owner_id.
								owner_id new_owner_id = get_new_owner_id();
								// Insert client process info.
								process_register_[new_owner_id] =
								process_info(the_event,
								the_monitor_interface_->get_process_type(),
								the_monitor_interface_->get_pid(),
								the_monitor_interface_->get_segment_name());
								
								// Store the event to be monitored.
								client_process_group_[group].event_.push_back
								(the_event);
								
								// Set out data in the monitor_interface.
								the_monitor_interface_
								->set_owner_id(new_owner_id);
								
								the_monitor_interface_
								->set_out_data_available_state(true);
							}
							
							// Notify the registering client process that out
							// data is available.
							the_monitor_interface_
							->out_data_is_available_notify_one();
						}
						break;
					case monitor_interface::unregistration_request: {
							// A client process unregisters.
							// Remove unregistering client process from the
							// process_register.
							{
								/// TODO: Try to optimize and hold this mutex
								/// for the shortest time possible, as usual.
								boost::mutex::scoped_lock register_lock
								(register_mutex_);
								
								// Find the process info to remove by searching
								// for the key - the owner_id.
								for (process_register_type::iterator pos
								= process_register_.begin(); pos
								!= process_register_.end(); ++pos) {
									if (pos->second.get_pid()
									== the_monitor_interface_->get_pid()
									&& pos->first
									== the_monitor_interface_->get_owner_id()) {
										// The pid exitst and the owner_id
										// matches. Remove it from the register.
										break;
									}
								}
								
								// Set out data in the monitor_interface.
								//the_monitor_interface_
								//->set_owner_id(owner_id::none);
								
								the_monitor_interface_
								->set_out_data_available_state(true);
							}
							
							// Notify the unregistering client process that out
							// data is available.
							the_monitor_interface_
							->out_data_is_available_notify_one();
						}
						break;
					}
				}
				break;
			case WAIT_FAILED:
				break;
			case ERROR_INVALID_PARAMETER:
				break;
			default:
				break;
			}
		}
	}
}

/// private:

void monitor::registrar() {
	/// TODO: Shutdown mechanism.
	while (true) {
		the_monitor_interface_->wait_for_registration();
		
		// In data is available.
		switch (the_monitor_interface_->get_process_type()) {
		case monitor_interface::database_process:
			switch (the_monitor_interface_->get_operation()) {
			case monitor_interface::registration_request:
				// Database process registration request.
				// Search all groups for a vector<event> that is not full.
				for (std::size_t i = 0; i < database_process_event_groups; ++i) {
					if (database_process_group_[i].event_.size()
					< events_per_group) {
						/// TODO: use thread_primitives.hpp
						// Queue an user apc to that thread.
						QueueUserAPC(apc_function,
						database_process_group_[i].thread_handle_,
						reinterpret_cast<boost::detail::win32::ulong_ptr>(this));
						break;
					}
				}
				
				// Did we find a vacant element to push in an event or not??
				
				// That thread may wake up at any moment but it cannot add
				// a new event it can only remove (when a process exit).
				break;
			case monitor_interface::unregistration_request:
				// The database unregisters.
				break;
			}
			break;
		case monitor_interface::client_process:
			switch (the_monitor_interface_->get_operation()) {
			case monitor_interface::registration_request:
				// Client process registration request.
				// Search all groups for a vector<event> that is not full.
				for (std::size_t i = 0; i < client_process_event_groups; ++i) {
					if (client_process_group_[i].event_.size()
					< events_per_group) {
						/// TODO: use thread_primitives.hpp
						// Queue an user apc to that thread.
						::QueueUserAPC(apc_function,
						client_process_group_[i].thread_handle_,
						reinterpret_cast<boost::detail::win32::ulong_ptr>
						(this));
						break;
					}
				}
				
				// Did we find a vacant element to push in an event or not??
				
				// That thread may wake up at any moment but it cannot add
				// a new event it can only remove (when a process exit).
				// ...
				break;
			case monitor_interface::unregistration_request:
				// A client process unregisters.
				// Remove unregistering client process from the
				// process_register.
				{
					/// TODO: Try to optimize and hold this mutex
					/// for the shortest time possible, as usual.
					boost::mutex::scoped_lock register_lock(register_mutex_);
					
					// Find the process info to remove by searching for the key
					// - the owner_id.
					process_register_type::iterator pos = process_register_
					.find(the_monitor_interface_->get_owner_id());
					
					if (pos != process_register_.end()) {
						// Found it. Check that the pid matches as well.
						if (pos->second.get_pid()
						== the_monitor_interface_->get_pid()) {
							// The pid matches as well. Remove from the
							// register.
							remove_client_process_event(pos->second
							.get_handle());
							
							process_register_.erase(pos);
							the_monitor_interface_->set_owner_id(owner_id
							(owner_id::none));
						}
						else {
							// The owner_id matches but not the pid. Something
							// is wrong, so the process_info is not removed.
							log().error(SCERRTHEOWNERIDMATCHBUTNOTTHEPID);
						}
					}
					else {
						// The pid matches but not the owner_id. Something is wrong,
						// so the process_info is not removed.
						log().error(SCERRTHEPIDMATCHBUTNOTTHEOWNERID);
					}
					
					the_monitor_interface_->set_out_data_available_state(true);
					
					// Notify the unregistering process that out data is
					// available.
					the_monitor_interface_->out_data_is_available_notify_one();
				}
			}
			break;
		}
	}
}

void monitor::cleanup() {
	/// TODO: Shutdown mechanism.
	while (true) {
		switch (::WaitForSingleObject(ipc_monitor_cleanup_event_, INFINITE)) {
		case WAIT_OBJECT_0:
			// A database is done with recovering the channels and have notified
			// the IPC monitor to recover resources (chunks and client_interface.)
			// It will do so until there is nothing more to cleanup.
			while (the_monitor_interface_->get_cleanup_flag() != 0) {
				const char* segment_name_to_be_opened;
				segment_name_to_be_opened = the_monitor_interface_
				->get_a_segment_name(ipc_monitor_cleanup_event_);

				if (segment_name_to_be_opened) {
					try {
						shared_interface shared(segment_name_to_be_opened,
						std::string(), pid_type(pid_type::no_pid));

						// If failed to open the segment, a
						// shared_interface_exception is thrown.

						////std::cout << "monitor::cleanup(): Opened the IPC shared memory segment "
						////<< segment_name_to_be_opened << std::endl;
						// Does the destructor close it? TODO: Check!<<<<<<<<<<<<<<<<<<<<<<<<

						///=====================================================
						/// Recover chunks and then client_interface.
						///=====================================================
						
						// Now, when the IPC monitor cleanup thread have opened
						// an IPC shared memory segment that need cleanup, it
						// may find one or several client interfaces marked for
						// cleanup, including those that scheduler's are not
						// finnished with doing cleanup.

						// For each client_interface, find the ones that
						// have the owner_id_of_terminated_process.
						for (std::size_t n = 0; n < max_number_of_clients; ++n) {
							if (shared.client_interface(n).get_owner_id().get_clean_up()) {
								client_interface_type* client_interface_ptr
								= &shared.client_interface(n);
								
								_mm_mfence();

								// Spin until number of allocated channels is 0. TODO: This is uggly.
								// I think it can be removed and instead test if channels is 0 and if not
								// just take the next cleanup task, etc.
								while (client_interface_ptr->allocated_channels() != 0) {
									_mm_pause();
								}
								
								//if (client_interface_ptr->allocated_channels() == 0) {
									// Ready to release chunks.
									////std::cout << "Releasing chunks in client_interface " << n << "." << std::endl;

									bool release_chunk_result = shared.shared_chunk_pool()
									.release_clients_chunks(client_interface_ptr, 10000 /* milliseconds */);
								
									/// Release client_interface[n].

									client_interface_ptr->set_owner_id(owner_id::none);

#if defined (IPC_CLIENT_NUMBER_POOL_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
									bool release_client_number_res =
									shared.common_client_interface().release_client_number
									(n, &shared.client_interface(0), get_owner_id());
#else // !defined (IPC_CLIENT_NUMBER_POOL_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
									bool release_client_number_res =
									shared.common_client_interface().release_client_number
									(n, &shared.client_interface(0));
#endif // defined (IPC_CLIENT_NUMBER_POOL_USE_SMP_SPINLOCK_AND_WINDOWS_EVENTS_TO_SYNC)
								
									////std::cout << "release_client_number_res = " << release_client_number_res << std::endl;

									shared.common_client_interface().decrement_client_interfaces_to_clean_up();

									//std::cout << "client_interfaces_to_clean_up: "
									//<< shared.common_client_interface().client_interfaces_to_clean_up()
									//<< ", allocated_channels: "
									//<< client_interface_ptr->allocated_channels()
									//<< std::endl;
								//}
							}
						}
					}
					catch (shared_interface_exception&) {
						// The IPC monitor cleanup failed. Could not open the IPC database shared memory segment.
						log().error(SCERRCLEANUPTOOPENTHEDBIPCSHMSEG);
					}
				}
				else {
					////std::cout << "No segment_name_to_be_opened." << std::endl;
				}
				// If queue is empty, ::ResetEvent(ipc_monitor_clean_up_event);
			}
			break;
		case WAIT_TIMEOUT:
			// The IPC monitor was not notified. A timeout occurred.
			// This can't happen because the wait is infinite.
			break;
		case WAIT_FAILED:
			// The IPC monitor was not notified. An error occurred.
			log().error(SCERRIPCMONITORCLEANUPWAITFAILED);
			break;
		}
	}
}

void __stdcall monitor::apc_function(boost::detail::win32::ulong_ptr arg) {
	// Instead of accessing the object from here like this:
	// reinterpret_cast<monitor*>(arg)->do_registration();
	// return and continue in the switch case WAIT_IO_COMPLETION of the caller.
}

const owner_id& monitor::get_owner_id() const {
	return owner_id_;
}

#if defined (IPC_OWNER_ID_IS_32_BIT)
inline owner_id monitor::get_new_owner_id() {
	// The register_mutex_ is already locked by the caller.
	
	// The owner_id value type was changed from 64-bit to 32-bit. Therefore it
	// may wrap so this need to be handled. The range will be owner_id::id_field
	// except that owner_id::none (0) and owner_id::anonymous (1) is out of the
	// id range, since smp::spinlocks are unlocked with 0, and anonymously
	// locked with 1. Using the smp::spinlocks in robust mode requires locking
	// with an id in the range 2 to 2^30 -1. Bit 31 (MSB) in the owner_id is
	// used to flag clean-up so the range is about 31-bits.

	//--------------------------------------------------------------------------
	// At most max_number_of_monitored_processes +2 (0 and 1) IDs can be in use.
	for (std::size_t i = 0; i < max_number_of_monitored_processes +2; ++i) {
		++owner_id_counter_;
		owner_id_counter_ &= owner_id::id_field;

		if (owner_id_counter_ != owner_id::none
		&& owner_id_counter_ != owner_id::anonymous
		&& owner_id_counter_ != ipc_monitor_owner_id) {
			if (process_register_.find(owner_id_counter_) == process_register_.end()) {
				// This owner_id is not used by any monitored process.
				return owner_id_counter_;
			}
		}
	}

	// Getting here should be impossible. Returning owner_id::none to a
	// registering process indicates it could not register and be monitored.
	return owner_id::none;
}

#else // !defined (IPC_OWNER_ID_IS_32_BIT)
inline owner_id monitor::get_new_owner_id() {
	return ++owner_id_counter_;
}
#endif // defined (IPC_OWNER_ID_IS_32_BIT)

#if 0
void monitor::update_active_databases() {
	boost::mutex::scoped_lock register_lock(register_mutex_);
	
	for (process_register_type::iterator pos =
	process_register_.begin();
	pos != process_register_.end(); ++pos) {
		std::string database_name = segment_name_to_database_name
		(pos->second.get_segment_name());
		active_databases_.insert(database_name);
	}
}
#endif

bool monitor::insert_database_name(const std::string& database_name) {
	// The register_mutex_ is already locked by the caller.
	const boost::system_time timeout = boost::posix_time::microsec_clock
	::universal_time() +boost::posix_time::milliseconds(1000);
	
	boost::mutex::scoped_lock active_databases_lock(active_databases_mutex_,
	timeout);
	
	if (active_databases_lock.owns_lock()) {
		if (active_databases_.insert(database_name).second) {
			the_monitor_interface_->active_database_set().update(active_databases_);
			set_active_databases_updated_flag(true);
			active_databases_lock.unlock();
			active_databases_updated_.notify_one();
			return true;
		}
		else {
			return false;
		}
	}
	else {
		return false;
	}
}

bool monitor::erase_database_name(const std::string& database_name) {
	// The register_mutex_ is already locked by the caller.
	const boost::system_time timeout = boost::posix_time::microsec_clock
	::universal_time() +boost::posix_time::milliseconds(1000);
	
	boost::mutex::scoped_lock active_databases_lock(active_databases_mutex_,
	timeout);
	
	if (active_databases_lock.owns_lock()) {
		if (active_databases_.erase(database_name)) {
			the_monitor_interface_->active_database_set().update(active_databases_);
			set_active_databases_updated_flag(true);
			active_databases_lock.unlock();
			active_databases_updated_.notify_one();
			return true;
		}
		else {
			return false;
		}
	}
	else {
		return false;
	}
}

void monitor::update_active_databases_file() {
	do {
		boost::mutex::scoped_lock active_databases_lock
		(active_databases_mutex_);
		
		// Waiting for active_databases_ to be updated. . .
		active_databases_updated_.wait(active_databases_lock,
		boost::bind(&monitor::active_databases_updated_flag, this));
		
		// Try to open the active databases file in text mode.
		for (std::size_t retries = 6; retries > 0; --retries) {
			monitor_active_databases_file_.open(active_databases_file_path_,
			std::ios::out);
			
			if (!monitor_active_databases_file_.is_open()) {
				Sleep(500);
				continue;
			}
			
			break;
		}
		
		if (monitor_active_databases_file_.is_open()) {
			std::size_t i = 0;
			// Write the names of the active databases to the file.
			for (std::set<std::string>::iterator it = active_databases_.begin();
			it != active_databases_.end(); ++it) {
				monitor_active_databases_file_ << *it << '\n';
			}
			
			// Close the active databases file.
			monitor_active_databases_file_.close();
			set_active_databases_updated_flag(false);
		}
	} while (true);
}

void monitor::gotoxy(int16_t x, int16_t y) {
	COORD coord;
	coord.X = x;
	coord.Y = y;
	SetConsoleCursorPosition(GetStdHandle(STD_OUTPUT_HANDLE), coord);
}

#if defined (STARCOUNTER_CORE_ATOMIC_BUFFER_PERFORMANCE_COUNTERS)
void monitor::print_rate_with_precision(double rate) {
	// The rate is printed in one of the formats:
	//    0 - 9999 (<1E4)
	//  10k - 999k (<1E6)
	// 1.0M - 9.9M (<1E7)
	//  10M - 999M (or higher)
	if (rate >= 1E4) {
		if (rate >= 1E6) {
			if (rate >= 1E7) {
				// " 10M" - "999M" (or higher)
				std::cout.width(3);
				std::cout << int(rate / 1E6) << 'M';
			}
			else {
				// "1.0M" - "9.9M" (<1E7)
				std::cout.width(3);
				std::cout << std::fixed << std::setprecision(1)
				<< (double(rate) / 1E6) << 'M';
			}
		}
		else {
			// "  1k" - "999k" (<1E6)
			std::cout.width(3);
			std::cout << std::fixed << std::setprecision(0)
			<< int(double(rate) / 1E3) << 'k';
		}
	}
	else {
		// "   0" - "9999" (<1E4)
		std::cout.width(4);
		std::cout << int(rate);
	}
}
#endif // defined (STARCOUNTER_CORE_ATOMIC_BUFFER_PERFORMANCE_COUNTERS)

inline starcounter::log& monitor::log() {
	return log_;
}

inline const starcounter::log& monitor::log() const {
	return log_;
}

/// NOTE: Originally was ment to be able to show multiple databases at once,
/// which it can but then statistics are messed up completely. Only test with
/// one database running.
#if defined (IPC_MONITOR_SHOW_ACTIVITY)
void monitor::watch_resources() {
	// Vector of all shared interfaces.
	std::vector<boost::shared_ptr<shared_interface> > shared;
	shared.reserve(256);
	
	std::string segment_name;
	std::size_t retries;
	
	Sleep(1000);
	system("cls");

#if defined (STARCOUNTER_CORE_ATOMIC_BUFFER_PERFORMANCE_COUNTERS)
	boost::timer t;
	
	// stat[0] contains the most recently collected statistics, and
	// stat[1] contains the previously collected statistics.
	struct stat {
		stat()
		: timestamp(0) {}
		
		struct channel_statistics {
			channel_statistics()
			: in_pushed(0LL),
			in_popped(0LL),
			out_pushed(0LL),
			out_popped(0LL) {}
			
			int64_t in_pushed;
			int64_t in_popped;
			int64_t out_pushed;
			int64_t out_popped;
		} channel[channels];
		
		double timestamp;
	} stat[2];
	
	// Number of chunks in the in and out queue of the current channel,
	// that have been pushed and popped since start, recent statistics:
	int64_t in_pushed_recent;
	int64_t in_popped_recent;
	int64_t out_pushed_recent;
	int64_t out_popped_recent;
	
	// Number of chunks in the in and out queue of the current channel,
	// that have been pushed and popped since start, previous statistics:
	int64_t in_pushed_previous;
	int64_t in_popped_previous;
	int64_t out_pushed_previous;
	int64_t out_popped_previous;

	// Sum of number of chunks in all channels in and out queue that have been
	// pushed and popped recently since start:
	int64_t in_pushed_recent_sum;
	int64_t in_popped_recent_sum;
	int64_t out_pushed_recent_sum;
	int64_t out_popped_recent_sum;

	// Sum of number of chunks in all channels in and out queue that have been
	// pushed and popped recently since start:
	int64_t in_pushed_previous_sum;
	int64_t in_popped_previous_sum;
	int64_t out_pushed_previous_sum;
	int64_t out_popped_previous_sum;

	// The flow in a channel is measured by the rate chunks are passing through
	// per second.
	double rate;

	// Elapsed time is used to compute the number of push/pop per second.
	double elapsed_time;

	// Wait at least 1 ms before showing statistics.
	Sleep(1);

	//  If elapsed time is 0, division by 0 will happen.
	while (t.elapsed() == 0) {
		Sleep(1);
	}

#endif // defined (STARCOUNTER_CORE_ATOMIC_BUFFER_PERFORMANCE_COUNTERS)
	int active_segments_update_counter = 0; // Prevent checking too often.

	do {
		std::cout.flush();
		gotoxy(0, 0);

		if (active_segments_update_counter-- <= 0) {
			// Check if there is a new segment name to add.
			if (active_segments_update_.pop_back(&segment_name, 0, 100)) {
				retries = 0;
			
				while (true) {
					try {
						shared.push_back(boost::shared_ptr<shared_interface>
						(new shared_interface(segment_name, std::string(),
						pid_type(pid_type::no_pid))));
					
						break;
					}
					catch (shared_interface_exception&) {
						// Not possible to open yet. . .
						++retries;
					}
				}
			
				//std::cout << "Opened segment name: " << segment_name << '\n'
				//<< "After " << retries << " retries.\n";
				segment_name.clear();
			}

			active_segments_update_counter = 10;
		}
		
		// No sleep is needed because output to cmd on Windows takes so much time.
		// On a Linux machine a sleep might be needed.

		for (std::size_t i = 0; i < shared.size(); ++i) {
			shared_interface& the_shared = *shared[i];

            // TODO: Checking if process(with this shared_interface) is still active.
            if (the_shared.channel(0).is_to_be_released())
                continue;

			std::cout << "Segment: " << the_shared.get_segment_name() << '\n'
			<< "  free chunks:                  ";
			std::cout.width(4);
			std::cout << the_shared.shared_chunk_pool().size() << '\n';
			
			std::size_t schedulers = the_shared.common_scheduler_interface()
			.number_of_active_schedulers();
			uint32_t free_channels_sum = 0;

			for (std::size_t j = 0; j < schedulers; ++j) {
				uint32_t free_channels_in_scheduler_interface = the_shared
				.scheduler_interface(j).channel_number_queue().size();

				free_channels_sum += free_channels_in_scheduler_interface;
				
				std::cout << "  free channels in scheduler " << j << ": ";
				std::cout.width(4);
				std::cout << free_channels_in_scheduler_interface << '\n';
			}
			
			std::cout << "  free channels (total):        ";
			std::cout.width(4);
			std::cout << free_channels_sum << '\n';
			
			std::cout << "  free client interfaces:       ";
			std::cout.width(4);
			std::cout << the_shared.common_client_interface().client_number_pool().size() << '\n';
			
			/// WATCH OWNER_ID IN CLIENT_INTERFACE[0]
			//bool c = the_shared.client_interface(0).get_owner_id().get_clean_up();
			
			/// Debug: Watch the owned channels mask in client_interface[0..1]
			//for (std::size_t ci = 0; ci < 2; ++ci) {
			//	std::cout << "client_interface[" << ci << "].owned_channels_mask:\n";
			//	the_shared.client_interface(ci).get_resource_map().print_owned_channels_mask();
			//}

			//------------------------------------------------------------------
#if defined (STARCOUNTER_CORE_ATOMIC_BUFFER_PERFORMANCE_COUNTERS)
			std::cout << "\nChannels (rate, client/scheduler)    "
			<< "Elapsed time: " << stat[0].timestamp << " s";

			// Taking the timestamp before collecting statistics is probably better
			// than taking the timestamp after having collected the statistics,
			// because the timestamp might be more correct then.
			stat[0].timestamp = t.elapsed();
			
			// Elapsed time between stat[0] and stat[1].
			elapsed_time = stat[0].timestamp -stat[1].timestamp;
			
			// Copy recent statistics data [0] to previous statistics data [1].
			stat[1] = stat[0];
			
			// Collect new statistics data from each channel.
			for (std::size_t ch = 0; ch < channels; ++ch) {
				stat[0].channel[ch].in_pushed
				= the_shared.channel(ch).in.pushed_counter().get();
				
				stat[0].channel[ch].in_popped
				= the_shared.channel(ch).in.popped_counter().get();
				
				stat[0].channel[ch].out_pushed
				= the_shared.channel(ch).out.pushed_counter().get();
				
				stat[0].channel[ch].out_popped
				= the_shared.channel(ch).out.popped_counter().get();
			}
			
			// Sanity check.
			if (elapsed_time == 0) {
				// Avoid division by zero and don't print statistics.
				continue;
			}
			
			// Clear sums.
			in_pushed_recent_sum = 0LL;
			in_popped_recent_sum = 0LL;
			out_pushed_recent_sum = 0LL;
			out_popped_recent_sum = 0LL;
			in_pushed_previous_sum = 0LL;
			in_popped_previous_sum = 0LL;
			out_pushed_previous_sum = 0LL;
			out_popped_previous_sum = 0LL;

			// Right place?
			// Get number of chunks pushed and popped in the current
			// channels (ch) in and out queues, recently and previously.
			for (std::size_t ch = 0; ch < channels; ++ch) {
				in_pushed_recent = stat[0].channel[ch].in_pushed;
				in_popped_recent = stat[0].channel[ch].in_popped;
				out_pushed_recent = stat[0].channel[ch].out_pushed;
				out_popped_recent = stat[0].channel[ch].out_popped;
				in_pushed_previous = stat[1].channel[ch].in_pushed;
				in_popped_previous = stat[1].channel[ch].in_popped;
				out_pushed_previous = stat[1].channel[ch].out_pushed;
				out_popped_previous = stat[1].channel[ch].out_popped;
				
				// Add to sum.
				in_pushed_recent_sum += in_pushed_recent;
				in_popped_recent_sum += in_popped_recent;
				out_pushed_recent_sum += out_pushed_recent;
				out_popped_recent_sum += out_popped_recent;
				in_pushed_previous_sum += in_pushed_previous;
				in_popped_previous_sum += in_popped_previous;
				out_pushed_previous_sum += out_pushed_previous;
				out_popped_previous_sum += out_popped_previous;
			}

#else // !defined (STARCOUNTER_CORE_ATOMIC_BUFFER_PERFORMANCE_COUNTERS)
			std::cout << "\nChannels (client/scheduler):";
#endif // defined (STARCOUNTER_CORE_ATOMIC_BUFFER_PERFORMANCE_COUNTERS)

			for (std::size_t ch = 0; ch < channels; ++ch) {
				if (!(ch % 8)) {
					std::cout << "\n";
					std::cout.width(3);
					std::cout << ch << "-";
					std::cout.width(3);
					std::cout << ch +7 << ":  ";
				}

				// Reference used as shorthand.
				channel_type& this_channel = the_shared.channel(ch);

				//--------------------------------------------------------------
				// Calculate the flow in the channel, the channel flow at which
				// chunks passes through it. This is number of chunks
				// popped from the out queue per second.

				// First indicator: Rate (chunks/sec that are popped from the out
				// queue), or spaces if not available.
#if defined (STARCOUNTER_CORE_ATOMIC_BUFFER_PERFORMANCE_COUNTERS)
				
				//--------------------------------------------------------------
				// Calculate the channel flow as number of chunks that are
				// popped from this channels out queue per second.

				rate = double(out_pushed_recent -out_pushed_previous)
				/ elapsed_time;

				print_rate_with_precision(rate);

				//--------------------------------------------------------------
#else // !defined (STARCOUNTER_CORE_ATOMIC_BUFFER_PERFORMANCE_COUNTERS)
				std::cout << "    "; // Flow unknown.
#endif // defined (STARCOUNTER_CORE_ATOMIC_BUFFER_PERFORMANCE_COUNTERS)
				
				// Distance to next indicator.
				std::cout << " ";

				//--------------------------------------------------------------
				// Second indicator: If the channel is owned by a client or not.
				//   '.' = no client owns this channel,
				//   a digit = the number of the client that owns it,
				//   according to the client scan mask.
				if (this_channel.get_client_number() != -1) {
					if (the_shared.client_interface(this_channel
					.get_client_number()).is_channel_owner(ch)) {
						std::cout << this_channel.get_client_number();
					}
					else {
						std::cout << " ";
					}
				}
				else {
					std::cout << " ";
				}

				// Separator.
				std::cout << "/";

				//--------------------------------------------------------------
				// Third indicator: If the channel is owned by a scheduler or not.
				//   '.' = no scheduler owns this channel,
				//   a digit = the number of the scheduler that owns it,
				//   according to the scheduler scan mask.
				if (this_channel.get_scheduler_number() != -1) {
					if (the_shared.scheduler_interface(this_channel
					.get_scheduler_number()).is_channel_owner(ch)) {
						std::cout << this_channel.get_scheduler_number();
					}
					else {
						std::cout << " ";
					}
				}
				else {
					std::cout << " ";
				}

				#if 0
				// Separator.
				std::cout << "/";
				//--------------------------------------------------------------
				// Fourth char indicates if the channel is marked for release or not.
				//   '.' = the channel is not marked for release,
				//   a digit = the number of the scheduler that shall release it,
				//   according to the scheduler number indicated in the channel.
				if (this_channel.get_scheduler_number() != -1) {
					if (this_channel.is_to_be_released()) {
						std::cout << this_channel.get_scheduler_number();
					}
					else {
						std::cout << " ";
					}
				}
				else {
					std::cout << " ";
				}
				#endif

				//--------------------------------------------------------------
				// Two spaces separate channels information.
				std::cout << "  ";
			}

			std::cout << "\n";
		}

        Sleep(10);

	} while (true);
}

void monitor::remove_database_process_event(process_info::handle_type e) {
	for (std::size_t group = 0; group < database_process_event_groups; ++group)
	{
		for (uint32_t i = 0; i < events_per_group; ++i) {
			if (e == database_process_group_[group].event_[i]) {
				remove_database_process_event(group, i);
				return;
			}
		}
	}
}

#endif // defined (IPC_MONITOR_SHOW_ACTIVITY)

void monitor::remove_database_process_event(std::size_t group, uint32_t
event_code) {
	// Close the handle.
	CloseHandle(database_process_group_[group].event_[event_code]);
	
	// Copy the last element of the vector to index event_code.
	database_process_group_[group].event_[event_code]
	= database_process_group_[group].event_.back();
	
	// Remove the last element.
	database_process_group_[group].event_.pop_back();
}

void monitor::remove_client_process_event(process_info::handle_type e) {
	for (std::size_t group = 0; group < client_process_event_groups; ++group) {
		for (uint32_t i = 0; i < events_per_group; ++i) {
			if (e == client_process_group_[group].event_[i]) {
				remove_client_process_event(group, i);
				return;
			}
		}
	}
}

void monitor::remove_client_process_event(std::size_t group, uint32_t
event_code) {
	// Close the handle.
	CloseHandle(client_process_group_[group].event_[event_code]);
	
	// Copy the last element of the vector to index event_code.
	client_process_group_[group].event_[event_code]
	= client_process_group_[group].event_.back();
	
	// Remove the last element.
	client_process_group_[group].event_.pop_back();
}

void monitor::print_event_register() {
	boost::mutex::scoped_lock register_lock(register_mutex_);
	
	// Print the event register.
	std::cout <<
	//"----------------------------------------"
	//"----------------------------------------\n"
	"........................................"
	"........................................\n"
	<< "OID:\tHANDLE:\t\t\tPROCESS:\tPID:\tSEGMENT:" << std::endl;
	//<< "OID:\tPROCESS:\tPID:\tSEGMENT:" << std::endl;
	
	for (process_register_type::iterator pos = process_register_.begin();
	pos != process_register_.end(); ++pos) {
		std::cout << pos->first << "\t" << pos->second << std::endl;
	}
	
	std::cout << "Processes: " << process_register_.size() << std::endl;
}

void monitor::print_active_databases() {
	std::cout << "Active databases:\n";
	
	for (std::set<std::string>::iterator it = active_databases_.begin();
	it != active_databases_.end(); ++it) {
		std::cout << *it << '\n';
	}
	
	std::cout << std::endl;
}

std::string monitor::segment_name_to_database_name(const std::string&
segment_name) {
	std::string database_name = segment_name;
	std::size_t start = std::string(DATABASE_NAME_PREFIX).size() +1;
	std::size_t end = database_name.find_last_of("_");
	database_name = database_name.substr(start, end -start);
	return database_name;
}

} // namespace core
} // namespace starcounter

#endif // STARCOUNTER_CORE_IMPL_MONITOR_HPP
