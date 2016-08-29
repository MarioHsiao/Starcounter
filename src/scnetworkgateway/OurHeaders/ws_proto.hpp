#pragma once
#ifndef WS_PROTO_HPP
#define WS_PROTO_HPP

namespace starcounter {
namespace network {

// Message types.
enum WS_OPCODE
{
    WS_OPCODE_CONTINUATION = 0,
    WS_OPCODE_TEXT = MixedCodeConstants::WebSocketDataTypes::WS_OPCODE_TEXT,
    WS_OPCODE_BINARY = MixedCodeConstants::WebSocketDataTypes::WS_OPCODE_BINARY,
    WS_OPCODE_RESERVED1 = 3,
    WS_OPCODE_RESERVED2 = 4,
    WS_OPCODE_RESERVED3 = 5,
    WS_OPCODE_RESERVED4 = 6,
    WS_OPCODE_RESERVED5 = 7,
    WS_OPCODE_CLOSE = MixedCodeConstants::WebSocketDataTypes::WS_OPCODE_CLOSE,
    WS_OPCODE_PING = MixedCodeConstants::WebSocketDataTypes::WS_OPCODE_PING,
    WS_OPCODE_PONG = 10,
    WS_OPCODE_RESERVED6 = 11,
    WS_OPCODE_RESERVED7 = 12,
    WS_OPCODE_RESERVED8 = 13,
    WS_OPCODE_RESERVED9 = 14,
    WS_OPCODE_RESERVED10 = 15
};

// Frame types.
enum WS_FRAGMENT_FLAG
{
    WS_FRAME_SINGLE,
    WS_FRAME_FIRST,
    WS_FRAME_CONT,
    WS_FRAME_LAST
};

class GatewayWorker;
class SocketDataChunk;

class WsProto
{
    // Opcode type.
    uint8_t opcode_;

public:

    uint8_t* get_opcode_addr() {
        return &opcode_;
    }

    // Sets the client key.
    void SetClientKey(char *client_key, int32_t client_key_len);

    // Sets the sub protocol.
    void SetSubProtocol(char *sub_protocol, int32_t sub_protocol_len);

    // Resets the structure.
    void Reset();

    void Init();

    uint32_t UnmaskFrameAndPush(GatewayWorker *gw, SocketDataChunkRef sd, BMX_HANDLER_TYPE user_handler_id, uint32_t mask, bool last_frame);

    uint32_t ProcessWsDataToDb(GatewayWorker *gw, SocketDataChunkRef sd, BMX_HANDLER_TYPE user_handler_id);

    static uint32_t SendWebSocketDisconnectToDb(GatewayWorker *gw, SocketDataChunk* sd);

    uint32_t ProcessWsDataFromDb(GatewayWorker *gw, SocketDataChunkRef sd, BMX_HANDLER_TYPE user_handler_id);

    static uint32_t DoHandshake(GatewayWorker *gw, SocketDataChunkRef sd, BMX_HANDLER_TYPE user_handler_id);

    void MaskUnMask(
        uint8_t* data,
        int32_t data_len,
        uint64_t mask,
        int8_t& num_remaining_bytes);

    void UnMaskPayload(GatewayWorker* gw, SocketDataChunkRef sd, uint32_t payloadLen, uint64_t mask, uint8_t* data);

    uint8_t *WritePayload(GatewayWorker* gw, SocketDataChunkRef sd, uint8_t opcode, bool masking, WS_FRAGMENT_FLAG frame_type, uint32_t total_payload_len, uint8_t* payload, uint32_t& payload_len);

    // Parses WebSockets frame info.
    bool ParseFrameInfo(
        uint8_t* data,
        uint8_t* limit,
        uint32_t* out_mask, 
        uint64_t* out_payload_len,
        uint8_t* out_header_len);
};

class RegisteredWsChannel
{
    HandlersList* handler_list_;

public:

    RegisteredWsChannel() {}

    // Getting owner app name.
    char* get_app_name()
    {
        return handler_list_->get_app_name();
    }

    // Getting owner database index.
    db_index_type get_db_index()
    {
        return handler_list_->get_db_index();
    }

    // Getting registered channel name.
    char* get_channel_name()
    {
        return handler_list_->get_method_space_uri();
    }

    uint16_t get_port()
    {
        return handler_list_->get_port();
    }

    // Getting registered channel id.
    int32_t get_channel_id()
    {
        return handler_list_->get_subport();
    }

    RegisteredWsChannel(HandlersList* handler_list)
    {
        handler_list_ = handler_list;
    }

    // Resetting entry.
    void Erase()
    {
        GwDeleteSingle(handler_list_);
        handler_list_ = NULL;
    }

    // Removes certain entry.
    bool ContainsDb(db_index_type db_index)
    {
        return (handler_list_->get_db_index() == db_index);
    }

    BMX_HANDLER_TYPE GetHandlerInfo()
    {
        return handler_list_->get_handler_info();
    }
};

class PortWsGroups
{
    // Array of all registered URIs.
    LinearList<RegisteredWsChannel, bmx::MAX_TOTAL_NUMBER_OF_HANDLERS> reg_ws_channels_;

