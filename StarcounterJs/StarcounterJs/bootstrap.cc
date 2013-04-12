// Starcounter Global Memory - A few globals pertaining to the Starcounter sccode process
// Starcounter Kernel Memory - Iterator och andra icke databas object
// Starcounter Storage Memory - Shared memory with database + image and other database related stuff.

// Attached worker thread is allowed read database memory
// detached is not 
// There is always one attached worker thread at any time in any single scheduler
// Free threads. Non scheduled threads.

#include <starcounter-host.h>

#define _OUTPUT_DIRECTORY L"c:/temp/testapp1/data"
#define _DATABASE_DIRECTORY _OUTPUT_DIRECTORY
#define _TEMP_DIRECTORY _OUTPUT_DIRECTORY
#define _COMPILER_PATH L"c:/sc/level1/bin/debug/mingw/bin/x86_64-w64-mingw32-gcc.exe"

//#define _SCHEDULER_COUNT 1
#define _SCHEDULER_COUNT 4
#define _APPLICATION_NAME L"TEST"
#define _SERVER_NAME L"PERSONAL"


extern void *_vm_commit(void *pmem, size_t size, int32_t except);



   

static uint32_t __setup(struct _host **pphost);
static uint32_t __start(void *hsched);
static uint32_t __stop(void *hsched);
static uint32_t __cleanup();
static void __log_critical(uint64_t hlogs, uint32_t r);




///////////////////////////////////////////////////////////////////////////////


static VOID __stdcall __thread_enter(VOID *h, BYTE cpun, VOID *p, BOOL init);
static VOID __stdcall __thread_leave(VOID *h, BYTE cpun, VOID *p, DWORD yr);
static VOID __stdcall __thread_start(VOID *h, BYTE cpun, VOID *p, DWORD sf);
static VOID __stdcall __thread_reset(VOID *h, BYTE cpun, VOID *p);
static BOOL __stdcall __thread_yield(VOID *h, BYTE cpun, VOID *p, DWORD yr);
static VOID __stdcall __sched_background_task(VOID *h, BYTE cpun, VOID *p);
static VOID __stdcall __sched_clock_tick(VOID *h, BYTE cpun, VOID *p, DWORD psec);
static BOOL __stdcall __sched_idle(VOID *h, BYTE cpun, VOID *p);
static VOID __stdcall __sched_wait(VOID *h, BYTE cpun, VOID *p);
static VOID __stdcall __alert_stall(VOID *h, VOID *p, BYTE cpun, DWORD sr, DWORD sc);
static VOID __stdcall __alert_lowmem(VOID *h, VOID *p, DWORD lr);

static void __on_new_schema(uint64_t generation);
static uint32_t on_no_transaction();

static void __critical_log_handler(void *c, const wchar_t *message);


VOID __stdcall __thread_enter(VOID *h, BYTE cpun, VOID *p, BOOL init)
{
	uint32_t r;
	r = SCAttachThread(cpun, init);
	_SC_ASSERT(!r);
}

VOID __stdcall __thread_leave(VOID *h, BYTE cpun, VOID *p, DWORD yr)
{
	uint32_t r;
	r = SCDetachThread(yr);
	_SC_ASSERT(!r);
}

VOID __stdcall __thread_start(VOID *h, BYTE cpun, VOID *p, DWORD sf)
{
	uint32_t r;
	CM2_TASK_DATA data;

	// TODO:
	
	for (;;)
	{
		r = cm2_standby(h, &data);
		_SC_ASSERT(!r);
		switch (data.Type)
		{
		case CM2_TYPE_RELEASE: return;
		case CM2_TYPE_REQUEST: break;
		}
	}
}

VOID __stdcall __thread_reset(VOID *h, BYTE cpun, VOID *p)
{
	uint32_t r;
	r = SCResetThread();
	_SC_ASSERT(!r);
}

BOOL __stdcall __thread_yield(VOID *h, BYTE cpun, VOID *p, DWORD yr) { return 1; }

VOID __stdcall __sched_background_task(VOID *h, BYTE cpun, VOID *p)
{
	uint32_t r;
	r = SCBackgroundTask();
	_SC_ASSERT(!r);
}

VOID __stdcall __sched_clock_tick(VOID *h, BYTE cpun, VOID *p, DWORD psec)
{
	struct _host *phost;
	sccoredb_advance_clock(cpun);
	if (cpun == 0)
	{
		phost = (struct _host *)p;
		mh4_menv_trim_cache(phost->hmenv, 1);
	}
}

BOOL __stdcall __sched_idle(VOID *h, BYTE cpun, VOID *p)
{
	uint32_t r;
	int32_t call_again_if_still_idle;
	r = SCIdleTask(&call_again_if_still_idle);
	_SC_ASSERT(!r);
	return call_again_if_still_idle;
}

VOID __stdcall __sched_wait(VOID *h, BYTE cpun, VOID *p) { }

