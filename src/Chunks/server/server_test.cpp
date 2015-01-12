//
// server_test.cpp
//
// Copyright © 2006-2011 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

// Use -DBOOST_CB_DISABLE_DEBUG or -DNDEBUG flags
#define BOOST_CB_DISABLE_DEBUG

#include <cstdlib>
#include <iostream>
#include <string>
#include <boost/cstdint.hpp>
#include <boost/bind.hpp>
#include <boost/thread/thread.hpp>
#include <boost/interprocess/managed_shared_memory.hpp>
#include <boost/interprocess/allocators/allocator.hpp>
#include <boost/interprocess/exceptions.hpp>
#include <boost/shared_ptr.hpp>
#include <boost/call_traits.hpp>
#include <boost/bind.hpp>
#include <boost/utility.hpp>
#include <boost/noncopyable.hpp>
#include <boost/timer.hpp>

#define WIN32_LEAN_AND_MEAN
# include <windows.h>
# include <intrin.h>
#undef WIN32_LEAN_AND_MEAN

#include "../common/circular_buffer.hpp"
#include "../common/bounded_buffer.hpp"
#include "../common/chunk.hpp"
#include "../common/shared_chunk_pool.hpp"
#include "../common/channel.hpp"
#include "../common/scheduler_interface.hpp"
#include "../common/config_param.hpp"
#include "../common/macro_definitions.hpp"
//#include "../common/database_number.hpp"
#include "initialize.hpp"
#include "server.hpp"
#include "scheduler.hpp"

#ifdef USE_SCCOREERR_LIBRARY
# include <sccoreerr.h>
#else
# include <scerrres.h>
#endif

#define _E_UNSPECIFIED SCERRUNSPECIFIED
#define _E_INVALID_SERVER_NAME SCERRINVALIDSERVERNAME

#if 0
So I understand we must have two (or one?) schedulers per core and make the client
thred look up the scheduler attached to the same core it is running on. Or can we
suggest where a client thread is running? If the IIS does not allow us to pick
where client threads are run, the above method should allow us to pick the right
scheduler instead.

A simple approach would be this: Assuming that a server has 12 cores and 24 threads
we create a server with 12 schedulers and set server process affinity to 0, 2, 4 etc
so all schedulers is dedicated to a specific core and hardware thread. Then we set
ideal processor (soft affinity) on all threads on a specific scheduler to a specific
core.

On the web server or whatever running on the machine we set process affinity to
1, 3, 5 etc so again all cores are covered but the other hardware thread is used.

When a web server thread creates a connection we check the current processor for
the thread (GetCurrentProcessorNumber). If it is 1 we connect to scheduler attached
to 0, if 2 we attach to sheduler 1 and so on. This will make it likely but not
guaranteed that both threads talking runs on the same core (but on different
hardware threads). Not guaranteed because of the soft affinity on the database
management server and the fact that threads can migrate.

Since in this case the database server will often not be working when web server
is working and vice verse this model should also make close to best possible use
of the hardware (all cores, although not all threads, will be busy at peak load).
Should work well I think.

(We might not want to use all the cores on the machine for this of course but it
doesn't matter for this example).
#endif

EXTERN_C unsigned long sc_initialize_io_service(
    const char* name,
    const char* server_name,
    unsigned long port_count,
    bool is_system,
    unsigned int num_shm_chunks,
    unsigned char gateway_num_workers);

unsigned long sc_initialize_io_service(
    const char* name,
    const char* server_name,
    unsigned long port_count,
    bool is_system,
    unsigned int num_shm_chunks,
    unsigned char gateway_num_workers)
{
    try {
        if (strlen(name) > 127) return _E_INVALID_SERVER_NAME;

        // Initialize the shared memory segments and all objects in it.
        unsigned long dr = starcounter::core::initialize(name, server_name, port_count, is_system, num_shm_chunks, gateway_num_workers);
        return dr;
    }
    catch (...) {
        std::cerr << "main: unknown exception thrown" << std::endl;
        return _E_UNSPECIFIED;
    }
}
