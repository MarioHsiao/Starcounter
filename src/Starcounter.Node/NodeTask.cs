#define RECONNECT

using Starcounter.Advanced;
using Starcounter.Internal;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

namespace Starcounter {

    /// <summary>
    /// Wrapper around socket, which is closed on disposal.
    /// </summary>
    internal class SocketWrapper {

        Socket socket_;

        public Socket SocketObj {

            get {
                Debug.Assert(socket_ != null);
                return socket_;
            }

            set { socket_ = value; }
        }

        public void Destroy() {

            if (null != socket_) {

                socket_.Close();
                socket_ = null;
            }
        }

        ~SocketWrapper() {
            Destroy();            
        }
    }

    internal class NodeTask
    {
        /// <summary>
        /// Private buffer size for each connection.
        /// </summary>
        const Int32 PrivateBufferSize = 8192;

        /// <summary>
        /// Maximum number of pending asynchronous tasks.
        /// </summary>
        public const Int32 MaxNumPendingAsyncTasks = 1024;

        /// <summary>
        /// Maximum number of pending aggregated tasks.
        /// </summary>
        public const Int32 MaxNumPendingAggregatedTasks = 8192 * 2;

        /// <summary>
        /// Socket wrapper around currently used socket.
        /// </summary>
        SocketWrapper socketWrapper_;

        /// <summary>
        /// Buffer used for accumulation.
        /// </summary>
        Byte[] accumBuffer_ = null;

        /// <summary>
        /// Timer used for receive timeout.
        /// </summary>
        Timer receiveTimer_ = null;

        /// <summary>
        /// Indicates if connection has timed out.
        /// </summary>
        Boolean connectionTimedOut_ = false;

        /// <summary>
        /// Response.
        /// </summary>
        Response resp_ = null;

        /// <summary>
        /// Total received bytes.
        /// </summary>
        Int32 totallyReceivedBytes_ = 0;

        /// <summary>
        /// Current receive offset.
        /// </summary>
        Int32 receiveOffsetBytes_ = 0;

        /// <summary>
        /// Response size bytes.
        /// </summary>
        Int32 responseSizeBytes_ = 0;

        /// <summary>
        /// Request bytes.
        /// </summary>
        Byte[] requestBytes_ = null;

        /// <summary>
        /// Request bytes.
        /// </summary>
        public Byte[] RequestBytes {
            get { return requestBytes_; }
        }

        /// <summary>
        /// Size of request in bytes.
        /// </summary>
        Int32 requestBytesLength_ = 0;

        /// <summary>
        /// 
        /// </summary>
        public Int32 RequestBytesLength {
            get { return requestBytesLength_; }
        }

        /// <summary>
        /// User delegate.
        /// </summary>
        Action<Response, Object> userDelegate_ = null;

        /// <summary>
        /// Aggregation message delegate.
        /// </summary>
        Action<Node.AggregationStruct> aggrMsgDelegate_ = null;

        /// <summary>
        /// Aggregation message delegate.
        /// </summary>
        public Action<Node.AggregationStruct> AggrMsgDelegate {
            get { return aggrMsgDelegate_; }
        }

        /// <summary>
        /// Bound scheduler id.
        /// </summary>
        Byte boundSchedulerId_ = 0;

        /// <summary>
        /// User object.
        /// </summary>
        Object userObject_ = null;

        /// <summary>
        /// Memory stream.
        /// </summary>
        MemoryStream memStream_ = null;

        /// <summary>
        /// Node to which this connection belongs.
        /// </summary>
        Node nodeInst_ = null;

        /// <summary>
        /// Receive timeout in milliseconds.
        /// </summary>
        Int32 receiveTimeoutMs_;

