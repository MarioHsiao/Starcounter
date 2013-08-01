#include "bmx.hpp"

EXTERN_C uint32_t __stdcall sccoredb_set_current_transaction(int32_t unlock_tran_from_thread, uint64_t handle, uint64_t verify);

using namespace starcounter::bmx;
using namespace starcounter::core;

// Pushes registered port handler.
uint32_t HandlersList::PushRegisteredPortHandler(BmxData* bmx_data)
{
    // Checking if we are ready to push.
    if (!bmx_data->get_push_ready())
        return 0;

    starcounter::core::chunk_index chunk_index;
    shared_memory_chunk* smc;

    // If have a channel to push on: Lets send the registration immediately.
    uint32_t err_code = cm_acquire_shared_memory_chunk(&chunk_index, (uint8_t**)&smc);
    if (err_code)
        return err_code;

    // First we need to reset chunk using request.
    smc->get_request_chunk()->reset_offset();

    // Filling the chunk.
    smc->set_bmx_handler_info(BMX_MANAGEMENT_HANDLER_INFO);

    response_chunk_part *resp_chunk = smc->get_response_chunk();
    resp_chunk->reset_offset();

    // Writing handler information into chunk.
    if (!WriteRegisteredPortHandler(resp_chunk))
        return SCERRUNSPECIFIED; // SCERRTOOBIGHANDLERINFO

    // Terminating last chunk.
    smc->terminate_link();

    // Sending prepared chunk to client.
    err_code = cm_send_to_client(chunk_index);

    return err_code;
}

// Pushes registered subport handler.
uint32_t HandlersList::PushRegisteredSubportHandler(BmxData* bmx_data)
{
    // Checking if we are ready to push.
    if (!bmx_data->get_push_ready())
        return 0;

    starcounter::core::chunk_index chunk_index;
    shared_memory_chunk* smc;

    // If have a channel to push on: Lets send the registration immediately.
    uint32_t err_code = cm_acquire_shared_memory_chunk(&chunk_index, (uint8_t**)&smc);
    if (err_code)
        return err_code;

    // First we need to reset chunk using request.
    smc->get_request_chunk()->reset_offset();

    // Filling the chunk.
    smc->set_bmx_handler_info(BMX_MANAGEMENT_HANDLER_INFO);

    response_chunk_part *resp_chunk = smc->get_response_chunk();
    resp_chunk->reset_offset();

    // Writing handler information into chunk.
    if (!WriteRegisteredSubPortHandler(resp_chunk))
        return SCERRUNSPECIFIED; // SCERRTOOBIGHANDLERINFO

    // Terminating last chunk.
    smc->terminate_link();

    // Sending prepared chunk to client.
    err_code = cm_send_to_client(chunk_index);

    return err_code;
}

// Pushes registered URI handler.
uint32_t HandlersList::PushRegisteredUriHandler(BmxData* bmx_data)
{
    // Checking if we are ready to push.
    if (!bmx_data->get_push_ready())
        return 0;

    starcounter::core::chunk_index chunk_index;
    shared_memory_chunk* smc;

    // If have a channel to push on: Lets send the registration immediately.
    uint32_t err_code = cm_acquire_shared_memory_chunk(&chunk_index, (uint8_t**)&smc);
    if (err_code)
        return err_code;

    // First we need to reset chunk using request.
    smc->get_request_chunk()->reset_offset();

    // Filling the chunk.
    smc->set_bmx_handler_info(BMX_MANAGEMENT_HANDLER_INFO);

    response_chunk_part *resp_chunk = smc->get_response_chunk();
    resp_chunk->reset_offset();

    // Writing handler information into chunk.
    if (!WriteRegisteredUriHandler(resp_chunk))
        return SCERRUNSPECIFIED; // SCERRTOOBIGHANDLERINFO

    // Terminating last chunk.
    smc->terminate_link();

    // Sending prepared chunk to client.
    err_code = cm_send_to_client(chunk_index);

    return err_code;
}

