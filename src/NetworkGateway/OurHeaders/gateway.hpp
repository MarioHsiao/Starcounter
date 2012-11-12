#pragma once
#ifndef GATEWAY_HPP
#define GATEWAY_HPP

// Connectivity headers include.
#include "common/macro_definitions.hpp"
#include "common/config_param.hpp"
#include "common/shared_interface.hpp"
#include "common/database_shared_memory_parameters.hpp"
#include "common/monitor_interface.hpp"
#include "common/circular_buffer.hpp"
#include "common/bounded_buffer.hpp"
#include "common/chunk.hpp"
#include "common/shared_chunk_pool.hpp"
#include "common/chunk_pool.hpp"
#include "common/channel.hpp"
#include "common/scheduler_channel.hpp"
#include "common/common_scheduler_interface.hpp"
#include "common/scheduler_interface.hpp"
#include "common/common_client_interface.hpp"
#include "common/client_interface.hpp"
#include "common/client_number.hpp"
#include "common/macro_definitions.hpp"
#include "common/interprocess.hpp"
#include "common/name_definitions.hpp"

// HTTP related stuff.
#include "../../HTTP/HttpParser/OurHeaders/http_request.hpp"

// BMX/Blast2 include.
#include "../Chunks/bmx/bmx.hpp"
#include "../Chunks/bmx/chunk_helper.h"

// Internal includes.
#include "utilities.hpp"
#include "profiler.hpp"