        /// <summary>
        /// Resets the connection details, but keeps the existing socket.
        /// </summary>
        public void ResetButKeepSocket(
            Byte[] requestBytes,
            Int32 requestBytesLength,
            Action<Response, Object> userDelegate,
            Action<Node.AggregationStruct> aggrMsgDelegate,
            Object userObject,
            Int32 receiveTimeoutMs,
            Byte boundSchedulerId)
        {
            resp_ = null;
            totallyReceivedBytes_ = 0;
            receiveOffsetBytes_ = 0;
            responseSizeBytes_ = 0;

            requestBytes_ = requestBytes;
            requestBytesLength_ = requestBytesLength;

            userDelegate_ = userDelegate;
            userObject_ = userObject;

            receiveTimeoutMs_ = receiveTimeoutMs;
            connectionTimedOut_ = false;

            memStream_ = new MemoryStream();
            boundSchedulerId_ = boundSchedulerId;
            aggrMsgDelegate_ = aggrMsgDelegate;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="hostName"></param>
        /// <param name="portNumber"></param>
        public NodeTask(Node nodeInst)
        {
            nodeInst_ = nodeInst;

            if (!nodeInst_.UsesAggregation())
                accumBuffer_ = new Byte[NodeTask.PrivateBufferSize];
        }

        /// <summary>
        /// Called when receive is finished on socket.
        /// </summary>
        /// <param name="ar"></param>
        void NetworkOnReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Checking if connection was timed out.
                if (connectionTimedOut_)
                    throw new IOException("Connection timed out.");

                // Calling end read to indicate finished read operation.
                Int32 recievedBytes = socketWrapper_.SocketObj.EndReceive(ar);

                // Checking if remote host has closed the connection.
                if (0 == recievedBytes)
                    throw new IOException("Remote host closed the connection.");

                // Dumping data to memory stream.
                memStream_.Write(accumBuffer_, receiveOffsetBytes_, recievedBytes);

                // Adding to total received size.
                totallyReceivedBytes_ += recievedBytes;

                // Process the bytes here.
                if (resp_ == null)
                {
                    try
                    {
                        // Trying to parse the response.
                        resp_ = new Response(accumBuffer_, 0, totallyReceivedBytes_, null, false);

                        // Setting offset bytes to 0.
                        receiveOffsetBytes_ = 0;

                        // Getting the whole response size.
                        responseSizeBytes_ = resp_.ResponseSizeBytes;
                    }
                    catch (Exception exc)
                    {
                        // Continue to receive when there is not enough data.
                        resp_ = null;
                        receiveOffsetBytes_ = totallyReceivedBytes_;

                        // Trying to fetch recognized error code.
                        UInt32 code;
                        if ((!ErrorCode.TryGetCode(exc, out code)) || (code != Error.SCERRAPPSHTTPPARSERINCOMPLETEHEADERS))
                        {
                            CallUserDelegateOnFailure(exc, false);

                            return;
                        }
                    }
                }

                // Checking if we have received everything.
                if ((resp_ != null) && (totallyReceivedBytes_ == responseSizeBytes_))
                {
                    // Setting the response buffer.
                    resp_.SetResponseBuffer(memStream_.GetBuffer(), memStream_, totallyReceivedBytes_);

                    // Invoking user delegate.
                    nodeInst_.CallUserDelegate(resp_, userDelegate_, userObject_, boundSchedulerId_);

                    // Freeing connection resources.
                    nodeInst_.FreeConnection(this, false);
                }
                else
                {
                    // Read again. This callback will be called again.
                    socketWrapper_.SocketObj.BeginReceive(accumBuffer_, receiveOffsetBytes_, PrivateBufferSize - receiveOffsetBytes_, SocketFlags.None, NetworkOnReceiveCallback, null);
                }
            }
            catch (Exception exc)
            {
                CallUserDelegateOnFailure(exc, false);
            }
        }

        /// <summary>
        /// Called on receive timer expiration.
        /// </summary>
        /// <param name="obj"></param>
        void OnReceiveTimeout(object obj)
        {
            receiveTimer_.Dispose();
            connectionTimedOut_ = true;

            if (null != socketWrapper_) {
                socketWrapper_.Destroy();
                socketWrapper_ = null;
            }
        }

        /// <summary>
        /// Called when send is finished on socket.
        /// </summary>
        /// <param name="ar"></param>
        void NetworkOnSendCallback(IAsyncResult ar)
        {
            try
            {
                // Calling end write to indicate finished write operation.
                Int32 numBytesSent = socketWrapper_.SocketObj.EndSend(ar);

                // Setting the receive timeout.
                socketWrapper_.SocketObj.ReceiveTimeout = receiveTimeoutMs_;

                // Checking for correct number of bytes sent.
                if (numBytesSent != requestBytesLength_)
                {
                    CallUserDelegateOnFailure(new Exception("Socket has sent wrong amount of data!"), false);
                    return;
                }

                // Starting read operation.
                IAsyncResult res = socketWrapper_.SocketObj.BeginReceive(accumBuffer_, 0, PrivateBufferSize, SocketFlags.None, NetworkOnReceiveCallback, null);

                // Checking if receive is not completed immediately.
                if(!res.IsCompleted)
                {
                    // Checking if we have timeout on receive.
                    if (receiveTimeoutMs_ != 0)
                    {
                        // Scheduling a timer timeout job.
                        receiveTimer_ = new Timer(OnReceiveTimeout, null, receiveTimeoutMs_, Timeout.Infinite);
                    }
                }
            }
            catch (Exception exc)
            {
                CallUserDelegateOnFailure(exc, false);
            }
        }

