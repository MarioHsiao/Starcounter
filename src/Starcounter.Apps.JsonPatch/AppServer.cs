// ***********************************************************************
// <copyright file="AppServer.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Text;
using HttpStructs;
using Starcounter.Apps;
using Starcounter.Internal.REST;
using Starcounter.Advanced;
using System.Net;
using Codeplex.Data;
using Starcounter.Internal.JsonPatch;

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
        private StaticWebServer StaticFileServer;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpAppServer" /> class.
        /// </summary>
        /// <param name="staticFileServer">The static file server.</param>
        public HttpAppServer(StaticWebServer staticFileServer) {
            StaticFileServer = staticFileServer;
        }

        /// <summary>
        /// Handles the response once the user delegate is called and has a result.
        /// </summary>
        /// <param name="request">Incomming HTTP request.</param>
        /// <param name="x">Result of calling user delegate.</param>
        /// <returns>HttpResponse instance.</returns>
        public HttpResponse HandleResponse(HttpRequest request, Object x) {
//            uint errorCode;
            HttpResponse response = null;
            string responseReasonPhrase;
            Session session = null;

            try {
                if (x != null) {
                    if (x is Obj) {
                        // A json object as a response could be the following:
                        // 1) A new object not attached to a Session, in which case we just serialize it and
                        //    send the response as normal json.
                        // 2) A new object attached to a Session, in which case we respond with a 201 Created and
                        //    set the location on how to access the object.
                        // 3) Updates to a session-bound object, in which case we respond with a batch of json-patches.

                        // We always start from the root object, even if the object returned from the handler is further down in the tree.
                        var root = GetJsonRoot((Obj)x);

                        session = Session.Current;
                        if (session == null || session.root != root) {
                            // A simple object with no serverstate. Return a 200 OK with the json as content.
                            response = new HttpResponse() {
                                Uncompressed = HttpResponseBuilder.FromJsonUTF8Content(root.ToJsonUtf8())
                            };
                        } else {
                            if (root.LogChanges) {
                                // An existing sessionbound object have been updated. Return a batch of jsonpatches.
                                response = new HttpResponse() {
                                    Uncompressed = HttpPatchBuilder.CreateHttpPatchResponse(ChangeLog.CurrentOnThread)
                                };
                            } else {
                                // A new sessionbound object. Return a 201 Created together with location and content.
                                request.Debug(" (new view model)");
                                root.LogChanges = true;
                                response = new HttpResponse() {
                                    Uncompressed = HttpResponseBuilder.Create201Response(root.ToJsonUtf8(), session.GetDataLocation())
                                };
                            }

                            // Need to check if this is a new session and if so generate a new session in the gateway.
                            //if (!request.HasSession) {
                            //    errorCode = request.GenerateNewSession(session);
                            //    if (errorCode != 0)
                            //        throw ErrorCode.ToException(errorCode);
                            //}
                        }
                    } else if (x is DynamicJson) {
                        var dynJson = (DynamicJson)x;
                        response = new HttpResponse() {
                            Uncompressed = HttpResponseBuilder.FromJsonUTF8Content(System.Text.Encoding.UTF8.GetBytes(dynJson.ToString()))
                        };
                    } else if (x is int || x is HttpStatusCode) {
                        int statusCode = (int)x;
                        if (!HttpStatusCodeAndReason.TryGetRecommendedHttp11ReasonPhrase(
                            statusCode, out responseReasonPhrase)) {
                            // The code was outside the bounds of pre-defined, known codes
                            // in the HTTP/1.1 specification, but still within the valid
                            // range of codes - i.e. it's a so called "extension code". We
                            // give back our default, "reason phrase not available" message.
                            responseReasonPhrase = HttpStatusCodeAndReason.ReasonNotAvailable;
                        }
                        response = new HttpResponse() {
                            Uncompressed = HttpResponseBuilder.FromCodeAndReason_NOT_VALIDATING(statusCode, responseReasonPhrase)
                        };
                    } else if (x is HttpStatusCodeAndReason) {
                        var codeAndReason = (HttpStatusCodeAndReason)x;
                        response = new HttpResponse() {
                            Uncompressed = HttpResponseBuilder.FromCodeAndReason_NOT_VALIDATING(
                            codeAndReason.StatusCode,
                            codeAndReason.ReasonPhrase)
                        };
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

                //if (request.HasNewSession) {
                //    // A new session have been created. We need to inject a session-cookie stub to the response.

                //    // TODO:
                //    // We should try to inject all things in one go (scriptinjection, headerinjection) to avoid 
                //    // unnecessary creation and copying of buffers.
                //    response.Uncompressed = ScriptInjector.InjectInHeader(
                //        response.GetBytes(request),
                //        ScSessionStruct.SessionIdCookiePlusEndlineStubBytes,
                //        response.HeaderInjectionPoint);
                //}

                return response;
            } catch (Exception ex) {
                byte[] error = Encoding.UTF8.GetBytes(this.GetExceptionString(ex));
                return new HttpResponse() { Uncompressed = HttpResponseBuilder.Create500WithContent(error) };
            }
        }

        private Obj GetJsonRoot(Obj json) {
            Container current = json;
            while (current.Parent != null) {
                current = current.Parent;
            }
            return (Obj)current;
        }

        /// <summary>
        /// The GET Method. Returns a representation of a resource.
        /// Works for http and web sockets.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>The bytes according to the appropriate protocol</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override HttpResponse Handle(HttpRequest request) {
            object x;

            try {
                // Checking if we are in session already.
                if (request.HasSession) {
                    Session.Start((Session)request.AppsSessionInterface);
                }
                // Invoking original user delegate with parameters here.
#if GW_URI_MATCHING_CODEGEN
                UserHandlerCodegen.HandlersManager.RunDelegate(request, out x);
#else
            RequestHandler.RequestProcessor.Invoke(request, out x);
#endif
                // Handling and returning the HTTP response.
                return HandleResponse(request, x);
            } catch (Exception ex) {
                byte[] error = Encoding.UTF8.GetBytes(this.GetExceptionString(ex));
                return new HttpResponse() { Uncompressed = HttpResponseBuilder.Create500WithContent(error) };
            } finally {
                Session.End();
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

                return ScriptInjector.Inject(original, script, ri.HeadersLength, ri.ContentLength, ri.ContentLengthLength, ri.ContentLengthInjectionPoint, ri.ScriptInjectionPoint);
            }

            return original;
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
