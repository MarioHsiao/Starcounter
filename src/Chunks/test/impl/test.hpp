//
// impl/test.hpp
// IPC test
//
// Copyright � 2006-2012 Starcounter AB. All rights reserved.
// Starcounter� is a registered trademark of Starcounter AB.
//
// Implementation of class test.
//

#ifndef STARCOUNTER_INTERPROCESS_COMMUNICATION_IMPL_TEST_HPP
#define STARCOUNTER_INTERPROCESS_COMMUNICATION_IMPL_TEST_HPP

// Implementation

namespace starcounter {
namespace interprocess_communication {

test::test(int argc, wchar_t* argv[])
: active_databases_updates_event_(),
all_pushed_(0),
all_popped_(0)
#if defined (STARCOUNTER_CORE_ATOMIC_BUFFER_PERFORMANCE_COUNTERS)
, statistics_thread_()
#endif // defined (STARCOUNTER_CORE_ATOMIC_BUFFER_PERFORMANCE_COUNTERS)
{
	///=========================================================================
	/// First argument: <server name>, for example "PERSONAL" or "SYSTEM".
	/// Second argument: <number of worker threads>, for example "4".
    /// Third argument: <timeout in milliseconds>, for example "180000".
    /// Fourth argument: <database name>, for example "myDatabase".
    ///
    /// For all tests, each worker will connect to each scheduler in each
    /// database.
    /// 
	/// Example 1:
    /// To connect to database1 under the PERSONAL server, starting 1 worker
    /// and running the IPC test for 18000 ms:
	/// >sc_ipc_test.exe PERSONAL 1 18000 database1
    ///
    /// Example 2:
    /// To connect to database8 and database5 under the PERSONAL server,
    /// starting 3 worker threads and running the IPC test for 240000 ms:
	/// >sc_ipc_test.exe PERSONAL 3 240000 database8 database5
	///=========================================================================
	number_of_shared_ = 0;

	if (argc >= 6) {
        //----------------------------------------------------------------------
		// First argument: <server name>
        // Convert it from wide-character string to multibyte string.
		char server_name_buffer[server_name_size];
		std::wcstombs(server_name_buffer, argv[1], server_name_size -1);
		server_name_ = server_name_buffer;

        //----------------------------------------------------------------------
        // Second argument: <number of worker threads> in the IPC test process.
        // Convert it from wide-character string to multibyte string.
        char number_of_workers_buffer[16];
        std::wcstombs(number_of_workers_buffer, argv[2], 16 -1);

        // Instantiate the number of workers requested.
        size_t workers = std::atoi(number_of_workers_buffer);

        for (size_t w = 0; w < workers; ++w) {
            worker_.push_back(new worker());
        }

        //----------------------------------------------------------------------
        // Third argument: <timeout in milliseconds>
        // Convert it from wide-character string to multibyte string.
        char timeout_buffer[16];
        std::wcstombs(timeout_buffer, argv[3], 16 -1);

        // Instantiate the number of workers requested.
        timeout_ = std::atoi(timeout_buffer);
        
        //----------------------------------------------------------------------
		// Fourth argument: <database name>
        // Convert it from wide character string to multibyte string.
		char database_name_buffer[segment_name_size];

        //----------------------------------------------------------------------
        // Fifth argument: <number of schedulers to use in database>
        // Convert it from wide-character string to multibyte string.
        char num_schedulers_buffer[16];
        std::wcstombs(num_schedulers_buffer, argv[4], 16 -1);

        // Instantiate the number of workers requested.
        num_schedulers_ = std::atoi(num_schedulers_buffer);

		std::string database_name;
		std::string db_shm_params_name;
		std::vector<std::string> ipc_shm_params_name;

        // This loop is obsolete since the IPC test can only connect to one
        // database. But it works so I leave it as is for now, because the
        // IPC test shall be able to connect to multiple databases so this
        // loop may be usable later if that is fixed.
		for (std::size_t i = 5; i < argc; ++i) {
			std::wcstombs(database_name_buffer, argv[i], segment_name_size -1);
			database_name = database_name_buffer;
			
			db_shm_params_name = server_name_ +"_"
			+boost::to_upper_copy<std::string>(database_name);
			
			ipc_shm_params_name.push_back(db_shm_params_name);
		}

		if (ipc_shm_params_name.size() > 0) {
			initialize(ipc_shm_params_name);
			
			for (std::size_t i = 0; i < number_of_shared_; ++i) {
				active_schedulers_[i] = shared_[i].common_scheduler_interface()
				.number_of_active_schedulers();
			}
		}
		else {
			std::wcout << "error: no database name(s) entered after server name." << std::endl;
		}
	}
	else {
		std::wcout << "Please enter five arguments in this order:\n"
		"First argument: <server name>, for example \"PERSONAL\" or \"SYSTEM\".\n"
		"Second argument: <number of worker threads>, for example \"4\".\n"
		"Third argument: <timeout in milliseconds>, for example \"10000\".\n"
		"Fourth argument: <database name>, for example \"myDatabase\".\n"
		"Fifth argument: <number of schedulers>, for example \"3\".\n"
        << std::endl;
		
		throw test_exception(0 /* TODO: Starcounter error code. SCERRINVALIDARGUMENTSTOIPCTEST */);
	}
}

test::~test() {
	// Join worker threads.
	for (std::size_t i = 0; i < workers(); ++i) {
		get_worker(i).join();
	}

#if defined (STARCOUNTER_CORE_ATOMIC_BUFFER_PERFORMANCE_COUNTERS)
	// Join the statistics_thread.
	statistics_thread_.join();
#endif // defined (STARCOUNTER_CORE_ATOMIC_BUFFER_PERFORMANCE_COUNTERS)
}

void test::initialize(const std::vector<std::string>& ipc_shm_params_name) {
	///=========================================================================
	/// "6" Open the database shared memory parameters.
	///=========================================================================
	using namespace starcounter::core;
	bool registered = false;

	// Construct the string containing the name of the database IPC shared
	// memory parameters shared memory object.
	
	for (std::size_t n = 0; n < ipc_shm_params_name.size(); ++n) {
		// Name of the shared_memory_object containing the sequence_number to be
		// appended to the name of the IPC shared memory segment. The format is
		// <DATABASE_NAME_PREFIX>_<DATABASE_NAME>_0.
		char database_shared_memory_parameters_name[sizeof(DATABASE_NAME_PREFIX)
		+segment_name_size +4 /* two delimiters, '0', and null */];
		
		std::size_t length;
		
		// Construct the database_shared_memory_parameters_name. The format is
		// <DATABASE_NAME_PREFIX>_<DATABASE_NAME>_0
		if ((length = _snprintf_s(database_shared_memory_parameters_name,
		_countof(database_shared_memory_parameters_name),
		sizeof(database_shared_memory_parameters_name) -1 /* null */, "%s_%s_0",
		DATABASE_NAME_PREFIX, ipc_shm_params_name[n].c_str())) < 0) {
			// Failed to construct the database_shared_memory_parameters_name.
			throw test_exception(SCERRCCONSTRUCTDBSHMPARAMNAME);
		}
		
		database_shared_memory_parameters_name[length] = '\0';
		
		// Open the database shared memory parameters file and obtains a pointer to
		// the shared structure.
		database_shared_memory_parameters_ptr db_shm_params
		(database_shared_memory_parameters_name);
		
		char monitor_interface_name[segment_name_size
		+sizeof(MONITOR_INTERFACE_SUFFIX) +2 /* delimiter and null */];
		
		///=========================================================================
		/// "7" Construct the monitor_interface_name.
		///=========================================================================

		// Construct the server_name. The format is
		// <SERVER_NAME>_<MONITOR_INTERFACE_SUFFIX>.
		if ((length = _snprintf_s(monitor_interface_name,
		_countof(monitor_interface_name), sizeof(monitor_interface_name), "%s_%s",
		db_shm_params->get_server_name(), MONITOR_INTERFACE_SUFFIX)) < 0) {
			// Buffer overflow.
			throw test_exception(SCERRCCONSTRMONITORINTERFACENAME);
		}
		
		monitor_interface_name[length] = '\0';
		
		///=========================================================================
		/// "8" Register with the same IPC monitor as the database, if not already.
		///=========================================================================
		
		if (!registered) {
			// Get monitor_interface_ptr for monitor_interface_name.
			the_monitor_interface().init(monitor_interface_name);
			
			//--------------------------------------------------------------------------
			// Send registration request to the monitor and try to acquire an owner_id.
			// Without an owner_id we can not proceed and have to exit.
			
			// Get process id and store it in the monitor_interface.
			pid_.set_current();
			uint32_t error_code;
			
			// Try to register this client process pid. Wait up to 10000 ms.
			if ((error_code = the_monitor_interface()->register_client_process(pid_,
			owner_id_, 10000/*ms*/)) != 0) {
				// Failed to register this client process pid.
				throw test_exception(error_code);
			}

			registered = true;
		}

		// Threads in this process can now acquire resources.

		///=========================================================================
		/// "9" Open the database shared memory segment.
		///=========================================================================
		
		if (db_shm_params->get_sequence_number() == 0) {
			// Cannot open the database shared memory segment, because it is not
			// initialized yet.
			throw test_exception(SCERRCLIENTOPENDBSHMSEGMENT);
		}
		
		// The database shared memory segment is initialized.
		
		//--------------------------------------------------------------------------
		// Name of the database shared memory segment.
		char segment_name[sizeof(DATABASE_NAME_PREFIX) +segment_name_size
		+13 /* two delimiters, '4294967295', and null */];
		
		// Construct the name of the database shared memory segment. The format is
		// <DATABASE_NAME_PREFIX>_<DATABASE_NAME>_<SEQUENCE_NUMBER>
		if ((length = _snprintf_s(segment_name, _countof(segment_name),
		sizeof(segment_name) -1 /* null */, "%s_%s_%u", DATABASE_NAME_PREFIX,
		ipc_shm_params_name[n].c_str(), db_shm_params->get_sequence_number())) < 0) {
			throw test_exception(SCERRCCONSTRDBSHMSEGMENTNAME);
		}
		
		segment_name[length] = '\0';
		segment_name_[n] = segment_name;
		
		///=========================================================================
		/// Construct a shared_interface.
		///=========================================================================

		shared_[n].init(segment_name, monitor_interface_name, pid_, owner_id_);
		++number_of_shared_;

		for (std::size_t i = 0; i < workers(); ++i) {
			get_worker(i).set_segment_name(segment_name_[n])
			.set_monitor_interface_name(monitor_interface_name_)
			.set_pid(pid_)
			.set_owner_id(owner_id_);
		}
	}
}

inline test& test::set_segment_name(std::size_t n, std::string& segment_name) {
	segment_name_[n] = segment_name;
	return *this;
}

inline std::string test::get_segment_name(std::size_t n) const {
	return segment_name_[n];
}

inline test& test::set_monitor_interface_name(const std::string& monitor_interface_name) {
	monitor_interface_name_ = monitor_interface_name;
	return *this;
}

inline std::string test::get_monitor_interface_name() const {
	return monitor_interface_name_;
}

inline test& test::set_pid(const pid_type pid) {
	pid_ = pid;
	return *this;
}

inline pid_type test::get_pid() const {
	return pid_;
}

inline test& test::set_owner_id(const owner_id oid) {
	owner_id_ = oid;
	return *this;
}

inline owner_id test::get_owner_id() const {
	return owner_id_;
}

void test::run() {
	// Start workers.
	for (std::size_t i = 0; i < workers(); ++i) {
		// Set worker parameters.
		get_worker(i)
		.set_segment_name(segment_name_[0])
		.set_monitor_interface_name(monitor_interface_name_)
		.set_pid(pid_)
		.set_owner_id(owner_id_)
		.set_active_schedulers(num_schedulers_/*active_schedulers_[0]*/)
		.set_worker_number(i)
		.set_shared_interface();

		// Start the worker - starts the workers thread.
		get_worker(i).start();
	}
	
#if defined (STARCOUNTER_CORE_ATOMIC_BUFFER_PERFORMANCE_COUNTERS)
    uint32_t interval_time_milliseconds = 200;

	// Start statistics thread
# if defined(IPC_MONITOR_USE_STARCOUNTER_CORE_THREADS)
	std::pair<test*,std::size_t>* arg = new std::pair<test*,std::size_t>
	(this, interval_time_milliseconds); // TODO: Fix this leak.
	
	statistics_thread_.create((thread::start_routine_type)
	&test::show_statistics, arg);
# else // !defined(IPC_MONITOR_USE_STARCOUNTER_CORE_THREADS)
	statistics_thread_ = boost::thread(&test::show_statistics, this,
	interval_time_milliseconds); 
# endif // defined(IPC_MONITOR_USE_STARCOUNTER_CORE_THREADS)
#endif // defined (STARCOUNTER_CORE_ATOMIC_BUFFER_PERFORMANCE_COUNTERS)
}

void test::stop_worker(std::size_t n) {
	// Stop worker[n]
	get_worker(n).set_state(worker::stopped);
	get_worker(n).join();
    all_pushed_ += get_worker(n).pushed();
    all_popped_ += get_worker(n).popped();
}

void test::stop_all_workers() {
	// Stop the workers
	for (std::size_t i = 0; i < workers(); ++i) {
		get_worker(i).set_state(worker::stopped);
		get_worker(i).join();
        std::wcout << "Stopped worker[" << i << "]" << std::endl;
	}
}

void test::open_active_databases_updated_event() {
	// Number of characters in the multibyte string after being converted.
	std::size_t length;

	// Construct the active_databases_updated_event_name.
	char active_databases_updated_event_name[active_databases_updated_event_name_size];

	// Format: "Local\<server_name>_ipc_monitor_cleanup_event".
	// Example: "Local\PERSONAL_ipc_monitor_cleanup_event"
	if ((length = _snprintf_s(active_databases_updated_event_name, _countof
	(active_databases_updated_event_name), active_databases_updated_event_name_size
	-1 /* null */, "Local\\%s_"ACTIVE_DATABASES_UPDATED_EVENT, server_name_.c_str())) < 0) {
		throw test_exception(SCERRFORMATACTIVEDBUPDATEDEVNAME);
	}
	active_databases_updated_event_name[length] = '\0';

	wchar_t w_active_databases_updated_event_name[active_databases_updated_event_name_size];

	/// TODO: Fix insecure
	if ((length = mbstowcs(w_active_databases_updated_event_name,
	active_databases_updated_event_name, segment_name_size)) < 0) {
		// Failed to convert active_databases_updated_event_name to multi-byte string.
		throw test_exception(SCERRCONVERTACTIVEDBUPDATEDEVMBS);
	}
	w_active_databases_updated_event_name[length] = L'\0';

	// Open the active_databases_updated_event_name.
	if ((active_databases_updates_event() = ::OpenEvent(SYNCHRONIZE | EVENT_MODIFY_STATE,
	FALSE, w_active_databases_updated_event_name)) == NULL) {
		// Failed to open the active_databases_updated_event.
		throw test_exception(SCERROPENACTIVEDBUPDATEDEV);
	}
}

#if defined (STARCOUNTER_CORE_ATOMIC_BUFFER_PERFORMANCE_COUNTERS)
int test::plot_dots(double rate) {
	int dots = 0;

	// If there is no flow, no dots are plotted.
	if (rate > 0) {
		//int dots = int(std::log10(rate));
		int dots = int(std::log(rate) / std::log(2));
		int c = int(std::log10(rate));

		// If there is some flow, at least one dot is plotted.
		if (dots < 1) {
			dots = 1;
		}

		// Plot dots
		for (int i = 0; i < dots; ++i) {
			std::cout << char(c +'A');
		}
	}

	std::cout << "\n";
	return dots;
}

void test::print_rate(double rate) {
	if (rate >= 1000) {
		std::cout.width(10);
		std::cout << int64_t(rate / 1000) << "k/s ";
	}
	else {
		std::cout.width(11);
		std::cout << int64_t(rate) << "/s ";
	}
}

#if defined(IPC_MONITOR_USE_STARCOUNTER_CORE_THREADS)
void test::show_statistics(std::pair<test*,std::size_t> arg) {
	test* test = arg.first;
	uint32_t interval_time_milliseconds = arg.second;
#else // !defined(IPC_MONITOR_USE_STARCOUNTER_CORE_THREADS)
void test::show_statistics(uint32_t interval_time_milliseconds) {
	test* test = this;
#endif // defined(IPC_MONITOR_USE_STARCOUNTER_CORE_THREADS)
	uint32_t duration_time_milliseconds = 6000000;
	system("cls");
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
	
	///=========================================================================
	/// Periodically show statistics until duration_time_milliseconds have
	/// elapsed, or the database state is not normal. Then exit.
	///=========================================================================

	while (1000 * t.elapsed() < double(duration_time_milliseconds)
	&& test->shared(0).common_client_interface().database_state() ==
	common_client_interface_type::normal) {
		test->gotoxy(0, 0);
		//std::cout << "\n===================================================\n"
		std::cout << "Elapsed time: " << stat[0].timestamp << " s\n";
		
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
			= test->shared(0).channel(ch).in.pushed_counter().get();
			
			stat[0].channel[ch].in_popped
			= test->shared(0).channel(ch).in.popped_counter().get();
			
			stat[0].channel[ch].out_pushed
			= test->shared(0).channel(ch).out.pushed_counter().get();
			
			stat[0].channel[ch].out_popped
			= test->shared(0).channel(ch).out.popped_counter().get();
		}
		
		// Sanity check.
		if (elapsed_time == 0) {
			goto bypass_statistics_output;
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

		//----------------------------------------------------------------------
		// Output statistics for each workers channel:
		for (std::size_t i = 0; i < workers; ++i) {
			for (std::size_t j = 0; j < test->get_worker(i).num_channels_; ++j) {
				// Get channel index.
				std::size_t ch = test->get_worker(i).channel_[j];

				// Get number of chunks pushed and popped in the current
				// channels (ch) in and out queues, recently and previously.
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
				
                std::cout << "Flow in channel " << ch << " (worker " << i
				<< " <===> scheduler "
				<< test->shared(0).channel(ch).get_scheduler_number() << "):\n";
				
				//--------------------------------------------------------------
				std::cout << "  in pushed:  ";
				std::cout.width(20);
				std::cout << in_pushed_recent << " at ";
				rate = double(in_pushed_recent -in_pushed_previous)
				/ elapsed_time;
				rate = int64_t(rate);
				
				test->print_rate(rate);
				std::cout << '\n';
				//plot_dots(rate);

				//--------------------------------------------------------------
				std::cout << "  in popped:  ";
				std::cout.width(20);
				std::cout << in_popped_recent << " at ";
				rate = double(in_popped_recent -in_popped_previous)
				/ elapsed_time;

				test->print_rate(rate);
				std::cout << '\n';
				//plot_dots(rate);

				//--------------------------------------------------------------
				std::cout << "  out pushed: ";
				std::cout.width(20);
				std::cout << out_pushed_recent << " at ";
				rate = double(out_pushed_recent -out_pushed_previous)
				/ elapsed_time;

				test->print_rate(rate);
				std::cout << '\n';
				//plot_dots(rate);

				//--------------------------------------------------------------
				std::cout << "  out popped: ";
				std::cout.width(20);
				std::cout << out_popped_recent << " at ";
				rate = double(out_popped_recent -out_popped_previous)
				/ elapsed_time;

				test->print_rate(rate);
				std::cout << '\n';
				//plot_dots(rate);
			}
		}
		
		//----------------------------------------------------------------------
		// Output statistics for the flow in all channels summed up:
		std::cout << "Flow in all channels summed up:\n";

		//----------------------------------------------------------------------
		std::cout << "  in pushed:  ";
		std::cout.width(20);
		std::cout << in_pushed_recent_sum << " at ";
		rate = double(in_pushed_recent_sum -in_pushed_previous_sum)
		/ elapsed_time;
		
		test->print_rate(rate);
		std::cout << '\n';
		//plot_dots(rate);

		//----------------------------------------------------------------------
		std::cout << "  in popped:  ";
		std::cout.width(20);
		std::cout << in_popped_recent_sum << " at ";
		rate = double(in_popped_recent_sum -in_popped_previous_sum)
		/ elapsed_time;

		test->print_rate(rate);
		std::cout << '\n';
		//plot_dots(rate);

		//----------------------------------------------------------------------
		std::cout << "  out pushed: ";
		std::cout.width(20);
		std::cout << out_pushed_recent_sum << " at ";
		rate = double(out_pushed_recent_sum -out_pushed_previous_sum)
		/ elapsed_time;
		
		test->print_rate(rate);
		std::cout << '\n';
		//plot_dots(rate);

		//----------------------------------------------------------------------
		std::cout << "  out popped: ";
		std::cout.width(20);
		std::cout << out_popped_recent_sum << " at ";
		rate = double(out_popped_recent_sum -out_popped_previous_sum)
		/ elapsed_time;
		
		test->print_rate(rate);
		std::cout << '\n';
		//plot_dots(rate);

		//----------------------------------------------------------------------
bypass_statistics_output:
		// Got here because the elapsed_time is 0 and can not use it to compute with
		// (division by zero). This should never happen. Try again.
		
		// Wait a moment before showing statistics again...
		// Clear the stringstream content.
		//gotoxy(0, 0);
		::Sleep(interval_time_milliseconds);
	}
	
    return;
}
#endif // defined (STARCOUNTER_CORE_ATOMIC_BUFFER_PERFORMANCE_COUNTERS)

} // namespace interprocess_communication
} // namespace starcounter

#endif // STARCOUNTER_INTERPROCESS_COMMUNICATION_IMPL_TEST_HPP
