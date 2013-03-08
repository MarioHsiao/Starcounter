// ***********************************************************************
// <copyright file="ScUri.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Net;
using Starcounter.Internal;
using Starcounter.Advanced;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Starcounter
{
    public class Node
    {
        /// <summary>
        /// Host name of this node e.g.: www.starcounter.com, 192.168.0.1
        /// </summary>
        String hostName_;

        /// <summary>
        /// HTTP port number, e.g.: 80
        /// </summary>
        UInt16 portNumber_;

        // Delegate to process the results of calling user delegate.
        public delegate HttpResponse HandleResponse(HttpRequest request, Object x);
        public static HandleResponse HandleResponse_ = null;

        /// <summary>
        /// Sets the delegate from using code.
        /// </summary>
        /// <param name="hr"></param>
        public static void SetHandleResponse(HandleResponse hr)
        {
            HandleResponse_ = hr;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="hostName"></param>
        /// <param name="portNumber"></param>
        public Node(String hostName, UInt16 portNumber = 80)
        {
            hostName_ = hostName;
            portNumber_ = portNumber;
        }

        public void GET(String uri, HttpRequest req, Func<HttpResponse, Object> func)
        {
            HttpResponse resp;
            DoRESTRequest(uri, "GET", null, req, out resp);
            if (resp == null) // TODO: Determine what to do in this situation.
                return;

            Object o = func.Invoke(resp);
            HttpResponse respOnResp = HandleResponse_(req, o);
            req.SendResponse(respOnResp.ResponseBytes, 0, respOnResp.ResponseLength);
        }

        public void GET(String uri, HttpRequest httpRequest, out HttpResponse httpResponse)
        {
            DoRESTRequest(uri, "GET", null, httpRequest, out httpResponse);
        }

        public void POST(String uri, HttpRequest req, Func<HttpResponse, Object> func)
        {
            HttpResponse resp;
            DoRESTRequest(uri, "POST", null, req, out resp);
            if (resp == null) // TODO: Determine what to do in this situation.
                return;

            Object o = func.Invoke(resp);
            HttpResponse respOnResp = HandleResponse_(req, o);
            req.SendResponse(respOnResp.ResponseBytes, 0, respOnResp.ResponseLength);
        }

        public void POST(String uri, String content, HttpRequest httpRequest, out HttpResponse httpResponse)
        {
            DoRESTRequest(uri, "POST", content, httpRequest, out httpResponse);
        }

        public void PUT(String uri, HttpRequest req, Func<HttpResponse, Object> func)
        {
            HttpResponse resp;
            DoRESTRequest(uri, "PUT", null, req, out resp);
            if (resp == null) // TODO: Determine what to do in this situation.
                return;

            Object o = func.Invoke(resp);
            HttpResponse respOnResp = HandleResponse_(req, o);
            req.SendResponse(respOnResp.ResponseBytes, 0, respOnResp.ResponseLength);
        }

        public void PUT(String uri, String content, HttpRequest httpRequest, out HttpResponse httpResponse)
        {
            DoRESTRequest(uri, "PUT", content, httpRequest, out httpResponse);
        }

        public void DELETE(String uri, HttpRequest req, Func<HttpResponse, Object> func)
        {
            HttpResponse resp;
            DoRESTRequest(uri, "DELETE", null, req, out resp);
            if (resp == null) // TODO: Determine what to do in this situation.
                return;

            Object o = func.Invoke(resp);
            HttpResponse respOnResp = HandleResponse_(req, o);
            req.SendResponse(respOnResp.ResponseBytes, 0, respOnResp.ResponseLength);
        }

        public void DELETE(String uri, String content, HttpRequest httpRequest, out HttpResponse httpResponse)
        {
            DoRESTRequest(uri, "DELETE", content, httpRequest, out httpResponse);
        }

        /// <summary>
        /// Core function to send REST requests and get the responses.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="method"></param>
        /// <param name="content"></param>
        /// <param name="httpRequest"></param>
        /// <param name="httpResponse"></param>
        void DoRESTRequest(String uri, String method, String content, HttpRequest httpRequest, out HttpResponse httpResponse)
        {
            httpResponse = null;

            // Constructing request headers.
            String headers = method + " " + uri + " HTTP/1.1\r\nHost: " + hostName_ + "\r\n\r\n";

            Byte[] requestBytes;

            // Converting headers to ASCII bytes.
            Byte[] headersBytes = Encoding.ASCII.GetBytes(headers);

            // Adding body if needed.
            if (content != null)
            {
                // Converting body to UTF8 bytes.
                Byte[] contentBytes = Encoding.UTF8.GetBytes(content);

                // Concatenating the arrays.
                requestBytes = new Byte[headersBytes.Length + contentBytes.Length];
                System.Buffer.BlockCopy(headersBytes, 0, requestBytes, 0, headersBytes.Length);
                System.Buffer.BlockCopy(contentBytes, 0, requestBytes, headersBytes.Length, contentBytes.Length);
            }
            else
            {
                requestBytes = headersBytes;
            }

            // Establishing TCP connection.
            TcpClient client = new TcpClient(hostName_, portNumber_);
            MemoryStream ms = new MemoryStream();

            NetworkStream stream = client.GetStream();

            // Sending the request.
            stream.Write(requestBytes, 0, requestBytes.Length);

            // Temporary accumulating buffer.
            Byte[] tempBuf = new Byte[4096];
            Int32 recievedBytes, totallyReceivedBytes = 0, headersLen = 0, contentLen = 0;

            // Looping until we get everything.
            while (true)
            {
                recievedBytes = stream.Read(tempBuf, 0, tempBuf.Length);
                if (recievedBytes <= 0)
                    break;

                if (httpResponse == null)
                {
                    try
                    {
                        // Trying to parse the response.
                        httpResponse = new HttpResponse(tempBuf, recievedBytes, httpRequest);

                        // Getting the headers and content length then.
                        headersLen = (Int32)httpResponse.GetHeadersLength();
                        contentLen = (Int32)httpResponse.ContentLength;
                    }
                    catch (Exception exc)
                    {
                        httpResponse = null;

                        // Checking that we are in a good state.
                        UInt32 errCode;
                        if (ErrorCode.TryGetCode(exc, out errCode))
                        {
                            // Checking if its just not enough data.
                            if (errCode != Error.SCERRAPPSHTTPPARSERINCOMPLETEHEADERS)
                            {
                                // Closing network streams.
                                stream.Close();
                                client.Close();

                                return;
                            }
                        }
                    }
                }

                ms.Write(tempBuf, 0, recievedBytes);
                totallyReceivedBytes += recievedBytes;

                // Checking if we have received everything.
                if ((httpResponse != null) &&
                    (contentLen > 0) &&
                    (totallyReceivedBytes >= (headersLen + contentLen + 4)) &&
                    (stream.DataAvailable == false))
                {
                    break;
                }

                // Checking if any data is available.
                if (!stream.DataAvailable)
                    break;
            }

            // Closing network streams.
            stream.Close();
            client.Close();

            // Setting full response buffer.
            httpResponse.SetResponseBytes(ms, totallyReceivedBytes);

            // Checking if content length was not determined yet.
            if (contentLen <= 0)
                httpResponse.ContentLength = totallyReceivedBytes - 4 - headersLen;
        }

        /*public async void GETHttpClient(String uri, HttpRequest httpRequest)
        {
            HttpClient client = new HttpClient();

            HttpResponseMessage response = await client.GetAsync("http://" + hostName_ + ":" + portNumber_);
            response.EnsureSuccessStatusCode();

            HttpResponseHeaders responseHeaders = response.Headers;
            Byte[] responseContent = await response.Content.ReadAsByteArrayAsync();
        }*/
    }

}