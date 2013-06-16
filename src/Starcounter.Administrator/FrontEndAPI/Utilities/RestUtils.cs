using System;
using Codeplex.Data;
using Starcounter.Advanced;

namespace Starcounter.Administrator.FrontEndAPI {
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

            dynamic response = new DynamicJson();

            // Create error response
            response.exception = new { };
            response.exception.message = e.Message;
            response.exception.stackTrace = e.StackTrace;
            response.exception.helpLink = e.HelpLink;

            return new Response() { Body = response.ToString(), StatusCode = (ushort)System.Net.HttpStatusCode.InternalServerError };
        }


    }
}
