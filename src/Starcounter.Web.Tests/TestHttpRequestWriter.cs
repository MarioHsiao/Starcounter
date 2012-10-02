using System;
using System.Diagnostics;
using System.Text;
using NUnit.Framework;
using Starcounter.Internal.Web;

namespace Starcounter.Internal.Tests {

   public class TestHttpWriters {

      static byte[] uri;

      static TestHttpWriters() {
         var str = "/test/123";
         uri = Encoding.UTF8.GetBytes(str);
      }

      [Test]
      public static void BenchmarkCreatePutUri() {
         int repeats = 1;
         var buffer = new byte[100000];
         var sw = new Stopwatch();
         sw.Start();
         uint len = 0;
         for (int i = 0; i < repeats; i++) {
            len = HttpRequestWriter.WriteRequest(buffer, 0, HttpRequestWriter.PUT, uri, uri.Length, HttpRequestWriter.PUT, 0, 3);
         }
         sw.Stop();
         Console.WriteLine(Encoding.UTF8.GetString(buffer, 0, (int)len));
         Console.WriteLine(String.Format("Ran {0} times in {1} ms", repeats, sw.ElapsedMilliseconds));
      }

      [Test]
      public static void BenchmarkCreateGetUri() {
         int repeats = 10;
         var buffer = new byte[10000000];
         var sw = new Stopwatch();
         sw.Start();
         uint len = 0;
         uint offset = 0;
         for (int i = 0; i < repeats; i++) {
            len = HttpRequestWriter.WriteRequest(buffer, offset, HttpRequestWriter.GET, uri, uri.Length, null, 0, 0);
            offset += len;
         }
         sw.Stop();
         Console.WriteLine(Encoding.UTF8.GetString(buffer, 0, (int)len));
         Console.WriteLine(String.Format("Ran {0} times in {1} ms", repeats, sw.ElapsedMilliseconds));
      }


      [Test]
      public static void BenchmarkOk200_with_content() {
         int repeats = 1;
         var sw = new Stopwatch();
         sw.Start();
         byte[] ret = null;
         byte[] content = new byte[] { (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o' };
         for (int i = 0; i < repeats; i++) {
            ret = HttpResponseBuilder.CreateResponse(HttpResponseBuilder.Ok200_Content, content, 0, (uint)content.Length);
         }
         sw.Stop();
         Console.WriteLine(Encoding.UTF8.GetString(ret, 0, ret.Length));
         Console.WriteLine(String.Format("Ran {0} times in {1} ms", repeats, sw.ElapsedMilliseconds));
      }

   }
}