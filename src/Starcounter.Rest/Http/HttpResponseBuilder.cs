// ***********************************************************************
// <copyright file="HttpResponseBuilder.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Internal.REST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Starcounter.Internal.Web {

    /// <summary>
    /// Class HttpResponseBuilder
    /// </summary>
   public class HttpResponseBuilder {



       /// <summary>
       /// The created201
       /// </summary>
      public static byte[] Created201 = new byte[] { (byte)'H', (byte)'T', (byte)'T', (byte)'P', (byte)'/', (byte)'1', (byte)'.', (byte)'0', (byte)' ',
                                       (byte)'2', (byte)'0', (byte)'1', (byte)' ', (byte)'C', (byte)'r', (byte)'e', (byte)'a', (byte)'t', (byte)'e', (byte)'d', (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n' };

      /// <summary>
      /// The ok200
      /// </summary>
      public static byte[] Ok200 = new byte[] { (byte)'H', (byte)'T', (byte)'T', (byte)'P', (byte)'/', (byte)'1', (byte)'.', (byte)'0', (byte)' ',
                                       (byte)'2', (byte)'0', (byte)'0', (byte)' ', (byte)'O', (byte)'K', (byte)'\n', (byte)'\n' };
      /// <summary>
      /// The ok200_ content
      /// </summary>
      public static byte[] Ok200_Content = new byte[] { (byte)'H', (byte)'T', (byte)'T', (byte)'P', (byte)'/', (byte)'1', (byte)'.', (byte)'0', (byte)' ',
                                       (byte)'2', (byte)'0', (byte)'0', (byte)' ', (byte)'O', (byte)'K', (byte)'\n',
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
      public static byte[] HeaderSealing = Encoding.UTF8.GetBytes("\r\n\r\n");
      /// <summary>
      /// The ok response content length insertion point
      /// </summary>
      public static uint OkResponseContentLengthInsertionPoint;

      /// <summary>
      /// Initializes static members of the <see cref="HttpResponseBuilder" /> class.
      /// </summary>
      static HttpResponseBuilder() {
         var str = "HTTP/1.0 200 OK\r\nContent-Length:9999999999";
         OkResponseHeader = Encoding.UTF8.GetBytes(str);
         OkResponseContentLengthInsertionPoint = 32; // Content-Length

          str = "HTTP/1.1 500 Internal Server Error\nContent-Length: ";
          ISE500_Content = Encoding.UTF8.GetBytes(str);
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
                  *(pret++) = (byte)'\n';
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
      public static byte[] NotFound404(Byte[] payload) {

         long len = 0;
         bool isText = true;
         string contentType = "";

         len = payload.Length;

         string str = "HTTP/1.1 404 Not Found\nServer:Starcounter\nConnection:close\n";

         isText = contentType.StartsWith("text/");

         str += "L:" + len.ToString() + "\n" +
                "Content-Type:text/plain;charset=UTF-8" + "\n\n";

         byte[] header = Encoding.UTF8.GetBytes(str);
         var response = new byte[header.Length + len];
         header.CopyTo(response, 0);
         payload.CopyTo(response, header.Length);

         return response;
      }

      public static byte[] FromCodeAndReason_NOT_VALIDATING(int code, string reason) {
          const string statusAndHeaders =
              "HTTP/1.1 {0}\r\n" + "Server:Starcounter\r\n" + "Content-Length: 0\r\n" + "\r\n";
          
          string header = string.Format(statusAndHeaders, HttpStatusCodeAndReason.ToStatusLineFormatNoValidate(code, reason));
          return Encoding.UTF8.GetBytes(header);
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

         long len = 0;
         bool isText = true;
         string contentType = "";
         byte[] payload = Encoding.UTF8.GetBytes(text);

         len = payload.Length;

         string str = "HTTP/1.1 200 OK\r\nServer:SC\r\nConnection:close\r\n";

         isText = contentType.StartsWith("text/");

         str += "Content-Length:" + len.ToString() + "\r\n" +
                "Content-Type:text/plain;charset=UTF-8" + "\r\n\r\n";

         byte[] header = Encoding.UTF8.GetBytes(str);
         var response = new byte[header.Length + len];
         header.CopyTo(response, 0);
         payload.CopyTo(response, header.Length);

         return response;
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
      public static byte[] FromJsonUTF8Content(byte[] content) {
          var header = "HTTP/1.1 200 OK\r\nServer:SC\r\nConnection:close\r\n";
          header += "Content-Length:" + content.Length.ToString() + "\r\n" +
                "Content-Type:application/json;charset=UTF-8" + "\r\n\r\n";

          var utf8Header = Encoding.UTF8.GetBytes(header);
          var response = new byte[utf8Header.Length + content.Length];
          utf8Header.CopyTo(response, 0);
          content.CopyTo(response, utf8Header.Length);

          return response;
      }

      // TODO!
      /*
      public static byte[] JsonFromBytes(byte[] content, SessionID sid) {

         long len = 0;

         len = content.Length;

         // In HTTP 1.1, adding space(s) after the colon is not required. The omitting of the additional space character is intentional in Starcounter
         // HTTP responses.
         string str = "HTTP/1.1 200 OK\nServer:SC\nConnection:Keep-Alive\nContent-Type:application/json\nL:" + len.ToString() + "\n\n";

         byte[] header = Encoding.UTF8.GetBytes(str);
         var response = new byte[header.Length + len];
         header.CopyTo(response, 0);
         content.CopyTo(response, header.Length);

         return response;
      }
     */

   }
}