VOID __stdcall __alert_stall(VOID *h, VOID *p, BYTE cpun, DWORD sr, DWORD sc) { }

VOID __stdcall __alert_lowmem(VOID *h, VOID *p, DWORD lr)
{
	uint32_t r;
	uint8_t cpun;
	struct _host *phost;
            
	r = SCLowMemoryAlert(lr);
	_SC_ASSERT(!r);

	r = cm3_get_cpun(0, &cpun);

	if (r == 0)
	{
		return; // This is a worker thread.
	}

	// This is the monitor thread.
	if (lr == CM5_LOWMEM_REASON_PHYSICAL_MEMORY)
	{
		phost = (struct _host *)p;
		mh4_menv_alert_lowmem(phost->hmenv);
		mh4_menv_trim_cache(phost->hmenv, 0);
	}
}

void __on_new_schema(uint64_t generation) { }

uint32_t on_no_transaction() { return 0; }


void __critical_log_handler(void *c, const wchar_t *message)
{
	struct _host *phost;

	phost = (struct _host *)c;
	sccorelog_kernel_write_to_logs(phost->hlogs, SC_ENTRY_CRITICAL, 0, message);
	sccorelog_flush_to_logs(phost->hlogs);
}


///////////////////////////////////////////////////////////////////////////////


static uint32_t __configure_memory(void *mem128, uint64_t *phmenv);
static uint32_t __configure_logging(uint64_t hmenv, uint64_t *phlogs);
static uint32_t __configure_scheduler(void* mem, uint64_t hmenv, uint32_t scheduler_count, struct _host *phost, void **phsched);
static uint32_t __configure_database();
static uint32_t __connect_database(void* hsched, uint64_t hmenv, uint64_t hlogs);


uint32_t __setup(struct _host **pphost)
{
   // It is the task of the host (us) to allocate the memory needed for the Starcounter global variables.
   // How much memory is needed depends on the number of scheduleras. The reason is that we want to host
   // globals and the Starcounter globals to be on the same page.
	uint32_t scheduler_count;
	uint32_t mem_size;
	uint8_t *mem;
	struct _host *phost;
	uint32_t r;

	scheduler_count = _SCHEDULER_COUNT; // Number of schedulers, typically  2 per Core

	mem_size =
		64 +  // Host globals         // TODO! DEFINES
		128 + // Kernel memory setup. // TODO! DEFINES

		// In order for per scheduler memory to be aligned to page boundary we
		// align 512 bytes for the above.

		(512 - 128 - 64) + // TODO! DEFINES

		// Scheduler: 1024 shared + 512 per scheduler.

		1024 + // TODO! DEFINES
		(scheduler_count * 512) + // TODO! DEFINES

		0;
	mem = (uint8_t *)_vm_commit(0, mem_size, 0);
	if (!mem) return SCERROUTOFMEMORY;

	*pphost = phost = (struct _host *)mem;
	memset(phost, 0, sizeof(struct _host));
	mem += 64;

	phost->hevent = CreateEvent(0, 1, 0, 0);

	r = __configure_memory(mem, &phost->hmenv);
	if (r) return r;
	mem += 128; // TODO! DEFINES

	r = __configure_logging(phost->hmenv, &phost->hlogs);
	if (r) {
      wprintf(L"Failed to initialize event log");
      return r;
   }

	_SetCriticalLogHandler(__critical_log_handler, phost);

	mem += (512 - 128 - 64); // TODO! DEFINES

	r = __configure_scheduler(mem, phost->hmenv, scheduler_count, phost, &phost->hsched);
	if (r) {
      wprintf(L"Failed to initialize scheduler");
      return r;
   }
	mem += (1024 + (scheduler_count * 512)); // TODO! DEFINES

	r = __configure_database();
	if (r) {
      wprintf(L"Failed to configure database");
      return r;
   }

	r = __connect_database(phost->hsched, phost->hmenv, phost->hlogs);
	if (r) {
      wprintf(L"Failed to configure database");
      return r;
   }

	return 0;
}


uint32_t __configure_memory(void *mem128, uint64_t *phmenv)
{
	uint32_t slabs = (0xFFFFF000 - 4096) / 4096;  // 4 GB - 4 KB
	*phmenv = mh4_menv_create(mem128, slabs); 
               // Menv = Kernel Memory. Environment. Keeps iterators and other non database memory state and objects
               // "sunflower" = kernel memory .
	if (*phmenv) return 0;
	return SCERROUTOFMEMORY;
}

uint32_t __configure_logging(uint64_t hmenv, uint64_t *phlogs) // event log
{
	uint32_t r;

	r = sccorelog_init(hmenv);
	if (r) return r;

	r = sccorelog_connect_to_logs(
		_APPLICATION_NAME,
		0,
		phlogs
		);
	if (r) return r;

	r = sccorelog_bind_logs_to_dir(*phlogs, _OUTPUT_DIRECTORY); // sccorelog is the event log
	if (r) {
      wprintf( L"Failed to use directory " );
      wprintf( _OUTPUT_DIRECTORY );
      wprintf( L" for the event log." );
      return r;
   }

	return 0;
}