namespace starcounter {
namespace network {

// Data types definitions.
typedef uint32_t channel_chunk;
typedef uint64_t apps_unique_session_num_type;
typedef uint64_t session_salt_type;
typedef uint32_t session_index_type;

// Some defines, e.g. debugging, statistics, etc.
//#define GW_GLOBAL_STATISTICS
//#define GW_SOCKET_DIAG
//#define GW_HTTP_DIAG
//#define GW_WEBSOCKET_DIAG
#define GW_ERRORS_DIAG
#define GW_WARNINGS_DIAG
//#define GW_CHUNKS_DIAG
#define GW_DATABASES_DIAG
#define GW_SESSIONS_DIAG
//#define GW_PONG_MODE

// Maximum number of ports the gateway operates with.
const int32_t MAX_PORTS_NUM = 16;

// Maximum number of URIs the gateway operates with.
const int32_t MAX_URIS_NUM = 1024;

// Maximum number of handlers per port.
const int32_t MAX_RAW_HANDLERS_PER_PORT = 256;

// Maximum number of URI handlers per port.
const int32_t MAX_URI_HANDLERS_PER_PORT = 16;

// Length of accepting data structure.
const int32_t ACCEPT_DATA_SIZE_BYTES = 64;

// Maximum number of chunks to pop at once.
const int32_t MAX_CHUNKS_TO_POP_AT_ONCE = 128;

// Maximum number of fetched OVLs at once.
const int32_t MAX_FETCHED_OVLS = 256;

// Maximum size of HTTP body.
const int32_t MAX_HTTP_BODY_SIZE = 1024 * 1024 * 32;

// Number of sockets to increase the accept roof.
const int32_t ACCEPT_ROOF_STEP_SIZE = 1;

// Offset of data blob in socket data.
const int32_t SOCKET_DATA_BLOB_OFFSET_BYTES = 680;

// Length of blob data in bytes.
const int32_t SOCKET_DATA_BLOB_SIZE_BYTES = bmx::MAX_DATA_BYTES_IN_CHUNK - bmx::BMX_HEADER_MAX_SIZE_BYTES - SOCKET_DATA_BLOB_OFFSET_BYTES;

// Size of OVERLAPPED structure.
const int32_t OVERLAPPED_SIZE = sizeof(OVERLAPPED);

// Bad chunk index.
const uint32_t INVALID_CHUNK_INDEX = shared_memory_chunk::LINK_TERMINATOR;

// Bad linear session index.
const session_index_type INVALID_SESSION_INDEX = ~0;

// Bad linear URI index.
const uint32_t INVALID_URI_INDEX = ~0;

// Bad scheduler index.
const uint32_t INVALID_SCHEDULER_ID = ~0;

// Bad session salt.
const session_salt_type INVALID_SESSION_SALT = 0;

// Invalid Apps unique number.
const uint64_t INVALID_APPS_UNIQUE_SESSION_NUMBER = 0;

// Maximum number of chunks to keep in private chunk pool
// until we release them to shared chunk pool.
const int32_t MAX_CHUNKS_IN_PRIVATE_POOL = 512;

// Number of chunks to leave in private chunk pool after releasing
// rest of the chunks to shared chunk pool.
const int32_t NUM_CHUNKS_TO_LEAVE_IN_PRIVATE_POOL = 256;

// Offset of overlapped data structure inside socket data chunk:
const int32_t OVL_OFFSET_IN_CHUNK = 24;

// Number of predefined gateway port types
const int32_t NUM_PREDEFINED_PORT_TYPES = 5;

// Size of local/remove address structure.
const int32_t SOCKADDR_SIZE_EXT = sizeof(sockaddr_in) + 16;

// Maximum number of active databases.
const int32_t MAX_ACTIVE_DATABASES = 32;

// Maximum number of workers.
const int32_t MAX_WORKER_THREADS = 32;

// Maximum number of active server ports.
const int32_t MAX_ACTIVE_SERVER_PORTS = 32;

// Maximum port handle integer.
const int32_t MAX_SOCKET_HANDLE = 100000;

// Session string length in characters.
const int32_t SC_SESSION_STRING_LEN_CHARS = 24;

// User data offset in blobs for different protocols.
const int32_t HTTP_BLOB_USER_DATA_OFFSET = 0;
const int32_t HTTPS_BLOB_USER_DATA_OFFSET = 2048;
const int32_t RAW_BLOB_USER_DATA_OFFSET = 0;
const int32_t AGGR_BLOB_USER_DATA_OFFSET = 64;
const int32_t SUBPORT_BLOB_USER_DATA_OFFSET = 32;
const int32_t WS_BLOB_USER_DATA_OFFSET = 16;

// Error code type.
#define GW_ERR_CHECK(err_code) if (0 != err_code) return err_code

// Printing prefixes.
#define GW_PRINT_WORKER (GW_COUT << "[" << worker_id_ << "]: ")
#define GW_PRINT_GLOBAL (GW_COUT << "Global: ")

// Port types.
enum PortType
{
    GENSOCKETS_PORT = 1,
    HTTP_PORT = 2,
    WEBSOCKETS_PORT = 4,
    HTTPS_PORT = 8,
    AGGREGATION_PORT = 16
};

// Type of operation on the socket.
enum SocketOperType
{
    SEND_OPER,
    RECEIVE_OPER,
    ACCEPT_OPER,
    CONNECT_OPER,
    DISCONNECT_OPER,
    TO_DB_OPER,
    FROM_DB_OPER,
    UNKNOWN_OPER
};

class SocketDataChunk;
class GatewayWorker;

typedef uint32_t (*GENERIC_HANDLER_CALLBACK) (
    GatewayWorker *gw,
    SocketDataChunk *sd,
    BMX_HANDLER_TYPE handler_id,
    bool* is_handled);

uint32_t OuterPortProcessData(
    GatewayWorker *gw,
    SocketDataChunk *sd,
    BMX_HANDLER_TYPE handler_id,
    bool* is_handled);

uint32_t PortProcessData(
    GatewayWorker *gw,
    SocketDataChunk *sd,
    BMX_HANDLER_TYPE handler_id,
    bool* is_handled);

uint32_t OuterSubportProcessData(
    GatewayWorker *gw,
    SocketDataChunk *sd,
    BMX_HANDLER_TYPE handler_id,
    bool* is_handled);

uint32_t SubportProcessData(
    GatewayWorker *gw,
    SocketDataChunk *sd,
    BMX_HANDLER_TYPE handler_id,
    bool* is_handled);

uint32_t OuterUriProcessData(
    GatewayWorker *gw,
    SocketDataChunk *sd,
    BMX_HANDLER_TYPE handler_id,
    bool* is_handled);

uint32_t UriProcessData(
    GatewayWorker *gw,
    SocketDataChunk *sd,
    BMX_HANDLER_TYPE handler_id,
    bool* is_handled);

extern std::string GetOperTypeString(SocketOperType typeOfOper);

// Pointers to extended WinSock functions.
extern LPFN_ACCEPTEX AcceptExFunc;
extern LPFN_CONNECTEX ConnectExFunc;
extern LPFN_DISCONNECTEX DisconnectExFunc;

template <class T, uint32_t MaxElems>
class LinearList
{
    T elems_[MaxElems];
    uint32_t num_entries_;

public:

    LinearList()
    {
        num_entries_ = 0;
    }

    uint32_t get_num_entries()
    {
        return num_entries_;
    }

    T& operator[](uint32_t index)
    {
        return elems_[index];
    }

    T* GetElemRef(uint32_t index)
    {
        return elems_ + index;
    }

    bool IsEmpty()
    {
        return (0 == num_entries_);
    }

    void Add(T& new_elem)
    {
        elems_[num_entries_] = new_elem;
        num_entries_++;
    }

    void Clear()
    {
        num_entries_ = 0;
    }

    void RemoveByIndex(uint32_t index)
    {
        // Checking if it was not the last handler in the array.
        if (index < (num_entries_ - 1))
        {
            // Shifting all forward handlers.
            for (uint32_t k = index; k < (num_entries_ - 1); ++k)
                elems_[k] = elems_[k + 1];
        }

        // Number of entries decreased by one.
        num_entries_--;
    }

    bool Remove(T& elem)
    {
        for (uint32_t i = 0; i < num_entries_; i++)
        {
            if (elem == elems_[i])
            {
                // Checking if it was not the last handler in the array.
                if (i < (num_entries_ - 1))
                {
                    // Shifting all forward handlers.
                    for (uint32_t k = i; k < (num_entries_ - 1); ++k)
                        elems_[k] = elems_[k + 1];
                }

                // Number of entries decreased by one.
                num_entries_--;

                return true;
            }
        }

        return false;
    }

