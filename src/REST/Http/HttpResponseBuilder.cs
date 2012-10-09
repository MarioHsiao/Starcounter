using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Starcounter.Internal.Web {

   /// <summary>
   /// 
   /// </summary>
   public class HttpResponseBuilder {



      public static byte[] Created201 = new byte[] { (byte)'H', (byte)'T', (byte)'T', (byte)'P', (byte)'/', (byte)'1', (byte)'.', (byte)'0', (byte)' ',
                                       (byte)'2', (byte)'0', (byte)'1', (byte)' ', (byte)'C', (byte)'r', (byte)'e', (byte)'a', (byte)'t', (byte)'e', (byte)'d', (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n' };

      public static byte[] Ok200 = new byte[] { (byte)'H', (byte)'T', (byte)'T', (byte)'P', (byte)'/', (byte)'1', (byte)'.', (byte)'0', (byte)' ',
                                       (byte)'2', (byte)'0', (byte)'0', (byte)' ', (byte)'O', (byte)'K', (byte)'\n', (byte)'\n' };
      public static byte[] Ok200_Content = new byte[] { (byte)'H', (byte)'T', (byte)'T', (byte)'P', (byte)'/', (byte)'1', (byte)'.', (byte)'0', (byte)' ',
                                       (byte)'2', (byte)'0', (byte)'0', (byte)' ', (byte)'O', (byte)'K', (byte)'\n',
                                       (byte)'C', (byte)'o',(byte)'n',(byte)'t',(byte)'e',(byte)'n',(byte)'t', (byte)'-',(byte)'L',(byte)'e',(byte)'n',(byte)'g',(byte)'t',(byte)'h',  (byte)':'};

      public static byte[] ISE500_Content;

      public static byte[] OkResponseHeader;
      public static byte[] HeaderSealing = Encoding.UTF8.GetBytes("\r\n\r\n");
      public static uint OkResponseContentLengthInsertionPoint;

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

        public static byte[] NotFound404(String payload)
        {
            return NotFound404(System.Text.Encoding.UTF8.GetBytes(payload));
        }

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

        

       // TODO!
  //    public static byte[] FromText(string text) {
  //       return FromText(text, SessionID.NullSession);
  //    }

     
      public static byte[] FromText(string text) {

         long len = 0;
         bool isText = true;
         string contentType = "";
         byte[] payload = Encoding.UTF8.GetBytes(text);

         len = payload.Length;

         string str = "HTTP/1.1 200 OK\r\nServer:SC\nConnection:close\r\n";

         isText = contentType.StartsWith("text/");

         str += "Content-Length:" + len.ToString() + "\r\n" +
                "Content-Type:text/plain;charset=UTF-8" + "\r\n\r\n";

         byte[] header = Encoding.UTF8.GetBytes(str);
         var response = new byte[header.Length + len];
         header.CopyTo(response, 0);
         payload.CopyTo(response, header.Length);

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

