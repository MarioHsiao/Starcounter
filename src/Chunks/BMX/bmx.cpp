#include "bmx.hpp"

using namespace starcounter::bmx;
using namespace starcounter::core;

// Registers port handler.
uint32_t BmxData::RegisterPortHandler(
    const uint16_t port_num,
    const char* app_name,
    const GENERIC_HANDLER_CALLBACK port_handler,
    const uint16_t managed_handler_index,
    BMX_HANDLER_TYPE* phandler_info)
{
    // Checking number of handlers.
    if (max_num_entries_ >= MAX_TOTAL_NUMBER_OF_HANDLERS)
        return SCERRMAXHANDLERSREACHED;

    if (port_num)
        GenerateNewId();

    uint32_t err_code = 0;

    BMX_HANDLER_INDEX_TYPE i, empty_slot = max_num_entries_;

    // Running throw existing handlers.
    for (i = 0; i < max_num_entries_; i++)
    {
        // Checking if empty slot.
        if (registered_handlers_[i].IsEmpty())
        {
            empty_slot = i;
        }
        // Checking current handler type to be port handler.
        else if (PORT_HANDLER == registered_handlers_[i].get_type())
        {
            // Checking if port is the same.
            if (port_num == registered_handlers_[i].get_port())
            {
                // Disallowing handler duplicates.
                return SCERRHANDLERALREADYREGISTERED;
            }
        }
    }

    // Constructing handler info from slot index and unique number.
    *phandler_info = MakeHandlerInfo(empty_slot, unique_handler_num_);

    // Initializing new handlers list.
    err_code = registered_handlers_[empty_slot].Init(
        PORT_HANDLER,
        *phandler_info,
        managed_handler_index,
        port_num,
        app_name,
        0,
        NULL,
        NULL,
        NULL,
        0,
        MixedCodeConstants::PROTOCOL_RAW_PORT);

    if (err_code)
        return err_code;

    // Adding handler to the list.
    err_code = registered_handlers_[empty_slot].AddUserHandler(port_handler);
    if (err_code)
        return err_code;

    // Adding new handler.
    if (empty_slot == max_num_entries_)
        max_num_entries_++;

    return err_code;
}

// Registers URI handler.
uint32_t BmxData::RegisterUriHandler(
    const uint16_t port,
    const char* app_name,
    const char* original_uri_info,
    const char* processed_uri_info,
    const uint8_t* param_types,
    const int32_t num_params,
    const GENERIC_HANDLER_CALLBACK uri_handler, 
    const uint16_t managed_handler_index,
    BMX_HANDLER_TYPE* phandler_info)
{
    // Checking number of handlers.
    if (max_num_entries_ >= MAX_TOTAL_NUMBER_OF_HANDLERS)
        return SCERRMAXHANDLERSREACHED;

    GenerateNewId();

    uint32_t err_code = 0;

    // Getting the URI string length.
    uint32_t original_uri_len_chars = (uint32_t)strlen(original_uri_info);
    uint32_t processed_uri_len_chars = (uint32_t)strlen(processed_uri_info);
    if ((original_uri_len_chars >= MixedCodeConstants::MAX_URI_STRING_LEN) ||
        (processed_uri_len_chars >= MixedCodeConstants::MAX_URI_STRING_LEN))
        return SCERRHANDLERINFOEXCEEDSLIMITS;

    BMX_HANDLER_INDEX_TYPE i, empty_slot = max_num_entries_;

    // Running throw existing handlers.
    for (i = 0; i < max_num_entries_; i++)
    {
        // Checking if empty slot.
        if (registered_handlers_[i].IsEmpty())
        {
            empty_slot = i;
        }
        // Checking current handler type to be port handler.
        else if (URI_HANDLER == registered_handlers_[i].get_type())
        {
            // Checking if port is the same.
            if (port == registered_handlers_[i].get_port())
            {
                // Checking if URI string is the same.
                if (!strcmp(processed_uri_info, registered_handlers_[i].get_processed_uri_info()))
                {
                    // Disallowing handler duplicates.
                    return SCERRHANDLERALREADYREGISTERED;
                }
            }
        }
    }

    // Constructing handler info from slot index and unique number.
    *phandler_info = MakeHandlerInfo(empty_slot, unique_handler_num_);

    // Initializing new handlers list.
    err_code = registered_handlers_[empty_slot].Init(
        bmx::HANDLER_TYPE::URI_HANDLER,
        *phandler_info,
        managed_handler_index,
        port,
        app_name,
        0,
        original_uri_info,
        processed_uri_info,
        param_types,
        num_params,
        starcounter::MixedCodeConstants::NetworkProtocolType::PROTOCOL_HTTP1);

    if (err_code)
        return err_code;

    // Adding handler to the list.
    err_code = registered_handlers_[empty_slot].AddUserHandler(uri_handler);
    if (err_code)
        return err_code;

    // Adding new handler.
    if (empty_slot == max_num_entries_)
        max_num_entries_++;

    return err_code;
}

