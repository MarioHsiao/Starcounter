//
// bmx.hpp
//
// 
//
// Copyright � 2006-2012 Starcounter AB. All rights reserved.
// Starcounter� is a registered trademark of Starcounter AB.
//

#ifndef BMX_HPP
#define BMX_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#define WIN32_LEAN_AND_MEAN
#include <winsock2.h>
#undef WIN32_LEAN_AND_MEAN

#include <stdint.h>
#include <list>
#include <vector>
#include "../../Starcounter.Internal/Constants/MixedCodeConstants.cs"
#include "coalmine.h"
#include "chunk_helper.h"
#include "..\common\chunk.hpp"
#include "sccorensm.h"

// BMX task information.
struct TASK_INFO_TYPE {
    uint8_t flags;
    uint8_t scheduler_number;
    BMX_HANDLER_INDEX_TYPE handler_index;
    uint8_t fill1;
    starcounter::core::chunk_index chunk_index;
    uint64_t transaction_handle;
    SC_SESSION_ID session_id;
};

// User handler callback.
typedef uint32_t (__stdcall *GENERIC_HANDLER_CALLBACK)(
    uint64_t session_id,
    shared_memory_chunk* smc, 
    TASK_INFO_TYPE* task_info,
    bool* is_handled
);

namespace starcounter
{
namespace bmx
{
    typedef uint32_t BMX_SUBPORT_TYPE;

    // Flag indicating that multiple chunks are passed.
    const uint8_t LINKED_CHUNKS_FLAG = 1;

    // Scheduler spin count.
    const uint32_t SCHEDULER_SPIN_COUNT = 1000000;

    // Constants needed for chunks processing.
    const uint32_t CHUNK_MAX_DATA_BYTES = starcounter::core::chunk_size - shared_memory_chunk::link_size;

    // NOTE: Excluding original chunk since its for extra linked.
    const uint32_t MAX_EXTRA_LINKED_WSABUFS = CHUNK_MAX_DATA_BYTES / sizeof(WSABUF) - 1;
    const uint32_t MAX_BYTES_EXTRA_LINKED_WSABUFS = MAX_EXTRA_LINKED_WSABUFS * CHUNK_MAX_DATA_BYTES;

    const uint32_t BMX_HEADER_MAX_SIZE_BYTES = MixedCodeConstants::BMX_HEADER_MAX_SIZE_BYTES;

    // Some socket data constants.
    const uint32_t  SOCKET_DATA_FLAGS_JUST_SEND = 64;

    // WARNING: Change corresponding managed code variables
    // that have exactly the same names!
    const uint32_t SESSION_STRUCT_SIZE = 32;
    const uint32_t OVERLAPPED_STRUCT_SIZE = 32;

    const uint32_t SOCKET_DATA_NUM_CLONE_BYTES = 152;

    // Offset of data blob in socket data.
    const int32_t SOCKET_DATA_OFFSET_BLOB = 712;

    const uint32_t CHUNK_OFFSET_USER_DATA = BMX_HEADER_MAX_SIZE_BYTES + OVERLAPPED_STRUCT_SIZE + SESSION_STRUCT_SIZE;
    const uint32_t CHUNK_OFFSET_MAX_USER_DATA_BYTES = CHUNK_OFFSET_USER_DATA + 4;
    const uint32_t CHUNK_OFFSET_USER_DATA_WRITTEN_BYTES = CHUNK_OFFSET_MAX_USER_DATA_BYTES + 4;
    const uint32_t CHUNK_OFFSET_SOCKET_FLAGS = BMX_HEADER_MAX_SIZE_BYTES + 112;
    const uint32_t BMX_NUM_CLONE_BYTES = BMX_HEADER_MAX_SIZE_BYTES + SOCKET_DATA_NUM_CLONE_BYTES;
    const uint32_t CHUNK_OFFSET_BMX_HTTP_REQUEST = BMX_HEADER_MAX_SIZE_BYTES + MixedCodeConstants::SOCKET_DATA_OFFSET_HTTP_REQUEST;
    const uint32_t SOCKET_DATA_MAX_SIZE = starcounter::core::chunk_size - BMX_HEADER_MAX_SIZE_BYTES - shared_memory_chunk::link_size;

