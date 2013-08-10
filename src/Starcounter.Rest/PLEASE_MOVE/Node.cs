// ***********************************************************************
// <copyright file="ScUri.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Net;
using Starcounter.Internal;
using Starcounter.Advanced;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using HttpStructs;
using Starcounter.Logging;

namespace Starcounter
{
    internal class NodeTask
    {
        /// <summary>
        /// Private buffer size for each connection.
        /// </summary>
        public const Int32 PrivateBufferSize = 8192;

        /// <summary>
        /// Maximum number of pending asynchronous calls.
        /// </summary>
        public const Int32 MaxNumPendingAsyncTasks = 128;

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
        public void Reset(Byte[] requestBytes, Request origReq, Func<Response, Object, Response> userDelegate, Object userObject)
        {
            Resp = null;
            TotallyReceivedBytes = 0;
            ResponseSizeBytes = 0;

            RequestBytes = requestBytes;
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

                // Process the bytes here.
                if (Resp == null)
                {
                    try
                    {
                        // Trying to parse the response.
                        Resp = new Response(NodeInst.AccumBuffer, 0, recievedBytes, OrigReq, false);

                        // Getting the whole response size.
                        ResponseSizeBytes = (Int32)Resp.GetResponseLength();
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
                if (numBytesSent != RequestBytes.Length)
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

                SocketObj.BeginSend(RequestBytes, 0, RequestBytes.Length, SocketFlags.None, NetworkWriteCallback, null);
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
                    SocketObj.BeginSend(RequestBytes, 0, RequestBytes.Length, SocketFlags.None, NetworkWriteCallback, null);
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
        /// Performs synchronous request and returns the response.
        /// </summary>
        /// <returns></returns>
        public Response PerformSyncRequest()
        {
            Boolean tried_reconnect = false;

RECONNECT:

            // Checking if we are connected.
            if (null == SocketObj)
                AttachConnection(null);

            // Sending the request.
            try
            {
                SocketObj.Send(RequestBytes, 0, RequestBytes.Length, SocketFlags.None);
            }
            catch
            {
                // Assuming that existing TCP connection is down.
                // So we need to create a new one.
                AttachConnection(null);

                SocketObj.Send(RequestBytes, 0, RequestBytes.Length, SocketFlags.None);
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

                    // Trying only once to reconnect.
                    if (tried_reconnect)
                    {
                        throw new IOException("Remote host closed the connection.");
                    }
                    else
                    {
                        tried_reconnect = true;
                        goto RECONNECT;
                    }
                }

                if (Resp == null)
                {
                    try
                    {
                        // Trying to parse the response.
                        Resp = new Response(NodeInst.AccumBuffer, 0, recievedBytes, OrigReq, false);

                        // Getting the whole response size.
                        ResponseSizeBytes = (Int32) Resp.GetResponseLength();
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
                                Node.ErrorLogger.LogException(exc);

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
                Node.ErrorLogger.LogException(exc);

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

    public class Node
    {
        Boolean ShouldLogErrors_;

        /// <summary>
        /// Indicates if Node errors should be logged.
        /// </summary>
        public Boolean ShouldLogErrors
        {
            get { return ShouldLogErrors_; }
            
            set {

                // Checking if we are running inside Starcounter.
                if (!StarcounterEnvironment.IsCodeHosted)
                    throw new ArgumentException("Node is not running inside Starcounter to log errors.");

                ShouldLogErrors_ = value;
            }
        }

        /// <summary>
        /// The Node log source.
        /// </summary>
        internal static LogSource ErrorLogger = new LogSource("Node");

        /// <summary>
        /// Represents this Starcounter node.
        /// </summary>
        [ThreadStatic]
        static Node LocalhostSystemPortNode_ = null;

        /// <summary>
        /// Creates an instance of localhost system node.
        /// </summary>
        public static Node LocalhostSystemPortNode
        {
            get
            {
                // Checking if Node instance is already created for this thread.
                if (null != LocalhostSystemPortNode_)
                    return LocalhostSystemPortNode_;

                // Creating new node instance.
                LocalhostSystemPortNode_ = new Node("127.0.0.1", StarcounterEnvironment.Default.SystemHttpPort);

                return LocalhostSystemPortNode_;
            }
        }

        /// <summary>
        /// Represents this Starcounter node for default user port.
        /// </summary>
        [ThreadStatic]
        static Node This_ = null;

        /// <summary>
        /// Creates an instance of localhost system node.
        /// </summary>
        public static Node This
        {
            get
            {
                // Checking if Node instance is already created for this thread.
                if (null != This_)
                    return This_;

                // Creating new node instance.
                This_ = new Node("127.0.0.1", StarcounterEnvironment.Default.UserHttpPort);

                return This_;
            }
        }

        /// <summary>
        /// Static constructor to automatically initialize REST.
        /// </summary>
        static Node()
        {
            HelperFunctions.LoadNonGACDependencies();
            RequestHandler.InitREST();
        }

        /// <summary>
        /// Setting local node flag for unit tests explicitly.
        /// </summary>
        internal void InternalSetLocalNodeForUnitTests(bool value = true)
        {
            localNode_ = value;
            unitTests_ = true;
        }

        /// <summary>
        /// Indicates the local node.
        /// </summary>
        Boolean localNode_ = false;

        /// <summary>
        /// True if we are running unit tests.
        /// </summary>
        Boolean unitTests_ = false;

        /// <summary>
        /// Host name of this node e.g.: www.starcounter.com, 192.168.0.1
        /// </summary>
        String hostName_;

        /// <summary>
        /// HTTP port number, e.g.: 80
        /// </summary>
        UInt16 portNumber_;

        /// <summary>
        /// Pending async tasks.
        /// </summary>
        Queue<NodeTask> pending_async_tasks_ = new Queue<NodeTask>();

        /// <summary>
        /// Finished async tasks.
        /// </summary>
        Queue<NodeTask> finished_async_tasks_ = new Queue<NodeTask>();

        /// <summary>
        /// Lock object.
        /// </summary>
        internal Object LockObj = new Object();

        /// <summary>
        /// Buffer used for accumulation.
        /// </summary>
        internal Byte[] AccumBuffer = new Byte[NodeTask.PrivateBufferSize];

        /// <summary>
        /// Node core task information.
        /// </summary>
        NodeTask core_task_info_ = null;

        /// <summary>
        /// Delegate to process the results of calling user delegate.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        internal delegate Response HandleResponse(Request request, Response x);

        /// <summary>
        /// Handle responses delegate.
        /// </summary>
        internal static HandleResponse HandleResponse_ = null;

        /// <summary>
        /// Sets the delegate from using code.
        /// </summary>
        /// <param name="hr"></param>
        internal static void SetHandleResponse(HandleResponse hr)
        {
            HandleResponse_ = hr;
        }

        /// <summary>
        /// Returns this node port number.
        /// </summary>
        public UInt16 PortNumber { get { return portNumber_; } }

        /// <summary>
        /// Returns this node host name.
        /// </summary>
        public String HostName { get { return hostName_; } }

        /// <summary>
        /// Returns this node URI.
        /// </summary>
        public Uri BaseAddress { get { return new Uri("http://" + hostName_ + ":" + portNumber_); } }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="hostName"></param>
        /// <param name="portNumber"></param>
        public Node(String hostName, UInt16 portNumber = 0)
        {
            if (hostName.ToLower().Contains("localhost") ||
                hostName.ToLower().Contains("127.0.0.1"))
            {
                // Checking if we are running inside Starcounter hosting process.
                if (StarcounterEnvironment.IsCodeHosted)
                    localNode_ = true;
            }

            if (0 == portNumber)
                portNumber = StarcounterEnvironment.Default.UserHttpPort;

            hostName_ = hostName;
            portNumber_ = portNumber;
            core_task_info_ = new NodeTask(this);
        }

        /// <summary>
        /// Performs asynchronous HTTP GET.
        /// </summary>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="req">Original HTTP request.</param>
        /// <param name="userObject">User object to be passed on response.</param>
        /// <param name="userDelegate">User delegate to be called on response.</param>
        public void GET(String relativeUri, String customHeaders, Request req, Object userObject, Func<Response, Object, Response> userDelegate)
        {
            DoRESTRequestAndGetResponse("GET", relativeUri, customHeaders, null, req, userDelegate, userObject);
        }

        /// <summary>
        /// Performs synchronous HTTP GET.
        /// </summary>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="req">Original HTTP request.</param>
        /// <returns>HTTP response.</returns>
        public Response GET(String relativeUri, String customHeaders, Request req)
        {
            return DoRESTRequestAndGetResponse("GET", relativeUri, customHeaders, null, req, null, null);
        }

        /// <summary>
        /// Performs asynchronous HTTP POST.
        /// </summary>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="body">HTTP body or null.</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="req">Original HTTP request.</param>
        /// <param name="userObject">User object to be passed on response.</param>
        /// <param name="userDelegate">User delegate to be called on response.</param>
        public void POST(String relativeUri, String body, String customHeaders, Request req, Object userObject, Func<Response, Object, Response> userDelegate)
        {
            Byte[] bodyBytes = null;
            if (body != null)
                bodyBytes = Encoding.UTF8.GetBytes(body);

            DoRESTRequestAndGetResponse("POST", relativeUri, customHeaders, bodyBytes, req, userDelegate, userObject);
        }

        /// <summary>
        /// Performs asynchronous HTTP POST.
        /// </summary>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="body">HTTP body or null.</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="req">Original HTTP request.</param>
        /// <param name="userObject">User object to be passed on response.</param>
        /// <param name="userDelegate">User delegate to be called on response.</param>
        public void POST(String relativeUri, Byte[] bodyBytes, String customHeaders, Request req, Object userObject, Func<Response, Object, Response> userDelegate)
        {
            DoRESTRequestAndGetResponse("POST", relativeUri, customHeaders, bodyBytes, req, userDelegate, userObject);
        }

        /// <summary>
        /// Performs synchronous HTTP POST.
        /// </summary>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="body">HTTP body or null.</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="req">Original HTTP request.</param>
        /// <returns>HTTP response.</returns>
        public Response POST(String relativeUri, String body, String customHeaders, Request req)
        {
            Byte[] bodyBytes = null;
            if (body != null)
                bodyBytes = Encoding.UTF8.GetBytes(body);

            return DoRESTRequestAndGetResponse("POST", relativeUri, customHeaders, bodyBytes, req, null, null);
        }

        /// <summary>
        /// Performs synchronous HTTP POST.
        /// </summary>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="body">HTTP body or null.</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="req">Original HTTP request.</param>
        /// <returns>HTTP response.</returns>
        public Response POST(String relativeUri, Byte[] bodyBytes, String customHeaders, Request req)
        {
            return DoRESTRequestAndGetResponse("POST", relativeUri, customHeaders, bodyBytes, req, null, null);
        }

        /// <summary>
        /// Performs asynchronous HTTP PUT.
        /// </summary>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="body">HTTP body or null.</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="req">Original HTTP request.</param>
        /// <param name="userObject">User object to be passed on response.</param>
        /// <param name="userDelegate">User delegate to be called on response.</param>
        public void PUT(String relativeUri, String body, String customHeaders, Request req, Object userObject, Func<Response, Object, Response> userDelegate)
        {
            Byte[] bodyBytes = null;
            if (body != null)
                bodyBytes = Encoding.UTF8.GetBytes(body);

            DoRESTRequestAndGetResponse("PUT", relativeUri, customHeaders, bodyBytes, req, userDelegate, userObject);
        }

        /// <summary>
        /// Performs asynchronous HTTP PUT.
        /// </summary>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="body">HTTP body or null.</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="req">Original HTTP request.</param>
        /// <param name="userObject">User object to be passed on response.</param>
        /// <param name="userDelegate">User delegate to be called on response.</param>
        public void PUT(String relativeUri, Byte[] bodyBytes, String customHeaders, Request req, Object userObject, Func<Response, Object, Response> userDelegate)
        {
            DoRESTRequestAndGetResponse("PUT", relativeUri, customHeaders, bodyBytes, req, userDelegate, userObject);
        }

        /// <summary>
        /// Performs synchronous HTTP PUT.
        /// </summary>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="body">HTTP body or null.</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="req">Original HTTP request.</param>
        /// <returns>HTTP response.</returns>
        public Response PUT(String relativeUri, String body, String customHeaders, Request req)
        {
            Byte[] bodyBytes = null;
            if (body != null)
                bodyBytes = Encoding.UTF8.GetBytes(body);

            return DoRESTRequestAndGetResponse("PUT", relativeUri, customHeaders, bodyBytes, req, null, null);
        }

        /// <summary>
        /// Performs synchronous HTTP PUT.
        /// </summary>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="body">HTTP body or null.</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="req">Original HTTP request.</param>
        /// <returns>HTTP response.</returns>
        public Response PUT(String relativeUri, Byte[] bodyBytes, String customHeaders, Request req)
        {
            return DoRESTRequestAndGetResponse("PUT", relativeUri, customHeaders, bodyBytes, req, null, null);
        }

        /// <summary>
        /// Performs asynchronous HTTP PATCH.
        /// </summary>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="body">HTTP body or null.</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="req">Original HTTP request.</param>
        /// <param name="userObject">User object to be passed on response.</param>
        /// <param name="userDelegate">User delegate to be called on response.</param>
        public void PATCH(String relativeUri, String body, String customHeaders, Request req, Object userObject, Func<Response, Object, Response> userDelegate)
        {
            Byte[] bodyBytes = null;
            if (body != null)
                bodyBytes = Encoding.UTF8.GetBytes(body);

            DoRESTRequestAndGetResponse("PATCH", relativeUri, customHeaders, bodyBytes, req, userDelegate, userObject);
        }

        /// <summary>
        /// Performs asynchronous HTTP PATCH.
        /// </summary>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="body">HTTP body or null.</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="req">Original HTTP request.</param>
        /// <param name="userObject">User object to be passed on response.</param>
        /// <param name="userDelegate">User delegate to be called on response.</param>
        public void PATCH(String relativeUri, Byte[] bodyBytes, String customHeaders, Request req, Object userObject, Func<Response, Object, Response> userDelegate)
        {
            DoRESTRequestAndGetResponse("PATCH", relativeUri, customHeaders, bodyBytes, req, userDelegate, userObject);
        }

        /// <summary>
        /// Performs synchronous HTTP PATCH.
        /// </summary>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="body">HTTP body or null.</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="req">Original HTTP request.</param>
        /// <returns>HTTP response.</returns>
        public Response PATCH(String relativeUri, String body, String customHeaders, Request req)
        {
            Byte[] bodyBytes = null;
            if (body != null)
                bodyBytes = Encoding.UTF8.GetBytes(body);

            return DoRESTRequestAndGetResponse("PATCH", relativeUri, customHeaders, bodyBytes, req, null, null);
        }

        /// <summary>
        /// Performs synchronous HTTP PATCH.
        /// </summary>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="body">HTTP body or null.</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="req">Original HTTP request.</param>
        /// <returns>HTTP response.</returns>
        public Response PATCH(String relativeUri, Byte[] bodyBytes, String customHeaders, Request req)
        {
            return DoRESTRequestAndGetResponse("PATCH", relativeUri, customHeaders, bodyBytes, req, null, null);
        }

        /// <summary>
        /// Performs asynchronous HTTP DELETE.
        /// </summary>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="body">HTTP body or null.</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="req">Original HTTP request.</param>
        /// <param name="userObject">User object to be passed on response.</param>
        /// <param name="userDelegate">User delegate to be called on response.</param>
        public void DELETE(String relativeUri, String body, String customHeaders, Request req, Object userObject, Func<Response, Object, Response> userDelegate)
        {
            Byte[] bodyBytes = null;
            if (body != null)
                bodyBytes = Encoding.UTF8.GetBytes(body);

            DoRESTRequestAndGetResponse("DELETE", relativeUri, customHeaders, bodyBytes, req, userDelegate, userObject);
        }

        /// <summary>
        /// Performs asynchronous HTTP DELETE.
        /// </summary>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="body">HTTP body or null.</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="req">Original HTTP request.</param>
        /// <param name="userObject">User object to be passed on response.</param>
        /// <param name="userDelegate">User delegate to be called on response.</param>
        public void DELETE(String relativeUri, Byte[] bodyBytes, String customHeaders, Request req, Object userObject, Func<Response, Object, Response> userDelegate)
        {
            DoRESTRequestAndGetResponse("DELETE", relativeUri, customHeaders, bodyBytes, req, userDelegate, userObject);
        }

        /// <summary>
        /// Performs synchronous HTTP DELETE.
        /// </summary>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="body">HTTP body or null.</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="req">Original HTTP request.</param>
        /// <returns>HTTP response.</returns>
        public Response DELETE(String relativeUri, String body, String customHeaders, Request req)
        {
            Byte[] bodyBytes = null;
            if (body != null)
                bodyBytes = Encoding.UTF8.GetBytes(body);

            return DoRESTRequestAndGetResponse("DELETE", relativeUri, customHeaders, bodyBytes, req, null, null);
        }

        /// <summary>
        /// Performs synchronous HTTP DELETE.
        /// </summary>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="body">HTTP body or null.</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="req">Original HTTP request.</param>
        /// <returns>HTTP response.</returns>
        public Response DELETE(String relativeUri, Byte[] bodyBytes, String customHeaders, Request req)
        {
            return DoRESTRequestAndGetResponse("DELETE", relativeUri, customHeaders, bodyBytes, req, null, null);
        }

        /// <summary>
        /// Performs asynchronous HTTP request with given HTTP method.
        /// </summary>
        /// <param name="method">HTTP method, e.g.: "OPTIONS", "HEAD".</param>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="body">HTTP body or null.</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="req">Original HTTP request.</param>
        /// <param name="userObject">User object to be passed on response.</param>
        /// <param name="userDelegate">User delegate to be called on response.</param>
        public void CustomRESTRequest(String method, String relativeUri, String body, String customHeaders, Request req, Object userObject, Func<Response, Object, Response> userDelegate)
        {
            Byte[] bodyBytes = null;
            if (body != null)
                bodyBytes = Encoding.UTF8.GetBytes(body);

            DoRESTRequestAndGetResponse(method, relativeUri, customHeaders, bodyBytes, req, userDelegate, userObject);
        }

        /// <summary>
        /// Performs asynchronous HTTP request with given HTTP method.
        /// </summary>
        /// <param name="method">HTTP method, e.g.: "OPTIONS", "HEAD".</param>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="body">HTTP body or null.</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="req">Original HTTP request.</param>
        /// <param name="userObject">User object to be passed on response.</param>
        /// <param name="userDelegate">User delegate to be called on response.</param>
        public void CustomRESTRequest(String method, String relativeUri, Byte[] bodyBytes, String customHeaders, Request req, Object userObject, Func<Response, Object, Response> userDelegate)
        {
            DoRESTRequestAndGetResponse(method, relativeUri, customHeaders, bodyBytes, req, userDelegate, userObject);
        }

        /// <summary>
        /// Performs synchronous HTTP request with given HTTP method.
        /// </summary>
        /// <param name="method">HTTP method, e.g.: "OPTIONS", "HEAD".</param>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="body">HTTP body or null.</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="req">Original HTTP request.</param>
        /// <returns>HTTP response.</returns>
        public Response CustomRESTRequest(String method, String relativeUri, String body, String customHeaders, Request req)
        {
            Byte[] bodyBytes = null;
            if (body != null)
                bodyBytes = Encoding.UTF8.GetBytes(body);

            return DoRESTRequestAndGetResponse(method, relativeUri, customHeaders, bodyBytes, req, null, null);
        }

        /// <summary>
        /// Performs synchronous HTTP request with given HTTP method.
        /// </summary>
        /// <param name="method">HTTP method, e.g.: "OPTIONS", "HEAD".</param>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="body">HTTP body or null.</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="req">Original HTTP request.</param>
        /// <returns>HTTP response.</returns>
        public Response CustomRESTRequest(String method, String relativeUri, Byte[] bodyBytes, String customHeaders, Request req)
        {
            return DoRESTRequestAndGetResponse(method, relativeUri, customHeaders, bodyBytes, req, null, null);
        }

        /// <summary>
        /// Performs asynchronous HTTP request with given HTTP method.
        /// </summary>
        /// <param name="method">HTTP method, e.g.: "OPTIONS", "HEAD".</param>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="body">HTTP body or null.</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="req">Original HTTP request.</param>
        /// <param name="userObject">User object to be passed on response.</param>
        /// <param name="userDelegate">User delegate to be called on response.</param>
        public void CustomRESTRequest(Request req, Object userObject, Func<Response, Object, Response> userDelegate)
        {
            if (null != req.Body)
            {
                Byte[] bodyBytes = Encoding.UTF8.GetBytes(req.Body);
                DoRESTRequestAndGetResponse(req.Method, req.Uri, req.Headers, bodyBytes, req, userDelegate, userObject);
                return;
            }

            DoRESTRequestAndGetResponse(req.Method, req.Uri, req.Headers, req.BodyBytes, req, userDelegate, userObject);
        }

        /// <summary>
        /// Performs synchronous HTTP request with given HTTP method.
        /// </summary>
        /// <param name="method">HTTP method, e.g.: "OPTIONS", "HEAD".</param>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="body">HTTP body or null.</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="req">Original HTTP request.</param>
        /// <returns>HTTP response.</returns>
        public Response CustomRESTRequest(Request req)
        {
            if (null != req.Body)
            {
                Byte[] bodyBytes = Encoding.UTF8.GetBytes(req.Body);
                return DoRESTRequestAndGetResponse(req.Method, req.Uri, req.Headers, bodyBytes, req, null, null);
            }

            return DoRESTRequestAndGetResponse(req.Method, req.Uri, req.Headers, req.BodyBytes, req, null, null);
        }

        /// <summary>
        /// Performs local node REST.
        /// </summary>
        /// <param name="methodAndUriPlusSpace">Method and URI plus space at the end.</param>
        /// <param name="requestBytes">Bytes that contain the HTTP request.</param>
        /// <param name="resp">HTTP response which is an answer on given request.</param>
        /// <returns>True if handled.</returns>
        Boolean DoLocalNodeRest(String methodAndUriPlusSpace, Byte[] requestBytes, out Response resp)
        {
            resp = null;

            // Checking if local RESTing is initialized.
            if (!UserHandlerCodegen.HandlersManager.IsSupportingLocalNodeResting())
                return false;

            // Checking if port is initialized.
            PortUris portUris = UserHandlerCodegen.HandlersManager.SearchPort(portNumber_);
            if (portUris == null)
                portUris = UserHandlerCodegen.HandlersManager.AddPort(portNumber_);

            // Calling the code generation for URIs if needed.
            if (null == portUris.MatchUriAndGetHandlerId)
            {
                if (!portUris.GenerateUriMatcher(
                    portNumber_,
                    UserHandlerCodegen.HandlersManager.AllUserHandlerInfos,
                    UserHandlerCodegen.HandlersManager.NumRegisteredHandlers,
                    unitTests_))
                {
                    return false;
                }
            }

            // Calling the generated URI matcher.
            Int32 handler_id = -1;
            unsafe
            {
                // Allocating space for parameter information.
                Byte* native_params_bytes = stackalloc Byte[MixedCodeConstants.PARAMS_INFO_MAX_SIZE_BYTES];
                MixedCodeConstants.UserDelegateParamInfo* native_params = (MixedCodeConstants.UserDelegateParamInfo*)native_params_bytes;
                MixedCodeConstants.UserDelegateParamInfo** native_params_addr = &native_params;

                fixed (Byte* p = requestBytes)
                {
                    // TODO: Resolve this hack with only positive handler ids in generated code.
                    handler_id = portUris.MatchUriAndGetHandlerId(p, (UInt32)methodAndUriPlusSpace.Length, native_params_addr) - 1;
                }

                // Checking if we have found the handler.
                if (handler_id >= 0)
                {
                    // Creating HTTP request.
                    Request req = new Request(requestBytes, native_params_bytes);
                    req.HandlerId = (UInt16)handler_id;
                    req.MethodEnum = UserHandlerCodegen.HandlersManager.AllUserHandlerInfos[handler_id].UriInfo.http_method_;

                    // Invoking original user delegate with parameters here.
                    resp = UserHandlerCodegen.HandlersManager.HandleInternalRequest_(req);

                    // Parsing the response.
                    resp.ParseResponseFromUncompressed();

                    // Request successfully handled.
                    return true;
                }
            }

            return false;
        }

        ~Node()
        {
            if (core_task_info_.IsConnectionEstablished())
                core_task_info_.Close();
        }

        /// <summary>
        /// Frees network streams.
        /// </summary>
        internal void FreeConnection(NodeTask conn, Boolean fromSyncRequest = false)
        {
            // Checking if we are called from sync request.
            if (!fromSyncRequest)
            {
                // Attaching the connection since it could already be reconnected.
                core_task_info_.AttachConnection(conn.TcpClientObj);

                lock (conn.NodeInst.LockObj)
                {
                    // Popping from pending async calls.
                    NodeTask task = pending_async_tasks_.Dequeue();
                    Debug.Assert(task == conn, "Dequeued async call is different!");

                    // Pushing to finished queue.
                    finished_async_tasks_.Enqueue(conn);
                }
            }

            // Checking if there are other pending async calls.
            if (pending_async_tasks_.Count > 0)
            {
                lock (conn.NodeInst.LockObj)
                {
                    if (pending_async_tasks_.Count > 0)
                    {
                        // Taking the oldest task.
                        NodeTask oldest_queue_task = pending_async_tasks_.Peek();

                        // Attaching the "fresh" connection.
                        oldest_queue_task.AttachConnection(conn.TcpClientObj);

                        // Starting another async request.
                        oldest_queue_task.PerformAsyncRequest();
                    }
                }
            }
        }

        /// <summary>
        /// Simply calls given user delegate.
        /// </summary>
        /// <param name="origReq"></param>
        /// <param name="resp"></param>
        /// <param name="userDelegate"></param>
        internal static void CallUserDelegate(Request origReq, Response resp, Func<Response, Object, Response> userDelegate, Object userObject)
        {
            // Invoking user delegate.
            Response userResp = userDelegate.Invoke(resp, userObject);

            // Checking if we have HTTP request.
            if (origReq != null)
            {
                Response respOnResp = HandleResponse_(origReq, userResp);
                origReq.SendResponse(respOnResp.ResponseBytes, 0, respOnResp.ResponseLength);
            }
        }

        /// <summary>
        /// Core function to send REST requests and get the responses.
        /// </summary>
        /// <param name="method">HTTP method.</param>
        /// <param name="relativeUri">Relative URI.</param>
        /// <param name="customHeaders">Custom HTTP headers if any.</param>
        /// <param name="body">HTTP Body string or null if no such.</param>
        /// <param name="origReq">Original HTTP request or null if no such.</param>
        /// <param name="func">User delegate to be called.</param>
        Response DoRESTRequestAndGetResponse(
            String method,
            String relativeUri,
            String customHeaders,
            Byte[] bodyBytes,
            Request origReq,
            Func<Response, Object, Response> userDelegate,
            Object userObject)
        {
            // TODO: Inject data instead of constructing new.

            // Constructing request headers.
            String methodAndUriPlusSpace = method + " " + relativeUri + " ";

            String headers = methodAndUriPlusSpace + "HTTP/1.1" + StarcounterConstants.NetworkConstants.CRLF +
                "Host: " + hostName_ + StarcounterConstants.NetworkConstants.CRLF;

            if (customHeaders != null)
            {
                // Checking for correct custom headers format.
                if (!customHeaders.EndsWith("\r\n"))
                    throw new ArgumentException("Each custom header should be in following form: \"<HeaderName>:<space><value>\\r\\n\" For example: \"MyNewHeader: value123\\r\\n\"");

                headers += customHeaders;
            }

            if (bodyBytes != null)
                headers += "Content-Length: " + bodyBytes.Length + StarcounterConstants.NetworkConstants.CRLF;
            else
                headers += "Content-Length: 0" + StarcounterConstants.NetworkConstants.CRLF;

            headers += StarcounterConstants.NetworkConstants.CRLF;

            // Converting headers to ASCII bytes.
            Byte[] headersBytes = Encoding.ASCII.GetBytes(headers);

            Byte[] requestBytes;

            // Adding body if needed.
            if (bodyBytes != null)
            {
                // Concatenating the arrays.
                requestBytes = new Byte[headersBytes.Length + bodyBytes.Length];
                System.Buffer.BlockCopy(headersBytes, 0, requestBytes, 0, headersBytes.Length);
                System.Buffer.BlockCopy(bodyBytes, 0, requestBytes, headersBytes.Length, bodyBytes.Length);
            }
            else
            {
                requestBytes = headersBytes;
            }

            // No response initially.
            Response resp = null;

            // Checking if we are on local node.
            if (localNode_)
            {
                // Trying to do local node REST.
                if (DoLocalNodeRest(methodAndUriPlusSpace, requestBytes, out resp))
                {
                    // Checking if user has supplied a delegate to be called.
                    if (null != userDelegate)
                    {
                        // Invoking user delegate.
                        CallUserDelegate(origReq, resp, userDelegate, userObject);

                        return null;
                    }

                    return resp;
                }
            }

            // Checking if user has supplied a delegate to be called.
            if (null != userDelegate)
            {
                NodeTask task = null;

                // Checking if any tasks are finished.
                if (finished_async_tasks_.Count > 0)
                {
                    lock (LockObj)
                    {
                        if (finished_async_tasks_.Count > 0)
                            task = finished_async_tasks_.Dequeue();
                    }
                }
                else
                {
                    // Checking if we exceeded the maximum number of pending tasks.
                    if (pending_async_tasks_.Count >= NodeTask.MaxNumPendingAsyncTasks)
                    {
                        while (finished_async_tasks_.Count <= 0)
                            Thread.Sleep(1);

                        lock (LockObj)
                        {
                            if (finished_async_tasks_.Count > 0)
                                task = finished_async_tasks_.Dequeue();
                        }

                        // throw new Exception("Too many active Node connections. Maximum allowed number is " + ActiveConn.MaxNumConnections);
                    }
                }

                // Checking if any empty tasks was dequeued.
                if (null == task)
                    task = new NodeTask(this);

                // Initializing connection.
                task.Reset(requestBytes, origReq, userDelegate, userObject);

                // Checking if we already have an active connection.
                if (core_task_info_.IsConnectionEstablished())
                    task.AttachConnection(core_task_info_.TcpClientObj);

                lock (LockObj)
                {
                    // Putting to async queue.
                    pending_async_tasks_.Enqueue(task);

                    // Starting asynchronous network operations if no one is in queue already.
                    if (1 == pending_async_tasks_.Count)
                        task.PerformAsyncRequest();
                }

                return null;
            }

            // Checking if there are any pending async operations.
            while (pending_async_tasks_.Count > 0)
                Thread.Sleep(1);

            // Initializing connection.
            core_task_info_.Reset(requestBytes, origReq, userDelegate, userObject);

            // Checking if its the first time, then creating new connection.
            if (!core_task_info_.IsConnectionEstablished())
                core_task_info_.AttachConnection(null);

            // Doing synchronous request and returning response.
            return core_task_info_.PerformSyncRequest();
        }
    }
}