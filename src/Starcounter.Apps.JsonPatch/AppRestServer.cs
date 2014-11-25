//#define STUB_AGGREGATED

// ***********************************************************************
// <copyright file="AppServer.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Text;
using Starcounter.Internal.REST;
using Starcounter.Advanced;
using System.Net;
using Codeplex.Data;
using System.Collections.Generic;
using Starcounter.Rest;
using Starcounter.Logging;
using System.Diagnostics;

namespace Starcounter.Internal.Web {
    /// <summary>
    /// Wraps the file based http web resource resolver and the App view model resolver.
    /// </summary>
    /// <remarks>Supports Http as well as proprietary protocols.
    /// If the URI does not point to a App view model or a user implemented
    /// handler, the request is routed to a standard file based static resource
    /// web serving implementation that will serve html, png, jpg etc. using the file system.
    /// This file based resolver will be injected into the constructor of this class.</remarks>
    public partial class AppRestServer : IRestServer {

        /// <summary>
        /// A standard file based static resource web serving implementation.
        /// Will serve html, png, jpg etc. using the file system.
        /// If the URI does not point to a App view model or a user implemented
        /// handler, this is where the request will go.
        /// </summary>
        private Dictionary<UInt16, StaticWebServer> staticFileServers_;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppRestServer" /> class.
        /// </summary>
        /// <param name="staticFileServer">The static file server.</param>
        public AppRestServer(Dictionary<UInt16, StaticWebServer> staticFileServer) {
            staticFileServers_ = staticFileServer;
        }

        /// <summary>
        /// Handles the response returned from the user handler.
        /// </summary>
        /// <param name="request">Incomming HTTP request.</param>
        /// <param name="response">Result of calling user handler (i.e. the delegate).</param>
        /// <returns>The same object as provide in the response parameter</returns>
        public Response OnResponseHttp(Request req, Response resp) {
            Debug.Assert(resp != null);

#if STUB_AGGREGATED

            if (req.IsAggregated)
                return SchedulerResources.Current.AggregationStubResponse;
#endif

            try {
                // Checking if we need to resolve static resource.
                if (resp.HandlingStatus == HandlerStatusInternal.ResolveStaticContent) {
                    resp = ResolveAndPrepareFile(req.Uri, req);
                    resp.HandlingStatus = HandlerStatusInternal.Done;
                }
                else {
                    // NOTE: Checking if its internal request then just returning response without modification.
                    if (req.IsInternal)
                        return resp;

                    // Checking if JSON object is attached.
                    if (resp.Resource is Json) {
                        Json r = (Json)resp.Resource;

                        while (r.Parent != null)
                            r = r.Parent;

                        resp.Resource = (Json)r;
                    }
                }

                resp.Request = req;

                return resp;
            }
            catch (Exception ex) {
                // Logging the exception to server log.
                LogSources.Hosting.LogException(ex);
                var errResp = Response.FromStatusCode(500);
                errResp.Body = GetExceptionString(ex);
                errResp.ContentType = "text/plain";
                return errResp;
            }
        }

        /// <summary>
        /// Handles request.
        /// </summary>
        public Response HandleRequest(Request request, HandlerOptions.HandlerLevels handlerLevel) {

            Response resp;

            try {
                return _HandleRequest(request, handlerLevel);
            }
            catch (ResponseException exc) {
                // NOTE: if internal request then throw the exception up.
                if (request.IsInternal)
                    throw exc;

                resp = exc.ResponseObject;
                resp.ConnFlags = Response.ConnectionFlags.DisconnectAfterSend;
                return resp;
            }
            catch (UriInjectMethods.IncorrectSessionException) {
                resp = Response.FromStatusCode(400);
                resp["Connection"] = "close";
                return resp;
            }
            catch (Exception exc) {
                // Logging the exception to server log.
                LogSources.Hosting.LogException(exc);
                resp = Response.FromStatusCode(500);
                resp.Body = GetExceptionString(exc);
                resp.ContentType = "text/plain";
                return resp;
            }
        }

        // TODO:
        // Can be moved back to method above when implicit transaction no longer depends on exceptions.

