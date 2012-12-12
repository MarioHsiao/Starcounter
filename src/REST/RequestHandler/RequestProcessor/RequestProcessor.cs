// ***********************************************************************
// <copyright file="RequestProcessor.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Text;
using HttpStructs;

namespace Starcounter.Internal.Uri
{
    /// <summary>
    /// A request processor represents a set of URI and verb handlers or a single uri and verb handler.
    /// It matches, parses and, if the caller wants, invokes a certain handler.
    /// </summary>
    /// <remarks>Example:
    /// The top most RequestProcessor might encapulate three SingleRequestProcessors:
    /// 
    /// "GET /test/{?}"
    /// "POST /hello/world"
    /// "DELETE /test/{?}"
    /// 
    /// The byte array containing the verb and URI would be passed to the Process function of the
    /// top level RequestProcessor and the matching would be done by calling child RequestProcessors.
    /// In this example, there would only be two levels (the top level and the leaf level).
    /// 
    /// However, in the example below, there would be three levels and there is no immediate way for
    /// the top level to distinguish between to almost ambigous URIs without parsing the values.
    /// 
    /// "GET /test/{?}"
    /// "POST /hello/cruel/world"
    /// "POST /hello/{?}/world"
    /// "DELETE /test/{?}"
    /// 
    /// In this case, the top level RequestProcessor would have three children:
    /// "GET /test/{param}"
    /// "POST /hello/...
    /// "DELETE /test/{?}"
    /// 
    /// And the child "POST /hello..." RequestProcessor would have two children:
    /// "POST /hello/cruel/world"
    /// "POST /hello/{?}/world"</remarks>
    public abstract class RequestProcessor {
        /// <summary>
        /// Tries to match and parse a verb and a URI. Can also envoke its handler.
        /// </summary>
        /// <param name="uri">A pointer to the beginning of the buffer containing the verb and URI</param>
        /// <param name="uriSize">Size of the uri</param>
        /// <param name="fragment">A pointer to the current position in the buffer containing the verb and the URI</param>
        /// <param name="fragmentSize">Size of the fragment.</param>
        /// <param name="invoke">If true, the handler (delegate) will be called with the parsed parameters</param>
        /// <param name="request">If the handler accepts a request as a parameter and invoke is true, this request will be passed as a parameter to the handler delegate</param>
        /// <param name="handler">The RequestProcessor matching the verb and URI provided in the fragment</param>
        /// <param name="resource">If invoke is true, this is the return value from the handler delegate</param>
        /// <returns>True if a match was made. If parsing of the parameter fails, false is returned. This allows the caller to try ambiguous handlers.</returns>
        public abstract bool Process(IntPtr uri, int uriSize, IntPtr fragment, int fragmentSize, bool invoke, HttpRequest request, out SingleRequestProcessorBase handler, out object resource);

        /// <summary>
        /// Invokes the specified request.
        /// </summary>
        /// <param name="request">The request to handle.</param>
        /// <param name="resource">Optional resource returned from handler.</param>
        /// <returns>true if the request was handled, false otherwise.</returns>
        public bool Invoke(HttpRequest request, out object resource) {
            SingleRequestProcessorBase rp;
            IntPtr pvu;
            uint vuSize;
            int vuSize2;

            request.GetRawMethodAndUriPlusAnExtraCharacter(out pvu, out vuSize);
            vuSize2 = (int)vuSize - 1;
            return Process(pvu, vuSize2, pvu, vuSize2, true, request, out rp, out resource);
        }

        /// <summary>
        /// Parses the URI int.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="size">The size.</param>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public bool ParseUriInt(IntPtr ptr, int size, out int value) {
            value = (int)Utf8Helper.IntFastParseFromAscii(ptr, (uint)size);
            return true;
        }

        /// <summary>
        /// Parses the URI string.
        /// </summary>
        /// <param name="ptr">The PTR.</param>
        /// <param name="size">The size.</param>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public bool ParseUriString(IntPtr ptr, int size, out string value) {
            byte[] buffer = new byte[size];
            unsafe {
                fixed (byte* pbuf = buffer) {
                    Intrinsics.MemCpy((void*)pbuf, (void*)ptr, (uint)size);
                    value = Encoding.UTF8.GetString(buffer, 0, size);
                }
            }
            return true;
        }
    }

}