uint32_t __configure_scheduler(void* mem, uint64_t hmenv, uint32_t scheduler_count, struct _host *phost, void **phsched)
{
	uint32_t space_needed_for_scheduler;
	CM2_SETUP setup;

	space_needed_for_scheduler = 1024 + (scheduler_count * 512); // TODO! DEFINES
	setup.name = _APPLICATION_NAME;
	setup.server_name = _SERVER_NAME;
	setup.db_data_dir_path = _OUTPUT_DIRECTORY;
	setup.is_system = 0;
	setup.num_shm_chunks = 1 << 14;
	setup.mem = mem;
	setup.mem_size = space_needed_for_scheduler;
	setup.hmenv = hmenv;
	setup.cpuc = (uint8_t)scheduler_count;
	setup.th_enter = __thread_enter; // Callback when entering scheduler worker thread.
	setup.th_leave = __thread_leave; // Callback when leaving scheduler worker thread.
	setup.th_start = __thread_start; // Callback when scheduler has create a worker thread.
	setup.th_reset = __thread_reset; // Callback when a task is finished
	setup.th_yield = __thread_yield; // Callback when before doing a worker thread leave. Host can return to refuse.
                                    // If returned false, then the scheduler will continue to have the worker thread attached.
	setup.vp_bgtask = __sched_background_task; // recurring callback every 128 milliseconds (vp_ = scheduler). The host
                                              // can use this for generic event based house keeping work.
	setup.vp_ctick = __sched_clock_tick; // Calls 3600 times per hour, often with one second interval. The host
                                        // Can use this general housekeeping. 
	setup.vp_idle = __sched_idle;        // Callback when where are no tasks pending.
	setup.vp_wait = __sched_wait;       // Callback does we don't know what it does. Let's get back to this.
	setup.al_lowmem = __alert_lowmem;   // Callback when short on memory. Slow down. Proactive.
	setup.al_stall = __alert_stall;     // Callback when a single task have been running for an execisive period of time without yielding. 
	setup.pex_ctxt = phost;             // Host cargo pointer that will be sent as a parameter to each callback.

	return cm2_setup(&setup, phsched);
}

uint32_t __configure_database()
{
	uint32_t r;
	struct sccoredb_callbacks callbacks;

	r = sccoredb_set_system_variable(L"NAME", _APPLICATION_NAME); // This is supposed to moved so scdata. 
	if (r) return r;

	r = sccoredb_set_system_variable(L"IMAGEDIR", _DATABASE_DIRECTORY);
	if (r) return r;

	r = sccoredb_set_system_variable(L"OLOGDIR", _DATABASE_DIRECTORY);
	if (r) return r;

	r = sccoredb_set_system_variable(L"TLOGDIR", _DATABASE_DIRECTORY);
	if (r) return r;

	r = sccoredb_set_system_variable(L"TEMPDIR", _TEMP_DIRECTORY);
	if (r) return r;

	r = sccoredb_set_system_variable(L"COMPPATH", _COMPILER_PATH);
	if (r) return r;

	r = sccoredb_set_system_variable(L"OUTDIR", _OUTPUT_DIRECTORY);
	if (r) return r;

	callbacks.on_new_schema = __on_new_schema;
	callbacks.on_no_transaction = on_no_transaction;

	return 0;
}

uint32_t __connect_database(void* hsched, uint64_t hmenv, uint64_t hlogs)
{
	uint32_t flags;
	int32_t empty;

	flags = 0;
	flags |= SCCOREDB_LOAD_DATABASE;
	flags |= SCCOREDB_USE_BUFFERED_IO;
	flags |= SCCOREDB_ENABLE_CHECK_FILE_ON_LOAD;
//	flags |= SCCOREDB_ENABLE_CHECK_FILE_ON_CHECKP;
	flags |= SCCOREDB_ENABLE_CHECK_FILE_ON_BACKUP;
	flags |= SCCOREDB_ENABLE_CHECK_MEMORY_ON_CHECKP;

	return sccoredb_connect(flags, hsched, hmenv, hlogs, &empty);
}


///////////////////////////////////////////////////////////////////////////////


uint32_t __start(void *hsched)
{
   // Start the worker threads in all schedulers
	return cm2_start(hsched);
}

uint32_t __stop(void *hsched)
{
	return cm2_stop(hsched, 1);
}

uint32_t __cleanup()
{
	return sccoredb_disconnect(0);
}

void __log_critical(uint64_t hlogs, uint32_t r)
{
	wchar_t buf[512];
	_format_err(
		LoadLibrary(L"scerrres.dll"),
		buf,
		512,
		0,
		0,
		0,
		r,
		0
		);
	sccorelog_kernel_write_to_logs(hlogs, SC_ENTRY_CRITICAL, r, buf);
	sccorelog_flush_to_logs(hlogs);
}
