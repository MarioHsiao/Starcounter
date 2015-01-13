

using Starcounter.Advanced;
using Starcounter.Internal;
using System;
using System.Collections.Generic;
namespace Starcounter {

    /// <summary>
    /// Special handler options.
    /// </summary>
    public class HandlerOptions {

        /// <summary>
        /// Invalid URI handler id.
        /// </summary>
        internal const UInt16 InvalidUriHandlerId = UInt16.MaxValue;

        /// <summary>
        /// Specific handler levels.
        /// </summary>
        public enum HandlerLevels {
            FilteringLevel,
            DefaultLevel,
            ApplicationLevel,
            ApplicationExtraLevel,
            CodeHostStaticFileServer
        }

        /// <summary>
        /// Application name.
        /// </summary>
        internal String AppName {
            get;
            set;
        }

        /// <summary>
        /// Don't merge on this handler.
        /// </summary>
        internal Boolean DontMerge {
            get;
            set;
        }

        /// <summary>
        /// Proxy delegate trigger.
        /// </summary>
        internal Boolean ProxyDelegateTrigger {
            get;
            set;
        }

        HandlerLevels handlerLevel_ = HandlerLevels.DefaultLevel;

        /// <summary>
        /// Current handler level.
        /// </summary>
        public HandlerLevels HandlerLevel {
            get {
                return handlerLevel_;
            }

            set {
                handlerLevel_ = value;
            }
        }

        /// <summary>
        /// Forcing setting non-polyjuice handler flag.
        /// </summary>
        public Boolean AllowNonPolyjuiceHandler {
            get;
            set;
        }

        /// <summary>
        /// Flag that allows only external calls.
        /// </summary>
        public Boolean CallExternalOnly {
            get; set;
        }

        /// <summary>
        /// Parameters info.
        /// </summary>
        internal MixedCodeConstants.UserDelegateParamInfo ParametersInfo {
            get;
            set;
        }

        /// <summary>
        /// Specific handler id.
        /// </summary>
        UInt16 handlerId_ = HandlerOptions.InvalidUriHandlerId;

        /// <summary>
        /// Specific handler id.
        /// </summary>
        internal UInt16 HandlerId {
            get {
                return handlerId_;
            }

            set {
                handlerId_ = value;
            }
        }

        /// <summary>
        /// Default handler options.
        /// </summary>
        public static HandlerOptions DefaultHandlerOptions = new HandlerOptions() {
            HandlerLevel = HandlerOptions.HandlerLevels.DefaultLevel
        };

        /// <summary>
        /// Security level.
        /// </summary>
        public readonly static HandlerOptions FilteringLevel = new HandlerOptions() {
            HandlerLevel = HandlerOptions.HandlerLevels.FilteringLevel
        };

        /// <summary>
        /// General level.
        /// </summary>
        public readonly static HandlerOptions DefaultLevel = new HandlerOptions() {
            HandlerLevel = HandlerOptions.HandlerLevels.DefaultLevel
        };

        /// <summary>
        /// Application level.
        /// </summary>
        public readonly static HandlerOptions ApplicationLevel = new HandlerOptions() {
            HandlerLevel = HandlerOptions.HandlerLevels.ApplicationLevel
        };

        /// <summary>
        /// Extra application level.
        /// </summary>
        public readonly static HandlerOptions ApplicationExtraLevel = new HandlerOptions() {
            HandlerLevel = HandlerOptions.HandlerLevels.ApplicationExtraLevel
        };

        /// <summary>
        /// Code host static file server level.
        /// </summary>
        public readonly static HandlerOptions CodeHostStaticFileServer = new HandlerOptions() {
            HandlerLevel = HandlerOptions.HandlerLevels.CodeHostStaticFileServer
        };
    }
    
    /// <summary>
    /// Allows you to register communication endpoints such as REST style handlers
    /// (GET/POST/PUT/DELETE/PATCH etc.)
    /// </summary>
    /// <remarks>
    /// Allows endpoints to be registered that corresponds to methods and URIs (paths
    /// with optional parameters). Even though REST handlers are typically associated with http, they 
    /// can also be implemented in protocols such as HTTP1.1, SPDY, HTTP2.0 (DRAFT),
    /// WebSockets and other means of communication. As long as their endpoints are defined as methods
    /// (verbs such as GET) and URI templates (i.e. /news/sports/{?}).
    /// </remarks>
    public partial class Handle {

        const String GET_METHOD = "GET";
        const String PUT_METHOD = "PUT";
        const String POST_METHOD = "POST";
        const String DELETE_METHOD = "DELETE";
        const String PATCH_METHOD = "PATCH";

        /// <summary>
        /// Indicator of parameter in URI.
        /// </summary>
        public const String UriParameterIndicator = "{?}";

        /// <summary>
        /// Inject REST handler function provider here
        /// </summary>
        public static volatile IREST _REST;

