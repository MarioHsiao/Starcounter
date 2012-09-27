
#include "internal.h"


COALMINE_STANDBY coalmine_standby;

// Handles all incoming chunks.
extern "C" uint32_t sc_handle_incoming_chunks(CM2_TASK_DATA* task_data);

extern "C" uint32_t __stdcall sccoreapp_standby(void* hsched, CM2_TASK_DATA* ptask_data)
{
	for (;;)
	{
		uint32_t e;
		e = coalmine_standby(hsched, ptask_data);
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
