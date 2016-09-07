#define CASE_INSENSITIVE_URI_MATCHER

using System;
using System.Net;
using Starcounter.Internal;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;

namespace Starcounter {

    public class Self {

        /// <summary>
        /// Performs HTTP GET.
        /// </summary>
        public static T GET<T>(String uri, HandlerOptions ho = null) {

            Response resp = GET(uri, null, ho);

            if (null != resp)
                return resp.GetContent<T>();

            return default(T);
        }

        /// <summary>
        /// Performs HTTP GET.
        /// </summary>
        public static T GET<T>(UInt16 port, String uri, HandlerOptions ho = null) {

            Response resp = GET(port, uri, null, ho);

            if (null != resp)
                return resp.GetContent<T>();

            return default(T);
        }

        /// <summary>
        /// Performs HTTP GET.
        /// </summary>
        public static T GET<T>(String uri, Func<Response> substituteHandler) {

            Response resp = GET(uri, substituteHandler);

            if (null != resp)
                return resp.GetContent<T>();

            return default(T);
        }

        /// <summary>
        /// Performs HTTP GET.
        /// </summary>
        public static T GET<T>(UInt16 port, String uri, Func<Response> substituteHandler) {

            Response resp = GET(port, uri, substituteHandler);

            if (null != resp)
                return resp.GetContent<T>();

            return default(T);
        }

        /// <summary>
        /// Performs HTTP GET.
        /// </summary>
        public static Response GET(String uri) {

            return GET(uri, null, null);
        }

        /// <summary>
        /// Performs HTTP GET.
        /// </summary>
        public static Response GET(UInt16 port, String uri) {

            return GET(port, uri, null, null);
        }

        /// <summary>
        /// Performs HTTP GET and provides substitute handler.
        /// </summary>
        public static Response GET(String uri, Func<Response> substituteHandler) {

            HandlerOptions ho = new HandlerOptions() {
                SubstituteHandler = substituteHandler
            };

            return GET(uri, null, ho);
        }

