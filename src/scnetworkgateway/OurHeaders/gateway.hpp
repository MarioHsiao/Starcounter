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
#include <sccoredbg.h>
#include <sccorelog.h>
#include <profiler.hpp>

// HTTP related stuff.
#include "../../HTTP/HttpParser/OurHeaders/http_request.hpp"

// BMX/Blast2 include.
#include "../Chunks/bmx/bmx.hpp"
#include "../Chunks/bmx/chunk_helper.h"

#include "../../Starcounter.Internal/Constants/MixedCodeConstants.cs"

// Internal includes.
#include "utilities.hpp"

//#pragma warning(pop)
//#pragma warning(pop)

namespace starcounter {
namespace network {

// Data types definitions.
typedef uint32_t channel_chunk;
typedef int64_t random_salt_type;
typedef uint32_t session_index_type;
typedef int32_t socket_index_type;
typedef uint8_t scheduler_id_type;
typedef uint64_t socket_timestamp_type;
typedef int64_t echo_id_type;
typedef uint64_t ip_info_type;
typedef int32_t uri_index_type;
typedef int8_t port_index_type;
typedef int8_t db_index_type;
typedef int8_t worker_id_type;
typedef int8_t chunk_store_type;
typedef uint32_t ws_group_id_type;

// Statistics macros.
//#define GW_DETAILED_STATISTICS
//#define GW_ECHO_STATISTICS

// Diagnostics macros.
//#define GW_SOCKET_DIAG
//#define GW_HTTP_DIAG
//#define GW_WEBSOCKET_DIAG
//#define GW_ERRORS_DIAG
//#define GW_WARNINGS_DIAG
//#define GW_CHUNKS_DIAG
#define GW_DATABASES_DIAG
//#define GW_SESSIONS_DIAG
//#define GW_IOCP_IMMEDIATE_COMPLETION
//#define WORKER_NO_SLEEP
//#define LEAST_USED_SCHEDULING
#define CASE_INSENSITIVE_URI_MATCHER
#define DISCONNECT_SOCKETS_WHEN_CODEHOST_DIES
//#define USE_OLD_IPC_MONITOR

#ifdef GW_DEV_DEBUG
#define GW_SC_BEGIN_FUNC
#define GW_SC_END_FUNC
#define GW_ASSERT assert
#define GW_ASSERT_DEBUG assert
#else
#define GW_SC_BEGIN_FUNC
#define GW_SC_END_FUNC
#define GW_ASSERT _SC_ASSERT
#define GW_ASSERT_DEBUG _SC_ASSERT_DEBUG
#endif

//#define GW_PONG_MODE

enum GatewayErrorCodes
{
    SCERRGWFAILEDWSARECV = 12346,
    SCERRGWSOCKETCLOSEDBYPEER,
    SCERRGWFAILEDACCEPTEX,
    SCERRGWFAILEDWSASEND,
    SCERRGWDISCONNECTAFTERSENDFLAG,
    SCERRGWDISCONNECTFLAG,
    SCERRGWWEBSOCKETUNKNOWNOPCODE,
    SCERRGWPORTNOTHANDLED,
    SCERRGWWRONGHTTPDATA,
    SCERRGWRECEIVEMORE,
    SCERRGWHTTPNONWEBSOCKETSUPGRADE,
    SCERRGWHTTPWRONGWEBSOCKETSVERSION,
    SCERRGWHTTPINCORRECTDATA,
    SCERRGWHTTPPROCESSFAILED,
    SCERRGWHTTPSPROCESSFAILED,
    SCERRGWCONNECTEXFAILED,
    SCERRGWOPERATIONONWRONGSOCKET,
    SCERRGWOPERATIONONWRONGSOCKETWHENPUSHING,
    SCERRGWFAILEDTOATTACHSOCKETTOIOCP,
    SCERRGWFAILEDTOLISTENONSOCKET,
    SCERRGWIPISNOTONWHITELIST,
    SCERRGWMAXCHUNKSIZEREACHED,
    SCERRGWMAXCHUNKSNUMBERREACHED,
    SCERRGWMAXDATASIZEREACHED,
    SCERRGWWEBSOCKETWRONGHANDSHAKEDATA,
    SCERRGWNULLCODEHOST,
    SCERRGWCANTOBTAINFREESOCKETINDEX,
    SCERRGWWRONGUDPFROMPORT,
    SCERRGWWRONGPORTINDEX,
    SCERRGWREGISTERERINGINCORRECTURI,
    SCERRGWINVALIDSESSIONVALUE
};

// Maximum number of ports the gateway operates with.
const int32_t MAX_PORTS_NUM = 32;

// Maximum number of chunks to pop at once.
const int32_t MAX_CHUNKS_TO_POP_AT_ONCE = 100;

// Maximum number of fetched OVLs at once.
const int32_t MAX_FETCHED_OVLS = 10;

// Maximum number of attempts to push overflow SDs.
const int32_t MAX_OVERFLOW_ATTEMPTS = 100;

// Size of circular log buffer.
const int32_t GW_LOG_BUFFER_SIZE = 8192 * 32;

// Maximum number of proxied URIs.
const int32_t MAX_PROXIED_URIS = 32;

// Maximum number of URI aliases.
const int32_t MAX_URI_ALIASES = 32;

// Maximum number of URI aliases string characters.
const int32_t MAX_URI_ALIAS_CHARS = 128;

// Number of sockets to increase the accept roof.
const int32_t ACCEPT_ROOF_STEP_SIZE = 1;

// Maximum number of cached URI matchers.
const int32_t MAX_CACHED_URI_MATCHERS = 32;

// Offset of data blob in socket data.
const int32_t SOCKET_DATA_OFFSET_BLOB = MixedCodeConstants::SOCKET_DATA_OFFSET_BLOB;

// Size of OVERLAPPED structure.
const int32_t OVERLAPPED_SIZE = sizeof(OVERLAPPED);

// Bad database index.
const db_index_type INVALID_DB_INDEX = -1;

// Bad worker index.
const worker_id_type INVALID_WORKER_INDEX = -1;

// Bad port index.
const port_index_type INVALID_PORT_INDEX = -1;

// Bad index.
const int32_t INVALID_CHUNK_STORE_INDEX = -1;

// Invalid socket index.
const socket_index_type INVALID_SOCKET_INDEX = -1;

// Bad port number.
const int32_t INVALID_PORT_NUMBER = 0;

// Bad URI index.
const uri_index_type INVALID_URI_INDEX = -1;

// Bad reverse proxy index.
const uri_index_type INVALID_RP_INDEX = -1;

// Invalid parameter index in user delegate.
const uint8_t INVALID_PARAMETER_INDEX = 255;

// Bad chunk index.
const core::chunk_index INVALID_CHUNK_INDEX = MixedCodeConstants::INVALID_CHUNK_INDEX;

// Bad linear session index.
const session_index_type INVALID_SESSION_INDEX = ~0;

// Bad scheduler index.
const scheduler_id_type INVALID_SCHEDULER_ID = 255;

// Bad session salt.
const random_salt_type INVALID_SESSION_SALT = 0;

// Bad Apps session salt.
const random_salt_type INVALID_APPS_SESSION_SALT = 0;

// Invalid Apps unique number.
const uint64_t INVALID_APPS_UNIQUE_SESSION_NUMBER = ~(uint64_t)0;

// Bad unique database number.
const random_salt_type INVALID_UNIQUE_DB_NUMBER = 0;

// Maximum number of chunks to keep in private chunk pool
// until we release them to shared chunk pool.
const int32_t MAX_CHUNKS_IN_PRIVATE_POOL = 128;
const int32_t MAX_CHUNKS_IN_PRIVATE_POOL_DOUBLE = MAX_CHUNKS_IN_PRIVATE_POOL * 2;

// Size of local/remove address structure.
const int32_t SOCKADDR_SIZE_EXT = sizeof(sockaddr_in) + 16;

// Maximum number of active databases.
const int32_t MAX_ACTIVE_DATABASES = 16;

// Maximum number of workers.
const int32_t MAX_WORKER_THREADS = 32;

// Maximum number of test echoes.
const int32_t MAX_TEST_ECHOES = 50000000;

// Number of seconds monitor thread sleeps between checks.
const int32_t GW_MONITOR_THREAD_TIMEOUT_SECONDS = 5;

// Maximum blacklisted IPs per worker.
const int32_t MAX_BLACK_LIST_IPS_PER_WORKER = 10000;

// Socket life time multiplier.
const int32_t SOCKET_LIFETIME_MULTIPLIER = 5;

// First port number used for binding.
const uint16_t FIRST_BIND_PORT_NUM = 1500;

// Size of the listening queue.
const int32_t LISTENING_SOCKET_QUEUE_SIZE = 256;

// Number of prepared UDP sockets.
const int32_t NUM_PREPARED_UDP_SOCKETS_PER_WORKER = 256;

// Number of times to spin when obtaining global lock.
const int32_t GLOBAL_LOCK_SPIN_TIMES = 50000000;

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

const int32_t NumGatewayChunkSizes = 7;
const int32_t DefaultGatewayChunkSizeType = 1;

const int32_t GatewayChunkSizes[NumGatewayChunkSizes] = {
    768,
    2 * 1024, // Default chunk size.
    8 * 1024,
    32 * 1024,
	64 * 1024,
    128 * 1024,
    2 * 1024 * 1024
};

const int32_t GatewayChunkStoresSizes[NumGatewayChunkSizes] = {
    100000,
    200000, // Default chunk size.
    50000,
    30000,
	20000,
    10000,
    100
};

// Maximum number of chunks for each worker.
const int32_t MAX_WORKER_CHUNKS = 
    100000 +
    200000 + // Default chunk size.
    50000 +
    30000 +
	20000 +
    10000 +
    100;

const int32_t GatewayChunkDataSizes[NumGatewayChunkSizes] = {
    GatewayChunkSizes[0] - SOCKET_DATA_OFFSET_BLOB,
    GatewayChunkSizes[1] - SOCKET_DATA_OFFSET_BLOB,
    GatewayChunkSizes[2] - SOCKET_DATA_OFFSET_BLOB,
    GatewayChunkSizes[3] - SOCKET_DATA_OFFSET_BLOB,
    GatewayChunkSizes[4] - SOCKET_DATA_OFFSET_BLOB,
    GatewayChunkSizes[5] - SOCKET_DATA_OFFSET_BLOB,
	GatewayChunkSizes[6] - SOCKET_DATA_OFFSET_BLOB
};

// Maximum size of socket data.
const int32_t MAX_SOCKET_DATA_SIZE = GatewayChunkDataSizes[NumGatewayChunkSizes - 1];

// Maximum size of UDP datagram.
const int32_t MAX_UDP_DATAGRAM_SIZE = GatewayChunkDataSizes[3];

inline chunk_store_type ObtainGatewayChunkType(int32_t data_size)
{
    for (int32_t i = 0; i < NumGatewayChunkSizes; i++) {

        if (data_size <= GatewayChunkDataSizes[i]) {
            return i;
        }
    }

    GW_ASSERT(false);
    return INVALID_CHUNK_STORE_INDEX;
}

const char* const kHttpOKResponse =
    "HTTP/1.1 200 OK\r\n"
    "Content-Type: text/html; charset=UTF-8\r\n"
    "Content-Length: 0\r\n"
    "\r\n";

const int32_t kHttpOKResponseLength = static_cast<int32_t> (strlen(kHttpOKResponse));

const char* const kHttpGetFinishSend =
	"GET /sc/finishsend/* HTTP/1.1\r\n\r\n";

const int32_t kHttpGetFinishSendPortOffset = static_cast<int32_t> (strstr(kHttpGetFinishSend, "*") - kHttpGetFinishSend);

const char* const kHttpDeleteStream =
	"DELETE /sc/stream/* HTTP/1.1\r\n\r\n";

const int32_t kHttpDeleteStreamPortOffset = static_cast<int32_t> (strstr(kHttpDeleteStream, "*") - kHttpDeleteStream);

struct AggregationStruct
{
    random_salt_type unique_socket_id_;
    int32_t size_bytes_;
    socket_index_type socket_info_index_;
    int32_t unique_aggr_index_;
    uint16_t port_number_;
    uint8_t msg_type_;
    uint8_t msg_flags_;
};

const int32_t AggregationStructSizeBytes = sizeof(AggregationStruct);

struct HttpTestInformation
{
    const char* const method_and_uri_info;
    int32_t method_and_uri_info_len;

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
const int32_t WS_MAX_FRAME_INFO_SIZE = 16;

// Error code type.
#define GW_ERR_CHECK(err_code) if (0 != err_code) return err_code

// Printing prefixes.
#define GW_PRINT_WORKER GW_COUT << "[" << (int32_t)worker_id_ << "]: "
#define GW_PRINT_WORKER_DB GW_COUT << "[" << (int32_t)worker_id_ << "][" << (int32_t)db_index_ << "]: "
#define GW_PRINT_GLOBAL GW_COUT << "Global: "

// Gateway program name.
const wchar_t* const GW_PROGRAM_NAME = L"scnetworkgateway";
const char* const GW_PROCESS_NAME = "networkgateway";
const wchar_t* const GW_SAMPLE_CONFIG_NAME = L"scnetworkgateway.sample.xml";

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
class HandlersList;

typedef uint32_t (*GENERIC_HANDLER_CALLBACK) (
    HandlersList* hl,
    GatewayWorker *gw,
    SocketDataChunkRef sd,
    BMX_HANDLER_TYPE handler_info,
    bool* is_handled);

uint32_t UdpPortProcessData(
    HandlersList* hl,
    GatewayWorker *gw,
    SocketDataChunkRef sd,
    BMX_HANDLER_TYPE handler_info,
    bool* is_handled);

uint32_t TcpPortProcessData(
    HandlersList* hl,
    GatewayWorker *gw,
    SocketDataChunkRef sd,
    BMX_HANDLER_TYPE handler_info,
    bool* is_handled);

uint32_t OuterUriProcessData(
    HandlersList* hl,
    GatewayWorker *gw,
    SocketDataChunkRef sd,
    BMX_HANDLER_TYPE handler_info,
    bool* is_handled);

uint32_t AppsUriProcessData(
    HandlersList* hl,
    GatewayWorker *gw,
    SocketDataChunkRef sd,
    BMX_HANDLER_TYPE handler_info,
    bool* is_handled);

// HTTP/WebSockets statistics for Gateway.
uint32_t GatewayStatisticsInfo(
    HandlersList* hl,
    GatewayWorker *gw,
    SocketDataChunkRef sd,
    BMX_HANDLER_TYPE handler_info,
    bool* is_handled);

uint32_t GatewayTestSample(
    HandlersList* hl,
    GatewayWorker *gw,
    SocketDataChunkRef sd,
    BMX_HANDLER_TYPE handler_info,
    bool* is_handled);

// Updates configuration for Gateway.
uint32_t GatewayUpdateConfiguration(
    HandlersList* hl,
    GatewayWorker *gw,
    SocketDataChunkRef sd, 
    BMX_HANDLER_TYPE handler_id, 
    bool* is_handled);

// Profilers statistics for Gateway.
uint32_t GatewayProfilersInfo(
    HandlersList* hl,
    GatewayWorker *gw,
    SocketDataChunkRef sd,
    BMX_HANDLER_TYPE handler_info,
    bool* is_handled);

// Aggregation on gateway.
uint32_t PortAggregator(
    HandlersList* hl,
    GatewayWorker *gw,
    SocketDataChunkRef sd,
    BMX_HANDLER_TYPE handler_info,
    bool* is_handled);

uint32_t GatewayUriProcessProxy(
    HandlersList* hl,
    GatewayWorker *gw,
    SocketDataChunkRef sd,
    BMX_HANDLER_TYPE handler_info,
    bool* is_handled);

// Waking up a thread using APC.
void WakeUpThreadUsingAPC(HANDLE thread_handle);

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