    const BMX_HANDLER_TYPE BMX_INVALID_HANDLER_INFO = ~(BMX_HANDLER_TYPE)0;
    const BMX_HANDLER_INDEX_TYPE BMX_INVALID_HANDLER_INDEX = ~(BMX_HANDLER_INDEX_TYPE)0;

    // Predefined BMX management handler.
    const BMX_HANDLER_TYPE BMX_MANAGEMENT_HANDLER_ID = 0;

    inline BMX_HANDLER_TYPE MakeHandlerInfo(BMX_HANDLER_INDEX_TYPE handler_index, BMX_HANDLER_UNIQUE_NUM_TYPE unique_num)
    {
        return (((uint64_t)unique_num) << 16) | handler_index;
    }

    const BMX_HANDLER_TYPE BMX_MANAGEMENT_HANDLER_INFO = MakeHandlerInfo(bmx::BMX_MANAGEMENT_HANDLER_ID, 1);

    // Maximum total number of registered handlers.
    const uint32_t MAX_TOTAL_NUMBER_OF_HANDLERS = 256;

    // Maximum number of the same handlers in a list.
    const uint32_t MAX_NUMBER_OF_HANDLERS_IN_LIST = 8;

    // Maximum number of URI callback parameters.
    const uint32_t MAX_URI_CALLBACK_PARAMS = 32;

    // Maximum URI string length.
    const uint32_t MAX_URI_STRING_LEN = 512;

    // BMX message types.
    const uint8_t BMX_REGISTER_PORT = 0;
    const uint8_t BMX_REGISTER_PORT_SUBPORT = 1;
    const uint8_t BMX_REGISTER_URI = 2;
    const uint8_t BMX_UNREGISTER = 3;
    const uint8_t BMX_ERROR = 4;
    const uint8_t BMX_REGISTER_PUSH_CHANNEL = 5;
    const uint8_t BMX_REGISTER_PUSH_CHANNEL_RESPONSE = 6;
    const uint8_t BMX_DEREGISTER_PUSH_CHANNEL = 7;
    const uint8_t BMX_SEND_ALL_HANDLERS = 8;
    const uint8_t BMX_SESSION_CREATED = 9;
    const uint8_t BMX_SESSION_DESTROYED = 10;
    const uint8_t BMX_PING = 254;
    const uint8_t BMX_PONG = 255;
    const uint8_t BMX_REGISTER_URI_NEW = 253;

    // Supported HTTP methods.
    enum HTTP_METHODS
    {
        GET_METHOD,
        POST_METHOD,
        PUT_METHOD,
        DELETE_METHOD,
        HEAD_METHOD,
        OPTIONS_METHOD,
        TRACE_METHOD,
        PATCH_METHOD,    
        OTHER_METHOD
    };

    // Entrance to process any BMX message.
    extern uint32_t OnIncomingBmxMessage(
        uint64_t session_id,
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
        URI_HANDLER
    };

    class BmxData;

    // Handler list.
    class HandlersList
    {
        // Type of the underlying handler.
        bmx::HANDLER_TYPE type_;

        // Assigned handler info.
        BMX_HANDLER_TYPE handler_info_;

        // Current number of handlers.
        uint8_t num_entries_;

        // User handler callbacks.
        GENERIC_HANDLER_CALLBACK handlers_[bmx::MAX_NUMBER_OF_HANDLERS_IN_LIST];

        // Port number.
        uint16_t port_;

        // Sub-port number.
        BMX_SUBPORT_TYPE subport_;

        // URI string.
        char uri_string_[bmx::MAX_URI_STRING_LEN];
        uint32_t uri_len_chars_;
        bmx::HTTP_METHODS http_method_;