        // Added a separate method that does not catch any exception to allow wrapping whole block
        // in an implicit transaction. The current solution for the implicit is to catch exception
        // and upgrade if necessary which does not work when we are catching all exceptions above.
        private Response _HandleRequest(Request request, HandlerOptions.HandlerLevels handlerLevel) {
            Response resp = null;

            if (!request.IsInternal)
                Session.InitialRequest = request;

            Profiler.Current.Start(ProfilerNames.Empty);
            Profiler.Current.Stop(ProfilerNames.Empty);

            // Running all available HTTP handlers.
            Profiler.Current.Start(ProfilerNames.GetUriHandlersManager);
            UriHandlersManager.GetUriHandlersManager(handlerLevel).RunDelegate(request, out resp);
            Profiler.Current.Stop(ProfilerNames.GetUriHandlersManager);

            // Checking if we still have no response.
            if (resp == null || resp.HandlingStatus == HandlerStatusInternal.NotHandled)
                return null;

            // Handling and returning the HTTP response.
            Profiler.Current.Start(ProfilerNames.HandleResponse);
            resp = OnResponseHttp(request, resp);
            Profiler.Current.Stop(ProfilerNames.HandleResponse);

            return resp;
        }

        /// <summary>
        /// This is where the AppServer calls to get a resource from the file system.
        /// </summary>
        /// <param name="relativeUri">The uri to resolve</param>
        /// <param name="request">The http request</param>
        /// <returns>The http response</returns>
        private Response ResolveAndPrepareFile(string relativeUri, Request request) {
            StaticWebServer staticWebServer;

            // Trying to fetch resource for this port.
            if (staticFileServers_.TryGetValue(request.PortNumber, out staticWebServer)) {
                return staticWebServer.GetStaticResponseClone(relativeUri, request);
            }

            var badReq = Response.FromStatusCode(400);
            badReq["Connection"] = "close";
            return badReq;
        }

        /// <summary>
        /// Gets the exception string.
        /// </summary>
        /// <param name="ex">The ex.</param>
        /// <returns>String.</returns>
        private String GetExceptionString(Exception ex) {
            string errorMsg = ExceptionFormatter.ExceptionToString(ex);
            errorMsg += "\r\nMore information about this error may be available in the server error log.";
            return errorMsg;
        }

        /// <summary>
        /// Sent from the Node when the user runs a module (an .EXE).
        /// </summary>
        /// <param name="path">The path.</param>
        /// <remarks>There is no need to add the directory to the static resolver as the static resolver
        /// will already be bootstrapped as a lower priority handler for stuff that this
        /// AppServer does not handle.</remarks>
        public void UserAddedLocalFileDirectoryWithStaticContent(UInt16 port, String path) {
            lock (staticFileServers_) {
                StaticWebServer staticWebServer;

                // Try to fetch static web server.
                if (staticFileServers_.TryGetValue(port, out staticWebServer)) {
                    staticWebServer.UserAddedLocalFileDirectoryWithStaticContent(port, path);
                }
                else {
                    staticWebServer = new StaticWebServer();
                    staticFileServers_.Add(port, staticWebServer);
                    staticWebServer.UserAddedLocalFileDirectoryWithStaticContent(port, path);

                    // Registering static handler on given port.
                    Handle.GET(port, "/{?}", (string res) => {
                        return HandlerStatus.ResolveStaticContent;
                    });
                }
            }
        }

        /// <summary>
        /// Get a list with all folders where static file resources such as .html files or images are kept.
        /// </summary>
        /// <returns>List with ports and folders</returns>
        public Dictionary<UInt16, IList<string>> GetWorkingDirectories() {

            Dictionary<UInt16, IList<string>> list = new Dictionary<ushort, IList<string>>();

            foreach (KeyValuePair<UInt16, StaticWebServer> entry in staticFileServers_) {

                List<string> portList = GetWorkingDirectories(entry.Key);
                if (portList != null) {
                    foreach (string folder in portList) {

                        if (list.ContainsKey(entry.Key)) {
                            list[entry.Key].Add(folder);
                        }
                        else {
                            IList<string> folders = new List<string> { folder };
                            list.Add(entry.Key, folders);
                        }
                    }
                }
            }
            return list;
        }


        /// <summary>
        /// Get a list with all folders where static file resources such as .html files or images are kept.
        /// </summary>
        /// <returns>List with folders or null</returns>
        public List<string> GetWorkingDirectories(ushort port) {

            StaticWebServer staticWebServer;

            // Try to fetch static web server.
            if (staticFileServers_.TryGetValue(port, out staticWebServer)) {
                return staticWebServer.GetWorkingDirectories(port);
            }

            return null;
        }


        /// <summary>
        /// Housekeeps this instance.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public int Housekeep() {
            lock (staticFileServers_) {
                // Doing house keeping for each port.
                foreach (KeyValuePair<UInt16, StaticWebServer> s in staticFileServers_)
                    s.Value.Housekeep();

                return 0;
            }
        }
    }

}
