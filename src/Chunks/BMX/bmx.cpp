#include "bmx.hpp"

using namespace starcounter::core;
using namespace starcounter::utils;

// Global BMX data structures.
Profiler* Profiler::schedulers_profilers_ = NULL;

namespace starcounter
{
namespace bmx
{
	DestroyAppsSessionCallback starcounter::bmx::g_destroy_apps_session_callback = NULL;
	CreateNewAppsSessionCallback starcounter::bmx::g_create_new_apps_session_callback = NULL;
	ErrorHandlingCallback starcounter::bmx::g_error_handling_callback = NULL;
	GenericManagedCallback starcounter::bmx::g_generic_managed_handler = NULL;

	// Initializes BMX related data structures.
	EXTERN_C uint32_t __stdcall sc_init_bmx_manager(
		DestroyAppsSessionCallback destroy_apps_session_callback,
		CreateNewAppsSessionCallback create_new_apps_session_callback,
		ErrorHandlingCallback error_handling_callback,
		GenericManagedCallback generic_managed_handler)
	{
		g_destroy_apps_session_callback = destroy_apps_session_callback;
		g_create_new_apps_session_callback = create_new_apps_session_callback;
		g_error_handling_callback = error_handling_callback;
		g_generic_managed_handler = generic_managed_handler;

		return 0;
	}

	// Initializes profilers.
	EXTERN_C void __stdcall sc_init_profilers(
		uint8_t num_workers
		)
	{
		// Initializing profilers.
		Profiler::InitAll(num_workers);
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
		Profiler::GetProfiler(sched_id)->ResetAll();
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

	// Construct BMX Ping message.
	uint32_t sc_bmx_construct_ping(
		uint64_t ping_data,
		shared_memory_chunk* smc
		)
	{
		// Predefined BMX management handler.
		smc->set_bmx_handler_info(BMX_MANAGEMENT_HANDLER_INFO);

		request_chunk_part* request = smc->get_request_chunk();
		request->reset_offset();

		// Writing BMX message type.
		request->write(BMX_PING);

		// Writing Ping data.
		request->write(ping_data);

		// No linked chunks.
		smc->terminate_link();

		return 0;
	}

	// Parse BMX Pong message.
	uint32_t sc_bmx_parse_pong(
		shared_memory_chunk* smc,
		uint64_t* pong_data
		)
	{
		// Checking that its a BMX message.
		if (BMX_MANAGEMENT_HANDLER_INFO != smc->get_bmx_handler_info())
			return 1;

		// Getting the response part of the chunk.
		response_chunk_part* response = smc->get_response_chunk();
		uint32_t response_size = response->get_offset();

		// Checking for correct response size.
		if (9 != response_size) // 9 = 1 byte Ping message type + 8 bytes pong_data.
			return 2;

		// Reading BMX message type.
		response->reset_offset();
		uint8_t bmx_type = response->read_uint8();

		// Checking if its a Pong message.
		if (BMX_PONG != bmx_type)
			return 3;

		// Reading original Ping data.
		*pong_data = response->read_uint64();

		return 0;
	}

	// Send Pong response for initial Ping message.
	uint32_t SendPongResponse(request_chunk_part *request, shared_memory_chunk* smc, TASK_INFO_TYPE* task_info)
	{
		// Original 8-bytes of data.
		uint64_t orig_data = request->read_uint64();

		response_chunk_part *response = smc->get_response_chunk();
		response->reset_offset();

		// Writing response Pong with initial data.
		response->write(BMX_PONG);
		response->write(orig_data);

		// Now the chunk is ready to be sent.
		client_index_type client_index = 0; // TODO:
		uint32_t err_code = cm_send_to_client(client_index, task_info->the_chunk_index);

		return err_code;
	}

	// Handles destroyed session message.
	uint32_t HandleSessionDestruction(request_chunk_part* request, TASK_INFO_TYPE* task_info)
	{
		// Reading Apps unique session number.
		uint32_t linear_index = request->read_uint32();

		// Reading Apps session salt.
		uint64_t random_salt = request->read_uint64();

		// Calling managed function to destroy session.
		g_destroy_apps_session_callback(task_info->scheduler_number, linear_index, random_salt);

		//std::cout << "Session " << linear_index << ":" << random_salt << " was destroyed." << std::endl;

		// Returning chunk to pool.
		uint32_t err_code = cm_release_linked_shared_memory_chunks(task_info->the_chunk_index);
		if (err_code)
			return err_code;

		return 0;
	}

	// Handles error from gateway.
	uint32_t HandleErrorFromGateway(request_chunk_part* request, TASK_INFO_TYPE* task_info)
	{
		// Reading error code number.
		uint32_t gw_err_code = request->read_uint32();

		// Reading error string.
		wchar_t err_string[MixedCodeConstants::MAX_URI_STRING_LEN];
		uint32_t err_string_len = request->read_uint32();

		uint32_t err_code = request->read_wstring(err_string, err_string_len, MixedCodeConstants::MAX_URI_STRING_LEN);
		if (err_code)
			goto RETURN_CHUNK;

		//std::cout << "Error from gateway: " << err_string << std::endl;

		// Calling managed function to handle error.
		g_error_handling_callback(gw_err_code, err_string, err_string_len);

RETURN_CHUNK:

		// Returning chunk to pool.
		cm_release_linked_shared_memory_chunks(task_info->the_chunk_index);

		return err_code;
	}

	// The specific handler that is responsible for handling responses
	// from the gateway registration process.
	uint32_t OnBmxMessage(
		shared_memory_chunk* smc,
		TASK_INFO_TYPE* task_info,
		bool* is_handled)
	{
		uint32_t err_code = 0;

		// This is going to be a BMX management chunk.
		request_chunk_part* request = smc->get_request_chunk();
		request->reset_offset();

		// Reading BMX management type.
		uint8_t message_id = request->read_uint8();
		switch (message_id)
		{
			case BMX_ERROR:
			{
				// Handling session destruction.
				err_code = HandleErrorFromGateway(request, task_info);

				if (err_code)
					return err_code;
			}

			case BMX_PING:
			{
				// Writing Pong message and sending it back.
				err_code = SendPongResponse(request, smc, task_info);

				if (err_code)
					return err_code;

				break;
			}

			case BMX_SESSION_DESTROY:
			{
				// Handling session destruction.
				err_code = HandleSessionDestruction(request, task_info);

				if (err_code)
					return err_code;

				break;
			}

			default:
			{
				_SC_ASSERT(false);
			}
		}

		// BMX messages were handled successfully.
		*is_handled = true;

		return err_code;
	}

	// Main message loop for incoming requests. Handles the 
	// dispatching of the message to the correct handler as 
	// well as sending any responses back.
	uint32_t HandleBmxChunk(CM2_TASK_DATA* task_data)
	{

		// Initializing task information.
#if 0
		task_info.session_id.high = 0;
		task_info.session_id.low = 0;
#endif

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
		if (smc->get_link() != smc->link_terminator)
			task_info.flags |= MixedCodeConstants::LINKED_CHUNKS_FLAG;

		bool is_handled = false;

		// Checking if its a BMX message.
		if (BMX_MANAGEMENT_HANDLER_INFO == handler_info) {
			OnBmxMessage(smc, &task_info, &is_handled);
			goto finish;
		}

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

#if 0 // No need to do this here. Current transaction will be reset on task completion.
		// Resetting current transaction.
    star_context_set_current_transaction(context_handle, 0);
#endif

		return err_code;

	}
}
}