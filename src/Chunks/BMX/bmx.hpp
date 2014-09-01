//
// bmx.hpp
//
// 
//
// Copyright © 2006-2013 Starcounter AB. All rights reserved.
// Starcounter® is a registered trademark of Starcounter AB.
//

#ifndef BMX_HPP
#define BMX_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#define WIN32_LEAN_AND_MEAN
#include <winsock2.h>
#undef WIN32_LEAN_AND_MEAN

#include <cstdint>
#include <list>
#include <vector>
#include <cassert>
#include "../../Starcounter.Internal/Constants/MixedCodeConstants.cs"
#include "coalmine.h"

#define CODE_HOSTED
#include "profiler.hpp"

#include "sccoredbg.h"
#include "sccoredb.h"
#include "chunk_helper.h"
#include "..\common\chunk.hpp"

//#undef _SC_ASSERT
//#define _SC_ASSERT assert

// BMX task information.
struct TASK_INFO_TYPE
{
    BMX_HANDLER_TYPE handler_info;
    starcounter::core::chunk_index the_chunk_index;
    uint8_t flags;
    uint8_t scheduler_number;
    uint8_t client_worker_id;
};

// User handler callback.
typedef uint32_t (__stdcall *GENERIC_HANDLER_CALLBACK)(
    uint16_t managed_handler_id,
    shared_memory_chunk* smc, 
    TASK_INFO_TYPE* task_info,
    bool* is_handled
);

namespace starcounter
{
namespace bmx
{
    typedef uint32_t BMX_SUBPORT_TYPE;

    // Invalid BMX handler info.
    const BMX_HANDLER_TYPE BMX_INVALID_HANDLER_INFO = ~((BMX_HANDLER_TYPE) 0);

    // Invalid BMX handler index.
    const BMX_HANDLER_INDEX_TYPE BMX_INVALID_HANDLER_INDEX = ~((BMX_HANDLER_INDEX_TYPE) 0);

    // Predefined BMX management handler.
    const BMX_HANDLER_TYPE BMX_MANAGEMENT_HANDLER_INDEX = 0;

    inline BMX_HANDLER_TYPE MakeHandlerInfo(BMX_HANDLER_INDEX_TYPE handler_index, BMX_HANDLER_UNIQUE_NUM_TYPE unique_num)
    {
        return (((uint64_t)unique_num) << 16) | handler_index;
    }

    const BMX_HANDLER_TYPE BMX_MANAGEMENT_HANDLER_INFO = MakeHandlerInfo(bmx::BMX_MANAGEMENT_HANDLER_INDEX, 1);

    // Maximum total number of registered handlers.
    const uint32_t MAX_TOTAL_NUMBER_OF_HANDLERS = 256;

    // Maximum number of the same handlers in a list.
    const uint32_t MAX_NUMBER_OF_HANDLERS_IN_LIST = 8;

    // BMX message types.
    const uint8_t BMX_ERROR = 0;
    const uint8_t BMX_SESSION_DESTROY = 1;
    const uint8_t BMX_PING = 254;
    const uint8_t BMX_PONG = 255;

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
        uint16_t managed_handler_id,
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
        URI_HANDLER,
        WS_HANDLER
    };

    class BmxData;

    // Handler list.
    class HandlersList
    {
        // Type of the underlying handler.
        bmx::HANDLER_TYPE type_;

        // Assigned handler info.
        BMX_HANDLER_TYPE handler_info_;

        // Managed handler index.
        uint16_t managed_handler_index_;

        // Current number of handlers.
        uint8_t num_entries_;

        // User handler callbacks.
        GENERIC_HANDLER_CALLBACK handlers_[bmx::MAX_NUMBER_OF_HANDLERS_IN_LIST];

        // Port number.
        uint16_t port_;

        // Sub-port number.
        BMX_SUBPORT_TYPE subport_;

        // URI string.
        char* original_uri_info_;
        char* processed_uri_info_;
        char* app_name_;

        uint8_t num_params_;
        uint8_t param_types_[MixedCodeConstants::MAX_URI_CALLBACK_PARAMS];

        starcounter::MixedCodeConstants::NetworkProtocolType proto_type_;

        HandlersList(const HandlersList&);
        HandlersList& operator=(const HandlersList&);

    public:

