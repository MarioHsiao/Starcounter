
#include "internal.h"


extern "C" uint32_t __stdcall sccoreapp_init(void *hlogs)
{
	_init(hlogs);
	return 0;
}

// Handles all incoming chunks.
extern "C" uint32_t sc_handle_incoming_chunks(CM2_TASK_DATA* task_data);

extern "C" uint32_t __stdcall sccoreapp_standby(void* hsched, CM2_TASK_DATA* ptask_data)
{
	for (;;)
	{
		uint32_t e;
		e = cm2_standby(hsched, ptask_data);
		if (e == 0)
		{
            switch (ptask_data->Type)
            {
            case CM2_TYPE_REQUEST:

                // Processing all incoming chunks here.
                sc_handle_incoming_chunks(ptask_data);

                // Continue processing on native level.
                break;

				return 0; // Exit to managed code.
			default:
				return 0; // Exit to managed code.
            }
		}
		else
		{
			return e; // Exit to managed code.
		}
	}
}


// TODO:
// Temporary solution for handling critical errors in message loop. To be
// reviewed.

extern "C" void sccoreapp_log_critical_code(uint32_t e)
{
	_log_critical(e);
}

extern "C" void sccoreapp_log_critical_message(const wchar_t *message)
{
	_log_critical(message);
}