    // Port to which this URI matcher belongs.
    uint16_t port_number_;

public:

    // Printing the registered channels.
    void PrintRegisteredChannels(std::stringstream& stats_stream)
    {
        bool first = true;

        stats_stream << ",\"registeredWsChannels\":[";

        for (int32_t i = 0; i < reg_ws_channels_.get_num_entries(); i++)
        {
            if (!first) 
                stats_stream << ",";
            first = false;

            stats_stream << "{\"wschannel\":\"" << reg_ws_channels_[i].get_channel_name() << "\",";
            stats_stream << "\"location\":";

            db_index_type db_index = reg_ws_channels_[i].get_db_index();
            if (INVALID_DB_INDEX == db_index) {
                stats_stream << "\"gateway\"";
            } else {
                stats_stream << '"' << g_gateway.GetDatabase(db_index)->get_db_name() << '"';
            }

            stats_stream << ",\"application\":\"" << reg_ws_channels_[i].get_app_name() << "\""; 

            stats_stream << "}";
        }

        stats_stream << "]";
    }

    // Checking if handlers list is empty.
    bool IsEmpty()
    {
        return reg_ws_channels_.IsEmpty();
    }

    uint16_t get_port_number()
    {
        return port_number_;
    }

    // Constructor.
    PortWsGroups(uint16_t port_number)
    {
        port_number_ = port_number;
    }

    // Removes certain entry.
    bool RemoveEntry(db_index_type db_index)
    {
        bool removed = false;

        // Going through all handler list.
        for (int32_t i = 0; i < reg_ws_channels_.get_num_entries(); i++)
        {
            // Checking if database index is the same.
            if (reg_ws_channels_[i].ContainsDb(db_index))
            {
                reg_ws_channels_[i].Erase();

                reg_ws_channels_.RemoveByIndex(i);
                i--;

                removed = true;

                // Not stopping, going through all entries.
            }
        }

        return removed;
    }

    // Printing the registered URIs.
    void PrintRegisteredWsChannels(std::stringstream& stats_stream)
    {
        stats_stream << "Following URIs are registered: " << "<br>";
        for (int32_t i = 0; i < reg_ws_channels_.get_num_entries(); i++)
        {
            stats_stream << "    \"" << reg_ws_channels_[i].get_channel_name() << "\" in";

            stats_stream << " database \"" << g_gateway.GetDatabase(reg_ws_channels_[i].get_db_index())->get_db_name() << "\"";

            stats_stream << "<br>";
        }
    }

    // Checking if entry already exists.
    bool ContainsEntry(char* channel_name)
    {
        int32_t index = FindRegisteredChannelName(channel_name);
        return (index >= 0);
    }

    // Checking if entry already exists.
    bool ContainsEntry(char* channel_name, db_index_type db_index)
    {
        int32_t index = FindRegisteredChannelName(channel_name);

        // Checking if entry found.
        if (index >= 0)
        {
            // Checking database index.
            if (reg_ws_channels_[index].ContainsDb(db_index))
                return true;
        }

        return false;
    }

    // Find certain entry.
    BMX_HANDLER_TYPE FindRegisteredHandlerByChannelId(const uint32_t channel_id)
    {
        // Going through all entries.
        for (uri_index_type i = 0; i < reg_ws_channels_.get_num_entries(); i++)
        {
            // Doing exact comparison.
            if (channel_id == reg_ws_channels_[i].get_channel_id())
            {
                return reg_ws_channels_[i].GetHandlerInfo();
            }
        }

        // Returning negative if nothing is found.
        return bmx::BMX_INVALID_HANDLER_INFO;
    }

    // Find certain entry.
    uri_index_type FindRegisteredChannelName(const char* channel_name)
    {
        // Going through all entries.
        for (uri_index_type i = 0; i < reg_ws_channels_.get_num_entries(); i++)
        {
            // Doing exact comparison.
            if (!strcmp(channel_name, reg_ws_channels_[i].get_channel_name()))
            {
                return i;
            }
        }

        // Returning negative if nothing is found.
        return INVALID_URI_INDEX;
    }

    // Getting entry by index.
    RegisteredWsChannel* GetEntryByIndex(int32_t index)
    {
        return reg_ws_channels_.GetElemPtr(index);
    }

    void AddNewEntry(
        BMX_HANDLER_TYPE handler_info,
        const char* app_name_string,
        ws_group_id_type channel_id,
        const char* channel_name,
        db_index_type db_index)
    {
        HandlersList* hl = GwNewConstructor(HandlersList);

        uint32_t err_code = hl->Init(
            bmx::HANDLER_TYPE::WS_HANDLER,
            handler_info,
            port_number_,
            app_name_string,
            channel_id,
            channel_name,
            NULL,
            0,
            db_index,
            0,
            NULL
            );

        GW_ASSERT(0 == err_code);

        RegisteredWsChannel w(hl);        
        reg_ws_channels_.Add(w);
    }
};

} // namespace network
} // namespace starcounter

#endif // WS_PROTO_HPP