        uint8_t num_params_;
        uint8_t param_types_[MAX_URI_CALLBACK_PARAMS];

    public:

        // Constructor.
        explicit HandlersList()
        {
            Unregister();
        }

        // Getting handler index.
        BMX_HANDLER_INDEX_TYPE get_handler_index()
        {
            return GetBmxHandlerIndex(handler_info_);
        }

        // Getting handler id.
        BMX_HANDLER_TYPE get_handler_info()
        {
            return handler_info_;
        }

        // Getting number of entries.
        uint8_t get_num_entries()
        {
            return num_entries_;
        }

        // Getting all handlers.
        GENERIC_HANDLER_CALLBACK* get_handlers()
        {
            return handlers_;
        }

        // Gets sub-port number.
        BMX_SUBPORT_TYPE get_subport()
        {
            return subport_;
        }

        // Gets port number.
        uint16_t get_port()
        {
            return port_;
        }

        // Gets URI.
        char* get_uri()
        {
            return uri_string_;
        }

        // Get URI length.
        uint32_t get_uri_len_chars()
        {
            return uri_len_chars_;
        }

        // Get HTTP method.
        bmx::HTTP_METHODS get_http_method()
        {
            return http_method_;
        }

        // Get number of URI callback parameters.
        int32_t get_num_params()
        {
            return num_params_;
        }

        // Gets handler type.
        bmx::HANDLER_TYPE get_type()
        {
            return type_;
        }

        // Find existing handler.
        bool HandlerAlreadyExists(GENERIC_HANDLER_CALLBACK handler_callback)
        {
            // Going through all registered handlers.
            for (uint8_t i = 0; i < num_entries_; ++i)
            {
                if (handler_callback == handlers_[i])
                    return true;
            }

            return false;
        }

        // Adds user handler.
        uint32_t AddUserHandler(GENERIC_HANDLER_CALLBACK handler_callback)
        {
            // Reached maximum amount of handlers.
            if (num_entries_ >= bmx::MAX_NUMBER_OF_HANDLERS_IN_LIST)
                return SCERRUNSPECIFIED; // SCERRMAXPORTHANDLERS

            // Checking if handler already exists.
            if (HandlerAlreadyExists(handler_callback))
                return SCERRUNSPECIFIED; // SCERRHANDLEREXISTS

            // Adding handler to array.
            handlers_[num_entries_] = handler_callback;
            num_entries_++;

            return 0;
        }

        // Init.
        uint32_t Init(
            bmx::HANDLER_TYPE type,
            BMX_HANDLER_TYPE handler_info,
            uint16_t port,
            BMX_SUBPORT_TYPE subport,
            char* uri_string,
            uint32_t uri_len_chars,
            bmx::HTTP_METHODS http_method,
            uint8_t* param_types,
            int32_t num_params)
        {
            num_entries_ = 0;

            type_ = type;
            port_ = port;

            subport_ = subport;
            handler_info_ = handler_info;

            http_method_ = http_method;
            uri_len_chars_ = uri_len_chars;

            num_params_ = num_params;
            if (num_params_ > 0)
                memcpy(param_types_, param_types, MAX_URI_CALLBACK_PARAMS);

            // Checking the type of handler.
            switch(type)
            {
                case bmx::HANDLER_TYPE::PORT_HANDLER:
                {
                    break;
                }

                case bmx::HANDLER_TYPE::SUBPORT_HANDLER:
                {
                    break;
                }

                case bmx::HANDLER_TYPE::URI_HANDLER:
                {
                    // Copying the URI string.
                    strncpy_s(uri_string_, bmx::MAX_URI_STRING_LEN, uri_string, uri_len_chars);

                    // Getting string length in characters.
                    uri_len_chars_ = uri_len_chars;

                    // Copying the HTTP method.
                    http_method_ = http_method;

                    break;
                }

                default:
                {
                    return SCERRUNSPECIFIED; // SCERRWRONGHANDLERTYPE
                }
            }

            return 0;
        }