        // Constructor.
        HandlersList()
        {
            original_uri_info_ = NULL;
            processed_uri_info_ = NULL;
            app_name_ = NULL;

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
        char* get_original_uri_info()
        {
            return original_uri_info_;
        }

        char* get_app_name()
        {
            return app_name_;
        }

        // Gets URI.
        char* get_processed_uri_info()
        {
            return processed_uri_info_;
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
                return SCERRMAXHANDLERSREACHED;

            // Checking if handler already exists.
            if (HandlerAlreadyExists(handler_callback))
                return SCERRHANDLERALREADYREGISTERED;

            // Adding handler to array.
            handlers_[num_entries_] = handler_callback;
            num_entries_++;

            return 0;
        }

        // Init.
        uint32_t Init(
            const bmx::HANDLER_TYPE type,
            const BMX_HANDLER_TYPE handler_info,
            const uint16_t managed_handler_index,
            const uint16_t port,
            const char* app_name,
            const BMX_SUBPORT_TYPE subport,
            const char* original_uri_info,
            const char* processed_uri_info,
            const uint8_t* param_types,
            const int32_t num_params,
            const starcounter::MixedCodeConstants::NetworkProtocolType proto_type)
        {
            num_entries_ = 0;

            type_ = type;
            port_ = port;

            subport_ = subport;
            managed_handler_index_ = managed_handler_index;

            // Deleting previous allocations if any.
            if (original_uri_info_)
            {
                delete original_uri_info_;
                original_uri_info_ = NULL;
            }

            if (processed_uri_info_)
            {
                delete processed_uri_info_;
                processed_uri_info_ = NULL;
            }

            if (app_name_)
            {
                delete app_name_;
                app_name_ = NULL;
            }

            _SC_ASSERT(app_name != NULL);
            uint32_t len = (uint32_t) strlen(app_name);
            app_name_ = new char[len + 1];
            strncpy_s(app_name_, len + 1, app_name, len);

            num_params_ = num_params;
            if (num_params_ > 0)
                memcpy(param_types_, param_types, MixedCodeConstants::MAX_URI_CALLBACK_PARAMS);

            proto_type_ = proto_type;

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
                    _SC_ASSERT (original_uri_info != NULL);
                    len = (uint32_t) strlen(original_uri_info);
                    original_uri_info_ = new char[len + 1];
                    strncpy_s(original_uri_info_, len + 1, original_uri_info, len);

                    _SC_ASSERT (processed_uri_info != NULL);
                    len = (uint32_t) strlen(processed_uri_info);
                    processed_uri_info_ = new char[len + 1];
                    strncpy_s(processed_uri_info_, len + 1, processed_uri_info, len);

                    break;
                }

                case bmx::HANDLER_TYPE::WS_HANDLER:
                {
                    // Copying the WS channel string.
                    _SC_ASSERT(original_uri_info != NULL);

                    len = (uint32_t) strlen(original_uri_info);
                    original_uri_info_ = new char[len + 1];
                    strncpy_s(original_uri_info_, len + 1, original_uri_info, len);

                    break;
                }

                default:
                {
                    _SC_ASSERT(false);
                }
            }

            handler_info_ = handler_info;

            return 0;
        }

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

