
using Starcounter.Internal.REST;
using Starcounter.Internal.Web;
using System.Collections.Generic;
using System.Net;
using System.Text;
namespace Starcounter.Advanced {
    public partial class Response {

        /// <summary>
        /// Creates a response from an HTTP status code
        /// as defined in section
        /// <see href="http://www.w3.org/Protocols/rfc2616/rfc2616-sec6.html#sec6.1.1">
        /// 6.1.1</see> of the HTTP specification (RFC2616).
        /// </summary>
        /// <param name="statusCode">The status code (for instance 404)</param>
        /// <returns>The complete response</returns>
        public static Response FromStatusCode( int statusCode ) {
            Response response;
            string responseReasonPhrase;
            if (!HttpStatusCodeAndReason.TryGetRecommendedHttp11ReasonPhrase(
                statusCode, out responseReasonPhrase)) {
                // The code was outside the bounds of pre-defined, known codes
                // in the HTTP/1.1 specification, but still within the valid
                // range of codes - i.e. it's a so called "extension code". We
                // give back our default, "reason phrase not available" message.
                responseReasonPhrase = HttpStatusCodeAndReason.ReasonNotAvailable;
            }
            response = new Response() {
               Uncompressed = HttpResponseBuilder.FromCodeAndReason_NOT_VALIDATING(statusCode, responseReasonPhrase)
            };
            return response;
        }

        public static implicit operator Response(byte[] bytes) {
            return new Response() {
                BodyBytes = bytes
                // The response will get its content type from the request Accept header
            };
        }

        public static implicit operator Response(string text) {
            return new Response() {
                BodyBytes = Encoding.UTF8.GetBytes(text)
                // The response will get its content type from the request Accept header
            };
        }

        public static implicit operator Response(HttpStatusCode code) {
            return Response.FromStatusCode((int)code);
        }

        public static implicit operator Response(int code) {
            return Response.FromStatusCode(code);
        }

        public static implicit operator Response(HttpStatusCodeAndReason codeAndReason) {
            var response = new Response() {
                Uncompressed = HttpResponseBuilder.FromCodeAndReason_NOT_VALIDATING(
                codeAndReason.StatusCode,
                codeAndReason.ReasonPhrase)
            };
            return response;
        }


    }
}
