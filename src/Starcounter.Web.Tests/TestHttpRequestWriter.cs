// ***********************************************************************
// <copyright file="TestHttpRequestWriter.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using NUnit.Framework;
using Starcounter.Advanced;
using Starcounter.Internal.Web;
using Starcounter.Templates;

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

		///// <summary>
		///// Benchmarks the create put URI.
		///// </summary>
		//[Test]
		//public static void BenchmarkCreatePutUri() {
		//	int repeats = 1;
		//	var buffer = new byte[100000];
		//	var sw = new Stopwatch();
		//	sw.Start();
		//	uint len = 0;
		//	for (int i = 0; i < repeats; i++) {
		//		len = HttpRequestWriter.WriteRequest(buffer, 0, HttpRequestWriter.PUT, uri, uri.Length, HttpRequestWriter.PUT, 0, 3);
		//	}
		//	sw.Stop();
		//	Console.WriteLine(Encoding.UTF8.GetString(buffer, 0, (int)len));
		//	Console.WriteLine(String.Format("Ran {0} times in {1} ms", repeats, sw.ElapsedMilliseconds));
		//}

		///// <summary>
		///// Benchmarks the create get URI.
		///// </summary>
		//[Test]
		//public static void BenchmarkCreateGetUri() {
		//	int repeats = 10;
		//	var buffer = new byte[10000000];
		//	var sw = new Stopwatch();
		//	sw.Start();
		//	uint len = 0;
		//	uint offset = 0;
		//	for (int i = 0; i < repeats; i++) {
		//		len = HttpRequestWriter.WriteRequest(buffer, offset, HttpRequestWriter.GET, uri, uri.Length, null, 0, 0);
		//		offset += len;
		//	}
		//	sw.Stop();
		//	Console.WriteLine(Encoding.UTF8.GetString(buffer, 0, (int)len));
		//	Console.WriteLine(String.Format("Ran {0} times in {1} ms", repeats, sw.ElapsedMilliseconds));
		//}


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

		[Test]
		public static void TestResponseFastConstructFromFields() {
			Response slowResponse = Response.FromStatusCode(200);
			Response fastResponse = Response.FromStatusCode(200);

			slowResponse.ConstructFromFields_Slow();
			fastResponse.ConstructFromFields();

			Assert.AreEqual(slowResponse.Uncompressed, fastResponse.Uncompressed);

			slowResponse = Response.FromStatusCode((int)HttpStatusCode.InternalServerError);
			slowResponse.Body = "Some exception message describing the error.";
			fastResponse = Response.FromStatusCode((int)HttpStatusCode.InternalServerError);
			fastResponse.Body = "Some exception message describing the error.";

			Assert.AreEqual(slowResponse.Uncompressed, fastResponse.Uncompressed);
		}

		[Test]
		public static void BenchmarkResponseConstructFromFields() {
			int repeats = 1000000;
			Response response;

			Console.WriteLine("200 OK, repeats: " + repeats);
			response = Response.FromStatusCode(200);
			RunResponseBenchmark(response, repeats);

			Console.WriteLine("200 OK with json content, repeats: " + repeats);
			string json = File.ReadAllText("simple.json");
			response = Response.FromStatusCode(200);
			response.Body = json;
			response.ContentType = "application/json";
			RunResponseBenchmark(response, repeats);

			Console.WriteLine("200 with more fields set, repeats: " + repeats);
			response = Response.FromStatusCode(200);
			response.Body = json;
			response.ContentType = "application/json";
			response.ContentEncoding = "utf8";
			response.Headers = "somespecialheader: myvalue\r\n";
			response.StatusDescription = " My special status";
			RunResponseBenchmark(response, repeats);
		}

		private static void RunResponseBenchmark(Response response, int repeats) {
			DateTime start;
			DateTime stop;

			start = DateTime.Now;
			for (int i = 0; i < repeats; i++) {
				response.ConstructFromFields();
			}
			stop = DateTime.Now;

			Console.Write((stop - start).TotalMilliseconds + "    ");

			start = DateTime.Now;
			for (int i = 0; i < repeats; i++) {
				response.ConstructFromFields_Slow();
			}
			stop = DateTime.Now;

			Console.WriteLine((stop - start).TotalMilliseconds);
		}
	}
}