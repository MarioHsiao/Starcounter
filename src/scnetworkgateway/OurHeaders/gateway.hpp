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

// Level0 includes.
#include "../../../../Level0/src/include/sccorelog.h"

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
typedef uint64_t session_timestamp_type;
typedef int64_t echo_id_type;
typedef uint64_t log_handle_type;

// Statistics macros.
#define GW_GLOBAL_STATISTICS
#define GW_COLLECT_SOCKET_STATISTICS
//#define GW_DETAILED_STATISTICS
//#define GW_ECHO_STATISTICS

// Diagnostics macros.
//#define GW_SOCKET_DIAG
//#define GW_HTTP_DIAG
//#define GW_WEBSOCKET_DIAG
#define GW_ERRORS_DIAG
#define GW_WARNINGS_DIAG
//#define GW_CHUNKS_DIAG
#define GW_DATABASES_DIAG
#define GW_SESSIONS_DIAG

// Enable to check for unique socket usage.
#define GW_SOCKET_ID_CHECK

#ifdef GW_DEV_DEBUG
#define GW_SC_BEGIN_FUNC
#define GW_SC_END_FUNC
#define GW_ASSERT assert
#define GW_ASSERT_DEBUG assert
#else
#define GW_SC_BEGIN_FUNC _SC_BEGIN_FUNC
#define GW_SC_END_FUNC _SC_END_FUNC
#define GW_ASSERT _SC_ASSERT
#define GW_ASSERT_DEBUG _SC_ASSERT_DEBUG
#endif

//#define GW_PONG_MODE
//#define GW_TESTING_MODE
//#define GW_LOOPED_TEST_MODE
//#define GW_PROFILER_ON
//#define GW_LIMITED_ECHO_TEST

// Checking that macro definitions are correct.
#ifdef GW_LOOPED_TEST_MODE
#define GW_TESTING_MODE
#endif

#ifndef GW_TESTING_MODE
// Enabling proxy if we are not in testing mode.
#define GW_PROXY_MODE
#endif

// TODO: Move error codes to errors XML!
#define SCERRGWINCORRECTHANDLER 12345
#define SCERRGWFAILEDWSARECV 12346
#define SCERRGWSOCKETCLOSEDBYPEER 12347
#define SCERRGWDATAFROMABANDONEDSOCKET 12348
#define SCERRGWFAILEDWSASEND 12349
#define SCERRGWDISCONNECTAFTERSENDFLAG 12350
#define SCERRGWWEBSOCKETOPCODECLOSE 12351
#define SCERRGWWEBSOCKETUNKNOWNOPCODE 12352
#define SCERRGWSOCKETISTRACKEDALREADY 12353
#define SCERRGWSOCKETISNOTACTIVE 12354
#define SCERRGWMAXPORTHANDLERS 12355
#define SCERRGWHANDLEREXISTS 12356
#define SCERRGWWRONGHANDLERTYPE 12357
#define SCERRGWHANDLERNOTFOUND 12358
#define SCERRGWPORTNOTHANDLED 12359
#define SCERRGWPUSHEDTOOVERFLOWPOOL 12360
#define SCERRGWSOCKETDATAWRONGDATABASE 12361
#define SCERRGWNONHTTPPROTOCOL 12362
#define SCERRGWCANTACQUIRECLIENTINTERFACE 12363
#define SCERRGWHTTPTOOMANYHEADERS 12364
#define SCERRGWCANTACQUIRECHANNEL 12365
#define SCERRGWHTTPWRONGSESSIONINDEXFORMAT 12366
#define SCERRGWHTTPWRONGSESSIONSALTFORMAT 12367
#define SCERRGWHTTPWRONGSESSION 12368
#define SCERRGWBMXCHUNKWRONGFORMAT 12369
#define SCERRGWHTTPNONWEBSOCKETSUPGRADE 12370
#define SCERRGWHTTPWRONGWEBSOCKETSVERSION 12371
#define SCERRGWHTTPINCORRECTDATA 12372
#define SCERRGWHTTPPROCESSFAILED 12373
#define SCERRGWWRONGBMXCHUNKTYPE 12374
#define SCERRGWWRONGARGS 12375
#define SCERRGWCANTCREATELOGDIR 12376
#define SCERRGWCANTLOADXMLSETTINGS 12377
#define SCERRGWFAILEDASSERTCORRECTSTATE 12378
#define SCERRGWPATHTOIPCMONITORDIR 12379
#define SCERRGWACTIVEDBLISTENPROBLEM 12380
#define SCERRGWHTTPSPROCESSFAILED 12381
#define SCERRGWWORKERISDEAD 12382
#define SCERRGWDATABASEMONITORISDEAD 12383
#define SCERRGWINCORRECTBYTESSEND 12384
#define SCERRGWSOCKETNOTCONNECTED 12385
#define SCERRGWFAILEDDISCONNECTEX 12386
#define SCERRGWCONNECTEXFAILED 12387
#define SCERRGWACCEPTEXFAILED 12388
#define SCERRGWUNKNOWNIOCPOPERATION 12389
#define SCERRGWWORKERROUTINEFAILED 12390
#define SCERRGWMAXHANDLERSREACHED 12391
#define SCERRGWWRONGHANDLERINSLOT 12392
#define SCERRGWPORTPROCESSFAILED 12393
#define SCERRGWCANTRELEASETOSHAREDPOOL 12394
#define SCERRGWFAILEDFINDNEXTCHANGENOTIFICATION 12395
#define SCERRGWWRONGMAXIDLESESSIONLIFETIME 12396
#define SCERRGWWRONGDATABASEINDEX 12397
#define SCERRJUSTRELEASEDSOCKETDATA 12398
#define SCERRGWCHANNELSEVENTSTHREADISDEAD 12399
#define SCERRGWSESSIONSCLEANUPTHREADISDEAD 12400
#define SCERRGWGATEWAYLOGGINGTHREADISDEAD 12401
#define SCERRGWSOMETHREADDIED 12402
#define SCERRGWOPERATIONONWRONGSOCKET 12403
#define SCERRGWTESTTIMEOUT 12404
#define SCERRGWTESTFAILED 12405
#define SCERRGWTESTFINISHED 12406

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
const int32_t MAX_FETCHED_OVLS = 1000;

// Maximum size of HTTP body.
const int32_t MAX_HTTP_BODY_SIZE = 1024 * 1024 * 32;

// Size of circular log buffer.
const int32_t GW_LOG_BUFFER_SIZE = 8192 * 32;

