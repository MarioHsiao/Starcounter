﻿// ***********************************************************************
// <copyright file="AppServer.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Text;
using HttpStructs;
using Starcounter.Internal.REST;
using Starcounter.Advanced;
using System.Net;
using Codeplex.Data;
using Starcounter.Internal.JsonPatch;
using System.Collections.Generic;

namespace Starcounter.Internal.Web {
    /// <summary>
    /// Wrapps the file based http web resource resolver and the App view model resolver.
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
        public Response OnResponse(Request request, Response response) {
            try {

                // Checking if we need to resolve static resource.
                if (response == null) {
                    response = new Response() { Uncompressed = ResolveAndPrepareFile(request.Uri, request) };
                    return response;
                }

                // NOTE: Checking if its internal request then just returning response without modification.
                if (request.IsInternal) {
                    return response;
                }

                if (response.Hypermedia is Json) {
                    Container r = (Container)response.Hypermedia;
                    while (r.Parent != null) {
                        r = r.Parent;
                    }
                    response.Hypermedia = (Json)r;
                }
                response.Request = request;
                response.ConstructFromFields();

                return response;
            }
            catch (Exception ex) {
                byte[] error = Encoding.UTF8.GetBytes(this.GetExceptionString(ex));
                return new Response() { Uncompressed = HttpResponseBuilder.Create500WithContent(error) };
            }
        }

//        private Obj GetJsonRoot(Obj json) {
//            Container current = json;
//            while (current.Parent != null) {
//                current = current.Parent;
//            }
//            return (Obj)current;
//        }