        // Writes needed port handler data into chunk.
        uint32_t WriteRegisteredPortHandler(response_chunk_part *resp_chunk)
        {
            // Checking if message fits the chunk.
            if ((starcounter::core::chunk_size - resp_chunk->get_offset() - shared_memory_chunk::link_size) <=
                sizeof(BMX_REGISTER_PORT) + sizeof(handler_info_) + sizeof(port_))
            {
                return 0;
            }

            resp_chunk->write(BMX_REGISTER_PORT);
            resp_chunk->write(handler_info_);
            resp_chunk->write(port_);

            return resp_chunk->get_offset();
        }

        // Writes needed subport handler data into chunk.
        uint32_t WriteRegisteredSubPortHandler(response_chunk_part *resp_chunk)
        {
            // Checking if message fits the chunk.
            if ((starcounter::core::chunk_size - resp_chunk->get_offset() - shared_memory_chunk::link_size) <=
                sizeof(BMX_REGISTER_PORT_SUBPORT) + sizeof(handler_info_) + sizeof(port_) + sizeof(subport_))
            {
                return 0;
            }

            resp_chunk->write(BMX_REGISTER_PORT_SUBPORT);
            resp_chunk->write(handler_info_);
            resp_chunk->write(port_);
            resp_chunk->write(subport_);

            return resp_chunk->get_offset();
        }

        // Writes needed URI handler data into chunk.
        uint32_t WriteRegisteredUriHandler(response_chunk_part *resp_chunk)
        {
            // Checking if message fits the chunk.
            if ((starcounter::core::chunk_size - resp_chunk->get_offset() - shared_memory_chunk::link_size) <=
                sizeof(BMX_REGISTER_URI) + sizeof(handler_info_) + sizeof(port_) + uri_len_chars_ * sizeof(char) + 1)
            {
                return 0;
            }

            resp_chunk->write(BMX_REGISTER_URI);
            resp_chunk->write(handler_info_);
            resp_chunk->write(port_);
            resp_chunk->write_string(uri_string_, uri_len_chars_);
            resp_chunk->write((uint8_t)http_method_);

            return resp_chunk->get_offset();
        }

        // Writes needed URI handler data into chunk.
        uint32_t WriteRegisteredUriHandlerNew(response_chunk_part *resp_chunk)
        {
            // Checking if message fits the chunk.
            if ((starcounter::core::chunk_size - resp_chunk->get_offset() - shared_memory_chunk::link_size) <=
                sizeof(BMX_REGISTER_URI) + sizeof(handler_info_) + sizeof(port_) + uri_len_chars_ * sizeof(char) + 1)
            {
                return 0;
            }

            resp_chunk->write(BMX_REGISTER_URI_NEW);
            resp_chunk->write(handler_info_);
            resp_chunk->write(port_);
            resp_chunk->write_string(uri_string_, uri_len_chars_);
            resp_chunk->write((uint8_t)http_method_);
            resp_chunk->write(num_params_);
            resp_chunk->write_data_only(param_types_, MAX_URI_CALLBACK_PARAMS);

            return resp_chunk->get_offset();
        }

        // Pushes registered URI handler.
        uint32_t PushRegisteredUriHandler(BmxData* bmx_data);
        uint32_t PushRegisteredUriHandlerNew(BmxData* bmx_data);

        // Pushes registered port handler.
        uint32_t PushRegisteredPortHandler(BmxData* bmx_data);

        // Pushes registered subport handler.
        uint32_t PushRegisteredSubportHandler(BmxData* bmx_data);

        // Should be called when whole handlers list should be unregistered.
        uint32_t Unregister()
        {
            type_ = bmx::HANDLER_TYPE::UNUSED_HANDLER;
            handler_info_ = BMX_INVALID_HANDLER_INFO;
            num_entries_ = 0;

            return 0;
        }

