using Starcounter.Advanced;
using Starcounter.Internal;
using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Starcounter {
    internal class NodeTask
    {
        /// <summary>
        /// Private buffer size for each connection.
        /// </summary>
        public const Int32 PrivateBufferSize = 8192;

        /// <summary>
        /// Maximum number of pending asynchronous calls.
        /// </summary>
        public const Int32 MaxNumPendingAsyncTasks = 8192 * 4;

        /// <summary>
        /// Tcp client.
        /// </summary>
        public TcpClient TcpClientObj = null;

        /// <summary>
        /// Connection socket.
        /// </summary>
        public Socket SocketObj = null;

        /// <summary>
        /// Response.
        /// </summary>
        public Response Resp = null;

        /// <summary>
        /// Total received bytes.
        /// </summary>
        public Int32 TotallyReceivedBytes = 0;

        /// <summary>
        /// Response size bytes.
        /// </summary>
        public Int32 ResponseSizeBytes = 0;

        /// <summary>
        /// Request bytes.
        /// </summary>
        public Byte[] RequestBytes = null;

        /// <summary>
        /// Size of request in bytes.
        /// </summary>
        public Int32 RequestBytesLength = 0;

        /// <summary>
        /// Original request.
        /// </summary>
        public Request OrigReq = null;

        /// <summary>
        /// User delegate.
        /// </summary>
        public Func<Response, Object, Response> UserDelegate = null;

        /// <summary>
        /// User object.
        /// </summary>
        public Object UserObject = null;

        /// <summary>
        /// Memory stream.
        /// </summary>
        public MemoryStream MemStream = null;

        /// <summary>
        /// Node to which this connection belongs.
        /// </summary>
        public Node NodeInst = null;

        /// <summary>
        /// Resets the connection details.
        /// </summary>
        public void Reset(Byte[] requestBytes, Int32 requestBytesLength, Request origReq, Func<Response, Object, Response> userDelegate, Object userObject)
        {
            Resp = null;
            TotallyReceivedBytes = 0;
            ResponseSizeBytes = 0;

            RequestBytes = requestBytes;
            RequestBytesLength = requestBytesLength;
            OrigReq = origReq;

            UserDelegate = userDelegate;
            UserObject = userObject;

            MemStream = new MemoryStream();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="hostName"></param>
        /// <param name="portNumber"></param>
        public NodeTask(Node nodeInst)
        {
            NodeInst = nodeInst;
        }

        /// <summary>
        /// Returns True if connection is established.
        /// </summary>
        public Boolean IsConnectionEstablished()
        {
            return (null != TcpClientObj);
        }

        /// <summary>
        /// Attaching existing connection or reconnecting synchronously.
        /// </summary>
        public void AttachConnection(TcpClient existingTcpClient)
        {
            if (null == existingTcpClient)
                TcpClientObj = new TcpClient(NodeInst.HostName, NodeInst.PortNumber);
            else
                TcpClientObj = existingTcpClient;

            SocketObj = TcpClientObj.Client;
        }

        /// <summary>
        /// Closes the connection.
        /// </summary>
        public void Close()
        {
            if (null != TcpClientObj)
            {
                TcpClientObj.Close();
                TcpClientObj = null;
            }

            if (null != SocketObj)
            {
                SocketObj.Close();
                SocketObj = null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ar"></param>
        void NetworkReadCallback(IAsyncResult ar)
        {
            try
            {
                // Calling end read to indicate finished read operation.
                Int32 recievedBytes = SocketObj.EndReceive(ar);

                // Checking if remote host has closed the connection.
                if (0 == recievedBytes)
                    throw new Exception("Remote host closed the connection.");

                // Process the bytes here.
                if (Resp == null)
                {
                    try
                    {
                        // Trying to parse the response.
                        Resp = new Response(NodeInst.AccumBuffer, 0, recievedBytes, OrigReq, false);

                        // Getting the whole response size.
                        ResponseSizeBytes = Resp.ResponseLength;
                    }
                    catch (Exception exc)
                    {
                        // Continue to receive when there is not enough data.
                        Resp = null;

                        // Trying to fetch recognized error code.
                        UInt32 code;
                        if ((!ErrorCode.TryGetCode(exc, out code)) || (code != Error.SCERRAPPSHTTPPARSERINCOMPLETEHEADERS))
                        {
                            CallUserDelegateOnFailure(exc);

                            return;
                        }
                    }
                }

                // Writing received data to memory stream.
                MemStream.Write(NodeInst.AccumBuffer, 0, recievedBytes);
                TotallyReceivedBytes += recievedBytes;

                // Checking if we have received everything.
                if ((Resp != null) && (TotallyReceivedBytes == ResponseSizeBytes))
                {
                    // Setting the response buffer.
                    Resp.SetResponseBuffer(MemStream.GetBuffer(), MemStream, TotallyReceivedBytes);

                    // Invoking user delegate.
                    Node.CallUserDelegate(OrigReq, Resp, UserDelegate, UserObject);

                    // Freeing connection resources.
                    NodeInst.FreeConnection(this);
                }
                else
                {
                    // Read again. This callback will be called again.
                    SocketObj.BeginReceive(NodeInst.AccumBuffer, 0, PrivateBufferSize, SocketFlags.None, NetworkReadCallback, null);
                }
            }
            catch (Exception exc)
            {
                CallUserDelegateOnFailure(exc);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ar"></param>
        void NetworkWriteCallback(IAsyncResult ar)
        {
            try
            {
                // Calling end write to indicate finished write operation.
                Int32 numBytesSent = SocketObj.EndSend(ar);

                // Checking for correct number of bytes sent.
                if (numBytesSent != RequestBytesLength)
                {
                    CallUserDelegateOnFailure(new Exception("Socket has sent wrong amount of data!"));
                    return;
                }

                // Starting read operation.
                SocketObj.BeginReceive(NodeInst.AccumBuffer, 0, PrivateBufferSize, SocketFlags.None, NetworkReadCallback, null);
            }
            catch (Exception exc)
            {
                CallUserDelegateOnFailure(exc);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ar"></param>
        void NetworkConnectCallback(IAsyncResult ar)
        {
            try
            {
                SocketObj.EndConnect(ar);

                TcpClientObj = new TcpClient();
                TcpClientObj.Client = SocketObj;

                SocketObj.BeginSend(RequestBytes, 0, RequestBytesLength, SocketFlags.None, NetworkWriteCallback, null);
            }
            catch (Exception exc)
            {
                CallUserDelegateOnFailure(exc);
            }
        }

        /// <summary>
        /// Performs asynchronous request.
        /// </summary>
        public void PerformAsyncRequest()
        {
            // Obtaining existing or creating new connection.
            if (null == SocketObj)
            {
                SocketObj = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                SocketObj.BeginConnect(NodeInst.HostName, NodeInst.PortNumber, NetworkConnectCallback, null);
            }
            else
            {
                try
                {
                    SocketObj.BeginSend(RequestBytes, 0, RequestBytesLength, SocketFlags.None, NetworkWriteCallback, null);
                }
                catch
                {
                    // Seems connection was closed so reconnecting.
                    SocketObj = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                    SocketObj.BeginConnect(NodeInst.HostName, NodeInst.PortNumber, NetworkConnectCallback, null);
                }
            }
        }

        /// <summary>
        /// Constructs response from bytes.
        /// </summary>
        internal void ConstructResponseAndCallDelegate(Byte[] bytes, Int32 offset, Int32 resp_len_bytes)
        {
            try
            {
                // Trying to parse the response.
                Resp = new Response(bytes, offset, resp_len_bytes, OrigReq, false);

                // Getting the whole response size.
                ResponseSizeBytes = Resp.ResponseLength;
            }
            catch (Exception exc)
            {
                // Continue to receive when there is not enough data.
                Resp = null;

                // Trying to fetch recognized error code.
                UInt32 code;
                if ((!ErrorCode.TryGetCode(exc, out code)) || (code != Error.SCERRAPPSHTTPPARSERINCOMPLETEHEADERS))
                {
                    // Logging the exception to server log.
                    if (NodeInst.ShouldLogErrors)
                        Node.NodeLogException_(exc);

                    // Freeing connection resources.
                    NodeInst.FreeConnection(this, true);

                    throw exc;
                }
            }

            // Setting the response buffer.
            Byte[] resp_bytes = new Byte[resp_len_bytes];
            Buffer.BlockCopy(bytes, offset, resp_bytes, 0, resp_len_bytes);
            Resp.SetResponseBuffer(resp_bytes, null, resp_len_bytes);

            // Invoking user delegate.
            Node.CallUserDelegate(OrigReq, Resp, UserDelegate, UserObject);
        }

        /// <summary>
        /// Performs synchronous request and returns the response.
        /// </summary>
        /// <returns></returns>
        public Response PerformSyncRequest()
        {
            // Checking if we are connected.
            if (null == SocketObj)
                AttachConnection(null);

            // Sending the request.
            try
            {
                SocketObj.Send(RequestBytes, 0, RequestBytesLength, SocketFlags.None);
            }
            catch
            {
                // Assuming that existing TCP connection is down.
                // So we need to create a new one.
                AttachConnection(null);

                SocketObj.Send(RequestBytes, 0, RequestBytesLength, SocketFlags.None);
            }

            Int32 recievedBytes;

            // Looping until we get everything.
            while (true)
            {
                // Reading the response into predefined buffer.
                recievedBytes = SocketObj.Receive(NodeInst.AccumBuffer, 0, PrivateBufferSize, SocketFlags.None);
                if (recievedBytes <= 0)
                {
                    SocketObj = null;
                    throw new IOException("Remote host closed the connection.");
                }

                if (Resp == null)
                {
                    try
                    {
                        // Trying to parse the response.
                        Resp = new Response(NodeInst.AccumBuffer, 0, recievedBytes, OrigReq, false);

                        // Getting the whole response size.
                        ResponseSizeBytes = Resp.ResponseLength;
                    }
                    catch (Exception exc)
                    {
                        // Continue to receive when there is not enough data.
                        Resp = null;

                        // Trying to fetch recognized error code.
                        UInt32 code;
                        if ((!ErrorCode.TryGetCode(exc, out code)) || (code != Error.SCERRAPPSHTTPPARSERINCOMPLETEHEADERS))
                        {
                            // Logging the exception to server log.
                            if (NodeInst.ShouldLogErrors)
                                Node.NodeLogException_(exc);

                            // Freeing connection resources.
                            NodeInst.FreeConnection(this, true);

                            throw exc;
                        }
                    }
                }

                MemStream.Write(NodeInst.AccumBuffer, 0, recievedBytes);
                TotallyReceivedBytes += recievedBytes;

                // Checking if we have received everything.
                if ((Resp != null) && (TotallyReceivedBytes == ResponseSizeBytes))
                    break;
            }

            // Setting the response buffer.
            Resp.SetResponseBuffer(MemStream.GetBuffer(), MemStream, TotallyReceivedBytes);

            // Freeing connection resources.
            NodeInst.FreeConnection(this, true);

            return Resp;
        }

        /// <summary>
        /// Calls user delegate when response has failed.
        /// </summary>
        /// <param name="exc"></param>
        void CallUserDelegateOnFailure(Exception exc)
        {
            // Logging the exception to server log.
            if (NodeInst.ShouldLogErrors)
                Node.NodeLogException_(exc);

            Resp = new Response()
            {
                StatusCode = 503,
                StatusDescription = "Service Unavailable",
                ContentType = "text/plain",
                Body = exc.ToString()
            };

            // Parsing the response.
            Resp.ConstructFromFields();
            Resp.ParseResponseFromUncompressed();

            // Invoking user delegate.
            Node.CallUserDelegate(OrigReq, Resp, UserDelegate, UserObject);

            // Freeing connection resources.
            NodeInst.FreeConnection(this);
        }
    }
}