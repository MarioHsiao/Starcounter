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
        return (type_ == bmx::HANDLER_TYPE::UNUSED_HANDLER);
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

    // Runs port handlers.
    uint32_t RunHandlers(GatewayWorker *gw, SocketDataChunk *sd)
    {
        uint32_t err_code;
        bool is_handled = false;

        // Going through all registered handlers.
        for (int32_t i = 0; i < num_entries_; i++)
        {
            // Running the handler.
            err_code = handlers_[i](gw, sd, handler_id_, &is_handled);

            // Checking if information was handled and no errors occurred.
            if (is_handled || err_code)
                return err_code;
        }

        return SCERRUNSPECIFIED; // SCPORTNOTHANDLED
    }

    /*
    // Should be called when whole handlers list should be unregistered.
    uint32_t UnregisterGlobally(int32_t db_index)
    {
        // Checking the type of handler.
        switch(type_)
        {
            case bmx::HANDLER_TYPE::PORT_HANDLER:
            {
                break;
            }

            case bmx::HANDLER_TYPE::SUBPORT_HANDLER:
            {
                // Unregister globally.

                break;
            }

            case bmx::HANDLER_TYPE::URI_HANDLER:
            {
                // Unregister globally.
                g_gateway.FindServerPort(port_)->get_registered_uris()->RemoveEntry(uri_string_, uri_len_chars_, db_index);

                break;
            }

            default:
            {
                return SCERRUNSPECIFIED; // SCERRWRONGHANDLERTYPE
            }
        }

        return 0;
    }
    */
};

// All handlers belonging to database.
class HandlersTable
{
    // Total registered handlers.
    BMX_HANDLER_TYPE max_num_entries_;

    // All registered handlers.
    HandlersList registered_handlers_[bmx::MAX_TOTAL_NUMBER_OF_HANDLERS];

public:

    // Find port user handler id.
    BMX_HANDLER_TYPE FindPortUserHandlerIndex(uint16_t port_num)
    {
        for (BMX_HANDLER_TYPE i = 0; i < max_num_entries_; i++)
        {
            if (bmx::PORT_HANDLER == registered_handlers_[i].get_type())
            {
                if (port_num == registered_handlers_[i].get_port())
                {
                    return i;
                }
            }
        }

        return -1;
    }

    // Find subport user handler id.
    BMX_HANDLER_TYPE FindSubPortUserHandlerIndex(uint16_t port_num, uint32_t subport_num)
    {
        for (BMX_HANDLER_TYPE i = 0; i < max_num_entries_; i++)
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

        return -1;
    }

