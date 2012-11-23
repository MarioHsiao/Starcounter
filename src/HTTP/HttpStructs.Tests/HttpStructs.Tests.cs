﻿// ***********************************************************************
// <copyright file="HttpStructs.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using NUnit.Framework;
using Starcounter;
using Starcounter.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HttpStructs.Tests
{
    /// <summary>
    /// Used for HttpStructs tests initialization/shutdown.
    /// </summary>
    [SetUpFixture]
    public class HttpStructsTestsSetup
    {
        /// <summary>
        /// HttpStructs tests initialization.
        /// </summary>
        [SetUp]
        public void InitHttpStructsTests()
        {
            HttpStructs.HttpRequest.sc_init_http_parser();
        }
    }

    /// <summary>
    /// Tests general HTTP requests.
    /// </summary>
    [TestFixture]
    public class GeneralAppsHttpParserTests
    {
        /// <summary>
        /// Tests simple correct HTTP request.
        /// </summary>
        [Test]
        public void TestSimpleCorrectHttpRequest()
        {
            String[] http_request_strings =
            {
                "GET /players/123 HTTP/1.0\r\n\r\n",

                "GET /123\r\n\r\n",

                "DELETE /all\r\n\r\n",

                "GET /pub/WWW/TheProject.html HTTP/1.1\r\n" +
                "Host: www.w3.org\r\n" +
                "\r\n",
                                         
                "GET /get_funky_content_length_body_hello HTTP/1.0\r\n" +
                "conTENT-Length: 5\r\n" +
                "\r\n" +
                "HELLO",

                "GET /vi/Q1Nnm4AZv4c/hqdefault.jpg HTTP/1.1\r\n" +
                "Host: i2.ytimg.com\r\n" +
                "Connection: keep-alive\r\n" +
                "User-Agent: Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.4 (KHTML, like Gecko) Chrome/22.0.1229.94 Safari/537.4\r\n" +
                "Accept: */*\r\n" +
                "Referer: http://www.youtube.com/\r\n" +
                "Accept-Encoding: gzip,deflate,sdch\r\n" +
                "Accept-Language: en-US,en;q=0.8\r\n" +
                "Accept-Charset: ISO-8859-1,utf-8;q=0.7,*;q=0.3\r\n" +
                "\r\n",

                "POST /post_identity_body_world?q=search#hey HTTP/1.1\r\n" +
                "Accept: */*\r\n" +
                "Transfer-Encoding: identity\r\n" +
                "Content-Length: 5\r\n" +
                "\r\n" +
                "World",

                "PATCH /file.txt HTTP/1.1\r\n" +
                "Host: www.example.com\r\n" +
                "Content-Type: application/example\r\n" +
                "If-Match: \"e0023aa4e\"\r\n" +
                "Content-Length: 10\r\n" +
                "\r\n" +
                "cccccccccc",

                "POST / HTTP/1.1\r\n" +
                "Host: www.example.com\r\n" +
                "Content-Type: application/x-www-form-urlencoded\r\n" +
                "Content-Length: 4\r\n" +
                "Connection: close\r\n" +
                "\r\n" +
                "q=42\r\n"
            };

            // Correct HTTP request URIs.
            String[] http_request_uris =
            {
                "/players/123",
                "/123",
                "/all",
                "/pub/WWW/TheProject.html",
                "/get_funky_content_length_body_hello",
                "/vi/Q1Nnm4AZv4c/hqdefault.jpg",
                "/post_identity_body_world?q=search#hey",
                "/file.txt",
                "/"
            };

            // Correct HTTP request hosts.
            String[] http_request_hosts =
            {
                null,
                null,
                null,
                "www.w3.org",
                null,
                "i2.ytimg.com",
                null,
                "www.example.com",
                "www.example.com"
            };

            // Correct HTTP request bodies.
            String[] http_request_bodies =
            {
                null,
                null,
                null,
                null,
                "HELLO",
                null,
                "World",
                "cccccccccc",
                "q=42"
            };

            // Collecting all parsed HTTP requests.
            for (Int32 i = 0; i < http_request_strings.Length; i++)
            {
                Console.WriteLine("Processing correct HTTP request: " + i);

                Byte[] http_request_bytes = Encoding.ASCII.GetBytes(http_request_strings[i]);

                HttpRequest http_request = new HttpRequest(http_request_bytes);

                // Checking correct URIs.
                Assert.That(http_request.Uri == http_request_uris[i], Is.True);

                // Checking correct hosts.
                Assert.That(http_request["Host"] == http_request_hosts[i], Is.True);

                // Checking correct bodies.
                Assert.That(http_request.GetBodyStringUtf8_Slow() == http_request_bodies[i], Is.True);

                // Immediately destroying the structure.
                http_request.Destroy();

                // Checking if destroyed successfully.
                Assert.That(http_request.IsDestroyed(), Is.True);
            }
        }

        /// <summary>
        /// Tests incorrect HTTP request.
        /// </summary>
        [Test]
        public void TestIncorrectHttpRequest()
        {
            String simple_http_request =
                "GET /pub/WWW/TheProject.html HTTP/1.1\r\n" +
                "Ho st: www.w3.org\r\n" +
                "\r\n";

            Byte[] simple_http_request_bytes = Encoding.ASCII.GetBytes(simple_http_request);

            HttpRequest http_request = null;

            // Checking that only correct exceptions are thrown.
            Assert.DoesNotThrow(() =>
            {
                try
                {
                    // Should through only ScErrAppsHttpParserIncompleteHeaders, ScErrAppsHttpParserIncorrect.
                    http_request = new HttpRequest(simple_http_request_bytes);
                }
                catch (Exception e)
                {
                    UInt32 code;
                    if (!ErrorCode.TryGetCode(e, out code))
                        throw;

                    // Checking correct error codes.
                    if ((code != Error.SCERRAPPSHTTPPARSERINCOMPLETEHEADERS) &&
                        (code != Error.SCERRAPPSHTTPPARSERINCORRECT))
                        throw;
                }
            });

            Assert.That(http_request, Is.Null);
        }
    }
}
