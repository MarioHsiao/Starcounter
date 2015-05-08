
using Starcounter.Internal;
using System;
using System.Collections.Generic;

namespace Starcounter.Advanced {


    /// <summary>
    /// Handlers registrations of user code handlers (delegates) of REST style requests.
    /// Usually, the developer uses the global static functions (GET/POST/etc.)
    /// in the StarcounterBase class rather.
    /// </summary>
    public interface IREST {
        /// <summary>
        /// Registers a handler for a WebSocket.
        /// </summary>
        void RegisterWsHandler(ushort port, string channel, Action<Byte[], WebSocket> handler);

        /// <summary>
        /// Registers a handler for a WebSocket.
        /// </summary>
        void RegisterWsHandler(ushort port, string channel, Action<String, WebSocket> handler);

        /// <summary>
        /// Registers a disconnect handler for a WebSocket.
        /// </summary>
        void RegisterWsDisconnectHandler(ushort port, string channel, Action<WebSocket> handler);

        /// <summary>
        /// Registers a handler with no parameters
        /// </summary>
        void RegisterHandler(ushort port, String methodAndUriInfo, Func<Response> handler, HandlerOptions ho = null);

        /// <summary>
        /// Registers a handler with one parameter
        /// </summary>
        void RegisterHandler<T>(ushort port, String methodAndUriInfo, Func<T, Response> handler, HandlerOptions ho = null);

        /// <summary>
        /// Registers a handler with two parameters
        /// </summary>
        void RegisterHandler<T1, T2>(ushort port, String methodAndUriInfo, Func<T1, T2, Response> handler, HandlerOptions ho = null);

        /// <summary>
        /// Registers a handler with three parameters
        /// </summary>
        void RegisterHandler<T1, T2, T3>(ushort port, String methodAndUriInfo, Func<T1, T2, T3, Response> handler, HandlerOptions ho = null);

        /// <summary>
        /// Registers a handler with four parameters
        /// </summary>
        void RegisterHandler<T1, T2, T3, T4>(ushort port, String methodAndUriInfo, Func<T1, T2, T3, T4, Response> handler, HandlerOptions ho = null);

        /// <summary>
        /// Registers a handler with five parameters
        /// </summary>
        void RegisterHandler<T1, T2, T3, T4, T5>(ushort port, String methodAndUriInfo, Func<T1, T2, T3, T4, T5, Response> handler, HandlerOptions ho = null);
    }
}
