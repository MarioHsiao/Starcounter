// ***********************************************************************
// <copyright file="ScUri.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

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

    public class Http {

        /// <summary>
        /// Retrieves endpoint and relative URI information from URI.
        /// </summary>
        internal static void GetEndpointFromUri(String uri, out String endpoint, out String relativeUri) {
            // Checking if its a localhost communication.
            if ('/' == uri[0]) {
                endpoint = "127.0.0.1";
                relativeUri = uri;

                return;
            }

            endpoint = "";
            relativeUri = "/";

            Int32 startEndpoint = -1, endEndpoint = uri.Length - 1;

            for (Int32 i = 0; i < uri.Length; i++) {
                Char s = uri[i];
                if ('/' == uri[i]) {
                    if (startEndpoint < 0) {
                        if (':' == uri[i - 1]) {
                            startEndpoint = i + 2;
                            i += 2;
                        } else {
                            startEndpoint = 0;
                            endEndpoint = i - 1;
                            break;
                        }
                    } else {
                        endEndpoint = i - 1;
                        break;
                    }
                }
            }

            if (startEndpoint < 0) {
                endpoint = uri;
                return;
            }

            Int32 endpointLen = endEndpoint - startEndpoint + 1;

            endpoint = uri.Substring(startEndpoint, endpointLen);

            if (endEndpoint < (uri.Length - 1))
                relativeUri = uri.Substring(endEndpoint + 1);
        }

        // Nodes cache when used for Starcounter client.
        [ThreadStatic]
        static Dictionary<String, Node> threadStaticNodeDict_;

        /// <summary>
        /// Gets node instance from given URI.
        /// </summary>
        internal static void GetNodeFromUri(UInt16 port, String uri, out Node node, out String relativeUri) {

            // Checking for URI contents.
            if ((uri == null) || (!uri.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase))) {
                throw new ArgumentOutOfRangeException("URI should start with \"http://\" prefix.");
            }

            // Checking if static object is initialized.
            if (null == threadStaticNodeDict_) {
                threadStaticNodeDict_ = new Dictionary<String, Node>();
            }
                
            // Calculating endpoint name from given URI.
            String endpoint;
            GetEndpointFromUri(uri, out endpoint, out relativeUri);

            // Trying to get node instance from node cache.
            if (!threadStaticNodeDict_.TryGetValue(endpoint, out node)) {

                String endpointWithoutPort = endpoint;
                UInt16 destPort = port;

                Int32 i = endpoint.Length - 2, colonPos = -1;

                // Checking if port is defined.
                while (i >= 0) {
                    if (endpoint[i] == ':') {
                        colonPos = i;
                        break;
                    }
                    i--;
                }

                // Checking if port is specified within URI.
                if (colonPos > 0) {
                    destPort = UInt16.Parse(endpoint.Substring(colonPos + 1));
                    endpointWithoutPort = endpoint.Substring(0, colonPos);
                }

                // Checking if port is defined.
                if (StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort == destPort) {
                    destPort = 80;
                }

                node = new Node(endpointWithoutPort, destPort);

                // Adding node to dictionary.
                threadStaticNodeDict_.Add(endpoint, node);
            }
        }

        /// <summary>
        /// Performs HTTP GET.
        /// </summary>
        public static T GET<T>(String uri, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null) {

            Response resp = GET(uri, null, receiveTimeoutSeconds, ho);

            if (null != resp)
                return resp.GetContent<T>();

            return default(T);
        }

        /// <summary>
        /// Performs HTTP GET.
        /// </summary>
        public static T GET<T>(UInt16 port, String uri, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null) {

            Response resp = GET(port, uri, null, receiveTimeoutSeconds, ho);

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

            Response resp = Http.GET(uri, null, 0, null);
            return resp;
        }

        /// <summary>
        /// Performs HTTP GET.
        /// </summary>
        public static Response GET(UInt16 port, String uri) {

            Response resp = Http.GET(port, uri, null, 0, null);
            return resp;
        }

        /// <summary>
        /// Performs HTTP GET and provides substitute handler.
        /// </summary>
        public static Response GET(String uri, Func<Response> substituteHandler) {

            HandlerOptions ho = new HandlerOptions() {
                SubstituteHandler = substituteHandler
            };

            Response resp = Http.GET(uri, null, 0, ho);
            return resp;
        }

        /// <summary>
        /// Performs HTTP GET and provides substitute handler.
        /// </summary>
        public static Response GET(UInt16 port, String uri, Func<Response> substituteHandler) {

            HandlerOptions ho = new HandlerOptions() {
                SubstituteHandler = substituteHandler
            };

            Response resp = Http.GET(port, uri, null, 0, ho);
            return resp;
        }

        /// <summary>
        /// Performs asynchronous HTTP GET.
        /// </summary>
        public static Response GET(
            String uri,
            Dictionary<String, String> headersDictionary = null,
            Int32 receiveTimeoutSeconds = 0,
            HandlerOptions ho = null) {

            Node node;
            String relativeUri;

            GetNodeFromUri(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, uri, out node, out relativeUri);

            return node.GET(relativeUri, headersDictionary, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP GET.
        /// </summary>
        public static Response GET(
            UInt16 port,
            String uri,
            Dictionary<String, String> headersDictionary = null,
            Int32 receiveTimeoutSeconds = 0,
            HandlerOptions ho = null) {

            Node node;
            String relativeUri;

            GetNodeFromUri(port, uri, out node, out relativeUri);

            return node.GET(relativeUri, headersDictionary, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP GET.
        /// </summary>
        public static void GET(
            String uri,
            Dictionary<String, String> headersDictionary,
            Action<Response> userDelegate,
            Int32 receiveTimeoutSeconds = 0,
            HandlerOptions ho = null) {

            Node node;
            String relativeUri;

            GetNodeFromUri(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, uri, out node, out relativeUri);

            node.GET(relativeUri, headersDictionary, userDelegate, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP GET.
        /// </summary>
        public static void GET(
            UInt16 port,
            String uri,
            Dictionary<String, String> headersDictionary,
            Action<Response> userDelegate,
            Int32 receiveTimeoutSeconds = 0,
            HandlerOptions ho = null) {

            Node node;
            String relativeUri;

            GetNodeFromUri(port, uri, out node, out relativeUri);

            node.GET(relativeUri, headersDictionary, userDelegate, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP POST.
        /// </summary>
        public static void POST(String uri, String body, Dictionary<String, String> headersDictionary, Action<Response> userDelegate, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null) {
            Node node;
            String relativeUri;

            GetNodeFromUri(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, uri, out node, out relativeUri);

            node.POST(relativeUri, body, headersDictionary, userDelegate, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP POST.
        /// </summary>
        public static void POST(String uri, Byte[] bodyBytes, Dictionary<String, String> headersDictionary, Action<Response> userDelegate, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null) {
            Node node;
            String relativeUri;

            GetNodeFromUri(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, uri, out node, out relativeUri);

            node.POST(relativeUri, bodyBytes, headersDictionary, userDelegate, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP POST.
        /// </summary>
        public static Response POST(String uri, String body, Dictionary<String, String> headersDictionary, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null) {
            Node node;
            String relativeUri;

            GetNodeFromUri(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, uri, out node, out relativeUri);

            return node.POST(relativeUri, body, headersDictionary, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP POST.
        /// </summary>
        public static Response POST(String uri, Byte[] bodyBytes, Dictionary<String, String> headersDictionary, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null) {
            Node node;
            String relativeUri;

            GetNodeFromUri(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, uri, out node, out relativeUri);

            return node.POST(relativeUri, bodyBytes, headersDictionary, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP PUT.
        /// </summary>
        public static void PUT(String uri, String body, Dictionary<String, String> headersDictionary, Action<Response> userDelegate, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null) {
            Node node;
            String relativeUri;

            GetNodeFromUri(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, uri, out node, out relativeUri);

            node.PUT(relativeUri, body, headersDictionary, userDelegate, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP PUT.
        /// </summary>
        public static void PUT(String uri, Byte[] bodyBytes, Dictionary<String, String> headersDictionary, Action<Response> userDelegate, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null) {
            Node node;
            String relativeUri;

            GetNodeFromUri(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, uri, out node, out relativeUri);

            node.PUT(relativeUri, bodyBytes, headersDictionary, userDelegate, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP PUT.
        /// </summary>
        public static Response PUT(String uri, String body, Dictionary<String, String> headersDictionary, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null) {
            Node node;
            String relativeUri;

            GetNodeFromUri(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, uri, out node, out relativeUri);

            return node.PUT(relativeUri, body, headersDictionary, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP PUT.
        /// </summary>
        public static Response PUT(String uri, Byte[] bodyBytes, Dictionary<String, String> headersDictionary, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null) {
            Node node;
            String relativeUri;

            GetNodeFromUri(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, uri, out node, out relativeUri);

            return node.PUT(relativeUri, bodyBytes, headersDictionary, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP PATCH.
        /// </summary>
        public static void PATCH(String uri, String body, Dictionary<String, String> headersDictionary, Action<Response> userDelegate, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null) {
            Node node;
            String relativeUri;

            GetNodeFromUri(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, uri, out node, out relativeUri);

            node.PATCH(relativeUri, body, headersDictionary, userDelegate, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP PATCH.
        /// </summary>
        public static void PATCH(String uri, Byte[] bodyBytes, Dictionary<String, String> headersDictionary, Action<Response> userDelegate, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null) {
            Node node;
            String relativeUri;

            GetNodeFromUri(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, uri, out node, out relativeUri);

            node.PATCH(relativeUri, bodyBytes, headersDictionary, userDelegate, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP PATCH.
        /// </summary>
        public static Response PATCH(String uri, String body, Dictionary<String, String> headersDictionary, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null) {
            Node node;
            String relativeUri;

            GetNodeFromUri(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, uri, out node, out relativeUri);

            return node.PATCH(relativeUri, body, headersDictionary, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP PATCH.
        /// </summary>
        public static Response PATCH(String uri, Byte[] bodyBytes, Dictionary<String, String> headersDictionary, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null) {
            Node node;
            String relativeUri;

            GetNodeFromUri(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, uri, out node, out relativeUri);

            return node.PATCH(relativeUri, bodyBytes, headersDictionary, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP DELETE.
        /// </summary>
        public static void DELETE(String uri, String body, Dictionary<String, String> headersDictionary, Action<Response> userDelegate, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null) {
            Node node;
            String relativeUri;

            GetNodeFromUri(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, uri, out node, out relativeUri);

            node.DELETE(relativeUri, body, headersDictionary, userDelegate, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP DELETE.
        /// </summary>
        public static void DELETE(String uri, Byte[] bodyBytes, Dictionary<String, String> headersDictionary, Action<Response> userDelegate, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null) {
            Node node;
            String relativeUri;

            GetNodeFromUri(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, uri, out node, out relativeUri);

            node.DELETE(relativeUri, bodyBytes, headersDictionary, userDelegate, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP DELETE.
        /// </summary>
        public static Response DELETE(String uri, String body, Dictionary<String, String> headersDictionary, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null) {
            Node node;
            String relativeUri;

            GetNodeFromUri(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, uri, out node, out relativeUri);

            return node.DELETE(relativeUri, body, headersDictionary, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP DELETE.
        /// </summary>
        public static Response DELETE(String uri, Byte[] bodyBytes, Dictionary<String, String> headersDictionary, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null) {
            Node node;
            String relativeUri;

            GetNodeFromUri(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, uri, out node, out relativeUri);

            return node.DELETE(relativeUri, bodyBytes, headersDictionary, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP request with given HTTP method.
        /// </summary>
        public static void CustomRESTRequest(String method, String uri, String body, Dictionary<String, String> headersDictionary, Action<Response> userDelegate, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null) {
            Node node;
            String relativeUri;

            GetNodeFromUri(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, uri, out node, out relativeUri);

            node.CustomRESTRequest(method, relativeUri, body, headersDictionary, userDelegate, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP request with given HTTP method.
        /// </summary>
        public static void CustomRESTRequest(String method, String uri, Byte[] bodyBytes, Dictionary<String, String> headersDictionary, Action<Response> userDelegate, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null) {
            Node node;
            String relativeUri;

            GetNodeFromUri(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, uri, out node, out relativeUri);

            node.CustomRESTRequest(method, relativeUri, bodyBytes, headersDictionary, userDelegate, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP request with given HTTP method.
        /// </summary>
        public static Response CustomRESTRequest(String method, String uri, String body, Dictionary<String, String> headersDictionary, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null) {
            Node node;
            String relativeUri;

            GetNodeFromUri(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, uri, out node, out relativeUri);

            return node.CustomRESTRequest(method, relativeUri, body, headersDictionary, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP request with given HTTP method.
        /// </summary>
        public static Response CustomRESTRequest(String method, String uri, Byte[] bodyBytes, Dictionary<String, String> headersDictionary, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null) {
            Node node;
            String relativeUri;

            GetNodeFromUri(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, uri, out node, out relativeUri);

            return node.CustomRESTRequest(method, relativeUri, bodyBytes, headersDictionary, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs asynchronous HTTP request with given HTTP method.
        /// </summary>
        public static void CustomRESTRequest(Request req, Action<Response> userDelegate, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null) {
            Node node;
            String relativeUri;

            GetNodeFromUri(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, req.Uri, out node, out relativeUri);

            node.CustomRESTRequest(req, userDelegate, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP request with given HTTP method.
        /// </summary>
        public static Response CustomRESTRequest(Request req, Int32 receiveTimeoutSeconds = 0, HandlerOptions ho = null) {
            Node node;
            String relativeUri;

            GetNodeFromUri(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, req.Uri, out node, out relativeUri);

            return node.CustomRESTRequest(req, receiveTimeoutSeconds, ho);
        }

        /// <summary>
        /// Performs synchronous HTTP request with given original external request.
        /// </summary>
        public static Response CallUsingExternalRequest(Request req, Func<Response> substituteHandler) {

            HandlerOptions ho = new HandlerOptions() {
                SubstituteHandler = substituteHandler,
                HandlerId = req.ManagedHandlerId,
                ParametersInfo = req.GetParametersInfo()
            };

            Node node;
            String relativeUri;

            GetNodeFromUri(StarcounterConstants.NetworkPorts.DefaultUnspecifiedPort, req.Uri, out node, out relativeUri);

            return node.CustomRESTRequest(req, 0, ho);
        }
    }
}