// Registers WebSocket handler.
uint32_t BmxData::RegisterWsHandler(
    const uint16_t port,
    const char* app_name,
    const char* channel_name,
    const uint32_t channel_id,
    const GENERIC_HANDLER_CALLBACK ws_handler, 
    const uint16_t managed_handler_index,
    BMX_HANDLER_TYPE* phandler_info)
{
    // Checking number of handlers.
    if (max_num_entries_ >= MAX_TOTAL_NUMBER_OF_HANDLERS)
        return SCERRMAXHANDLERSREACHED;

    GenerateNewId();

    uint32_t err_code = 0;

    // Getting the URI string length.
    uint32_t channel_name_len_chars = (uint32_t)strlen(channel_name);
    if (channel_name_len_chars >= MixedCodeConstants::MAX_URI_STRING_LEN)
        return SCERRHANDLERINFOEXCEEDSLIMITS;

    BMX_HANDLER_INDEX_TYPE i, empty_slot = max_num_entries_;

    // Running throw existing handlers.
    for (i = 0; i < max_num_entries_; i++)
    {
        // Checking if empty slot.
        if (registered_handlers_[i].IsEmpty())
        {
            empty_slot = i;
        }
        // Checking current handler type to be port handler.
        else if (WS_HANDLER == registered_handlers_[i].get_type())
        {
            // Checking if port is the same.
            if (port == registered_handlers_[i].get_port())
            {
                // Checking if URI string is the same.
                if (!strcmp(channel_name, registered_handlers_[i].get_original_uri_info()))
                {
                    // Disallowing handler duplicates.
                    return SCERRHANDLERALREADYREGISTERED;
                }
            }
        }
    }

    // Constructing handler info from slot index and unique number.
    *phandler_info = MakeHandlerInfo(empty_slot, unique_handler_num_);

    // Initializing new handlers list.
    err_code = registered_handlers_[empty_slot].Init(
        bmx::HANDLER_TYPE::WS_HANDLER,
        *phandler_info,
        managed_handler_index,
        port,
        app_name,
        channel_id,
        channel_name,
        NULL,
        0,
        0,
        MixedCodeConstants::NetworkProtocolType::PROTOCOL_WEBSOCKETS);

    if (err_code)
        return err_code;

    // Adding handler to the list.
    err_code = registered_handlers_[empty_slot].AddUserHandler(ws_handler);
    if (err_code)
        return err_code;

    // Adding new handler.
    if (empty_slot == max_num_entries_)
        max_num_entries_++;

    return err_code;
}

// Unregisters certain handler.
uint32_t BmxData::UnregisterHandler(
    BMX_HANDLER_INDEX_TYPE handler_index,
    GENERIC_HANDLER_CALLBACK user_handler,
    bool* is_empty_handler)
{
    // We don't know if handler will become empty.
    *is_empty_handler = false;

    // Checking all registered handlers.
    uint32_t err_code = 0;
    for (BMX_HANDLER_INDEX_TYPE i = 0; i < max_num_entries_; i++)
    {
        // Checking if the same handler.
        if (handler_index == registered_handlers_[i].get_handler_index())
        {
            // Unregistering certain handler.
            if (!user_handler)
                err_code = registered_handlers_[i].Unregister();
            else
                err_code = registered_handlers_[i].Unregister(user_handler);

            if (err_code)
                return err_code;

            // Checking if it was the last handler.
            if (registered_handlers_[i].IsEmpty())
            {
                // Slot is empty.
                *is_empty_handler = true;
            }

            return err_code;
        }
    }

    // If not removed.
    return SCERRHANDLERNOTFOUND;
}

// Finds certain handler.
bool BmxData::IsHandlerExist(
    BMX_HANDLER_INDEX_TYPE handler_index)
{
    // Checking all registered handlers.
    for (BMX_HANDLER_INDEX_TYPE i = 0; i < max_num_entries_; i++)
    {
        if (!registered_handlers_[i].IsEmpty())
        {
            if (handler_index == registered_handlers_[i].get_handler_index())
            {
                return true;
            }
        }
    }

    return false;
}

// Finds certain handler.
uint32_t BmxData::FindUriHandler(
    uint16_t port_num,
    const char* processed_uri_info,
    BMX_HANDLER_INDEX_TYPE* handler_index)
{
    // Checking all registered handlers.
    for (BMX_HANDLER_INDEX_TYPE i = 0; i < max_num_entries_; i++)
    {
        if (!registered_handlers_[i].IsEmpty())
        {
            if (URI_HANDLER == registered_handlers_[i].get_type())
            {
                if (port_num == registered_handlers_[i].get_port())
                {
                    if (!strcmp(processed_uri_info, registered_handlers_[i].get_processed_uri_info()))
                    {
                        *handler_index = i;
                        return 0;
                    }
                }
            }
        }
    }

    return SCERRHANDLERNOTFOUND;
}

