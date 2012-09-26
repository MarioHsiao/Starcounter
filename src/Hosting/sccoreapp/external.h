
#pragma once


#include <stdint.h>


typedef struct _CM2_TASK_DATA
{
    uint16_t Type;
    uint16_t Prio;
    uint32_t Output1;
    uint64_t Output2;
    uint64_t Output3;
} CM2_TASK_DATA;


#define CM2_TYPE_REQUEST 0x0001


typedef uint32_t (*COALMINE_STANDBY)(void* hsched, CM2_TASK_DATA* ptask_data);
