// ***********************************************************************
// <copyright file="HttpParser.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

/*
 * Parser a byte buffer to a structured Http request
 * 
 * In the real implementation, the https://github.com/joyent/http-parser unmanaged C parser should be used 
 * instead.
 */
/*
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System;
using HttpStructs;

namespace Starcounter.Internal.Web {
    public class RequestParser {
        const string pattern = @"^(?<method>[^\s]+)\s(?<path>[^\s]+)\sHTTP\/1\.1\r\n" + // request line
                               @"((?<field_name>[^:\r\n]+):\s(?<field_value>[^\r\n]+)\r\n)+" + //headers
                               @"\r\n" + //newline
                               @"(?<body>.+)?";

        private static readonly Regex _regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static Request Parse(byte[] bytes, int len) // JOCKE
        {
            var body = Encoding.UTF8.GetString(bytes, 0, len); // JOCKE
            Match match = _regex.Match(body);

            if (!match.Success) {
                Console.WriteLine("Illegal http request: " + body);
                return null;
            }

            var request = new Request(bytes);

            var fields = match.Groups["field_name"].Captures;
            var values = match.Groups["field_value"].Captures;
            for (var i = 0; i < fields.Count; i++) {
                var name = fields[i].ToString();
                var value = values[i].ToString();
                //                request.Headers[name] = value;
                if (name == "Accept-Encoding") {
                    if (value.StartsWith("gzip") ) {
                        request.IsGzipEnabled = true;
                    }
                }
                request[name] = value;
            }
            string cookieString;
            string sid = null;
            if (request.TryGetValue("Cookie", out cookieString)) {
                string[] cookies = cookieString.Split(';');
                foreach (var cookie in cookies) {
                    if (cookie.TrimStart(' ').StartsWith("sid")) {
                        sid = cookie;
                        break;
                    }
                }
                if (sid != null) {
                    request.SessionID = SessionID.RestoreSessionID(1);
                }
            }
            return request;
        }
    }
}
*/