    bool Find(T& elem)
    {
        for (uint32_t i = 0; i < num_entries_; i++)
        {
            if (elem == elems_[i])
            {
                return true;
            }
        }

        return false;
    }

    void Sort()
    {
        std::sort(elems_, elems_ + num_entries_);
    }
};

// Accumulative buffer.
class AccumBuffer
{
    // Length of the buffer used in the next operation: receive, send.
    ULONG buf_len_bytes_;

    // Current buffer pointer (assuming data accumulation).
    uint8_t* cur_buf_ptr_;

    // Initial data pointer.
    uint8_t* orig_buf_ptr_;

    // Original buffer total length.
    ULONG orig_buf_len_bytes_;

    // Total number of bytes accumulated.
    ULONG accum_len_bytes_;

    // Number of bytes received last time.
    ULONG last_recv_bytes_;

    // Desired number of bytes to accumulate.
    ULONG desired_accum_bytes_;

public:

    // Default initializer.
    AccumBuffer()
    {
        buf_len_bytes_ = 0;
        cur_buf_ptr_ = NULL;
        orig_buf_ptr_ = NULL;
        orig_buf_len_bytes_ = 0;
        accum_len_bytes_ = 0;
        last_recv_bytes_ = 0;
        desired_accum_bytes_ = 0;
    }

    // Initializes accumulative buffer.
    void Init(ULONG buf_total_len_bytes, uint8_t* orig_buf_ptr, bool reset_accum_len)
    {
        orig_buf_len_bytes_ = buf_total_len_bytes;
        buf_len_bytes_ = buf_total_len_bytes;
        orig_buf_ptr_ = orig_buf_ptr;
        cur_buf_ptr_ = orig_buf_ptr;
        last_recv_bytes_ = 0;

        // Checking if we need to reset accumulated length.
        if (reset_accum_len)
            accum_len_bytes_ = 0;
    }

    // Get buffer length.
    ULONG get_buf_len_bytes()
    {
        return buf_len_bytes_;
    }

    // Getting desired accumulating bytes.
    ULONG get_desired_accum_bytes()
    {
        return desired_accum_bytes_;
    }

    // Setting desired accumulating bytes.
    void set_desired_accum_bytes(ULONG value)
    {
        desired_accum_bytes_ = value;
    }

    // Setting number of accumulating bytes.
    void set_accum_len_bytes(ULONG value)
    {
        accum_len_bytes_ = value;
    }

    // Retrieves length of the buffer.
    ULONG buf_len_bytes()
    {
        return buf_len_bytes_;
    }

    // Setting the data pointer for the next operation.
    void SetDataPointer(uint8_t *dataPointer)
    {
        cur_buf_ptr_ = dataPointer;
    }

    // Adds accumulated bytes.
    void AddAccumulatedBytes(int32_t numBytes)
    {
        accum_len_bytes_ += numBytes;
    }

    // Preparing socket buffer for the new communication.
    void ResetBufferForNewOperation()
    {
        cur_buf_ptr_ = orig_buf_ptr_;
        accum_len_bytes_ = 0;
        buf_len_bytes_ = orig_buf_len_bytes_;
    }

    // Prepare buffer to send outside.
    void PrepareForSend(uint8_t *data, ULONG num_bytes)
    {
        buf_len_bytes_ = num_bytes;
        cur_buf_ptr_ = data;
        accum_len_bytes_ = 0;
    }

    // Prepare socket to continue receiving.
    void ContinueReceive()
    {
        cur_buf_ptr_ += last_recv_bytes_;
        buf_len_bytes_ -= last_recv_bytes_;
        last_recv_bytes_ = 0;
    }

    // Getting original buffer length bytes.
    ULONG get_orig_buf_len_bytes()
    {
        return orig_buf_len_bytes_;
    }

    // Setting the number of bytes retrieved at last receive.
    void SetLastReceivedBytes(ULONG lenBytes)
    {
        last_recv_bytes_ = lenBytes;
    }

    // Adds the number of bytes retrieved at last receive.
    void AddLastReceivedBytes(ULONG lenBytes)
    {
        last_recv_bytes_ += lenBytes;
    }

    // Returns pointer to original data buffer.
    uint8_t* get_orig_buf_ptr()
    {
        return orig_buf_ptr_;
    }

    // Returns pointer to response data.
    uint8_t* ResponseDataStart()
    {
        return orig_buf_ptr_ + accum_len_bytes_;
    }

    // Returns the size in bytes of accumulated data.
    ULONG get_accum_len_bytes()
    {
        return accum_len_bytes_;
    }

    // Checks if accumulating buffer is filled.
    bool IsBufferFilled()
    {
        return (buf_len_bytes_ - last_recv_bytes_ == 0);
    }

