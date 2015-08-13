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
using System.Collections.Concurrent;
using Starcounter.Templates;

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
        private ConcurrentDictionary<UInt16, StaticWebServer> fileServerPerPort_;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppRestServer" /> class.
        /// </summary>
        /// <param name="staticFileServer">The static file server.</param>
        public AppRestServer(ConcurrentDictionary<UInt16, StaticWebServer> staticFileServer) {
            fileServerPerPort_ = staticFileServer;
        }

        /// <summary>
        /// Gets the exception string.
        /// </summary>
        /// <param name="ex">The ex.</param>
        /// <returns>String.</returns>
        public static String GetExceptionString(Exception ex) {
            string errorMsg = ExceptionFormatter.ExceptionToString(ex);
            errorMsg += "\r\nMore information about this error may be available in the server error log.";
            return errorMsg;
        }

        /// <summary>
        /// Runs delegate and process response.
        /// </summary>
        public Response RunDelegateAndProcessResponse(
            IntPtr methodSpaceUriSpaceOnStack,
            IntPtr parametersInfoOnStack,
            Request req) {

            Response resp = null;

            HandlerOptions handlerOptions = req.HandlerOpts;

            Profiler.Current.Start(ProfilerNames.Empty);
            Profiler.Current.Stop(ProfilerNames.Empty);

            // Running all available HTTP handlers.
            Profiler.Current.Start(ProfilerNames.GetUriHandlersManager);

            UriHandlersManager uhm = UriHandlersManager.GetUriHandlersManager(handlerOptions.HandlerLevel);

            unsafe {

                UserHandlerInfo uhi = uhm.AllUserHandlerInfos[req.ManagedHandlerId];

                // Checking if we had custom type user Message argument.
                if (uhi.ArgMessageType != null) {
                    req.ArgMessageObjectType = uhi.ArgMessageType;
                    req.ArgMessageObjectCreate = uhi.ArgMessageCreate;
                }

                // Setting some request parameters.
                req.PortNumber = uhi.UriInfo.port_;
                req.MethodEnum = uhi.UriInfo.http_method_;

                // Saving original application name.
                String origAppName = StarcounterEnvironment.AppName;

                try {

                    // Calling user delegate.
                    resp = uhi.RunUserDelegate(
                        req,
                        methodSpaceUriSpaceOnStack,
                        parametersInfoOnStack);

                } finally {

                    // Setting back the application name.
                    StarcounterEnvironment.AppName = origAppName;
                }
            }

            Profiler.Current.Stop(ProfilerNames.GetUriHandlersManager);

            return resp;
        }

        /// <summary>
        /// This is where the AppServer calls to get a resource from the file system.
        /// </summary>
        /// <param name="relativeUri">The uri to resolve</param>
        /// <param name="request">The http request</param>
        /// <returns>The http response</returns>
        internal Response ResolveAndPrepareFile(string relativeUri, Request req) {

            StaticWebServer staticWebServer;

            // Trying to fetch resource for this port.
            if (fileServerPerPort_.TryGetValue(req.PortNumber, out staticWebServer)) {
                return staticWebServer.GetStaticResponseClone(relativeUri, req);
            }

            return new Response() {
                StatusCode = 404,
                StatusDescription = "Not Found",
                Body = String.Format("Error 404: Resource \"{0}\" not found. No static file resolver on port {1}.", relativeUri, req.PortNumber),
                HandlingStatus = HandlerStatusInternal.ResourceNotFound
            };
        }

        /// <summary>
        /// Sent from the Node when the user runs a module (an .EXE).
        /// </summary>
        /// <param name="path">The path.</param>
        /// <remarks>There is no need to add the directory to the static resolver as the static resolver
        /// will already be bootstrapped as a lower priority handler for stuff that this
        /// AppServer does not handle.</remarks>
        public void UserAddedLocalFileDirectoryWithStaticContent(String appName, UInt16 port, String path) {

            lock (fileServerPerPort_) {

                StaticWebServer staticWebServer;

                // Try to fetch static web server.
                if (fileServerPerPort_.TryGetValue(port, out staticWebServer)) {

                    staticWebServer.UserAddedLocalFileDirectoryWithStaticContent(appName, port, path);

                } else {

                    staticWebServer = new StaticWebServer();
                    fileServerPerPort_[port] = staticWebServer;
                    staticWebServer.UserAddedLocalFileDirectoryWithStaticContent(appName, port, path);

                    String savedAppName = StarcounterEnvironment.AppName;

                    StarcounterEnvironment.AppName = null;

                    // Registering static handler on given port.
                    Handle.GET(port, "/{?}", (String uri, Request req) => {

                        return Handle.ResolveStaticResource(req.Uri, req);

                    }, new HandlerOptions() {
                        ProxyDelegateTrigger = true,
                        SkipMiddlewareFilters = true
                    });

                    // Json templates used to return static files statistics.
                    var workingFolderTemplate = new TObject();
                    workingFolderTemplate.Add<TLong>("Port");
                    workingFolderTemplate.Add<TString>("Folder");

                    var workingFoldersTemplate = new TObject();
                    workingFoldersTemplate.Add<TArray<Json>>("Items", workingFolderTemplate);

                    // Handler to get all registered static resource folders
                    Handle.GET(port, "/staticcontentdir", (Request req) => {

                        Dictionary<UInt16, IList<string>> folders = AppsBootstrapper.GetFileServingDirectories();

                        dynamic workingFolders = new Json();
                        workingFolders.Template = workingFoldersTemplate;

                        foreach (KeyValuePair<UInt16, IList<string>> entry in folders) {

                            if (entry.Value != null && entry.Value.Count > 0) {
                                foreach (string folder in entry.Value) {
                                    var folderJson = workingFolders.Items.Add();
                                    folderJson.Port = entry.Key;
                                    folderJson.Folder = folder;
                                }
                            }
                        }

                        return new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.OK, BodyBytes = workingFolders.ToJsonUtf8() };

                    }, new HandlerOptions() {
                        ProxyDelegateTrigger = true,
                        SkipMiddlewareFilters = true
                    });

                    StarcounterEnvironment.AppName = savedAppName;
                }
            }
        }
        
        /// <summary>
        /// Get a list with all folders where static file resources such as .html files or images are kept.
        /// </summary>
        /// <returns>List with ports and folders</returns>
        public Dictionary<UInt16, IList<string>> GetWorkingDirectories() {

            Dictionary<UInt16, IList<string>> list = new Dictionary<ushort, IList<string>>();

            foreach (KeyValuePair<UInt16, StaticWebServer> entry in fileServerPerPort_) {

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
            if (fileServerPerPort_.TryGetValue(port, out staticWebServer)) {
                return staticWebServer.GetWorkingDirectories(port);
            }

            return null;
        }

        /// <summary>
        /// Housekeeps this instance.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public void Housekeep() {

            lock (fileServerPerPort_) {

                // Doing house keeping for each port.
                foreach (KeyValuePair<UInt16, StaticWebServer> s in fileServerPerPort_) {
                    s.Value.Housekeep();
                }
            }
        }
    }

}