// Maximum number of proxied URIs.
const int32_t MAX_PROXIED_URIS = 32;

// Number of sockets to increase the accept roof.
const int32_t ACCEPT_ROOF_STEP_SIZE = 1;

// Offset of data blob in socket data.
const int32_t SOCKET_DATA_BLOB_OFFSET_BYTES = 704;

// Length of blob data in bytes.
const int32_t SOCKET_DATA_BLOB_SIZE_BYTES = bmx::MAX_DATA_BYTES_IN_CHUNK - bmx::BMX_HEADER_MAX_SIZE_BYTES - SOCKET_DATA_BLOB_OFFSET_BYTES;

// Size of OVERLAPPED structure.
const int32_t OVERLAPPED_SIZE = sizeof(OVERLAPPED);

// Bad database index.
const int32_t INVALID_DB_INDEX = -1;

// Bad worker index.
const int32_t INVALID_WORKER_INDEX = -1;

// Bad port index.
const int32_t INVALID_PORT_INDEX = -1;

// Bad port number.
const int32_t INVALID_PORT_NUMBER = 0;

// Bad URI index.
const int32_t INVALID_URI_INDEX = -1;

// Bad handler index.
const int32_t INVALID_HANDLER_INDEX = -1;

// Bad log handler.
const log_handle_type INVALID_LOG_HANDLE = 0;

// Bad chunk index.
const uint32_t INVALID_CHUNK_INDEX = shared_memory_chunk::LINK_TERMINATOR;

// Bad linear session index.
const session_index_type INVALID_SESSION_INDEX = ~0;

// Bad scheduler index.
const uint32_t INVALID_SCHEDULER_ID = ~0;

// Bad session salt.
const session_salt_type INVALID_SESSION_SALT = 0;

// Bad Apps session salt.
const session_salt_type INVALID_APPS_SESSION_SALT = 0;

// Invalid Apps unique number.
const uint64_t INVALID_APPS_UNIQUE_SESSION_NUMBER = ~(uint64_t)0;

// Bad unique database number.
const session_salt_type INVALID_UNIQUE_DB_NUMBER = 0;

// Maximum number of chunks to keep in private chunk pool
// until we release them to shared chunk pool.
const int32_t MAX_CHUNKS_IN_PRIVATE_POOL = 256;

// Number of predefined gateway port types
const int32_t NUM_PREDEFINED_PORT_TYPES = 5;

// Size of local/remove address structure.
const int32_t SOCKADDR_SIZE_EXT = sizeof(sockaddr_in) + 16;

// Maximum number of active databases.
const int32_t MAX_ACTIVE_DATABASES = 16;

// Maximum number of workers.
const int32_t MAX_WORKER_THREADS = 32;

// Maximum number of active server ports.
const int32_t MAX_ACTIVE_SERVER_PORTS = 32;

// Maximum port handle integer.
const int32_t MAX_SOCKET_HANDLE = 10000000;

// Maximum number of test echoes.
const int32_t MAX_TEST_ECHOES = 50000000;

// Number of seconds monitor thread sleeps between checks.
const int32_t GW_MONITOR_THREAD_TIMEOUT_SECONDS = 5;

// Maximum reusable connect sockets per worker.
const int32_t MAX_REUSABLE_CONNECT_SOCKETS_PER_WORKER = 10000;

// Maximum blacklisted IPs per worker.
const int32_t MAX_BLACK_LIST_IPS_PER_WORKER = 10000;

// Hard-coded gateway test port number on server.
const int32_t GATEWAY_TEST_PORT_NUMBER_SERVER = 123;

// Session life time multiplier.
const int32_t SESSION_LIFETIME_MULTIPLIER = 3;

// First port number used for binding.
const uint16_t FIRST_BIND_PORT_NUM = 1500;

// Maximum length of gateway statistics string.
const int32_t MAX_STATS_LENGTH = 8192;

// Gateway mode.
enum GatewayTestingMode
{
    // NOTE: HTTP echo requests should come first!
    // They serve as direct array indexes.
    MODE_GATEWAY_HTTP = 0,
    MODE_GATEWAY_SMC_HTTP = 1,
    MODE_GATEWAY_SMC_APPS_HTTP = 2,

    MODE_GATEWAY_RAW = 3,
    MODE_GATEWAY_SMC_RAW = 4,
    MODE_GATEWAY_SMC_APPS_RAW = 5,

    MODE_GATEWAY_UNKNOWN = 6
};

const int32_t NumGatewayModes = 7;
const char* const GatewayTestingModeStrings[NumGatewayModes] = 
{
    "MODE_GATEWAY_HTTP",
    "MODE_GATEWAY_SMC_HTTP",
    "MODE_GATEWAY_SMC_APPS_HTTP",

    "MODE_GATEWAY_RAW",
    "MODE_GATEWAY_SMC_RAW",
    "MODE_GATEWAY_SMC_APPS_RAW",

    "MODE_GATEWAY_UNKNOWN"
};

inline GatewayTestingMode GetGatewayTestingMode(std::string modeString)
{
    for (int32_t i = 0; i < NumGatewayModes; i++)
    {
        if (modeString == GatewayTestingModeStrings[i])
            return (GatewayTestingMode)i;
    }
    return GatewayTestingMode::MODE_GATEWAY_UNKNOWN;
}

struct TestHttpEchoRequest
{
    const char* const uri;
    int32_t uri_len;

    const char* const http_request_str;
    int32_t http_request_len;

    int32_t http_request_insert_point;
};

const int32_t kNumTestHttpEchoRequests = 3;

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

// Gateway program name.
const wchar_t* const GW_PROGRAM_NAME = L"scnetworkgateway";

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
    // Active connections statistics.
    RECEIVE_SOCKET_OPER,
    DISCONNECT_SOCKET_OPER,

    // Non-active connections.
    ACCEPT_SOCKET_OPER,
    SEND_SOCKET_OPER,
    CONNECT_SOCKET_OPER,
    UNKNOWN_SOCKET_OPER
};

class SocketDataChunk;

// Defining reference type for socket data chunk.
typedef SocketDataChunk*& SocketDataChunkRef;

class GatewayWorker;

typedef uint32_t (*GENERIC_HANDLER_CALLBACK) (
    GatewayWorker *gw,
    SocketDataChunkRef sd,
    BMX_HANDLER_TYPE handler_id,
    bool* is_handled);

#ifdef GW_LOOPED_TEST_MODE

