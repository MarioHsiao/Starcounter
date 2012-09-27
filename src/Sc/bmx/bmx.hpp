//
// bmx.hpp
//
// 
//
// Copyright © 2006-2012 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#ifndef BMX_HPP
#define BMX_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#include <stdint.h>
#include <list>
#include <vector>
#include "coalmine.h"
#include "chunk_helper.h"
#include "common\chunk.hpp"
#include "sccorensm.h"

#define LINKED_CHUNK 0x01
#define SCHEDULER_SPIN_COUNT 1000000

// BMX task information.
struct TASK_INFO_TYPE {
    uint8_t flags;
    uint8_t scheduler_number;
    BMX_HANDLER_TYPE handler_id;
    uint8_t fill1;
    uint32_t chunk_index;
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

// Register port handler.
EXTERN_C uint32_t sc_bmx_register_port_handler(
    uint16_t port, 
    GENERIC_HANDLER_CALLBACK callback, 
    BMX_HANDLER_TYPE* handler_id
);

// Register sub-port handler.
EXTERN_C uint32_t sc_bmx_register_subport_handler(
    uint16_t port,
    uint32_t subport,
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

namespace starcounter
{
namespace bmx
{
    // Predefined BMX management handler.
    const BMX_HANDLER_TYPE BMX_MANAGEMENT_HANDLER = 0;

    // Maximum total number of registered handlers.
    const uint32_t MAX_TOTAL_NUMBER_OF_HANDLERS = 256;

    // Maximum number of the same handlers in a list.
    const uint32_t MAX_NUMBER_OF_HANDLERS_IN_LIST = 8;

    // Maximum URI string length.
    const uint32_t MAX_URI_STRING_LEN = 512;

    // BMX message types.
    const uint8_t BMX_REGISTER_PORT = 0;
    const uint8_t BMX_REGISTER_PORT_SUBPORT = 1;
    const uint8_t BMX_REGISTER_URI = 2;
    const uint8_t BMX_UNREGISTER = 3;
    const uint8_t BMX_ERROR = 4;
    const uint8_t BMX_REGISTER_PUSH_CHANNEL = 5;
    const uint8_t BMX_DEREGISTER_PUSH_CHANNEL = 6;
    const uint8_t BMX_SEND_ALL_HANDLERS = 7;

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

    // Handler list.
    class HandlersList
    {
        // Type of the underlying handler.
        bmx::HANDLER_TYPE type_;

        // Assigned handler id.
        BMX_HANDLER_TYPE handler_id_;

        // Current number of handlers.
        uint8_t num_entries_;

        // User handler callbacks.
        GENERIC_HANDLER_CALLBACK handlers_[bmx::MAX_NUMBER_OF_HANDLERS_IN_LIST];

        // Port number.
        uint16_t port_;

        // Sub-port number.
        uint32_t subport_;

        // URI string.
        char uri_string_[bmx::MAX_URI_STRING_LEN];
        uint32_t uri_len_chars_;
        bmx::HTTP_METHODS http_method_;

    public:

        // Constructor.
        explicit HandlersList()
        {
            Unregister();
        }

        // Getting handler id.
        BMX_HANDLER_TYPE get_handler_id()
        {
            return handler_id_;
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
        uint32_t get_subport()
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
            BMX_HANDLER_TYPE handler_id,
            uint16_t port,
            uint32_t subport,
            char* uri_string,
            uint32_t uri_len_chars,
            bmx::HTTP_METHODS http_method)
        {
            num_entries_ = 0;

            type_ = type;
            port_ = port;

            subport_ = subport;
            handler_id_ = handler_id;

            http_method_ = http_method;
            uri_len_chars_ = uri_len_chars;

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

        // Should be called when whole handlers list should be unregistered.
        uint32_t Unregister()
        {
            type_ = bmx::HANDLER_TYPE::UNUSED_HANDLER;
            handler_id_ = -1;
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
        // Channel index for pushing chunk.
        uint32_t channel_index_for_push_;

        // Current maximum number of handlers.
        BMX_HANDLER_TYPE max_num_entries_;

        // All registered handlers.
        HandlersList* registered_handlers_;

    public:

        // Clones current BMX data.
        BmxData* Clone()
        {
            // Note: creating with one more entry.
            BmxData* new_copy = new BmxData(max_num_entries_ + 1);

            new_copy->channel_index_for_push_ = channel_index_for_push_;
            new_copy->max_num_entries_ = max_num_entries_;

            // Note: for non-linear HandlersList structure, need to copy element by element.
            memcpy(new_copy->registered_handlers_, registered_handlers_, sizeof(registered_handlers_[0]) * max_num_entries_);

            return new_copy;
        }

        // Checks if session has changed from current one.
        uint32_t CheckAndSwitchSession(TASK_INFO_TYPE* task_info, uint64_t session_id);

        uint32_t AcquireNewChunk(shared_memory_chunk*& chunk, uint32_t& chunk_index);

        uint32_t WriteRegisteredPortHandler(
            shared_memory_chunk* smc,
            BMX_HANDLER_TYPE handler_id,
            uint16_t port);

        uint32_t WriteRegisteredSubPortHandler(
            shared_memory_chunk* smc,
            BMX_HANDLER_TYPE handler_id,
            uint16_t port,
            uint32_t subport);

        uint32_t WriteRegisteredUriHandler(
            shared_memory_chunk* smc,
            BMX_HANDLER_TYPE handler_id,
            uint16_t port,
            char* uri_string,
            uint32_t uri_len_bytes,
            HTTP_METHODS http_method);

        uint32_t PushHandlerUnregistration(BMX_HANDLER_TYPE handler_id);
        uint32_t PushRegisteredPortHandler(BMX_HANDLER_TYPE handler_id, uint16_t port_num);
        uint32_t PushRegisteredSubportHandler(BMX_HANDLER_TYPE handler_id, uint16_t port, uint32_t subport);
        uint32_t PushRegisteredUriHandler(BMX_HANDLER_TYPE handler_id, uint16_t port, char* uri, uint32_t uri_len_chars, HTTP_METHODS http_method);

        // Sends information about all registered handlers.
        uint32_t SendAllHandlersInfo(shared_memory_chunk* smc, TASK_INFO_TYPE* task_info);

        // Unregisters certain handler.
        uint32_t UnregisterHandler(BMX_HANDLER_TYPE handler_id);
        uint32_t UnregisterHandler(BMX_HANDLER_TYPE handler_id, GENERIC_HANDLER_CALLBACK user_handler);

        // Registers port handler.
        uint32_t RegisterPortHandler(
            uint16_t port_num,
            GENERIC_HANDLER_CALLBACK port_handler,
            BMX_HANDLER_TYPE* handler_id);

        // Registers sub-port handler.
        uint32_t RegisterSubPortHandler(
            uint16_t port,
            uint32_t subport,
            GENERIC_HANDLER_CALLBACK subport_handler,
            BMX_HANDLER_TYPE* handler_id);

        // Registers URI handler.
        uint32_t RegisterUriHandler(
            uint16_t port,
            char* uri_string,
            HTTP_METHODS http_method,
            GENERIC_HANDLER_CALLBACK uri_handler, 
            BMX_HANDLER_TYPE* handler_id);

        // Getting channel index for push.
        uint32_t get_channel_index_for_push()
        {
            return channel_index_for_push_;
        }

        // Constructor.
        BmxData(uint32_t max_total_handlers)
        {
            registered_handlers_ = new HandlersList[max_total_handlers];
            channel_index_for_push_ = ~0;
            max_num_entries_ = 0;
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

}  // namespace bmx
}; // namespace starcounter

#endif
