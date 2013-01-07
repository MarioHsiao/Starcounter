#pragma once
#ifndef SESSION_DATA_H
#define SESSION_DATA_H

namespace starcounter {
namespace network {

// Session related data.
class GatewayWorker;
class SessionData
{
    // Session random salt.
    UINT64 random_salt_;

    // Unique session linear index.
    // Points to the element in sessions linear array.
    UINT32 session_index_;

    // Used to track one-to-one relationship between attached socket and user session.
    UINT64 socket_stamp_;

    // Scheduler ID.
    UINT32 scheduler_id_;

    // Number of visits.
    UINT32 num_visits_;

    // Socket to which this session is attached.
    SOCKET attached_socket_;

public:

    // Gets attached socket.
    SOCKET attached_socket()
    {
        return attached_socket_;
    }

    // Scheduler ID.
    UINT32 scheduler_id()
    {
        return scheduler_id_;
    }

    // Number of visits.
    UINT32 num_visits()
    {
        return num_visits_;
    }

    // Unique session linear index.
    // Points to the element in sessions linear array.
    UINT32 session_index()
    {
        return session_index_;
    }

    // Used to track one-to-one relationship between attached socket and user session.
    UINT64 socket_stamp()
    {
        return socket_stamp_;
    }

    // Attaches a new socket.
    void AttachSocket(SOCKET newSocket, UINT64 newStamp)
    {
#ifdef GW_SESSIONS_DIAG
        GW_COUT << "Session: " << session_index_ << ", new socket attached: " << newSocket << std::endl;
#endif
        attached_socket_ = newSocket;
        socket_stamp_ = newStamp;
    }

    // Create new session based on random salt, linear index, scheduler.
    void GenerateNewSession(GatewayWorker *gw, UINT32 sessionIndex, UINT32 schedulerId);

    // Increase number of visits in this session.
    void IncreaseVisits()
    {
        num_visits_++;
    }

    // Compare socket stamps of two sessions.
    bool CompareSocketStamps(UINT64 socketStamp)
    {
        return socket_stamp_ == socketStamp;
    }

    // Compare random salt of two sessions.
    bool CompareSalt(UINT64 randomSalt)
    {
        return random_salt_ == randomSalt;
    }

    // Compare two sessions.
    bool Compare(UINT64 randomSalt, UINT32 sessionIndex)
    {
        return (random_salt_ == randomSalt) && (session_index_ == sessionIndex);
    }

    // Compare two sessions.
    bool Compare(SessionData *sessionData)
    {
        return (random_salt_ == sessionData->random_salt_) &&
            (session_index_ == sessionData->session_index_);
    }

    // Converts session to string.
    INT32 ConvertToString(char *str_out)
    {
        // Translating session index.
        INT32 sessionStringLen = uint64_to_hex_string(session_index_, str_out, 8);

        // Translating session random salt.
        sessionStringLen += uint64_to_hex_string(random_salt_, str_out + sessionStringLen, 16);

        return sessionStringLen;
    }
};

} // namespace network
} // namespace starcounter

#endif // SESSION_DATA_H