// Looped processors.
typedef uint32_t (*ECHO_REQUEST_CREATOR) (char* buf, echo_id_type echo_id, uint32_t* num_request_bytes);
typedef uint32_t (*ECHO_RESPONSE_PROCESSOR) (char* buf, uint32_t buf_len, echo_id_type* echo_id);

extern uint32_t DefaultHttpEchoRequestCreator(char* buf, echo_id_type echo_id, uint32_t* num_request_bytes);
extern uint32_t DefaultHttpEchoResponseProcessor(char* buf, uint32_t buf_len, echo_id_type* echo_id);

extern uint32_t DefaultRawEchoRequestCreator(char* buf, echo_id_type echo_id, uint32_t* num_request_bytes);
extern uint32_t DefaultRawEchoResponseProcessor(char* buf, uint32_t buf_len, echo_id_type* echo_id);

#endif

uint32_t OuterPortProcessData(
    GatewayWorker *gw,
    SocketDataChunkRef sd,
    BMX_HANDLER_TYPE handler_id,
    bool* is_handled);

uint32_t AppsPortProcessData(
    GatewayWorker *gw,
    SocketDataChunkRef sd,
    BMX_HANDLER_TYPE handler_id,
    bool* is_handled);

#ifdef GW_TESTING_MODE

uint32_t GatewayPortProcessEcho(
    GatewayWorker *gw,
    SocketDataChunkRef sd,
    BMX_HANDLER_TYPE handler_id,
    bool* is_handled);

#endif

uint32_t OuterSubportProcessData(
    GatewayWorker *gw,
    SocketDataChunkRef sd,
    BMX_HANDLER_TYPE handler_id,
    bool* is_handled);

uint32_t AppsSubportProcessData(
    GatewayWorker *gw,
    SocketDataChunkRef sd,
    BMX_HANDLER_TYPE handler_id,
    bool* is_handled);

#ifdef GW_TESTING_MODE

uint32_t GatewaySubportProcessEcho(
    GatewayWorker *gw,
    SocketDataChunkRef sd,
    BMX_HANDLER_TYPE handler_id,
    bool* is_handled);

#endif

uint32_t OuterUriProcessData(
    GatewayWorker *gw,
    SocketDataChunkRef sd,
    BMX_HANDLER_TYPE handler_id,
    bool* is_handled);

uint32_t AppsUriProcessData(
    GatewayWorker *gw,
    SocketDataChunkRef sd,
    BMX_HANDLER_TYPE handler_id,
    bool* is_handled);

#ifdef GW_TESTING_MODE

uint32_t GatewayUriProcessEcho(
    GatewayWorker *gw,
    SocketDataChunkRef sd,
    BMX_HANDLER_TYPE handler_id,
    bool* is_handled);

#endif

#ifdef GW_GLOBAL_STATISTICS

// HTTP/WebSockets statistics for Gateway.
uint32_t GatewayStatisticsInfo(
    GatewayWorker *gw,
    SocketDataChunkRef sd,
    BMX_HANDLER_TYPE handler_id,
    bool* is_handled);

#endif

