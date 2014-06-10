#include "bmx.hpp"

using namespace starcounter::bmx;
using namespace starcounter::core;
using namespace starcounter::utils;

// Global BMX data structures.
static BmxData* starcounter::bmx::g_bmx_data = NULL;
Profiler* Profiler::schedulers_profilers_ = NULL;
CRITICAL_SECTION g_bmx_cs_;
std::list<BmxData*> g_bmx_old_clones_;

BmxData* EnterSafeBmxManagement()
{
    // Entering critical section.
    EnterCriticalSection(&g_bmx_cs_);

    //std::cout << "EnterSafeBmxManagement." << std::endl;

    // Checking if there are too many old clones.
    if (g_bmx_old_clones_.size() > 128)
    {
        // Deleting very old BMX data copy.
        delete g_bmx_old_clones_.front();
        g_bmx_old_clones_.pop_front();
    }

    // Cloning the BMX data.
    return g_bmx_data->Clone();
}

void LeaveSafeBmxManagement(BmxData* new_bmx_data)
{
    // Pushing old BMX data to history.
    g_bmx_old_clones_.push_back(g_bmx_data);

    // Switching to new BMX data.
    g_bmx_data = new_bmx_data;

    // Leaving the critical sections.
    LeaveCriticalSection(&g_bmx_cs_);

    //std::cout << "LeaveSafeBmxManagement." << std::endl;
}

DestroyAppsSessionCallback starcounter::bmx::g_destroy_apps_session_callback = NULL;
CreateNewAppsSessionCallback starcounter::bmx::g_create_new_apps_session_callback = NULL;
ErrorHandlingCallback starcounter::bmx::g_error_handling_callback = NULL;

// Initializes BMX related data structures.
EXTERN_C uint32_t __stdcall sc_init_bmx_manager(
    DestroyAppsSessionCallback destroy_apps_session_callback,
    CreateNewAppsSessionCallback create_new_apps_session_callback,
    ErrorHandlingCallback error_handling_callback)
{
    // Initializing BMX critical section.
    InitializeCriticalSection(&g_bmx_cs_);

    // Allocating global BMX data.
    g_bmx_data = new BmxData(1);

    // Adding BMX port handler.
    BMX_HANDLER_TYPE bmx_handler_info;
    uint32_t err_code = g_bmx_data->RegisterPortHandler(0, "", OnIncomingBmxMessage, 0, &bmx_handler_info);
    if (err_code)
        return err_code;

    // Checking that handler id is 0 for BMX management.
    _SC_ASSERT(bmx_handler_info == BMX_MANAGEMENT_HANDLER_INFO);

    g_destroy_apps_session_callback = destroy_apps_session_callback;
    g_create_new_apps_session_callback = create_new_apps_session_callback;
    g_error_handling_callback = error_handling_callback;

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

    return g_bmx_data->HandleBmxChunk(task_data);

    _SC_END_FUNC
}

// Registers port handler.
EXTERN_C uint32_t __stdcall sc_bmx_register_port_handler(
    const uint16_t port_num,
    const char* app_name,
    const GENERIC_HANDLER_CALLBACK callback,
    const uint16_t managed_handler_index,
    BMX_HANDLER_TYPE* phandler_info
    )
{
    _SC_BEGIN_FUNC

    _SC_ASSERT(NULL != g_bmx_data);

    BMX_HANDLER_INDEX_TYPE handler_index;
    uint32_t err_code = g_bmx_data->FindPortHandler(port_num, &handler_index);
    if (0 == err_code)
        return SCERRHANDLERALREADYREGISTERED;

    // Performing operation on a copy.
    BmxData* g_bmx_data_copy = EnterSafeBmxManagement();
    err_code = g_bmx_data_copy->RegisterPortHandler(port_num, app_name, callback, managed_handler_index, phandler_info);
    LeaveSafeBmxManagement(g_bmx_data_copy);

    return err_code;

    _SC_END_FUNC
};

// Registers sub-port handler.
EXTERN_C uint32_t __stdcall sc_bmx_register_subport_handler(
    const uint16_t port_num,
    const char* app_name,
    const BMX_SUBPORT_TYPE sub_port,
    const GENERIC_HANDLER_CALLBACK callback,
    const uint16_t managed_handler_index,
    BMX_HANDLER_TYPE* phandler_info
    )
{
    _SC_BEGIN_FUNC

    _SC_ASSERT(NULL != g_bmx_data);

    BMX_HANDLER_INDEX_TYPE handler_index;
    uint32_t err_code = g_bmx_data->FindSubportHandler(port_num, sub_port, &handler_index);
    if (0 == err_code)
        return SCERRHANDLERALREADYREGISTERED;

    // Performing operation on a copy.
    BmxData* g_bmx_data_copy = EnterSafeBmxManagement();
    err_code = g_bmx_data_copy->RegisterSubPortHandler(port_num, app_name, sub_port, callback, managed_handler_index, phandler_info);
    LeaveSafeBmxManagement(g_bmx_data_copy);

    return err_code;

    _SC_END_FUNC
}

