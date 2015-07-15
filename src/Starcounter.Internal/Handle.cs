

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
            DefaultLevel,
            ApplicationLevel,
            ApplicationExtraLevel
        }

        /// <summary>
        /// Skip middleware filters flag.
        /// </summary>
        public Boolean SkipMiddlewareFilters {
            get;
            set;
        }

        /// <summary>
        /// Calling application name.
        /// </summary>
        internal String CallingAppName {
            get;
            set;
        }

        /// <summary>
        /// Proxy delegate trigger.
        /// </summary>
        public Boolean ProxyDelegateTrigger {
            get;
            set;
        }

        /// <summary>
        /// Type of registered handler.
        /// </summary>
        public enum TypesOfHandler {
            Generic,
            OrdinaryMapping,
            OntologyMapping
        }

        /// <summary>
        /// Type of handler.
        /// </summary>
        internal TypesOfHandler TypeOfHandler;

        /// <summary>
        /// Replace existing delegate.
        /// </summary>
        public Boolean ReplaceExistingDelegate {
            get;
            set;
        }

        /// <summary>
        /// Handler level.
        /// </summary>
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
        /// Substitute handler.
        /// </summary>
        public Func<Response> SubstituteHandler {
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
        /// Constructor with handler level.
        /// </summary>
        public HandlerOptions(HandlerLevels handlerLevel) {
            handlerLevel_ = handlerLevel;
        }

        /// <summary>
        /// Constructor without handler level.
        /// </summary>
        public HandlerOptions() {
        }
    }

    /// <summary>
    /// Represents a middleware filter that is called for external
    /// requests before the actual user handler.
    /// </summary>
    public class MiddlewareFilter {

        /// <summary>
        /// Application name that registered this handler.
        /// </summary>
        public string AppName {
            get;
            set;
        }

        /// <summary>
        /// The actual filter delegate.
        /// </summary>
        public Func<Request, Response> Filter {
            get;
            set;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="filterRequest">Filter request parameter.</param>
        public MiddlewareFilter(Func<Request, Response> filter) {
            AppName = StarcounterEnvironment.AppName;
            Filter = filter;
        }
    }

    /// <summary>
    /// Represents an outgoing response filter.
    /// </summary>
    public class OutgoingFilter {

        /// <summary>
        /// Application name that registered this handler.
        /// </summary>
        public string AppName {
            get;
            set;
        }

        /// <summary>
        /// The actual filter delegate.
        /// </summary>
        public Func<Request, Response, Response> Filter {
            get;
            set;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="filterRequest">Outgoing response parameter.</param>
        public OutgoingFilter(Func<Request, Response, Response> filter) {
            AppName = StarcounterEnvironment.AppName;
            Filter = filter;
        }
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

        /// <summary>
        /// Incoming request reference.
        /// </summary>
        [ThreadStatic]
        static Request incomingRequest_;

        /// <summary>
        /// Incoming external request.
        /// </summary>
        public static Request IncomingRequest {
            get {
                return incomingRequest_;
            }

            internal set {
                incomingRequest_ = value;
            }
        }

        /// <summary>
        /// Outgoing cookies.
        /// </summary>
        [ThreadStatic]
        static Dictionary<String, String> outgoingCookies_;

        /// <summary>
        /// Outgoing HTTP cookies list.
        /// </summary>
        internal static Dictionary<String, String> OutgoingCookies {
            get {
                return outgoingCookies_;
            }
        }

        /// <summary>
        /// Adding cookie to outgoing HTTP response.
        /// </summary>
        public static void AddOutgoingCookie(String cookieName, String cookieValue) {

            if (null == outgoingCookies_) {
                outgoingCookies_ = new Dictionary<String, String>(StringComparer.InvariantCultureIgnoreCase);
            }

            outgoingCookies_[cookieName] = cookieValue;
        }

        /// <summary>
        /// Outgoing HTTP headers.
        /// </summary>
        [ThreadStatic]
        static Dictionary<String, String> outgoingHeaders_;

        /// <summary>
        /// Outgoing HTTP headers list.
        /// </summary>
        internal static Dictionary<String, String> OutgoingHeaders {
            get {
                return outgoingHeaders_;
            }
        }

        /// <summary>
        /// Adding HTTP header to outgoing response.
        /// </summary>
        public static void AddOutgoingHeader(String headerName, String headerValue) {

            if (null == outgoingHeaders_) {
                outgoingHeaders_ = new Dictionary<String, String>(StringComparer.InvariantCultureIgnoreCase);
            }

            outgoingHeaders_[headerName] = headerValue;
        }

        /// <summary>
        /// Outgoing HTTP status description.
        /// </summary>
        [ThreadStatic]
        static String outgoingStatusDescription_;

        /// <summary>
        /// Outgoing HTTP status code.
        /// </summary>
        internal static String OutgoingStatusDescription {
            get {
                return outgoingStatusDescription_;
            }
        }

        /// <summary>
        /// Setting status description for outgoing HTTP response.
        /// </summary>
        public static void SetOutgoingStatusDescription(String statusDescription) {
            outgoingStatusDescription_ = statusDescription;
        }

        /// <summary>
        /// Outgoing HTTP status code.
        /// </summary>
        [ThreadStatic]
        static UInt16 outgoingStatusCode_;

        /// <summary>
        /// Outgoing HTTP status code.
        /// </summary>
        internal static UInt16 OutgoingStatusCode {
            get {
                return outgoingStatusCode_;
            }
        }

        /// <summary>
        /// Setting status code for outgoing HTTP response.
        /// </summary>
        public static void SetOutgoingStatusCode(UInt16 statusCode) {
            outgoingStatusCode_ = statusCode;
        }

        /// <summary>
        /// Incoming request reference.
        /// </summary>
        [ThreadStatic]
        static Int32 callLevel_;

        /// <summary>
        /// Incoming external request.
        /// </summary>
        public static Int32 CallLevel {
            get {
                return callLevel_;
            }

            set {
                callLevel_ = value;
            }
        }

        /// <summary>
        /// Resetting all outgoing fields for new request.
        /// </summary>
        internal static void ResetAllOutgoingFields() {

            incomingRequest_ = null;
            outgoingCookies_ = null;
            outgoingHeaders_ = null;
            outgoingStatusDescription_ = null;
            outgoingStatusCode_ = 0;
        }

        public const String GET_METHOD = "GET";
        public const String PUT_METHOD = "PUT";
        public const String POST_METHOD = "POST";
        public const String DELETE_METHOD = "DELETE";
        public const String PATCH_METHOD = "PATCH";
        public const String OPTIONS_METHOD = "OPTIONS";

        /// <summary>
        /// Checks if given URI is a part of 
        /// </summary>
        /// <param name="uri">Incomming URI.</param>
        /// <returns>True if its a system handler.</returns>
        public static Boolean IsSystemHandlerUri(String uri) {

            if (uri.StartsWith("/polyjuice", StringComparison.InvariantCultureIgnoreCase) ||
                uri.StartsWith("/codehost", StringComparison.InvariantCultureIgnoreCase) ||
                uri.StartsWith("/_", StringComparison.InvariantCultureIgnoreCase)) {

                return true;
            }

            return false;
        }

        internal static Func<String, HandlerOptions, Boolean> isHandlerRegistered_;

        /// <summary>
        /// Checks if given URI handler is registered.
        /// </summary>
        public static Boolean IsHandlerRegistered(String methodSpaceProcessedUriSpace, HandlerOptions ho) {

            return isHandlerRegistered_(methodSpaceProcessedUriSpace, ho);
        }

        internal static Func<String, Response> customResourceNotFoundResolver_;

        /// <summary>
        /// Setting custom not-found resource resolver.
        /// </summary>
        public static void SetCustomResourceNotFoundResolver(Func<String, Response> customResourceNotFoundResolver) {

            customResourceNotFoundResolver_ = customResourceNotFoundResolver;
        }

        /// <summary>
        /// Filtering request.
        /// </summary>
        public static List<MiddlewareFilter> middlewareFilters_ = new List<MiddlewareFilter>();

        /// <summary>
        /// Saved middleware filters list.
        /// </summary>
        public static List<MiddlewareFilter> savedMiddlewareFilters_ = new List<MiddlewareFilter>();

        /// <summary>
        /// Enable/Disable middleware filters.
        /// </summary>
        public static void EnableDisableMiddleware(Boolean enable) {

            if (enable) {

                middlewareFilters_ = savedMiddlewareFilters_;

            } else {

                savedMiddlewareFilters_ = middlewareFilters_;
                middlewareFilters_ = new List<MiddlewareFilter>();
            }
        }

        /// <summary>
        /// Adding new filter to middleware.
        /// </summary>
        public static void AddFilterToMiddleware(Func<Request, Response> filter) {

            MiddlewareFilter mf = new MiddlewareFilter(filter);

            middlewareFilters_.Add(mf);
        }

        /// <summary>
        /// Runs all added middleware filters until one that returns non-null response.
        /// </summary>
        /// <returns>Filtered response or null.</returns>
        internal static Response RunMiddlewareFilters(Request req) {

            String curAppName = StarcounterEnvironment.AppName;

            try {

                for (Int32 i = (middlewareFilters_.Count - 1); i >= 0; i--) {

                    MiddlewareFilter mf = middlewareFilters_[i];

                    StarcounterEnvironment.AppName = mf.AppName;

                    Response resp = mf.Filter(req);

                    if (null != resp)
                        return resp;
                }

            } finally {

                StarcounterEnvironment.AppName = curAppName;
            }
            
            return null;
        }

        /// <summary>
        /// Outgoing response filter.
        /// </summary>
        public static List<OutgoingFilter> outgoingFilters_ = new List<OutgoingFilter>();

        /// <summary>
        /// Saved outgoing filters list.
        /// </summary>
        public static List<OutgoingFilter> savedOutgoingFilters_ = new List<OutgoingFilter>();

        /// <summary>
        /// Enable/Disable outgoing filters.
        /// </summary>
        public static void EnableDisableOutgoingFilter(Boolean enable) {

            if (enable) {

                outgoingFilters_ = savedOutgoingFilters_;

            } else {

                savedOutgoingFilters_ = outgoingFilters_;

                outgoingFilters_ = new List<OutgoingFilter>();
            }
        }

        /// <summary>
        /// Adding new filter to outgoing.
        /// </summary>
        public static void AddOutgoingFilter(Func<Request, Response, Response> filter) {

            OutgoingFilter mf = new OutgoingFilter(filter);

            outgoingFilters_.Add(mf);
        }

        /// <summary>
        /// Runs all added outgoing filters until one that returns non-null response.
        /// </summary>
        /// <returns>Filtered response or null.</returns>
        internal static Response RunOutgoingFilters(Request req, Response resp) {

            String curAppName = StarcounterEnvironment.AppName;

            try {

                for (Int32 i = (outgoingFilters_.Count - 1); i >= 0; i--) {

                    OutgoingFilter mf = outgoingFilters_[i];

                    StarcounterEnvironment.AppName = mf.AppName;

                    Response response = mf.Filter(req, resp);

                    if (null != response)
                        return response;
                }

            } finally {

                StarcounterEnvironment.AppName = curAppName;
            }

            return null;
        }

        /// <summary>
        /// Indicator of parameter in URI.
        /// </summary>
        public const String UriParameterIndicator = "{?}";

        /// <summary>
        /// Inject REST handler function provider here
        /// </summary>
        public static volatile IREST _REST;

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

        public static void OPTIONS(String uriTemplate, Func<Response> handler, HandlerOptions ho = null) {
            _REST.RegisterHandler(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, OPTIONS_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void OPTIONS(ushort port, String uriTemplate, Func<Response> handler, HandlerOptions ho = null) {
            _REST.RegisterHandler(port, OPTIONS_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void OPTIONS<T>(String uriTemplate, Func<T, Response> handler, HandlerOptions ho = null) {
            _REST.RegisterHandler<T>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, OPTIONS_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void OPTIONS<T>(ushort port, String uriTemplate, Func<T, Response> handler, HandlerOptions ho = null) {
            _REST.RegisterHandler<T>(port, OPTIONS_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void OPTIONS<T1, T2>(String uriTemplate, Func<T1, T2, Response> handler, HandlerOptions ho = null) {
            _REST.RegisterHandler<T1, T2>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, OPTIONS_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void OPTIONS<T1, T2>(ushort port, String uriTemplate, Func<T1, T2, Response> handler, HandlerOptions ho = null) {
            _REST.RegisterHandler<T1, T2>(port, OPTIONS_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void OPTIONS<T1, T2, T3>(String uriTemplate, Func<T1, T2, T3, Response> handler, HandlerOptions ho = null) {
            _REST.RegisterHandler<T1, T2, T3>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, OPTIONS_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void OPTIONS<T1, T2, T3>(ushort port, String uriTemplate, Func<T1, T2, T3, Response> handler, HandlerOptions ho = null) {
            _REST.RegisterHandler<T1, T2, T3>(port, OPTIONS_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void OPTIONS<T1, T2, T3, T4>(String uriTemplate, Func<T1, T2, T3, T4, Response> handler, HandlerOptions ho = null) {
            _REST.RegisterHandler<T1, T2, T3, T4>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, OPTIONS_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void OPTIONS<T1, T2, T3, T4>(ushort port, String uriTemplate, Func<T1, T2, T3, T4, Response> handler, HandlerOptions ho = null) {
            _REST.RegisterHandler<T1, T2, T3, T4>(port, OPTIONS_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void OPTIONS<T1, T2, T3, T4, T5>(String uriTemplate, Func<T1, T2, T3, T4, T5, Response> handler, HandlerOptions ho = null) {
            _REST.RegisterHandler<T1, T2, T3, T4, T5>(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, OPTIONS_METHOD + " " + uriTemplate, handler, ho);
        }

        public static void OPTIONS<T1, T2, T3, T4, T5>(ushort port, String uriTemplate, Func<T1, T2, T3, T4, T5, Response> handler, HandlerOptions ho = null) {
            _REST.RegisterHandler<T1, T2, T3, T4, T5>(port, OPTIONS_METHOD + " " + uriTemplate, handler, ho);
        }
    }
}
