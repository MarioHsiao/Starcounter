// ***********************************************************************
// <copyright file="AppServer.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Text;
using Starcounter.Internal.REST;
using Starcounter.Internal.Application;
using HttpStructs;

namespace Starcounter.Internal.Web {

    /// <summary>
    /// Class HardcodedStuff
    /// </summary>
    public class HardcodedStuff
    {
        /// <summary>
        /// The HTTP request
        /// </summary>
        public HttpRequest HttpRequest;
        /// <summary>
        /// The sessions
        /// </summary>
        public SessionDictionary Sessions;

        /// <summary>
        /// The here
        /// </summary>
        [ThreadStatic]
        public static HardcodedStuff Here;

        /// <summary>
        /// Initializes a new instance of the <see cref="HardcodedStuff" /> class.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="sessions">The sessions.</param>
        private HardcodedStuff(HttpRequest request, SessionDictionary sessions)
        {
            HttpRequest = request;
            Sessions = sessions;
        }

        /// <summary>
        /// Begins the request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="sessions">The sessions.</param>
        public static void BeginRequest(HttpRequest request, SessionDictionary sessions)
        {
            Here = new HardcodedStuff(request, sessions);
        }

        /// <summary>
        /// Ends the request.
        /// </summary>
        public static void EndRequest()
        {
            Here = null;
        }
    }

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
        /// The sessions
        /// </summary>
        protected SessionDictionary Sessions;


        /// <summary>
        /// Initializes a new instance of the <see cref="HttpAppServer" /> class.
        /// </summary>
        /// <param name="staticFileServer">The static file server.</param>
        /// <param name="sessions">The sessions.</param>
        public HttpAppServer(StaticWebServer staticFileServer, SessionDictionary sessions ) {
            StaticFileServer = staticFileServer;
            Sessions = sessions;
        }

        /// <summary>
        /// The GET Method. Returns a representation of a resource.
        /// Works for http and web sockets.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>The bytes according to the appropriate protocol</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override HttpResponse Handle(  HttpRequest request ) {
            Session session;
            // TODO!
            //SessionID sid = request.SessionID;

            // TODO:
            // Is the sessionid sent in the header or as part of the uri 
            // for patch and __vm messages?
            // Sending it in the header seems like a better idea since we
            // are going to need some kind of temporary accessible object.
          
            //if (sid.IsNullSession)
            //{
            //    request.Debug(" (new session)");
            //    session = Sessions.CreateSession();
            //}
            //else
            //{
            //    session = Sessions.GetSession(sid);
            //}

            HardcodedStuff.BeginRequest(request, Sessions);

            try
            {
                Object x = RequestHandler.RequestProcessor.Invoke(request);
                if (x != null)
                {
                    if (x is App)
                    {
                        var app = (App)x;
                        //                       return new HttpResponse() { Uncompressed = HttpResponseBuilder.CreateMinimalOk200WithContent(data, 0, len) };

                        request.Debug(" (new view model)");

                        session = Sessions.CreateSession();
                        session.AttachRootApp(app);

                        // TODO:
                        // Just need it here to be able to get the sessionId when serializing app.
                        // Needs to be rewritten.
                        session.Execute(request, () =>
                        {
                            request.IsAppView = true;
                            request.ViewModel = app.ToJsonUtf8(false, true);
                            request.NeedsScriptInjection = true;
                            //                    request.CanUseStaticResponse = false; // We need to provide the view model, so we can use 
                            //                                                          // cached (and gziped) content, but not a complete cached
                            //                                                          // response.
                        });
                        
                        var view = (string)app.View;
                        if (view == null)
                        {
                            view = app.Template.ClassName + ".html";
                        }
                        view = "/" + view;
                        request.GzipAdvisable = false;
                        return new HttpResponse() { Uncompressed = ResolveAndPrepareFile(view, request) };
                    }
                    if (x is HttpResponse)
                    {
                        return x as HttpResponse;
                    }
                    else if (x is string)
                    {
                        return new HttpResponse() { Uncompressed = HttpResponseBuilder.FromText((string)x/*, sid*/) };
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                    //                else
                    //                {
                    //                    if (request.Uri.StartsWith("/__vm/"))
                    //                    {
                    //                        //                    session = Sessions.GetSession(request.Uri.Substring(8));
                    //                        if (sid.IsNullSession)
                    //                            response = new HttpResponse() { Uncompressed = HttpResponseBuilder.NotFound404("Invalid session") };
                    //                        else
                    //                        {
                    ////                            response = new HttpResponse() { Uncompressed = HttpResponseBuilder.JsonFromBytes(Encoding.UTF8.GetBytes(this.Sessions.GetRootApp(sid).ToJson()), sid) };
                    //                        }
                    //                        return;
                    //                    }
                    //                }
                }
                return new HttpResponse() { Uncompressed = ResolveAndPrepareFile(request.Uri, request) };
            }
            finally
            {
                HardcodedStuff.EndRequest();
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