    // Checking if all needed bytes are accumulated.
    bool IsAccumulationComplete()
    {
        return accum_len_bytes_ == desired_accum_bytes_;
    }
};

struct ScSessionStruct
{
    // Session random salt.
    session_salt_type session_salt_;

    // Unique session linear index.
    // Points to the element in sessions linear array.
    session_index_type session_index_;

    // Scheduler ID.
    uint32_t scheduler_id_;

    // Unique number coming from Apps.
    apps_unique_session_num_type apps_unique_session_num_;

    // Default constructor.
    ScSessionStruct()
    {
        Reset();
    }

    // Reset.
    void Reset()
    {
        session_salt_ = INVALID_SESSION_SALT;
        session_index_ = INVALID_SESSION_INDEX;
        scheduler_id_ = INVALID_SCHEDULER_ID;
        apps_unique_session_num_ = INVALID_APPS_UNIQUE_SESSION_NUMBER;
    }

    // Initializes.
    void Init(
        session_salt_type session_salt,
        session_index_type session_index,
        apps_unique_session_num_type apps_unique_session_num,
        uint32_t scheduler_id)
    {
        session_salt_ = session_salt;
        session_index_ = session_index;
        scheduler_id_ = scheduler_id;
        apps_unique_session_num_ = apps_unique_session_num;
    }

    // Comparing two sessions.
    bool IsEqual(
        session_salt_type session_salt,
        session_index_type session_index)
    {
        return (session_salt_ == session_salt) && (session_index_ == session_index);
    }

    // Comparing two sessions.
    bool IsEqual(ScSessionStruct* session_struct)
    {
        return IsEqual(session_struct->session_salt_, session_struct->session_index_);
    }

    // Converts session to string.
    int32_t ConvertToString(char *str_out)
    {
        // Translating session index.
        int32_t sessionStringLen = uint64_to_hex_string(session_index_, str_out, 8, false);

        // Translating session random salt.
        sessionStringLen += uint64_to_hex_string(session_salt_, str_out + sessionStringLen, 16, false);

        return sessionStringLen;
    }

    // Copying one session structure into another.
    void Copy(ScSessionStruct* session_struct)
    {
        session_salt_ = session_struct->session_salt_;
        session_index_ = session_struct->session_index_;
        scheduler_id_ = session_struct->scheduler_id_;
        apps_unique_session_num_ = session_struct->apps_unique_session_num_;
    }

    // Scheduler ID.
    uint32_t get_scheduler_id()
    {
        return scheduler_id_;
    }

    // Getting session unique salt.
    session_salt_type get_session_salt()
    {
        return session_salt_;
    }

    // Unique session linear index.
    // Points to the element in sessions linear array.
    session_index_type get_session_index()
    {
        return session_index_;
    }

    // Create new session based on random salt, linear index, scheduler.
    void GenerateNewSession(
        session_salt_type session_salt,
        session_index_type session_index,
        apps_unique_session_num_type apps_unique_session_num,
        uint32_t scheduler_id);

    // Compare socket stamps of two sessions.
    bool CompareSalts(session_salt_type session_salt)
    {
        return session_salt_ == session_salt;
    }

    // Compare two sessions.
    bool Compare(session_salt_type session_salt, session_index_type sessionIndex)
    {
        return IsEqual(session_salt, sessionIndex);
    }

    // Compare two sessions.
    bool Compare(ScSessionStruct *session_struct)
    {
        return IsEqual(session_struct);
    }
};

class SocketData
{
    // Index into existing session or INVALID_SESSION_INDEX.
    session_index_type session_index_;

    // Unique socket stamp.
    uint64_t socket_stamp_;

public:

    // Gets sessions index.
    session_index_type get_session_index()
    {
        return session_index_;
    }

    // Sets session index.
    void set_session_index(session_index_type session_index)
    {
        session_index_ = session_index;
    }

    // Gets socket stamp.
    uint64_t get_socket_stamp()
    {
        return socket_stamp_;
    }

    // Reset.
    void Reset()
    {
        session_index_ = INVALID_SESSION_INDEX;
        socket_stamp_ = 0;
    }
};

// Represents an active database.
uint32_t __stdcall DatabaseChannelsEventsMonitorRoutine(LPVOID params);
class HandlersTable;
class RegisteredUris;
class ActiveDatabase
{
    // Index of this database in global namespace.
    int32_t db_index_;

    // Original database name.
    std::string db_name_;

    // Unique sequence number.
    volatile uint64_t unique_num_;

    // Open socket handles.
    bool active_sockets_[MAX_SOCKET_HANDLE];

    // Number of used sockets.
    volatile int64_t num_used_sockets_;

    // Number of used chunks.
    volatile int64_t num_used_chunks_;

    // Indicates if closure was performed.
    bool were_sockets_closed_;

    // Database handlers.
    HandlersTable* user_handlers_;

    // Number of confirmed register push channels.
    int32_t num_confirmed_push_channels_;

