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
using System.Collections;
using System.Collections.Generic;

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
				response.ConstructFromFields(null, null);
				ret = response.BufferContainingResponse;
				retSize = response.ResponseSizeBytes;
			}
			sw.Stop();
			Console.WriteLine(Encoding.UTF8.GetString(ret, 0, retSize));
			Console.WriteLine(String.Format("Ran {0} times in {1} ms", repeats, sw.ElapsedMilliseconds));
		}

		[Test]
		public static void TestResponseConstructFromFields() {
			string json = File.ReadAllText("simple.json");

			Response response = Response.FromStatusCode(200);
			Assert.IsTrue(response.StatusCode == 200);

			response = Response.FromStatusCode((int)HttpStatusCode.InternalServerError);
			response.Body = "Some exception message describing the error.";
            Assert.IsTrue(response.Body == "Some exception message describing the error.");

			response = Response.FromStatusCode(200);
			response.Body = json;
			response.ContentType = "application/json";
			response.ContentEncoding = "utf8";
			response.Headers["somespecialheader"] = "myvalue";
			response.StatusDescription = " My special status";

            Assert.IsTrue(response.StatusCode == 200);
            Assert.IsTrue(response.Body == json);
            Assert.IsTrue(response.ContentType == "application/json");
            Assert.IsTrue(response.ContentEncoding == "utf8");
            Assert.IsTrue(response.Headers["somespecialheader"] == "myvalue");
            Assert.IsTrue(response.StatusDescription == " My special status");

			response = Response.FromStatusCode(404);
			response.BodyBytes = Encoding.UTF8.GetBytes(json);
			response.ContentType = "application/json";
			response.ContentEncoding = "utf8";
			response.Headers["somespecialheader"] = "myvalue";
			response.StatusDescription = " My special status";

            Assert.IsTrue(response.StatusCode == 404);
            IStructuralEquatable eqa1 = Encoding.UTF8.GetBytes(json);
            Assert.IsTrue(eqa1.Equals(response.BodyBytes, StructuralComparisons.StructuralEqualityComparer));
            Assert.IsTrue(response.ContentType == "application/json");
            Assert.IsTrue(response.ContentEncoding == "utf8");
            Assert.IsTrue(response.Headers["somespecialheader"] == "myvalue");
            Assert.IsTrue(response.StatusDescription == " My special status");
		}

        [Test]
        public static void TestResponseConstructFromFieldsWithMultiByteChars() {
            string multiByteValue = "";
            for (int i = 0; i < 200; i++) {
                multiByteValue += "Ö";
            }

            // Multibyte value content
            var r = new Response();
            r.Body = multiByteValue;
            r.ConstructFromFields(null, null);

            // Multibyte value as header value
            r = new Response();
            r.Headers["key"] = multiByteValue;
            r.ConstructFromFields(null, null);

            // Multibyte value as header key
            r = new Response();
            r.Headers[multiByteValue] = "value";
            r.ConstructFromFields(null, null);

            // Multibyte value as cookie
            r = new Response();
            var cookies = new List<string>();
            cookies.Add(multiByteValue);
            r.Cookies = cookies;
            r.ConstructFromFields(null, null);

            // Original failing Location-header from https://github.com/Starcounter/Starcounter/issues/3735
            string org = File.ReadAllText("failingheaderstring_3735.txt");
            r = new Response();
            r.Headers["Location"] = org;
            r.ConstructFromFields(null, null);
        }

        [Test]
		public static void TestRequestConstructFromFields() {
			string json = File.ReadAllText("simple.json");

			Request request = new Request();
			request.Uri = "/test";
			request.Host = "127.0.0.1:8080";
			Assert.IsTrue(request.Uri == "/test");
            Assert.IsTrue(request.Host == "127.0.0.1:8080");

			request = new Request();
			request.Method = "PUT";
			request.Uri = "/MyJson";
			request.Host = "192.168.8.1";
			request.ContentType = "application/json";
			request.Body = json;
            Assert.IsTrue(request.Method == "PUT");
            Assert.IsTrue(request.Uri == "/MyJson");
            Assert.IsTrue(request.Host == "192.168.8.1");
            Assert.IsTrue(request.ContentType == "application/json");
            Assert.IsTrue(request.Body == json);

			request = new Request();
			request.Method = "PUT";
			request.Uri = "/MyJson";
			request.Host = "192.168.8.1";
			request.ContentType = "application/json";
			request.ContentEncoding = "utf8";
			request.Cookies.Add("dfsafeHYWERGSfswefw");
			request.BodyBytes = Encoding.UTF8.GetBytes(json);

            Assert.IsTrue(request.Method == "PUT");
            Assert.IsTrue(request.Uri == "/MyJson");
            Assert.IsTrue(request.Host == "192.168.8.1");
            Assert.IsTrue(request.ContentType == "application/json");
            Assert.IsTrue(request.ContentEncoding == "utf8");
            Assert.IsTrue(request.Cookies[0] == "dfsafeHYWERGSfswefw");

            IStructuralEquatable eqa1 = Encoding.UTF8.GetBytes(json);
            Assert.IsTrue(eqa1.Equals(request.BodyBytes, StructuralComparisons.StructuralEqualityComparer));
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
			response.Headers["somespecialheader"] = "myvalue";
			response.StatusDescription = " My special status";
			RunResponseBenchmark(response, repeats);

			Console.WriteLine("200 with more fields and BodyBytes set, repeats: " + repeats);
			response = Response.FromStatusCode(200);
			response.BodyBytes = Encoding.UTF8.GetBytes(json);
			response.ContentType = "application/json";
			response.ContentEncoding = "utf8";
			response.Headers["somespecialheader"] = "myvalue";
			response.StatusDescription = " My special status";
			RunResponseBenchmark(response, repeats);
		}

		private static void RunResponseBenchmark(Response response, int repeats) {
			DateTime start;
			DateTime stop;

			start = DateTime.Now;
			for (int i = 0; i < repeats; i++) {
				response.ConstructFromFields(null, null);
			}
			stop = DateTime.Now;
			Console.Write((stop - start).TotalMilliseconds);
		}
	}
}