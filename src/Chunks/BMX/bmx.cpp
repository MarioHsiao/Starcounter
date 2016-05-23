#include "bmx.hpp"

using namespace starcounter::core;
using namespace starcounter::utils;

// Global BMX data structures.
Profiler* Profiler::schedulers_profilers_ = NULL;

namespace starcounter
{
namespace bmx
{
	GenericManagedCallback starcounter::bmx::g_generic_managed_handler = NULL;

	// Initializes BMX related data structures.
	EXTERN_C uint32_t __stdcall sc_init_bmx_manager(
		GenericManagedCallback generic_managed_handler)
	{
		_SC_BEGIN_FUNC

		g_generic_managed_handler = generic_managed_handler;

		return 0;

		_SC_END_FUNC
	}

	// Initializes profilers.
	EXTERN_C void __stdcall sc_init_profilers(uint8_t num_workers)
	{
		_SC_BEGIN_FUNC

		// Initializing profilers.
		Profiler::InitAll(num_workers);

		_SC_END_FUNC
	}

	// Gets profiler results for a given scheduler.
	EXTERN_C uint32_t __stdcall sc_profiler_get_results_in_json(
		uint8_t sched_id,
		uint8_t* buf,
		int32_t buf_max_len)
	{
		std::string s = Profiler::GetProfiler(sched_id)->GetResultsInJson(false);

		if (s.length() >= buf_max_len)
			return SCERRBADARGUMENTS;

		strncpy_s((char*)buf, buf_max_len, s.c_str(), _TRUNCATE);

		return 0;
	}

	// Reset profilers on given scheduler.
	EXTERN_C void __stdcall sc_profiler_reset(
		uint8_t sched_id)
	{
		_SC_BEGIN_FUNC

		Profiler::GetProfiler(sched_id)->ResetAll();

		_SC_END_FUNC
	}

	// Main message loop for incoming requests. Handles the 
	// dispatching of the message to the correct handler as 
	// well as sending any responses back.
	uint32_t sc_handle_incoming_chunks(CM2_TASK_DATA* task_data)
	{
		_SC_BEGIN_FUNC

		return HandleBmxChunk(task_data);

		_SC_END_FUNC
	}
	
	// Main message loop for incoming requests. Handles the 
	// dispatching of the message to the correct handler as 
	// well as sending any responses back.
	uint32_t HandleBmxChunk(CM2_TASK_DATA* task_data)
	{
		TASK_INFO_TYPE task_info;
		cm3_get_cpun(0, &task_info.scheduler_number);
		task_info.client_worker_id = (uint8_t)task_data->Output1;
		task_info.the_chunk_index = (uint32_t)task_data->Output2;

		// Retrieve the chunk.
		uint8_t* raw_chunk;
		uint32_t err_code = cm_get_shared_memory_chunk(task_info.the_chunk_index, &raw_chunk);
		_SC_ASSERT(err_code == 0);

		// Read the metadata in the chunk (session id and handler id).
		shared_memory_chunk* smc = (shared_memory_chunk*)raw_chunk;

		BMX_HANDLER_TYPE handler_info = smc->get_bmx_handler_info();
		task_info.handler_info = handler_info;

		// Checking if chunks are linked.
		if (smc->get_link() != smc->link_terminator) {
			task_info.flags |= MixedCodeConstants::LINKED_CHUNKS_FLAG;
		}

		bool is_handled = false;

		// Send the response back.
		if (((*(uint32_t*)(raw_chunk + MixedCodeConstants::CHUNK_OFFSET_SOCKET_FLAGS)) & MixedCodeConstants::SOCKET_DATA_GATEWAY_AND_IPC_TEST) != 0)
		{
			err_code = cm_send_to_client(task_info.client_worker_id, task_info.the_chunk_index);
			if (err_code != 0)
				goto finish;

			goto finish;
		}

		// Checking if its host looping chunks.
		if (((*(uint32_t*)(raw_chunk + MixedCodeConstants::CHUNK_OFFSET_SOCKET_FLAGS)) & MixedCodeConstants::SOCKET_DATA_HOST_LOOPING_CHUNKS) != 0) {

			starcounter::core::chunk_index cur_chunk_index = task_info.the_chunk_index;

			while (true) {

				// Cloning current linked chunks.
				starcounter::core::chunk_index new_chunk_index;
				err_code = sc_clone_linked_chunks(cur_chunk_index, &new_chunk_index);
				_SC_ASSERT(0 == err_code);

				err_code = g_generic_managed_handler(handler_info, smc, &task_info, &is_handled);
				_SC_ASSERT(0 == err_code);

				cur_chunk_index = new_chunk_index;
				task_info.the_chunk_index = new_chunk_index;

				err_code = cm_get_shared_memory_chunk(new_chunk_index, (uint8_t**)&smc);
				_SC_ASSERT(err_code == 0);
			}

			goto finish;
		}

		// Calling managed handler.
		err_code = g_generic_managed_handler(handler_info, smc, &task_info, &is_handled);

finish:

		// Resetting current transaction.
		sccoredb_set_current_transaction(0, 0, 0); // You may comment this line to avoid throwing an exception when using NODB.

		return err_code;
	}
}
}