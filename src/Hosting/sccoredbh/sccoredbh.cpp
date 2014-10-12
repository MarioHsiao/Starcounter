
#include "internal.h"


extern "C" void sccoreapp_log_critical_code(uint32_t e);

__declspec(noinline) static void _fatal_error(uint32_t r);

void _fatal_error(uint32_t r)
{
    sccoreapp_log_critical_code(r);
	ExitProcess(r);
}


static uint64_t __hmenv = 0;


extern "C" void __stdcall sccoredbh_init(uint64_t hmenv)
{
	__hmenv = hmenv;
}

extern "C" uint32_t __stdcall sccoredbh_get_image_version()
{
	return SC_VERSION_IMAGE;
}

extern "C" uint32_t __stdcall sccoredbh_get_image_magic()
{
	return SC_IMAGE_MAGIC_NUMBER;
}

extern "C" void __stdcall sccoredbh_thread_enter(void* hsched, uint8_t cpun, void* p, int32_t init)
{
	uint32_t r;
	void *wtds;
	uint16_t size;
	r = cm3_get_wtds(0, &wtds, &size);
	if (r == 0)
	{
		uint64_t tpid;
		r = cm3_get_tpid(0, &tpid);
		if (r == 0) r = star_attach(tpid, wtds, init);
	}
    if (r == 0) return;
    _fatal_error(r);
}

extern "C" void __stdcall sccoredbh_thread_leave(void* hsched, uint8_t cpun, void* p, uint32_t yr)
{
	uint32_t arg;
	switch (yr)
	{
	case CM5_YIELD_REASON_DETACHED:
	case CM5_YIELD_REASON_BLOCKED:
		arg = STAR_RELEASE_SNAPHOT;
		break;
	case CM5_YIELD_REASON_RELEASED:
		arg = STAR_RELEASE_ALL;
		break;
	default:
		arg = STAR_RELEASE_NOTHING;
		break;
	};
	uint32_t r = star_detach(arg);
    if (r == 0) return;
    _fatal_error(r);
}

extern "C" void __stdcall sccoredbh_thread_reset(void* hsched, uint8_t cpun, void* p)
{
    uint32_t r = star_reset();
    if (r == 0) return;
    _fatal_error(r);
}

extern "C" void __stdcall sccoredbh_vproc_bgtask(void* hsched, uint8_t cpun, void* p)
{
}

extern "C" void __stdcall sccoredbh_vproc_ctick(void* hsched, uint8_t cpun, uint32_t psec)
{
    star_advance_clock(cpun);

    // TODO: Here be session clock advance.

    if (cpun == 0)
    {
        mh4_menv_trim_cache(__hmenv, 1);
    }
}

extern "C" int32_t __stdcall sccoredbh_vproc_idle(void* hsched, uint8_t cpun, void* p)
{
    int32_t call_again_if_still_idle;
    uint32_t r = star_idle_task(&call_again_if_still_idle);
    if (r == 0) return call_again_if_still_idle;
    _fatal_error(r);
    return 0;
}

extern "C" void __stdcall sccoredbh_vproc_wait(void* hsched, uint8_t cpun, void* p) { }

extern "C" void __stdcall sccoredbh_alert_lowmem(void* hsched, void* p, uint32_t lr)
{
	uint32_t rt = lr; // Resource type matched callback code.
    uint32_t r = star_alert_low_memory(lr);
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
				mh4_menv_alert_lowmem(__hmenv);
                mh4_menv_trim_cache(__hmenv, 0);
            }
        }

        return;
    }

    _fatal_error(r);
}
