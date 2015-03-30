#define CASE_INSENSITIVE_URI_MATCHER

// ***********************************************************************
// <copyright file="Node.cs" company="Starcounter AB">
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
using System.Runtime.InteropServices;

namespace Starcounter
{
    public class Node
    {
        Boolean ShouldLogErrors_;

        /// <summary>
        /// Indicates if Node errors should be logged.
        /// </summary>
        public Boolean ShouldLogErrors
        {
            get { return ShouldLogErrors_; }

            set
            {
                // Checking if we are running inside Starcounter.
                if (!StarcounterEnvironment.IsCodeHosted)
                    throw new ArgumentException("Node is not running inside Starcounter to log errors.");

                ShouldLogErrors_ = value;
            }
        }

        /// <summary>
        /// Maximum number of parallel sockets.
        /// </summary>
        Int32 maxNumAsyncConnections_ = 128;

        /// <summary>
        /// Maximum number of parallel connections in async node.
        /// </summary>
        public Int32 MaxNumAsyncConnections {
            get {
                return maxNumAsyncConnections_;
            }

            set {
                maxNumAsyncConnections_ = value;
            }
        }

        /// <summary>
        /// Connect synchronously.
        /// </summary>
        Boolean connectSynchronuously_ = false;

        /// <summary>
        /// Indicates if synchronous connect should be performed.
        /// </summary>
        public Boolean ConnectSynchronuously {
            get {
                return connectSynchronuously_;
            }

            set {
                connectSynchronuously_ = value;
            }
        }

        /// <summary>
        /// Performs local Node REST call.
        /// </summary>
        static RunUriMatcherAndCallHandlerDelegate runUriMatcherAndCallHandler_;

        /// <summary>
        /// Pending async tasks.
        /// </summary>
        LockFreeQueue<NodeTask> aggr_pending_async_tasks_ = new LockFreeQueue<NodeTask>();

        /// <summary>
        /// The Node log source for logging exceptions.
        /// </summary>
        internal static Action<Exception> nodeLogException_;

        /// <summary>
        /// Initializes Node implementation.
        /// </summary>
        internal static void InjectHostedImpl(
            RunUriMatcherAndCallHandlerDelegate runUriMatcherAndCallHandler,
            Action<Exception> nodeLogException)
        {
            runUriMatcherAndCallHandler_ = runUriMatcherAndCallHandler;
            nodeLogException_ = nodeLogException;
        }

        // Trying to set a SIO_LOOPBACK_FAST_PATH on a TCP socket.
        internal static void SetLoopbackFastPathOnTcpSocket(Socket sock) {

            // NOTE: Tries to configure a TCP socket for lower latency and faster operations on the loopback interface.
            try {
                const int SIO_LOOPBACK_FAST_PATH = (-1744830448);

                Byte[] OptionInValue = BitConverter.GetBytes(1);

                sock.IOControl(
                    SIO_LOOPBACK_FAST_PATH,
                    OptionInValue,
                    null);
            } catch {
                // Simply ignoring the error if fast loopback is not supported.
            }
        }

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
        /// Static constructor to automatically initialize REST.
        /// </summary>
        static Node()
        {
            // Pre-loading non GAC assemblies and setting assembly resolvers.
            HelperFunctions.PreLoadCustomDependencies();

            // Initializes HTTP parser.
            Request.sc_init_http_parser();
        }

        /// <summary>
        /// Indicates the local node.
        /// </summary>
        Boolean isLocalNode_ = false;

        /// <summary>
        /// Indicates that this node is restricted to this codehost.
        /// </summary>
        internal Boolean IsLocalNode
        {
            get { return isLocalNode_; }
            set { isLocalNode_ = value; }
        }

        /// <summary>
        /// Host name of this node e.g.: www.starcounter.com, 192.168.0.1
        /// </summary>
        String hostName_;

        /// <summary>
        /// HTTP port number, e.g.: 80
        /// </summary>
        UInt16 portNumber_;

        /// <summary>
        /// Receive timeout in milliseconds.
        /// </summary>
        public Int32 DefaultReceiveTimeoutMs { get; set; }

        /// <summary>
        /// Aggregation port number, e.g.: 1234
        /// </summary>
        UInt16 aggrPortNumber_;