            return SCERRHANDLERNOTFOUND;
        }

        // Runs user handlers.
        uint32_t RunHandlers(
            shared_memory_chunk* smc, 
            TASK_INFO_TYPE* task_info)
        {
            uint32_t err_code;
            bool is_handled = false;

            // Going through all registered handlers.
            for (uint8_t i = 0; i < num_entries_; ++i)
            {
                // Running the handler.
                err_code = handlers_[i](managed_handler_index_, smc, task_info, &is_handled);

                // Checking if information was handled and no errors occurred.
                if (is_handled || err_code)
                    return err_code;
            }

            return 0;
        }
    };

    // Global BMX data, including handlers, memory, etc.
    class BmxData
    {
        // Current maximum number of handlers.
        int32_t max_num_entries_;

        // All registered handlers.
        HandlersList* registered_handlers_;

        // Current unique number.
        BMX_HANDLER_UNIQUE_NUM_TYPE unique_handler_num_;

    public:

        // Gets specific registered handler.
        HandlersList* GetRegisteredHandlerByIndex(BMX_HANDLER_INDEX_TYPE handler_index)
        {
            return registered_handlers_ + handler_index;
        }

        int32_t get_max_num_entries()
        {
            return max_num_entries_;
        }

        // Number of remaining push channels.
        int32_t get_num_remaining_push_channels();

        // Clones current BMX data.
        BmxData* Clone()
        {
            // Note: creating with one more entry.
            BmxData* new_copy = new BmxData(max_num_entries_ + 1);

            new_copy->max_num_entries_ = max_num_entries_;
            new_copy->unique_handler_num_ = unique_handler_num_;

            // Note: for non-linear HandlersList structure, need to copy element by element.
            memcpy(new_copy->registered_handlers_, registered_handlers_, sizeof(registered_handlers_[0]) * max_num_entries_);

            return new_copy;
        }

        // Pushes unregistered handler.
        uint32_t HandleSessionDestruction(request_chunk_part* request, TASK_INFO_TYPE* task_info);
        uint32_t HandleErrorFromGateway(request_chunk_part* request, TASK_INFO_TYPE* task_info);

        // Unregisters certain handler.
        uint32_t UnregisterHandler(BMX_HANDLER_INDEX_TYPE handler_index, bool* is_empty_handler);
        uint32_t UnregisterHandler(BMX_HANDLER_INDEX_TYPE handler_index, GENERIC_HANDLER_CALLBACK user_handler, bool* is_empty_handler);

        uint32_t FindUriHandler(
            uint16_t port_num,
            const char* processed_uri_info,
            BMX_HANDLER_INDEX_TYPE* handler_index);

        uint32_t FindWsHandler(
            uint16_t port_num,
            const char* channel_name,
            BMX_HANDLER_INDEX_TYPE* handler_index);

        uint32_t FindPortHandler(
            uint16_t port_num,
            BMX_HANDLER_INDEX_TYPE* handler_index);

        // Registers port handler.
        uint32_t RegisterPortHandler(
            const uint16_t port_num,
            const char* app_name,
            const GENERIC_HANDLER_CALLBACK port_handler,
            const uint16_t managed_handler_index,
            BMX_HANDLER_TYPE* phandler_info);

        // Registers URI handler.
        uint32_t RegisterUriHandler(
            const uint16_t port,
            const char* app_name,
            const char* original_uri_info,
            const char* processed_uri_info,
            const uint8_t* param_types,
            const int32_t num_params,
            const GENERIC_HANDLER_CALLBACK uri_handler, 
            const uint16_t managed_handler_index,
            BMX_HANDLER_TYPE* phandler_info);

        // Registers WebSocket handler.
        uint32_t RegisterWsHandler(
            const uint16_t port,
            const char* app_name,
            const char* channel_name,
            const uint32_t channel_id,
            const GENERIC_HANDLER_CALLBACK ws_handler, 
            const uint16_t managed_handler_index,
            BMX_HANDLER_TYPE* phandler_info);

        // Finds certain handler.
        bool IsHandlerExist(BMX_HANDLER_INDEX_TYPE handler_index);

        // Constructor.
        BmxData(uint32_t max_total_handlers)
        {
            registered_handlers_ = new HandlersList[max_total_handlers]();
            max_num_entries_ = 0;

            unique_handler_num_ = 1;
        }

        // Generates new unique id.
        void GenerateNewId() {

            LARGE_INTEGER t;
            QueryPerformanceCounter(&t);
            unique_handler_num_ = t.LowPart;
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
        uint8_t scheduler_id,
        uint32_t linear_index,
        uint64_t random_salt);

    extern DestroyAppsSessionCallback g_destroy_apps_session_callback;

    // Managed callback to create a new Apps session.
    typedef void (*CreateNewAppsSessionCallback)(
        uint8_t scheduler_id,
        uint32_t* linear_index,
        uint64_t* random_salt,
        uint32_t* reserved);

    extern CreateNewAppsSessionCallback g_create_new_apps_session_callback;

    // Managed callback to handle errors from gateway.
    typedef void (*ErrorHandlingCallback)(
        uint32_t err_code,
        wchar_t* err_string,
        int32_t err_string_len);

    extern ErrorHandlingCallback g_error_handling_callback;

}  // namespace bmx
}; // namespace starcounter

#if 0
// Waits for BMX manager to be ready.
EXTERN_C int32_t __stdcall sc_wait_for_bmx_ready(uint32_t max_time_to_wait_ms);
#endif

// Handles all incoming chunks.
EXTERN_C uint32_t __stdcall sc_handle_incoming_chunks(CM2_TASK_DATA* task_data);

// Construct BMX Ping message.
EXTERN_C uint32_t __stdcall sc_bmx_construct_ping(
    uint64_t ping_data, 
    shared_memory_chunk* smc
    );

// Parse BMX Pong message.
EXTERN_C uint32_t __stdcall sc_bmx_parse_pong(
    shared_memory_chunk* smc,
    uint64_t* pong_data
    );

#endif
