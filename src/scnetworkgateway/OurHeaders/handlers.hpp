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
        if (original_uri_info_)
        {
            delete [] original_uri_info_;
            original_uri_info_ = NULL;
        }

        if (processed_uri_info_)
        {
            delete [] processed_uri_info_;
            processed_uri_info_ = NULL;
        }

        if (app_name_)
        {
            delete [] app_name_;
            app_name_ = NULL;
        }

        GW_ASSERT(app_name != NULL);
        uint32_t len = (uint32_t) strlen(app_name);
        app_name_ = new char[len + 1];
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
                original_uri_info_ = new char[len + 1];
                strncpy_s(original_uri_info_, len + 1, original_uri_info, len);
                
                GW_ASSERT(processed_uri_info != NULL);

                len = (uint32_t) strlen(processed_uri_info);
                processed_uri_info_ = new char[len + 1];
                strncpy_s(processed_uri_info_, len + 1, processed_uri_info, len);

                processed_uri_info_len_ = len;

                break;
            }

            case bmx::HANDLER_TYPE::WS_HANDLER:
            {
                // Copying the WS channel string.
                GW_ASSERT(original_uri_info != NULL);

                len = (uint32_t) strlen(original_uri_info);
                original_uri_info_ = new char[len + 1];
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

// All handlers belonging to database.
class HandlersTable
{
    // Total registered handlers.
    BMX_HANDLER_INDEX_TYPE max_num_entries_;

    // All registered handlers.
    HandlersList registered_handlers_[bmx::MAX_TOTAL_NUMBER_OF_HANDLERS];

public:

    // Find port handler id.
    BMX_HANDLER_INDEX_TYPE FindPortHandlerIndex(uint16_t port_num)
    {
        for (BMX_HANDLER_INDEX_TYPE i = 0; i < max_num_entries_; i++)
        {
            if (!registered_handlers_[i].IsEmpty())
            {
                if (bmx::PORT_HANDLER == registered_handlers_[i].get_type())
                {
                    if (port_num == registered_handlers_[i].get_port())
                    {
                        return i;
                    }
                }
            }
        }

        return bmx::BMX_INVALID_HANDLER_INDEX;
    }

    // Find subport handler id.
    BMX_HANDLER_INDEX_TYPE FindSubPortHandlerIndex(uint16_t port_num, bmx::BMX_SUBPORT_TYPE subport_num)
    {
        for (BMX_HANDLER_INDEX_TYPE i = 0; i < max_num_entries_; i++)
        {
            if (!registered_handlers_[i].IsEmpty())
            {
                if (bmx::SUBPORT_HANDLER == registered_handlers_[i].get_type())
                {
                    if (port_num == registered_handlers_[i].get_port())
                    {
                        if (subport_num == registered_handlers_[i].get_subport())
                        {
                            return i;
                        }
                    }
                }
            }
        }

        return bmx::BMX_INVALID_HANDLER_INDEX;
    }

    // Find URI handler id.
    BMX_HANDLER_INDEX_TYPE FindUriHandlerIndex(uint16_t port_num, const char* processed_uri_info)
    {
        int32_t longest_matched_uri = 0;

        for (BMX_HANDLER_INDEX_TYPE i = 0; i < max_num_entries_; i++)
        {
            if (!registered_handlers_[i].IsEmpty())
            {
                if (bmx::HANDLER_TYPE::URI_HANDLER == registered_handlers_[i].get_type())
                {
                    if (port_num == registered_handlers_[i].get_port())
                    {
                        // Comparing URI as starts with.
                        if (!strncmp(registered_handlers_[i].get_processed_uri_info(), processed_uri_info, strlen(processed_uri_info)))
                        {
                            return i;
                        }
                    }
                }
            }
        }

        return bmx::BMX_INVALID_HANDLER_INDEX;
    }

    // Gets specific handler.
    HandlersList* get_handler_list(BMX_HANDLER_TYPE handler_index)
    {
        return registered_handlers_ + handler_index;
    }

    // Finds specific handler.
    HandlersList* FindHandler(BMX_HANDLER_TYPE handler_id);

    // Unregisters certain handler.
    uint32_t UnregisterHandler(BMX_HANDLER_TYPE handler_id);
    uint32_t UnregisterHandler(BMX_HANDLER_TYPE handler_id, GENERIC_HANDLER_CALLBACK handler_callback);

    // Registers port handler.
    uint32_t RegisterPortHandler(
        GatewayWorker *gw,
        uint16_t port_num,
        const char* app_name_string,
        BMX_HANDLER_TYPE handler_id,
        GENERIC_HANDLER_CALLBACK port_handler,
        db_index_type db_index,
        BMX_HANDLER_INDEX_TYPE& out_handler_index);

    // Registers sub-port handler.
    uint32_t RegisterSubPortHandler(
        GatewayWorker *gw,
        uint16_t port,
        const char* app_name_string,
        bmx::BMX_SUBPORT_TYPE subport,
        BMX_HANDLER_TYPE handler_id,
        GENERIC_HANDLER_CALLBACK port_handle,
        db_index_type db_index,
        BMX_HANDLER_INDEX_TYPE& out_handler_index);

    // Registers URI handler.
    uint32_t RegisterUriHandler(
        GatewayWorker *gw,
        uint16_t port,
        const char* app_name_string,
        const char* original_uri_string,
        const char* processed_uri_string,
        uint8_t* param_types,
        int32_t num_params,
        BMX_HANDLER_TYPE handler_id,
        GENERIC_HANDLER_CALLBACK port_handle,
        db_index_type db_index,
        BMX_HANDLER_INDEX_TYPE& out_handler_index,
        ReverseProxyInfo* reverse_proxy_info);

    // Constructor.
    HandlersTable()
    {
        Erase();
    }

    // Erasing this table.
    void Erase()
    {
        max_num_entries_ = 0;
    }

    // Destructor.
    ~HandlersTable()
    {
        Erase();
    }
};

class PortHandlers
{
    // Port number.
    uint16_t port_number_;

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
        global_port_statistics_stream << "Port " << port_number_ << " has following handlers registered: ";
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
                    handler_lists_.RemoveByIndex(i);
                    i--;
                }

                removed = true;

                // Not stopping, going through all entries.
            }
        }

        return removed;
    }

    // Removing certain entry.
    bool RemoveEntry(HandlersList* handlers_list)
    {
        // Removing handler list.
        return handler_lists_.RemoveEntry(handlers_list);
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
                // Checking if there are no databases left.
                handler_lists_.RemoveByIndex(i);
                --i;

                removed = true;

                // Not stopping, going through all entries.
            }
        }

        return removed;
    }

    // Checking if certain database is contained.
    int32_t GetEntryIndex(db_index_type db_index)
    {
        // Going through all handler lists.
        for (int32_t i = 0; i < handler_lists_.get_num_entries(); ++i)
        {
            if (handler_lists_[i]->get_db_index() == db_index)
            {
                return i;
            }
        }

        return INVALID_INDEX;
    }

    // Checking if certain handler is contained.
    int32_t GetEntryIndex(GENERIC_HANDLER_CALLBACK handler)
    {
        // Going through all handler lists.
        for (int32_t i = 0; i < handler_lists_.get_num_entries(); ++i)
        {
            if (handler_lists_[i]->ContainsHandler(handler))
            {
                return i;
            }
        }

        return INVALID_INDEX;
    }

    void set_port_number(uint16_t port_num)
    {
        port_number_ = port_num;
    }

    // Resetting entry.
    void Reset()
    {
        // Removing all handlers lists.
        handler_lists_.Clear();

        port_number_ = 0;
    }

    // Running all registered handlers.
    uint32_t RunHandlers(GatewayWorker *gw, SocketDataChunkRef sd, bool* is_handled);
};

class RegisteredSubport
{
    // Subport number.
    bmx::BMX_SUBPORT_TYPE subport_;
 
    // Unique handler lists.
    LinearList<HandlersList*, bmx::MAX_NUMBER_OF_HANDLERS_IN_LIST> handler_lists_;

public:

    bmx::BMX_SUBPORT_TYPE get_subport()
    {
        return subport_;
    }

    RegisteredSubport()
    {
        Reset();
    }

    bool IsEmpty()
    {
        return (0 == subport_);
    }

    void Reset()
    {
        subport_ = 0;
    }
};

class RegisteredSubports
{
    // Array of all registered URIs.
    LinearList<RegisteredSubport, bmx::MAX_TOTAL_NUMBER_OF_HANDLERS> reg_uris_;

public:

    // Constructor.
    RegisteredSubports()
    {
    }

    // Checking if handlers list is empty.
    bool IsEmpty()
    {
        return reg_uris_.IsEmpty();
    }

    // Removing certain entry.
    bool RemoveEntry(db_index_type db_index)
    {
        return false;
    }

    // Removing certain entry.
    bool RemoveEntry(HandlersList* handlers_list)
    {
        return false;
    }
};

} // namespace network
} // namespace starcounter

#endif // HANDLERS_HPP