        /// <summary>
        /// Aggregation TCP socket.
        /// </summary>
        Socket aggrSocket_;

        /// <summary>
        /// Disconnecting aggregation socket.
        /// </summary>
        public void StopAggregation() {
            if (UsesAggregation()) {
                aggrSocket_.Disconnect(false);
            }
        }

        /// <summary>
        /// Returns True if this node uses aggregation.
        /// </summary>
        /// <returns></returns>
        public Boolean UsesAggregation()
        {
            return (null != aggrSocket_);
        }

        /// <summary>
        /// Finished async tasks.
        /// </summary>
        LockFreeQueue<NodeTask> finished_async_tasks_ = new LockFreeQueue<NodeTask>();

        /// <summary>
        /// Free task indexes.
        /// </summary>
        LockFreeQueue<Int32> free_task_indexes_ = new LockFreeQueue<Int32>();

        /// <summary>
        /// Aggregation awaiting tasks array.
        /// </summary>
        NodeTask[] aggregation_awaiting_tasks_array_;

        /// <summary>
        /// Number of created tasks.
        /// </summary>
        Int32 num_tasks_created_ = 0;

        /// <summary>
        /// Node synchronous task.
        /// </summary>
        NodeTask sync_task_info_ = null;

        /// <summary>
        /// Delegate to process the results of calling user delegate.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        internal delegate Response HandleResponse(Request request, Response x);

        /// <summary>
        /// Delegate to run URI matcher and call handler.
        /// </summary>
        internal delegate Boolean RunUriMatcherAndCallHandlerDelegate(
            String methodAndUriPlusSpace,
            String methodAndUriPlusSpaceLower,
            Request req,
            UInt16 portNumber,
            out Response resp);

        /// <summary>
        /// Returns this node port number.
        /// </summary>
        public UInt16 PortNumber { get { return portNumber_; } }

        /// <summary>
        /// Endpoint string for this node.
        /// </summary>
        String endpoint_;

        /// <summary>
        /// Endpoint consists of IP and port separated by colon.
        /// </summary>
        public String Endpoint { get { return endpoint_; } }

        /// <summary>
        /// Returns this node port number.
        /// </summary>
        public UInt16 AggrPortNumber { get { return aggrPortNumber_; } }

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
        public Node(
            String hostName,
            UInt16 portNumber = StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort,
            Int32 defaultReceiveTimeoutMs = 0,
            Boolean useAggregation = false,
            UInt16 aggrPortNumber = StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort)
        {
            if (hostName == null) {

                // Checking if we are running inside Starcounter hosting process.
                if (StarcounterEnvironment.IsCodeHosted)
                    isLocalNode_ = true;

                hostName = "localhost";
            }

            // Checking if port is not specified.
            if (StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort == portNumber) {
                if (StarcounterEnvironment.IsAdministratorApp) {
                    portNumber = StarcounterEnvironment.Default.SystemHttpPort;
                } else {
                    portNumber = StarcounterEnvironment.Default.UserHttpPort;
                }
            }

            aggrPortNumber_ = aggrPortNumber;

            if (StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort == aggrPortNumber_) {
                aggrPortNumber_ = StarcounterEnvironment.Default.AggregationPort;
            }

            hostName_ = hostName;
            portNumber_ = portNumber;
            endpoint_ = hostName + ":" + portNumber;

            DefaultReceiveTimeoutMs = defaultReceiveTimeoutMs;

            sync_task_info_ = new NodeTask(this);
            num_tasks_created_ = 1;
           
            // Checking that code is not hosted in Starcounter.
            if (StarcounterEnvironment.IsCodeHosted)
                if (useAggregation)
                    throw new Exception("Aggregation can't be used on hosted node.");

            // Initializing aggregation struct.
            if (useAggregation)
            {
                // When aggregating we need more NodeTasks for better network utilization.
                maxNumAsyncConnections_ = 1024;

                aggrSocket_ = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                // Trying to set a SIO_LOOPBACK_FAST_PATH on a TCP socket.
                Node.SetLoopbackFastPathOnTcpSocket(aggrSocket_);

                aggrSocket_.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, 1 << 19);
                aggrSocket_.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, 1 << 19);
                