// Registers port handler.
uint32_t BmxData::RegisterPortHandler(
    uint16_t port_num,
    GENERIC_HANDLER_CALLBACK port_handler,
    BMX_HANDLER_TYPE* handler_id)
{
    // Checking number of handlers.
    if (max_num_entries_ >= MAX_TOTAL_NUMBER_OF_HANDLERS)
        return SCERRUNSPECIFIED; // SCERRMAXHANDLERSREACHED

    unique_handler_num_++;

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

                // Search if handler is already in the list.
                if (!registered_handlers_[i].HandlerAlreadyExists(port_handler))
                {
                    // Adding new handler to the list.
                    err_code = registered_handlers_[i].AddUserHandler(port_handler);
                    if (err_code)
                        return err_code;
                }

                // Assigning existing handler id.
                *handler_id = i;

                return err_code;
            }
        }
    }

    // Initializing new handlers list.
    err_code = registered_handlers_[empty_slot].Init(
        PORT_HANDLER,
        MakeHandlerInfo(empty_slot, unique_handler_num_),
        port_num,
        0,
        NULL,
        0,
        NULL,
        0,
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
    *handler_id = empty_slot;
    if (empty_slot == max_num_entries_)
        max_num_entries_++;

    return err_code;
}

// Registers sub-port handler.
uint32_t BmxData::RegisterSubPortHandler(
    uint16_t port,
    BMX_SUBPORT_TYPE subport,
    GENERIC_HANDLER_CALLBACK subport_handler, 
    BMX_HANDLER_TYPE* handler_id)
{
    // Checking number of handlers.
    if (max_num_entries_ >= MAX_TOTAL_NUMBER_OF_HANDLERS)
        return SCERRUNSPECIFIED; // SCERRMAXHANDLERSREACHED

    unique_handler_num_++;

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
        // Checking handler type.
        else if (bmx::HANDLER_TYPE::SUBPORT_HANDLER == registered_handlers_[i].get_type())
        {
            // Checking that port is correct.
            if (port == registered_handlers_[i].get_port())
            {
                // Checking that sub-port is correct.
                if (subport == registered_handlers_[i].get_subport())
                {
                    // Disallowing handler duplicates.
                    return SCERRHANDLERALREADYREGISTERED;

                    // Search if handler is already in the list.
                    if (!registered_handlers_[i].HandlerAlreadyExists(subport_handler))
                    {
                        // Adding new handler to the list.
                        err_code = registered_handlers_[i].AddUserHandler(subport_handler);
                        if (err_code)
                            return err_code;
                    }

                    // Assigning existing handler id.
                    *handler_id = i;

                    return err_code;
                }
            }
        }
    }

    // Initializing new handlers list.
    err_code = registered_handlers_[empty_slot].Init(
        SUBPORT_HANDLER,
        MakeHandlerInfo(empty_slot, unique_handler_num_),
        port,
        subport,
        NULL,
        0,
        NULL,
        0,
        NULL,
        0,
        MixedCodeConstants::PROTOCOL_SUB_PORT);

    if (err_code)
        return err_code;

    // Adding handler to the list.
    err_code = registered_handlers_[empty_slot].AddUserHandler(subport_handler);
    if (err_code)
        return err_code;

    // Saving handler id.
    *handler_id = empty_slot;
    if (empty_slot == max_num_entries_)
        max_num_entries_++;

    return err_code;
}

