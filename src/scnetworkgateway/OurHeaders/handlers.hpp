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

    // Handler callbacks.
    LinearList<GENERIC_HANDLER_CALLBACK, bmx::MAX_NUMBER_OF_HANDLERS_IN_LIST> handlers_;

    // Port number.
    uint16_t port_;

    // Sub-port number.
    bmx::BMX_SUBPORT_TYPE subport_;

    // URI string.
    char* original_uri_info_;
    char* processed_uri_info_;

    int32_t processed_uri_info_len_;

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
        if (original_uri_info_)
        {
            GwDeleteArray(original_uri_info_);
            original_uri_info_ = NULL;
        }

        if (processed_uri_info_)
        {
            GwDeleteArray(processed_uri_info_);
            processed_uri_info_ = NULL;
        }

        if (app_name_)
        {
            GwDeleteArray(app_name_);
            app_name_ = NULL;
        }
    }

    bool ContainsHandler(GENERIC_HANDLER_CALLBACK handler)
    {
        return handlers_.Find(handler);
    }

    bool RemoveHandler(GENERIC_HANDLER_CALLBACK handler)
    {
        return handlers_.RemoveEntry(handler);
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

        original_uri_info_ = NULL;
        processed_uri_info_ = NULL;
        processed_uri_info_len_ = 0;
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

    // Getting number of entries.
    uint8_t get_num_entries()
    {
        return handlers_.get_num_entries();
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
    char* get_original_uri_info()
    {
        return original_uri_info_;
    }

    // Gets URI.
    char* get_processed_uri_info()
    {
        return processed_uri_info_;
    }

    int32_t get_processed_uri_info_len()
    {
        return processed_uri_info_len_;
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
        for (uint8_t i = 0; i < handlers_.get_num_entries(); ++i)
        {
            if (handler_callback == handlers_[i])
                return true;
        }

        return false;
    }

    // Adds handler.
    uint32_t AddHandler(GENERIC_HANDLER_CALLBACK handler_callback)
    {
        // Reached maximum amount of handlers.
        if (handlers_.get_num_entries() >= bmx::MAX_NUMBER_OF_HANDLERS_IN_LIST)
            return SCERRMAXHANDLERSREACHED;

        // Checking if handler already exists.
        if (HandlerAlreadyExists(handler_callback))
            return SCERRHANDLERALREADYREGISTERED;

        // Adding handler to array.
        handlers_.Add(handler_callback);

        return 0;
    }

    // Init.
    uint32_t Init(
        const bmx::HANDLER_TYPE type,
        const BMX_HANDLER_TYPE handler_info,
        const uint16_t port,
        const char* app_name,
        const bmx::BMX_SUBPORT_TYPE subport,
        const char* original_uri_info,
        const char* processed_uri_info,
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
                GW_ASSERT(original_uri_info != NULL);

                len = (uint32_t) strlen(original_uri_info);
                original_uri_info_ = GwNewArray(char, len + 1);
                strncpy_s(original_uri_info_, len + 1, original_uri_info, len);
                
                GW_ASSERT(processed_uri_info != NULL);

                len = (uint32_t) strlen(processed_uri_info);
                processed_uri_info_ = GwNewArray(char, len + 1);
                strncpy_s(processed_uri_info_, len + 1, processed_uri_info, len);

                processed_uri_info_len_ = len;

                break;
            }

            case bmx::HANDLER_TYPE::WS_HANDLER:
            {
                // Copying the WS channel string.
                GW_ASSERT(original_uri_info != NULL);

                len = (uint32_t) strlen(original_uri_info);
                original_uri_info_ = GwNewArray(char, len + 1);
                strncpy_s(original_uri_info_, len + 1, original_uri_info, len);

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
    uint32_t Unregister(GENERIC_HANDLER_CALLBACK handler_callback)
    {
        // Removing handler.
        if (handlers_.RemoveEntry(handler_callback))
        {
            // Checking if it was the last handler.
            if (handlers_.IsEmpty())
                return Unregister();
        }

        return SCERRHANDLERNOTFOUND;
    }

    // Runs port handlers.
    uint32_t RunHandlers(GatewayWorker *gw, SocketDataChunkRef sd, bool* is_handled)
    {
        uint32_t err_code;

        // Going through all registered handlers.
        for (int32_t i = 0; i < handlers_.get_num_entries(); i++)
        {
            // Running the handler.
            err_code = handlers_[i](this, gw, sd, handler_info_, is_handled);

            // Checking if information was handled and no errors occurred.
            if (*is_handled || err_code)
                return err_code;
        }

        return SCERRGWPORTNOTHANDLED;
    }

    // Should be called when whole handlers list should be unregistered.
    uint32_t UnregisterGlobally(db_index_type db_index);
};

class PortHandlers
{
    // Unique handler lists.
    LinearList<HandlersList*, bmx::MAX_NUMBER_OF_HANDLERS_IN_LIST> handler_lists_;

public:

    // Initializing the entry.
    void Add(db_index_type db_index, HandlersList* handlers_list)
    {
        // Adding only if it does not exist.
        if (!handler_lists_.Find(handlers_list))
            handler_lists_.Add(handlers_list);
    }

    // Printing the registered URIs.
    void PrintRegisteredHandlers(std::stringstream& global_port_statistics_stream)
    {
        global_port_statistics_stream << "Port has following handlers registered: ";
        for (int32_t i = 0; i < handler_lists_.get_num_entries(); i++)
        {
            global_port_statistics_stream << handler_lists_[i]->get_db_index() << ", ";
        }
        global_port_statistics_stream << "<br>";
    }

    // Constructor.
    PortHandlers()
    {
        Reset();
    }

    // Checking if handlers list is empty.
    bool IsEmpty()
    {
        return handler_lists_.IsEmpty();
    }

    // Has certain handler.
    bool HasHandler(GENERIC_HANDLER_CALLBACK handler)
    {
        // Going through all handler lists.
        for (int32_t i = 0; i < handler_lists_.get_num_entries(); ++i) {

            if (!handler_lists_[i]->IsEmpty()) {

                if (handler_lists_[i]->HandlerAlreadyExists(handler)) {
                    return true;
                }
            }
        }

        return false;
    }

    // Removes certain entry.
    bool RemoveEntry(GENERIC_HANDLER_CALLBACK handler)
    {
        bool removed = false;

        // Going through all handler lists.
        for (int32_t i = 0; i < handler_lists_.get_num_entries(); ++i)
        {
            if (handler_lists_[i]->RemoveHandler(handler))
            {
                if (handler_lists_[i]->IsEmpty())
                {
                    // Deleting the entry.
                    GwDeleteSingle(handler_lists_[i]);
                    handler_lists_[i] = NULL;

                    handler_lists_.RemoveByIndex(i);
                    i--;
                }

                removed = true;

                // Not stopping, going through all entries.
            }
        }

        return removed;
    }

    // Removes certain entry.
    bool RemoveEntry(db_index_type db_index)
    {
        bool removed = false;

        // Going through all handler lists.
        for (int32_t i = 0; i < handler_lists_.get_num_entries(); ++i)
        {
            if (db_index == handler_lists_[i]->get_db_index())
            {
                // Deleting the entry.
                GwDeleteSingle(handler_lists_[i]);
                handler_lists_[i] = NULL;

                // Checking if there are no databases left.
                handler_lists_.RemoveByIndex(i);
                --i;

                removed = true;

                // Not stopping, going through all entries.
            }
        }

        return removed;
    }

    // Resetting entry.
    void Reset()
    {
        for (int32_t i = 0; i < handler_lists_.get_num_entries(); ++i) {
            // Deleting the entry.
            GwDeleteSingle(handler_lists_[i]);
            handler_lists_[i] = NULL;
        }

        // Removing all handlers lists.
        handler_lists_.Clear();
    }

    // Running all registered handlers.
    uint32_t RunHandlers(GatewayWorker *gw, SocketDataChunkRef sd, bool* is_handled);
};

} // namespace network
} // namespace starcounter

#endif // HANDLERS_HPP