                aggrSocket_.Connect(hostName_, aggrPortNumber_);

                aggregate_send_blob_ = new Byte[AggregationBlobSizeBytes];
                aggregate_receive_blob_ = new Byte[AggregationBlobSizeBytes];
                aggregation_awaiting_tasks_array_ = new NodeTask[NodeTask.MaxNumPendingAggregatedTasks];
                for (Int32 i = 0; i < NodeTask.MaxNumPendingAggregatedTasks; i++)
                    free_task_indexes_.Enqueue(i);

                this_node_aggr_struct_.port_number_ = portNumber_;

                // Starting send and receive threads.
                (new Thread(new ThreadStart(AggregateSendThread))).Start();
                (new Thread(new ThreadStart(AggregateReceiveThread))).Start();

                Boolean nodeAggrInitialized = false;

                // Requesting socket creation.
                DoAsyncTransfer(null, null, (AggregationStruct aggr_struct) => {
                    unsafe { this_node_aggr_struct_ = aggr_struct; }
                    nodeAggrInitialized = true;
                }, null, 0);

                // Waiting for socket creation result.
                Int32 numRetries = 5000;
                while (!nodeAggrInitialized) {
                    Thread.Sleep(1);
                    numRetries--;
                    if (0 == numRetries)
                        throw new Exception("Can't create aggregation socket!");
                }
            }
        }

        /// <summary>
        /// Performs asynchronous HTTP GET.
        /// </summary>
        public void GET(String relativeUri, Dictionary<String, String> headersDictionary, Object userObject, Action<Response, Object> userDelegate, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            DoRESTRequestAndGetResponse("GET", relativeUri, headersDictionary, null, userDelegate, userObject, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP GET.
        /// </summary>
        public Response GET(String relativeUri, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            return DoRESTRequestAndGetResponse("GET", relativeUri, null, null, null, null, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP GET.
        /// </summary>
        public Response GET(String relativeUri, Dictionary<String, String> headersDictionary, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            return DoRESTRequestAndGetResponse("GET", relativeUri, headersDictionary, null, null, null, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP POST.
        /// </summary>
        public void POST(String relativeUri, String body, Dictionary<String, String> headersDictionary, Object userObject, Action<Response, Object> userDelegate, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            Byte[] bodyBytes = null;
            if (body != null)
                bodyBytes = Encoding.UTF8.GetBytes(body);

            DoRESTRequestAndGetResponse("POST", relativeUri, headersDictionary, bodyBytes, userDelegate, userObject, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP POST.
        /// </summary>
        public void POST(String relativeUri, Byte[] bodyBytes, Dictionary<String, String> headersDictionary, Object userObject, Action<Response, Object> userDelegate, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            DoRESTRequestAndGetResponse("POST", relativeUri, headersDictionary, bodyBytes, userDelegate, userObject, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP POST.
        /// </summary>
        public Response POST(String relativeUri, String body, Dictionary<String, String> headersDictionary, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            Byte[] bodyBytes = null;
            if (body != null)
                bodyBytes = Encoding.UTF8.GetBytes(body);

            return DoRESTRequestAndGetResponse("POST", relativeUri, headersDictionary, bodyBytes, null, null, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP POST.
        /// </summary>
        public Response POST(String relativeUri, Byte[] bodyBytes, Dictionary<String, String> headersDictionary, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            return DoRESTRequestAndGetResponse("POST", relativeUri, headersDictionary, bodyBytes, null, null, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP PUT.
        /// </summary>
        public void PUT(String relativeUri, String body, Dictionary<String, String> headersDictionary, Object userObject, Action<Response, Object> userDelegate, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            Byte[] bodyBytes = null;
            if (body != null)
                bodyBytes = Encoding.UTF8.GetBytes(body);

            DoRESTRequestAndGetResponse("PUT", relativeUri, headersDictionary, bodyBytes, userDelegate, userObject, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP PUT.
        /// </summary>
        public void PUT(String relativeUri, Byte[] bodyBytes, Dictionary<String, String> headersDictionary, Object userObject, Action<Response, Object> userDelegate, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            DoRESTRequestAndGetResponse("PUT", relativeUri, headersDictionary, bodyBytes, userDelegate, userObject, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP PUT.
        /// </summary>
        public Response PUT(String relativeUri, String body, Dictionary<String, String> headersDictionary, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            Byte[] bodyBytes = null;
            if (body != null)
                bodyBytes = Encoding.UTF8.GetBytes(body);

            return DoRESTRequestAndGetResponse("PUT", relativeUri, headersDictionary, bodyBytes, null, null, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP PUT.
        /// </summary>
        public Response PUT(String relativeUri, Byte[] bodyBytes, Dictionary<String, String> headersDictionary, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            return DoRESTRequestAndGetResponse("PUT", relativeUri, headersDictionary, bodyBytes, null, null, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP PATCH.
        /// </summary>
        public void PATCH(String relativeUri, String body, Dictionary<String, String> headersDictionary, Object userObject, Action<Response, Object> userDelegate, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            Byte[] bodyBytes = null;
            if (body != null)
                bodyBytes = Encoding.UTF8.GetBytes(body);

            DoRESTRequestAndGetResponse("PATCH", relativeUri, headersDictionary, bodyBytes, userDelegate, userObject, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP PATCH.
        /// </summary>
        public void PATCH(String relativeUri, Byte[] bodyBytes, Dictionary<String, String> headersDictionary, Object userObject, Action<Response, Object> userDelegate, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            DoRESTRequestAndGetResponse("PATCH", relativeUri, headersDictionary, bodyBytes, userDelegate, userObject, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP PATCH.
        /// </summary>
        public Response PATCH(String relativeUri, String body, Dictionary<String, String> headersDictionary, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            Byte[] bodyBytes = null;
            if (body != null)
                bodyBytes = Encoding.UTF8.GetBytes(body);

            return DoRESTRequestAndGetResponse("PATCH", relativeUri, headersDictionary, bodyBytes, null, null, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP PATCH.
        /// </summary>
        public Response PATCH(String relativeUri, Byte[] bodyBytes, Dictionary<String, String> headersDictionary, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            return DoRESTRequestAndGetResponse("PATCH", relativeUri, headersDictionary, bodyBytes, null, null, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP DELETE.
        /// </summary>
        public void DELETE(String relativeUri, String body, Dictionary<String, String> headersDictionary, Object userObject, Action<Response, Object> userDelegate, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            Byte[] bodyBytes = null;
            if (body != null)
                bodyBytes = Encoding.UTF8.GetBytes(body);

            DoRESTRequestAndGetResponse("DELETE", relativeUri, headersDictionary, bodyBytes, userDelegate, userObject, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP DELETE.
        /// </summary>
        public void DELETE(String relativeUri, Byte[] bodyBytes, Dictionary<String, String> headersDictionary, Object userObject, Action<Response, Object> userDelegate, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            DoRESTRequestAndGetResponse("DELETE", relativeUri, headersDictionary, bodyBytes, userDelegate, userObject, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP DELETE.
        /// </summary>
        public Response DELETE(String relativeUri, String body, Dictionary<String, String> headersDictionary, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            Byte[] bodyBytes = null;
            if (body != null)
                bodyBytes = Encoding.UTF8.GetBytes(body);

            return DoRESTRequestAndGetResponse("DELETE", relativeUri, headersDictionary, bodyBytes, null, null, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP DELETE.
        /// </summary>
        public Response DELETE(String relativeUri, Byte[] bodyBytes, Dictionary<String, String> headersDictionary, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            return DoRESTRequestAndGetResponse("DELETE", relativeUri, headersDictionary, bodyBytes, null, null, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP request with given HTTP method.
        /// </summary>
        public void CustomRESTRequest(String method, String relativeUri, String body, Dictionary<String, String> headersDictionary, Object userObject, Action<Response, Object> userDelegate, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            Byte[] bodyBytes = null;
            if (body != null)
                bodyBytes = Encoding.UTF8.GetBytes(body);

            DoRESTRequestAndGetResponse(method, relativeUri, headersDictionary, bodyBytes, userDelegate, userObject, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP request with given HTTP method.
        /// </summary>
        public void CustomRESTRequest(String method, String relativeUri, Byte[] bodyBytes, Dictionary<String, String> headersDictionary, Object userObject, Action<Response, Object> userDelegate, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            DoRESTRequestAndGetResponse(method, relativeUri, headersDictionary, bodyBytes, userDelegate, userObject, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP request with given HTTP method.
        /// </summary>
        public Response CustomRESTRequest(String method, String relativeUri, String body, Dictionary<String, String> headersDictionary, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            Byte[] bodyBytes = null;
            if (body != null)
                bodyBytes = Encoding.UTF8.GetBytes(body);

            return DoRESTRequestAndGetResponse(method, relativeUri, headersDictionary, bodyBytes, null, null, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP request with given HTTP method.
        /// </summary>
        public Response CustomRESTRequest(String method, String relativeUri, Byte[] bodyBytes, Dictionary<String, String> headersDictionary, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            return DoRESTRequestAndGetResponse(method, relativeUri, headersDictionary, bodyBytes, null, null, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP request with given HTTP method.
        /// </summary>
        public void CustomRESTRequest(Request req, Object userObject, Action<Response, Object> userDelegate, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            DoRESTRequestAndGetResponse(
                req.Method,
                req.Uri,
                null,
                null,
                userDelegate,
                userObject,
                receiveTimeoutMs,
                ho,
                req);
        }

        /// <summary>
        /// Performs synchronous HTTP request with given HTTP method.
        /// </summary>
        public Response CustomRESTRequest(Request req, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            return DoRESTRequestAndGetResponse(
                req.Method,
                req.Uri,
                null,
                null,
                null,
                null,
                receiveTimeoutMs,
                ho,
                req);
        }

        /// <summary>
        /// Frees network streams.
        /// </summary>
        internal void FreeConnection(NodeTask nt, Boolean isSyncCall)
        {
            // Checking if we are called from async request.
            if (!isSyncCall)
            {
                // Pushing to finished queue.
                finished_async_tasks_.Enqueue(nt);
            }
        }

        /// <summary>
        /// Simply calls given user delegate.
        /// </summary>
        internal void CallUserDelegate(
            Response resp,
            Action<Response, Object> userDelegate,
            Object userObject,
            Byte boundSchedulerId)
        {
            try
            {
                // Invoking user delegate either on the same scheduler if inside Starcounter.
                if (StarcounterEnvironment.IsCodeHosted)
                {
                    StarcounterBase._DB.RunAsync(() => { userDelegate.Invoke(resp, userObject); }, boundSchedulerId);
                }
                else
                {
                    userDelegate.Invoke(resp, userObject);
                }
            }
            catch (Exception exc)
            {
                // Checking if exception should be logged.
                if (ShouldLogErrors_)
                    Node.nodeLogException_(exc);
                else
                    throw exc;
            }
        }

        /// <summary>
        /// Perform asynchronous REST.
        /// </summary>
        void DoAsyncTransfer(
            Request req,
            Action<Response, Object> userDelegate,
            Action<AggregationStruct> aggrMsgDelegate = null,
            Object userObject = null,
            Int32 receiveTimeoutMs = 0)
        {
            // Setting the receive timeout.
            if (0 == receiveTimeoutMs)
                receiveTimeoutMs = DefaultReceiveTimeoutMs;

            NodeTask nt = null;

            // Checking if any tasks are finished.
            if (!finished_async_tasks_.Dequeue(out nt)) {

                // Checking if we exceeded the maximum number of created tasks.
                if (num_tasks_created_ >= maxNumAsyncConnections_) {

                    // Looping until task is dequeued.
                    while (!finished_async_tasks_.Dequeue(out nt)) {
                        Thread.Sleep(1);
                    }
                }
            }

            // Checking if any empty tasks was dequeued.
            if (null == nt) {
                Interlocked.Increment(ref num_tasks_created_);
                nt = new NodeTask(this);
            }

            // Getting current scheduler number if in Starcounter.
            Byte currentSchedulerId = 0;
            if (StarcounterEnvironment.IsCodeHosted)
                currentSchedulerId = StarcounterEnvironment.CurrentSchedulerId;

            // Initializing connection.
            nt.ResetButKeepSocket(
                req,
                userDelegate,
                aggrMsgDelegate,
                userObject,
                receiveTimeoutMs,
                currentSchedulerId);

            // Checking if we don't use aggregation.
            if (!UsesAggregation()) {
                // Starting async request on that node task.
                nt.PerformAsyncRequest();
            } else {
                // Putting to aggregation queue.
                aggr_pending_async_tasks_.Enqueue(nt);
            }
        }

        /// <summary>
        /// Core function to send REST requests and get the responses.
        /// </summary>
        public Response DoRESTRequestAndGetResponse(
            String method,
            String relativeUri,
            Dictionary<String, String> headersDictionary,
            Byte[] bodyBytes,
            Action<Response, Object> userDelegate,
            Object userObject,
            Int32 receiveTimeoutMs,
            HandlerOptions handlerOptions,
            Request req = null)
        {
            Boolean callOnlySpecificHandlerLevel = true;

            // Checking if handler options is defined.
            if (handlerOptions == null) {

                handlerOptions = new HandlerOptions();

                callOnlySpecificHandlerLevel = false;
            }

            // Setting application name.
            handlerOptions.CallingAppName = StarcounterEnvironment.AppName;

            // Creating the request object if it does not exist.
            if (req == null) {

                req = new Request() {

                    Method = method,
                    Uri = relativeUri,
                    BodyBytes = bodyBytes,
                    HeadersDictionary = headersDictionary,
                    Host = Endpoint
                };
            }

            // Checking if we are on local node.
            if ((isLocalNode_) && (!handlerOptions.CallExternalOnly)) {

                String methodSpaceUriSpace = method + " " + relativeUri + " ";
                String methodSpaceUriSpaceLower = methodSpaceUriSpace;

#if CASE_INSENSITIVE_URI_MATCHER

                // Making incoming URI lower case.
                methodSpaceUriSpaceLower = method + " " + relativeUri.ToLowerInvariant() + " ";
#endif

DO_CALL_ON_GIVEN_LEVEL:

                // Setting handler options.
                req.HandlerOpts = handlerOptions;

                // No response initially.
                Response resp = null;

                // Running URI matcher and call handler.
                Boolean handlerFound = runUriMatcherAndCallHandler_(
                    methodSpaceUriSpace,
                    methodSpaceUriSpaceLower,
                    req,
                    portNumber_,
                    out resp);

                // Checking if there is some response.
                if (resp != null) {

                    // Checking if user has supplied a delegate to be called.
                    if (null != userDelegate) {

                        // Invoking user delegate.
                        userDelegate.Invoke(resp, userObject);

                        return null;
                    }

                    return resp;
                }
            
                // Going level by level up.
                if (!handlerFound) {

                    if (false == callOnlySpecificHandlerLevel) {

                        switch (handlerOptions.HandlerLevel) {

                            case HandlerOptions.HandlerLevels.DefaultLevel: {
                                handlerOptions.HandlerLevel = HandlerOptions.HandlerLevels.ApplicationLevel;
                                goto DO_CALL_ON_GIVEN_LEVEL;
                            }

                            case HandlerOptions.HandlerLevels.ApplicationLevel: {
                                handlerOptions.HandlerLevel = HandlerOptions.HandlerLevels.ApplicationExtraLevel;
                                goto DO_CALL_ON_GIVEN_LEVEL;
                            }

                            case HandlerOptions.HandlerLevels.ApplicationExtraLevel: {
                                handlerOptions.HandlerLevel = HandlerOptions.HandlerLevels.CodeHostStaticFileServer;
                                goto DO_CALL_ON_GIVEN_LEVEL;
                            }
                        };
                    }

                    // Checking if there is a substitute handler.
                    if (req.HandlerOpts.SubstituteHandler != null) {

                        resp = req.HandlerOpts.SubstituteHandler();

                        if (resp != null) {

                            // Setting the response application name.
                            resp.AppName = req.HandlerOpts.CallingAppName;

                            if (StarcounterEnvironment.PolyjuiceAppsFlag)
                                return Response.ResponsesMergerRoutine_(req, resp, null);
                        }

                        return resp;
                    }

                    if (true == callOnlySpecificHandlerLevel) {

                        // NOTE: We tried a specific handler level but didn't get any response, so returning.
                        return null;
                    }
                }

                return null;
            }

            // Setting the receive timeout.
            if (0 == receiveTimeoutMs) {
                receiveTimeoutMs = DefaultReceiveTimeoutMs;
            }

            // Checking if user has supplied a delegate to be called.
            if (null != userDelegate) {

                // Trying to perform an async request.
                DoAsyncTransfer(req, userDelegate, null, userObject, receiveTimeoutMs);

                return null;
            }

            lock (finished_async_tasks_) {

                // Initializing connection.
                sync_task_info_.ResetButKeepSocket(req, null, null, userObject, receiveTimeoutMs, 0);

                // Doing synchronous request and returning response.
                return sync_task_info_.PerformSyncRequest();
            }
        }

        /// <summary>
        /// Size of aggregation blob.
        /// </summary>
        const Int32 AggregationBlobSizeBytes = 4 * 1024 * 1024;

        /// <summary>
        /// Bytes blob used for aggregated sends.
        /// </summary>
        Byte[] aggregate_send_blob_;

        /// <summary>
        /// Bytes blob used for aggregated receives.
        /// </summary>
        Byte[] aggregate_receive_blob_;

        [StructLayout(LayoutKind.Sequential)]
        public struct AggregationStruct
        {
            public UInt64 unique_socket_id_;
            public Int32 size_bytes_;
            public UInt32 socket_info_index_;
            public Int32 unique_aggr_index_;
            public UInt16 port_number_;
            public Byte msg_type_;
            public Byte msg_flags_;
        }

        /// <summary>
        /// Size in bytes of aggregation structure.
        /// </summary>
        const Int32 AggregationStructSizeBytes = 24;

        /// <summary>
        /// Aggregation struct for this node.
        /// </summary>
        AggregationStruct this_node_aggr_struct_;

        /// <summary>
        /// Send/Received balance.
        /// </summary>
        Int64 sent_received_balance_ = 0;

        public Int64 SentReceivedBalance
        {
            get { return sent_received_balance_; }
        }

        /// <summary>
        /// Performs aggregation receive.
        /// </summary>
        void AggregateReceiveThread()
        {
            try
            {
                Int32 num_received_bytes = 0, receive_bytes_offset = 0;

                unsafe
                {
                    fixed (Byte* rb = aggregate_receive_blob_)
                    {
                        while (true)
                        {
                            Thread.Sleep(1);

                            // Checking if socket is not connected.
                            if (!aggrSocket_.Connected) {
                                return;
                            }

                            // Receiving from scratch.
                            num_received_bytes = 0;

START_RECEIVING:

                            num_received_bytes += aggrSocket_.Receive(aggregate_receive_blob_, num_received_bytes, AggregationBlobSizeBytes - num_received_bytes, SocketFlags.None);

                            // Checking if we have received the aggregation structure at least.
                            if (num_received_bytes < AggregationStructSizeBytes)
                                continue;

                            // Unpacking received data.
                            receive_bytes_offset = 0;

                            while (receive_bytes_offset < num_received_bytes)
                            {
                                AggregationStruct* ags = (AggregationStruct*)(rb + receive_bytes_offset);

                                // Checking if message is received completely.
                                Int32 processing_bytes_left = num_received_bytes - receive_bytes_offset;

                                if ((processing_bytes_left < AggregationStructSizeBytes) ||
                                    (processing_bytes_left < AggregationStructSizeBytes + ags->size_bytes_))
                                {
                                    // Moving the tail up to the beginning.
                                    Buffer.BlockCopy(aggregate_receive_blob_, receive_bytes_offset, aggregate_receive_blob_, 0, processing_bytes_left);

                                    // Continuing receiving from the beginning.
                                    num_received_bytes = processing_bytes_left;

                                    goto START_RECEIVING;
                                }

                                // Getting from awaiting task.
                                NodeTask nt = aggregation_awaiting_tasks_array_[ags->unique_aggr_index_];

                                //Console.WriteLine("Dequeued from sent: " + nt.RequestBytes.Length);
                                Interlocked.Decrement(ref sent_received_balance_);

                                // Checking type of node task.
                                switch ((MixedCodeConstants.AggregationMessageTypes) ags->msg_type_) {

                                    case MixedCodeConstants.AggregationMessageTypes.AGGR_DATA: {

                                        // Constructing the response from received bytes and calling user delegate.
                                        nt.ConstructResponseAndCallDelegate(aggregate_receive_blob_, receive_bytes_offset + AggregationStructSizeBytes, ags->size_bytes_);

                                        break;
                                    }

                                    case MixedCodeConstants.AggregationMessageTypes.AGGR_CREATE_SOCKET:
                                    case MixedCodeConstants.AggregationMessageTypes.AGGR_DESTROY_SOCKET: {

                                        nt.AggrMsgDelegate(*ags);

                                        break;
                                    }
                                }

                                // Releasing free task index.
                                free_task_indexes_.Enqueue(ags->unique_aggr_index_);

                                // Adding finished node task.
                                finished_async_tasks_.Enqueue(nt);

                                // Switching to next message.
                                receive_bytes_offset += AggregationStructSizeBytes + ags->size_bytes_;
                            }
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc);
                throw exc;
            }
        }

        /// <summary>
        /// Performs aggregation send. 
        /// </summary>
        void AggregateSendThread()
        {
            try
            {
                unsafe
                {
                    fixed (Byte* sb = aggregate_send_blob_)
                    {
                        while (true)
                        {
                            // Sleeping some time.
                            Thread.Sleep(1);

                            // Checking if socket is not connected.
                            if (!aggrSocket_.Connected) {
                                return;
                            }

                            // Checking if we have anything to send.
                            Int32 send_bytes_offset = 0;

                            NodeTask nt;

                            // While we have pending tasks to send.
                            while (aggr_pending_async_tasks_.Dequeue(out nt))
                            {
                                // Getting free task index.
                                Int32 free_task_index;
                                Boolean success = free_task_indexes_.Dequeue(out free_task_index);
                                Debug.Assert(success);

                                // Putting task to awaiting array.
                                aggregation_awaiting_tasks_array_[free_task_index] = nt;

                                //Console.WriteLine("Enqueued to send: " + nt.RequestBytes.Length);
                                Interlocked.Increment(ref sent_received_balance_);

                                // Checking if request fits.
                                if (AggregationStructSizeBytes + nt.RequestBytesLength >= AggregationBlobSizeBytes - send_bytes_offset)
                                {
                                    if (0 == send_bytes_offset)
                                        throw new Exception("Request size is bigger than: " + AggregationBlobSizeBytes);

                                    aggrSocket_.Send(aggregate_send_blob_, send_bytes_offset, SocketFlags.None);
                                    send_bytes_offset = 0;
                                }

                                // Checking if we have any request bytes.
                                if (nt.RequestBytesLength > 0) {

                                    // Creating the aggregation struct.
                                    AggregationStruct* ags = (AggregationStruct*)(sb + send_bytes_offset);
                                    *ags = this_node_aggr_struct_;
                                    ags->size_bytes_ = nt.RequestBytesLength;
                                    ags->unique_aggr_index_ = free_task_index;
                                    ags->msg_type_ = (Byte) MixedCodeConstants.AggregationMessageTypes.AGGR_DATA;
                                    ags->msg_flags_ = 0;

                                    // Using fast memory copy here.
                                    Buffer.BlockCopy(nt.RequestBytes, 0, aggregate_send_blob_, send_bytes_offset + AggregationStructSizeBytes, ags->size_bytes_);

                                    // Shifting offset in the array.
                                    send_bytes_offset += AggregationStructSizeBytes + ags->size_bytes_;

                                } else {

                                    // Creating the aggregation struct.
                                    AggregationStruct* ags = (AggregationStruct*)(sb + send_bytes_offset);
                                    *ags = this_node_aggr_struct_;
                                    ags->size_bytes_ = 0;
                                    ags->unique_aggr_index_ = free_task_index;
                                    ags->msg_type_ = (Byte) MixedCodeConstants.AggregationMessageTypes.AGGR_CREATE_SOCKET;
                                    ags->msg_flags_ = 0;

                                    // Shifting offset in the array.
                                    send_bytes_offset += AggregationStructSizeBytes;
                                }
                            }

                            // Sending last processed requests.
                            if (send_bytes_offset > 0)
                                aggrSocket_.Send(aggregate_send_blob_, send_bytes_offset, SocketFlags.None);
                        }
                    }
                }
            }
            catch(Exception exc)
            {
                Console.WriteLine(exc);
                throw exc;
            }
        }
    }
}