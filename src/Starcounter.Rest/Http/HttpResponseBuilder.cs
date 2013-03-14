// ***********************************************************************
// <copyright file="HttpResponseBuilder.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Internal.REST;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace Starcounter.Internal.Web
{
    /// <summary>
    /// Class HttpResponseBuilder
    /// </summary>
    public class HttpResponseBuilder
    {
    /// <summary>
    /// End of line for HTTP.
    /// </summary>
    public const string CRLF = "\r\n";

    /// <summary>
    /// Double end of line for HTTP.
    /// </summary>
    public const string CRLFCRLF = CRLF + CRLF;

    /// <summary>
    /// Expose a set of factory methods that are slowish prototypes of
    /// HTTP response creation functionality we should consider making
    /// an effort to support as fast.
    /// </summary>
    public static class Slow
    {

        /// <summary>
        /// Build a HTTP/1.1 response using a given status code, a set
        /// of headers and content given in the form of a <see cref="string"/>.
        /// </summary>
        /// <remarks>
        /// Clients should not specify the entity header Content-Type
        /// as part of the <paramref name="headers"/> set. This is set by
        /// the method, and based on the <paramref name="contentEncoding"/>
        /// and <paramref name="contentType"/> parameters respectively.
        /// Specifying a Content-Type as part of the headers collection
        /// yields in an undefined behaviour.
        /// </remarks>
        /// <param name="code">The status code.</param>
        /// <param name="headers">General-, response and/or entity headers.</param>
        /// <param name="content">The content to include in the response, forming
        /// it's body. If <see langword="null"/> is specified, the response will
        /// be created with no entity/content.</param>
        /// <param name="contentEncoding">Type of encoding to use when turning
        /// the given content to an array of bytes. Ignored if <paramref name="content"/>
        /// is <see langword="null"/>.</param>
        /// <param name="contentType">The content type to indicating the media
        /// type of the given content. If not given, this method assumes
        /// "application/json" as the default.</param>
        /// <returns>A byte array that is the HTTP response message before any
        /// transfer encoding is applied (i.e. it's uncompressed).</returns>
        public static byte[] FromStatusHeadersAndStringContent(
            int code,
            NameValueCollection headers, 
            string content,
            Encoding contentEncoding = null,
            string contentType = "application/json"
            )
        {
            string reason;
            string msgHeader;
            string header;
            bool hasContent;
            byte[] message;
            byte[] entityBody;
            int bodyLength;

            hasContent = !string.IsNullOrEmpty(content);
            bodyLength = 0;
            entityBody = null;
            if (!HttpStatusCodeAndReason.TryGetRecommendedHttp11ReasonPhrase(code, out reason)) {
                reason = HttpStatusCodeAndReason.ReasonNotAvailable;
            }
            msgHeader = "HTTP/1.1 " + HttpStatusCodeAndReason.ToStatusLineFormatNoValidate(code, reason);
            msgHeader += CRLF;

            if (headers != null) {
                foreach (var key in headers.AllKeys) {
                    var value = headers[key];
                    header = string.Concat(key, ":", value, CRLF);
                    msgHeader += header;
                }
            }
               
            if (hasContent) {
                contentEncoding = contentEncoding ?? Encoding.UTF8;
                entityBody = contentEncoding.GetBytes(content);
                bodyLength = entityBody.Length;
                header = string.Concat("Content-Type: ", contentType, ";", contentEncoding.WebName, CRLF);
                msgHeader += header;
                header = string.Concat("Content-Length: ", bodyLength, CRLF);
                msgHeader += header;   
            }

            msgHeader += CRLF;
            var msgHeaderBytes = Encoding.UTF8.GetBytes(msgHeader);

            message = new byte[msgHeaderBytes.Length + bodyLength];
            msgHeaderBytes.CopyTo(message, 0);
            if (hasContent) {
                entityBody.CopyTo(message, msgHeaderBytes.Length);
            }

            return message;
        }
    }

    /// <summary>
    /// The created201
    /// </summary>
    public static byte[] Created201 = new byte[] { (byte)'H', (byte)'T', (byte)'T', (byte)'P', (byte)'/', (byte)'1', (byte)'.', (byte)'0', (byte)' ',
                                    (byte)'2', (byte)'0', (byte)'1', (byte)' ', (byte)'C', (byte)'r', (byte)'e', (byte)'a', (byte)'t', (byte)'e', (byte)'d', (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n' };

    /// <summary>
    /// The ok200
    /// </summary>
    public static byte[] Ok200 = new byte[] { (byte)'H', (byte)'T', (byte)'T', (byte)'P', (byte)'/', (byte)'1', (byte)'.', (byte)'0', (byte)' ',
                                    (byte)'2', (byte)'0', (byte)'0', (byte)' ', (byte)'O', (byte)'K', (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n' };
    /// <summary>
    /// The ok200_ content
    /// </summary>
    public static byte[] Ok200_Content = new byte[] { (byte)'H', (byte)'T', (byte)'T', (byte)'P', (byte)'/', (byte)'1', (byte)'.', (byte)'0', (byte)' ',
                                    (byte)'2', (byte)'0', (byte)'0', (byte)' ', (byte)'O', (byte)'K', (byte)'\r', (byte)'\n',
                                    (byte)'C', (byte)'o',(byte)'n',(byte)'t',(byte)'e',(byte)'n',(byte)'t', (byte)'-',(byte)'L',(byte)'e',(byte)'n',(byte)'g',(byte)'t',(byte)'h',  (byte)':'};

    /// <summary>
    /// The IS e500_ content
    /// </summary>
    public static byte[] ISE500_Content;

    /// <summary>
    /// The ok response header
    /// </summary>
    public static byte[] OkResponseHeader;
    /// <summary>
    /// The header sealing
    /// </summary>
    public static byte[] HeaderSealing = Encoding.UTF8.GetBytes(CRLF + CRLF);
    /// <summary>
    /// The ok response content length insertion point
    /// </summary>
    public static uint OkResponseContentLengthInsertionPoint;

    /// <summary>
    /// Initializes static members of the <see cref="HttpResponseBuilder" /> class.
    /// </summary>
    static HttpResponseBuilder() {
        string str = "HTTP/1.0 200 OK" + CRLF + "Content-Length: 9999999999";
        OkResponseHeader = Encoding.ASCII.GetBytes(str);
        OkResponseContentLengthInsertionPoint = 32; // Content-Length

        str = "HTTP/1.1 500 Internal Server Error" + CRLF + "Content-Length: ";
        ISE500_Content = Encoding.ASCII.GetBytes(str);
    }

    /// <summary>
    /// Will encapsulate some binary content inside a minimal "200 OK" response encoded using Utf8.
    /// </summary>
    /// <param name="data">The buffer holding the data for the http body (i.e. content) of the response</param>
    /// <param name="offset">The first byte inside the data parameter to copy into response</param>
    /// <param name="len">The number of bytes to copy into the response</param>
    /// <returns>A complete http response</returns>
    public static byte[] CreateMinimalOk200WithContent(byte[] data, uint offset, uint len) {
        var headerLen = OkResponseContentLengthInsertionPoint;
        var contentLength = new byte[10];
        byte[] ret;
        unsafe {
        fixed (byte* pcontentlength = contentLength) {
            var contentLengthLength = Utf8Helper.WriteUIntAsUtf8(pcontentlength, (uint)len);
            var totlen = headerLen + contentLengthLength + HeaderSealing.Length + len;
            ret = new byte[totlen];
            fixed (byte* pret = ret, pok = OkResponseHeader, phseal = HeaderSealing, pcontent = data)
            {
                Intrinsics.MemCpy((void*)(pret), pok, (uint)headerLen);
                Intrinsics.MemCpy((void*)(pret + headerLen), pcontentlength, contentLengthLength);
                headerLen += contentLengthLength;
                Intrinsics.MemCpy((void*)(pret + headerLen), phseal, (uint)HeaderSealing.Length);
                Intrinsics.MemCpy((void*)(pret + headerLen + HeaderSealing.Length), pcontent, (uint)len);
            }
        }
        return ret;
        }
    }


    /// <summary>
    /// Creates the response.
    /// </summary>
    /// <param name="preamble">The preamble.</param>
    /// <param name="content">The content.</param>
    /// <param name="contentOffset">The content offset.</param>
    /// <param name="contentLength">Length of the content.</param>
    /// <returns>System.Byte[][].</returns>
    public static byte[] CreateResponse(byte[] preamble, byte[] content, uint contentOffset, uint contentLength) {
        unsafe {
        byte[] buffer = new byte[20];
        uint contentLengthLength;
        fixed (byte* pbuf = buffer) {
            contentLengthLength = Utf8Helper.WriteUIntAsUtf8(pbuf, contentLength);
            byte[] ret = new byte[preamble.Length + contentLengthLength + HeaderSealing.Length + contentLength];
            fixed (byte* pre = preamble, pretx = ret, pcont = content) {
                byte* pret = pretx;
                Intrinsics.MemCpy((void*)(pret), pre, (uint)preamble.Length);
                pret += preamble.Length;
                Intrinsics.MemCpy((void*)(pret), pbuf, contentLengthLength);
                pret += contentLengthLength;
                *(pret++) = (byte)'\r';
                *(pret++) = (byte)'\n';
                *(pret++) = (byte)'\r';
                *(pret++) = (byte)'\n';
                Intrinsics.MemCpy((void*)(pret), pcont, contentLength);
                pret += contentLength;
            }
            return ret;
        }
        }
    }

    /// <summary>
    /// Create500s the content of the with.
    /// </summary>
    /// <param name="content">The content.</param>
    /// <returns>Byte[][].</returns>
    public static Byte[] Create500WithContent(Byte[] content)
    {
        Byte[] response;
        Byte[] contentLengthBuffer;
        Int32 contentLengthLength;
        Int32 offset;
        contentLengthBuffer = new Byte[10];

        unsafe
        {
            fixed (byte* p = contentLengthBuffer)
            {
                contentLengthLength = (Int32)Utf8Helper.WriteUIntAsUtf8(p, (UInt32)content.Length);
            }
        }

        response = new Byte[ISE500_Content.Length + contentLengthLength + HeaderSealing.Length + content.Length];
        Array.Copy(ISE500_Content, response, ISE500_Content.Length);
        offset = ISE500_Content.Length;
        Array.Copy(contentLengthBuffer, 0, response, offset, contentLengthLength);
        offset += contentLengthLength;
        Array.Copy(HeaderSealing, 0, response, offset, HeaderSealing.Length);
        offset += HeaderSealing.Length;
        Array.Copy(content, 0, response, offset, content.Length);
        return response;
    }

    /// <summary>
    /// Nots the found404.
    /// </summary>
    /// <param name="payload">The payload.</param>
    /// <returns>System.Byte[][].</returns>
    public static byte[] NotFound404(String payload)
    {
        return NotFound404(System.Text.Encoding.UTF8.GetBytes(payload));
    }

    /// <summary>
    /// Nots the found404.
    /// </summary>
    /// <param name="payload">The payload.</param>
    /// <returns>System.Byte[][].</returns>
    public static byte[] NotFound404(Byte[] contentBytes)
    {
        string str = "HTTP/1.1 404 Not Found" + CRLF + "Server: SC" + CRLF + "Connection: close" + CRLF;
        str += "Content-Length: " + contentBytes.Length + CRLF +
            "Content-Type: text/plain;charset=UTF-8" + CRLFCRLF;

        byte[] headersBytes = Encoding.UTF8.GetBytes(str);
        var responseBytes = new byte[headersBytes.Length + contentBytes.Length];

        System.Buffer.BlockCopy(headersBytes, 0, responseBytes, 0, headersBytes.Length);
        System.Buffer.BlockCopy(contentBytes, 0, responseBytes, headersBytes.Length, contentBytes.Length);

        return responseBytes;
    }

    public static byte[] FromCodeAndReason_NOT_VALIDATING(int code, string reason) {
        string headers = "HTTP/1.1 " + HttpStatusCodeAndReason.ToStatusLineFormatNoValidate(code, reason) +
            CRLF + "Server: SC" + CRLF + "Content-Length: 0" + CRLFCRLF;
          
        return Encoding.UTF8.GetBytes(headers);
    }
       

    // TODO!
    //    public static byte[] FromText(string text) {
    //       return FromText(text, SessionID.NullSession);
    //    }


    /// <summary>
    /// Froms the text.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>System.Byte[][].</returns>
    public static byte[] FromText(string text) {
        byte[] contentBytes = Encoding.UTF8.GetBytes(text);
          
        String responseStr = "HTTP/1.1 200 OK" + CRLF + "Server: SC" + CRLF + "Connection: close" + CRLF +
        "Content-Length: " + contentBytes.Length + CRLF +
        "Content-Type: text/plain;charset=UTF-8" + CRLFCRLF;

        byte[] headersBytes = Encoding.ASCII.GetBytes(responseStr);
        var responseBytes = new byte[headersBytes.Length + contentBytes.Length];

        System.Buffer.BlockCopy(headersBytes, 0, responseBytes, 0, headersBytes.Length);
        System.Buffer.BlockCopy(contentBytes, 0, responseBytes, headersBytes.Length, contentBytes.Length);

        return responseBytes;
    }

    /// <summary>
    /// Creates a response with a serialized JSON payload, assuming the
    /// charset used is the default for the application/json media type
    /// (i.e. UTF8).
    /// </summary>
    /// <param name="content">The serialized JSON entity body.</param>
    /// <returns>An uncompressed representation of a response message,
    /// with headers properly specifying the metadata of the enclosed
    /// content.</returns>
    public static byte[] FromJsonUTF8Content(byte[] contentBytes) {
        String responseStr = "HTTP/1.1 200 OK" + CRLF + "Server: SC" + CRLF + "Connection: close" + CRLF +
        "Content-Length: " + contentBytes.Length + CRLF +
        "Content-Type: application/json;charset=UTF-8" + CRLFCRLF;

        var headersBytes = Encoding.ASCII.GetBytes(responseStr);
        var responseBytes = new byte[headersBytes.Length + contentBytes.Length];

        System.Buffer.BlockCopy(headersBytes, 0, responseBytes, 0, headersBytes.Length);
        System.Buffer.BlockCopy(contentBytes, 0, responseBytes, headersBytes.Length, contentBytes.Length);

        return responseBytes;
    }

    /// <summary>
    /// Creates a 201 Created response with a serialized JSON payload, assuming 
    /// the charset used is the default for the application/json media type (i.e. UTF8).
    /// A location header (as per specification) is also added.
    /// </summary>
    /// <param name="content">The serialized JSON entity body.</param>
    /// <param name="location"></param>
    /// <returns>
    /// An uncompressed representation of a response message, with headers properly 
    /// specifying the metadata of the enclosed content.
    /// </returns>
    public static byte[] FromJsonUTF8ContentWithLocation(byte[] contentBytes, string location)
    {
        var headers = "HTTP/1.1 201 Created" + CRLF + "Server: SC" + CRLF + "Connection: close" + CRLF;
        headers += "Content-Length: " + contentBytes.Length + CRLF
                 + "Content-Type: application/json;charset=UTF-8" + CRLF
                 + "Location: " + location + CRLFCRLF;

        var headersBytes = Encoding.ASCII.GetBytes(headers);
        var responseBytes = new byte[headersBytes.Length + contentBytes.Length];

        System.Buffer.BlockCopy(headersBytes, 0, responseBytes, 0, headersBytes.Length);
        System.Buffer.BlockCopy(contentBytes, 0, responseBytes, headersBytes.Length, contentBytes.Length);

        return responseBytes;
    }

    // TODO!
    /*
    public static byte[] JsonFromBytes(byte[] contentBytes, SessionID sid) {

        // In HTTP 1.1, adding space(s) after the colon is not required. The omitting of the additional space character is intentional in Starcounter
        // HTTP responses.
        string str = "HTTP/1.1 200 OK" + CRLF + "Server: SC" + CRLF + "Connection: Keep-Alive" + CRLF + "Content-Type: application/json" + CRLF + "Content-Length: " + contentBytes.Length + CRLFCRLF;

        byte[] headersBytes = Encoding.UTF8.GetBytes(str);
        var responseBytes = new byte[headersBytes.Length + contentBytes.Length];

        System.Buffer.BlockCopy(headersBytes, 0, responseBytes, 0, headersBytes.Length);
        System.Buffer.BlockCopy(contentBytes, 0, responseBytes, headersBytes.Length, contentBytes.Length);

        return responseBytes;
    }
    */
    }
}