    // Find URI user handler id.
    BMX_HANDLER_TYPE FindUriUserHandlerIndex(uint16_t port_num, const char* uri_string, uint32_t uri_len_chars)
    {
        int32_t longest_matched_uri = 0;

        for (BMX_HANDLER_TYPE i = 0; i < max_num_entries_; i++)
        {
            if (bmx::HANDLER_TYPE::URI_HANDLER == registered_handlers_[i].get_type())
            {
                if (port_num == registered_handlers_[i].get_port())
                {
                    // Comparing URI as starts with.
                    if (!strncmp(registered_handlers_[i].get_uri(), uri_string, uri_len_chars))
                    {
                        return i;
                    }
                }
            }
        }

        return -1;
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
    uint32_t UnregisterHandler(BMX_HANDLER_TYPE handler_id, GENERIC_HANDLER_CALLBACK user_handler);

    // Registers port handler.
    uint32_t RegisterPortHandler(
        GatewayWorker *gw,
        uint16_t port_num,
        BMX_HANDLER_TYPE user_handler_id,
        GENERIC_HANDLER_CALLBACK port_handler,
        int32_t db_index);

    // Registers sub-port handler.
    uint32_t RegisterSubPortHandler(
        GatewayWorker *gw,
        uint16_t port,
        uint32_t subport,
        BMX_HANDLER_TYPE user_handler_id,
        GENERIC_HANDLER_CALLBACK port_handle,
        int32_t db_index);

    // Registers URI handler.
    uint32_t RegisterUriHandler(
        GatewayWorker *gw,
        uint16_t port,
        char* uri_string,
        uint32_t uri_str_chars,
        bmx::HTTP_METHODS http_method,
        BMX_HANDLER_TYPE user_handler_id,
        GENERIC_HANDLER_CALLBACK port_handle,
        int32_t db_index);

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

class UniqueHandlerList
{
    // Database index (if any).
    int32_t db_index_;

    // Reference to corresponding handlers list.
    HandlersList* handlers_list_;

public:

    UniqueHandlerList(int32_t db_index, HandlersList* handlers_list)
    {
        db_index_ = db_index;
        handlers_list_ = handlers_list;
    }

    UniqueHandlerList()
    {
        Reset();
    }

    int32_t get_db_index()
    {
        return db_index_;
    }

    HandlersList* get_handlers_list()
    {
        return handlers_list_;
    }

    void Reset()
    {
        db_index_ = ~0;
        handlers_list_ = NULL;
    }
};

class UniquePortHandler
{
    // Database index (if any).
    LinearList<int32_t, MAX_ACTIVE_DATABASES> db_indexes_;

    // Reference to corresponding handlers list.
    GENERIC_HANDLER_CALLBACK handler_;

public:

    LinearList<int32_t, MAX_ACTIVE_DATABASES>* get_db_indexes()
    {
        return &db_indexes_;
    }

    GENERIC_HANDLER_CALLBACK get_handler()
    {
        return handler_;
    }

    uint32_t GetNumberOfAttachedDbs()
    {
        return db_indexes_.get_num_entries();
    }

    UniquePortHandler()
    {
        Reset();
    }

    void Add(int32_t db_index)
    {
        db_indexes_.Add(db_index);
    }

    bool Remove(int32_t db_index)
    {
        return db_indexes_.Remove(db_index);
    }

    bool IsEmpty()
    {
        return db_indexes_.IsEmpty();
    }

    void Add(int32_t db_index, GENERIC_HANDLER_CALLBACK handler)
    {
        db_indexes_.Add(db_index);
        handler_ = handler;
    }

    void Reset()
    {
        db_indexes_.Clear();
        handler_ = NULL;
    }
};

class PortHandlers
{
    // Port number.
    uint16_t port_number_;

    // Handler lists.
    LinearList<UniquePortHandler, bmx::MAX_NUMBER_OF_HANDLERS_IN_LIST> handlers_;

public:

    // Printing the registered URIs.
    void Print()
    {
        GW_PRINT_GLOBAL << "Port " << port_number_ << " has following handlers registered: ";
        for (int32_t i = 0; i < handlers_.get_num_entries(); i++)
        {
            GW_COUT << handlers_[i].GetNumberOfAttachedDbs() << ", ";
        }
        GW_COUT << std::endl;
    }

    // Constructor.
    PortHandlers()
    {
        Reset();
    }

    // Checking if handlers list is empty.
    bool IsEmpty()
    {
        return handlers_.IsEmpty();
    }

    // Removes certain entry.
    bool RemoveEntry(GENERIC_HANDLER_CALLBACK handler)
    {
        // Going through all handler lists.
        for (int32_t i = 0; i < handlers_.get_num_entries(); ++i)
        {
            // Checking if handler is the same.
            if (handlers_[i].get_handler() == handler)
            {
                handlers_.RemoveByIndex(i);
                break;
            }
        }

        // Checking if list is empty.
        if (IsEmpty())
            return true;

        return false;
    }

    // Removes certain entry.
    bool RemoveEntry(int32_t db_index, GENERIC_HANDLER_CALLBACK handler)
    {
        return RemoveEntry(db_index) && RemoveEntry(handler);
    }

    // Removes certain entry.
    bool RemoveEntry(int32_t db_index)
    {
        // Going through all handler lists.
        for (int32_t i = 0; i < handlers_.get_num_entries(); ++i)
        {
            if (handlers_[i].Remove(db_index))
            {
                // Checking if there are no databases left.
                if (handlers_[i].IsEmpty())
                {
                    handlers_.RemoveByIndex(i);
                    --i;
                }

                // Not stopping, going through all entries.
            }
        }

        // Checking if list is empty.
        if (IsEmpty())
            return true;

        return false;
    }

    // Checking if certain database is contained.
    int32_t GetEntryIndex(int32_t db_index)
    {
        // Going through all handler lists.
        for (int32_t i = 0; i < handlers_.get_num_entries(); ++i)
        {
            if (handlers_[i].get_db_indexes()->Find(db_index))
            {
                return i;
            }
        }

        return -1;
    }

    // Checking if certain handler is contained.
    int32_t GetEntryIndex(GENERIC_HANDLER_CALLBACK handler)
    {
        // Going through all handler lists.
        for (int32_t i = 0; i < handlers_.get_num_entries(); ++i)
        {
            if (handlers_[i].get_handler() == handler)
            {
                return i;
            }
        }

        return -1;
    }

    // Checking if certain handler is contained.
    int32_t GetEntryIndex(int32_t db_index, GENERIC_HANDLER_CALLBACK handler)
    {
        // Going through all handler lists.
        for (int32_t i = 0; i < handlers_.get_num_entries(); ++i)
        {
            if ((handlers_[i].get_db_indexes()->Find(db_index)) &&
                (handlers_[i].get_handler() == handler))
            {
                return i;
            }
        }

        return -1;
    }

    void set_port_number(uint16_t port_num)
    {
        port_number_ = port_num;
    }

    // Initializing the entry.
    void Add(int32_t db_index, GENERIC_HANDLER_CALLBACK handler)
    {
        // Checking if does not have this handler.
        int32_t index = GetEntryIndex(handler);
        if (index < 0)
        {
            // Creating and pushing new handlers list.
            UniquePortHandler new_entry;
            new_entry.Add(db_index, handler);
            handlers_.Add(new_entry);
        }
        else
        {
            // Checking if does not contain entry for this database.
            if (GetEntryIndex(db_index, handler) < 0)
                handlers_[index].Add(db_index);
        }
    }

    // Resetting entry.
    void Reset()
    {
        // Removing all handlers lists.
        handlers_.Clear();

        port_number_ = 0;
    }

    // Running all registered handlers.
    uint32_t RunHandlers(GatewayWorker *gw, SocketDataChunk *sd)
    {
        uint32_t err_code;
        bool is_handled = false;

        // Going through all handler list.
        for (int32_t i = 0; i < handlers_.get_num_entries(); ++i)
        {
            err_code = (handlers_[i].get_handler())(gw, sd, -1, &is_handled);

            // Checking if information was handled and no errors occurred.
            if (is_handled || err_code)
                return err_code;
        }

        return SCERRUNSPECIFIED; // SCPORTNOTHANDLED
    }
};

/*
class RegisteredPort
{
    // Port number.
    uint16_t port_num_;

    // Handler lists.
    std::list<UniqueHandlerList> handler_lists_;

public:

    // Constructor.
    RegisteredPort()
    {
        Reset();
    }

    // Checking if handlers list is empty.
    bool IsEmpty()
    {
        return handler_lists_.empty();
    }

    // Removes certain entry.
    bool Remove(HandlersList* handlers_list)
    {
        // Going through all handler list.
        for (std::list<UniqueHandlerList>::iterator i = handler_lists_.begin();
            i != handler_lists_.end();
            ++i)
        {
            if ((*i).handlers_list_ == handlers_list)
            {
                handler_lists_.erase(i);
                break;
            }
        }

        // Checking if list is empty.
        if (IsEmpty())
            return true;

        return false;
    }

    // Removes certain entry.
    bool Remove(int32_t db_index)
    {
        // Going through all handler list.
        for (std::list<UniqueHandlerList>::iterator i = handler_lists_.begin();
            i != handler_lists_.end();
            ++i)
        {
            if ((*i).db_index_ == db_index)
            {
                handler_lists_.erase(i);
                break;
            }
        }

        // Checking if list is empty.
        if (IsEmpty())
            return true;

        return false;
    }

    // Checking if certain handler is contained.
    bool Contains(int32_t db_index, HandlersList* handlers_list)
    {
        // Going through all handler list.
        for (std::list<UniqueHandlerList>::iterator i = handler_lists_.begin();
            i != handler_lists_.end();
            ++i)
        {
            if (((*i).db_index_ == db_index) &&
                ((*i).handlers_list_ == handlers_list))
            {
                return true;
            }
        }

        return false;
    }

    RegisteredPort(uint16_t port_num)
    {
        port_num_ = port_num;
    }

    // Initializing the entry.
    void Add(int32_t db_index, HandlersList* handlers_list)
    {
        // Adding entry only if its not contained there.
        if (!Contains(db_index, handlers_list))
        {
            // Creating and pushing new handlers list.
            UniqueHandlerList new_entry(db_index, handlers_list);
            handler_lists_.push_back(new_entry);
        }
    }

    // Resetting entry.
    void Reset()
    {
        // Removing all handlers lists.
        handler_lists_.clear();

        port_num_ = 0;
    }

    // Running all registered handlers.
    uint32_t RunHandlers(GatewayWorker *gw, SocketDataChunk *sd)
    {
        uint32_t err_code;
        bool is_handled = false;

        // Going through all handler list.
        for (std::list<UniqueHandlerList>::iterator i = handler_lists_.begin();
            i != handler_lists_.end();
            ++i)
        {
            err_code = (*i).handlers_list_->RunHandlers(gw, sd);

            // Checking if information was handled and no errors occurred.
            if (is_handled || err_code)
                return err_code;
        }

        return SCERRUNSPECIFIED; // SCPORTNOTHANDLED
    }
};
*/

class RegisteredSubport
{
    // Subport number.
    uint32_t subport_;
 
    // Unique handler lists.
    LinearList<UniqueHandlerList, bmx::MAX_NUMBER_OF_HANDLERS_IN_LIST> handler_lists_;

public:

    uint32_t get_subport()
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
    bool RemoveEntry(int32_t db_index)
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