// ***********************************************************************
// <copyright file="AppServer.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Diagnostics;
using System.Text;
using HttpStructs;
using Starcounter.Apps;
using Starcounter.Internal.REST;
using Starcounter.Advanced;

namespace Starcounter.Internal.Web {
    /// <summary>
    /// Wrapps the file based http web resource resolver and the App view model resolver.
    /// </summary>
    /// <remarks>Supports Http as well as proprietary protocols.
    /// If the URI does not point to a App view model or a user implemented
    /// handler, the request is routed to a standard file based static resource
    /// web serving implementation that will serve html, png, jpg etc. using the file system.
    /// This file based resolver will be injected into the constructor of this class.</remarks>
    public partial class HttpAppServer : HttpRestServer {

        /// <summary>
        /// A standard file based static resource web serving implementation.
        /// Will serve html, png, jpg etc. using the file system.
        /// If the URI does not point to a App view model or a user implemented
        /// handler, this is where the request will go.
        /// </summary>
        public StaticWebServer StaticFileServer;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpAppServer" /> class.
        /// </summary>
        /// <param name="staticFileServer">The static file server.</param>
        public HttpAppServer(StaticWebServer staticFileServer) {
            StaticFileServer = staticFileServer;
        }

        /// <summary>
        /// The GET Method. Returns a representation of a resource.
        /// Works for http and web sockets.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>The bytes according to the appropriate protocol</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override HttpResponse Handle(HttpRequest request) {
            HttpResponse response = null;
            Session session = null;
            uint errorCode;

            try {
                if (request.HasSession) {
                    session = (Session)request.AppsSessionInterface;
                    session.Start(request);
                }

                object x;

                // Invoking original user delegate with parameters here.
#if GW_URI_MATCHING_CODEGEN
                UserHandlerCodegen.UHC.HandlersManager.RunDelegate(request, out x);
#else
                RequestHandler.RequestProcessor.Invoke(request, out x);
#endif
                if (x != null) {
                    if (x is Puppet) {
                        var app = (Puppet)x;
                       
                        // TODO:
                        // How do we create new sessions and what is allowed here...
                        // Should the users themselves create the session?
                        if (session == null) {
                            session = new Session();
                            errorCode = request.GenerateNewSession(session);
                            if (errorCode != 0)
                                throw new Exception("TODO: proper starcounter exception: " + errorCode);
                            session.Start(request);
                        }
    
                        request.Debug(" (new view model)");
                        session.AttachRootApp(app);
                        request.IsAppView = true;
                        request.ViewModel = app.ToJsonUtf8();
                        request.NeedsScriptInjection = true;
//                          request.CanUseStaticResponse = false; // We need to provide the view model, so we can use 
//                                                          // cached (and gziped) content, but not a complete cached
//                                                          // response.

                        var view = (string)app.View;
                        if (view == null) {
                            view = app.Template.ClassName + ".html";
                        }
                        view = "/" + view;
                        request.GzipAdvisable = false;
                        response = new HttpResponse() { Uncompressed = ResolveAndPrepareFile(view, request) };
                        app.IsSentExternally = true;
                    } else if (x is HttpResponse) {
                        response = x as HttpResponse;
                    } else if (x is string) {
                        response = new HttpResponse() { Uncompressed = HttpResponseBuilder.FromText((string)x/*, sid*/) };
                    } else {
                        throw new NotImplementedException();
                    }
                }
                if (response == null)
                    response = new HttpResponse() { Uncompressed = ResolveAndPrepareFile(request.Uri, request) };

                if (request.HasNewSession) {
                    // A new session have been created. We need to inject a session-cookie stub to the response.

                    // TODO:
                    // We should try to inject all things in one go (scriptinjection, headerinjection) to avoid 
                    // unnecessary creation and copying of buffers.
                    response.Uncompressed = ScriptInjector.InjectInHeader(
                        response.GetBytes(request),
                        ScSessionStruct.SessionIdCookiePlusEndlineStubBytes,
                        response.HeaderInjectionPoint);
                }

                return response;
            } catch (Exception ex) {
                byte[] error = Encoding.UTF8.GetBytes(this.GetExceptionString(ex));
                return new HttpResponse() { Uncompressed = HttpResponseBuilder.Create500WithContent(error) };
            } finally {
                if (Session.Current != null) {
                    Session.Current.End();
                }
            }
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
        private byte[] ResolveAndPrepareFile(string relativeUri, HttpRequest request) {
            HttpResponse ri = StaticFileServer.GetStatic(relativeUri, request);
            byte[] original = ri.GetBytes(request);
            if (request.NeedsScriptInjection) {
                request.Debug(" (injecting script)");
                byte[] script = Encoding.UTF8.GetBytes("<script>window.__elim_req=" + Encoding.UTF8.GetString(request.ViewModel) + "</script>");

                return ScriptInjector.Inject(original, script, ri.HeaderLength, ri.ContentLength, ri.ContentLengthLength, ri.ContentLengthInjectionPoint, ri.ScriptInjectionPoint);
            }

            return original;
        }

        /// <summary>
        /// Gets the exception string.
        /// </summary>
        /// <param name="ex">The ex.</param>
        /// <returns>String.</returns>
		private String GetExceptionString(Exception ex)
		{
			Exception inner;
			StringBuilder sb = new StringBuilder();

			sb.AppendLine(ex.Message);
			sb.AppendLine(ex.StackTrace);

			inner = ex.InnerException;
			while (inner != null)
			{
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
        public override void UserAddedLocalFileDirectoryWithStaticContent(string path) {
            StaticFileServer.UserAddedLocalFileDirectoryWithStaticContent(path);
        }

        /// <summary>
        /// Housekeeps this instance.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public override int Housekeep() {
           return StaticFileServer.Housekeep();
        }
    }

}