        /// <summary>
        /// Registers a routine to merge several responses.
        /// </summary>
        /// <param name="mergerRoutine">Provided merging routine.</param>
        public static void MergeResponses(Func<Request, List<Response>, Response> mergerRoutine)
        {
            _REST.RegisterResponsesMerger(mergerRoutine);
        }

        public static void CUSTOM(String methodAndUriInfo, Func<Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, methodAndUriInfo, handler, ho);
        }

        public static void CUSTOM(ushort port, String methodAndUriInfo, Func<Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler(port, methodAndUriInfo, handler, ho);
        }

        public static void CUSTOM<T>(String methodAndUriInfo, Func<T, Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler<T>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, methodAndUriInfo, handler, ho);
        }

        public static void CUSTOM<T>(ushort port, String methodAndUriInfo, Func<T, Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler<T>(port, methodAndUriInfo, handler, ho);
        }

        public static void CUSTOM<T1, T2>(String methodAndUriInfo, Func<T1, T2, Response> handler, HandlerOptions ho = null) {

            _REST.RegisterHandler<T1, T2>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, methodAndUriInfo, handler, ho);
        }

        public static void CUSTOM<T1, T2>(ushort port, String methodAndUriInfo, Func<T1, T2, Response> handler, HandlerOptions ho = null) {

            _REST.RegisterHandler<T1, T2>(port, methodAndUriInfo, handler, ho);
        }

        public static void CUSTOM<T1, T2, T3>(String methodAndUriInfo, Func<T1, T2, T3, Response> handler, HandlerOptions ho = null) {

            _REST.RegisterHandler<T1, T2, T3>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, methodAndUriInfo, handler, ho);
        }

        public static void CUSTOM<T1, T2, T3>(ushort port, String methodAndUriInfo, Func<T1, T2, T3, Response> handler, HandlerOptions ho = null) {

            _REST.RegisterHandler<T1, T2, T3>(port, methodAndUriInfo, handler, ho);
        }

        public static void CUSTOM<T1, T2, T3, T4>(String methodAndUriInfo, Func<T1, T2, T3, T4, Response> handler, HandlerOptions ho = null) {

            _REST.RegisterHandler<T1, T2, T3, T4>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, methodAndUriInfo, handler, ho);
        }

        public static void CUSTOM<T1, T2, T3, T4>(ushort port, String methodAndUriInfo, Func<T1, T2, T3, T4, Response> handler, HandlerOptions ho = null) {

            _REST.RegisterHandler<T1, T2, T3, T4>(port, methodAndUriInfo, handler, ho);
        }

        public static void CUSTOM<T1, T2, T3, T4, T5>(String methodAndUriInfo, Func<T1, T2, T3, T4, T5, Response> handler, HandlerOptions ho = null) {

            _REST.RegisterHandler<T1, T2, T3, T4, T5>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, methodAndUriInfo, handler, ho);
        }

        public static void CUSTOM<T1, T2, T3, T4, T5>(ushort port, String methodAndUriInfo, Func<T1, T2, T3, T4, T5, Response> handler, HandlerOptions ho = null) {

            _REST.RegisterHandler<T1, T2, T3, T4, T5>(port, methodAndUriInfo, handler, ho);
        }

        public static void PUT(String uriTemplate, Func<Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, PUT_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void PUT(ushort port, String uriTemplate, Func<Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler(port, PUT_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void PUT<T>(String uriTemplate, Func<T, Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler<T>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, PUT_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void PUT<T>(ushort port, String uriTemplate, Func<T, Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler<T>(port, PUT_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void PUT<T1, T2>(String uriTemplate, Func<T1, T2, Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler<T1, T2>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, PUT_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void PUT<T1, T2>(ushort port, String uriTemplate, Func<T1, T2, Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler<T1, T2>(port, PUT_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void PUT<T1, T2, T3>(String uriTemplate, Func<T1, T2, T3, Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler<T1, T2, T3>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, PUT_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void PUT<T1, T2, T3>(ushort port, String uriTemplate, Func<T1, T2, T3, Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler<T1, T2, T3>(port, PUT_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void PUT<T1, T2, T3, T4>(String uriTemplate, Func<T1, T2, T3, T4, Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler<T1, T2, T3, T4>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, PUT_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void PUT<T1, T2, T3, T4>(ushort port, String uriTemplate, Func<T1, T2, T3, T4, Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler<T1, T2, T3, T4>(port, PUT_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void PUT<T1, T2, T3, T4, T5>(String uriTemplate, Func<T1, T2, T3, T4, T5, Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler<T1, T2, T3, T4, T5>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, PUT_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void PUT<T1, T2, T3, T4, T5>(ushort port, String uriTemplate, Func<T1, T2, T3, T4, T5, Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler<T1, T2, T3, T4, T5>(port, PUT_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void POST(String uriTemplate, Func<Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, POST_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void POST(ushort port, String uriTemplate, Func<Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler(port, POST_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void POST<T>(String uriTemplate, Func<T, Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler<T>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, POST_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void POST<T>(ushort port, String uriTemplate, Func<T, Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler<T>(port, POST_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void POST<T1, T2>(String uriTemplate, Func<T1, T2, Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler<T1, T2>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, POST_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void POST<T1, T2>(ushort port, String uriTemplate, Func<T1, T2, Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler<T1, T2>(port, POST_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void POST<T1, T2, T3>(String uriTemplate, Func<T1, T2, T3, Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler<T1, T2, T3>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, POST_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void POST<T1, T2, T3>(ushort port, String uriTemplate, Func<T1, T2, T3, Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler<T1, T2, T3>(port, POST_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void POST<T1, T2, T3, T4>(String uriTemplate, Func<T1, T2, T3, T4, Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler<T1, T2, T3, T4>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, POST_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void POST<T1, T2, T3, T4>(ushort port, String uriTemplate, Func<T1, T2, T3, T4, Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler<T1, T2, T3, T4>(port, POST_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void POST<T1, T2, T3, T4, T5>(String uriTemplate, Func<T1, T2, T3, T4, T5, Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler<T1, T2, T3, T4, T5>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, POST_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void POST<T1, T2, T3, T4, T5>(ushort port, String uriTemplate, Func<T1, T2, T3, T4, T5, Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler<T1, T2, T3, T4, T5>(port, POST_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void DELETE(String uriTemplate, Func<Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, DELETE_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void DELETE(ushort port, String uriTemplate, Func<Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler(port, DELETE_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void DELETE<T>(String uriTemplate, Func<T, Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler<T>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, DELETE_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void DELETE<T>(ushort port, String uriTemplate, Func<T, Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler<T>(port, DELETE_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void DELETE<T1, T2>(String uriTemplate, Func<T1, T2, Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler<T1, T2>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, DELETE_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void DELETE<T1, T2>(ushort port, String uriTemplate, Func<T1, T2, Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler<T1, T2>(port, DELETE_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void DELETE<T1, T2, T3>(String uriTemplate, Func<T1, T2, T3, Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler<T1, T2, T3>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, DELETE_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void DELETE<T1, T2, T3>(ushort port, String uriTemplate, Func<T1, T2, T3, Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler<T1, T2, T3>(port, DELETE_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void DELETE<T1, T2, T3, T4>(String uriTemplate, Func<T1, T2, T3, T4, Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler<T1, T2, T3, T4>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, DELETE_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void DELETE<T1, T2, T3, T4>(ushort port, String uriTemplate, Func<T1, T2, T3, T4, Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler<T1, T2, T3, T4>(port, DELETE_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void DELETE<T1, T2, T3, T4, T5>(String uriTemplate, Func<T1, T2, T3, T4, T5, Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler<T1, T2, T3, T4, T5>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, DELETE_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void DELETE<T1, T2, T3, T4, T5>(ushort port, String uriTemplate, Func<T1, T2, T3, T4, T5, Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler<T1, T2, T3, T4, T5>(port, DELETE_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void PATCH(String uriTemplate, Func<Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, PATCH_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void PATCH(ushort port, String uriTemplate, Func<Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler(port, PATCH_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void PATCH<T>(String uriTemplate, Func<T, Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler<T>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, PATCH_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void PATCH<T>(ushort port, String uriTemplate, Func<T, Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler<T>(port, PATCH_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void PATCH<T1, T2>(String uriTemplate, Func<T1, T2, Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler<T1, T2>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, PATCH_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void PATCH<T1, T2>(ushort port, String uriTemplate, Func<T1, T2, Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler<T1, T2>(port, PATCH_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void PATCH<T1, T2, T3>(String uriTemplate, Func<T1, T2, T3, Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler<T1, T2, T3>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, PATCH_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void PATCH<T1, T2, T3>(ushort port, String uriTemplate, Func<T1, T2, T3, Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler<T1, T2, T3>(port, PATCH_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void PATCH<T1, T2, T3, T4>(String uriTemplate, Func<T1, T2, T3, T4, Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler<T1, T2, T3, T4>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, PATCH_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void PATCH<T1, T2, T3, T4>(ushort port, String uriTemplate, Func<T1, T2, T3, T4, Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler<T1, T2, T3, T4>(port, PATCH_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void PATCH<T1, T2, T3, T4, T5>(String uriTemplate, Func<T1, T2, T3, T4, T5, Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler<T1, T2, T3, T4, T5>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, PATCH_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void PATCH<T1, T2, T3, T4, T5>(ushort port, String uriTemplate, Func<T1, T2, T3, T4, T5, Response> handler, HandlerOptions ho = null)
        {
            _REST.RegisterHandler<T1, T2, T3, T4, T5>(port, PATCH_METHOD + " " + uriTemplate, handler, ho);
        }
    }
}
