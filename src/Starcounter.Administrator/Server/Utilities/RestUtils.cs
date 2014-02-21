using System;
using Codeplex.Data;
using Starcounter.Advanced;

namespace Starcounter.Administrator.Server.Utilities {
    /// <summary>
    /// General utils
    /// </summary>
    internal class RestUtils {

        /// <summary>
        /// Create an Error Json response from an exception
        /// </summary>
        /// <param name="e"></param>
        /// <returns>Response</returns>
        public static Response CreateErrorResponse(Exception e) {

            ErrorResponse errorResponse = new ErrorResponse();
            errorResponse.message = e.Message;
            errorResponse.stackTrace = e.StackTrace;
            errorResponse.helpLink = e.HelpLink;

            return new Response() { BodyBytes = errorResponse.ToJsonUtf8(), StatusCode = (ushort)System.Net.HttpStatusCode.InternalServerError };
        }


    }
}
