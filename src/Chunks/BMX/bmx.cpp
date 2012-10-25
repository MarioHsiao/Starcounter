#include "bmx.hpp"

EXTERN_C uint32_t SCAPI sccoredb_set_current_transaction(int32_t unlock_tran_from_thread, uint64_t handle, uint64_t verify);

using namespace starcounter::bmx;
using namespace starcounter::core;

// Registers port handler.
uint32_t BmxData::RegisterPortHandler(
    uint16_t port_num,
    GENERIC_HANDLER_CALLBACK port_handler,
    BMX_HANDLER_TYPE* handler_id)
{
    // Checking number of handlers.
    if (max_num_entries_ >= MAX_TOTAL_NUMBER_OF_HANDLERS)
        return SCERRUNSPECIFIED; // SCERRMAXHANDLERSREACHED

    uint32_t err_code = 0;

    BMX_HANDLER_TYPE i, empty_slot = max_num_entries_;

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

                // Sending the handler registration information (only during first handler registration in the list).
                err_code = PushRegisteredPortHandler(*handler_id, port_num);

                return err_code;
            }
        }
    }

    // Initializing new handlers list.
    err_code = registered_handlers_[empty_slot].Init(PORT_HANDLER, empty_slot, port_num, 0, NULL, 0, bmx::HTTP_METHODS::OTHER_METHOD);
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

    // Sending the handler registration information (only during first handler registration in the list).
    err_code = PushRegisteredPortHandler(*handler_id, port_num);

    return err_code;
}

// Registers sub-port handler.
uint32_t BmxData::RegisterSubPortHandler(
    uint16_t port,
    uint32_t subport,
    GENERIC_HANDLER_CALLBACK subport_handler, 
    BMX_HANDLER_TYPE* handler_id)
{
    // Checking number of handlers.
    if (max_num_entries_ >= MAX_TOTAL_NUMBER_OF_HANDLERS)
        return SCERRUNSPECIFIED; // SCERRMAXHANDLERSREACHED

    uint32_t err_code = 0;

    BMX_HANDLER_TYPE i, empty_slot = max_num_entries_;

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

                    // Sending the handler registration information (only during first handler registration in the list).
                    err_code = PushRegisteredSubportHandler(*handler_id, port, subport);

                    return err_code;
                }
            }
        }
    }

    // Initializing new handlers list.
    err_code = registered_handlers_[empty_slot].Init(SUBPORT_HANDLER, empty_slot, port, subport, NULL, 0, bmx::HTTP_METHODS::OTHER_METHOD);
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

    // Sending the handler registration information (only during first handler registration in the list).
    err_code = PushRegisteredSubportHandler(*handler_id, port, subport);

    return err_code;
}

// Registers URI handler.
uint32_t BmxData::RegisterUriHandler(
    uint16_t port,
    char* uri_string,
    HTTP_METHODS http_method,
    GENERIC_HANDLER_CALLBACK uri_handler, 
    BMX_HANDLER_TYPE* handler_id)
{
    // Checking number of handlers.
    if (max_num_entries_ >= MAX_TOTAL_NUMBER_OF_HANDLERS)
        return SCERRUNSPECIFIED; // SCERRMAXHANDLERSREACHED

    // Checking if URI starts with slash.
    //if (uri_string[0] != '/')
    //    return SCERRUNSPECIFIED; // SCERRURIMUSTSTARTWITHSLASH

    uint32_t err_code = 0;
    char uri_str_lc[MAX_URI_STRING_LEN];

    // Getting the URI string length.
    uint32_t uri_len_chars = (uint32_t)strlen(uri_string);
    if (uri_len_chars >= MAX_URI_STRING_LEN)
        return SCERRUNSPECIFIED; // SCERRURIHANDLERSTRINGLENGTH

    // Copying the URI string.
    strncpy_s(uri_str_lc, MAX_URI_STRING_LEN, uri_string, uri_len_chars);

    // Convert string to lower case.
    _strlwr_s(uri_str_lc);

    BMX_HANDLER_TYPE i, empty_slot = max_num_entries_;

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
                if (!strcmp(uri_str_lc, registered_handlers_[i].get_uri()))
                {
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

                    // Sending the handler registration information (only during first handler registration in the list).
                    err_code = PushRegisteredUriHandler(*handler_id, port, uri_str_lc, uri_len_chars, http_method);

                    return err_code;
                }
            }
        }
    }

    // Initializing new handlers list.
    err_code = registered_handlers_[empty_slot].Init(bmx::HANDLER_TYPE::URI_HANDLER, empty_slot, port, 0, uri_str_lc, uri_len_chars, http_method);
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

    // Sending the handler registration information (only during first handler registration in the list).
    err_code = PushRegisteredUriHandler(*handler_id, port, uri_str_lc, uri_len_chars, http_method);

    return err_code;
}

