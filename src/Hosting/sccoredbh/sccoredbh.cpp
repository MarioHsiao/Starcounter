
#include "internal.h"


__declspec(noinline) extern void _fatal_error(uint32_t r);

void _fatal_error(uint32_t r)
{
	ExitProcess(r); // TODO:
}


static uint64_t __hmenv = 0;


extern "C" void __stdcall sccoredbh_init(uint64_t hmenv)
{
	__hmenv = hmenv;
}

extern "C" void __stdcall sccoredbh_thread_enter(void* hsched, uint8_t cpun, void* p, int32_t init)
{
	uint32_t r = SCAttachThread(cpun, init);
    if (r == 0) return;
    _fatal_error(r);
}

extern "C" void __stdcall sccoredbh_thread_leave(void* hsched, uint8_t cpun, void* p, uint32_t yr)
{
	uint32_t r = SCDetachThread(yr);
    if (r == 0) return;
    _fatal_error(r);
}

extern "C" void __stdcall sccoredbh_thread_reset(void* hsched, uint8_t cpun, void* p)
{
    uint32_t r = SCResetThread();
    if (r == 0)
    {
        SCNewActivity();
        return;
    }
    _fatal_error(r);
}

extern "C" void __stdcall sccoredbh_vproc_bgtask(void* hsched, uint8_t cpun, void* p)
{
    uint32_t r = SCBackgroundTask();
    if (r == 0) return;
	_fatal_error(r);
}

extern "C" void __stdcall sccoredbh_vproc_ctick(void* hsched, uint8_t cpun, uint32_t psec)
{
    sccoredb_advance_clock(cpun);

    // TODO: Here be session clock advance.

    if (cpun == 0)
    {
        mh4_menv_trim_cache(__hmenv, 1);
    }
}

extern "C" int32_t __stdcall sccoredbh_vproc_idle(void* hsched, uint8_t cpun, void* p)
{
    int32_t callAgainIfStillIdle;
    uint32_t r = SCIdleTask(&callAgainIfStillIdle);
    if (r == 0) return callAgainIfStillIdle;
    _fatal_error(r);
    return 0;
}

extern "C" void __stdcall sccoredbh_vproc_wait(void* hsched, uint8_t cpun, void* p) { }

extern "C" void __stdcall sccoredbh_alert_lowmem(void* hsched, void* p, uint32_t lr)
{
    uint32_t r;
            
    r = SCLowMemoryAlert(lr);
    if (r == 0)
    {
        uint8_t cpun;
        r = cm3_get_cpun(0, &cpun);

        if (r == 0)
        {
            // This is a worker thread.

            return;
        }
        else
        {
            // This is the monitor thread.

            if (lr == CM5_LOWMEM_REASON_PHYSICAL_MEMORY)
            {
                mh4_menv_trim_cache(__hmenv, 0);
            }
        }

        return;
    }

    _fatal_error(r);
}
