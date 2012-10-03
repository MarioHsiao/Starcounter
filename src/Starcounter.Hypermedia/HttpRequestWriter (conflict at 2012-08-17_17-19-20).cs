using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Starcounter.Internal.Web {
    public class HttpRequestWriter {

//       public static byte[] OkResponseHeader;
 //      public static uint OkResponseContentLengthInsertionPoint;

       static HttpRequestWriter() {
          var str = "HTTP/1.0\r\nContent-Length:9999999999";
   //       OkResponseHeader = Encoding.UTF8.GetBytes(str);
//          OkResponseContentLengthInsertionPoint = 32;
//       }

       public static uint WritePutRequestWithContent(
                        byte[] buffer,
                        uint offset,
                        byte[] uri,
                        byte[] content,
                        uint contentOffset,
                        uint contentLength) {
          unsafe {
             fixed (byte* obuf = buffer, puri = uri ) {
                byte* buf = obuf + offset;
                *(buf++) = (byte)'P';
                *(buf++) = (byte)'U';
                *(buf++) = (byte)'T';
                *(buf++) = (byte)' ';
                var uriLength = (uint)uri.Length;
                Intrinsics.MemCpy((void*)(buf), puri, uriLength);
                buf += uriLength;



                *(buf++) = (byte)10; // CR                
                *(buf++) = (byte)13; // LF
                *(buf++) = (byte)10; // CR                
                *(buf++) = (byte)13; // LF
                return (uint)(buf - obuf);
             }
          }
       }

    }
}
 
