using System;
using Codeplex.Data;
using Starcounter.Advanced;

namespace Starcounter.Applications.UsageTrackerApp {
    /// <summary>
    /// General utils
    /// </summary>
    internal class Utils {

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

        /// <summary>
        /// Parse out the protcol version number from the header
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Protocol number</returns>
        public static int GetRequestProtocolVersion(Request request) {

            // Accept: application/starcounter.tracker.usage-v2+json\r\n

            string headers = request.GetHeadersStringUtf8_Slow();

            try {

                if (!string.IsNullOrEmpty(headers)) {

                    string[] lines = headers.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                    for (int i = 0; i < lines.Length; i++) {

                        string line = lines[i];

                        // Check if the line is field values
                        if (line.StartsWith(" ") || line.StartsWith("\t")) continue;

                        if (line.StartsWith("accept:", StringComparison.CurrentCultureIgnoreCase)) {
                            // Found our accept header.
                            string match = "application/starcounter.tracker.usage-v";

                            int vStartIndex = line.IndexOf(match, 7, StringComparison.CurrentCultureIgnoreCase);
                            if (vStartIndex == -1) continue;
                            vStartIndex += match.Length;
                            int vStopIndex = line.IndexOf('+', vStartIndex);
                            if (vStopIndex == -1) continue;

                            string str = line.Substring(vStartIndex, vStopIndex - vStartIndex);
                            int num;

                            if (int.TryParse(str, out num) == false) continue;

                            // TODO: For the moment we will ignore values that spans over multiple lines
                            // In the future i think this code will be replaced by logic in the Request
                            return num;
                        }

                    }

                }
            }
            catch (Exception) {
            }

            return 1;
        }

    }
}
