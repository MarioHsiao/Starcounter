
using Codeplex.Data;
using System;
using System.Net;

namespace Starcounter.Bootstrap.Management
{
    /// <summary>
    /// Implements the code host functionality behind the code host "Host"
    /// management resource.
    /// </summary>
    internal static class CodeHostHandler {
        static ManagementService managementService;

        /// <summary>
        /// Provides a set of utility methods for working with JSON representations
        /// in the code host management context.
        /// </summary>
        public static class JSON {
            /// <summary>
            /// Creates a response based on JSON content, passed as a string.
            /// </summary>
            /// <param name="jsonContent">A JSON body.</param>
            /// <param name="status">The status code; 200/OK by default.</param>
            /// <param name="headers">Optional headers.</param>
            /// <returns>A response to be sent back to the client.</returns>
            public static Response CreateResponse(
                string jsonContent, int status = (int)HttpStatusCode.OK /*, Dictionary<string, string> headers = null*/) {
					var response = Response.FromStatusCode(status);
					response.ContentType = MimeTypeHelper.MimeTypeAsString(MimeType.Application_Json);
					response.Body = jsonContent;
					return response;
				//return new Response() {
				//	Uncompressed = HttpResponseBuilder.Slow.FromStatusHeadersAndStringContent(status, headers, jsonContent)
				//};
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
                    result.PopulateFromJson(request.Body);
                } catch (FormatException fe) {
                    uint code;
                    if (!ErrorCode.TryGetCode(fe, out code)) {
                        throw;
                    }
                    if (code != Error.SCERRJSONVALUEWRONGTYPE && code != Error.SCERRJSONPROPERTYNOTFOUND) {
                        throw;
                    }

                    obj = null;
                    var detail = CreateError(code, fe.Message, ErrorCode.ToHelpLink(code));
                    return CreateResponse(detail.ToString(), 400);
                }

                obj = result;
                return null;
            }

            /// <summary>
            /// Creates a dynamic error detail based on the given parameters.
            /// </summary>
            /// <param name="serverErrorCode">The Starcounter server-side error code.
            /// <param name="text">Optional text.</param>
            /// <param name="helplink">Optional help link.</param>
            /// <returns>A dynamic JSON object that can be transformed to JSON string
            /// and passed in HTTP messages.
            /// </returns>
            internal static DynamicJson CreateError(uint serverErrorCode, string text = "", string helplink = "") {
                dynamic detail = new DynamicJson();
                detail.Text = text;
                detail.ServerCode = serverErrorCode;
                detail.Helplink = helplink;
                return detail;
            }
        }

        /// <summary>
        /// Performs setup of the <see cref="CodeHostHandler"/>.
        /// </summary>
        internal static void Setup(ManagementService manager) {
            managementService = manager;

            var uri = CodeHostAPI.Uris.Host;
            var port = manager.Port;

            Handle.GET(port, uri, CodeHostHandler.OnGET);
            Handle.DELETE(port, uri, CodeHostHandler.OnDELETE);
        }

        static Response OnGET() {
            if (managementService.Unavailable) {
                return 503;
            }
            return 204;
        }

        static Response OnDELETE() {
            if (managementService.Unavailable) {
                return 503;
            }
            managementService.Shutdown();
            return 204;
        }
    }
}