        /// <summary>
        /// Called when connect is finished on socket.
        /// </summary>
        /// <param name="ar"></param>
        void NetworkOnConnectCallback(IAsyncResult ar)
        {
            try
            {
                socketWrapper_.SocketObj.EndConnect(ar);

                socketWrapper_.SocketObj.BeginSend(requestBytes_, 0, requestBytesLength_, SocketFlags.None, NetworkOnSendCallback, null);
            }
            catch (Exception exc)
            {
                CallUserDelegateOnFailure(exc, false);
            }
        }

        /// <summary>
        /// Performs asynchronous request.
        /// </summary>
        public void PerformAsyncRequest()
        {
            try
            {
                // Obtaining existing or creating new connection.
                if (null == socketWrapper_)
                {
                    socketWrapper_ = new SocketWrapper();

                    socketWrapper_.SocketObj = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                    socketWrapper_.SocketObj.BeginConnect(nodeInst_.HostName, nodeInst_.PortNumber, NetworkOnConnectCallback, null);
                }
                else
                {
#if RECONNECT
                    try
                    {
#endif
                        socketWrapper_.SocketObj.BeginSend(requestBytes_, 0, requestBytesLength_, SocketFlags.None, NetworkOnSendCallback, null);
#if RECONNECT
                    }
                    catch
                    {
                        // Seems the connection was closed so trying to reconnect.

                        socketWrapper_ = new SocketWrapper();

                        socketWrapper_.SocketObj = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                        socketWrapper_.SocketObj.BeginConnect(nodeInst_.HostName, nodeInst_.PortNumber, NetworkOnConnectCallback, null);
                    }
#endif
                }
            }
            catch (Exception exc)
            {
                CallUserDelegateOnFailure(exc, false);
            }
        }

        /// <summary>
        /// Constructs response from bytes.
        /// </summary>
        internal void ConstructResponseAndCallDelegate(Byte[] bytes, Int32 offset, Int32 resp_len_bytes)
        {
            try
            {
                // Checking if server closed the connection.
                if (resp_len_bytes <= 0) {
                    CallUserDelegateOnFailure(new Exception("Server has sent zero response!"), false);
                    return;
                }

                // Trying to parse the response.
                resp_ = new Response(bytes, offset, resp_len_bytes, null, false);

                // Getting the whole response size.
                responseSizeBytes_ = resp_.ResponseSizeBytes;
            }
            catch (Exception exc)
            {
                // Continue to receive when there is not enough data.
                resp_ = null;

                // Trying to fetch recognized error code.
                UInt32 code;
                if ((!ErrorCode.TryGetCode(exc, out code)) || (code != Error.SCERRAPPSHTTPPARSERINCOMPLETEHEADERS))
                {
                    // Logging the exception to server log.
                    if (nodeInst_.ShouldLogErrors)
                        Node.NodeLogException_(exc);

                    // Freeing connection resources.
                    nodeInst_.FreeConnection(this, false);

                    throw exc;
                }
            }

            // Setting the response buffer.
            Byte[] resp_bytes = new Byte[resp_len_bytes];
            Buffer.BlockCopy(bytes, offset, resp_bytes, 0, resp_len_bytes);
            resp_.SetResponseBuffer(resp_bytes, null, resp_len_bytes);

            // Invoking user delegate.
            nodeInst_.CallUserDelegate(resp_, userDelegate_, userObject_, boundSchedulerId_);
        }