// Unregisters certain handler.
uint32_t BmxData::UnregisterHandler(BMX_HANDLER_TYPE handler_id, GENERIC_HANDLER_CALLBACK user_handler)
{
    // Checking all registered handlers.
    uint32_t err_code = SCERRUNSPECIFIED;
    for (BMX_HANDLER_TYPE i = 0; i < max_num_entries_; i++)
    {
        // Checking if the same handler.
        if (handler_id == registered_handlers_[i].get_handler_id())
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
                // Pushing handler unregistration to the client.
                err_code = PushHandlerUnregistration(handler_id);
            }

            return err_code;
        }
    }

    // If not removed.
    return SCERRUNSPECIFIED; // SCERRHANDLERNOTFOUND 
}

// Unregisters certain handler.
uint32_t BmxData::UnregisterHandler(BMX_HANDLER_TYPE handler_id)
{
    return UnregisterHandler(handler_id, NULL);
}

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
    num_registered_push_channels_++;

    response_chunk_part *response = smc->get_response_chunk();
    response->reset_offset();

    // Writing response on push channel registration.
    response->write(BMX_REGISTER_PUSH_CHANNEL_RESPONSE);

    // Now the chunk is ready to be sent.
    err_code = cm_send_to_client(task_info->chunk_index);

    return err_code;
}

// Handles destroyed session message.
uint32_t BmxData::HandleDestroyedSession(request_chunk_part* request, TASK_INFO_TYPE* task_info)
{
    // Entering critical section.
    uint32_t err_code = 0;

    // Reading Apps unique session number.
    uint64_t apps_unique_session_num = request->read_uint64();

    std::cout << "Session " << apps_unique_session_num << " was destroyed." << std::endl;

    // TODO: Handle destroyed session.

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
                if (!WriteRegisteredPortHandler(
                    response,
                    registered_handlers_[i].get_handler_id(),
                    registered_handlers_[i].get_port()))
                {
                    // TODO: Solve multi linked chunks.
                    return SCERRUNSPECIFIED; // SCERRTOOBIGHANDLERINFO
                }

                break;
            }

            case SUBPORT_HANDLER:
            {
                if (!WriteRegisteredSubPortHandler(
                    response,
                    registered_handlers_[i].get_handler_id(),
                    registered_handlers_[i].get_port(),
                    registered_handlers_[i].get_subport()))
                {
                    // TODO: Solve multi linked chunks.
                    return SCERRUNSPECIFIED; // SCERRTOOBIGHANDLERINFO
                }

                break;
            }

            case URI_HANDLER:
            {
                if (!WriteRegisteredUriHandler(
                    response,
                    registered_handlers_[i].get_handler_id(),
                    registered_handlers_[i].get_port(),
                    registered_handlers_[i].get_uri(),
                    registered_handlers_[i].get_uri_len_chars(),
                    registered_handlers_[i].get_http_method()))
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
    BMX_HANDLER_TYPE handler_id;
    uint8_t* raw_chunk;
    uint32_t err_code;
    uint32_t loop_count;
    uint64_t session_id;
    shared_memory_chunk* smc;
    TASK_INFO_TYPE task_info;

    // Initializing task information.
    task_info.session_id.high = 0;
    task_info.session_id.low = 0;
    cm3_get_cpun(0, &task_info.scheduler_number);
    task_info.chunk_index = (uint32_t)task_data->Output2;

//do_work:
    loop_count = 0;

    // Retrieve the chunk.
    err_code = cm_get_shared_memory_chunk(task_info.chunk_index, &raw_chunk);
    if (err_code != 0)
        goto finish;

    // Read the metadata in the chunk (session id and handler id).
    smc = (shared_memory_chunk*)raw_chunk;
    session_id = smc->get_user_data();
    handler_id = smc->get_bmx_protocol();

    // Checking if handler exists (ignoring if wrong handler).
    if (registered_handlers_[handler_id].IsEmpty())
        goto finish;

    task_info.handler_id = handler_id;
    if (smc->get_link() != smc->LINK_TERMINATOR)
        task_info.flags |= LINKED_CHUNK;

    // TODO: Init session values.

    // Switching the session if needed.
    //	errorcode = CheckAndSwitchSession(&task_info, session_id);
    //	if (errorcode != 0) goto finish;

    // TODO:
    // tag the chunk with needed metadata.

    // Send the response back.
    //	errorcode = cm_send_to_client(task_info.chunk_index);
    //	if (errorcode != 0) goto finish;

    // Running user handler.
    err_code = registered_handlers_[handler_id].RunHandlers(session_id, smc, &task_info);
    if (err_code)
        goto finish;

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
finish:

    // Resetting current transaction.
    sccoredb_set_current_transaction(0, 0, 0);
    if (task_info.session_id.low != 0)
    {
        sccorensm_leave_session(task_info.session_id.high);
        task_info.session_id.low = 0;
    }

    return err_code;
}

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

