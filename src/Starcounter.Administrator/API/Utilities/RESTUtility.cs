using Starcounter.Advanced;
using Starcounter.Internal.Web;
using Starcounter.Server.Rest.Representations.JSON;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;


namespace Starcounter.Administrator.API.Utilities {
    /// <summary>
    /// Provides a set of REST-related utility methods.
    /// </summary>
    /// <remarks>
    /// Once established usable, these should probably move to some lower
    /// layer assembly, like Starcounter.Rest.
    /// </remarks>
    internal static class RESTUtility {
        /// <summary>
        /// Provides a set of REST-related utility methods working on
        /// JSON representations.
        /// </summary>
        internal static class JSON {
            /// <summary>
            /// Creates a stronly typed JSON instance <see cref="ErrorDetail"/> from
            /// a given Starcounter error code. The detail will be populated data that
            /// can be derived from the given error code, using the <see cref="ErrorCode"/>
            /// class.
            /// </summary>
            /// <param name="serverErrorCode">The Starcounter server-side error code.
            /// </param>
            /// <returns>A stronly typed JSON <see cref="ErrorDetail"/> whose values
            /// will be derived from the given error code.</returns>
            internal static ErrorDetail CreateError(uint serverErrorCode) {
                return CreateError(serverErrorCode, ErrorCode.ToMessage(serverErrorCode), ErrorCode.ToHelpLink(serverErrorCode));
            }

            /// <summary>
            /// Creates a stronly typed JSON instance <see cref="ErrorDetail"/> from
            /// a given Starcounter error code, an optional text and a help link.
            /// </summary>
            /// <param name="serverErrorCode">The Starcounter server-side error code.
            /// <param name="text">Optional text.</param>
            /// <param name="helplink">Optional help link.</param>
            /// <returns>A stronly typed JSON <see cref="ErrorDetail"/> whose values
            /// correspond to those supplied in the call.
            /// </returns>
            internal static ErrorDetail CreateError(uint serverErrorCode, string text = "", string helplink = "") {
                var detail = new ErrorDetail();
                detail.Text = text;
                detail.ServerCode = serverErrorCode;
                detail.Helplink = helplink;
                return detail;
            }

            /// <summary>
            /// Simplistic response creation utility for the API library, taking
            /// advantage about built-in constraints of the admin server REST API
            /// to create better responses.
            /// </summary>
            /// <param name="jsonContent">A JSON body.</param>
            /// <param name="status">The status code; 200/OK by default.</param>
            /// <param name="headers">Optional headers.</param>
            /// <returns>A response to be sent back to the client.</returns>
            public static Response CreateResponse(
                string jsonContent, int status = (int)HttpStatusCode.OK, Dictionary<string, string> headers = null) {
                return new Response() {
                    Uncompressed = HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent(status, headers, jsonContent)
                };
            }

            /// <summary>
            /// Creates a JSON representation from the body/content of a given
            /// request and returns it on successful. If the request is somehow
            /// considered invalid for the current (JSON) context, a response
            /// is created and the caller is expected to immediately return it.
            /// </summary>
            /// <remarks>
            /// The most obvious failure is a problem parsing the request body.
            /// But other conditions specific for JSON representations are, or
            /// will be, handled here too, like the failure to accept JSON as a
            /// media type.
            /// </remarks>
            /// <typeparam name="T">The strongly typed JSON to create and populate
            /// from the enclosed entity.</typeparam>
            /// <param name="request">The request whose body we'll use.</param>
            /// <param name="obj">The resulting strongly typed object.</param>
            /// <returns>A response to be returned if the request can not be
            /// accepted.</returns>
            public static Response CreateFromRequest<T>(Request request, out T obj) where T : Json, new() {
                T result = new T();
                try {
                    result.PopulateFromJson(request.GetBodyStringUtf8_Slow());
                } catch (FormatException fe) {
                    uint code;
                    if (!ErrorCode.TryGetCode(fe, out code)) {
                        throw;
                    }
                    if (code != Error.SCERRJSONVALUEWRONGTYPE && code != Error.SCERRJSONPROPERTYNOTFOUND) {
                        throw;
                    }

                    obj = null;
                    var errorDetail = CreateError(code, fe.Message, ErrorCode.ToHelpLink(code));
                    return CreateResponse(errorDetail.ToJson(), 400);
                }

                obj = result;
                return null;
            }

            public static Response ExpectAsynchronousOrNone(Request request, out bool asynchronous) {
                string expectations;
                asynchronous = RESTUtility.ExpectAsynchronous(request, out expectations);
                if (!asynchronous && !string.IsNullOrWhiteSpace(expectations)) {
                    return CreateResponse(
                        CreateError(Error.SCERRBADARGUMENTS, string.Format("Unable to meet expectation(s): {0}", expectations)).ToJson(),
                        (int)HttpStatusCode.ExpectationFailed
                        );
                }
                return null;
            }
        }

