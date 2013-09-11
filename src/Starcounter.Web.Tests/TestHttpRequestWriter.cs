// ***********************************************************************
// <copyright file="TestHttpRequestWriter.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Diagnostics;
using System.Text;
using NUnit.Framework;
using Starcounter.Advanced;
using Starcounter.Internal.Web;

namespace Starcounter.Internal.Tests {

    /// <summary>
    /// Class TestHttpWriters
    /// </summary>
   public class TestHttpWriters {

       /// <summary>
       /// The URI
       /// </summary>
      static byte[] uri;

      /// <summary>
      /// Initializes static members of the <see cref="TestHttpWriters" /> class.
      /// </summary>
      static TestHttpWriters() {
         var str = "/test/123";
         uri = Encoding.UTF8.GetBytes(str);
      }

      /// <summary>
      /// Benchmarks the create put URI.
      /// </summary>
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

      /// <summary>
      /// Benchmarks the create get URI.
      /// </summary>
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


      /// <summary>
      /// Benchmarks the ok200_with_content.
      /// </summary>
      [Test]
      public static void BenchmarkOk200_with_content() {
         int repeats = 1;
         var sw = new Stopwatch();
         sw.Start();
         byte[] ret = null;
         byte[] content = new byte[] { (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o' };
         for (int i = 0; i < repeats; i++) {
			 (new Response() { BodyBytes = content, ContentLength = content.Length }).ConstructFromFields();
         }
         sw.Stop();
         Console.WriteLine(Encoding.UTF8.GetString(ret, 0, ret.Length));
         Console.WriteLine(String.Format("Ran {0} times in {1} ms", repeats, sw.ElapsedMilliseconds));
      }

   }
}