    // Channels events monitor thread handle.
    HANDLE channels_events_thread_handle_;

    // Apps session numbers.
    apps_unique_session_num_type* apps_sessions_;

public:

    // Sets value for Apps specific session.
    void SetAppsSessionValue(
        session_index_type session_index,
        apps_unique_session_num_type apps_session_value)
    {
        apps_sessions_[session_index] = apps_session_value;
    }

    // Gets value for Apps specific session.
    apps_unique_session_num_type GetAppsSessionValue(session_index_type session_index)
    {
        return apps_sessions_[session_index];
    }

    // Spawns channels events monitor thread.
    uint32_t SpawnChannelsEventsMonitor()
    {
        uint32_t channelsEventsThreadId;

        // Starting channels events monitor thread.
        channels_events_thread_handle_ = CreateThread(
            NULL, // Default security attributes.
            0, // Use default stack size.
            (LPTHREAD_START_ROUTINE)DatabaseChannelsEventsMonitorRoutine, // Thread function name.
            &db_index_, // Argument to thread function.
            0, // Use default creation flags.
            (LPDWORD)&channelsEventsThreadId); // Returns the thread identifier.

        return (NULL == channels_events_thread_handle_);
    }

    // Kill the channels events monitor.
    void KillChannelsEventsMonitor()
    {
        TerminateThread(channels_events_thread_handle_, 0);
        channels_events_thread_handle_ = NULL;
    }

    // Checks if its enough confirmed push channels.
    bool IsAllPushChannelsConfirmed();

    // Received confirmation push channel.
    void ReceivedPushChannelConfirmation()
    {
        num_confirmed_push_channels_++;
    }

    // Gets database name.
    std::string& get_db_name()
    {
        return db_name_;
    }

    // Gets database handlers.
    HandlersTable* get_user_handlers()
    {
        return user_handlers_;
    }

    // Getting the number of used chunks.
    int64_t num_used_chunks()
    {
        return num_used_chunks_;
    }

    // Getting the number of used sockets.
    int64_t num_used_sockets()
    {
        return num_used_sockets_;
    }

    // Increments or decrements the number of active chunks.
    void ChangeNumUsedChunks(int64_t change_value)
    {
        InterlockedAdd64(&num_used_chunks_, change_value);
    }

    // Closes all tracked sockets.
    void CloseSocketData();

    // Tracks certain socket.
    uint32_t TrackSocket(SOCKET s)
    {
        if (active_sockets_[s])
            return 1;

        InterlockedAdd64(&num_used_sockets_, 1);
        active_sockets_[s] = true;
        return 0;
    }

    // Untracks certain socket.
    uint32_t UntrackSocket(SOCKET s)
    {
        if (!active_sockets_[s])
            return 1;

        InterlockedAdd64(&num_used_sockets_, -1);
        active_sockets_[s] = false;
        return 0;
    }

    // Makes this database slot empty.
    void StartDeletion();

    // Checks if this database slot empty.
    bool IsEmpty()
    {
        return ((0 == unique_num_) && (0 == num_used_chunks_) && (0 == num_used_sockets_));
    }

    // Checks if this database slot emptying was started.
    bool IsDeletionStarted()
    {
        return (0 == unique_num_);
    }

    // Active database constructor.
    ActiveDatabase();

    // Destructor.
    ~ActiveDatabase();

    // Gets the name of the database.
    std::string db_name()
    {
        return db_name_;
    }

    // Returns unique number for this database.
    uint64_t unique_num()
    {
        return unique_num_;
    }

    // Initializes this active database slot.
    void Init(std::string new_name, uint64_t new_unique_num, int32_t db_index);
};


// Represents an active server port.
class HandlersList;
class SocketDataChunk;
class PortHandlers;
class RegisteredUris;
class RegisteredSubports;
class ServerPort
{
    // Socket.
    SOCKET listening_sock_;

    // Port number, e.g. 80, 443.
    uint16_t port_number_;

    // Statistics.
    volatile int64_t num_allocated_sockets_[MAX_ACTIVE_DATABASES];
    volatile int64_t num_active_conns_[MAX_ACTIVE_DATABASES];

    // Offset for the user data to be written.
    int32_t blob_user_data_offset_;

    // Ports handler lists.
    PortHandlers* port_handlers_;

    // All registered URIs belonging to this port.
    RegisteredUris* registered_uris_;

    // All registered subports belonging to this port.
    // TODO: Fix full support!
    RegisteredSubports* registered_subports_;

public:

    // Printing the registered URIs.
    void Print();

    // Getting registered URIs.
    RegisteredUris* get_registered_uris()
    {
        return registered_uris_;
    }

    // Getting registered port handlers.
    PortHandlers* get_port_handlers()
    {
        return port_handlers_;
    }

    // Getting registered subports.
    RegisteredSubports* get_registered_subports()
    {
        return registered_subports_;
    }

    // Getting user data offset in blob.
    int32_t get_blob_user_data_offset()
    {
        return blob_user_data_offset_;
    }