        /// <summary>
        /// Registers a handler that returns 405 (Method Not Allowed) for
        /// a given URI and the set of standard verbs/methods that it don't
        /// explicitly provide. The handler confirms to HTTP/1.1 in that it
        /// will return a response with the Allow header set, containing
        /// all methods supported.
        /// </summary>
        /// <param name="uri">The URI to register handler(s) for.</param>
        /// <param name="port">The port to use.</param>
        /// <param name="methodsSupported">The methods supported by the
        /// resource reprsented by the given URI.</param>
        /// <param name="allowExtensionsBeyondPatch">Tells the method to
        /// relax and don't check the set of supported methods against the
        /// set of known ones.</param>
        public static void Register405OnAllUnsupported(string uri, ushort port, string[] methodsSupported, bool allowExtensionsBeyondPatch = false) {
            var restHandler = Handle._REST;
            var verbs = new string[] { "OPTIONS", "GET", "HEAD", "POST", "PUT", "DELETE", "TRACE", "CONNECT", "PATCH" };

            if (methodsSupported == null) {
                throw new ArgumentNullException("methodsSupported");
            }
            if (methodsSupported.Length == 0) {
                // Force at least some method to be allowed.
                throw new ArgumentOutOfRangeException("methodsSupported");
            }

            var allows = string.Empty;
            foreach (var allowedMethod in methodsSupported) {
                if (!allowExtensionsBeyondPatch) {
                    if (!verbs.Contains(allowedMethod)) {
                        throw new ArgumentOutOfRangeException("methodsSupported", string.Format("HTTP method {0} not recognized", allowedMethod));
                    }
                }
                allows += " " + allowedMethod + ",";
            }
            allows = allows.TrimStart().TrimEnd(',');

            var headers = new Dictionary<string, string>();
            headers.Add("Allow", allows);

            // Seems to be something in the response builder that don't produce an
            // accurate response if we give it no content. Let's duplicate what we
            // allow in the body until fixed. Use JSON format.
            var body = string.Format("{{ \"Allow\": \"{0}\" }}", allows);
            var response = new Response {
                Uncompressed = HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent(405, headers, body)
            };

            var methodsToRegisterFor = new List<string>();
            foreach (var verb in verbs) {
                if (!methodsSupported.Contains(verb)) {
                    methodsToRegisterFor.Add(verb);
                }
            }

            switch (uri.CountOccurrences("{?}")) {
                case 0:
                    Register0(restHandler, uri, port, methodsToRegisterFor.ToArray(), response);
                    break;
                case 1:
                    Register1(restHandler, uri, port, methodsToRegisterFor.ToArray(), response);
                    break;
                case 2:
                    Register2(restHandler, uri, port, methodsToRegisterFor.ToArray(), response);
                    break;
                case 3:
                    Register3(restHandler, uri, port, methodsToRegisterFor.ToArray(), response);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("uri", "Too many parameters in URI");
            }
        }

        public static bool ExpectAsynchronous(Request request, out string expectations) {
            var expect = request["Expect"];
            if (string.IsNullOrWhiteSpace(expect)) {
                expectations = null;
                return false;
            }

            expectations = expect;
            return expect.Equals("202-accepted", System.StringComparison.InvariantCultureIgnoreCase);
        }

        static void Register0(IREST restHandler, string uri, ushort port, string[] methodsToRegisterFor, Response response) {
            Func<Response> return405 = () => {
                return response;
            };

            foreach (var verb in methodsToRegisterFor) {
                restHandler.RegisterHandler(port, verb + " " + uri, return405);
            }
        }

        static void Register1(IREST restHandler, string uri, ushort port, string[] methodsToRegisterFor, Response response) {
            Func<string, Response> return405 = (string dummy) => {
                return response;
            };

            foreach (var verb in methodsToRegisterFor) {
                restHandler.RegisterHandler<string>(port, verb + " " + uri, return405);
            }
        }

        static void Register2(IREST restHandler, string uri, ushort port, string[] methodsToRegisterFor, Response response) {
            Func<string, string, Response> return405 = (string dummy, string dummy2) => {
                return response;
            };

            foreach (var verb in methodsToRegisterFor) {
                restHandler.RegisterHandler<string, string>(port, verb + " " + uri, return405);
            }
        }

        static void Register3(IREST restHandler, string uri, ushort port, string[] methodsToRegisterFor, Response response) {
            Func<string, string, string, Response> return405 = (string dummy, string dummy2, string dummy3) => {
                return response;
            };

            foreach (var verb in methodsToRegisterFor) {
                restHandler.RegisterHandler<string, string, string>(port, verb + " " + uri, return405);
            }
        }
    }
}
