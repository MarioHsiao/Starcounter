﻿// ***********************************************************************
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
        /// Performs local Node REST call.
        /// </summary>
        static DoLocalNodeRest DoLocalNodeRest_;

        /// <summary>
        /// Pending async tasks.
        /// </summary>
        LockFreeQueue<NodeTask> aggr_pending_async_tasks_ = new LockFreeQueue<NodeTask>();

        /// <summary>
        /// The Node log source for logging exceptions.
        /// </summary>
        internal static Action<Exception> NodeLogException_;

        /// <summary>
        /// Initializes Node implementation.
        /// </summary>
        /// <param name="rest"></param>
        /// <param name="logSource"></param>
        internal static void InjectHostedImpl(
            DoLocalNodeRest doLocalNodeRest,
            Action<Exception> nodeLogException)
        {
            DoLocalNodeRest_ = doLocalNodeRest;
            NodeLogException_ = nodeLogException;
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
        Boolean localNode_ = false;

        internal Boolean LocalNode
        {
            get { return localNode_; }
            set { localNode_ = value; }
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
        /// Delegate to process Node local requests.
        /// </summary>
        /// <param name="methodAndUriPlusSpace"></param>
        /// <param name="requestBytes"></param>
        /// <param name="resp"></param>
        /// <returns></returns>
        internal delegate Boolean DoLocalNodeRest(
            String methodAndUriPlusSpace,
            Byte[] requestBytes,
            Int32 requestBytesLength,
            UInt16 portNumber,
            Int32 handlerLevel,
            out Response resp);

        /// <summary>
        /// Returns this node port number.
        /// </summary>
        public UInt16 PortNumber { get { return portNumber_; } }

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
        /// <param name="hostName">Name of the host to connect to.</param>
        /// <param name="portNumber">Port number to connect to.</param>
        /// <param name="defaultReceiveTimeoutMs">Default receive timeout.</param>
        /// <param name="useAggregation">True to use aggregation to Starcounter server.</param>
        /// <param name="aggrPortNumber">Aggregation port on Starcounter server.</param>
        public Node(String hostName, UInt16 portNumber = 0, Int32 defaultReceiveTimeoutMs = 0, Boolean useAggregation = false, UInt16 aggrPortNumber = 0)
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

            aggrPortNumber_ = aggrPortNumber;

            if (0 == aggrPortNumber_)
                aggrPortNumber_ = StarcounterEnvironment.Default.AggregationPort;

            hostName_ = hostName;
            portNumber_ = portNumber;
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
                aggrSocket_ = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                try {
                    const int SIO_LOOPBACK_FAST_PATH = (-1744830448);
                
                    Byte[] OptionInValue = BitConverter.GetBytes(1);

                    aggrSocket_.IOControl(
                        SIO_LOOPBACK_FAST_PATH,
                        OptionInValue,
                        null);
                } catch {
                    // Simply ignoring the error if fast loopback is not supported.
                }

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
                DoAsyncTransfer(null, 0, null, (AggregationStruct aggr_struct) => {
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
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="userObject">User object to be passed on response.</param>
        /// <param name="userDelegate">User delegate to be called on response.</param>
        /// <param name="receiveTimeoutMs">Timeout for receive in milliseconds.</param>
        public void GET(String relativeUri, String customHeaders, Object userObject, Action<Response, Object> userDelegate, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            DoRESTRequestAndGetResponse("GET", relativeUri, customHeaders, null, userDelegate, userObject, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP GET.
        /// </summary>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="receiveTimeoutMs">Timeout for receive in milliseconds.</param>
        /// <returns>HTTP response.</returns>
        public Response GET(String relativeUri, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            return DoRESTRequestAndGetResponse("GET", relativeUri, null, null, null, null, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP GET.
        /// </summary>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="receiveTimeoutMs">Timeout for receive in milliseconds.</param>
        /// <returns>HTTP response.</returns>
        public Response GET(String relativeUri, String customHeaders, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            return DoRESTRequestAndGetResponse("GET", relativeUri, customHeaders, null, null, null, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP POST.
        /// </summary>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="body">HTTP body or null.</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="userObject">User object to be passed on response.</param>
        /// <param name="userDelegate">User delegate to be called on response.</param>
        /// <param name="receiveTimeoutMs">Timeout for receive in milliseconds.</param>
        public void POST(String relativeUri, String body, String customHeaders, Object userObject, Action<Response, Object> userDelegate, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            Byte[] bodyBytes = null;
            if (body != null)
                bodyBytes = Encoding.UTF8.GetBytes(body);

            DoRESTRequestAndGetResponse("POST", relativeUri, customHeaders, bodyBytes, userDelegate, userObject, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP POST.
        /// </summary>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="body">HTTP body or null.</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="userObject">User object to be passed on response.</param>
        /// <param name="userDelegate">User delegate to be called on response.</param>
        /// <param name="receiveTimeoutMs">Timeout for receive in milliseconds.</param>
        public void POST(String relativeUri, Byte[] bodyBytes, String customHeaders, Object userObject, Action<Response, Object> userDelegate, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            DoRESTRequestAndGetResponse("POST", relativeUri, customHeaders, bodyBytes, userDelegate, userObject, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP POST.
        /// </summary>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="body">HTTP body or null.</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="receiveTimeoutMs">Timeout for receive in milliseconds.</param>
        /// <returns>HTTP response.</returns>
        public Response POST(String relativeUri, String body, String customHeaders, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            Byte[] bodyBytes = null;
            if (body != null)
                bodyBytes = Encoding.UTF8.GetBytes(body);

            return DoRESTRequestAndGetResponse("POST", relativeUri, customHeaders, bodyBytes, null, null, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP POST.
        /// </summary>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="body">HTTP body or null.</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="receiveTimeoutMs">Timeout for receive in milliseconds.</param>
        /// <returns>HTTP response.</returns>
        public Response POST(String relativeUri, Byte[] bodyBytes, String customHeaders, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            return DoRESTRequestAndGetResponse("POST", relativeUri, customHeaders, bodyBytes, null, null, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP PUT.
        /// </summary>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="body">HTTP body or null.</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="userObject">User object to be passed on response.</param>
        /// <param name="userDelegate">User delegate to be called on response.</param>
        /// <param name="receiveTimeoutMs">Timeout for receive in milliseconds.</param>
        public void PUT(String relativeUri, String body, String customHeaders, Object userObject, Action<Response, Object> userDelegate, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            Byte[] bodyBytes = null;
            if (body != null)
                bodyBytes = Encoding.UTF8.GetBytes(body);

            DoRESTRequestAndGetResponse("PUT", relativeUri, customHeaders, bodyBytes, userDelegate, userObject, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP PUT.
        /// </summary>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="body">HTTP body or null.</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="userObject">User object to be passed on response.</param>
        /// <param name="userDelegate">User delegate to be called on response.</param>
        /// <param name="receiveTimeoutMs">Timeout for receive in milliseconds.</param>
        public void PUT(String relativeUri, Byte[] bodyBytes, String customHeaders, Object userObject, Action<Response, Object> userDelegate, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            DoRESTRequestAndGetResponse("PUT", relativeUri, customHeaders, bodyBytes, userDelegate, userObject, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP PUT.
        /// </summary>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="body">HTTP body or null.</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <returns>HTTP response.</returns>
        /// <param name="receiveTimeoutMs">Timeout for receive in milliseconds.</param>
        public Response PUT(String relativeUri, String body, String customHeaders, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            Byte[] bodyBytes = null;
            if (body != null)
                bodyBytes = Encoding.UTF8.GetBytes(body);

            return DoRESTRequestAndGetResponse("PUT", relativeUri, customHeaders, bodyBytes, null, null, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP PUT.
        /// </summary>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="body">HTTP body or null.</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="receiveTimeoutMs">Timeout for receive in milliseconds.</param>
        /// <returns>HTTP response.</returns>
        public Response PUT(String relativeUri, Byte[] bodyBytes, String customHeaders, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            return DoRESTRequestAndGetResponse("PUT", relativeUri, customHeaders, bodyBytes, null, null, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP PATCH.
        /// </summary>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="body">HTTP body or null.</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="userObject">User object to be passed on response.</param>
        /// <param name="userDelegate">User delegate to be called on response.</param>
        /// <param name="receiveTimeoutMs">Timeout for receive in milliseconds.</param>
        public void PATCH(String relativeUri, String body, String customHeaders, Object userObject, Action<Response, Object> userDelegate, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            Byte[] bodyBytes = null;
            if (body != null)
                bodyBytes = Encoding.UTF8.GetBytes(body);

            DoRESTRequestAndGetResponse("PATCH", relativeUri, customHeaders, bodyBytes, userDelegate, userObject, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP PATCH.
        /// </summary>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="body">HTTP body or null.</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="userObject">User object to be passed on response.</param>
        /// <param name="userDelegate">User delegate to be called on response.</param>
        /// <param name="receiveTimeoutMs">Timeout for receive in milliseconds.</param>
        public void PATCH(String relativeUri, Byte[] bodyBytes, String customHeaders, Object userObject, Action<Response, Object> userDelegate, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            DoRESTRequestAndGetResponse("PATCH", relativeUri, customHeaders, bodyBytes, userDelegate, userObject, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP PATCH.
        /// </summary>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="body">HTTP body or null.</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="receiveTimeoutMs">Timeout for receive in milliseconds.</param>
        /// <returns>HTTP response.</returns>
        public Response PATCH(String relativeUri, String body, String customHeaders, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            Byte[] bodyBytes = null;
            if (body != null)
                bodyBytes = Encoding.UTF8.GetBytes(body);

            return DoRESTRequestAndGetResponse("PATCH", relativeUri, customHeaders, bodyBytes, null, null, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP PATCH.
        /// </summary>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="body">HTTP body or null.</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="receiveTimeoutMs">Timeout for receive in milliseconds.</param>
        /// <returns>HTTP response.</returns>
        public Response PATCH(String relativeUri, Byte[] bodyBytes, String customHeaders, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            return DoRESTRequestAndGetResponse("PATCH", relativeUri, customHeaders, bodyBytes, null, null, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP DELETE.
        /// </summary>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="body">HTTP body or null.</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="userObject">User object to be passed on response.</param>
        /// <param name="userDelegate">User delegate to be called on response.</param>
        /// <param name="receiveTimeoutMs">Timeout for receive in milliseconds.</param>
        public void DELETE(String relativeUri, String body, String customHeaders, Object userObject, Action<Response, Object> userDelegate, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            Byte[] bodyBytes = null;
            if (body != null)
                bodyBytes = Encoding.UTF8.GetBytes(body);

            DoRESTRequestAndGetResponse("DELETE", relativeUri, customHeaders, bodyBytes, userDelegate, userObject, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP DELETE.
        /// </summary>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="body">HTTP body or null.</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="userObject">User object to be passed on response.</param>
        /// <param name="userDelegate">User delegate to be called on response.</param>
        /// <param name="receiveTimeoutMs">Timeout for receive in milliseconds.</param>
        public void DELETE(String relativeUri, Byte[] bodyBytes, String customHeaders, Object userObject, Action<Response, Object> userDelegate, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            DoRESTRequestAndGetResponse("DELETE", relativeUri, customHeaders, bodyBytes, userDelegate, userObject, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP DELETE.
        /// </summary>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="body">HTTP body or null.</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="receiveTimeoutMs">Timeout for receive in milliseconds.</param>
        /// <returns>HTTP response.</returns>
        public Response DELETE(String relativeUri, String body, String customHeaders, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            Byte[] bodyBytes = null;
            if (body != null)
                bodyBytes = Encoding.UTF8.GetBytes(body);

            return DoRESTRequestAndGetResponse("DELETE", relativeUri, customHeaders, bodyBytes, null, null, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP DELETE.
        /// </summary>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="body">HTTP body or null.</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="receiveTimeoutMs">Timeout for receive in milliseconds.</param>
        /// <returns>HTTP response.</returns>
        public Response DELETE(String relativeUri, Byte[] bodyBytes, String customHeaders, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            return DoRESTRequestAndGetResponse("DELETE", relativeUri, customHeaders, bodyBytes, null, null, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP request with given HTTP method.
        /// </summary>
        /// <param name="method">HTTP method, e.g.: "OPTIONS", "HEAD".</param>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="body">HTTP body or null.</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="userObject">User object to be passed on response.</param>
        /// <param name="userDelegate">User delegate to be called on response.</param>
        /// <param name="receiveTimeoutMs">Timeout for receive in milliseconds.</param>
        public void CustomRESTRequest(String method, String relativeUri, String body, String customHeaders, Object userObject, Action<Response, Object> userDelegate, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            Byte[] bodyBytes = null;
            if (body != null)
                bodyBytes = Encoding.UTF8.GetBytes(body);

            DoRESTRequestAndGetResponse(method, relativeUri, customHeaders, bodyBytes, userDelegate, userObject, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP request with given HTTP method.
        /// </summary>
        /// <param name="method">HTTP method, e.g.: "OPTIONS", "HEAD".</param>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="body">HTTP body or null.</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="userObject">User object to be passed on response.</param>
        /// <param name="userDelegate">User delegate to be called on response.</param>
        /// <param name="receiveTimeoutMs">Timeout for receive in milliseconds.</param>
        public void CustomRESTRequest(String method, String relativeUri, Byte[] bodyBytes, String customHeaders, Object userObject, Action<Response, Object> userDelegate, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            DoRESTRequestAndGetResponse(method, relativeUri, customHeaders, bodyBytes, userDelegate, userObject, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP request with given HTTP method.
        /// </summary>
        /// <param name="method">HTTP method, e.g.: "OPTIONS", "HEAD".</param>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="body">HTTP body or null.</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="receiveTimeoutMs">Timeout for receive in milliseconds.</param>
        /// <returns>HTTP response.</returns>
        public Response CustomRESTRequest(String method, String relativeUri, String body, String customHeaders, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            Byte[] bodyBytes = null;
            if (body != null)
                bodyBytes = Encoding.UTF8.GetBytes(body);

            return DoRESTRequestAndGetResponse(method, relativeUri, customHeaders, bodyBytes, null, null, receiveTimeoutMs, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP request with given HTTP method.
        /// </summary>
        /// <param name="method">HTTP method, e.g.: "OPTIONS", "HEAD".</param>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="body">HTTP body or null.</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="receiveTimeoutMs">Timeout for receive in milliseconds.</param>
        /// <returns>HTTP response.</returns>
        public Response CustomRESTRequest(String method, String relativeUri, Byte[] bodyBytes, String customHeaders, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            return DoRESTRequestAndGetResponse(method, relativeUri, customHeaders, bodyBytes, null, null, receiveTimeoutMs, ho);
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
        /// <param name="receiveTimeoutMs">Timeout for receive in milliseconds.</param>
        public void CustomRESTRequest(Request req, Object userObject, Action<Response, Object> userDelegate, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            if (ho == null)
                ho = new HandlerOptions();

            ho.DontModifyHeaders = true;

            DoRESTRequestAndGetResponse(req.Method, req.Uri, req.Headers, req.BodyBytes, userDelegate, userObject, receiveTimeoutMs, ho, req);
        }

        /// <summary>
        /// Performs synchronous HTTP request with given HTTP method.
        /// </summary>
        /// <param name="method">HTTP method, e.g.: "OPTIONS", "HEAD".</param>
        /// <param name="relativeUri">Relative HTTP URI, e.g.: "/hello", "index.html", "/"</param>
        /// <param name="body">HTTP body or null.</param>
        /// <param name="customHeaders">Custom HTTP headers or null, e.g.: "MyNewHeader: value123\r\n"</param>
        /// <param name="req">Original HTTP request.</param>
        /// <param name="receiveTimeoutMs">Timeout for receive in milliseconds.</param>
        /// <returns>HTTP response.</returns>
        public Response CustomRESTRequest(Request req, Int32 receiveTimeoutMs = 0, HandlerOptions ho = null)
        {
            if (ho == null)
                ho = new HandlerOptions();

            ho.DontModifyHeaders = true;

            return DoRESTRequestAndGetResponse(req.Method, req.Uri, req.Headers, req.BodyBytes, null, null, receiveTimeoutMs, ho, req);
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
        /// <param name="resp"></param>
        /// <param name="userDelegate"></param>
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

                // Checking if response should be sent.
                if (resp.Request != null)
                {
                    resp.Request.SendResponse(resp);
                    resp.Request = null;
                }
            }
            catch (Exception exc)
            {
                // Checking if exception should be logged.
                if (ShouldLogErrors_)
                    Node.NodeLogException_(exc);
                else
                    throw exc;
            }
        }

        /// <summary>
        /// Calculates estimated number of bytes for Request.
        /// </summary>
        /// <param name="method"></param>
        /// <param name="relativeUri"></param>
        /// <param name="customHeaders"></param>
        /// <param name="bodyBytes"></param>
        /// <returns></returns>
        Int32 EstimateRequestLengthBytes(
            String method,
            String relativeUri,
            String customHeaders,
            Byte[] bodyBytes)
        {
            Int32 len_bytes = method.Length;
            len_bytes += relativeUri.Length;
            len_bytes += hostName_.Length;
            len_bytes += 64; // For spaces, colons, newlines.

            if (null != customHeaders)
                len_bytes += customHeaders.Length;

            if (null != bodyBytes)
                len_bytes += bodyBytes.Length;

            return len_bytes;
        }

        public Byte[] ConstructRequestBytes(
            String method,
            String relativeUri,
            String customHeaders,
            Byte[] bodyBytes,
            Boolean dontModifyHeaders,
            out Int32 requestBytesLength) {

            Utf8Writer writer;

            Byte[] requestBytes = new Byte[EstimateRequestLengthBytes(method, relativeUri, customHeaders, bodyBytes)];

            unsafe {
                fixed (byte* p = requestBytes) {
                    writer = new Utf8Writer(p);

                    writer.Write(method);
                    writer.Write(' ');
                    writer.Write(relativeUri);
                    writer.Write(' ');
                    writer.Write(HttpHeadersUtf8.Http11NoSpace);
                    writer.Write(StarcounterConstants.NetworkConstants.CRLF);

                    // Checking if headers should be sent as-is.
                    if (!dontModifyHeaders) {
                        writer.Write(HttpHeadersUtf8.HostStart);
                        writer.Write(hostName_);
                        writer.Write(StarcounterConstants.NetworkConstants.CRLF);

                        writer.Write(HttpHeadersUtf8.ContentLengthStart);
                        if (bodyBytes != null)
                            writer.Write(bodyBytes.Length);
                        else
                            writer.Write(0);

                        writer.Write(StarcounterConstants.NetworkConstants.CRLF);
                    }

                    // Checking if headers already supplied.
                    if (customHeaders != null) {
                        // Checking for correct custom headers format.
                        if (!customHeaders.EndsWith(StarcounterConstants.NetworkConstants.CRLF))
                            throw new ArgumentException("Each custom header should be in following form: \"<HeaderName>:<space><value>\\r\\n\" For example: \"MyNewHeader: value123\\r\\n\"");

                        writer.Write(customHeaders);
                    }

                    writer.Write(StarcounterConstants.NetworkConstants.CRLF);

                    if (bodyBytes != null)
                        writer.Write(bodyBytes);
                }

                requestBytesLength = writer.Written;
            }

            return requestBytes;
        }

        void DoAsyncTransfer(
            Byte[] dataBytes,
            Int32 dataBytesLength,
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
                if (num_tasks_created_ >= NodeTask.MaxNumPendingAsyncTasks) {
                    // Looping until task is dequeued.
                    while (!finished_async_tasks_.Dequeue(out nt))
                        Thread.Sleep(1);
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
            nt.ResetButKeepSocket(dataBytes, dataBytesLength, userDelegate, aggrMsgDelegate, userObject, receiveTimeoutMs, currentSchedulerId);

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
        /// <param name="method">HTTP method.</param>
        /// <param name="relativeUri">Relative URI.</param>
        /// <param name="customHeaders">Custom HTTP headers if any.</param>
        /// <param name="body">HTTP Body string or null if no such.</param>
        /// <param name="func">User delegate to be called.</param>
        Response DoRESTRequestAndGetResponse(
            String method,
            String relativeUri,
            String customHeaders,
            Byte[] bodyBytes,
            Action<Response, Object> userDelegate,
            Object userObject,
            Int32 receiveTimeoutMs,
            HandlerOptions ho,
            Request req = null)
        {
            // Checking if handler options is defined.
            if (ho == null)
                ho = HandlerOptions.DefaultHandlerOptions;

            if (relativeUri == null || relativeUri.Length < 1)
                throw new ArgumentOutOfRangeException("URI should contain at least one character.");

            String methodAndUriPlusSpace = method + " " + relativeUri + " ";

            Int32 requestBytesLength;
            Byte[] requestBytes;
            
            // Checking if request is defined and initialized.
            if ((req == null) || (req.CustomBytes == null)) {
                requestBytes = ConstructRequestBytes(method, relativeUri, customHeaders, bodyBytes, ho.DontModifyHeaders, out requestBytesLength);
            } else {
                requestBytes = req.CustomBytes;
                requestBytesLength = req.CustomBytesLength;
            }
            
            // No response initially.
            Response resp = null;

            // Checking if we are on local node.
            if ((localNode_) && (!ho.CallExternalOnly)) {

                Int32 handlerLevel = 0;
                if (ho.IsSpecificHandlerLevel)
                    handlerLevel = ho.HandlerLevel;

                // Going through all handlers in the list.
                for (Int32 i = 0; i < HandlerOptions.NumHandlerLevels; i++) {

                    // Trying to do local node REST.
                    if (DoLocalNodeRest_(methodAndUriPlusSpace, requestBytes, requestBytesLength, portNumber_, handlerLevel, out resp)) {

                        // Checking if handled.
                        if (resp.HandlingStatus != HandlerStatusInternal.NotHandled) {

                            // Checking if user has supplied a delegate to be called.
                            if (null != userDelegate) {

                                // Invoking user delegate.
                                userDelegate.Invoke(resp, userObject);

                                // Checking if response should be sent.
                                if (resp.Request != null) {
                                    resp.Request.SendResponse(resp);
                                    resp.Request = null;
                                }

                                return null;
                            }

                            return resp;
                        }
                    }
            
                    // Checking if we have a special handler.
                    if (ho.IsSpecificHandlerLevel)
                        return resp;
                    else
                        handlerLevel++;
                }
            }

            // Setting the receive timeout.
            if (0 == receiveTimeoutMs)
                receiveTimeoutMs = DefaultReceiveTimeoutMs;

            // Checking if user has supplied a delegate to be called.
            if (null != userDelegate) {

                // Trying to perform an async request.
                DoAsyncTransfer(requestBytes, requestBytesLength, userDelegate, null, userObject, receiveTimeoutMs);

                return null;
            }

            lock (finished_async_tasks_)
            {
                // Initializing connection.
                sync_task_info_.ResetButKeepSocket(requestBytes, requestBytesLength, null, null, userObject, receiveTimeoutMs, 0);

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

        /// <summary>
        /// Add some more sophistication here
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private bool UserSuspectedOfForgettingLeadingSlash(string p) {
            return true;
        }
    }
}