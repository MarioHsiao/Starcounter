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
using System.Collections.Concurrent;

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
        Int32 maxNumNodeTasks_ = 128;

        /// <summary>
        /// Maximum number of parallel connections in async node.
        /// </summary>
        public Int32 MaxNumAsyncConnections {
            get {
                return maxNumNodeTasks_;
            }

            set {
                maxNumNodeTasks_ = value;
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
        /// The Node log source for logging exceptions.
        /// </summary>
        internal static Action<Exception> nodeLogException_;

        /// <summary>
        /// Initializes Node implementation.
        /// </summary>
        internal static void InjectHostedImpl(Action<Exception> nodeLogException) {
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
        /// Host name of this node e.g.: www.starcounter.com, 192.168.0.1
        /// </summary>
        String hostName_;

        /// <summary>
        /// HTTP port number, e.g.: 80
        /// </summary>
        UInt16 portNumber_;

        /// <summary>
        /// Receive timeout in seconds.
        /// </summary>
        public Int32 DefaultReceiveTimeoutSeconds { get; set; }

        /// <summary>
        /// Finished async tasks.
        /// </summary>
        ConcurrentQueue<NodeTask> finished_async_tasks_ = new ConcurrentQueue<NodeTask>();

        /// <summary>
        /// Timed out tasks.
        /// </summary>
        ConcurrentQueue<NodeTask> timed_out_tasks_ = new ConcurrentQueue<NodeTask>();

        /// <summary>
        /// Free task indexes.
        /// </summary>
        ConcurrentQueue<Int32> freeNodeTaskIndexes_ = new ConcurrentQueue<Int32>();

        /// <summary>
        /// Number of created tasks.
        /// </summary>
        Int32 numTasksCreated_ = 0;

        /// <summary>
        /// Node synchronous task.
        /// </summary>
        NodeTask syncTaskInfo_ = null;

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
            Int32 defaultreceiveTimeoutSeconds = 0)
        {
            // NOTE: We have to use ip address instead of "localhost" because of crazy DNS name 
            // resolution that takes time.
            if ((hostName == null) || (hostName == "localhost")) {
                hostName = "127.0.0.1";
            }

            // Checking if port is not specified.
            if (StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort == portNumber) {
                if (StarcounterEnvironment.IsAdministratorApp) {
                    portNumber = StarcounterEnvironment.Default.SystemHttpPort;
                } else {
                    portNumber = StarcounterEnvironment.Default.UserHttpPort;
                }
            }

            hostName_ = hostName;
            portNumber_ = portNumber;
            endpoint_ = hostName + ":" + portNumber;

            DefaultReceiveTimeoutSeconds = defaultreceiveTimeoutSeconds;

            syncTaskInfo_ = new NodeTask(this);
            numTasksCreated_ = 1;
        }

        /// <summary>
        /// Performs asynchronous HTTP GET.
        /// </summary>
        public void GET(String relativeUri, Dictionary<String, String> headersDictionary, Action<Response> userDelegate, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null)
        {
            DoRESTRequestAndGetResponse("GET", relativeUri, headersDictionary, null, userDelegate, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP GET.
        /// </summary>
        public Response GET(String relativeUri, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null)
        {
            return DoRESTRequestAndGetResponse("GET", relativeUri, null, null, null, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP GET.
        /// </summary>
        public Response GET(String relativeUri, Dictionary<String, String> headersDictionary, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null)
        {
            return DoRESTRequestAndGetResponse("GET", relativeUri, headersDictionary, null, null, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP POST.
        /// </summary>
        public void POST(String relativeUri, String body, Dictionary<String, String> headersDictionary, Action<Response> userDelegate, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null)
        {
            Byte[] bodyBytes = null;
            if (body != null)
                bodyBytes = Encoding.UTF8.GetBytes(body);

            DoRESTRequestAndGetResponse("POST", relativeUri, headersDictionary, bodyBytes, userDelegate, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP POST.
        /// </summary>
        public void POST(String relativeUri, Byte[] bodyBytes, Dictionary<String, String> headersDictionary, Action<Response> userDelegate, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null)
        {
            DoRESTRequestAndGetResponse("POST", relativeUri, headersDictionary, bodyBytes, userDelegate, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP POST.
        /// </summary>
        public Response POST(String relativeUri, String body, Dictionary<String, String> headersDictionary, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null)
        {
            Byte[] bodyBytes = null;
            if (body != null)
                bodyBytes = Encoding.UTF8.GetBytes(body);

            return DoRESTRequestAndGetResponse("POST", relativeUri, headersDictionary, bodyBytes, null, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP POST.
        /// </summary>
        public Response POST(String relativeUri, Byte[] bodyBytes, Dictionary<String, String> headersDictionary, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null)
        {
            return DoRESTRequestAndGetResponse("POST", relativeUri, headersDictionary, bodyBytes, null, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP PUT.
        /// </summary>
        public void PUT(String relativeUri, String body, Dictionary<String, String> headersDictionary, Action<Response> userDelegate, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null)
        {
            Byte[] bodyBytes = null;
            if (body != null)
                bodyBytes = Encoding.UTF8.GetBytes(body);

            DoRESTRequestAndGetResponse("PUT", relativeUri, headersDictionary, bodyBytes, userDelegate, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP PUT.
        /// </summary>
        public void PUT(String relativeUri, Byte[] bodyBytes, Dictionary<String, String> headersDictionary, Action<Response> userDelegate, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null)
        {
            DoRESTRequestAndGetResponse("PUT", relativeUri, headersDictionary, bodyBytes, userDelegate, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP PUT.
        /// </summary>
        public Response PUT(String relativeUri, String body, Dictionary<String, String> headersDictionary, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null)
        {
            Byte[] bodyBytes = null;
            if (body != null)
                bodyBytes = Encoding.UTF8.GetBytes(body);

            return DoRESTRequestAndGetResponse("PUT", relativeUri, headersDictionary, bodyBytes, null, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP PUT.
        /// </summary>
        public Response PUT(String relativeUri, Byte[] bodyBytes, Dictionary<String, String> headersDictionary, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null)
        {
            return DoRESTRequestAndGetResponse("PUT", relativeUri, headersDictionary, bodyBytes, null, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP PATCH.
        /// </summary>
        public void PATCH(String relativeUri, String body, Dictionary<String, String> headersDictionary, Action<Response> userDelegate, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null)
        {
            Byte[] bodyBytes = null;
            if (body != null)
                bodyBytes = Encoding.UTF8.GetBytes(body);

            DoRESTRequestAndGetResponse("PATCH", relativeUri, headersDictionary, bodyBytes, userDelegate, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP PATCH.
        /// </summary>
        public void PATCH(String relativeUri, Byte[] bodyBytes, Dictionary<String, String> headersDictionary, Action<Response> userDelegate, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null)
        {
            DoRESTRequestAndGetResponse("PATCH", relativeUri, headersDictionary, bodyBytes, userDelegate, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP PATCH.
        /// </summary>
        public Response PATCH(String relativeUri, String body, Dictionary<String, String> headersDictionary, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null)
        {
            Byte[] bodyBytes = null;
            if (body != null)
                bodyBytes = Encoding.UTF8.GetBytes(body);

            return DoRESTRequestAndGetResponse("PATCH", relativeUri, headersDictionary, bodyBytes, null, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP PATCH.
        /// </summary>
        public Response PATCH(String relativeUri, Byte[] bodyBytes, Dictionary<String, String> headersDictionary, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null)
        {
            return DoRESTRequestAndGetResponse("PATCH", relativeUri, headersDictionary, bodyBytes, null, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP DELETE.
        /// </summary>
        public void DELETE(String relativeUri, String body, Dictionary<String, String> headersDictionary, Action<Response> userDelegate, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null)
        {
            Byte[] bodyBytes = null;
            if (body != null)
                bodyBytes = Encoding.UTF8.GetBytes(body);

            DoRESTRequestAndGetResponse("DELETE", relativeUri, headersDictionary, bodyBytes, userDelegate, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP DELETE.
        /// </summary>
        public void DELETE(String relativeUri, Byte[] bodyBytes, Dictionary<String, String> headersDictionary, Action<Response> userDelegate, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null)
        {
            DoRESTRequestAndGetResponse("DELETE", relativeUri, headersDictionary, bodyBytes, userDelegate, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP DELETE.
        /// </summary>
        public Response DELETE(String relativeUri, String body, Dictionary<String, String> headersDictionary, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null)
        {
            Byte[] bodyBytes = null;
            if (body != null)
                bodyBytes = Encoding.UTF8.GetBytes(body);

            return DoRESTRequestAndGetResponse("DELETE", relativeUri, headersDictionary, bodyBytes, null, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP DELETE.
        /// </summary>
        public Response DELETE(String relativeUri, Byte[] bodyBytes, Dictionary<String, String> headersDictionary, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null)
        {
            return DoRESTRequestAndGetResponse("DELETE", relativeUri, headersDictionary, bodyBytes, null, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP request with given HTTP method.
        /// </summary>
        public void CustomRESTRequest(String method, String relativeUri, String body, Dictionary<String, String> headersDictionary, Action<Response> userDelegate, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null)
        {
            Byte[] bodyBytes = null;
            if (body != null)
                bodyBytes = Encoding.UTF8.GetBytes(body);

            DoRESTRequestAndGetResponse(method, relativeUri, headersDictionary, bodyBytes, userDelegate, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP request with given HTTP method.
        /// </summary>
        public void CustomRESTRequest(String method, String relativeUri, Byte[] bodyBytes, Dictionary<String, String> headersDictionary, Action<Response> userDelegate, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null)
        {
            DoRESTRequestAndGetResponse(method, relativeUri, headersDictionary, bodyBytes, userDelegate, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP request with given HTTP method.
        /// </summary>
        public Response CustomRESTRequest(String method, String relativeUri, String body, Dictionary<String, String> headersDictionary, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null)
        {
            Byte[] bodyBytes = null;
            if (body != null)
                bodyBytes = Encoding.UTF8.GetBytes(body);

            return DoRESTRequestAndGetResponse(method, relativeUri, headersDictionary, bodyBytes, null, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP request with given HTTP method.
        /// </summary>
        public Response CustomRESTRequest(String method, String relativeUri, Byte[] bodyBytes, Dictionary<String, String> headersDictionary, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null)
        {
            return DoRESTRequestAndGetResponse(method, relativeUri, headersDictionary, bodyBytes, null, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP request with given HTTP method.
        /// </summary>
        public void CustomRESTRequest(Request req, Action<Response> userDelegate, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null)
        {
            DoRESTRequestAndGetResponse(
                req.Method,
                req.Uri,
                null,
                null,
                userDelegate,
                receiveTimeoutSeconds,
                ho,
                req);
        }

        /// <summary>
        /// Performs synchronous HTTP request with given HTTP method.
        /// </summary>
        public Response CustomRESTRequest(Request req, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null)
        {
            return DoRESTRequestAndGetResponse(
                req.Method,
                req.Uri,
                null,
                null,
                null,
                receiveTimeoutSeconds,
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
            Action<Response> userDelegate,
            Byte boundSchedulerId,
            String appName)
        {
            try
            {
                // Invoking user delegate either on the same scheduler if inside Starcounter.
                if (StarcounterEnvironment.IsCodeHosted)
                {
                    StarcounterBase._DB.RunAsync(() => {

                        StarcounterEnvironment.RunWithinApplication(appName, () => {
                            userDelegate.Invoke(resp);
                        });
                        
                    }, boundSchedulerId);
                }
                else
                {
                    userDelegate(resp);
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
            Action<Response> userDelegate,
            Int32 receiveTimeoutSeconds = 0)
        {
            // Setting the receive timeout.
            if (0 == receiveTimeoutSeconds) {
                receiveTimeoutSeconds = DefaultReceiveTimeoutSeconds;
            }

            NodeTask nt = null;

            // Checking if any tasks are finished.
            if (!finished_async_tasks_.TryDequeue(out nt)) {

                // Checking if we exceeded the maximum number of created tasks.
                if (numTasksCreated_ >= maxNumNodeTasks_) {

                    // Looping until task is dequeued.
                    while (!finished_async_tasks_.TryDequeue(out nt)) {
                        //Thread.Sleep(1);
                    }
                }
            }

            // Checking if any empty tasks was dequeued.
            if (null == nt) {
                Interlocked.Increment(ref numTasksCreated_);
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
                receiveTimeoutSeconds,
                currentSchedulerId);

            // Starting async request on that node task.
            nt.PerformAsyncRequest();
        }

        /// <summary>
        /// Core function to send REST requests and get the responses.
        /// </summary>
        public Response DoRESTRequestAndGetResponse(
            String method,
            String relativeUri,
            Dictionary<String, String> headersDictionary,
            Byte[] bodyBytes,
            Action<Response> userDelegate,
            Int32 receiveTimeoutSeconds,
            HandlerOptions handlerOptions,
            Request req = null)
        {
            // Checking if handler options is defined.
            if (handlerOptions == null) {
                handlerOptions = new HandlerOptions();
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

            // Setting the receive timeout.
            if (0 == receiveTimeoutSeconds) {
                receiveTimeoutSeconds = DefaultReceiveTimeoutSeconds;
            }

            // Checking if user has supplied a delegate to be called.
            if (null != userDelegate) {

                // Trying to perform an async request.
                DoAsyncTransfer(req, userDelegate, receiveTimeoutSeconds);

                return null;
            }

            lock (finished_async_tasks_) {

                // Initializing connection.
                syncTaskInfo_.ResetButKeepSocket(req, null, receiveTimeoutSeconds, 0);

                Response resp = null;
                StarcounterEnvironment.RunDetached(() => {
                    // Doing synchronous request and returning response.
                    resp = syncTaskInfo_.PerformSyncRequest();
                });

                return resp;
            }
        }
    }
}