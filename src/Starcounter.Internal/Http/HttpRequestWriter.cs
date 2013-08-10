// ***********************************************************************
// <copyright file="HttpRequestWriter.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Starcounter.Internal.Web {
    /// <summary>
    /// Class HttpRequestWriter
    /// </summary>
    public class HttpRequestWriter {
       /// <summary>
       /// The buffer
       /// </summary>
       public static byte[] buffer = new byte[1000000];
       /// <summary>
       /// The PUT
       /// </summary>
       public static byte[] PUT = new byte[] { (byte)'P', (byte)'U', (byte)'T' };
       /// <summary>
       /// The GET
       /// </summary>
       public static byte[] GET = new byte[] { (byte)'G', (byte)'E', (byte)'T' };
       /// <summary>
       /// The POST
       /// </summary>
       public static byte[] POST = new byte[] { (byte)'P', (byte)'O', (byte)'S', (byte)'T' };
       /// <summary>
       /// The DELETE
       /// </summary>
       public static byte[] DELETE = new byte[] { (byte)'D', (byte)'E', (byte)'L', (byte)'E', (byte)'T', (byte)'E' };

       /// <summary>
       /// The protocol and termination
       /// </summary>
       public static byte[] ProtocolAndTermination = new byte[] { (byte)' ', (byte)'H', (byte)'T', (byte)'T', (byte)'P', (byte)'/', (byte)'1', (byte)'.', (byte)'0', (byte)'\n', (byte)'\n' };

       /// <summary>
       /// The protocol and content length
       /// </summary>
       public static byte[] ProtocolAndContentLength = new byte[] { (byte)' ', (byte)'H', (byte)'T', (byte)'T', (byte)'P',(byte)'/',(byte)'1',(byte)'.',(byte)'0', (byte)'\n',
                                                        (byte)'L', (byte)':' };

       /// <summary>
       /// Writes a http request to a existing byte array.
       /// </summary>
       /// <param name="buffer">The byte array being written to</param>
       /// <param name="offset">Where to write in the byte array</param>
       /// <param name="verb">The method of the request (i.e. GET, POST, PUT etc.)</param>
       /// <param name="uri">The uri of the request (i.e. /players/123)</param>
       /// <param name="uriLength">Length of the URI.</param>
       /// <param name="content">Contains body of the request. Can be null if contentLength is zero.</param>
       /// <param name="contentOffset">The first byte of the content (body) inside the content byte array</param>
       /// <param name="contentLength">The size of the content supplied</param>
       /// <returns>The number of bytes written</returns>
       public static uint WriteRequest(
                        byte[] buffer,
                        uint offset,
                        byte[] verb,
                        byte[] uri,
                        int uriLength,
                        byte[] content,
                        uint contentOffset,
                        uint contentLength) {
          unsafe {
             byte[] x;
             if (contentLength == 0) {
                x = ProtocolAndTermination;
             }
             else {
                x = ProtocolAndContentLength;
             }
             fixed (byte* obuff = buffer, puri = uri, ppart = x ) {
                byte* obuf = obuff + offset;
                byte* buf = obuf;
                foreach (byte b in verb) {
                   *(buf++) = b;
                }
                *(buf++) = (byte)' ';
//                var uriLength = (uint)uri.Length;
                Intrinsics.MemCpy((void*)(buf), puri, (uint)uriLength);
                buf += uriLength;
                if (contentLength == 0) {
                   int partLength = ProtocolAndTermination.Length;
                   Intrinsics.MemCpy((void*)(buf), ppart, (uint)partLength);
                   buf += partLength;
                }
                else {
                   int partLength = ProtocolAndContentLength.Length;
                   Intrinsics.MemCpy((void*)(buf), ppart, (uint)partLength);
                   buf += partLength;
                   buf += Utf8Helper.WriteIntAsUtf8(buf, contentLength);
                   *(buf++) = (byte)'\n';
                   *(buf++) = (byte)'\n';
                   fixed (byte* pcontentx = content) {
                      var pcontent = pcontentx + contentOffset;
                      Intrinsics.MemCpy((void*)(buf), pcontent, (uint)contentLength);
                      buf += contentLength;
                   }
                }
                return (uint)(buf - obuf);
             }
          }
       }

    }
}
 
