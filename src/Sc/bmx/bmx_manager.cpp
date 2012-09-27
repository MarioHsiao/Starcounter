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
}

// Initializes BMX related data structures.
uint32_t InitBmxManager()
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

// Main message loop for incoming requests. Handles the 
// dispatching of the message to the correct handler as 
// well as sending any responses back.
uint32_t sc_handle_incoming_chunks(CM2_TASK_DATA* task_data)
{
    return g_bmx_data->HandleBmxChunk(task_data);
}

// The specific handler that is responsible for handling responses
// from the gateway registration process.
uint32_t starcounter::bmx::OnIncomingBmxMessage(
    uint64_t session_id, 
    shared_memory_chunk* smc, 
    TASK_INFO_TYPE* task_info,
    bool* is_handled)
{
    uint8_t message_id;

    uint32_t err_code = 0;
    request_chunk_part* request = smc->get_request_chunk();
    uint32_t request_size = request->get_offset();
    uint32_t offset = 0;

    request->reset_offset();
    while (offset < request_size)
    {
        message_id = request->read_uint8();
        switch (message_id)
        {
            case BMX_ERROR:
            {
                return SCERRUNSPECIFIED; // SCERRBMXFAILURE
                break;
            }

            case BMX_REGISTER_PUSH_CHANNEL:
            {
                // NOTE:
                // Channel attached to thread. No storing away channel reference in
                // shared memory.

                // data->channel_index_for_push = task_info->channel_index;
                break;
            }

            case BMX_SEND_ALL_HANDLERS:
            {
                err_code = g_bmx_data->SendAllHandlersInfo(smc, task_info);

                if (err_code)
                    return err_code;

                break;
            }

            default:
            {
                return SCERRUNSPECIFIED; // SCERRUNKNOWNBMXMESSAGE;
                break;
            }
        }

        offset = request->get_offset();
    }

    // BMX messages were handled successfully.
    *is_handled = true;

    return err_code;
}