        // Checking if slot is empty.
        bool IsEmpty()
        {
            return (bmx::HANDLER_TYPE::UNUSED_HANDLER == type_);
        }

        // Unregistering specific user handler.
        uint32_t Unregister(GENERIC_HANDLER_CALLBACK handler_callback)
        {
            // Comparing all handlers.
            for (uint8_t i = 0; i < num_entries_; ++i)
            {
                // Checking if handler is the same.
                if (handler_callback == handlers_[i])
                {
                    // Checking if it was not the last handler in the array.
                    if (i < (num_entries_ - 1))
                    {
                        // Shifting all forward handlers.
                        for (uint8_t k = i; k < (num_entries_ - 1); ++k)
                            handlers_[k] = handlers_[k + 1];
                    }
                    
                    // Number of entries decreased by one.
                    num_entries_--;

                    break;
                }
            }

            // Checking if it was the last handler.
            if (num_entries_ <= 0)
                return Unregister();

            return SCERRUNSPECIFIED; // SCERRHANDLERNOTFOUND
        }

        // Runs user handlers.
        uint32_t RunHandlers(
            uint64_t session_id,
            shared_memory_chunk* smc, 
            TASK_INFO_TYPE* task_info)
        {
            uint32_t err_code;
            bool is_handled = false;

            // Going through all registered handlers.
            for (uint8_t i = 0; i < num_entries_; ++i)
            {
                // Running the handler.
                err_code = handlers_[i](session_id, smc, task_info, &is_handled);

                // Checking if information was handled and no errors occurred.
                if (is_handled || err_code)
                    return err_code;
            }

            return SCERRUNSPECIFIED; // SCERRHANDLERNOTCALLED
        }
    };

    // Global BMX data, including handlers, memory, etc.
    class BmxData
    {
        // Current maximum number of handlers.
        uint32_t max_num_entries_;

        // All registered handlers.
        HandlersList* registered_handlers_;

        // Number of registered push channels.
        volatile uint32_t num_registered_push_channels_;

        // Current unique number.
        BMX_HANDLER_UNIQUE_NUM_TYPE unique_handler_num_;

    public:

        // Gets specific registered handler.
        HandlersList* GetRegisteredHandler(BMX_HANDLER_TYPE handler_id)
        {
            return registered_handlers_ + handler_id;
        }

        // Gets the number of registered push channels.
        uint32_t get_num_registered_push_channels()
        {
            return num_registered_push_channels_;
        }

        // Clones current BMX data.
        BmxData* Clone()
        {
            // Note: creating with one more entry.
            BmxData* new_copy = new BmxData(max_num_entries_ + 1);

            new_copy->max_num_entries_ = max_num_entries_;
            new_copy->num_registered_push_channels_ = num_registered_push_channels_;
            new_copy->unique_handler_num_ = unique_handler_num_;

            // Note: for non-linear HandlersList structure, need to copy element by element.
            memcpy(new_copy->registered_handlers_, registered_handlers_, sizeof(registered_handlers_[0]) * max_num_entries_);

            return new_copy;
        }

        // Checks if session has changed from current one.
        uint32_t CheckAndSwitchSession(TASK_INFO_TYPE* task_info, uint64_t session_id);

        // Pushes unregistered handler.
        uint32_t PushHandlerUnregistration(BMX_HANDLER_TYPE handler_info);
        uint32_t SendRegisterPushChannelResponse(shared_memory_chunk* smc, TASK_INFO_TYPE* task_info);
        uint32_t HandleDestroyedSession(request_chunk_part* request, TASK_INFO_TYPE* task_info);

        // Sends information about all registered handlers.
        uint32_t SendAllHandlersInfo(shared_memory_chunk* smc, TASK_INFO_TYPE* task_info);

        // Unregisters certain handler.
        uint32_t UnregisterHandler(BMX_HANDLER_INDEX_TYPE handler_index, bool* is_empty_handler);
        uint32_t UnregisterHandler(BMX_HANDLER_INDEX_TYPE handler_index, GENERIC_HANDLER_CALLBACK user_handler, bool* is_empty_handler);

