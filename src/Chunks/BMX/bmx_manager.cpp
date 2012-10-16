#include "bmx.hpp"

using namespace starcounter::bmx;
using namespace starcounter::core;

// Global BMX data structures.
BmxData* starcounter::bmx::g_bmx_data;
CRITICAL_SECTION g_bmx_cs_;
std::list<BmxData*> g_bmx_old_clones_;

BmxData* EnterSafeBmxManagement()
{
    // Entering critical section.
    EnterCriticalSection(&g_bmx_cs_);

    //std::cout << "EnterSafeBmxManagement." << std::endl;

    // Checking if there are too many old clones.
    if (g_bmx_old_clones_.size() > 4096)
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

// Initializes BMX related data structures.
uint32_t sc_init_bmx_manager()
{
    // Initializing BMX critical section.
    InitializeCriticalSection(&g_bmx_cs_);

    // Allocating global BMX data.
    g_bmx_data = new BmxData(1);

    // Adding BMX port handler.
    BMX_HANDLER_TYPE bmx_handler_id;
    uint32_t err_code = g_bmx_data->RegisterPortHandler(0, OnIncomingBmxMessage, &bmx_handler_id);
    if (err_code)
        return err_code;

    // Checking that handler id is 0 for BMX management.
    if (bmx_handler_id != BMX_MANAGEMENT_HANDLER)
        return SCERRUNSPECIFIED; // SCERRWRONGBMXMANAGERHANDLER

    return 0;
}

// Main message loop for incoming requests. Handles the 
// dispatching of the message to the correct handler as 
// well as sending any responses back.
uint32_t sc_handle_incoming_chunks(CM2_TASK_DATA* task_data)
{
    /*
    SYSTEMTIME time;
    GetSystemTime(&time);
    WORD millis = (time.wSecond * 1000) + time.wMilliseconds;
    std::cout << "POP time: " << millis << std::endl;
    */

    return g_bmx_data->HandleBmxChunk(task_data);
}

// Registers port handler.
uint32_t sc_bmx_register_port_handler(
    uint16_t port, 
    GENERIC_HANDLER_CALLBACK callback,
    BMX_HANDLER_TYPE* handler_id
    )
{
    BmxData* g_bmx_data_copy = EnterSafeBmxManagement();

    // Performing operation on a copy.
    uint32_t err_code = g_bmx_data_copy->RegisterPortHandler(port, callback, handler_id);

    LeaveSafeBmxManagement(g_bmx_data_copy);

    return err_code;
};

// Registers sub-port handler.
uint32_t sc_bmx_register_subport_handler(
    uint16_t port,
    uint32_t sub_port,
    GENERIC_HANDLER_CALLBACK callback,
    BMX_HANDLER_TYPE* handler_id
    )
{
    BmxData* g_bmx_data_copy = EnterSafeBmxManagement();

    uint32_t err_code = g_bmx_data_copy->RegisterSubPortHandler(port, sub_port, callback, handler_id);

    LeaveSafeBmxManagement(g_bmx_data_copy);

    return err_code;
}

// Registers raw port handler.
uint32_t sc_bmx_register_uri_handler(
    uint16_t port,
    char* uri_string,
    uint8_t http_method,
    GENERIC_HANDLER_CALLBACK callback, 
    BMX_HANDLER_TYPE* handler_id
    )
{
    BmxData* g_bmx_data_copy = EnterSafeBmxManagement();

    uint32_t err_code = g_bmx_data_copy->RegisterUriHandler(port, uri_string, (HTTP_METHODS)http_method, callback, handler_id);

    LeaveSafeBmxManagement(g_bmx_data_copy);

    return err_code;
}

// Unregisters a handler.
uint32_t sc_bmx_unregister_handler(BMX_HANDLER_TYPE handler_id)
{
    BmxData* g_bmx_data_copy = EnterSafeBmxManagement();

    uint32_t err_code = g_bmx_data_copy->UnregisterHandler(handler_id);

    LeaveSafeBmxManagement(g_bmx_data_copy);

    return err_code;
}

// Construct BMX Ping message.
uint32_t sc_bmx_construct_ping(
    uint64_t ping_data, 
    shared_memory_chunk* smc
    )
{
    // Predefined BMX management handler.
    smc->set_bmx_protocol(BMX_MANAGEMENT_HANDLER);

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
    if (BMX_MANAGEMENT_HANDLER != smc->get_bmx_protocol())
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
inline uint32_t SendPongResponse(request_chunk_part *request, shared_memory_chunk* smc, TASK_INFO_TYPE* task_info)
{
    // Original 8-bytes of data.
    uint64_t orig_data = request->read_uint64();

    response_chunk_part *response = smc->get_response_chunk();
    response->reset_offset();

    // Writing response Pong with initial data.
    response->write(BMX_PONG);
    response->write(orig_data);

    // Now the chunk is ready to be sent.
    uint32_t err_code = cm_send_to_client(task_info->chunk_index);

    return err_code;
}

// The specific handler that is responsible for handling responses
// from the gateway registration process.
uint32_t starcounter::bmx::OnIncomingBmxMessage(
    uint64_t session_id, 
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
            return SCERRUNSPECIFIED; // SCERRBMXFAILURE
        }

        case BMX_PING:
        {
            // Writing Pong message and sending it back.
            err_code = SendPongResponse(request, smc, task_info);

            if (err_code)
                return err_code;

			break;
        }

        case BMX_REGISTER_PUSH_CHANNEL:
        {
            // Entering critical section.
            EnterCriticalSection(&g_bmx_cs_);

            //std::cout << "Received chunk: " << task_info->chunk_index << " on scheduler: " << (int32_t)task_info->scheduler_number << std::endl;
            //std::cout << "Received BMX_REGISTER_PUSH_CHANNEL." << std::endl;

            // Calling push channel registration.
            err_code = g_bmx_data->SendRegisterPushChannelResponse(smc, task_info);

            // Entering critical section.
            LeaveCriticalSection(&g_bmx_cs_);

            //std::cout << "Left critical section." << std::endl;

            if (err_code)
                return err_code;

            break;
        }

        case BMX_SEND_ALL_HANDLERS:
        {
            // Entering critical section.
            EnterCriticalSection(&g_bmx_cs_);

            //std::cout << "Received chunk: " << task_info->chunk_index << " on scheduler: " << (int32_t)task_info->scheduler_number << std::endl;
            //std::cout << "Received BMX_SEND_ALL_HANDLERS." << std::endl;

            err_code = g_bmx_data->SendAllHandlersInfo(smc, task_info);

            // Entering critical section.
            LeaveCriticalSection(&g_bmx_cs_);

            //std::cout << "Left critical section." << std::endl;

            if (err_code)
                return err_code;

            break;
        }

        default:
        {
            return SCERRUNSPECIFIED; // SCERRUNKNOWNBMXMESSAGE;
        }
    }

    // BMX messages were handled successfully.
    *is_handled = true;

    return err_code;
}