// ***********************************************************************
// <copyright file="ScriptInjector.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Text;
using HtmlAgilityPack;

namespace Starcounter.Internal.Web {

    /// <summary>
    /// Helper class for super-fast code injection into exiting http responses containing
    /// html content.
    /// </summary>
    /// <remarks>Starcounter caches static html files in RAM as complete http responses.
    /// To save request/response roundtrips
    /// (this is an important factor for browser speed), Starcounter can also use these
    /// static http responses to inject dynamic view models. This means that a view model
    /// does not have to be requested in a seperate XHR call as view models can be represented
    /// in text (as either JSON or Faster-than-Json). To do this, Starcounter needs to
    /// parse the http header and html content to find an appropriate injection point.
    /// Using this injection point, Starcounter can later inject the view model and update
    /// the http header accordingly (the Content-Length needs to be updated).</remarks>
    public class ScriptInjector {

        /// <summary>
        /// Given a http response with a known content offset, an offset where to
        /// insert a &lt;script&gt; element that will execute prior to any other &lt;script&gt;
        /// elements.
        /// </summary>
        /// <param name="response">The byte array containing the http response</param>
        /// <param name="contentOffset">The first byte of the UTF8 encoded html content</param>
        /// <returns>The offset where you can inject the new &lt;script&gt; tag</returns>
        /// <remarks>The current version expects UTF8 encoding. In order to inject code, you will also
        /// need metadata provided by the http response (such as the header size, whereabouts of
        /// the Content-Length). For this, see the Inject method.</remarks>
        public static int FindScriptInjectionPoint(byte[] response, int contentOffset) {
            HtmlDocument doc = new HtmlDocument();
            var html = Encoding.UTF8.GetString(response, contentOffset, response.Length - contentOffset);
            doc.LoadHtml(html);
            var script = doc.DocumentNode.SelectSingleNode("/script");
            HtmlNode headKid = null;

            var head = doc.DocumentNode.SelectSingleNode("/head");
            if (head == null)
                head = doc.DocumentNode.SelectSingleNode("/html/head");

            if (head != null) {
                var kids = head.ChildNodes;
                if (kids.Count > 0) {
                    headKid = kids[0];
                }
            }

            if (headKid != null) {
                if (script != null)
                    return Math.Min(script.StreamPosition, headKid.StreamPosition);
                return headKid.StreamPosition;
            }

            var doctype = doc.DocumentNode.SelectSingleNode("/comment()[starts-with(.,'<!DOCTYPE')]");
            if (doctype == null)
                doctype = doc.DocumentNode.SelectSingleNode("/comment()[starts-with(.,'<!doctype')]");

            if (doctype != null) {
                var first = doctype.NextSibling;
                if (first != null) {
                    if (script != null) {
                        return Math.Min(script.StreamPosition, first.StreamPosition);
                    }
                    return first.StreamPosition;
                }
            }

            return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        private static int FindHeaderInjectionPoint(byte[] response) {
            int injectionPoint = -1;
            int offset = 0;

            while (offset < (response.Length - 1)) {
                if (response[offset] == '\r' && response[++offset] == '\n') {
                    injectionPoint = offset + 1;
                    break;
                }
                offset++;
            }
            return injectionPoint;
        }

        /// <summary>
        /// Perform the actual code injection. This method operates on a complete HTTP response.
        /// This means that the header is also updated to reflect the new Content-Length of the
        /// response. To do this, this function requires information about the whereabout if
        /// the size text (HTTP Content-Length header) as well as other metadata about the
        /// header and content.
        /// </summary>
        /// <param name="original">The HTML source code</param>
        /// <param name="toInject">The source code to inject</param>
        /// <param name="headerLength">The previous offset of the content (same as the previous total length of the header)</param>
        /// <param name="contentLength">The previously used Content-Length value</param>
        /// <param name="contentLengthLength">The number of bytes previously used for the Content-Length value</param>
        /// <param name="contentLengthInjectionPoint">The offset of the Content-Length value</param>
        /// <param name="scriptInjectionPoint">The offset where the injected code should be inserted</param>
        /// <returns>The new, amended source code</returns>
        public static byte[] Inject(byte[] original, byte[] toInject, int headerLength, int contentLength, int contentLengthLength, int contentLengthInjectionPoint, int scriptInjectionPoint) {

            int newContentLength = contentLength + toInject.Length;
            byte[] newLength = Encoding.UTF8.GetBytes(newContentLength.ToString());

            int extraLengthLength = newLength.Length - contentLengthLength;

            byte[] response = new Byte[original.Length + toInject.Length + extraLengthLength];

            // Copy first part of header
            System.Buffer.BlockCopy(original, 0, response, 0, headerLength);

            int t = contentLengthInjectionPoint;
            // Copy new length
            System.Buffer.BlockCopy(newLength, 0, response, t, newLength.Length); // Copy Content-Length
            t += newLength.Length;
            response[t++] = (byte)'\r';
            response[t++] = (byte)'\n';
            response[t++] = (byte)'\r';
            response[t++] = (byte)'\n';
            // Copy the start of the original content
            System.Buffer.BlockCopy(original, headerLength, response, t, scriptInjectionPoint - headerLength);
            t = scriptInjectionPoint + extraLengthLength;
            // Copy the injected code
            System.Buffer.BlockCopy(toInject, 0, response, t, toInject.Length);
            // Copy the rest of the original content
            System.Buffer.BlockCopy(original, scriptInjectionPoint, response, t + toInject.Length, original.Length - scriptInjectionPoint);

            return response;
        }

        /// <summary>
        /// Injects the specified value into the header of the response.
        /// </summary>
        /// <param name="original">The original raw response</param>
        /// <param name="toInject">The header to inject</param>
        /// <param name="injectionPoint">The point in the header where to inject the new values.</param>
        /// <returns>The complete response with header injected.</returns>
        public static byte[] InjectInHeader(byte[] original, byte[] toInject, int injectionPoint) {
            byte[] afterInjection = new byte[original.Length + toInject.Length];

            if (injectionPoint == -1)
                injectionPoint = FindHeaderInjectionPoint(original);

            // Copy the first header row.
            Buffer.BlockCopy(original, 0, afterInjection, 0, injectionPoint);

            // Copy the header to be injected.
            Buffer.BlockCopy(toInject, 0, afterInjection, injectionPoint, toInject.Length);

            // Copy the rest of the header starting after the injection.
            Buffer.BlockCopy(original, injectionPoint, afterInjection, injectionPoint + toInject.Length, original.Length - injectionPoint);

            return afterInjection;
        }
    }
}