        /// <summary>
        /// Performs synchronous request and returns the response.
        /// </summary>
        /// <returns></returns>
        public Response PerformSyncRequest()
        {
            try
            {
#if RECONNECT
                try
                {
#endif
                    // Checking if we are connected.
                    if (null == socketWrapper_)
                    {
                        socketWrapper_ = new SocketWrapper();

                        // Connection wasn't established.
                        socketWrapper_.SocketObj = new Socket(SocketType.Stream, ProtocolType.Tcp);

                        // Assuming that existing TCP connection is down.
                        // So we need to create a new one.
                        socketWrapper_.SocketObj.Connect(nodeInst_.HostName, nodeInst_.PortNumber);
                    }

                    // Sending the request.
                    Int32 bytesSent = socketWrapper_.SocketObj.Send(requestBytes_, 0, requestBytesLength_, SocketFlags.None);
                    Debug.Assert(requestBytesLength_ == bytesSent);
#if RECONNECT
                }
                catch
                {
                    socketWrapper_ = new SocketWrapper();

                    // Connection wasn't established.
                    socketWrapper_.SocketObj = new Socket(SocketType.Stream, ProtocolType.Tcp);

                    // Assuming that existing TCP connection is down.
                    // So we need to create a new one.
                    socketWrapper_.SocketObj.Connect(nodeInst_.HostName, nodeInst_.PortNumber);

                    // Sending the request.
                    socketWrapper_.SocketObj.Send(requestBytes_, 0, requestBytesLength_, SocketFlags.None);
                }
#endif

                Int32 recievedBytes = 0;

                // Setting the receive timeout.
                socketWrapper_.SocketObj.ReceiveTimeout = receiveTimeoutMs_;

                // Looping until we get everything.
                while (true)
                {
                    // Reading the response into predefined buffer.
                    recievedBytes = socketWrapper_.SocketObj.Receive(accumBuffer_, receiveOffsetBytes_, PrivateBufferSize - receiveOffsetBytes_, SocketFlags.None);
                    if (recievedBytes <= 0) {
                        throw new IOException("Remote host closed the connection.");
                    }

                    // Dumping data to memory stream.
                    memStream_.Write(accumBuffer_, receiveOffsetBytes_, recievedBytes);

                    // Adding to total received size.
                    totallyReceivedBytes_ += recievedBytes;

                    if (resp_ == null)
                    {
                        try
                        {
                            // Trying to parse the response.
                            resp_ = new Response(accumBuffer_, 0, totallyReceivedBytes_, null, false);

                            // Setting offset bytes to 0.
                            receiveOffsetBytes_ = 0;

                            // Getting the whole response size.
                            responseSizeBytes_ = resp_.ResponseSizeBytes;
                        }
                        catch (Exception exc)
                        {
                            // Continue to receive when there is not enough data.
                            resp_ = null;
                            receiveOffsetBytes_ = totallyReceivedBytes_;

                            // Trying to fetch recognized error code.
                            UInt32 code;
                            if ((!ErrorCode.TryGetCode(exc, out code)) || (code != Error.SCERRAPPSHTTPPARSERINCOMPLETEHEADERS))
                            {
                                // Logging the exception to server log.
                                if (nodeInst_.ShouldLogErrors)
                                    Node.NodeLogException_(exc);

                                // Freeing connection resources.
                                nodeInst_.FreeConnection(this, true);

                                throw exc;
                            }
                        }
                    }

                    // Checking if we have received everything.
                    if ((resp_ != null) && (totallyReceivedBytes_ == responseSizeBytes_))
                        break;
                }

                // Setting the response buffer.
                resp_.SetResponseBuffer(memStream_.GetBuffer(), memStream_, totallyReceivedBytes_);

                // Freeing connection resources.
                nodeInst_.FreeConnection(this, true);
            }
            catch (Exception exc)
            {
                CallUserDelegateOnFailure(exc, true);
            }

            return resp_;
        }

        /// <summary>
        /// Calls user delegate when response has failed.
        /// </summary>
        /// <param name="exc"></param>
        void CallUserDelegateOnFailure(Exception exc, Boolean isSyncCall)
        {
            // We don't want to use failing socket anymore.
            if (null != socketWrapper_) {
                socketWrapper_.Destroy();
                socketWrapper_ = null;
            }

            // Logging the exception to server log.
            if (nodeInst_.ShouldLogErrors)
                Node.NodeLogException_(exc);

            resp_ = new Response()
            {
                StatusCode = 503,
                StatusDescription = "Service Unavailable",
                ContentType = "text/plain",
                Body = exc.ToString()
            };

            // Parsing the response.
            resp_.ConstructFromFields();
            resp_.ParseResponseFromPlainBuffer();

            // Invoking user delegate.
            if (null != userDelegate_)
                nodeInst_.CallUserDelegate(resp_, userDelegate_, userObject_, boundSchedulerId_);

            // Freeing connection resources.
            nodeInst_.FreeConnection(this, isSyncCall);
        }
    }
}