uint32_t BmxData::FindWsHandler(
    uint16_t port_num,
    const char* channel_name,
    BMX_HANDLER_INDEX_TYPE* handler_index)
{
    // Checking all registered handlers.
    for (BMX_HANDLER_INDEX_TYPE i = 0; i < max_num_entries_; i++)
    {
        if (!registered_handlers_[i].IsEmpty())
        {
            if (WS_HANDLER == registered_handlers_[i].get_type())
            {
                if (port_num == registered_handlers_[i].get_port())
                {
                    if (!strcmp(channel_name, registered_handlers_[i].get_original_uri_info()))
                    {
                        *handler_index = i;
                        return 0;
                    }
                }
            }
        }
    }

    return SCERRHANDLERNOTFOUND;
}

// Finds certain handler.
uint32_t BmxData::FindPortHandler(
    uint16_t port_num,
    BMX_HANDLER_INDEX_TYPE* handler_index)
{
    // Checking all registered handlers.
    for (BMX_HANDLER_INDEX_TYPE i = 0; i < max_num_entries_; i++)
    {
        if (!registered_handlers_[i].IsEmpty())
        {
            if (PORT_HANDLER == registered_handlers_[i].get_type())
            {
                if (port_num == registered_handlers_[i].get_port())
                {
                    *handler_index = i;
                    return 0;
                }
            }
        }
    }

    return SCERRHANDLERNOTFOUND; 
}

// Unregisters certain handler.
uint32_t BmxData::UnregisterHandler(BMX_HANDLER_INDEX_TYPE handler_index, bool* is_empty_handler)
{
    return UnregisterHandler(handler_index, NULL, is_empty_handler);
}

// Handles destroyed session message.
uint32_t BmxData::HandleSessionDestruction(request_chunk_part* request, TASK_INFO_TYPE* task_info)
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
uint32_t BmxData::HandleErrorFromGateway(request_chunk_part* request, TASK_INFO_TYPE* task_info)
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

// Main message loop for incoming requests. Handles the 
// dispatching of the message to the correct handler as 
// well as sending any responses back.
uint32_t BmxData::HandleBmxChunk(CM2_TASK_DATA* task_data)
{
    uint8_t* raw_chunk;
    uint32_t err_code;
    uint32_t loop_count;
    //uint64_t session_id;
    shared_memory_chunk* smc;
    TASK_INFO_TYPE task_info;

    // Initializing task information.
#if 0
    task_info.session_id.high = 0;
    task_info.session_id.low = 0;
#endif
    cm3_get_cpun(0, &task_info.scheduler_number);
    task_info.client_worker_id = (uint8_t)task_data->Output1;
    task_info.the_chunk_index = (uint32_t)task_data->Output2;

//do_work:
    loop_count = 0;

    // Retrieve the chunk.
    err_code = cm_get_shared_memory_chunk(task_info.the_chunk_index, &raw_chunk);
    _SC_ASSERT(err_code == 0);

    // Read the metadata in the chunk (session id and handler id).
    smc = (shared_memory_chunk*)raw_chunk;
    
    BMX_HANDLER_TYPE handler_info = smc->get_bmx_handler_info();
    task_info.handler_info = handler_info;
    BMX_HANDLER_INDEX_TYPE handler_index = GetBmxHandlerIndex(handler_info);
    _SC_ASSERT(handler_index < unique_handler_num_);

    // Checking if handler exists (ignoring if wrong handler).
    if (registered_handlers_[handler_index].IsEmpty())
        goto release_chunks;

    if (smc->get_link() != smc->link_terminator)
        task_info.flags |= MixedCodeConstants::LINKED_CHUNKS_FLAG;

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

            // Checking for the unique handler number.
            _SC_ASSERT(handler_info == registered_handlers_[handler_index].get_handler_info());

            // Running user handler.
            err_code = registered_handlers_[handler_index].RunHandlers(smc, &task_info);
            _SC_ASSERT(0 == err_code);

            cur_chunk_index = new_chunk_index;
            task_info.the_chunk_index = new_chunk_index;

            err_code = cm_get_shared_memory_chunk(new_chunk_index, (uint8_t**) &smc);
            _SC_ASSERT(err_code == 0);
        }

        goto finish;
    }

    // Checking for the unique handler number.
    if (handler_info == registered_handlers_[handler_index].get_handler_info())
    {
        // Running user handler.
        err_code = registered_handlers_[handler_index].RunHandlers(smc, &task_info);
        goto finish;
    }
    else
    {
        // Just releasing the chunk.
        goto release_chunks;
    }

/*try_receive:
    // Check if more chunks are available. If so repeat from beginning.
    err_code = cm_try_receive_from_client((chunk_index_type*)&task_info.chunk_index);
    if (!err_code) goto do_work;
    if (err_code != SCERRWAITTIMEOUT) goto finish;

    loop_count++;
    if (loop_count < 100000) goto try_receive;
    err_code = 0;

    goto finish;*/

release_chunks:

    // Just releasing the chunk.
    cm_release_linked_shared_memory_chunks(task_info.the_chunk_index);

finish:

    // Resetting current transaction.
    star_set_current_transaction(0, 0, 0); // You may comment this line to avoid throwing an exception when using NODB.

    return err_code;

}