        /// <summary>
        /// Handles request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>The bytes according to the appropriate protocol</returns>
        public Response HandleRequest(Request request) {
            Response result = null;
            UInt32 errCode;
            Boolean cameWithSession = request.HasSession;

            switch (request.ProtocolType)
            {
                case MixedCodeConstants.NetworkProtocolType.PROTOCOL_HTTP1:
                {
                    try
                    {
                        // Checking if we are in session already.
                        if (!request.IsInternal) {

                            // Setting the original request.
                            Session.InitialRequest = request;

                            // Obtaining session.
                            Session s = (Session) request.GetAppsSessionInterface();

                            if (cameWithSession && (null != s)) {

                                // Starting session.
                                Session.Start(s);

                                // Checking if we can reuse the cache.
                                if (X.CheckLocalCache(request.Uri, request, null, null, out result)) {

                                    // Setting the session again.
                                    result.AppsSession = Session.Current.InternalSession;

                                    // Handling and returning the HTTP response.
                                    result = OnResponse(request, result);

                                    return result;
                                }
                            }
                        }

                        // Invoking original user delegate with parameters here.
                        UserHandlerCodegen.HandlersManager.RunDelegate(request, out result);

                        // In case of returned JSON object within current session we need to save it
                        // for later reuse.
                        Obj rootJsonObj = Session.Data;
                        Obj curJsonObj = null;
                        if (null != result) {

                            // Setting session on result only if its original request.
                            if ((null != Session.Current) && (!request.IsInternal) && (!cameWithSession))
                                result.AppsSession = Session.Current.InternalSession;

                            // Converting response to JSON.
                            curJsonObj = result;

                            if ((null != curJsonObj) &&
                                (null != rootJsonObj) &&
                                (request.IsIdempotent()) &&
                                (curJsonObj.HasThisRoot(rootJsonObj)))
                            {
                                Session.Current.AddJsonNodeToCache(request.Uri, curJsonObj);
                            }
                        }

                        // Handling and returning the HTTP response.
                        result = OnResponse(request, result);

                        return result;
                    }
                    catch (Exception exc)
                    {
                        // Checking if session is incorrect.
                        if (exc is HandlersManagement.IncorrectSessionException)
                            return new Response() { Uncompressed = HttpResponseBuilder.BadRequest400 };

                        // Logging the exception to server log.
                        LogSources.Hosting.LogException(exc);

                        byte[] error = Encoding.UTF8.GetBytes(this.GetExceptionString(exc));
                        return new Response() { Uncompressed = HttpResponseBuilder.Create500WithContent(error) };
                    }
                    finally
                    {
                        // Checking if a new session was created during handler call.
                        if ((null != Session.Current) && (!request.IsInternal))
                            Session.End();
                    }
                }

                case MixedCodeConstants.NetworkProtocolType.PROTOCOL_WEBSOCKETS:
                {
                    try
                    {
                        // Checking if we are in session already.
                        if (!cameWithSession)
                        {
                            // Creating session on Request as well.
                            errCode = request.GenerateNewSession(Session.CreateNewEmptySession());
                            if (errCode != 0)
                                throw ErrorCode.ToException(errCode);
                        }
                        else
                        {
                            // Start using specific session.
                            Session.Start((Session)request.GetAppsSessionInterface());
                        }

                        // Updating session information (sockets info, WebSockets, etc).
                        request.UpdateSessionDetails();

                        // Invoking original user delegate with parameters here.
                        UserHandlerCodegen.HandlersManager.RunDelegate(request, out result);

                        Response resp;

                        // Handling result.
                        if (result != null)
                        {
                            Byte[] byte_result = null;

//                            if (result is Byte[])
//                                byte_result = (Byte[])result;
//                            else if (result is String)
//                                byte_result = Encoding.UTF8.GetBytes((String)result);

                            byte_result = result.BodyBytes;

                            // Creating a standard Response from result.
                            resp = new Response() { Uncompressed = byte_result };
                        }
                        else
                        {
                            // Creating an error Response from result.
                            resp = new Response() { Uncompressed = new Byte[] {} };
                        }

                        return resp;
                    }
                    finally
                    {
                        Session.End();
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// This is where the AppServer calls to get a resource from the file system.
        /// If needed, script injection optimization is also performed.
        /// </summary>
        /// <param name="relativeUri">The uri to resolve</param>
        /// <param name="request">The http request</param>
        /// <returns>The http response</returns>
        /// <remarks>To save an additional http request, in the event of a html resource request,
        /// the Starcounter App view model is embedded in a script tag.</remarks>
        private byte[] ResolveAndPrepareFile(string relativeUri, Request request) {
            StaticWebServer staticWebServer;

            // Trying to fetch resource for this port.
            if (StaticFileServers.TryGetValue(request.PortNumber, out staticWebServer))
            {
                Response ri = staticWebServer.GetStatic(relativeUri, request);
                byte[] original = ri.GetBytes(request);
                if (request.NeedsScriptInjection)
                {
                    request.Debug(" (injecting script)");
                    byte[] script = Encoding.UTF8.GetBytes("<script>window.__elim_req=" + Encoding.UTF8.GetString(request.ViewModel) + "</script>");

                    return ScriptInjector.Inject(original, script, ri.HeadersLength, ri.ContentLength, ri.ContentLengthLength, ri.ContentLengthInjectionPoint, ri.ScriptInjectionPoint);
                }

                return original;
            }

            return HttpResponseBuilder.BadRequest400;
        }

        /// <summary>
        /// Gets the exception string.
        /// </summary>
        /// <param name="ex">The ex.</param>
        /// <returns>String.</returns>
        private String GetExceptionString(Exception ex) {
            Exception inner;
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(ex.Message);
            sb.AppendLine(ex.StackTrace);

            inner = ex.InnerException;
            while (inner != null) {
                sb.Append("-->");
                sb.AppendLine(inner.Message);
                sb.AppendLine(inner.StackTrace);
                inner = inner.InnerException;
            }
            return sb.ToString();
        }

        /// <summary>
        /// Sent from the Node when the user runs a module (an .EXE).
        /// </summary>
        /// <param name="path">The path.</param>
        /// <remarks>There is no need to add the directory to the static resolver as the static resolver
        /// will already be bootstrapped as a lower priority handler for stuff that this
        /// AppServer does not handle.</remarks>
        public void UserAddedLocalFileDirectoryWithStaticContent(UInt16 port, String path)
        {
            lock (StaticFileServers)
            {
                StaticWebServer staticWebServer;

                // Try to fetch static web server.
                if (StaticFileServers.TryGetValue(port, out staticWebServer))
                {
                    staticWebServer.UserAddedLocalFileDirectoryWithStaticContent(port, path);
                }
                else
                {
                    staticWebServer = new StaticWebServer();
                    StaticFileServers.Add(port, staticWebServer);
                    staticWebServer.UserAddedLocalFileDirectoryWithStaticContent(port, path);
                }
            }
        }

        /// <summary>
        /// Housekeeps this instance.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public int Housekeep()
        {
            lock (StaticFileServers)
            {
                // Doing house keeping for each port.
                foreach (KeyValuePair<UInt16, StaticWebServer> s in StaticFileServers)
                    s.Value.Housekeep();

                return 0;
            }
        }
    }

}