        /// <summary>
        /// Performs HTTP GET and provides substitute handler.
        /// </summary>
        public static Response GET(UInt16 port, String uri, Func<Response> substituteHandler) {

            HandlerOptions ho = new HandlerOptions() {
                SubstituteHandler = substituteHandler
            };

            return GET(port, uri, null, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP GET.
        /// </summary>
        public static Response GET(
            String uri,
            Dictionary<String, String> headersDictionary = null,
            HandlerOptions ho = null) {

            return GET(
                StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort,
                uri, 
                headersDictionary,
                ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP GET.
        /// </summary>
        public static Response GET(
            UInt16 port,
            String uri,
            Dictionary<String, String> headersDictionary = null,
            HandlerOptions ho = null) {

            return DoSelfCall(
                port,
                Handle.GET_METHOD,
                uri,
                headersDictionary,
                null,
                null,
                ho,
                null);
        }

        /// <summary>
        /// Performs asynchronous HTTP POST.
        /// </summary>
        public static Response POST(
            String uri, 
            String body,
            Byte[] bodyBytes = null,
            Dictionary<String, String> headersDictionary = null,
            UInt16 port = StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort,
            HandlerOptions ho = null) {

            return DoSelfCall(
                port,
                Handle.POST_METHOD,
                uri,
                headersDictionary,
                body,
                bodyBytes,
                ho,
                null);
        }

        /// <summary>
        /// Performs asynchronous HTTP PUT.
        /// </summary>
        public static Response PUT(
            String uri, 
            String body,
            Byte[] bodyBytes = null,
            Dictionary<String, String> headersDictionary = null,
            UInt16 port = StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort,
            HandlerOptions ho = null) {

            return DoSelfCall(
                port,
                Handle.PUT_METHOD,
                uri,
                headersDictionary,
                body,
                bodyBytes,
                ho,
                null);
        }

        /// <summary>
        /// Performs asynchronous HTTP PATCH.
        /// </summary>
        public static Response PATCH(
            String uri, 
            String body,
            Byte[] bodyBytes = null,
            Dictionary<String, String> headersDictionary = null,
            UInt16 port = StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort,
            HandlerOptions ho = null) {

            return DoSelfCall(
                port,
                Handle.PATCH_METHOD,
                uri,
                headersDictionary,
                body,
                bodyBytes,
                ho,
                null);
        }

        /// <summary>
        /// Performs asynchronous HTTP DELETE.
        /// </summary>
        public static Response DELETE(
            String uri,
            String body,
            Byte[] bodyBytes = null,
            Dictionary<String, String> headersDictionary = null,
            UInt16 port = StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort,
            HandlerOptions ho = null) {

            return DoSelfCall(
                port,
                Handle.DELETE_METHOD,
                uri,
                headersDictionary,
                body,
                bodyBytes,
                ho,
                null);
        }

        /// <summary>
        /// Performs synchronous HTTP request with given HTTP method.
        /// </summary>
        public static Response CustomRESTRequest(
            String method,
            String uri,
            String body,
            Byte[] bodyBytes = null,
            Dictionary<String, String> headersDictionary = null,
            UInt16 port = StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort,
            HandlerOptions ho = null) {

            return DoSelfCall(
                port,
                method,
                uri,
                headersDictionary,
                body,
                bodyBytes,
                ho,
                null);
        }

        /// <summary>
        /// Performs synchronous HTTP request with given HTTP method.
        /// </summary>
        public static Response CustomRESTRequest(Request req, HandlerOptions ho = null) {

            return DoSelfCall(
                req.PortNumber,
                req.Method,
                req.Uri,
                null,
                null,
                null,
                ho,
                req);
        }

        /// <summary>
        /// Performs synchronous HTTP request with given original external request.
        /// </summary>
        public static Response CallUsingExternalRequest(Request req, Func<Response> substituteHandler) {

            HandlerOptions ho = new HandlerOptions() {
                SubstituteHandler = substituteHandler,
                CallingAppName = StarcounterEnvironment.AppName
            };

            // Setting handler options to request.
            req.HandlerOpts = ho;

            // Calling using external request.
            return runDelegateAndProcessResponse_(
                req.GetRawMethodAndUri(),
                req.GetRawParametersInfo(),
                req);
        }

        /// <summary>
        /// Delegate to run URI matcher and call handler.
        /// </summary>
        internal delegate Boolean RunUriMatcherAndCallHandlerDelegate(
            String methodSpaceUriSpace,
            String methodSpaceUriSpaceLower,
            Request req,
            UInt16 portNumber,
            out Response resp);

        /// <summary>
        /// Performs local REST call.
        /// </summary>
        static RunUriMatcherAndCallHandlerDelegate runUriMatcherAndCallHandler_;

        // Runs external response.
        static Func<IntPtr, IntPtr, Request, Response> runDelegateAndProcessResponse_;

        /// <summary>
        /// Initializes delegates.
        /// </summary>
        internal static void InjectDelegates(
            RunUriMatcherAndCallHandlerDelegate runUriMatcherAndCallHandler,
            Func<IntPtr, IntPtr, Request, Response> runDelegateAndProcessResponse) {

            runUriMatcherAndCallHandler_ = runUriMatcherAndCallHandler;
            runDelegateAndProcessResponse_ = runDelegateAndProcessResponse;
        }

        /// <summary>
        /// Perform Self call.
        /// </summary>
        static Response DoSelfCall(
            UInt16 portNumber,
            String method,
            String relativeUri,
            Dictionary<String, String> headersDictionary,
            String body,
            Byte[] bodyBytes,
            HandlerOptions handlerOptions,
            Request req) {

            // Checking if we are not on scheduler.
            if (!StarcounterEnvironment.IsStarcounterThread()) {
                throw new InvalidOperationException("You are trying to perform a Self call while not being on Starcounter thread.");
            }

            // Checking if URI starts with a slash.
            if (String.IsNullOrEmpty(relativeUri) || relativeUri[0] != '/') {
                throw new ArgumentOutOfRangeException(relativeUri, "Self should be called with URI starting with a slash.");
            }

            // Checking if port is not specified.
            if (StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort == portNumber) {
                if (StarcounterEnvironment.IsAdministratorApp) {
                    portNumber = StarcounterEnvironment.Default.SystemHttpPort;
                } else {
                    portNumber = StarcounterEnvironment.Default.UserHttpPort;
                }
            }

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
                    Body = body,
                    HeadersDictionary = headersDictionary,
                    Host = "localhost:" + portNumber
                };
            }

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
                portNumber,
                out resp);

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
                    };
                }

                // Checking if there is a substitute handler.
                if (req.HandlerOpts.SubstituteHandler != null) {

                    resp = req.HandlerOpts.SubstituteHandler();

                    if (resp != null) {

                        // Setting the response application name.
                        resp.AppName = req.HandlerOpts.CallingAppName;

                        if (StarcounterEnvironment.MergeJsonSiblings) {
                            return Response.ResponsesMergerRoutine_(req, resp, null);
                        }
                    }

                    return resp;
                }

                if (true == callOnlySpecificHandlerLevel) {

                    // NOTE: We tried a specific handler level but didn't get any response, so returning.
                    return null;
                }

            } else {

                // Checking if there is some response.
                if (resp != null) {
                    return resp;
                }
            }

            return null;
        }
    }
}