    // Removes this port.
    void EraseDb(int32_t db_index);

    // Removes this port.
    void Erase();

    // Checks if this database slot empty.
    bool EmptyForDb(int32_t db_index)
    {
        return (num_allocated_sockets_[db_index] == 0) && (num_active_conns_[db_index] == 0);
    }

    // Checking if port is unused by any database.
    bool IsEmpty();

    // Initializes server socket.
    void Init(uint16_t port_number, SOCKET port_socket, int32_t blob_user_data_offset);

    // Server port.
    ServerPort();

    // Server port.
    ~ServerPort();

    // Getting port socket.
    SOCKET get_port_socket()
    {
        return listening_sock_;
    }

    // Getting port number.
    uint16_t get_port_number()
    {
        return port_number_;
    }

    // Retrieves the number of active connections.
    int64_t get_num_active_conns(int32_t db_index)
    {
        return num_active_conns_[db_index];
    }

    // Retrieves the number of allocated sockets.
    int64_t get_num_allocated_sockets(int32_t db_index)
    {
        return num_allocated_sockets_[db_index];
    }

    // Increments or decrements the number of created sockets.
    int64_t ChangeNumAllocatedSockets(int32_t db_index, int64_t changeValue)
    {
        InterlockedAdd64(&(num_allocated_sockets_[db_index]), changeValue);
        return num_allocated_sockets_[db_index];
    }

    // Increments or decrements the number of active connections.
    int64_t ChangeNumActiveConns(int32_t db_index, int64_t changeValue)
    {
        InterlockedAdd64(&(num_active_conns_[db_index]), changeValue);
        return num_active_conns_[db_index];
    }

    // Resets the number of created sockets and active connections.
    void Reset(int32_t db_index)
    {
        InterlockedAnd64(&(num_allocated_sockets_[db_index]), 0);
        InterlockedAnd64(&(num_active_conns_[db_index]), 0);
    }
};

class GatewayWorker;
class Gateway
{
    ////////////////////////
    // SETTINGS
    ////////////////////////

    // Maximum total number of sockets aka connections.
    int32_t setting_max_connections_;

    // Master node IP address.
    std::string setting_master_ip_;

    // Starcounter server type.
    std::string setting_sc_server_type_;

    // Indicates if this node is a master node.
    bool setting_is_master_;

    // Gateway log file name.
    std::wstring setting_log_file_dir_;
    std::wstring setting_log_file_path_;

    // Gateway config file name.
    std::wstring setting_config_file_path_;

    // Local network interfaces to bind on.
    std::vector<std::string> setting_local_interfaces_;

    // Number of worker threads.
    int32_t setting_num_workers_;

    ////////////////////////
    // ACTIVE DATABASES
    ////////////////////////

    // List of active databases.
    ActiveDatabase active_databases_[MAX_ACTIVE_DATABASES];

    // Indicates what databases went down.
    bool db_did_go_down_[MAX_ACTIVE_DATABASES];

    // Current number of database slots.
    int32_t num_dbs_slots_;

    // Unique database sequence number.
    uint64_t db_seq_num_;

    ////////////////////////
    // WORKERS
    ////////////////////////

    // All worker structures.
    GatewayWorker* gw_workers_;

    // Worker thread handles.
    HANDLE* worker_thread_handles_;

    // Active databases monitor thread handle.
    HANDLE db_monitor_thread_handle_;

    // Channels events monitor thread handle.
    HANDLE channels_events_thread_handle_;

    ////////////////////////
    // SESSIONS
    ////////////////////////

    // Unique session sequence number.
    uint64_t unique_socket_stamp_;

    // All sessions information.
    ScSessionStruct* all_sessions_unsafe_;

    // All sockets information.
    SocketData sockets_data_unsafe_[MAX_SOCKET_HANDLE];

    // Represents delete state for all sockets.
    bool deleted_sockets_[MAX_SOCKET_HANDLE];

    // Free session indexes.
    uint32_t *free_session_indexes_unsafe_;

    // Number of active sessions.
    volatile uint32_t num_active_sessions_unsafe_;

    // Round-robin global scheduler number.
    uint32_t global_scheduler_id_unsafe_;

    ////////////////////////
    // GLOBAL LOCKING
    ////////////////////////

    // Critical section for sessions manipulation.
    CRITICAL_SECTION cs_session_;

    // Critical section on global lock.
    CRITICAL_SECTION cs_global_lock_;

    // Global lock.
    volatile bool global_lock_;

    ////////////////////////
    // OTHER STUFF
    ////////////////////////

    // Gateway handlers.
    HandlersTable* gw_handlers_;

    // All server ports.
    ServerPort server_ports_[MAX_ACTIVE_SERVER_PORTS];

    // Number of used server ports.
    volatile int32_t num_server_ports_;

    // The socket address of the server.
    sockaddr_in* server_addr_;

    // Number of active schedulers.
    uint32_t num_schedulers_;

    // Black list with malicious IP-addresses.
    std::list<uint32_t> black_list_ips_unsafe_;