uint32_t BmxData::PushRegisteredPortHandler(BMX_HANDLER_TYPE handler_id, uint16_t port_num)
{
    uint32_t chunk_index;
    shared_memory_chunk* smc;

    // If have a channel to push on: Lets send the registration immediately.
    uint32_t err_code = AcquireNewChunk(smc, chunk_index);
    if (err_code)
        return err_code;

    // Filling the chunk.
    smc->set_bmx_protocol(BMX_MANAGEMENT_HANDLER);

    response_chunk_part *response = smc->get_response_chunk();
    response->reset_offset();

    // Writing handler information into chunk.
    if (!WriteRegisteredPortHandler(response, handler_id, port_num))
        return SCERRUNSPECIFIED; // SCERRTOOBIGHANDLERINFO

    // Terminating last chunk.
    smc->terminate_link();

    // Sending prepared chunk to client.
    err_code = cm_send_to_client(chunk_index);

    return err_code;
}

uint32_t BmxData::PushRegisteredSubportHandler(BMX_HANDLER_TYPE handler_id, uint16_t port, uint32_t sub_port)
{
    uint32_t chunk_index;
    shared_memory_chunk* smc;

    // If have a channel to push on: Lets send the registration immediately.
    uint32_t err_code = AcquireNewChunk(smc, chunk_index);
    if (err_code)
        return err_code;

    // Filling the chunk.
    smc->set_bmx_protocol(BMX_MANAGEMENT_HANDLER);

    response_chunk_part *resp_chunk = smc->get_response_chunk();
    resp_chunk->reset_offset();

    // Writing handler information into chunk.
    if (!WriteRegisteredSubPortHandler(resp_chunk, handler_id, port, sub_port))
        return SCERRUNSPECIFIED; // SCERRTOOBIGHANDLERINFO

    // Terminating last chunk.
    smc->terminate_link();

    // Sending prepared chunk to client.
    err_code = cm_send_to_client(chunk_index);

    return err_code;
}

uint32_t BmxData::PushRegisteredUriHandler(
    BMX_HANDLER_TYPE handler_id,
    uint16_t port,
    char* uri,
    uint32_t uri_len_chars,
    HTTP_METHODS http_method)
{
    uint32_t chunk_index;
    shared_memory_chunk* smc;

    // If have a channel to push on: Lets send the registration immediately.
    uint32_t err_code = AcquireNewChunk(smc, chunk_index);
    if (err_code)
        return err_code;

    // Filling the chunk.
    smc->set_bmx_protocol(BMX_MANAGEMENT_HANDLER);

    response_chunk_part *resp_chunk = smc->get_response_chunk();
    resp_chunk->reset_offset();

    // Writing handler information into chunk.
    if (!WriteRegisteredUriHandler(resp_chunk, handler_id, port, uri, uri_len_chars, http_method))
        return SCERRUNSPECIFIED; // SCERRTOOBIGHANDLERINFO

    // Terminating last chunk.
    smc->terminate_link();

    // Sending prepared chunk to client.
    err_code = cm_send_to_client(chunk_index);

    return err_code;
}

