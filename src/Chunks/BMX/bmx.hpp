//
// bmx.hpp
//
// 
//
// Copyright © 2006-2013 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#ifndef BMX_HPP
#define BMX_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#define WIN32_LEAN_AND_MEAN
#include <winsock2.h>
#undef WIN32_LEAN_AND_MEAN

#define _SC_BEGIN_FUNC
#define _SC_END_FUNC

#include <cstdint>
#include <list>
#include <vector>
#include <cassert>
#include "../../Starcounter.Internal/Constants/MixedCodeConstants.cs"
#include "coalmine.h"

#define CODE_HOSTED
#include "profiler.hpp"

#include "sccoredbg2.h"
#include "sccoredb.h"
#include "chunk_helper.h"
#include "..\common\chunk.hpp"

//#undef _SC_ASSERT
//#define _SC_ASSERT assert

namespace starcounter
{
namespace bmx
{
	// BMX task information.
	struct TASK_INFO_TYPE
	{
		BMX_HANDLER_TYPE handler_info;
		starcounter::core::chunk_index the_chunk_index;
		uint8_t flags;
		uint8_t scheduler_number;
		uint8_t client_worker_id;
	};

	// User handler callback.
	typedef uint32_t(__stdcall *GENERIC_HANDLER_CALLBACK)(
		uint16_t managed_handler_id,
		shared_memory_chunk* smc,
		TASK_INFO_TYPE* task_info,
		bool* is_handled
		);

	// User handler callback.
	typedef uint32_t(__stdcall *GenericManagedCallback)(
		uint64_t handler_info,
		shared_memory_chunk* smc,
		TASK_INFO_TYPE* task_info,
		bool* is_handled
		);

	// Type of handler.
	enum HANDLER_TYPE
	{
		UNUSED_HANDLER,
		PORT_HANDLER,
		SUBPORT_HANDLER,
		URI_HANDLER,
		WS_HANDLER
	};

	// Maximum total number of registered handlers.
	const uint32_t MAX_TOTAL_NUMBER_OF_HANDLERS = 2048;

    typedef uint32_t BMX_SUBPORT_TYPE;

    // Invalid BMX handler info.
    const BMX_HANDLER_TYPE BMX_INVALID_HANDLER_INFO = ~((BMX_HANDLER_TYPE) 0);

    // Invalid BMX handler index.
    const BMX_HANDLER_INDEX_TYPE BMX_INVALID_HANDLER_INDEX = ~((BMX_HANDLER_INDEX_TYPE) 0);

    inline BMX_HANDLER_TYPE MakeHandlerInfo(BMX_HANDLER_INDEX_TYPE handler_index,
		BMX_HANDLER_UNIQUE_NUM_TYPE unique_num) {

        return (((uint64_t)unique_num) << 16) | handler_index;
    }

	extern uint32_t HandleBmxChunk(CM2_TASK_DATA* task_data);

	extern GenericManagedCallback g_generic_managed_handler;

	// Handles all incoming chunks.
	EXTERN_C uint32_t __stdcall sc_handle_incoming_chunks(CM2_TASK_DATA* task_data);

	EXTERN_C uint32_t __stdcall sc_bmx_clone_chunk(
		starcounter::core::chunk_index src_chunk_index,
		int32_t offset,
		int32_t num_bytes_to_copy,
		starcounter::core::chunk_index* new_chunk_index
		);

	EXTERN_C uint32_t __stdcall sc_clone_linked_chunks(
		starcounter::core::chunk_index first_chunk_index,
		starcounter::core::chunk_index* out_chunk_index);

}  // namespace bmx
}; // namespace starcounter

#endif