EXTERN_C uint32_t __stdcall sc_bmx_register_ws_handler(
    const uint16_t port_num,
    const char* app_name,
    const char* channel_name,
    const uint32_t channel_id,
    const GENERIC_HANDLER_CALLBACK callback,
    uint16_t managed_handler_index,
    BMX_HANDLER_TYPE* phandler_info
    )
{
    _SC_BEGIN_FUNC

    _SC_ASSERT(NULL != g_bmx_data);

    BMX_HANDLER_INDEX_TYPE handler_index;
    uint32_t err_code = g_bmx_data->FindWsHandler(port_num, channel_name, &handler_index);
    if (0 == err_code)
        return SCERRHANDLERALREADYREGISTERED;

    // Performing operation on a copy.
    BmxData* g_bmx_data_copy = EnterSafeBmxManagement();
    err_code = g_bmx_data_copy->RegisterWsHandler(port_num, app_name, channel_name, channel_id, callback, managed_handler_index, phandler_info);
    LeaveSafeBmxManagement(g_bmx_data_copy);

    return err_code;

    _SC_END_FUNC
};

EXTERN_C uint32_t __stdcall sc_bmx_register_uri_handler(
    const uint16_t port_num,
    const char* app_name,
    const char* original_uri_info,
    const char* processed_uri_info,
    const uint8_t* param_types,
    const uint8_t num_params,
    const GENERIC_HANDLER_CALLBACK callback, 
    const uint16_t managed_handler_index,
    BMX_HANDLER_TYPE* phandler_info
    )
{
    _SC_BEGIN_FUNC

    _SC_ASSERT(NULL != g_bmx_data);

    BMX_HANDLER_INDEX_TYPE handler_index;
    uint32_t err_code = g_bmx_data->FindUriHandler(port_num, processed_uri_info, &handler_index);
    if (0 == err_code)
        return SCERRHANDLERALREADYREGISTERED;

    // Performing operation on a copy.
    BmxData* g_bmx_data_copy = EnterSafeBmxManagement();
    err_code = g_bmx_data_copy->RegisterUriHandler(
        port_num,
        app_name,
        original_uri_info,
        processed_uri_info,
        param_types,
        num_params,
        callback,
        managed_handler_index,
        phandler_info);

    LeaveSafeBmxManagement(g_bmx_data_copy);

    return err_code;

    _SC_END_FUNC
}

// Unregisters a handler.
uint32_t sc_bmx_unregister_handler(BMX_HANDLER_INDEX_TYPE handler_index)
{
    _SC_BEGIN_FUNC

    bool is_empty_handler;

    // Checking if handler exists.
    if (g_bmx_data->IsHandlerExist(handler_index))
        return SCERRHANDLERNOTFOUND;

    // Performing operation on a copy.
    BmxData* g_bmx_data_copy = EnterSafeBmxManagement();
    uint32_t err_code = g_bmx_data_copy->UnregisterHandler(handler_index, &is_empty_handler);
    LeaveSafeBmxManagement(g_bmx_data_copy);

    return err_code;

    _SC_END_FUNC
}

uint32_t sc_bmx_unregister_uri(uint16_t port_num, char* processed_uri_info)
{
    _SC_BEGIN_FUNC

    BMX_HANDLER_INDEX_TYPE handler_index;
    uint32_t err_code;

    err_code = g_bmx_data->FindUriHandler(port_num, processed_uri_info, &handler_index);
    if (err_code)
        return err_code;

    err_code = sc_bmx_unregister_handler(handler_index);

    return err_code;

    _SC_END_FUNC
}

uint32_t sc_bmx_unregister_port(uint16_t port_num)
{
    _SC_BEGIN_FUNC

    BMX_HANDLER_INDEX_TYPE handler_index;
    uint32_t err_code;

    err_code = g_bmx_data->FindPortHandler(port_num, &handler_index);
    if (err_code)
        return err_code;

    err_code = sc_bmx_unregister_handler(handler_index);

    return err_code;

    _SC_END_FUNC
}

// Unregisters a handler.
uint32_t sc_bmx_unregister_subport(uint16_t port_num, BMX_SUBPORT_TYPE subport_num)
{
    _SC_BEGIN_FUNC

    BMX_HANDLER_INDEX_TYPE handler_index;
    uint32_t err_code;

    err_code = g_bmx_data->FindSubportHandler(port_num, subport_num, &handler_index);
    if (err_code)
        return err_code;

    err_code = sc_bmx_unregister_handler(handler_index);

    return err_code;

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

// The specific handler that is responsible for handling responses
// from the gateway registration process.
uint32_t starcounter::bmx::OnIncomingBmxMessage(
    uint16_t managed_handler_id,
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
            err_code = g_bmx_data->HandleErrorFromGateway(request, task_info);

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
            err_code = g_bmx_data->HandleSessionDestruction(request, task_info);

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