﻿// Generated code. This code serializes and deserializes Typed Json. The code was generated by Starcounter.

using System;
using System.Runtime.InteropServices;
using Starcounter;
using Starcounter.Internal;
using Starcounter.Internal.Application.CodeGeneration;
using Starcounter.Templates;
using Starcounter.XSON.Serializers;

namespace __starcountergenerated__ {
    public class PreGeneratedSerializer : TypedJsonSerializerBase {
#pragma warning disable 0219
#pragma warning disable 0168

#pragma warning disable 0414
        private static int VerificationOffset0 = 0; // Value1
        private static int VerificationOffset1 = 6; // Value2
#pragma warning restore 0414
        private static byte[] VerificationBytes = new byte[] { (byte)'V', (byte)'a', (byte)'l', (byte)'u', (byte)'e', (byte)'1', (byte)'V', (byte)'a', (byte)'l', (byte)'u', (byte)'e', (byte)'2' };
        private static IntPtr PointerVerificationBytes;

        static PreGeneratedSerializer() {
            PointerVerificationBytes = Marshal.AllocHGlobal(VerificationBytes.Length); // TODO. Free when program exists
            Marshal.Copy(VerificationBytes, 0, PointerVerificationBytes, VerificationBytes.Length);
        }
        public override int PopulateFromJson(Obj realObj, IntPtr buffer, int bufferSize) {
            int valueSize;
            dynamic obj = realObj;
            unsafe {
                byte* pBuffer = (byte*)buffer;
                byte* pver = null;
                int leftBufferSize = bufferSize;
                while (leftBufferSize > 0) {
                    // Skip until start of next property or end of current object.
                    while (true) {
                        if (*pBuffer == '"')
                            break;
                        if (*pBuffer == '}') {
                            pBuffer++;
                            leftBufferSize--;
                            return (bufferSize - leftBufferSize);
                        }
                        pBuffer++;
                        leftBufferSize--;
                        if (leftBufferSize < 0)
                            throw new Exception("Deserialization failed.");
                    }
                    pBuffer++;
                    leftBufferSize--;
                    if (leftBufferSize < 0)
                        throw new Exception("Deserialization failed.");
                    pver = ((byte*)PointerVerificationBytes + VerificationOffset1 + 0);
                    leftBufferSize -= 4;
                    if (leftBufferSize < 0 || (*(UInt32*)pBuffer) != (*(UInt32*)pver))
                        throw ErrorCode.ToException(Starcounter.Internal.Error.SCERRUNSPECIFIED);
                    pBuffer += 4;
                    pver += 4;
                    leftBufferSize--;
                    if (leftBufferSize < 0 || (*pBuffer) != (*pver))
                        throw ErrorCode.ToException(Starcounter.Internal.Error.SCERRUNSPECIFIED);
                    pBuffer++;
                    pver++;
                    switch (*pBuffer) {
                        case (byte)'2':
                            pBuffer++;
                            leftBufferSize--;
                            // Skip until start of value to parse.
                            while (*pBuffer != ':') {
                                pBuffer++;
                                leftBufferSize--;
                                if (leftBufferSize < 0)
                                    throw new Exception("Deserialization failed.");
                            }
                            pBuffer++; // Skip ':' or ','
                            leftBufferSize--;
                            if (leftBufferSize < 0)
                                throw new Exception("Deserialization failed.");
                            while (*pBuffer == ' ' || *pBuffer == '\n' || *pBuffer == '\r') {
                                pBuffer++;
                                leftBufferSize--;
                                if (leftBufferSize < 0)
                                    throw new Exception("Deserialization failed.");
                            }
                            String val1;
                            if (JsonHelper.ParseString((IntPtr)pBuffer, leftBufferSize, out val1, out valueSize)) {
                                obj.Value2 = val1;
                                leftBufferSize -= valueSize;
                                if (leftBufferSize < 0) {
                                    throw new Exception("Unable to deserialize App. Unexpected end of content");
                                }
                                pBuffer += valueSize;
                            } else {
                                throw new Exception("Unable to deserialize App. Content not compatible.");
                            }
                            break;
                        case (byte)'1':
                            pBuffer++;
                            leftBufferSize--;
                            // Skip until start of value to parse.
                            while (*pBuffer != ':') {
                                pBuffer++;
                                leftBufferSize--;
                                if (leftBufferSize < 0)
                                    throw new Exception("Deserialization failed.");
                            }
                            pBuffer++; // Skip ':' or ','
                            leftBufferSize--;
                            if (leftBufferSize < 0)
                                throw new Exception("Deserialization failed.");
                            while (*pBuffer == ' ' || *pBuffer == '\n' || *pBuffer == '\r') {
                                pBuffer++;
                                leftBufferSize--;
                                if (leftBufferSize < 0)
                                    throw new Exception("Deserialization failed.");
                            }
                            Int64 val0;
                            if (JsonHelper.ParseInt((IntPtr)pBuffer, leftBufferSize, out val0, out valueSize)) {
                                obj.Value1 = val0;
                                leftBufferSize -= valueSize;
                                if (leftBufferSize < 0) {
                                    throw new Exception("Unable to deserialize App. Unexpected end of content");
                                }
                                pBuffer += valueSize;
                            } else {
                                throw new Exception("Unable to deserialize App. Content not compatible.");
                            }
                            break;
                        default:
                            throw ErrorCode.ToException(Starcounter.Internal.Error.SCERRUNSPECIFIED, "char: '" + (char)*pBuffer + "', offset: " + (bufferSize - leftBufferSize) + "");
                    }
                }
            }
            throw new Exception("Deserialization of App failed.");
        }
#pragma warning restore 0168
#pragma warning restore 0219
    }
}


