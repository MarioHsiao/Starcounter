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
using Starcounter.Internal.JsonPatch;
using System.Collections.Generic;
using Starcounter.Rest;
using Starcounter.Logging;

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
        private Dictionary<UInt16, StaticWebServer> StaticFileServers;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppRestServer" /> class.
        /// </summary>
        /// <param name="staticFileServer">The static file server.</param>
        public AppRestServer(Dictionary<UInt16, StaticWebServer> staticFileServer) {
            StaticFileServers = staticFileServer;
        }

        /// <summary>
        /// Handles the response returned from the user handler.
        /// </summary>
        /// <param name="request">Incomming HTTP request.</param>
        /// <param name="response">Result of calling user handler (i.e. the delegate).</param>
        /// <returns>The same object as provide in the response parameter</returns>
        public Response OnResponseHttp(Request request, Response response) {
            try {
                // Checking if we need to resolve static resource.
                if (response.HandlingStatus == HandlerStatusInternal.NotHandled) {
                    response = ResolveAndPrepareFile(request.Uri, request);
                    response.HandlingStatus = HandlerStatusInternal.Done;
                }
                else {
                    // NOTE: Checking if its internal request then just returning response without modification.
                    if (request.IsInternal)
                        return response;

                    // Checking if JSON object is attached.
                    if (response.Resource is Json) {
                        Json r = (Json)response.Resource;

                        while (r.Parent != null)
                            r = r.Parent;

                        response.Resource = (Json)r;
                    }
                }

                response.Request = request;
                response.ConstructFromFields();

                return response;
            }
            catch (Exception ex) {
                // Logging the exception to server log.
                LogSources.Hosting.LogException(ex);
                var errResp = Response.FromStatusCode(500);
                errResp.Body = GetExceptionString(ex);
                errResp.ContentType = "text/plain";
                errResp.ConstructFromFields();
                return errResp;
            }
        }

        /// <summary>
        /// Handles request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>The bytes according to the appropriate protocol</returns>
        public Response HandleRequest(Request request) {
            Response response = null;
            Boolean cameWithSession = request.CameWithCorrectSession;

            try {
                // Checking if we are in session already.
                if (!request.IsInternal) {

                    // Setting the original request.
                    Session.InitialRequest = request;

                    // Obtaining session.
                    Session s = (Session)request.GetAppsSessionInterface();

                    if (cameWithSession && (null != s)) {

                        // Starting session.
                        Session.Start(s);

                        // Checking if we can reuse the cache.
                        if (request.IsInternal && X.CheckLocalCache(request.Uri, null, null, out response)) {

                            // Setting the session again.
                            response.AppsSession = Session.Current.InternalSession;

                            // Handling and returning the HTTP response.
                            response = OnResponseHttp(request, response);

                            return response;
                        }
                    }
                }

                // Invoking original user delegate with parameters here.
                UserHandlerCodegen.HandlersManager.RunDelegate(request, out response);

                // In case of returned JSON object within current session we need to save it
                // for later reuse.

                Json rootJsonObj = null;
                if (null != Session.Current)
                    rootJsonObj = Session.Current.Data;

                Json curJsonObj = null;
                if (null != response) {

                    // Checking if response is processed later.
                    if (response.HandlingStatus == HandlerStatusInternal.Handled)
                        return response;

                    // Setting session on result only if its original request.
                    if ((null != Session.Current) && (!request.IsInternal) && (!cameWithSession))
                        response.AppsSession = Session.Current.InternalSession;

                    // Converting response to JSON.
                    curJsonObj = response;

                    if ((null != curJsonObj) &&
                        (null != rootJsonObj) &&
                        (request.IsCachable()) &&
                        (curJsonObj.HasThisRoot(rootJsonObj))) {
                        Session.Current.AddJsonNodeToCache(request.Uri, curJsonObj);
                    }
                }
                else {
                    // Null equals 404.
                    response = Response.FromStatusCode(404);
                    response["Connection"] = "close";
                    response.ConstructFromFields();
                    return response;
                }

                // Handling and returning the HTTP response.
                response = OnResponseHttp(request, response);

                return response;
            }
            catch (ResponseException exc) {
                // NOTE: if internal request then throw the exception up.
                if (request.IsInternal)
                    throw exc;

                response = exc.ResponseObject;
                response.ConnFlags = Response.ConnectionFlags.DisconnectAfterSend;
                response.ConstructFromFields();
                return response;
            }
            catch (HandlersManagement.IncorrectSessionException) {
                response = Response.FromStatusCode(400);
                response["Connection"] = "close";
                response.ConstructFromFields();
                return response;
            }
            catch (Exception exc) {
                // Logging the exception to server log.
                LogSources.Hosting.LogException(exc);
                response = Response.FromStatusCode(500);
                response.Body = GetExceptionString(exc);
                response.ContentType = "text/plain";
                response.ConstructFromFields();
                return response;
            }
            finally {
                // Checking if a new session was created during handler call.
                if ((null != Session.Current) && (!request.IsInternal))
                    Session.End();
            }
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
            if (StaticFileServers.TryGetValue(request.PortNumber, out staticWebServer)) {
                return staticWebServer.GetStatic(relativeUri, request);
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
            lock (StaticFileServers) {
                StaticWebServer staticWebServer;

                // Try to fetch static web server.
                if (StaticFileServers.TryGetValue(port, out staticWebServer)) {
                    staticWebServer.UserAddedLocalFileDirectoryWithStaticContent(port, path);
                }
                else {
                    staticWebServer = new StaticWebServer();
                    StaticFileServers.Add(port, staticWebServer);
                    staticWebServer.UserAddedLocalFileDirectoryWithStaticContent(port, path);

                    // Registering static handler on given port.
                    Handle.GET(port, "/{?}", (string res) => {
                        return HandlerStatus.NotHandled;
                    });
                }
            }
        }

        /// <summary>
        /// Get a list with all folders where static file resources such as .html files or images are kept.
        /// </summary>
        /// <returns>List with folders</returns>
        public Dictionary<UInt16, string> GetWorkingDirectories() {

            Dictionary<UInt16, string> list = new Dictionary<ushort, string>();

            foreach (KeyValuePair<UInt16, StaticWebServer> entry in StaticFileServers) {

                List<string> portList = GetWorkingDirectories(entry.Key);
                if (portList != null) {
                    foreach (string folder in portList) {
                        list.Add(entry.Key, folder);
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
            if (StaticFileServers.TryGetValue(port, out staticWebServer)) {
                return staticWebServer.GetWorkingDirectories(port);
            }
            return null;
        }


        /// <summary>
        /// Housekeeps this instance.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public int Housekeep() {
            lock (StaticFileServers) {
                // Doing house keeping for each port.
                foreach (KeyValuePair<UInt16, StaticWebServer> s in StaticFileServers)
                    s.Value.Housekeep();

                return 0;
            }
        }
    }

}