    // Global IOCP handle.
    HANDLE iocp_;

public:

    // Getting settings log file directory.
    std::wstring& get_setting_log_file_dir()
    {
        return setting_log_file_dir_;
    }

    // Getting maximum number of connections.
    int32_t setting_max_connections()
    {
        return setting_max_connections_;
    }

    // Getting specific worker information.
    GatewayWorker* get_worker(int32_t worker_id);

    // Getting specific worker handle.
    HANDLE get_worker_thread_handle(int32_t worker_id)
    {
        return worker_thread_handles_[worker_id];
    }

    // Returns certain socket data.
    SocketData* GetSocketData(SOCKET sock_number)
    {
        assert (sock_number < MAX_SOCKET_HANDLE);

        return sockets_data_unsafe_ + sock_number;
    }

    // Increments and gets unique socket stamp number.
    uint64_t ObtainUniqueSocketStamp()
    {
        return ++unique_socket_stamp_;
    }

    // Round-robin global scheduler number.
    uint32_t obtain_scheduler_id()
    {
        if (++global_scheduler_id_unsafe_ >= num_schedulers_)
            global_scheduler_id_unsafe_ = 0;

        return global_scheduler_id_unsafe_;
    }

    // Getting state of the socket.
    bool ShouldSocketBeDeleted(SOCKET sock)
    {
        return deleted_sockets_[sock];
    }

    // Deletes specific socket.
    void MarkDeleteSocket(SOCKET sock)
    {
        deleted_sockets_[sock] = true;
    }

    // Makes specific socket available.
    void MarkSocketAlive(SOCKET sock)
    {
        deleted_sockets_[sock] = false;
    }

    // Getting gateway handlers.
    HandlersTable* get_gw_handlers()
    {
        return gw_handlers_;
    }

    // Checks if certain server port exists.
    ServerPort* FindServerPort(uint16_t port_num)
    {
        for (int32_t i = 0; i < num_server_ports_; i++)
        {
            if (port_num == server_ports_[i].get_port_number())
                return server_ports_ + i;
        }

        return NULL;
    }

    // Checks if certain server port exists.
    int32_t FindServerPortIndex(uint16_t port_num)
    {
        for (int32_t i = 0; i < num_server_ports_; i++)
        {
            if (port_num == server_ports_[i].get_port_number())
                return i;
        }

        return -1;
    }

    // Adds new server port.
    int32_t AddServerPort(uint16_t port_num, SOCKET listening_sock, int32_t blob_user_data_offset)
    {
        // Looking for an empty server port slot.
        int32_t empty_slot = 0;
        for (empty_slot = 0; empty_slot < num_server_ports_; ++empty_slot)
        {
            if (server_ports_[empty_slot].IsEmpty())
                break;
        }

        // Initializing server port on this slot.
        server_ports_[empty_slot].Init(port_num, listening_sock, blob_user_data_offset);

        // Checking if it was the last slot.
        if (empty_slot >= num_server_ports_)
            num_server_ports_++;

        return empty_slot;
    }

    // Runs all port handlers.
    uint32_t RunAllHandlers();

    // Delete all handlers associated with given database.
    uint32_t DeletePortsForDb(int32_t db_index);

    // Get active server ports.
    ServerPort* get_server_port(int32_t port_index)
    {
        return server_ports_ + port_index;
    }

    // Get number of active server ports.
    int32_t get_num_server_ports()
    {
        return num_server_ports_;
    }

    // Gets server address.
    sockaddr_in* get_server_addr()
    {
        return server_addr_;
    }

    // Check and wait for global lock.
    void SuspendWorker(GatewayWorker* gw);

    // Enters global lock and waits for workers.
    void EnterGlobalLock()
    {
        // Checking if already locked.
        if (global_lock_)
        {
            while (global_lock_)
                Sleep(1);
        }

        // Entering the critical section.
        EnterCriticalSection(&cs_global_lock_);

        // Setting the global lock key.
        global_lock_ = true;

        // Waiting until all workers reach the safe point and freeze there.
        WaitAllWorkersSuspended();

        // Now we are sure that all workers are suspended.
    }

    // Waits for all workers to suspend.
    void WaitAllWorkersSuspended();

    // Waking up all workers.
    void WakeUpAllWorkers();

    // Releases global lock.
    void LeaveGlobalLock()
    {
        global_lock_ = false;

        // Leaving critical section.
        LeaveCriticalSection(&cs_global_lock_);
    }

    // Gets global lock value.
    bool global_lock()
    {
        return global_lock_;
    }

    // Sets global lock value.
    void set_global_lock(bool lock_value)
    {
        global_lock_ = lock_value;
    }

    // Returns active database on this slot index.
    ActiveDatabase* GetDatabase(int32_t db_index)
    {
        return active_databases_ + db_index;
    }

    // Get number of active databases.
    int32_t get_num_dbs_slots()
    {
        return num_dbs_slots_;
    }