        // Registers port handler.
        uint32_t RegisterPortHandler(
            uint16_t port_num,
            GENERIC_HANDLER_CALLBACK port_handler,
            BMX_HANDLER_TYPE* handler_id);

        // Registers sub-port handler.
        uint32_t RegisterSubPortHandler(
            uint16_t port,
            BMX_SUBPORT_TYPE subport,
            GENERIC_HANDLER_CALLBACK subport_handler,
            BMX_HANDLER_TYPE* handler_id);

        // Registers URI handler.
        uint32_t RegisterUriHandler(
            uint16_t port,
            char* uri_string,
            HTTP_METHODS http_method,
            uint8_t* param_types,
            int32_t num_params,
            GENERIC_HANDLER_CALLBACK uri_handler, 
            BMX_HANDLER_TYPE* handler_id);

        // Constructor.
        BmxData(uint32_t max_total_handlers)
        {
            registered_handlers_ = new HandlersList[max_total_handlers];
            max_num_entries_ = 0;
            unique_handler_num_ = 0;
            num_registered_push_channels_ = 0;
        }

        // Destructor.
        ~BmxData()
        {
            delete [] registered_handlers_;
            registered_handlers_ = NULL;
        }

        // Main message loop for incoming requests. Handles the 
        // dispatching of the message to the correct handler as 
        // well as sending any responses back.
        uint32_t HandleBmxChunk(CM2_TASK_DATA* task_data);
    };

    // Global BMX data.
    extern BmxData* g_bmx_data;

    // Managed callback to destroy Apps session.
    typedef void (*DestroyAppsSessionCallback)(
        uint64_t apps_session_index,
        uint64_t apps_session_salt,
        uint32_t scheduler_id);

    // Callback to destroy Apps inactive session.
    extern DestroyAppsSessionCallback g_destroy_apps_session_callback;

}  // namespace bmx
}; // namespace starcounter

// Initializes bmx manager.
EXTERN_C uint32_t sc_init_bmx_manager();

// Waits for BMX manager to be ready.
EXTERN_C void sc_wait_for_bmx_ready();

// Handles all incoming chunks.
EXTERN_C uint32_t sc_handle_incoming_chunks(CM2_TASK_DATA* task_data);

// Register port handler.
EXTERN_C uint32_t sc_bmx_register_port_handler(
    uint16_t port, 
    GENERIC_HANDLER_CALLBACK callback, 
    BMX_HANDLER_TYPE* handler_id
    );

// Register sub-port handler.
EXTERN_C uint32_t sc_bmx_register_subport_handler(
    uint16_t port,
    starcounter::bmx::BMX_SUBPORT_TYPE subport,
    GENERIC_HANDLER_CALLBACK callback, 
    BMX_HANDLER_TYPE* handler_id
    );

// Register URI handler.
EXTERN_C uint32_t sc_bmx_register_uri_handler(
    uint16_t port,
    char* uri_string,
    uint8_t http_method,
    GENERIC_HANDLER_CALLBACK callback, 
    BMX_HANDLER_TYPE* handler_id
    );

// Register URI handler.
EXTERN_C uint32_t sc_bmx_register_uri_handler_new(
    uint16_t port,
    char* uri_info,
    uint8_t http_method,
    uint8_t* param_types,
    uint8_t num_params,
    GENERIC_HANDLER_CALLBACK callback, 
    BMX_HANDLER_TYPE* handler_id
    );

// Construct BMX Ping message.
EXTERN_C uint32_t sc_bmx_construct_ping(
    uint64_t ping_data, 
    shared_memory_chunk* smc
    );

// Parse BMX Pong message.
EXTERN_C uint32_t sc_bmx_parse_pong(
    shared_memory_chunk* smc,
    uint64_t* pong_data
    );

#endif