uint32_t GatewayUriProcessProxy(
    GatewayWorker *gw,
    SocketDataChunkRef sd,
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

template <class T, uint32_t MaxElems>
class LinearStack
{
    T elems_[MaxElems];
    uint32_t push_index_;
    uint32_t pop_index_;
    int32_t stripe_length_;

public:

    LinearStack()
    {
        Clear();
    }

    void Clear()
    {
        push_index_ = 0;
        pop_index_ = 0;
        stripe_length_ = 0;
    }

    void PushBack(T& new_elem)
    {
        elems_[push_index_] = new_elem;
        push_index_++;
        stripe_length_++;
        GW_ASSERT(stripe_length_ <= MaxElems);

        if (push_index_ == MaxElems)
            push_index_ = 0;
    }

    T& PopFront()
    {
        T& ret_value = elems_[pop_index_];
        pop_index_++;
        stripe_length_--;
        GW_ASSERT(stripe_length_ >= 0);

        if (pop_index_ == MaxElems)
            pop_index_ = 0;

        return ret_value;
    }

    int32_t get_num_entries()
    {
        return stripe_length_;
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
    void PrepareForSend(uint8_t *data, ULONG num_bytes_to_write)
    {
        buf_len_bytes_ = num_bytes_to_write;
        cur_buf_ptr_ = data;
        accum_len_bytes_ = 0;
    }

    // Prepare buffer to proxy outside.
    void PrepareForSend()
    {
        buf_len_bytes_ = accum_len_bytes_;
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

// Represents a session in terms of gateway/Apps.
struct ScSessionStruct
{
    // Session random salt.
    session_salt_type gw_session_salt_;

    // Unique session linear index.
    // Points to the element in sessions linear array.
    session_index_type gw_session_index_;

    // Scheduler ID.
    uint32_t scheduler_id_;

    // Unique number coming from Apps.
    apps_unique_session_num_type apps_unique_session_num_;

    // Apps unique session salt.
    session_salt_type apps_session_salt_;

    // Default constructor.
    ScSessionStruct()
    {
        Reset();
    }

    // Checks if session is valid.
    bool IsValid()
    {
        return /*(scheduler_id_ != INVALID_SCHEDULER_ID) &&*/ (gw_session_index_ != INVALID_SESSION_INDEX);
    }

    // Reset.
    void Reset()
    {
        gw_session_index_ = INVALID_SESSION_INDEX;
        gw_session_salt_ = INVALID_SESSION_SALT;
        scheduler_id_ = INVALID_SCHEDULER_ID;
        apps_unique_session_num_ = INVALID_APPS_UNIQUE_SESSION_NUMBER;
        apps_session_salt_ = INVALID_APPS_SESSION_SALT;
    }

    // Initializes.
    void Init(
        session_salt_type session_salt,
        session_index_type session_index,
        apps_unique_session_num_type apps_unique_session_num,
        session_salt_type apps_session_salt,
        uint32_t scheduler_id)
    {
        gw_session_salt_ = session_salt;
        gw_session_index_ = session_index;
        scheduler_id_ = scheduler_id;
        apps_unique_session_num_ = apps_unique_session_num;
        apps_session_salt_ = apps_session_salt;
    }

    // Comparing two sessions.
    bool IsEqual(
        session_salt_type session_salt,
        session_index_type session_index)
    {
        return (gw_session_salt_ == session_salt) && (gw_session_index_ == session_index);
    }

    // Comparing two sessions.
    bool IsEqual(ScSessionStruct* session_struct)
    {
        return IsEqual(session_struct->gw_session_salt_, session_struct->gw_session_index_);
    }

    // Converts session to string.
    int32_t ConvertToString(char *str_out)
    {
        // Translating session index.
        int32_t sessionStringLen = uint64_to_hex_string(gw_session_index_, str_out, 8, false);

        // Translating session random salt.
        sessionStringLen += uint64_to_hex_string(gw_session_salt_, str_out + sessionStringLen, 16, false);

        return sessionStringLen;
    }

    // Compare socket stamps of two sessions.
    bool CompareSalts(session_salt_type session_salt)
    {
        if (INVALID_SESSION_INDEX == gw_session_index_)
            return false;

        return gw_session_salt_ == session_salt;
    }
};

// Structure that wraps the session and contains some
// additional information like session time stamps.
_declspec(align(64)) struct ScSessionStructPlus
{
    // Main session structure.
    ScSessionStruct session_;

    // Session last activity timestamp.
    session_timestamp_type session_timestamp_;

    // Unique number for socket.
    session_salt_type unique_socket_id_;

    // Active socket flag.
    uint64_t active_socket_flag_;

    // Padding to cache size (64 bytes).
    uint64_t pad;
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
    volatile uint64_t unique_num_unsafe_;

    // Indicates if closure was performed.
    bool were_sockets_closed_;

    // Database handlers.
    HandlersTable* user_handlers_;

    // Number of confirmed register push channels.
    int32_t num_confirmed_push_channels_;

    // Channels events monitor thread handle.
    HANDLE channels_events_thread_handle_;

    // Apps unique session numbers.
    apps_unique_session_num_type* apps_unique_session_numbers_unsafe_;

    // Apps session salts.
    session_salt_type* apps_session_salts_unsafe_;

    // Indicates if database is ready to be deleted.
    bool is_empty_;

public:

    // Sets value for Apps specific session.
    void SetAppsSessionValue(
        session_index_type session_index,
        apps_unique_session_num_type apps_unique_session_num,
        session_salt_type apps_session_salt)
    {
        apps_unique_session_numbers_unsafe_[session_index] = apps_unique_session_num;
        apps_session_salts_unsafe_[session_index] = apps_session_salt;
    }

    // Gets unique number for Apps specific session.
    apps_unique_session_num_type GetAppsUniqueSessionNumber(session_index_type session_index)
    {
        return apps_unique_session_numbers_unsafe_[session_index];
    }

    // Gets session salt for Apps specific session.
    session_salt_type GetAppsSessionSalt(session_index_type session_index)
    {
        return apps_session_salts_unsafe_[session_index];
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

    // Closes all tracked sockets.
    void CloseSocketData();

    // Makes this database slot empty.
    void StartDeletion();

    // Checks if this database slot empty.
    bool IsEmpty();

    // Checks if this database slot emptying was started.
    bool IsDeletionStarted()
    {
        return (INVALID_UNIQUE_DB_NUMBER == unique_num_unsafe_);
    }

    // Active database constructor.
    ActiveDatabase();

    // Gets the name of the database.
    std::string db_name()
    {
        return db_name_;
    }

    // Returns unique number for this database.
    uint64_t unique_num()
    {
        return unique_num_unsafe_;
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
    volatile int64_t num_accepting_sockets_unsafe_;

#ifdef GW_COLLECT_SOCKET_STATISTICS
    volatile int64_t num_allocated_accept_sockets_unsafe_;
    volatile int64_t num_allocated_connect_sockets_unsafe_;
#endif

    // Offset for the user data to be written.
    int32_t blob_user_data_offset_;

    // Ports handler lists.
    PortHandlers* port_handlers_;

    // All registered URIs belonging to this port.
    RegisteredUris* registered_uris_;

    // All registered subports belonging to this port.
    // TODO: Fix full support!
    RegisteredSubports* registered_subports_;

    // This port index in global array.
    int32_t port_index_;

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

    // Resets the number of created sockets and active connections.
    void Reset();

    // Checking if port is unused by any database.
    bool IsEmpty();

    // Initializes server socket.
    void Init(int32_t port_index, uint16_t port_number, SOCKET port_socket, int32_t blob_user_data_offset);

    // Server port.
    ServerPort();

    // Server port.
    ~ServerPort();

    // Getting port listening socket.
    SOCKET get_listening_sock()
    {
        return listening_sock_;
    }

    // Getting port number.
    uint16_t get_port_number()
    {
        return port_number_;
    }

    // Retrieves the number of active connections.
    int64_t NumberOfActiveConnections();

    // Retrieves the number of accepting sockets.
    int64_t get_num_accepting_sockets()
    {
        return num_accepting_sockets_unsafe_;
    }

    // Increments or decrements the number of accepting sockets.
    int64_t ChangeNumAcceptingSockets(int64_t change_value)
    {
#ifdef GW_DETAILED_STATISTICS
        GW_COUT << "ChangeNumAcceptingSockets: " << change_value << " of " << num_accepting_sockets_unsafe_ << GW_ENDL;
#endif

        InterlockedAdd64(&num_accepting_sockets_unsafe_, change_value);
        return num_accepting_sockets_unsafe_;
    }

#ifdef GW_COLLECT_SOCKET_STATISTICS

    // Retrieves the number of allocated accept sockets.
    int64_t get_num_allocated_accept_sockets()
    {
        return num_allocated_accept_sockets_unsafe_;
    }

    // Increments or decrements the number of created accept sockets.
    int64_t ChangeNumAllocatedAcceptSockets(int64_t change_value)
    {
        InterlockedAdd64(&num_allocated_accept_sockets_unsafe_, change_value);
        return num_allocated_accept_sockets_unsafe_;
    }

    // Retrieves the number of allocated connect sockets.
    int64_t get_num_allocated_connect_sockets()
    {
        return num_allocated_connect_sockets_unsafe_;
    }

    // Increments or decrements the number of created connect sockets.
    int64_t ChangeNumAllocatedConnectSockets(int64_t change_value)
    {
        InterlockedAdd64(&num_allocated_connect_sockets_unsafe_, change_value);
        return num_allocated_connect_sockets_unsafe_;
    }

#endif
};

// Information about the reversed proxy.
struct ReverseProxyInfo
{
    // Uri that is being proxied.
    std::string uri_;
    int32_t uri_len_;

    // IP address of the destination server.
    std::string ip_;

    // Port on which proxied service sits on.
    uint16_t port_;

    // Proxied service address socket info.
    sockaddr_in addr_;
};

class GatewayLogWriter
{
    // Critical section for exclusive writes.
    CRITICAL_SECTION write_lock_;

    // Circular buffer for log entries.
    char log_buf_[GW_LOG_BUFFER_SIZE];

    // Current write position.
    int32_t log_write_pos_;

    // Current log read position.
    int32_t log_read_pos_;

    // Log file handle.
    HANDLE log_file_handle_;

    // Length of accumulated but not dumped data.
    int32_t accum_length_;

public:

    void Init(std::wstring& log_file_path);

    ~GatewayLogWriter()
    {
        DeleteCriticalSection(&write_lock_);

        CloseHandle(log_file_handle_);
    }

#ifdef GW_LOGGING_ON

    // Writes given string to log buffer.
    void WriteToLog(const char* text, int32_t text_len);

    // Dump accumulated logs in buffer to file.
    void DumpToLogFile();

#endif
};

class GatewayWorker;
class Gateway
{
    ////////////////////////
    // SETTINGS
    ////////////////////////

    // Maximum total number of sockets aka connections.
    int32_t setting_max_connections_;

    // Starcounter server type.
    std::string setting_sc_server_type_;

    // Gateway log file name.
    std::wstring setting_output_dir_;
    std::wstring setting_log_file_path_;

    // Gateway config file name.
    std::wstring setting_config_file_path_;

    // Number of worker threads.
    int32_t setting_num_workers_;

    // Gateway statistics port.
    uint16_t setting_gw_stats_port_;

    // Inactive session timeout in seconds.
    int32_t setting_inactive_session_timeout_seconds_;
    int32_t min_inactive_session_life_seconds_;

    // Local network interfaces to bind on.
    std::vector<std::string> setting_local_interfaces_;

#ifdef GW_TESTING_MODE

    // Master node IP address.
    std::string setting_master_ip_;

    // Indicates if this node is a master node.
    bool setting_is_master_;

    // Number of connections to make to master node.
    int32_t setting_num_connections_to_master_;

    // Number of connections to make to master node per worker.
    int32_t setting_num_connections_to_master_per_worker_;

    // Number of tracked echoes to master.
    int32_t setting_num_echoes_to_master_;

    // Gateway operational mode.
    GatewayTestingMode setting_mode_;

    // Maximum running time for tests.
    int32_t setting_max_test_time_seconds_;

    // Name of the statistics.
    std::string setting_stats_name_;

    int32_t cmd_setting_num_workers_;
    GatewayTestingMode cmd_setting_mode_;
    int32_t cmd_setting_num_connections_to_master_;
    int32_t cmd_setting_num_echoes_to_master_;
    int32_t cmd_setting_max_test_time_seconds_;
    std::string cmd_setting_stats_name_;

#endif

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

    // Dead sessions cleanup thread handle.
    HANDLE dead_sessions_cleanup_thread_handle_;

    // Gateway logging thread handle.
    HANDLE gateway_logging_thread_handle_;

    // All threads monitor thread handle.
    HANDLE all_threads_monitor_handle_;

    ////////////////////////
    // SESSIONS
    ////////////////////////

    // All sessions information.
    ScSessionStructPlus* all_sessions_unsafe_;

    // Free session indexes.
    session_index_type* free_session_indexes_unsafe_;

    // Number of active sessions.
    volatile int64_t num_active_sessions_unsafe_;

    // Global timer to keep track on old sessions.
    volatile session_timestamp_type global_timer_unsafe_;

    // Sessions to cleanup.
    session_index_type* sessions_to_cleanup_unsafe_;
    volatile int64_t num_sessions_to_cleanup_unsafe_;

    // Critical section for sessions cleanup.
    CRITICAL_SECTION cs_sessions_cleanup_;

    ////////////////////////
    // GLOBAL LOCKING
    ////////////////////////

    // Critical section for sessions manipulation.
    CRITICAL_SECTION cs_session_;

    // Critical section on global lock.
    CRITICAL_SECTION cs_global_lock_;

    // Global lock.
    volatile bool global_lock_unsafe_;

    ////////////////////////
    // OTHER STUFF
    ////////////////////////

    // Unique linear socket id.
    session_salt_type unique_socket_id_;

    // Handle to Starcounter log.
    log_handle_type sc_log_handle_;

    // Specific gateway log writer.
    GatewayLogWriter gw_log_writer_;

#ifdef GW_TESTING_MODE

    // Confirmed HTTP requests map.
    bool confirmed_echoes_shared_[MAX_TEST_ECHOES];

    // Starts measured test.
    bool started_measured_test_;

    // Time when test started.
    uint64_t test_begin_time_;

    // Number of confirmed echoes.
    volatile int64_t num_confirmed_echoes_unsafe_;

    // Current echo number.
    volatile int64_t current_echo_number_unsafe_;

    // Number of operations per second.
    int64_t num_gateway_ops_per_second_;

    // Number of measures.
    int64_t num_ops_measures_;

    // Predefined HTTP requests for tests.
    TestHttpEchoRequest* test_http_echo_requests_;

#ifdef GW_LOOPED_TEST_MODE

    // Looped echo processors.
    ECHO_RESPONSE_PROCESSOR looped_echo_response_processor_;

    ECHO_REQUEST_CREATOR looped_echo_request_creator_;

#endif

#endif

    // Gateway handlers.
    HandlersTable* gw_handlers_;

    // All server ports.
    ServerPort server_ports_[MAX_ACTIVE_SERVER_PORTS];

    // Number of used server ports.
    volatile int32_t num_server_ports_unsafe_;

    // Number of processed HTTP requests.
    volatile int64_t num_processed_http_requests_unsafe_;

    // The socket address of the server.
    sockaddr_in* server_addr_;

    // List of proxied servers.
    ReverseProxyInfo reverse_proxies_[MAX_PROXIED_URIS];
    int32_t num_reversed_proxies_;

    // Number of active schedulers.
    uint32_t num_schedulers_;

    // Black list with malicious IP-addresses.
    LinearList<uint32_t, MAX_BLACK_LIST_IPS_PER_WORKER> black_list_ips_unsafe_;

    // Global IOCP handle.
    HANDLE iocp_;

    // Last bound port number.
    volatile int64_t last_bind_port_num_unsafe_;

    // Last bound interface number.
    volatile int64_t last_bind_interface_num_unsafe_;

    // Current global statistics stream.
    std::stringstream global_statistics_stream_;
    char global_statistics_string_[MAX_STATS_LENGTH];

    // Critical section for statistics.
    CRITICAL_SECTION cs_statistics_;

public:

#ifdef GW_SOCKET_ID_CHECK
    // Checking if unique socket number is correct.
    bool CompareUniqueSocketId(SOCKET s, session_salt_type socket_id)
    {
        GW_ASSERT(s < setting_max_connections_);

        return (all_sessions_unsafe_[s].unique_socket_id_ == socket_id);
    }

    // Setting new unique socket number.
    session_salt_type SetUniqueSocketId(SOCKET s)
    {
        session_salt_type unique_id = get_unique_socket_id();

        all_sessions_unsafe_[s].unique_socket_id_ = unique_id;

#ifdef GW_SOCKET_DIAG
        GW_COUT << "New unique socket id " << s << ":" << unique_id << GW_ENDL;
#endif

        return unique_id;
    }

    // Getting unique socket number.
    session_salt_type GetUniqueSocketId(SOCKET s)
    {
        return all_sessions_unsafe_[s].unique_socket_id_;
    }
#endif

    // Unique linear socket id.
    session_salt_type get_unique_socket_id()
    {
        // NOTE: Doing simple increment here.
        return ++unique_socket_id_;
    }

    // Checks that all Gateway threads are alive.
    uint32_t GatewayMonitor();

    // Getting Gateway log writer.
    GatewayLogWriter* get_gw_log_writer()
    {
        return &gw_log_writer_;
    }

    // Full path to gateway log file.
    std::wstring& setting_log_file_path()
    {
        return setting_log_file_path_;
    }

    // Current global statistics value.
    const char* GetGlobalStatisticsString(int32_t* out_len);

    // Getting the number of used sockets.
    int64_t NumberUsedSocketsAllWorkersAndDatabases();

    // Getting the number of reusable connect sockets.
    int64_t NumberOfReusableConnectSockets();

    // Getting the number of used sockets per worker.
    int64_t NumberUsedSocketsPerWorker(int32_t worker_id);

    // Getting the number of used sockets per database.
    int64_t NumberUsedSocketsPerDatabase(int32_t db_index);

    // Last bind port number.
    int64_t get_last_bind_port_num()
    {
        return last_bind_port_num_unsafe_;
    }

    // Last bind interface number.
    int64_t get_last_bind_interface_num()
    {
        return last_bind_interface_num_unsafe_;
    }

    // Generating new port/interface.
    void GenerateNewBindPortInterfaceNumbers()
    {
        InterlockedAdd64(&last_bind_port_num_unsafe_, 1);

        if (last_bind_port_num_unsafe_ == (0xFFFF - setting_num_workers_))
        {
            // NOTE: The following is done only by one worker.

            // Resetting the port number.
            last_bind_port_num_unsafe_ = FIRST_BIND_PORT_NUM;

            // Checking how we should change the interface number.
            if (last_bind_interface_num_unsafe_ == (setting_local_interfaces_.size() - 1))
                last_bind_interface_num_unsafe_ = 0;
            else
                InterlockedAdd64(&last_bind_interface_num_unsafe_, 1);
        }
    }

    // Getting minimum session lifetime.
    int32_t get_min_inactive_session_life_seconds()
    {
        return min_inactive_session_life_seconds_;
    }

    // Returns instance of proxied server.
    ReverseProxyInfo* GetProxiedServerAddress(int32_t index)
    {
        return reverse_proxies_ + index;
    }

    // Returns instance of proxied server.
    ReverseProxyInfo* SearchProxiedServerAddress(char* uri)
    {
        for (int32_t i = 0; i < num_reversed_proxies_; i++)
        {
            int32_t k = 0;
            while (reverse_proxies_[i].uri_[k] == uri[k])
                k++;

            if (k >= reverse_proxies_[i].uri_len_)
                return reverse_proxies_ + i;
        }

        return NULL;
    }

    // Adds some URI handler: either Apps or Gateway.
    uint32_t AddUriHandler(
        GatewayWorker *gw,
        HandlersTable* handlers_table,
        uint16_t port,
        const char* uri,
        uint32_t uri_len_chars,
        bmx::HTTP_METHODS http_method,
        BMX_HANDLER_TYPE user_handler_id,
        int32_t db_index,
        GENERIC_HANDLER_CALLBACK handler_proc);

    // Adds some port handler: either Apps or Gateway.
    uint32_t AddPortHandler(
        GatewayWorker *gw,
        HandlersTable* handlers_table,
        uint16_t port,
        BMX_HANDLER_TYPE handler_id,
        int32_t db_index,
        GENERIC_HANDLER_CALLBACK handler_proc);

    // Adds some sub-port handler: either Apps or Gateway.
    uint32_t AddSubPortHandler(
        GatewayWorker *gw,
        HandlersTable* handlers_table,
        uint16_t port,
        bmx::BMX_SUBPORT_TYPE subport,
        BMX_HANDLER_TYPE handler_id,
        int32_t db_index,
        GENERIC_HANDLER_CALLBACK handler_proc);

#ifdef GW_TESTING_MODE

    // Number of connections to make to master node.
    int32_t setting_num_connections_to_master()
    {
        return setting_num_connections_to_master_;
    }

    // Number of connections to make to master node per worker.
    int32_t setting_num_connections_to_master_per_worker()
    {
        return setting_num_connections_to_master_per_worker_;
    }

    // Number of tracked echoes to master.
    int32_t setting_num_echoes_to_master()
    {
        return setting_num_echoes_to_master_;
    }

    // Registering confirmed HTTP echo.
    void ConfirmEcho(int64_t echo_num)
    {
        GW_ASSERT(echo_num < setting_num_echoes_to_master_);

        if (false != confirmed_echoes_shared_[echo_num])
        {
            GW_COUT << "Echo index occupied: " << echo_num << GW_ENDL;
            GW_ASSERT(false);
        }

        confirmed_echoes_shared_[echo_num] = true;
        //GW_COUT << "Confirmed: " << echo_num << GW_ENDL;

        InterlockedIncrement64(&num_confirmed_echoes_unsafe_);
    }

    // Getting number of confirmed echoes.
    int64_t get_num_confirmed_echoes()
    {
        return num_confirmed_echoes_unsafe_;
    }

    // Checks that echo responses are correct.
    bool CheckConfirmedEchoResponses(GatewayWorker* gw);

    // Starts measured test.
    bool get_started_measured_test()
    {
        return started_measured_test_;
    }

    // Starts measured test.
    void StartMeasuredTest()
    {
        num_confirmed_echoes_unsafe_ = 0;
        started_measured_test_ = true;
        test_begin_time_ = timeGetTime();
    }

    // Resetting echo tests.
    void ResetEchoTests()
    {
        num_confirmed_echoes_unsafe_ = 0;
        current_echo_number_unsafe_ = -1;
        started_measured_test_ = false;
        test_begin_time_ = 0;

        for (int32_t i = 0; i < setting_num_echoes_to_master_; i++)
            confirmed_echoes_shared_[i] = false;
    }

    // Incrementing and getting next echo number.
    int64_t GetNextEchoNumber()
    {
        return InterlockedIncrement64(&current_echo_number_unsafe_);
    }

    // Checks if all echoes have been sent already.
    bool AllEchoesSent()
    {
        return current_echo_number_unsafe_ >= (setting_num_echoes_to_master_ - 1);
    }

    // Gateway operational mode.
    GatewayTestingMode setting_mode()
    {
        return setting_mode_;
    }

    // Master node IP address.
    std::string setting_master_ip()
    {
        return setting_master_ip_;
    }

    // Is master node?
    bool setting_is_master()
    {
        return setting_is_master_;
    }

    // Getting average number of measured operations.
    int64_t GetAverageOpsPerSecond()
    {
        GW_ASSERT(0 != num_ops_measures_);

        int64_t aver_num_ops = num_gateway_ops_per_second_ / num_ops_measures_;

        num_gateway_ops_per_second_ = 0;
        num_ops_measures_ = 0;

        return aver_num_ops;
    }

    // Calculates number of created connections for all workers.
    int64_t GetNumberOfCreatedConnectionsAllWorkers();

    // Getting number of preparation network events.
    int64_t GetNumberOfPreparationNetworkEventsAllWorkers();

    void InitTestHttpEchoRequests();

    TestHttpEchoRequest GetTestHttpEchoRequest()
    {
        return test_http_echo_requests_[setting_mode_];
    }

    // Generates test HTTP request.
    uint32_t GenerateHttpRequest(char* buf, echo_id_type echo_id)
    {
        // Getting current test HTTP request type.
        TestHttpEchoRequest r = GetTestHttpEchoRequest();

        // Copying HTTP response.
        memcpy(buf, r.http_request_str, r.http_request_len);

        // Inserting number into HTTP ping request.
        uint64_to_hex_string(echo_id, buf + r.http_request_insert_point, 8, false);

        return r.http_request_len;
    }

#ifdef GW_LOOPED_TEST_MODE
    // Looped echo processors.
    ECHO_RESPONSE_PROCESSOR get_looped_echo_response_processor()
    {
        return looped_echo_response_processor_;
    }

    ECHO_REQUEST_CREATOR get_looped_echo_request_creator()
    {
        return looped_echo_request_creator_;
    }

#endif

#endif

#ifdef GW_COLLECT_SOCKET_STATISTICS

    // Increments number of processed HTTP requests.
    void IncrementNumProcessedHttpRequests()
    {
        InterlockedAdd64(&num_processed_http_requests_unsafe_, 1);
    }

#endif

    // Get number of processed HTTP requests.
    int64_t get_num_processed_http_requests()
    {
        return num_processed_http_requests_unsafe_;
    }

    // Number of active sessions.
    int64_t get_num_active_sessions_unsafe()
    {
        return num_active_sessions_unsafe_;
    }

    // Collects outdated sessions if any.
    uint32_t CollectInactiveSessions();

    // Gets number of sessions to cleanup.
    int64_t get_num_sessions_to_cleanup_unsafe()
    {
        return num_sessions_to_cleanup_unsafe_;
    }

    // Cleans up all collected inactive sessions.
    uint32_t CleanupInactiveSessions(GatewayWorker* gw);

    // Sets current global timer value on given session.
    void SetSessionTimeStamp(session_index_type session_index)
    {
        all_sessions_unsafe_[session_index].session_timestamp_ = global_timer_unsafe_;
    }

    // Steps global timer value.
    void step_global_timer_unsafe(int32_t value)
    {
        global_timer_unsafe_ += value;
    }

    // Getting settings log file directory.
    std::wstring& get_setting_output_dir()
    {
        return setting_output_dir_;
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

    // Getting state of the socket.
    bool ShouldSocketBeDeleted(SOCKET s)
    {
        GW_ASSERT(s < setting_max_connections_);

        return !all_sessions_unsafe_[s].active_socket_flag_;
    }

    // Deletes specific socket.
    void MarkSocketDelete(SOCKET s)
    {
        GW_ASSERT(s < setting_max_connections_);

        all_sessions_unsafe_[s].active_socket_flag_ = false;
    }

    // Makes specific socket available.
    void MarkSocketAlive(SOCKET s)
    {
        GW_ASSERT(s < setting_max_connections_);

        all_sessions_unsafe_[s].active_socket_flag_ = true;
    }

    // Getting gateway handlers.
    HandlersTable* get_gw_handlers()
    {
        return gw_handlers_;
    }

    // Checks if certain server port exists.
    ServerPort* FindServerPort(uint16_t port_num)
    {
        for (int32_t i = 0; i < num_server_ports_unsafe_; i++)
        {
            if (port_num == server_ports_[i].get_port_number())
                return server_ports_ + i;
        }

        return NULL;
    }

    // Checks if certain server port exists.
    int32_t FindServerPortIndex(uint16_t port_num)
    {
        for (int32_t i = 0; i < num_server_ports_unsafe_; i++)
        {
            if (port_num == server_ports_[i].get_port_number())
                return i;
        }

        return INVALID_PORT_INDEX;
    }

    // Adds new server port.
    int32_t AddServerPort(uint16_t port_num, SOCKET listening_sock, int32_t blob_user_data_offset)
    {
        // Looking for an empty server port slot.
        int32_t empty_slot = 0;
        for (empty_slot = 0; empty_slot < num_server_ports_unsafe_; ++empty_slot)
        {
            if (server_ports_[empty_slot].IsEmpty())
                break;
        }

        // Initializing server port on this slot.
        server_ports_[empty_slot].Init(empty_slot, port_num, listening_sock, blob_user_data_offset);

        // Checking if it was the last slot.
        if (empty_slot >= num_server_ports_unsafe_)
            num_server_ports_unsafe_++;

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
        return num_server_ports_unsafe_;
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
        if (global_lock_unsafe_)
        {
            while (global_lock_unsafe_)
                Sleep(1);
        }

        // Entering the critical section.
        EnterCriticalSection(&cs_global_lock_);

        // Setting the global lock key.
        global_lock_unsafe_ = true;

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
        global_lock_unsafe_ = false;

        // Leaving critical section.
        LeaveCriticalSection(&cs_global_lock_);
    }

    // Gets global lock value.
    bool global_lock()
    {
        return global_lock_unsafe_;
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
    uint32_t ProcessArgumentsAndInitLog(int argc, wchar_t* argv[]);

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
    int64_t NumberUsedChunksAllWorkersAndDatabases();

    // Getting the number of used sockets per database.
    int64_t NumberUsedChunksPerDatabase(int32_t db_index);

    // Getting the number of used sockets per worker.
    int64_t NumberUsedChunksPerWorker(int32_t worker_id);

    // Getting the number of active connections per port.
    int64_t NumberOfActiveConnectionsPerPort(int32_t port_index);

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
    uint32_t StatisticsAndMonitoringRoutine();

    // Safely shutdowns the gateway.
    void ShutdownGateway(GatewayWorker* gw, int32_t exit_code);

    // Creates socket and binds it to server port.
    uint32_t CreateListeningSocketAndBindToPort(GatewayWorker *gw, uint16_t port_num, SOCKET& sock);

    // Creates new connections on all workers.
    uint32_t CreateNewConnectionsAllWorkers(int32_t howMany, uint16_t port_num, int32_t db_index);

    // Start workers.
    uint32_t StartWorkerAndManagementThreads(
        LPTHREAD_START_ROUTINE workerRoutine,
        LPTHREAD_START_ROUTINE scanDbsRoutine,
        LPTHREAD_START_ROUTINE channelsEventsMonitorRoutine,
        LPTHREAD_START_ROUTINE oldSessionsCleanupRoutine,
        LPTHREAD_START_ROUTINE gatewayLoggingRoutine,
        LPTHREAD_START_ROUTINE threadsMonitorRoutine);

    // Cleanup resources.
    uint32_t GlobalCleanup();

    // Main function to start network gateway.
    int32_t StartGateway();

    // Destructor.
    ~Gateway();

    // Opens Starcounter log for writing.
    uint32_t OpenStarcounterLog();

    // Closes Starcounter log.
    void CloseStarcounterLog();

    // Write critical into log.
    void LogWriteCritical(const wchar_t* msg);

    // Deletes existing session.
    uint32_t KillSession(session_index_type session_index, bool* was_killed)
    {
        GW_ASSERT(INVALID_SESSION_INDEX != session_index);

        *was_killed = false;

        // Only killing the session is its valid.
        // NOTE: Using double-checked locking.
        ScSessionStructPlus* session_plus = all_sessions_unsafe_ + session_index;
        if (session_plus->session_.IsValid())
        {
            // Entering the critical section.
            EnterCriticalSection(&cs_session_);

            // Only killing the session is its valid.
            if (session_plus->session_.IsValid())
            {
                // Number of active sessions should always be correct.
                GW_ASSERT(num_active_sessions_unsafe_ > 0);

#ifdef GW_SESSIONS_DIAG
                GW_COUT << "Session being killed: " << session_plus->session_.gw_session_index_ << ":"
                    << session_plus->session_.gw_session_salt_ << GW_ENDL;
#endif

                // Resetting the session cell.
                session_plus->session_.Reset();

                // Setting the session time stamp to zero.
                session_plus->session_timestamp_ = 0;

                // Decrementing number of active sessions.
                num_active_sessions_unsafe_--;
                free_session_indexes_unsafe_[num_active_sessions_unsafe_] = session_index;

                // Indicating that session is killed.
                *was_killed = true;
            }

            // Leaving the critical section.
            LeaveCriticalSection(&cs_session_);
        }

        return 0;
    }

    // Generates new global session and returns its copy (or bad session if reached the limit).
    ScSessionStruct GenerateNewSessionAndReturnCopy(
        session_salt_type session_salt,
        apps_unique_session_num_type apps_unique_session_num,
        session_salt_type apps_session_salt,
        uint32_t scheduler_id)
    {
        // Checking that we have not reached maximum number of sessions.
        if (num_active_sessions_unsafe_ >= setting_max_connections_)
        {
#ifdef GW_SESSIONS_DIAG
            GW_COUT << "Exhausted sessions pool!" << GW_ENDL;
#endif

            return ScSessionStruct();
        }

        // Entering the critical section.
        EnterCriticalSection(&cs_session_);

        // Getting index of a free session data.
        uint32_t free_session_index = free_session_indexes_unsafe_[num_active_sessions_unsafe_];

        // Creating an instance of new unique session.
        all_sessions_unsafe_[free_session_index].session_.Init(
            session_salt,
            free_session_index,
            apps_unique_session_num,
            apps_session_salt,
            scheduler_id);

        // Setting new time stamp.
        SetSessionTimeStamp(free_session_index);

#ifdef GW_SESSIONS_DIAG
        GW_COUT << "New session generated: " << free_session_index << ":" << session_salt << GW_ENDL;
#endif

        // Incrementing number of active sessions.
        num_active_sessions_unsafe_++;

        // Leaving the critical section.
        LeaveCriticalSection(&cs_session_);

        // Returning new critical section.
        return all_sessions_unsafe_[free_session_index].session_;
    }

    // Gets session data by index.
    ScSessionStruct GetGlobalSessionDataCopy(session_index_type session_index)
    {
        // Checking validity of linear session index other wise return a wrong copy.
        if (INVALID_SESSION_INDEX == session_index)
            return ScSessionStruct();

        // Fetching the session by index.
        return all_sessions_unsafe_[session_index].session_;
    }

    // Gets scheduler id for specific session.
    uint32_t GetGlobalSessionSchedulerId(session_index_type session_index)
    {
        // Checking validity of linear session index other wise return a wrong copy.
        if (INVALID_SESSION_INDEX == session_index)
            return INVALID_SCHEDULER_ID;

        // Fetching the session by index.
        return all_sessions_unsafe_[session_index].session_.scheduler_id_;
    }

#ifdef GW_TESTING_MODE
    // Gracefully shutdowns all needed processes after test is finished.
    uint32_t ShutdownTest(GatewayWorker* gw, bool success);
#endif
};

// Globally accessed gateway object.
extern Gateway g_gateway;

} // namespace network
} // namespace starcounter

#endif // GATEWAY_HPP