    // Reading command line arguments.
    uint32_t ReadArguments(int argc, wchar_t* argv[]);

    // Master node IP address.
    std::string setting_master_ip()
    {
        return setting_master_ip_;
    }

    // Get number of active schedulers.
    uint32_t get_num_schedulers()
    {
        return num_schedulers_;
    }

    // Get number of workers.
    int32_t setting_num_workers()
    {
        return setting_num_workers_;
    }

    // Getting the total number of used chunks for all databases.
    int64_t GetTotalNumUsedChunks()
    {
        int64_t totalActiveChunks = 0;
        for (int32_t i = 0; i < get_num_dbs_slots(); i++)
        {
            if (!active_databases_[i].IsEmpty())
                totalActiveChunks += (active_databases_[i].num_used_chunks());
        }

        return totalActiveChunks;
    }

    // Getting the number of used sockets for all databases.
    int64_t GetTotalNumUsedSockets()
    {
        int64_t totalActiveSockets = 0;
        for (int32_t i = 0; i < get_num_dbs_slots(); i++)
        {
            if (!active_databases_[i].IsEmpty())
                totalActiveSockets += (active_databases_[i].num_used_sockets());
        }

        return totalActiveSockets;
    }

    // Get IOCP.
    HANDLE get_iocp()
    {
        return iocp_;
    }

    // Local network interfaces to bind on.
    std::vector<std::string> setting_local_interfaces()
    {
        return setting_local_interfaces_;
    }

    // Is master node?
    bool setting_is_master()
    {
        return setting_is_master_;
    }

    // Constructor.
    Gateway();

    // Load settings from XML.
    uint32_t LoadSettings(std::wstring configFilePath);

    // Assert some correct state parameters.
    uint32_t AssertCorrectState();

    // Initialize the network gateway.
    uint32_t Init();

    // Initializes shared memory.
    uint32_t InitSharedMemory(std::string setting_databaseName,
        core::shared_interface* sharedInt_readOnly);

    // Checking for database changes.
    uint32_t CheckDatabaseChanges(std::wstring active_dbs_file_path);

    // Print statistics.
    uint32_t GatewayStatisticsAndMonitoringRoutine();

    // Creates socket and binds it to server port.
    uint32_t CreateListeningSocketAndBindToPort(GatewayWorker *gw, uint16_t port_num, SOCKET& sock);

    // Creates new connections on all workers.
    uint32_t CreateNewConnectionsAllWorkers(int32_t howMany, uint16_t port_num, int32_t db_index);

    // Start workers.
    uint32_t StartWorkerAndManagementThreads(
        LPTHREAD_START_ROUTINE workerRoutine,
        LPTHREAD_START_ROUTINE scanDbsRoutine,
        LPTHREAD_START_ROUTINE channelsEventsMonitorRoutine);

    // Cleanup resources.
    uint32_t GlobalCleanup();

    // Main function to start network gateway.
    int32_t StartGateway();

    // Deletes existing session.
    uint32_t KillSession(session_index_type session_index)
    {
        assert(INVALID_SESSION_INDEX != session_index);

        // Entering the critical section.
        EnterCriticalSection(&cs_session_);

        // Number of active sessions should always be correct.
        assert(num_active_sessions_unsafe_ > 0);

        // Resetting the session cell.
        all_sessions_unsafe_[session_index].Reset();

        // Decrementing number of active sessions.
        num_active_sessions_unsafe_--;
        free_session_indexes_unsafe_[num_active_sessions_unsafe_] = session_index;

        // Leaving the critical section.
        LeaveCriticalSection(&cs_session_);

        return 0;
    }

    // Generates new global session.
    ScSessionStruct* GenerateNewSession(
        session_salt_type session_salt,
        apps_unique_session_num_type apps_unique_session_num,
        uint32_t scheduler_id)
    {
        // Checking that we have not reached maximum number of sessions.
        if (num_active_sessions_unsafe_ >= setting_max_connections_)
            return NULL;

        // Entering the critical section.
        EnterCriticalSection(&cs_session_);

        // Getting index of a free session data.
        uint32_t free_session_index = free_session_indexes_unsafe_[num_active_sessions_unsafe_];

        // Creating an instance of new unique session.
        all_sessions_unsafe_[free_session_index].GenerateNewSession(
            session_salt,
            free_session_index,
            apps_unique_session_num,
            scheduler_id);

        // Incrementing number of active sessions.
        num_active_sessions_unsafe_++;

        // Leaving the critical section.
        LeaveCriticalSection(&cs_session_);

        // Returning new critical section.
        return all_sessions_unsafe_ + free_session_index;
    }

    // Gets session data by index.
    ScSessionStruct* GetSessionData(session_index_type session_index)
    {
        // Checking validity of linear session index.
        if (INVALID_SESSION_INDEX == session_index)
            return NULL;

        // Fetching the session by index.
        return all_sessions_unsafe_ + session_index;
    }
};

extern Gateway g_gateway;

} // namespace network
} // namespace starcounter

#endif // GATEWAY_HPP
