// ***********************************************************************
// <copyright file="RequestHandler.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Advanced;
using Starcounter.Internal;
using Starcounter.Internal.Uri;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Starcounter.Rest
{
    /// <summary>
    /// For fast execution, Starcounter will generate code to match incoming HTTP requests.
    /// By matching and parsing the verb and URI, the correct user handler delegate will be called.
    /// This class is responsible to accept registration of user handlers and also to
    /// generate code for and instantiate the top level and sub level RequestProcessors
    /// needed to perform this task. The code generation and instantiation is performed as late
    /// as possible. If additional handlers are registered after code has been generated, a new
    /// version is generated and replaces the old version.
    /// </summary>
    public class RestRegistrationProxy : IREST
    {
        /// <summary>
        /// Registers responses merging routine.
        /// </summary>
        public void RegisterResponsesMerger(Func<Request, List<Response>, Response> mergerRoutine)
        {
            UriInjectMethods.ResponsesMergerRoutine_ = mergerRoutine;
        }

        /// <summary>
        /// Registers a handler for a WebSocket.
        /// </summary>
        public void RegisterWsHandler(ushort port, string channel, Action<Byte[], WebSocket> handler)
        {
            AllWsChannels.WsManager.RegisterWsDelegate(port, channel, handler);
        }

        /// <summary>
        /// Registers a handler for a WebSocket.
        /// </summary>
        public void RegisterWsHandler(ushort port, string channel, Action<String, WebSocket> handler)
        {
            AllWsChannels.WsManager.RegisterWsDelegate(port, channel, handler);
        }

        /// <summary>
        /// Registers a disconnect handler for a WebSocket.
        /// </summary>
        public void RegisterWsDisconnectHandler(ushort port, string channel, Action<UInt64, IAppsSession> handler)
        {
            AllWsChannels.WsManager.RegisterWsDisconnectDelegate(port, channel, handler);
        }

        /// <summary>
        /// Registers a handler with no parameters
        /// </summary>
        public void RegisterHandler(ushort port, String methodAndUriInfo, Func<Response> handler, HandlerOptions ho = null)
        {
            UriManagedHandlersCodegen.UMHC.GenerateParsingDelegate(port, methodAndUriInfo, handler, ho);
        }

        /// <summary>
        /// Registers a handler with one parameter
        /// </summary>
        public void RegisterHandler<T>(ushort port, String methodAndUriInfo, Func<T, Response> handler, HandlerOptions ho = null)
        {
            UriManagedHandlersCodegen.UMHC.GenerateParsingDelegate(port, methodAndUriInfo, handler, ho);
        }

        /// <summary>
        /// Registers a handler with two parameters
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <param name="verbAndUri">The verb and uri of the request. For example GET /things/123</param>
        /// <param name="handler">The code to call when receiving the request</param>
        public void RegisterHandler<T1, T2>(ushort port, String methodAndUriInfo, Func<T1, T2, Response> handler, HandlerOptions ho = null)
        {
            UriManagedHandlersCodegen.UMHC.GenerateParsingDelegate(port, methodAndUriInfo, handler, ho);
        }

        /// <summary>
        /// Registers a handler with three parameters
        /// </summary>
        public void RegisterHandler<T1, T2, T3>(ushort port, String methodAndUriInfo, Func<T1, T2, T3, Response> handler, HandlerOptions ho = null)
        {
            UriManagedHandlersCodegen.UMHC.GenerateParsingDelegate(port, methodAndUriInfo, handler, ho);
        }

        /// <summary>
        /// Registers a handler with four parameters
        /// </summary>
        public void RegisterHandler<T1, T2, T3, T4>(ushort port, String methodAndUriInfo, Func<T1, T2, T3, T4, Response> handler, HandlerOptions ho = null)
        {
            UriManagedHandlersCodegen.UMHC.GenerateParsingDelegate(port, methodAndUriInfo, handler, ho);
        }

        /// <summary>
        /// Registers a handler with five parameters
        /// </summary>
        public void RegisterHandler<T1, T2, T3, T4, T5>(ushort port, String methodAndUriInfo, Func<T1, T2, T3, T4, T5, Response> handler, HandlerOptions ho = null)
        {
            UriManagedHandlersCodegen.UMHC.GenerateParsingDelegate(port, methodAndUriInfo, handler, ho);
        }
    }

    /// <summary>
    /// Accepts registrations of Rest style user handlers. These handlers
    /// allows the user to catch restful calls using http verbs such as
    /// for example GET, POST, PATCH and DELETE using uri templates with parameters
    /// such as GET /persons/{name}.
    /// </summary>
    /// <remarks>Incomplete, needs some love. TODO!</remarks>
    public class RequestHandler
    {
        /// <summary>
        /// Object used for locking.
        /// </summary>
        static Object lockObject_ = new Object();

        /// <summary>
        /// Indicates if REST has been initialized.
        /// </summary>
        internal static Boolean IsRESTInitialized
        {
            get
            {
                return (null != StarcounterBase._REST);
            }
        }

        /// <summary>
        /// Initializes Starcounter REST settings.
        /// </summary>
        /// <param name="databaseTempDir">Path to database temp directory.</param>
        internal static void InitREST()
        {
            if (IsRESTInitialized)
                return;

            lock (lockObject_)
            {
                if (IsRESTInitialized)
                    return;

                // Initializes HTTP parser.
                Request.sc_init_http_parser();
                StarcounterBase._REST = new RestRegistrationProxy();

                // Resetting URI match builder.
                Reset();
            }
        }

        /// <summary>
        /// Make internal with friends
        /// </summary>
        internal static void Reset() {
            UriMatcherBuilder = new UriMatcherBuilder();
        }

        /// <summary>
        /// The URI matcher builder
        /// </summary>
        internal static UriMatcherBuilder UriMatcherBuilder;  
    }
}

