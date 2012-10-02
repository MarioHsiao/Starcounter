using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Starcounter.Internal.Web {
    public class HttpResponseBuilder {


       public static byte[] Created201 = new byte[] { (byte)'H', (byte)'T', (byte)'T', (byte)'P', (byte)'/', (byte)'1', (byte)'.', (byte)'0', (byte)' ',
                                       (byte)'2', (byte)'0', (byte)'1', (byte)' ', (byte)'C', (byte)'r', (byte)'e', (byte)'a', (byte)'t', (byte)'e', (byte)'d', 10, 13, 10, 13 };

       public static byte[] Ok200 = new byte[] { (byte)'H', (byte)'T', (byte)'T', (byte)'P', (byte)'/', (byte)'1', (byte)'.', (byte)'0', (byte)' ',
                                       (byte)'2', (byte)'0', (byte)'0', (byte)' ', (byte)'O', (byte)'K', 10, 13, 10, 13 };
       public static byte[] Ok200_Content = new byte[] { (byte)'H', (byte)'T', (byte)'T', (byte)'P', (byte)'/', (byte)'1', (byte)'.', (byte)'0', (byte)' ',
                                       (byte)'2', (byte)'0', (byte)'0', (byte)' ', (byte)'O', (byte)'K', 10, 13,
                                       (byte)'C', (byte)'o', (byte)'n', (byte)'t', (byte)'e', (byte)'n', (byte)'t', (byte)'-', (byte)'L', (byte)'e', (byte)'n', (byte)'g', (byte)'t', (byte)'h', (byte)':'};

       public static byte[] OkResponseHeader;
       public static uint OkResponseContentLengthInsertionPoint;

       static HttpResponseBuilder() {
          var str = "HTTP/1.1 200 OK\r\nContent-Length:9999999999";
          OkResponseHeader = Encoding.UTF8.GetBytes(str);
          OkResponseContentLengthInsertionPoint = 32;
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
             fixed (byte* pcontentlength = contentLength ) {
                var contentLengthLength = Utf8Helper.WriteUIntAsUtf8(pcontentlength, (uint)len);
                var totlen = headerLen + contentLengthLength + len;
                ret = new byte[totlen];
                fixed (byte* pret = ret, pok = OkResponseHeader, pcontent = data) {
                   Intrinsics.MemCpy((void*)(pret), pok, (uint)headerLen);
                   Intrinsics.MemCpy((void*)(pret+headerLen), pcontentlength, contentLengthLength);
                   headerLen += contentLengthLength;
                   Intrinsics.MemCpy((void*)(pret + headerLen), pcontent, (uint)len);
                }
             }
             return ret;
          }
       }


       public static byte[] CreateResponse(byte[] preamble, byte[] content, uint contentOffset, uint contentLength ) {
          unsafe {
             byte[] buffer = new byte[20];
             uint contentLengthLength;
             fixed (byte* pbuf = buffer) {
                contentLengthLength = Utf8Helper.WriteUIntAsUtf8(pbuf, contentLength);
                byte[] ret = new byte[preamble.Length + contentLengthLength + 4 + contentLength];
                fixed (byte* pre = preamble, pretx = ret, pcont = content) {
                   byte* pret = pretx;
                   Intrinsics.MemCpy((void*)(pret), pre, (uint)preamble.Length);
                   pret += preamble.Length;
                   Intrinsics.MemCpy((void*)(pret), pbuf, contentLengthLength);
                   pret += contentLengthLength;
                   *(pret++) = 10;
                   *(pret++) = 13;
                   *(pret++) = 10;
                   *(pret++) = 13;
                   Intrinsics.MemCpy((void*)(pret), pcont, contentLength);
                   pret += contentLength;
                }
                return ret;
             }
          }
       }




        public static byte[] NotFound404(string text) {

            long len = 0;
            bool isText = true;
            string contentType = "";
            byte[] payload = Encoding.UTF8.GetBytes(text);

            len = payload.Length;

            string str = "HTTP/1.1 404 Not Found\r\nServer:Starcounter\r\nConnection:close\r\n";

            isText = contentType.StartsWith("text/");

            str += "Content-Length:" + len.ToString() + "\r\n" +
                   "Content-Type:text/plain;charset=UTF-8" + "\r\n\r\n";

            byte[] header = Encoding.UTF8.GetBytes(str);
            var response = new byte[header.Length + len];
            header.CopyTo(response, 0);
            payload.CopyTo(response, header.Length);

            return response;
        }

        public static byte[] FromText(string text)
        {
            return FromText(text, SessionID.NullSession);
        }

        public static byte[] FromText(string text,SessionID sid) {

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



        public static byte[] JsonFromBytes(byte[] content, SessionID sid) {

            long len = 0;

            len = content.Length;

            // In HTTP 1.1, adding space(s) after the colon is not required. The omitting of the additional space character is intentional in Starcounter
            // HTTP responses.
            string str = "HTTP/1.1 200 OK\r\nServer:SC\r\nConnection:Keep-Alive\r\nContent-Type:application/json\r\nContent-Length:" + len.ToString() + "\r\n\r\n";

            byte[] header = Encoding.UTF8.GetBytes(str);
            var response = new byte[header.Length + len];
            header.CopyTo(response, 0);
            content.CopyTo(response, header.Length);

            return response;
        }

    }
}
 