uint32_t BmxData::PushHandlerUnregistration(BMX_HANDLER_TYPE handler_id)
{
    uint32_t chunk_index;
    shared_memory_chunk* smc;

    // If have a channel to push on: Lets send the registration immediately.
    uint32_t err_code = AcquireNewChunk(smc, chunk_index);
    if (err_code)
        return err_code;

    // This is going to be a BMX management chunk.
    smc->set_bmx_protocol(BMX_MANAGEMENT_HANDLER);
    request_chunk_part* request = smc->get_request_chunk();
    request->reset_offset();

    // Writing BMX unregister message.
    request->write(BMX_UNREGISTER);
    request->write(handler_id);

    // Terminating last chunk.
    smc->terminate_link();

    // Sending prepared chunk to client.
    err_code = cm_send_to_client(chunk_index);

    return err_code;
}

uint32_t BmxData::AcquireNewChunk(shared_memory_chunk*& chunk, uint32_t& chunk_index)
{
    uint8_t* raw_chunk;
    uint32_t errorcode;
    DWORD tmp_chunk_index;

    // TODO: 
    // Combine these 2 cm calls into one.
    errorcode = cm_acquire_shared_memory_chunk(&tmp_chunk_index);
    if (errorcode == 0)
    {
        errorcode = cm_get_shared_memory_chunk(tmp_chunk_index, &raw_chunk);
        if (errorcode == 0)
        {
            chunk_index = tmp_chunk_index;
            chunk = (shared_memory_chunk*)raw_chunk;
        }
    }

    return errorcode;
}

uint32_t BmxData::WriteRegisteredPortHandler(
    response_chunk_part *resp_chunk,
    BMX_HANDLER_TYPE handler_id,
    uint16_t port)
{
    // Checking if message fits the chunk.
    if ((chunk_size - resp_chunk->get_offset() - shared_memory_chunk::LINK_SIZE) <=
        sizeof(BMX_REGISTER_PORT) + sizeof(handler_id) + sizeof(port))
    {
        return 0;
    }

    resp_chunk->write(BMX_REGISTER_PORT);
    resp_chunk->write(handler_id);
    resp_chunk->write(port);

    return resp_chunk->get_offset();
}

uint32_t BmxData::WriteRegisteredSubPortHandler(
    response_chunk_part *resp_chunk, 
    BMX_HANDLER_TYPE handler_id, 
    uint16_t port, 
    uint32_t subport)
{
    // Checking if message fits the chunk.
    if ((chunk_size - resp_chunk->get_offset() - shared_memory_chunk::LINK_SIZE) <=
        sizeof(BMX_REGISTER_PORT_SUBPORT) + sizeof(handler_id) + sizeof(port) + sizeof(subport))
    {
        return 0;
    }

    resp_chunk->write(BMX_REGISTER_PORT_SUBPORT);
    resp_chunk->write(handler_id);
    resp_chunk->write(port);
    resp_chunk->write(subport);

    return resp_chunk->get_offset();
}

uint32_t BmxData::WriteRegisteredUriHandler(
    response_chunk_part *resp_chunk,
    BMX_HANDLER_TYPE handler_id, 
    uint16_t port, 
    char* uri_string,
    uint32_t uri_len_chars,
    HTTP_METHODS http_method)
{
    // Checking if message fits the chunk.
    if ((chunk_size - resp_chunk->get_offset() - shared_memory_chunk::LINK_SIZE) <=
        sizeof(BMX_REGISTER_URI) + sizeof(handler_id) + sizeof(port) + uri_len_chars * sizeof(char) + 1)
    {
        return 0;
    }

    resp_chunk->write(BMX_REGISTER_URI);
    resp_chunk->write(handler_id);
    resp_chunk->write(port);
    resp_chunk->write_string(uri_string, uri_len_chars);
    resp_chunk->write((uint8_t)http_method);

    return resp_chunk->get_offset();
}