
#include "internal.h"


COALMINE_STANDBY coalmine_standby;


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
