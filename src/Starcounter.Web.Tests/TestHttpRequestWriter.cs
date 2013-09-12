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

		/// <summary>
		/// Benchmarks the ok200_with_content.
		/// </summary>
		[Test]
		public static void BenchmarkOk200_with_content() {
			int repeats = 1;
			Response response;
			var sw = new Stopwatch();
			sw.Start();
			byte[] ret = null;
			int retSize = -1;
			byte[] content = new byte[] { (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o' };
			for (int i = 0; i < repeats; i++) {
				response = new Response() { BodyBytes = content, ContentLength = content.Length };
				response.ConstructFromFields();
				ret = response.Uncompressed;
				retSize = response.UncompressedLength;
			}
			sw.Stop();
			Console.WriteLine(Encoding.UTF8.GetString(ret, 0, retSize));
			Console.WriteLine(String.Format("Ran {0} times in {1} ms", repeats, sw.ElapsedMilliseconds));
		}

		[Test]
		public static void TestResponseConstructFromFields() {
			string json = File.ReadAllText("simple.json");

			Response response = Response.FromStatusCode(200);
			AssertConstructedResponsesAreEqual(response);

			response = Response.FromStatusCode((int)HttpStatusCode.InternalServerError);
			response.Body = "Some exception message describing the error.";
			AssertConstructedResponsesAreEqual(response);

			response = Response.FromStatusCode(200);
			response.Body = json;
			response.ContentType = "application/json";
			response.ContentEncoding = "utf8";
			response.Headers = "somespecialheader: myvalue\r\n";
			response.StatusDescription = " My special status";
			AssertConstructedResponsesAreEqual(response);

			response = Response.FromStatusCode(404);
			response.BodyBytes = Encoding.UTF8.GetBytes(json);
			response.ContentType = "application/json";
			response.ContentEncoding = "utf8";
			response.Headers = "somespecialheader: myvalue\r\n";
			response.StatusDescription = " My special status";
			AssertConstructedResponsesAreEqual(response);
		}

		[Test]
		public static void TestRequestConstructFromFields() {
			string json = File.ReadAllText("simple.json");

			Request request = new Request();
			request.Uri = "/test";
			request.HostName = "127.0.0.1:8080";
			AssertConstructedRequestsAreEqual(request);

			request = new Request();
			request.Method = "PUT";
			request.Uri = "/MyJson";
			request.HostName = "192.168.8.1";
			request.ContentType = "application/json";
			request.Body = json;
			AssertConstructedRequestsAreEqual(request);

			request = new Request();
			request.Method = "PUT";
			request.Uri = "/MyJson";
			request.HostName = "192.168.8.1";
			request.ContentType = "application/json";
			request.ContentEncoding = "utf8";
			request.Cookie = "dfsafeHYWERGSfswefw";
			request.Headers = "somespecialheader: myvalue\r\n";
			request.BodyBytes = Encoding.UTF8.GetBytes(json);
			AssertConstructedRequestsAreEqual(request);
		}

		private static void AssertConstructedResponsesAreEqual(Response response) {
			byte[] arr1;
			byte[] arr2;
			int arr1Size;
			int arr2Size;

			response.ConstructFromFields_Slow();
			arr1 = response.Uncompressed;
			arr1Size = response.UncompressedLength;

			response.SetCustomFieldsFlag();
			response.ConstructFromFields();
			arr2 = response.Uncompressed;
			arr2Size = response.UncompressedLength;

			Assert.AreEqual(arr1Size, arr2Size);

			for (int i = 0; i < arr1Size; i++) {
				Assert.AreEqual(arr1[i], arr2[i], "Arrays differ at position " + i);
			}
		}

		private static void AssertConstructedRequestsAreEqual(Request request) {
			byte[] arr1;
			byte[] arr2;

			request.ConstructFromFields_Slow();
			arr1 = request.CustomBytes;
			request.SetCustomFieldsFlag();
			request.ConstructFromFields();
			arr2 = request.CustomBytes;
			Assert.AreEqual(arr1, arr2);
		}

		[Test]
		[Category("LongRunning")]
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

			Console.WriteLine("200 with more fields and BodyBytes set, repeats: " + repeats);
			response = Response.FromStatusCode(200);
			response.BodyBytes = Encoding.UTF8.GetBytes(json);
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
				response.SetCustomFieldsFlag();
			}
			stop = DateTime.Now;
			Console.Write((stop - start).TotalMilliseconds + "    ");

			start = DateTime.Now;
			for (int i = 0; i < repeats; i++) {
				response.ConstructFromFields_Slow();
				response.SetCustomFieldsFlag();
			}
			stop = DateTime.Now;
			Console.WriteLine((stop - start).TotalMilliseconds);
		}
	}
}