#pragma once
#ifndef HANDLERS_HPP
#define HANDLERS_HPP

namespace starcounter {
namespace network {

// Handler list.
class HandlersList
{
    // Type of the underlying handler.
    bmx::HANDLER_TYPE type_;

    // Assigned handler info.
    BMX_HANDLER_TYPE handler_info_;

    // Database index (from which database this handler was registered).
    db_index_type db_index_;

    // Unique handler number.
    uint64_t unique_number_;

    // Handler function pointer.
    GENERIC_HANDLER_CALLBACK handler_;

    // Port number.
    uint16_t port_;

    // Sub-port number.
    bmx::BMX_SUBPORT_TYPE subport_;

    // URI string.
    char* method_space_uri_;

    char* app_name_;

    uint8_t param_types_[MixedCodeConstants::MAX_URI_CALLBACK_PARAMS];
    uint8_t num_params_;

    HandlersList(const HandlersList&);
    HandlersList& operator=(const HandlersList&);

    // Proxy information.
    ReverseProxyInfo* reverse_proxy_info_;

public:

    ~HandlersList() {
        Erase();
    }

    void Erase() {

        // Deleting previous allocations if any.
        if (method_space_uri_)
        {
            GwDeleteArray(method_space_uri_);
            method_space_uri_ = NULL;
        }

        if (app_name_)
        {
            GwDeleteArray(app_name_);
            app_name_ = NULL;
        }
    }

    bool ContainsHandler(GENERIC_HANDLER_CALLBACK handler)
    {
        return handler == handler_;
    }

    bool RemoveHandler(GENERIC_HANDLER_CALLBACK handler)
    {
		if (ContainsHandler(handler)) {
			handler_ = NULL;
			return true;
		}
        
		return false;
    }

    // Getting handler index.
    BMX_HANDLER_INDEX_TYPE get_handler_index()
    {
        return GetBmxHandlerIndex(handler_info_);
    }

    // Getting database index.
    db_index_type get_db_index()
    {
        return db_index_;
    }

    // Constructor.
    HandlersList()
    {
        Unregister();

        method_space_uri_ = NULL;

        app_name_ = NULL;
    }

    ReverseProxyInfo* get_reverse_proxy_info()
    {
        return reverse_proxy_info_;
    }

    // Getting params.
    uint8_t* get_param_types()
    {
        return param_types_;
    }

    // Getting number of parameters.
    uint8_t get_num_params()
    {
        return num_params_;
    }

    // Getting handler info.
    BMX_HANDLER_TYPE get_handler_info()
    {
        return handler_info_;
    }
	
    // Gets sub-port number.
    bmx::BMX_SUBPORT_TYPE get_subport()
    {
        return subport_;
    }

    // Gets port number.
    uint16_t get_port()
    {
        return port_;
    }

    char* get_app_name()
    {
        return app_name_;
    }

    // Gets URI.
    char* get_method_space_uri()
    {
        return method_space_uri_;
    }

    // Gets handler type.
    bmx::HANDLER_TYPE get_type()
    {
        return type_;
    }

    // Adds handler.
    uint32_t AddHandler(GENERIC_HANDLER_CALLBACK handler)
    {
		handler_ = handler;

        return 0;
    }

    // Init.
    uint32_t Init(
        const bmx::HANDLER_TYPE type,
        const BMX_HANDLER_TYPE handler_info,
        const uint16_t port,
        const char* app_name,
        const bmx::BMX_SUBPORT_TYPE subport,
        const char* method_space_uri,
        const uint8_t* param_types,
        const int32_t num_params,
        const db_index_type db_index,
        const uint64_t unique_number,
        ReverseProxyInfo* reverse_proxy_info)
    {
        type_ = type;
        port_ = port;

        subport_ = subport;
        handler_info_ = handler_info;

        db_index_ = db_index;
        unique_number_ = unique_number;

        reverse_proxy_info_ = reverse_proxy_info;

        // Deleting previous allocations if any.
        Erase();

        GW_ASSERT(app_name != NULL);
        uint32_t len = (uint32_t) strlen(app_name);
        app_name_ = GwNewArray(char, len + 1);
        strncpy_s(app_name_, len + 1, app_name, len);

        num_params_ = num_params;
        if (num_params_ > 0)
            memcpy(param_types_, param_types, MixedCodeConstants::MAX_URI_CALLBACK_PARAMS);

        // Checking the type of handler.
        switch(type_)
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
                GW_ASSERT(method_space_uri != NULL);

                len = (uint32_t) strlen(method_space_uri);
                method_space_uri_ = GwNewArray(char, len + 1);
                strncpy_s(method_space_uri_, len + 1, method_space_uri, len);
                
                break;
            }

            case bmx::HANDLER_TYPE::WS_HANDLER:
            {
                // Copying the WS channel string.
                GW_ASSERT(method_space_uri != NULL);

                len = (uint32_t) strlen(method_space_uri);
                method_space_uri_ = GwNewArray(char, len + 1);
                strncpy_s(method_space_uri_, len + 1, method_space_uri, len);

                break;
            }

            default:
            {
                GW_ASSERT(false);
            }
        }

        return 0;
    }

    // Should be called when whole handlers list should be unregistered.
    uint32_t Unregister()
    {
        unique_number_ = 0;
        type_ = bmx::HANDLER_TYPE::UNUSED_HANDLER;
        handler_info_ = bmx::BMX_INVALID_HANDLER_INFO;
        db_index_ = INVALID_DB_INDEX;
        reverse_proxy_info_ = NULL;

        return 0;
    }

    // Checking if slot is empty.
    bool IsEmpty()
    {
        return (type_ == bmx::HANDLER_TYPE::UNUSED_HANDLER);
    }

    // Unregistering specific handler.
    uint32_t Unregister(GENERIC_HANDLER_CALLBACK handler)
    {
        // Removing handler.
        if (RemoveHandler(handler)) {
            return Unregister();
        }

        return SCERRHANDLERNOTFOUND;
    }

    // Runs port handlers.
    uint32_t RunHandlers(GatewayWorker *gw, SocketDataChunkRef sd)
    {
        return handler_(this, gw, sd, handler_info_);
    }

    // Should be called when whole handlers list should be unregistered.
    uint32_t UnregisterGlobally(db_index_type db_index);
};

} // namespace network
} // namespace starcounter

#endif // HANDLERS_HPP