    int32_t get_num_entries()
    {
        return num_entries_;
    }

    T& operator[](uint32_t index)
    {
        return elems_[index];
    }

    T* GetElemPtr(uint32_t index)
    {
        return elems_ + index;
    }

    bool IsEmpty()
    {
        return (0 == num_entries_);
    }

    void Add(T& new_elem)
    {
        GW_ASSERT(num_entries_ < MaxElems - 1);

        elems_[num_entries_] = new_elem;
        num_entries_++;
    }

    uint32_t AddEmpty()
    {
        GW_ASSERT(num_entries_ < MaxElems - 1);

        num_entries_++;

        return num_entries_;
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

    bool RemoveEntry(T& elem)
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
};

template <class T, uint32_t MaxElems>
class LinearQueue
{
    T elems_[MaxElems];
    uint32_t push_index_;
    uint32_t pop_index_;
    int32_t stripe_length_;

public:

    LinearQueue()
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
        if (stripe_length_ > MaxElems)
            std::cout << "";

        if (push_index_ == MaxElems)
            push_index_ = 0;
    }

    T& PopBack()
    {
        push_index_--;
        T& ret_value = elems_[push_index_];
        stripe_length_--;
        GW_ASSERT(stripe_length_ >= 0);

        if (push_index_ < 0)
            push_index_ = 0;

        return ret_value;
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

// Represents a session in terms of gateway/Apps.
struct ScSessionStruct
{
    // Unique random number.
    random_salt_type random_salt_;

    // Session linear index.
    session_index_type linear_index_;

    // Scheduler id.
    scheduler_id_type scheduler_id_;

    // Worker id.
    worker_id_type gw_worker_id_;

    // Reset.
    void Reset()
    {
        // NOTE: We don't reset the scheduler id and worker id, since they are used for socket
        // sending data in order, even when session is not created!

        linear_index_ = INVALID_SESSION_INDEX;
        random_salt_ = INVALID_APPS_SESSION_SALT;
        scheduler_id_ = INVALID_SCHEDULER_ID;
        gw_worker_id_ = INVALID_WORKER_INDEX;
    }

    // Constructing session from string.
    uint32_t FillFromString(const char* str_in, uint32_t len_bytes)
    {
        GW_ASSERT(MixedCodeConstants::SESSION_STRING_LEN_CHARS == len_bytes);

        uint64_t tmp_random_salt = hex_string_to_uint64(str_in, 16);
        if (INVALID_CONVERTED_NUMBER == tmp_random_salt) {
            return SCERRGWINVALIDSESSIONVALUE;
        }

        uint64_t tmp_linear_index = hex_string_to_uint64(str_in + 16, 6);
        if (INVALID_CONVERTED_NUMBER == tmp_linear_index) {
            return SCERRGWINVALIDSESSIONVALUE;
        }

        uint64_t tmp_scheduler_id = hex_string_to_uint64(str_in + 22, 2);
        if (INVALID_CONVERTED_NUMBER == tmp_scheduler_id) {
            return SCERRGWINVALIDSESSIONVALUE;
        }
        
        random_salt_ = static_cast<random_salt_type>(tmp_random_salt);
        linear_index_ = static_cast<session_index_type>(tmp_linear_index);
        scheduler_id_ = static_cast<scheduler_id_type>(tmp_scheduler_id);

        return 0;
    }

    // Compare socket stamps of two sessions.
    bool CompareSalts(random_salt_type session_salt)
    {
        if (INVALID_SESSION_INDEX == linear_index_)
            return false;

        return random_salt_ == session_salt;
    }

    // Checks if session is active.
    bool IsActive()
    {
        return (INVALID_SESSION_INDEX != linear_index_);
    }
};

enum SOCKET_FLAGS
{
    SOCKET_FLAGS_AGGREGATED = 1,
    SOCKET_FLAGS_PROXY_CONNECT = 2,
    SOCKET_FLAGS_DISCONNECT_AFTER_SEND = 2 << 1,
    SOCKET_FLAGS_WS_CLOSE_ALREADY_SENT = 2 << 2,
	SOCKET_FLAGS_STREAMING_RESPONSE_BODY = 2 << 3,
	SOCKET_FLAGS_DISCONNECT_PUSHED_TO_CODEHOST = 2 << 4
};

// Structure that facilitates the socket.
_declspec(align(MEMORY_ALLOCATION_ALIGNMENT)) struct ScSocketInfoStruct
{
    // Main session structure attached to this socket.
    ScSessionStruct session_;

    //////////////////////////////
    //////// 64 bits data ////////
    //////////////////////////////

    // Socket last activity timestamp.
    socket_timestamp_type socket_timestamp_;

    // Unique number for socket.
    random_salt_type unique_socket_id_;

    // Unique number for aggregation socket.
    random_salt_type aggr_unique_socket_id_;

    // Socket number.
    SOCKET socket_;

    //////////////////////////////
    //////// 32 bits data ////////
    //////////////////////////////

    // Proxy socket identifier.
    socket_index_type proxy_socket_info_index_;

    // This socket info index.
    socket_index_type read_only_index_;

    // Aggregation socket index.
    socket_index_type aggr_socket_info_index_;

    // Number of bytes left for accumulation.
    uint32_t accum_data_bytes_left_;

    // WebSockets group id.
    ws_group_id_type ws_group_id_;

    //////////////////////////////
    //////// 16 bits data ////////
    //////////////////////////////

    // Port index.
    port_index_type port_index_;

    //////////////////////////////
    //////// 8 bits data /////////
    //////////////////////////////

    // Index to already determined database.
    db_index_type dest_db_index_;

    // Some flags on socket.
    uint8_t flags_;

    // Network protocol flag.
    uint8_t type_of_network_protocol_;

    // Disconnecting given socket handle.
    void DisconnectSocket() {

        GW_ASSERT(INVALID_SOCKET != socket_);

        shutdown(socket_, SD_BOTH);

        //DisconnectExFunc(socket_, NULL, 0, 0);

        closesocket(socket_);

        socket_ = INVALID_SOCKET;
    }

    bool get_socket_aggregated_flag()
    {
        return (flags_ & SOCKET_FLAGS::SOCKET_FLAGS_AGGREGATED) != 0;
    }

    void set_socket_aggregated_flag()
    {
        flags_ |= SOCKET_FLAGS::SOCKET_FLAGS_AGGREGATED;
    }

	db_index_type get_dest_db_index() {
		return dest_db_index_;
	}

	bool get_streaming_response_body_flag()
	{
		return (flags_ & SOCKET_FLAGS::SOCKET_FLAGS_STREAMING_RESPONSE_BODY) != 0;
	}

	void reset_streaming_response_body_flag()
	{
		flags_ &= ~SOCKET_FLAGS::SOCKET_FLAGS_STREAMING_RESPONSE_BODY;
	}

	void set_streaming_response_body_flag()
	{
		flags_ |= SOCKET_FLAGS::SOCKET_FLAGS_STREAMING_RESPONSE_BODY;
	}

    bool get_socket_proxy_connect_flag()
    {
        return (flags_ & SOCKET_FLAGS::SOCKET_FLAGS_PROXY_CONNECT) != 0;
    }

    void set_socket_proxy_connect_flag()
    {
        flags_ |= SOCKET_FLAGS::SOCKET_FLAGS_PROXY_CONNECT;
    }

    bool get_disconnect_after_send_flag()
    {
        return (flags_ & SOCKET_FLAGS::SOCKET_FLAGS_DISCONNECT_AFTER_SEND) != 0;
    }

    void set_disconnect_after_send_flag()
    {
        flags_ |= SOCKET_FLAGS::SOCKET_FLAGS_DISCONNECT_AFTER_SEND;
    }

	bool get_disconnect_pushed_to_codehost_flag()
	{
		return (flags_ & SOCKET_FLAGS::SOCKET_FLAGS_DISCONNECT_PUSHED_TO_CODEHOST) != 0;
	}

	void set_disconnect_pushed_to_codehost_flag()
	{
		flags_ |= SOCKET_FLAGS::SOCKET_FLAGS_DISCONNECT_PUSHED_TO_CODEHOST;
	}

    bool get_ws_close_already_sent_flag()
    {
        return (flags_ & SOCKET_FLAGS::SOCKET_FLAGS_WS_CLOSE_ALREADY_SENT) != 0;
    }

    // Indicating that we already have sent the WebSocket Close frame.
    void set_ws_close_already_sent_flag()
    {
        flags_ |= SOCKET_FLAGS::SOCKET_FLAGS_WS_CLOSE_ALREADY_SENT;
    }

    SOCKET get_socket() {
        return socket_;
    }

    bool IsInvalidSocket() {
        return INVALID_SOCKET == socket_;
    }

    ScSocketInfoStruct()
    {
        Reset();
    }

    void ResetTimestamp()
    {
        socket_timestamp_ = 0;
    }

    // Resets the session struct.
    void Reset()
    {
        unique_socket_id_ = INVALID_SESSION_SALT;
        session_.Reset();

        socket_ = INVALID_SOCKET;
        port_index_ = INVALID_PORT_INDEX;

        ResetTimestamp();
        type_of_network_protocol_ = MixedCodeConstants::NetworkProtocolType::PROTOCOL_HTTP1;
        flags_ = 0;
        dest_db_index_ = INVALID_DB_INDEX;
        proxy_socket_info_index_ = INVALID_SOCKET_INDEX;
        aggr_socket_info_index_ = INVALID_SOCKET_INDEX;
        ws_group_id_ = MixedCodeConstants::INVALID_WS_CHANNEL_ID;
    }

    bool IsReset() {
        return (INVALID_SESSION_SALT == unique_socket_id_) && 
            (INVALID_SCHEDULER_ID == session_.scheduler_id_);
    }
};

// Represents an active database.
uint32_t __stdcall DatabaseChannelsEventsMonitorRoutine(LPVOID params);
class RegisteredUris;
class ActiveDatabase
{
    // Index of this database in global namespace.
    db_index_type db_index_;

    // Original database name.
    std::string db_name_;

    // Shared memory segment name.
    std::string shm_seg_name_;

    // Unique sequence number.
    volatile uint64_t unique_num_unsafe_;

    // Channels events monitor thread handle.
    HANDLE channels_events_thread_handle_;

    // Indicates if database is ready to be deleted.
    volatile bool is_empty_;

    // Indicates if database is ready to be cleaned up.
    volatile bool is_ready_for_cleanup_;

    // Number of released workers.
	volatile uint32_t num_holding_workers_;

    // Critical section for database checks.
    CRITICAL_SECTION cs_db_checks_;

public:

    // Shared memory segment name.
    const std::string& get_shm_seg_name()
    {
        return shm_seg_name_;
    }

    // Printing the database information.
    void PrintInfo(std::stringstream& global_port_statistics_stream);

    // Releasing worker.
    void ReleaseHoldingWorker()
    {
		InterlockedDecrement(&num_holding_workers_);
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

    // Gets database name.
    const std::string& get_db_name()
    {
        return db_name_;
    }

    // Makes this database slot empty.
    void StartDeletion();

    // Makes this database slot empty.
    void Reset(bool hard_reset);

    // Checks if this database slot empty.
    bool IsEmpty();

    // Checks if this database is ready to be cleaned up by workers (no used chunks, etc).
    bool IsReadyForCleanup();

    // Checks if this database slot emptying was started.
    bool IsDeletionStarted()
    {
        return (INVALID_UNIQUE_DB_NUMBER == unique_num_unsafe_);
    }

    // Active database constructor.
    ActiveDatabase();

    // Initializes this active database slot.
    void Init(std::string db_name, uint64_t unique_num, db_index_type db_index);
};

class UriMatcherCacheEntry {

    // URI matcher function.
    MixedCodeConstants::MatchUriType gen_uri_matcher_func_;

    // Generated DLL handle.
    HMODULE gen_dll_handle_;

    // Cached URI list string.
    std::string uris_list_string_;

    // Number of cached URIs.
    int32_t num_uris_;

    // Established Clang engine.
    void* clang_engine_;

public:

    void** GetClangEngineAddress() {
        return &clang_engine_;
    }

    UriMatcherCacheEntry() {
        num_uris_ = 0;
        gen_dll_handle_ = NULL;
        gen_uri_matcher_func_ = NULL;
        clang_engine_ = NULL;
    }

    int32_t get_num_uris() {
        return num_uris_;
    }

    std::string get_uris_list_string() {
        return uris_list_string_;
    }

    MixedCodeConstants::MatchUriType get_uri_matcher_func() {
        GW_ASSERT(NULL != gen_uri_matcher_func_);
        return gen_uri_matcher_func_;
    }

    void Init(
        MixedCodeConstants::MatchUriType gen_uri_matcher_func,
        HMODULE gen_dll_handle,
        std::string uris_list_string,
        int32_t num_uris) {

            gen_uri_matcher_func_ = gen_uri_matcher_func;
            gen_dll_handle_ = gen_dll_handle;
            uris_list_string_ = uris_list_string;
            num_uris_ = num_uris;
    }

    void Destroy();
};

// Information about the alias URI.
struct UriAliasInfo
{
    char from_method_space_uri_space_[MAX_URI_ALIAS_CHARS];
    int32_t from_method_space_uri_space_len_;

    char to_method_space_uri_space_[MAX_URI_ALIAS_CHARS];
    int32_t to_method_space_uri_space_len_;

    char lower_to_method_space_uri_space_[MAX_URI_ALIAS_CHARS];

    char host_name_[MAX_URI_ALIAS_CHARS];
    int32_t host_name_len_;

    uint16_t port_;

    void Reset() {

        from_method_space_uri_space_len_ = 0;
        to_method_space_uri_space_len_ = 0;
        host_name_len_ = 0;

        port_ = INVALID_PORT_NUMBER;
    }

    void PrintInfo(std::stringstream& stats_stream)
    {
        stats_stream << "{";

        stats_stream << "\"From\":\"" << from_method_space_uri_space_ << "\",";
        stats_stream << "\"To\":\"" << to_method_space_uri_space_ << "\",";
        stats_stream << "\"Port\":" <<  port_;

        stats_stream << "}";
    }
};

// Represents an active server port.
class HandlersList;
class SocketDataChunk;
class RegisteredUris;
class PortWsGroups;
class RegisteredSubports;
class ServerPort
{
    // Socket.
    SOCKET listening_sock_;

    // Port number, e.g. 80, 443.
    uint16_t port_number_;

    // Statistics.
    volatile int64_t num_accepting_sockets_unsafe_;

    // Port handler.
	HandlersList* port_handler_;

    // All registered URIs belonging to this port.
    RegisteredUris* registered_uris_;

    // URI matcher cache.
    std::list<UriMatcherCacheEntry*> uri_matcher_cache_;

    // All registered WebSockets belonging to this port.
    PortWsGroups* registered_ws_groups_;

    // This port index in global array.
    port_index_type port_index_;

    // Is this an aggregation port.
    bool aggregating_flag_;

    // Is it a UDP port?
    bool is_udp_;

    // Number of active sockets for this server port.
    int32_t num_active_sockets_[MAX_WORKER_THREADS];

    // Indexes to prepared UDP sockets.
    LinearQueue<socket_index_type, NUM_PREPARED_UDP_SOCKETS_PER_WORKER> ready_udp_sockets_[MAX_WORKER_THREADS];

public:

    // Getting UDP socket info.
    ScSocketInfoStruct* GetUdpSocketInfo(worker_id_type worker_id);

    // Pushing existing UDP socket to ready queue.
    void PushToReadyUdpSockets(worker_id_type worker_id, socket_index_type udp_socket) {
        ready_udp_sockets_[worker_id].PushBack(udp_socket);
    }

    // Is it a UDP port?
    bool is_udp() {
        return is_udp_;
    }

    void RemoveOldestCacheEntry(UriMatcherCacheEntry* new_entry) {

        // Checking if cache contains too many entries.
        if (uri_matcher_cache_.size() >= MAX_CACHED_URI_MATCHERS) {

            UriMatcherCacheEntry* oldest_uri_matcher = uri_matcher_cache_.front();
            uri_matcher_cache_.pop_front();
            oldest_uri_matcher->Destroy();

            GwDeleteSingle(oldest_uri_matcher);
            oldest_uri_matcher = NULL;
        }

        // Adding new entry to cache.
        uri_matcher_cache_.push_back(new_entry);
    }

    UriMatcherCacheEntry* TryGetUriMatcherFromCache();

    port_index_type get_port_index() {
        return port_index_;
    }

    void AddToActiveSockets(worker_id_type worker_id) {

        InterlockedIncrement((uint32_t*)num_active_sockets_ + worker_id);
    }

    void RemoveFromActiveSockets(worker_id_type worker_id) {

        InterlockedDecrement((uint32_t*)num_active_sockets_ + worker_id);

        GW_ASSERT(num_active_sockets_[worker_id] >= 0);
    }

    int32_t GetNumberOfActiveSocketsForWorker(worker_id_type worker_id);

    int32_t GetNumberOfActiveSocketsAllWorkers();

    worker_id_type GetLeastBusyWorkerId();

    // Sets an aggregating port flag.
    void set_aggregating_flag()
    {
        aggregating_flag_ = true;
    }

    // Gets an aggregating port flag.
    bool get_aggregating_flag()
    {
        return aggregating_flag_;
    }

    // Printing the registered URIs.
    void PrintInfo(std::stringstream& global_port_statistics_stream);

    // Getting registered URIs.
    RegisteredUris* get_registered_uris()
    {
        return registered_uris_;
    }

    // Getting registered port WebSocket groups.
    PortWsGroups* get_registered_ws_groups()
    {
        return registered_ws_groups_;
    }

    // Getting registered port handlers.
    HandlersList* get_port_handlers()
    {
        return port_handler_;
    }

	void set_port_handlers(HandlersList* port_handler)
	{
		port_handler_ = port_handler;
	}

    // Removes this port.
    void EraseDb(db_index_type db_index);

    // Removes this port.
    void Erase();

    // Resets the number of created sockets and active connections.
    void Reset();

    // Checking if port is unused by any database.
    bool IsEmpty();

    // Initializes server socket.
    void Init(port_index_type port_index, uint16_t port_number, bool is_udp, SOCKET port_socket);

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
    int64_t NumberOfActiveSockets();

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
};

// Information about the reversed proxy.
struct ReverseProxyInfo
{
    // Uri that is being proxied.
    std::string matching_method_and_uri_;
    int32_t matching_method_and_uri_len_;

    // Host name.
    std::string matching_host_;
    int32_t matching_host_len_;

    // IP address of the destination server.
    std::string destination_ip_;

    // Port of the destination server.
    uint16_t destination_port_;

    // Source port which to used for redirection to proxied service.
    uint16_t sc_proxy_port_;

    // Proxied service address socket info.
    sockaddr_in destination_addr_;

    // Resetting the proxy info.
    void Reset() {

        matching_method_and_uri_ = std::string();
        matching_method_and_uri_len_ = 0;

        matching_host_ = std::string();
        matching_host_len_ = 0;

        destination_ip_ = std::string();

        destination_port_ = INVALID_PORT_NUMBER;

        sc_proxy_port_ = INVALID_PORT_NUMBER;

        destination_addr_ = sockaddr_in();
    }

    // Printing the proxy info.
    void PrintInfo(std::stringstream& stats_stream)
    {
        stats_stream << "{\"MatchingMethodAndUri\":\"" << matching_method_and_uri_ << "\",";
        stats_stream << "\"MatchingHost\":\"" << matching_host_ << "\",";
        stats_stream << "\"DestinationIP\":\"" <<  destination_ip_ << "\",";
        stats_stream << "\"DestinationPort\":" <<  destination_port_ << ",";
        stats_stream << "\"StarcounterProxyPort\":" <<  sc_proxy_port_;

        stats_stream << "}";
    }
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

    void Init(const std::wstring& log_file_path);

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

typedef uint32_t (*ClangCompileCodeAndGetFuntions) (
    void** const clang_engine,
    const bool accumulate_old_modules,
    const bool print_to_console,
    const bool do_optimizations,
    const char* const input_code_str,
    const char* const function_names_delimited,
    void* out_func_ptrs[],
    void** exec_module);

typedef void (*ClangDestroy) (void* clang_engine);

// Tries to set a SIO_LOOPBACK_FAST_PATH on a given TCP socket.
void SetLoopbackFastPathOnTcpSocket(SOCKET sock);

class CodegenUriMatcher;
class GatewayWorker;
class Gateway
{
    ////////////////////////
    // CONSTANTS
    ////////////////////////

    // The size of the array to hold the server name and active databases updated event name,
    // including terminating null. The format is:
    // "Local\<server_name>_ipc_monitor_active_databases_updated_event"
    static const std::size_t active_databases_updated_event_name_size = 256;

    ////////////////////////
    // SETTINGS
    ////////////////////////

    // Maximum total number of sockets aka connections per worker.
    socket_index_type setting_max_connections_per_worker_;

    // Maximum receive content length size in bytes.
    uint32_t setting_maximum_receive_content_length_;

    // Starcounter server type upper case.
    std::string setting_sc_server_type_upper_;

    // Gateway log file name.
    std::wstring setting_server_output_dir_;
    std::wstring setting_gateway_output_dir_;
    std::wstring setting_log_file_path_;
    std::wstring setting_sc_bin_dir_;

    // Gateway config file name.
    std::wstring setting_config_file_path_;

    // Number of worker threads.
    int32_t setting_num_workers_;

    // Internal system port used for communication between codehost and gateway.
    uint16_t setting_internal_system_port_;

    // Gateway aggregation port.
    uint16_t setting_aggregation_port_;

    // Inactive socket timeout in seconds.
    int32_t setting_inactive_socket_timeout_seconds_;
    int32_t min_inactive_socket_life_seconds_;

    // Local network interfaces to bind on.
    std::vector<std::string> setting_local_interfaces_;
    
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

    // Event to wait for active databases update.
    HANDLE active_databases_updates_event_;

#ifdef USE_OLD_IPC_MONITOR

    // Monitor shared interface.
    core::monitor_interface_ptr shm_monitor_interface_;

#endif

    // Gateway pid.
    core::pid_type gateway_pid_;

    // Gateway owner id.
    core::owner_id gateway_owner_id_;

#ifdef USE_OLD_IPC_MONITOR

    // Shared memory monitor interface name.
    std::string shm_monitor_int_name_;

#endif

    ////////////////////////
    // WORKERS
    ////////////////////////

    // All worker structures.
    GatewayWorker* gw_workers_;

    // Worker thread handles.
    HANDLE* worker_thread_handles_;

#ifdef USE_OLD_IPC_MONITOR

    // Active databases monitor thread handle.
    HANDLE db_monitor_thread_handle_;

#endif

    // Channels events monitor thread handle.
    HANDLE channels_events_thread_handle_;

    // Dead sockets cleanup thread handle.
    HANDLE dead_sockets_cleanup_thread_handle_;

    // Gateway logging thread handle.
    HANDLE gateway_logging_thread_handle_;

    // All threads monitor thread handle.
    HANDLE all_threads_monitor_handle_;

    ////////////////////////
    // SOCKETS INFOS
    ////////////////////////

    // Global timer to keep track on old connections.
    volatile socket_timestamp_type global_timer_unsafe_;

    ////////////////////////
    // GLOBAL LOCKING
    ////////////////////////

    // Critical section on global lock.
    CRITICAL_SECTION cs_global_lock_;

    // Global lock.
    volatile bool global_lock_unsafe_;

    ////////////////////////
    // OTHER STUFF
    ////////////////////////

    // Unique linear socket id.
    volatile random_salt_type unique_socket_id_;

    // Handle to Starcounter log.
    MixedCodeConstants::server_log_handle_type sc_log_handle_;

    // Specific gateway log writer.
    GatewayLogWriter gw_log_writer_;

    // All server ports.
    ServerPort server_ports_[MAX_PORTS_NUM];

    // Number of used server ports slots.
    volatile int32_t num_server_ports_slots_;

    // Number of processed HTTP requests.
    volatile int64_t num_processed_http_requests_unsafe_;

    // The socket address of the server.
    sockaddr_in* server_addr_;

    // List of proxied servers.
    ReverseProxyInfo reverse_proxies_[MAX_PROXIED_URIS];
    int32_t num_reversed_proxies_;

    // List of URI aliases.
    UriAliasInfo uri_aliases_[MAX_URI_ALIASES];
    int32_t num_uri_aliases_;

    // White list with allowed IP-addresses.
    LinearList<ip_info_type, MAX_BLACK_LIST_IPS_PER_WORKER> white_ips_list_;

    // Last bound port number.
    volatile int64_t last_bind_port_num_unsafe_;

    // Last bound interface number.
    volatile int64_t last_bind_interface_num_unsafe_;

    // Current global statistics stream.
    std::stringstream global_statistics_stream_;

    // Critical section for statistics.
    CRITICAL_SECTION cs_statistics_;

    // Codegen URI matcher.
    CodegenUriMatcher* codegen_uri_matcher_;

public:

    // Compares the input URI with existing aliases and returns one if matches.
    bool GetUriAliasIfAny(
        const uint16_t port,
        const char* input_uri,
        const int32_t input_uri_len,
        char** method_space_uri_space,
        char** lower_method_space_uri_space,
        int32_t* method_space_uri_space_len) {

        // Walking through every URI alias.
        for (int32_t i = 0; i < num_uri_aliases_; i++) {

            if (port == uri_aliases_[i].port_) {

                // Comparing to URI alias.
                if ((input_uri_len == uri_aliases_[i].from_method_space_uri_space_len_) &&
                    (0 == strncmp(input_uri, uri_aliases_[i].from_method_space_uri_space_, uri_aliases_[i].from_method_space_uri_space_len_))) {

                        if (uri_aliases_[i].host_name_len_ != 0) {

                        }

                        *method_space_uri_space_len = uri_aliases_[i].to_method_space_uri_space_len_;
                        *method_space_uri_space = uri_aliases_[i].to_method_space_uri_space_;
                        *lower_method_space_uri_space = uri_aliases_[i].lower_to_method_space_uri_space_;

                        return true;
                }
            }
        }

        return false;
    }

    ReverseProxyInfo* GetReverseProxyInfo(int32_t reverse_proxy_index) {
        GW_ASSERT(reverse_proxy_index >= 0);
        GW_ASSERT(reverse_proxy_index < num_reversed_proxies_);
        return reverse_proxies_ + reverse_proxy_index;
    }

    // Comparing given host header with registered reverse proxies hosts.
    int32_t GetHostHeaderIndexInReverseProxy(const char* const host_header_value, const size_t value_len, const uint16_t port) {

        // Checking each reverse proxy info.
        for (int32_t i = 0; i < num_reversed_proxies_; i++) {

            // Checking if port is also the same.
            if (reverse_proxies_[i].sc_proxy_port_ == port) {

                // Checking proxy host is exactly the same as host header value.
                if (0 == reverse_proxies_[i].matching_host_.compare(0, value_len, host_header_value, value_len)) {

                    return i;
                }
            }
        }

        return INVALID_RP_INDEX;
    }

    // Find certain URI entry.
    uri_index_type CheckIfGatewayHandler(const char* method_uri_space, const int32_t method_uri_space_len);

    int32_t setting_inactive_socket_timeout_seconds() {
       return setting_inactive_socket_timeout_seconds_;
    }

    socket_timestamp_type get_global_timer_unsafe()
    {
        return global_timer_unsafe_;
    }

    uint16_t get_setting_internal_system_port() {
        return setting_internal_system_port_;
    }

    int64_t num_pending_sends_;
    int64_t num_aggregated_sent_messages_;
    int64_t num_aggregated_recv_messages_;
    int64_t num_aggregated_send_queued_messages_;

    // Gets aggregation port number.
    uint16_t setting_aggregation_port()
    {
        return setting_aggregation_port_;
    }

    // Checks if IP is on white list.
    bool CheckIpForWhiteList(ip_info_type ip)
    {
        // Checking if white IPs list is empty.
        if (white_ips_list_.IsEmpty())
            return true;

        // TODO: Optimize check to be a switch statement.
        for (int32_t i = 0; i < white_ips_list_.get_num_entries(); i++)
        {
            if (ip == white_ips_list_[i])
                return true;
        }

        return false;
    }

    // Printing statistics for all ports.
    void PrintPortStatistics(std::stringstream& stats_stream);

    // Printing statistics for all reverse proxies.
    void PrintReverseProxiesStatistics(std::stringstream& stats_stream);

    // Printing statistics for all databases.
    void PrintDatabaseStatistics(std::stringstream& stats_stream);

    // Printing statistics for all workers.
    void PrintWorkersStatistics(std::stringstream& stats_stream);

    // Registering all gateway handlers.
    uint32_t RegisterGatewayHandlers();

    // Updating reverse proxies.
    uint32_t UpdateReverseProxies();

    // Handle to Starcounter log.
    MixedCodeConstants::server_log_handle_type get_sc_log_handle()
    {
        return sc_log_handle_;
    }

#ifdef USE_OLD_IPC_MONITOR

    // Constant reference to monitor interface.
    const core::monitor_interface_ptr& the_monitor_interface() const {
        return shm_monitor_interface_;
    }

#endif

    // Get a reference to the active_databases_updates_event_.
	HANDLE& active_databases_updates_event() {
		return active_databases_updates_event_;
	}

    // Pointer to Clang compile and get function pointer.
    ClangCompileCodeAndGetFuntions clangCompileCodeAndGetFuntions_;

    // Destroys existing Clang engine.
    ClangDestroy clangDestroyFunc_;

    // Generate the code using managed generator.
    uint32_t GenerateUriMatcher(ServerPort* sp, RegisteredUris* port_uris);

    // Codegen URI matcher.
    CodegenUriMatcher* get_codegen_uri_matcher()
    {
        return codegen_uri_matcher_;
    }
    
    // Unique linear socket id.
    random_salt_type get_unique_socket_id()
    {
		// Checking if we exceeded maximum allowed value.
		if (unique_socket_id_ >= MixedCodeConstants::MAX_UNIQUE_SOCKET_ID) {
			unique_socket_id_ = 0;
		}

		return InterlockedIncrement64(&unique_socket_id_);
    }

    // Checks that all Gateway threads are alive.
    uint32_t GatewayMonitor();

    // Getting Gateway log writer.
    GatewayLogWriter* get_gw_log_writer()
    {
        return &gw_log_writer_;
    }

    // Full path to gateway log file.
    const std::wstring& setting_log_file_path()
    {
        return setting_log_file_path_;
    }

    // Current gateway statistics.
    std::string GetGatewayStatisticsString();

    // Current global profilers stats.
    std::string GetGlobalProfilersString(int32_t* out_len);

    // Getting the number of used sockets per worker.
    int64_t NumberUsedSocketsPerWorker(int32_t worker_id);

    // Last bind port number.
    uint16_t get_last_bind_port_num()
    {
        return static_cast<uint16_t>(last_bind_port_num_unsafe_);
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

    // Getting minimum socket lifetime.
    int32_t get_min_inactive_socket_life_seconds()
    {
        return min_inactive_socket_life_seconds_;
    }

    // Returns instance of proxied server.
    ReverseProxyInfo* GetProxiedServerAddress(int32_t index)
    {
        return reverse_proxies_ + index;
    }

    // Adds some URI handler: either Apps or Gateway.
    uint32_t AddUriHandler(
        GatewayWorker *gw,
        uint16_t port,
        const char* app_name_string,
        const char* method_space_uri,
        uint8_t* param_types,
        int32_t num_params,
        BMX_HANDLER_TYPE user_handler_id,
        db_index_type db_index,
        GENERIC_HANDLER_CALLBACK handler_proc,
        bool is_gateway_handler = false,
        ReverseProxyInfo* reverse_proxy_info = NULL);

    // Adds some port handler: either Apps or Gateway.
    uint32_t AddPortHandler(
        GatewayWorker *gw,
        uint16_t port,
        bool is_udp,
        const char* app_name_string,
        BMX_HANDLER_TYPE handler_info,
        db_index_type db_index,
        GENERIC_HANDLER_CALLBACK handler_proc);

    // Increments number of processed HTTP requests.
    void IncrementNumProcessedHttpRequests()
    {
        InterlockedAdd64(&num_processed_http_requests_unsafe_, 1);
    }

    // Get number of processed HTTP requests.
    int64_t get_num_processed_http_requests()
    {
        return num_processed_http_requests_unsafe_;
    }

    // Steps global timer value.
    void step_global_timer_unsafe(int32_t value)
    {
        global_timer_unsafe_ += value;
    }

    // Gateway pid.
    core::pid_type get_gateway_pid()
    {
        return gateway_pid_;
    }

    // Gateway owner id.
    core::owner_id get_gateway_owner_id()
    {
        return gateway_owner_id_;
    }

#ifdef USE_OLD_IPC_MONITOR

    // Shared memory monitor interface name.
    const std::string& get_shm_monitor_int_name()
    {
        return shm_monitor_int_name_;
    }

#endif

    uint32_t setting_maximum_receive_content_length()
    {
        return setting_maximum_receive_content_length_;
    }

    // Getting settings log file directory.
    const std::wstring& get_setting_server_output_dir()
    {
        return setting_server_output_dir_;
    }

    // Getting gateway output directory.
    const std::wstring& get_setting_gateway_output_dir()
    {
        return setting_gateway_output_dir_;
    }

    // Getting Starcounter bin directory.
    const std::wstring& get_setting_sc_bin_dir()
    {
        return setting_sc_bin_dir_;
    }

    // Starcounter server type upper case.
    const std::string& setting_sc_server_type_upper()
    {
        return setting_sc_server_type_upper_;
    }

    // Getting maximum number of connections per worker.
    socket_index_type setting_max_connections_per_worker()
    {
        return setting_max_connections_per_worker_;
    }

    // Getting specific worker information.
    GatewayWorker* get_worker(worker_id_type worker_id);

    // Getting specific worker handle.
    HANDLE get_worker_thread_handle(worker_id_type worker_id)
    {
        return worker_thread_handles_[worker_id];
    }

    // Checks if certain server port exists.
    ServerPort* FindServerPort(uint16_t port_num)
    {
        for (int32_t i = 0; i < num_server_ports_slots_; i++)
        {
            if (port_num == server_ports_[i].get_port_number())
                return server_ports_ + i;
        }

        return NULL;
    }

    // Checks if certain server port exists.
    port_index_type FindServerPortIndex(uint16_t port_num)
    {
        for (port_index_type i = 0; i < num_server_ports_slots_; i++)
        {
            if (port_num == server_ports_[i].get_port_number())
                return i;
        }

        return INVALID_PORT_INDEX;
    }

    // Adds new server port.
    ServerPort* AddNewServerPort(uint16_t port_num, bool is_udp, SOCKET listening_sock)
    {
        // Looking for an empty server port slot.
        int32_t empty_slot = 0;
        for (empty_slot = 0; empty_slot < num_server_ports_slots_; ++empty_slot)
        {
            if (server_ports_[empty_slot].IsEmpty())
                break;
        }

        // Initializing server port on this slot.
        server_ports_[empty_slot].Init(empty_slot, port_num, is_udp, listening_sock);

        // Checking if it was the last slot.
        if (empty_slot >= num_server_ports_slots_)
            num_server_ports_slots_++;

        return server_ports_ + empty_slot;
    }

    // Delete all handlers associated with given database.
    uint32_t EraseDatabaseFromPorts(db_index_type db_index);

    // Cleans up empty ports.
    void CleanUpEmptyPorts();

    // Get active server ports.
    ServerPort* get_server_port(port_index_type port_index)
    {
        return server_ports_ + port_index;
    }

    // Retrieves the number of active sockets on all ports.
    int64_t NumberOfActiveSocketsOnAllPorts()
    {
        int64_t num_active_conns = 0;

        // Going through all ports.
        for (int32_t i = 0; i < num_server_ports_slots_; i++)
        {
            // Checking that port is not empty.
            if (!server_ports_[i].IsEmpty())
            {
                // Deleting port handlers if any.
                num_active_conns += server_ports_[i].GetNumberOfActiveSocketsAllWorkers();
            }
        }

        return num_active_conns;
    }

    // Retrieves the number of active connections.
    int64_t NumberOfActiveSocketsOnAllPortsForWorker(worker_id_type worker_id)
    {
        int64_t num_active_conns = 0;

        // Going through all ports.
        for (int32_t i = 0; i < num_server_ports_slots_; i++)
        {
            // Checking that port is not empty.
            if (!server_ports_[i].IsEmpty())
            {
                // Deleting port handlers if any.
                num_active_conns += server_ports_[i].GetNumberOfActiveSocketsForWorker(worker_id);
            }
        }

        return num_active_conns;
    }

    // Get number of active server ports.
    int32_t get_num_server_ports_slots()
    {
        return num_server_ports_slots_;
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
		int32_t max_tries = GLOBAL_LOCK_SPIN_TIMES * 2;
		while (global_lock_unsafe_) {
			max_tries--;
			if (0 == max_tries) {
				GW_ASSERT(!"Reached maximum number of tries to obtain a global lock.");
			}
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

    // Waking up all workers if they are sleeping.
    void WakeUpAllWorkersToCollectInactiveSockets();

	// Disconnect sockets when codehost dies.
	void DisconnectSocketsWhenCodehostDies(db_index_type db_index);

#ifdef USE_OLD_IPC_MONITOR

    // Opens active databases events with monitor.
    uint32_t OpenActiveDatabasesUpdatedEvent();

#endif

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
    ActiveDatabase* GetDatabase(db_index_type db_index)
    {
        return active_databases_ + db_index;
    }

    // Returns active database on this slot index.
    db_index_type FindDatabaseIndex(std::string db_name_lower)
    {
        for (int32_t i = 0; i < num_dbs_slots_; i++) {

            if (!active_databases_[i].IsEmpty()) {

                if (active_databases_[i].get_db_name() == db_name_lower)
                    return i;
            }
        }

        return INVALID_DB_INDEX;
    }

    // Get number of active databases.
    db_index_type get_num_dbs_slots()
    {
        return num_dbs_slots_;
    }

    // Reading command line arguments.
    uint32_t ProcessArgumentsAndInitLog(int argc, wchar_t* argv[]);

    // Get number of workers.
    int32_t setting_num_workers()
    {
        return setting_num_workers_;
    }

    // Getting the total number of overflow chunks for all workers.
    int64_t NumberOverflowChunksAllWorkers();

    // Local network interfaces to bind on.
    std::vector<std::string> setting_local_interfaces()
    {
        return setting_local_interfaces_;
    }

    // Constructor.
    Gateway();

    // Load settings from XML.
    uint32_t LoadSettings();

    // Load proxy configuration from XML.
    uint32_t LoadReverseProxies();

    // Getting gateway configuration XML contents.
    std::string GetConfigXmlContents();

    // Assert some correct state parameters.
    uint32_t AssertCorrectState();

    // Initialize the network gateway.
    uint32_t Init();

    // Sends an APC signal for rebalancing sockets.
    void SendRebalanceAPC(worker_id_type worker_id);

    // Checking for database changes.
    uint32_t CheckDatabaseChanges(const std::set<std::string>& active_databases);

	// Deleting existing codehost.
	uint32_t DeleteExistingCodehost(GatewayWorker *gw, const std::string codehost_name);

	// Adding new codehost.
	uint32_t AddNewCodehost(GatewayWorker *gw, const std::string codehost_name);

    // Print statistics.
    uint32_t StatisticsAndMonitoringRoutine();

    // Creates socket and binds it to server port.
    uint32_t CreateListeningSocketAndBindToPort(GatewayWorker *gw, uint16_t port_num, SOCKET& sock);

    // Start workers.
    uint32_t StartWorkerAndManagementThreads(
        LPTHREAD_START_ROUTINE workerRoutine,
#ifdef USE_OLD_IPC_MONITOR
        LPTHREAD_START_ROUTINE scanDbsRoutine,
#endif
        LPTHREAD_START_ROUTINE inactiveSocketsCleanupRoutine,
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
    void LogWriteCritical(const char* msg);
    void LogWriteCritical(const wchar_t* msg);
    void LogWriteError(const wchar_t* msg);
    void LogWriteWarning(const wchar_t* msg);
    void LogWriteNotice(const wchar_t* msg);
    void LogWriteGeneral(const char* msg, uint32_t log_type);
    void LogWriteGeneral(const wchar_t* msg, uint32_t log_type);
};

// Globally accessed gateway object.
extern Gateway g_gateway;

} // namespace network
} // namespace starcounter

#endif // GATEWAY_HPP