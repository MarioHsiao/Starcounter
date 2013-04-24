using Starcounter.Advanced;
using Starcounter.Internal.Web;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;


namespace Starcounter.Administrator.API.Handlers {
    /// <summary>
    /// Provides a set of REST-related utility methods.
    /// </summary>
    /// <remarks>
    /// Once established usable, these should probably move to some lower
    /// layer assembly, like Starcounter.Rest.
    /// </remarks>
    internal static class RESTUtility {
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

            var headers = new NameValueCollection();
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
                    Register0(restHandler, uri, port, methodsToRegisterFor.ToArray(), headers, response);
                    break;
                case 1:
                    Register1(restHandler, uri, port, methodsToRegisterFor.ToArray(), headers, response);
                    break;
                case 2:
                    Register2(restHandler, uri, port, methodsToRegisterFor.ToArray(), headers, response);
                    break;
                case 3:
                    Register3(restHandler, uri, port, methodsToRegisterFor.ToArray(), headers, response);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("uri", "Too many parameters in URI");
            }
        }

        static void Register0(IREST restHandler, string uri, ushort port, string[] methodsToRegisterFor, NameValueCollection headers, Response response) {
            Func<Response> return405 = () => {
                return response;
            };

            foreach (var verb in methodsToRegisterFor) {
                restHandler.RegisterHandler(port, verb + " " + uri, return405);
            }
        }

        static void Register1(IREST restHandler, string uri, ushort port, string[] methodsToRegisterFor, NameValueCollection headers, Response response) {
            Func<string, Response> return405 = (string dummy) => {
                return response;
            };

            foreach (var verb in methodsToRegisterFor) {
                restHandler.RegisterHandler<string>(port, verb + " " + uri, return405);
            }
        }

        static void Register2(IREST restHandler, string uri, ushort port, string[] methodsToRegisterFor, NameValueCollection headers, Response response) {
            Func<string, string, Response> return405 = (string dummy, string dummy2) => {
                return response;
            };

            foreach (var verb in methodsToRegisterFor) {
                restHandler.RegisterHandler<string, string>(port, verb + " " + uri, return405);
            }
        }

        static void Register3(IREST restHandler, string uri, ushort port, string[] methodsToRegisterFor, NameValueCollection headers, Response response) {
            Func<string, string, string, Response> return405 = (string dummy, string dummy2, string dummy3) => {
                return response;
            };

            foreach (var verb in methodsToRegisterFor) {
                restHandler.RegisterHandler<string, string, string>(port, verb + " " + uri, return405);
            }
        }
    }
}
