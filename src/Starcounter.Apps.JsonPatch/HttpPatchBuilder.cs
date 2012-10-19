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
        /// The O K200_ WIT h_ JSO n_ PATCH
        /// </summary>
        private static Byte[] OK200_WITH_JSON_PATCH;
        /// <summary>
        /// The HTT p_ HEADE r_ TERMINATOR
        /// </summary>
        private static Byte[] HTTP_HEADER_TERMINATOR;


        /// <summary>
        /// Initializes static members of the <see cref="HttpPatchBuilder" /> class.
        /// </summary>
        static HttpPatchBuilder()
        {
            String str;

            str = "HTTP/1.1 200 OK\r\nContent-Type: application/json-patch\r\nContent-Length: ";
            OK200_WITH_JSON_PATCH = Encoding.UTF8.GetBytes(str);

            str = "\r\n\r\n";
            HTTP_HEADER_TERMINATOR = Encoding.UTF8.GetBytes(str);
        }

        /// <summary>
        /// Creates the HTTP patch response.
        /// </summary>
        /// <param name="changeLog">The change log.</param>
        /// <returns>Byte[][].</returns>
        internal static Byte[] CreateHttpPatchResponse(ChangeLog changeLog)
        {
            Int32 responseOffset;
            Int32 contentLength;
            Byte[] contentLengthBuffer = new Byte[10];
            Int32 contentLengthLength;

            List<Byte> content = new List<Byte>(100);
            contentLength = CreateContentFromChangeLog(changeLog, content);
            contentLengthLength = (Int32)Utf8Helper.WriteUIntAsUtf8Man(contentLengthBuffer, 0, (UInt64)contentLength);

            Int32 responseLength = OK200_WITH_JSON_PATCH.Length 
                                   + contentLengthLength 
                                   + HTTP_HEADER_TERMINATOR.Length 
                                   + content.Count;

            Byte[] response = new Byte[responseLength];
            
            Buffer.BlockCopy(OK200_WITH_JSON_PATCH, 0, response, 0, OK200_WITH_JSON_PATCH.Length);
            responseOffset = OK200_WITH_JSON_PATCH.Length;

            Buffer.BlockCopy(contentLengthBuffer, 0, response, responseOffset, contentLengthLength);
            responseOffset += contentLengthLength;

            Buffer.BlockCopy(HTTP_HEADER_TERMINATOR, 0, response, responseOffset, HTTP_HEADER_TERMINATOR.Length);
            responseOffset += HTTP_HEADER_TERMINATOR.Length;

            content.CopyTo(response, responseOffset);

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
            // the patch instead of creating a string and then convert it to a bytearray
            // and then copy it to the responsebuffer...
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
                // TODO:
                // Better way to get the changed value.
                obj = null;
                template = change.Template;

                buffer.Add((byte)'{');
                if (template is StringProperty)
                {
                    obj = change.App.GetValue((StringProperty)template);
                }
                else if (template is ListingProperty)
                {
                    Listing appList = (Listing)change.App.GetValue((ListingProperty)template);

                    // TODO:
                    // Need to convert App to jsonformat.
                    obj = appList[change.Index];
                }

                patch = JsonPatch.BuildJsonPatch(change.ChangeType, change.App, change.Template, obj, change.Index);
                Byte[] patchArr = Encoding.UTF8.GetBytes(patch);
                buffer.AddRange(patchArr);

                buffer.Add((byte)'}');
                buffer.Add((byte)',');
                buffer.Add((byte)'\n');
            }

            // Remove the ',' char.
            buffer.RemoveAt(buffer.Count - 1);
            buffer.RemoveAt(buffer.Count - 1);
            buffer.Add((byte)']');
            
            return buffer.Count - startIndex;
        }
    }
}