// Registers URI handler.
uint32_t BmxData::RegisterUriHandler(
    uint16_t port,
    char* original_uri_info,
    char* processed_uri_info,
    uint8_t* param_types,
    int32_t num_params,
    GENERIC_HANDLER_CALLBACK uri_handler, 
    BMX_HANDLER_TYPE* handler_id,
    starcounter::MixedCodeConstants::NetworkProtocolType proto_type)
{
    // Checking number of handlers.
    if (max_num_entries_ >= MAX_TOTAL_NUMBER_OF_HANDLERS)
        return SCERRUNSPECIFIED; // SCERRMAXHANDLERSREACHED

    unique_handler_num_++;

    // Checking if URI starts with slash.
    //if (uri_string[0] != '/')
    //    return SCERRUNSPECIFIED; // SCERRURIMUSTSTARTWITHSLASH

    uint32_t err_code = 0;

    // Getting the URI string length.
    uint32_t original_uri_len_chars = (uint32_t)strlen(original_uri_info);
    uint32_t processed_uri_len_chars = (uint32_t)strlen(processed_uri_info);
    if ((original_uri_len_chars >= MixedCodeConstants::MAX_URI_STRING_LEN) ||
        (processed_uri_len_chars >= MixedCodeConstants::MAX_URI_STRING_LEN))
        return SCERRUNSPECIFIED; // SCERRURIHANDLERSTRINGLENGTH

    // Copying the URI string.
    //strncpy_s(uri_str_lc, MAX_URI_STRING_LEN, original_uri_info, original_uri_len_chars);

    // Convert string to lower case.
    // TODO: Remove lower casing if not needed.
    //_strlwr_s(uri_str_lc);

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

                    /*
                    // Search if handler is already in the list.
                    if (!registered_handlers_[i].HandlerAlreadyExists(uri_handler))
                    {
                        // Adding new handler to the list.
                        err_code = registered_handlers_[i].AddUserHandler(uri_handler);
                        if (err_code)
                            return err_code;
                    }

                    // Assigning existing handler id.
                    *handler_id = i;

                    return err_code;
                    */
                }
            }
        }
    }

    // Initializing new handlers list.
    err_code = registered_handlers_[empty_slot].Init(
        bmx::HANDLER_TYPE::URI_HANDLER,
        MakeHandlerInfo(empty_slot, unique_handler_num_),
        port,
        0,
        original_uri_info,
        original_uri_len_chars,
        processed_uri_info,
        processed_uri_len_chars,
        param_types,
        num_params,
        proto_type);

    if (err_code)
        return err_code;

    // Adding handler to the list.
    err_code = registered_handlers_[empty_slot].AddUserHandler(uri_handler);
    if (err_code)
        return err_code;

    // Adding new handler.
    *handler_id = empty_slot;
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
    uint32_t err_code = SCERRUNSPECIFIED;
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
    char* processed_uri_info,
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
                    if (!strcmp(processed_uri_info, registered_handlers_[i].get_original_uri_info()))
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

