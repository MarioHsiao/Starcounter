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

        public bool IsConnected() {
            try {
                return !(socket_.Poll(0, SelectMode.SelectRead) && socket_.Available == 0);
            } catch (SocketException) { return false; }
        }

        public Socket SocketObj
        {

            get
            {
                Debug.Assert(socket_ != null);
                return socket_;
            }

            set
            {
                socket_ = value;
            }
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

    internal class NodeTask {
        /// <summary>
        /// Private buffer size for each connection.
        /// </summary>
        const Int32 PrivateBufferSize = 8192;

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
        public Byte[] RequestBytes
        {
            get
            {
                return requestBytes_;
            }
        }

        /// <summary>
        /// Size of request in bytes.
        /// </summary>
        Int32 requestBytesLength_ = 0;

        /// <summary>
        /// 
        /// </summary>
        public Int32 RequestBytesLength
        {
            get
            {
                return requestBytesLength_;
            }
        }

        /// <summary>
        /// User delegate.
        /// </summary>
        Action<Response> userDelegate_ = null;

        /// <summary>
        /// User delegate to call.
        /// </summary>
        public Action<Response> UserDelegate
        {
            get
            {
                return userDelegate_;
            }
        }

        /// <summary>
        /// Bound scheduler id.
        /// </summary>
        Byte boundSchedulerId_ = 0;

        /// <summary>
        /// Bound application name.
        /// </summary>
        String appName_;

        /// <summary>
        /// Memory stream.
        /// </summary>
        MemoryStream memStream_ = null;

        /// <summary>
        /// Node to which this connection belongs.
        /// </summary>
        Node nodeInst_ = null;

        /// <summary>
        /// Receive timeout in seconds.
        /// </summary>
        Int32 receiveTimeoutSeconds_;

        /// <summary>
        /// Receive timeout in seconds.
        /// </summary>
        internal Int32 ReceiveTimeoutSeconds
        {
            get
            {
                return receiveTimeoutSeconds_;
            }
        }
        
        /// <summary>
        /// Resets the connection details, but keeps the existing socket.
        /// </summary>
        public void ResetButKeepSocket(
            Request req,
            Action<Response> userDelegate,
            Int32 receiveTimeoutSeconds,
            Byte boundSchedulerId) {

            resp_ = null;
            totallyReceivedBytes_ = 0;
            receiveOffsetBytes_ = 0;
            responseSizeBytes_ = 0;

            if (req != null) {
                requestBytes_ = req.RequestBytes;
                requestBytesLength_ = req.RequestLength;
            } else {
                requestBytes_ = null;
                requestBytesLength_ = 0;
            }

            userDelegate_ = userDelegate;

            receiveTimeoutSeconds_ = receiveTimeoutSeconds;
            connectionTimedOut_ = false;

            memStream_ = new MemoryStream();
            boundSchedulerId_ = boundSchedulerId;
            appName_ = StarcounterEnvironment.AppName;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="hostName"></param>
        /// <param name="portNumber"></param>
        public NodeTask(Node nodeInst) {
            nodeInst_ = nodeInst;
            accumBuffer_ = new Byte[NodeTask.PrivateBufferSize];
        }

        /// <summary>
        /// Called when receive is finished on socket.
        /// </summary>
        /// <param name="ar"></param>
        void NetworkOnReceiveCallback(IAsyncResult ar) {
            try {
                // Checking if connection was timed out.
                if (connectionTimedOut_)
                    throw new IOException("Connection timed out.");

                // Calling end read to indicate finished read operation.
                Int32 recievedBytes = socketWrapper_.SocketObj.EndReceive(ar);

                // Checking if remote host has closed the connection.
                if (0 == recievedBytes) {
                    throw new IOException("Remote host closed the connection.");
                }

                // Dumping data to memory stream.
                memStream_.Write(accumBuffer_, receiveOffsetBytes_, recievedBytes);

                // Adding to total received size.
                totallyReceivedBytes_ += recievedBytes;

                // Process the bytes here.
                if (resp_ == null) {
                    try {
                        // Trying to parse the response.
                        resp_ = new Response(accumBuffer_, 0, totallyReceivedBytes_, false);

                        // Setting offset bytes to 0.
                        receiveOffsetBytes_ = 0;

                        // Getting the whole response size.
                        responseSizeBytes_ = resp_.ResponseSizeBytes;
                    } catch (Exception exc) {
                        // Continue to receive when there is not enough data.
                        resp_ = null;
                        receiveOffsetBytes_ = totallyReceivedBytes_;

                        // Trying to fetch recognized error code.
                        UInt32 code;
                        if ((!ErrorCode.TryGetCode(exc, out code)) || (code != Error.SCERRAPPSHTTPPARSERINCOMPLETEHEADERS)) {
                            CallUserDelegateOnFailure(exc, false);

                            return;
                        }
                    }
                }

                // Checking if we have received everything.
                if ((resp_ != null) && (totallyReceivedBytes_ == responseSizeBytes_)) {

                    // Setting the response buffer.
                    resp_.SetResponseBuffer(memStream_.GetBuffer(), 0, totallyReceivedBytes_);
                    memStream_.Close();
                    memStream_ = null;

                    // Invoking user delegate.
                    nodeInst_.CallUserDelegate(resp_, userDelegate_, boundSchedulerId_, appName_);

                    // Freeing connection resources.
                    nodeInst_.FreeConnection(this, false);
                } else {
                    // Read again. This callback will be called again.
                    socketWrapper_.SocketObj.BeginReceive(accumBuffer_, receiveOffsetBytes_, PrivateBufferSize - receiveOffsetBytes_, SocketFlags.None, NetworkOnReceiveCallback, null);
                }
            } catch (Exception exc) {
                CallUserDelegateOnFailure(exc, false);
            }
        }

        /// <summary>
        /// Called on receive timer expiration.
        /// </summary>
        /// <param name="obj"></param>
        void OnReceiveTimeout(object obj) {
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
        void NetworkOnSendCallback(IAsyncResult ar) {
            try {
                // Calling end write to indicate finished write operation.
                Int32 numBytesSent = socketWrapper_.SocketObj.EndSend(ar);

                // Setting the receive timeout.
                socketWrapper_.SocketObj.ReceiveTimeout = receiveTimeoutSeconds_ * 1000;

                // Checking for correct number of bytes sent.
                if (numBytesSent != requestBytesLength_) {
                    CallUserDelegateOnFailure(new Exception("Socket has sent wrong amount of data!"), false);
                    return;
                }

                // Starting read operation.
                IAsyncResult res = socketWrapper_.SocketObj.BeginReceive(accumBuffer_, 0, PrivateBufferSize, SocketFlags.None, NetworkOnReceiveCallback, null);

                // Checking if receive is not completed immediately.
                if (!res.IsCompleted) {

                    // Checking if we have timeout on receive.
                    if (receiveTimeoutSeconds_ != 0) {

                        // Scheduling a timer timeout job.
                        receiveTimer_ = new Timer(OnReceiveTimeout, null, receiveTimeoutSeconds_ * 1000, Timeout.Infinite);
                    }
                }
            } catch (Exception exc) {
                CallUserDelegateOnFailure(exc, false);
            }
        }

        /// <summary>
        /// Called when connect is finished on socket.
        /// </summary>
        /// <param name="ar"></param>
        void NetworkOnConnectCallback(IAsyncResult ar) {
            try {
                socketWrapper_.SocketObj.EndConnect(ar);

                socketWrapper_.SocketObj.BeginSend(requestBytes_, 0, requestBytesLength_, SocketFlags.None, NetworkOnSendCallback, null);
            } catch (Exception exc) {
                CallUserDelegateOnFailure(exc, false);
            }
        }

        /// <summary>
        /// Performs asynchronous request.
        /// </summary>
        public void PerformAsyncRequest() {

            try {

                // Checking if socket is connected.
                if ((socketWrapper_ != null) && (!socketWrapper_.IsConnected())) {

                    socketWrapper_.Destroy();
                    socketWrapper_ = null;
                }

                // Obtaining existing or creating new connection.
                if (null == socketWrapper_) {

                    socketWrapper_ = new SocketWrapper();

                    socketWrapper_.SocketObj = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                    // Trying to set a SIO_LOOPBACK_FAST_PATH on a TCP socket.
                    Node.SetLoopbackFastPathOnTcpSocket(socketWrapper_.SocketObj);

                    // Checking if we need to perform synchronous connect.
                    if (nodeInst_.ConnectSynchronuously) {

                        socketWrapper_.SocketObj.Connect(nodeInst_.HostName, nodeInst_.PortNumber);

                        socketWrapper_.SocketObj.BeginSend(requestBytes_, 0, requestBytesLength_, SocketFlags.None, NetworkOnSendCallback, null);

                    } else {

                        socketWrapper_.SocketObj.BeginConnect(nodeInst_.HostName, nodeInst_.PortNumber, NetworkOnConnectCallback, null);
                    }
                } else {
                    try {
                        socketWrapper_.SocketObj.BeginSend(requestBytes_, 0, requestBytesLength_, SocketFlags.None, NetworkOnSendCallback, null);
                    } catch {

                        // Seems the connection was closed so trying to reconnect.
                        socketWrapper_.Destroy();

                        socketWrapper_ = new SocketWrapper();

                        socketWrapper_.SocketObj = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                        // Trying to set a SIO_LOOPBACK_FAST_PATH on a TCP socket.
                        Node.SetLoopbackFastPathOnTcpSocket(socketWrapper_.SocketObj);

                        // Checking if we need to perform synchronous connect.
                        if (nodeInst_.ConnectSynchronuously) {

                            socketWrapper_.SocketObj.Connect(nodeInst_.HostName, nodeInst_.PortNumber);

                            socketWrapper_.SocketObj.BeginSend(requestBytes_, 0, requestBytesLength_, SocketFlags.None, NetworkOnSendCallback, null);

                        } else {

                            socketWrapper_.SocketObj.BeginConnect(nodeInst_.HostName, nodeInst_.PortNumber, NetworkOnConnectCallback, null);
                        }
                    }
                }
            } catch (Exception exc) {
                CallUserDelegateOnFailure(exc, false);
            }
        }

        /// <summary>
        /// Constructs response from bytes.
        /// </summary>
        internal void ConstructResponseAndCallDelegate(Byte[] bytes, Int32 offset, Int32 resp_len_bytes) {

            try {
                // Checking if server closed the connection.
                if (resp_len_bytes <= 0) {
                    CallUserDelegateOnFailure(new Exception("Server has sent zero response!"), false);
                    return;
                }

                // Trying to parse the response.
                resp_ = new Response(bytes, offset, resp_len_bytes, true);

                // Getting the whole response size.
                responseSizeBytes_ = resp_.ResponseSizeBytes;

            } catch (Exception exc) {

                // Continue to receive when there is not enough data.
                resp_ = null;

                // Trying to fetch recognized error code.
                UInt32 code;
                if ((!ErrorCode.TryGetCode(exc, out code)) || (code != Error.SCERRAPPSHTTPPARSERINCOMPLETEHEADERS)) {
                    // Logging the exception to server log.
                    if (nodeInst_.ShouldLogErrors)
                        Node.nodeLogException_(exc);

                    // Freeing connection resources.
                    nodeInst_.FreeConnection(this, false);

                    throw exc;
                }
            }

            // Invoking user delegate.
            nodeInst_.CallUserDelegate(resp_, userDelegate_, boundSchedulerId_, appName_);
        }


        /// <summary>
        /// Performs synchronous request and returns the response.
        /// </summary>
        /// <returns></returns>
        public Response PerformSyncRequest() {
            try {

                // Checking if socket is connected.
                if ((socketWrapper_ != null) && (!socketWrapper_.IsConnected())) {

                    socketWrapper_.Destroy();
                    socketWrapper_ = null;
                }

                // Checking if we are connected.
                if (null == socketWrapper_) {

                    socketWrapper_ = new SocketWrapper();

                    // Connection wasn't established.
                    socketWrapper_.SocketObj = new Socket(SocketType.Stream, ProtocolType.Tcp);

                    // Trying to set a SIO_LOOPBACK_FAST_PATH on a TCP socket.
                    Node.SetLoopbackFastPathOnTcpSocket(socketWrapper_.SocketObj);

                    // Assuming that existing TCP connection is down.
                    // So we need to create a new one.
                    socketWrapper_.SocketObj.Connect(nodeInst_.HostName, nodeInst_.PortNumber);
                }

                // Sending the request.
                socketWrapper_.SocketObj.Send(requestBytes_, 0, requestBytesLength_, SocketFlags.None);

                Int32 recievedBytes = 0;

                // Setting the receive timeout.
                socketWrapper_.SocketObj.ReceiveTimeout = receiveTimeoutSeconds_ * 1000;

                // Looping until we get everything.
                while (true) {

                    // Reading the response into predefined buffer.
                    recievedBytes = socketWrapper_.SocketObj.Receive(accumBuffer_, receiveOffsetBytes_, PrivateBufferSize - receiveOffsetBytes_, SocketFlags.None);
                    if (recievedBytes <= 0) {
                        throw new IOException("Remote host closed the connection.");
                    }

                    // Dumping data to memory stream.
                    memStream_.Write(accumBuffer_, receiveOffsetBytes_, recievedBytes);

                    // Adding to total received size.
                    totallyReceivedBytes_ += recievedBytes;

                    if (resp_ == null) {
                        try {
                            // Trying to parse the response.
                            resp_ = new Response(accumBuffer_, 0, totallyReceivedBytes_, false);

                            // Setting offset bytes to 0.
                            receiveOffsetBytes_ = 0;

                            // Getting the whole response size.
                            responseSizeBytes_ = resp_.ResponseSizeBytes;
                        } catch (Exception exc) {
                            // Continue to receive when there is not enough data.
                            resp_ = null;
                            receiveOffsetBytes_ = totallyReceivedBytes_;

                            // Trying to fetch recognized error code.
                            UInt32 code;
                            if ((!ErrorCode.TryGetCode(exc, out code)) || (code != Error.SCERRAPPSHTTPPARSERINCOMPLETEHEADERS)) {
                                // Logging the exception to server log.
                                if (nodeInst_.ShouldLogErrors)
                                    Node.nodeLogException_(exc);

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
                resp_.SetResponseBuffer(memStream_.GetBuffer(), 0, totallyReceivedBytes_);
                memStream_.Close();
                memStream_ = null;

                // Freeing connection resources.
                nodeInst_.FreeConnection(this, true);

            } catch (Exception exc) {
                CallUserDelegateOnFailure(exc, true);
            }

            return resp_;
        }

        /// <summary>
        /// Calls user delegate when response has failed.
        /// </summary>
        /// <param name="exc"></param>
        void CallUserDelegateOnFailure(Exception exc, Boolean isSyncCall) {
            // We don't want to use failing socket anymore.
            if (null != socketWrapper_) {
                socketWrapper_.Destroy();
                socketWrapper_ = null;
            }

            // Logging the exception to server log.
            if (nodeInst_.ShouldLogErrors) {
                Node.nodeLogException_(exc);
            }

            resp_ = new Response() {

                StatusCode = 503,
                StatusDescription = "Service Unavailable",
                ContentType = "text/plain;charset=utf-8",
                Body = exc.ToString()
            };

            // Parsing the response.
            resp_.ConstructFromFields(null, null);

            // Invoking user delegate.
            if (null != userDelegate_) {
                nodeInst_.CallUserDelegate(resp_, userDelegate_, boundSchedulerId_, appName_);
            }

            // Freeing connection resources.
            nodeInst_.FreeConnection(this, isSyncCall);
        }
    }
}