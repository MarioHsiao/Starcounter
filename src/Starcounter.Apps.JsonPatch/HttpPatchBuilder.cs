// ***********************************************************************
// <copyright file="HttpPatchBuilder.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Starcounter.Internal;
using Starcounter.Templates;

namespace Starcounter.Internal.JsonPatch
{
    /// <summary>
    /// Class HttpPatchBuilder
    /// </summary>
    internal class HttpPatchBuilder
    {
        /// <summary>
        /// 
        /// </summary>
        private static Byte[] OK200_WITH_JSON_PATCH;

        /// <summary>
        /// 
        /// </summary>
        private static byte[] ERROR415_WITH_CONTENT;

        /// <summary>
        /// 
        /// </summary>
        private static byte[] ERROR400_WITH_CONTENT;

        /// <summary>
        /// 
        /// </summary>
        private static Byte[] HTTP_HEADER_TERMINATOR;

        


        /// <summary>
        /// Initializes static members of the <see cref="HttpPatchBuilder" /> class.
        /// </summary>
        static HttpPatchBuilder()
        {
            String headerStr;

            headerStr = "HTTP/1.1 200 OK" + StarcounterConstants.NetworkConstants.CRLF + "Content-Type: application/json-patch" + StarcounterConstants.NetworkConstants.CRLF + "Content-Length: ";
            OK200_WITH_JSON_PATCH = Encoding.UTF8.GetBytes(headerStr);

            headerStr = "HTTP/1.1 400 Bad Request" + StarcounterConstants.NetworkConstants.CRLF + "Content-Type: text/plain" + StarcounterConstants.NetworkConstants.CRLF + "Content-Length: ";
            ERROR400_WITH_CONTENT = Encoding.UTF8.GetBytes(headerStr);

            headerStr = "HTTP/1.1 415 Unsupported Media Type" + StarcounterConstants.NetworkConstants.CRLF + "Content-Type: text/plain" + StarcounterConstants.NetworkConstants.CRLF + "Content-Length: ";
            ERROR415_WITH_CONTENT = Encoding.UTF8.GetBytes(headerStr);

            headerStr = StarcounterConstants.NetworkConstants.CRLFCRLF;
            HTTP_HEADER_TERMINATOR = Encoding.UTF8.GetBytes(headerStr);
        }

        /// <summary>
        /// Creates an 200 ok response with all patches as content.
        /// </summary>
        /// <param name="changeLog">A log of the current changes</param>
        /// <returns>The httpresponse as a bytearray</returns>
        internal static byte[] CreateHttpPatchResponse(ChangeLog changeLog) {
            List<Byte> content = new List<Byte>(100);
            CreateContentFromChangeLog(changeLog, content);
            return CreateResponse(OK200_WITH_JSON_PATCH, content.ToArray());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        internal static byte[] Create400Response(string content) {
            byte[] contentArr = Encoding.UTF8.GetBytes(content);
            return CreateResponse(ERROR400_WITH_CONTENT, contentArr);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        internal static byte[] Create415Response(string content) {
            byte[] contentArr = Encoding.UTF8.GetBytes(content);
            return CreateResponse(ERROR400_WITH_CONTENT, contentArr);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="responseType"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        private static byte[] CreateResponse(byte[] responseType, byte[] content) {
            byte[] contentLengthBuffer;
            byte[] response;
            int contentLengthLength;
            int totalLength;
            int offset;

            contentLengthBuffer = new Byte[10];
            contentLengthLength = (Int32)Utf8Helper.WriteIntAsUtf8Man(contentLengthBuffer, 0, content.Length);

            totalLength = responseType.Length
                             + contentLengthLength
                             + HTTP_HEADER_TERMINATOR.Length
                             + content.Length;
            
            response = new byte[totalLength];

            Buffer.BlockCopy(responseType, 0, response, 0, responseType.Length);
            offset = responseType.Length;

            Buffer.BlockCopy(contentLengthBuffer, 0, response, offset, contentLengthLength);
            offset += contentLengthLength;

            Buffer.BlockCopy(HTTP_HEADER_TERMINATOR, 0, response, offset, HTTP_HEADER_TERMINATOR.Length);
            offset += HTTP_HEADER_TERMINATOR.Length;

            content.CopyTo(response, offset);
            return response;
        }

        /// <summary>
        /// Creates the content from change log.
        /// </summary>
        /// <param name="changeLog">The change log.</param>
        /// <param name="buffer">The buffer.</param>
        /// <returns>Int32.</returns>
        private static Int32 CreateContentFromChangeLog(ChangeLog changeLog, List<Byte> buffer)
        {
            // TODO: 
            // Change so that we can send in a buffer into the function that created 
            // the patch instead of creating a string and then convert it to a byte array
            // and then copy it to the response buffer...
            Int32 startIndex;
            String patch;
            Template template;
            Object obj;

            if (changeLog.Count == 0)
            {
                return 0;
            }

            startIndex = buffer.Count;
            buffer.Add((byte)'[');
            foreach (Change change in changeLog)
            {
                template = change.Template;

                if (change.ChangeType != Change.REMOVE) {
                    obj = GetValueFromChange(change);
                } else {
                    obj = null;
                }

                patch = JsonPatch.BuildJsonPatch(change.ChangeType, change.Obj, change.Template, obj, change.Index);
                Byte[] patchArr = Encoding.UTF8.GetBytes(patch);
                buffer.AddRange(patchArr);

                buffer.Add((byte)',');
                buffer.Add((byte)'\n');
            }

            // Remove the ',' char.
            buffer.RemoveAt(buffer.Count - 1);
            buffer.RemoveAt(buffer.Count - 1);
            buffer.Add((byte)']');
            
            return buffer.Count - startIndex;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="change"></param>
        /// <returns></returns>
        private static object GetValueFromChange(Change change) {
            object ret = null;
            Template template = change.Template;

            // TODO:
            // Need a faster way than checking type and casting to get the value.
                
            if (template is TString) {
                ret = change.Obj.Get((TString)template);
            } else if (template is TObjArr) {
                Arr appList = (Arr)change.Obj.Get((TObjArr)template);
                ret = appList[change.Index];
            } else if (template is TLong) {
                ret = change.Obj.Get((TLong)template);
            } else if (template is TBool) {
                ret = change.Obj.Get((TBool)template);
            } else if (template is TDouble) {
                ret = change.Obj.Get((TDouble)template);
            } else if (template is TDecimal) {
                ret = change.Obj.Get((TDecimal)template);
            } else if (template is TObj) {
                ret = change.Obj.Get((TObj)template);
            }
            return ret;
        }
    }
}