// Finds certain handler.
uint32_t BmxData::FindSubportHandler(
    uint16_t port_num,
    BMX_SUBPORT_TYPE subport_num,
    BMX_HANDLER_INDEX_TYPE* handler_index)
{
    // Checking all registered handlers.
    for (BMX_HANDLER_INDEX_TYPE i = 0; i < max_num_entries_; i++)
    {
        if (!registered_handlers_[i].IsEmpty())
        {
            if (SUBPORT_HANDLER == registered_handlers_[i].get_type())
            {
                if (port_num == registered_handlers_[i].get_port())
                {
                    if (subport_num == registered_handlers_[i].get_subport())
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

// Unregisters certain handler.
uint32_t BmxData::UnregisterHandler(BMX_HANDLER_INDEX_TYPE handler_index, bool* is_empty_handler)
{
    return UnregisterHandler(handler_index, NULL, is_empty_handler);
}

// Number of schedulers.
static uint8_t g_schedulers_count = 0;

// Registers push channel and send the response.
uint32_t BmxData::SendRegisterPushChannelResponse(shared_memory_chunk* smc, TASK_INFO_TYPE* task_info)
{
    // Entering critical section.
    uint32_t err_code = 0;

    // NOTE:
    // Channel attached to thread. No storing away channel reference in
    // shared memory.
    err_code = coalmine_set_current_channel_as_default();
    if (err_code)
        return err_code;

    // Increasing number of registered push channels.
    InterlockedIncrement(&num_registered_push_channels_);

    // Getting number of schedulers if needed.
    if (!g_schedulers_count)
        cm3_get_cpuc(NULL, &g_schedulers_count);

    // Checking if all push channels are registered.
    if (get_num_registered_push_channels() >= g_schedulers_count)
        set_push_ready();

    response_chunk_part *response = smc->get_response_chunk();
    response->reset_offset();

    // Writing response on push channel registration.
    response->write(BMX_REGISTER_PUSH_CHANNEL_RESPONSE);

    // Now the chunk is ready to be sent.
    err_code = cm_send_to_client(task_info->chunk_index);

    return err_code;
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
    uint32_t err_code = cm_release_linked_shared_memory_chunks(task_info->chunk_index);
    if (err_code)
        return err_code;

    return 0;
}

// Handle new session creation.
uint32_t BmxData::HandleSessionCreation(shared_memory_chunk* smc, TASK_INFO_TYPE* task_info)
{
    uint32_t* plinear_index = (uint32_t*)((uint8_t*)smc + MixedCodeConstants::CHUNK_OFFSET_SESSION_LINEAR_INDEX);
    uint64_t* prandom_salt = (uint64_t*)((uint8_t*)smc + MixedCodeConstants::CHUNK_OFFSET_SESSION_RANDOM_SALT);
    uint32_t* preserved = (uint32_t*)((uint8_t*)smc + MixedCodeConstants::CHUNK_OFFSET_SESSION_RESERVED_INDEX);

    // Calling managed function to destroy session.
    g_create_new_apps_session_callback(task_info->scheduler_number, plinear_index, prandom_salt, preserved);

    //std::cout << "Session " << apps_unique_session_index << ":" << apps_session_salt << " was created." << std::endl;

    // First we need to reset chunk using request.
    smc->get_request_chunk()->reset_offset();

    bmx_handler_type* handler_id = (bmx_handler_type*)((uint8_t*)smc + MixedCodeConstants::CHUNK_OFFSET_SAVED_USER_HANDLER_ID);

    // Setting fixed handler id in response.
    smc->set_bmx_handler_info(*handler_id);

    response_chunk_part *resp_chunk = smc->get_response_chunk();
    resp_chunk->reset_offset();

    // Now the chunk is ready to be sent back.
    uint32_t err_code = cm_send_to_client(task_info->chunk_index);

    return err_code;
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
    cm_release_linked_shared_memory_chunks(task_info->chunk_index);

    return err_code;
}

// Sends information about all registered handlers.
uint32_t BmxData::SendAllHandlersInfo(shared_memory_chunk* smc, TASK_INFO_TYPE* task_info)
{
    // Entering critical section.
    uint32_t err_code = 0;

    // TODO: fix linked chunks when not enough chunk space.

    // Filling the chunk.
    response_chunk_part *response = smc->get_response_chunk();
    response->reset_offset();

    // Sending information about each registered handler.
    // NOTE: Gateway needs to know only about the registered handler id of the particular type.
    // No need to know about individual handlers within handlers list.
    // So push should be done only when first handler is registered on the list.
    // And unregistration - when last handler is removed from the list.

    // Skipping first handler (BMX management).
    for (BMX_HANDLER_TYPE i = 1; i < max_num_entries_; i++)
    {
        // Checking the type of handler.
        switch(registered_handlers_[i].get_type())
        {
            case PORT_HANDLER:
            {
                if (!registered_handlers_[i].WriteRegisteredPortHandler(response))
                {
                    // TODO: Solve multi linked chunks.
                    return SCERRUNSPECIFIED; // SCERRTOOBIGHANDLERINFO
                }

                break;
            }

            case SUBPORT_HANDLER:
            {
                if (!registered_handlers_[i].WriteRegisteredSubPortHandler(response))
                {
                    // TODO: Solve multi linked chunks.
                    return SCERRUNSPECIFIED; // SCERRTOOBIGHANDLERINFO
                }

                break;
            }

            case URI_HANDLER:
            {
                if (!registered_handlers_[i].WriteRegisteredUriHandler(response))
                {
                    // TODO: Solve multi linked chunks.
                    return SCERRUNSPECIFIED; // SCERRTOOBIGHANDLERINFO
                }

                break;
            }

            default:
            {
                err_code = SCERRUNSPECIFIED; // SCERRUNKNOWNHANDLER

                break;
            }
        }
    }

    // Terminating last chunk.
    smc->terminate_link();

    // Checking that there was no error.
    if ((!err_code) && (max_num_entries_ > 1))
    {
        // Now the chunk is ready to be sent.
        err_code = cm_send_to_client(task_info->chunk_index);
    }

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
    task_info.chunk_index = (uint32_t)task_data->Output2;

//do_work:
    loop_count = 0;

    // Retrieve the chunk.
    err_code = cm_get_shared_memory_chunk(task_info.chunk_index, &raw_chunk);
    assert(err_code == 0);

    // Read the metadata in the chunk (session id and handler id).
    smc = (shared_memory_chunk*)raw_chunk;
    //session_id = smc->get_user_data(); The user data field in chunks is no longer used.
    BMX_HANDLER_TYPE handler_info = smc->get_bmx_handler_info();
    BMX_HANDLER_INDEX_TYPE handler_index = GetBmxHandlerIndex(handler_info);
    assert(handler_index < unique_handler_num_);

    // Checking if handler exists (ignoring if wrong handler).
    if (registered_handlers_[handler_index].IsEmpty())
        goto release_chunks;

    task_info.handler_index = (BMX_HANDLER_INDEX_TYPE)handler_info;
    if (smc->get_link() != smc->link_terminator)
        task_info.flags |= MixedCodeConstants::LINKED_CHUNKS_FLAG;

    // TODO: Init session values.

    // Switching the session if needed.
    //	errorcode = CheckAndSwitchSession(&task_info, session_id);
    //	if (errorcode != 0) goto finish;

    // TODO:
    // tag the chunk with needed metadata.

    // Send the response back.
    //	errorcode = cm_send_to_client(task_info.chunk_index);
    //	if (errorcode != 0) goto finish;

    // Checking for the unique handler number.
    if (handler_info == registered_handlers_[handler_index].get_handler_info())
    {
        // Running user handler.
        err_code = registered_handlers_[handler_index].RunHandlers(0 /*session_id*/, smc, &task_info);
        goto finish;
    }
    else
    {
        // Just releasing the chunk.
        goto release_chunks;
    }

/*
try_receive:
    // Check if more chunks are available. If so repeat from beginning.
    err_code = cm_try_receive_from_client((DWORD*)&task_info.chunk_index);
    if (!err_code) goto do_work;
    if (err_code != SCERRWAITTIMEOUT) goto finish;

    loop_count++;
    if (loop_count < SCHEDULER_SPIN_COUNT) goto try_receive;
    err_code = 0;
*/

release_chunks:

    // Just releasing the chunk.
    cm_release_linked_shared_memory_chunks(task_info.chunk_index);

finish:

    // Resetting current transaction.
    sccoredb_set_current_transaction(0, 0, 0); // You may comment this line to avoid throwing an exception when using NODB.
#if 0
	if (task_info.session_id.low != 0)
    {
        sccorensm_leave_session(task_info.session_id.high);
        task_info.session_id.low = 0;
    }
#endif

    return err_code;

}

#if 0
// Checks if session has changed from current one.
uint32_t BmxData::CheckAndSwitchSession(TASK_INFO_TYPE* task_info, uint64_t session_id)
{
    uint64_t current_session_low;

    current_session_low = task_info->session_id.low;
    if (current_session_low != session_id)
    {
        if (current_session_low != 0)
        {
            // Leave the old session before enter the new one.
            sccorensm_leave_session(task_info->session_id.high);
        }

        task_info->session_id.low = session_id;
        task_info->session_id.high = 0;

        uint32_t err_code = sccorensm_enter_session_and_get_default_transaction(
            task_info->scheduler_number,
            task_info->session_id, 
            &task_info->session_id.high,
            &task_info->transaction_handle
            );

        if (!err_code)
            return 0;

        task_info->session_id.low = 0;
        return err_code;
    }

    return 0;
}
#endif

// Pushes unregistered handler.
uint32_t BmxData::PushHandlerUnregistration(BMX_HANDLER_TYPE handler_info)
{
    // Checking if we are ready to push.
    if (!get_push_ready())
        return 0;

    starcounter::core::chunk_index chunk_index;
    shared_memory_chunk* smc;

    // If have a channel to push on: Lets send the registration immediately.
    uint32_t err_code = cm_acquire_shared_memory_chunk(&chunk_index, (uint8_t**)&smc);
    if (err_code)
        return err_code;

    // This is going to be a BMX management chunk.
    smc->set_bmx_handler_info(BMX_MANAGEMENT_HANDLER_INFO);
    request_chunk_part* request = smc->get_request_chunk();
    request->reset_offset();

    // Writing BMX unregister message.
    request->write(BMX_UNREGISTER);
    request->write(handler_info);

    // Terminating last chunk.
    smc->terminate_link();

    // Sending prepared chunk to client.
    err_code = cm_send_to_client(chunk_index);

    return err_code;
}