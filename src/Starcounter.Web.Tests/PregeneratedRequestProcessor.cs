﻿// ***********************************************************************
// <copyright file="PregeneratedRequestProcessor.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

// Generated code. This code matches, parses and invokes Http handlers. The code was generated by the Starcounter http/spdy handler engine.

using Starcounter;
using Starcounter.Internal;
using Starcounter.Internal.Uri;
using System.Text;
using System.Collections.Generic;
using System;
using HttpStructs;

namespace __urimatcher__ {

    /// <summary>
    /// Class GeneratedRequestProcessor
    /// </summary>
    public class GeneratedRequestProcessor : TopLevelRequestProcessor {

        /// <summary>
        /// The sub0 verification offset
        /// </summary>
        public int Sub0VerificationOffset = 0;
        /// <summary>
        /// The sub2 verification offset
        /// </summary>
        public int Sub2VerificationOffset = 13;
        /// <summary>
        /// The sub1 verification offset
        /// </summary>
        public int Sub1VerificationOffset = 28;
        /// <summary>
        /// The sub3 verification offset
        /// </summary>
        public int Sub3VerificationOffset = 41;
        /// <summary>
        /// The sub4 verification offset
        /// </summary>
        public int Sub4VerificationOffset = 54;
        /// <summary>
        /// The sub5 verification offset
        /// </summary>
        public int Sub5VerificationOffset = 69;
        /// <summary>
        /// The sub6 verification offset
        /// </summary>
        public int Sub6VerificationOffset = 83;
        /// <summary>
        /// The verification bytes
        /// </summary>
        public static byte[] VerificationBytes = new byte[] { (byte)'G', (byte)'E', (byte)'T', (byte)' ', (byte)'/', (byte)'p', (byte)'l', (byte)'a', (byte)'y', (byte)'e', (byte)'r', (byte)'s', (byte)'/', (byte)'G', (byte)'E', (byte)'T', (byte)' ', (byte)'/', (byte)'d', (byte)'a', (byte)'s', (byte)'h', (byte)'b', (byte)'o', (byte)'a', (byte)'r', (byte)'d', (byte)'/', (byte)'G', (byte)'E', (byte)'T', (byte)' ', (byte)'/', (byte)'p', (byte)'l', (byte)'a', (byte)'y', (byte)'e', (byte)'r', (byte)'s', (byte)'?', (byte)'P', (byte)'U', (byte)'T', (byte)' ', (byte)'/', (byte)'p', (byte)'l', (byte)'a', (byte)'y', (byte)'e', (byte)'r', (byte)'s', (byte)'/', (byte)'P', (byte)'O', (byte)'S', (byte)'T', (byte)' ', (byte)'/', (byte)'t', (byte)'r', (byte)'a', (byte)'n', (byte)'s', (byte)'f', (byte)'e', (byte)'r', (byte)'?', (byte)'P', (byte)'O', (byte)'S', (byte)'T', (byte)' ', (byte)'/', (byte)'d', (byte)'e', (byte)'p', (byte)'o', (byte)'s', (byte)'i', (byte)'t', (byte)'?', (byte)'D', (byte)'E', (byte)'L', (byte)'E', (byte)'T', (byte)'E', (byte)' ', (byte)'/', (byte)'a', (byte)'l', (byte)'l', (byte)' ' };
        /// <summary>
        /// The pointer verification bytes
        /// </summary>
        public static IntPtr PointerVerificationBytes;

        /// <summary>
        /// The sub0
        /// </summary>
        public static Sub0Processor Sub0 = new Sub0Processor();
        /// <summary>
        /// The sub2
        /// </summary>
        public static Sub2Processor Sub2 = new Sub2Processor();
        /// <summary>
        /// The sub1
        /// </summary>
        public static Sub1Processor Sub1 = new Sub1Processor();
        /// <summary>
        /// The sub3
        /// </summary>
        public static Sub3Processor Sub3 = new Sub3Processor();
        /// <summary>
        /// The sub4
        /// </summary>
        public static Sub4Processor Sub4 = new Sub4Processor();
        /// <summary>
        /// The sub5
        /// </summary>
        public static Sub5Processor Sub5 = new Sub5Processor();
        /// <summary>
        /// The sub6
        /// </summary>
        public static Sub6Processor Sub6 = new Sub6Processor();

        /// <summary>
        /// Initializes a new instance of the <see cref="GeneratedRequestProcessor" /> class.
        /// </summary>
        public GeneratedRequestProcessor() {
            Registrations["GET /players/@i "] = Sub0;
            Registrations["GET /dashboard/@i "] = Sub2;
            Registrations["GET /players?@s "] = Sub1;
            Registrations["PUT /players/@i "] = Sub3;
            Registrations["POST /transfer?@i "] = Sub4;
            Registrations["POST /deposit?@i "] = Sub5;
            Registrations["DELETE /all "] = Sub6;
            PointerVerificationBytes = BitsAndBytes.Alloc(VerificationBytes.Length); // TODO. Free when program exists
            BitsAndBytes.SlowMemCopy(PointerVerificationBytes, VerificationBytes, (uint)VerificationBytes.Length);
        }

        /// <summary>
        /// Processes the specified fragment.
        /// </summary>
        /// <param name="fragment">The fragment.</param>
        /// <param name="size">The size.</param>
        /// <param name="invoke">if set to <c>true</c> [invoke].</param>
        /// <param name="request">The request.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="resource">The resource.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Process(IntPtr fragment, int size, bool invoke, HttpRequest request, out SingleRequestProcessorBase handler, out object resource) {
            unsafe {
                byte* pfrag = (byte*)fragment;
                byte* ptempl = (byte*)PointerVerificationBytes;
                int nextSize = size;
                switch (*pfrag) {
                    case (byte)'G':
                        nextSize -= 4;
                        if (nextSize < 0 || (*(UInt32*)pfrag) != (*(UInt32*)ptempl)) {
                            handler = null;
                            resource = null;
                            return false;
                        }
                        pfrag += 4;
                        ptempl += 4;
                        nextSize--;
                        if (nextSize < 0 || (*pfrag) != (*ptempl)) {
                            handler = null;
                            resource = null;
                            return false;
                        }
                        pfrag++;
                        ptempl++;
                        switch (*pfrag) {
                            case (byte)'p':
                                nextSize -= 4;
                                if (nextSize < 0 || (*(UInt32*)pfrag) != (*(UInt32*)ptempl)) {
                                    handler = null;
                                    resource = null;
                                    return false;
                                }
                                pfrag += 4;
                                ptempl += 4;
                                nextSize -= 2;
                                if (nextSize < 0 || (*(UInt16*)pfrag) != (*(UInt16*)ptempl)) {
                                    handler = null;
                                    resource = null;
                                    return false;
                                }
                                pfrag += 2;
                                ptempl += 2;
                                nextSize--;
                                if (nextSize < 0 || (*pfrag) != (*ptempl)) {
                                    handler = null;
                                    resource = null;
                                    return false;
                                }
                                pfrag++;
                                ptempl++;
                                switch (*pfrag) {
                                    case (byte)'/':
                                        nextSize--;
                                        if (nextSize < 0 || (*pfrag) != (*ptempl)) {
                                            break;
                                        }
                                        pfrag++;
                                        ptempl++;
                                        if (Sub0.Process((IntPtr)pfrag, nextSize, invoke, request, out handler, out resource))
                                            return true;
                                        break;
                                    case (byte)'?':
                                        nextSize--;
                                        if (nextSize < 0 || (*pfrag) != (*ptempl)) {
                                            break;
                                        }
                                        pfrag++;
                                        ptempl++;
                                        if (Sub1.Process((IntPtr)pfrag, nextSize, invoke, request, out handler, out resource))
                                            return true;
                                        break;
                                }
                                break;
                            case (byte)'d':
                                nextSize -= 8;
                                if (nextSize < 0 || (*(UInt64*)pfrag) != (*(UInt64*)ptempl)) {
                                    break;
                                }
                                pfrag += 8;
                                ptempl += 8;
                                nextSize -= 2;
                                if (nextSize < 0 || (*(UInt16*)pfrag) != (*(UInt16*)ptempl)) {
                                    break;
                                }
                                pfrag += 2;
                                ptempl += 2;
                                if (Sub2.Process((IntPtr)pfrag, nextSize, invoke, request, out handler, out resource))
                                    return true;
                                break;
                        }
                        break;
                    case (byte)'P':
                        nextSize--;
                        if (nextSize < 0 || (*pfrag) != (*ptempl)) {
                            handler = null;
                            resource = null;
                            return false;
                        }
                        pfrag++;
                        ptempl++;
                        switch (*pfrag) {
                            case (byte)'U':
                                nextSize -= 8;
                                if (nextSize < 0 || (*(UInt64*)pfrag) != (*(UInt64*)ptempl)) {
                                    break;
                                }
                                pfrag += 8;
                                ptempl += 8;
                                nextSize -= 4;
                                if (nextSize < 0 || (*(UInt32*)pfrag) != (*(UInt32*)ptempl)) {
                                    break;
                                }
                                pfrag += 4;
                                ptempl += 4;
                                if (Sub3.Process((IntPtr)pfrag, nextSize, invoke, request, out handler, out resource))
                                    return true;
                                break;
                            case (byte)'O':
                                nextSize -= 4;
                                if (nextSize < 0 || (*(UInt32*)pfrag) != (*(UInt32*)ptempl)) {
                                    handler = null;
                                    resource = null;
                                    return false;
                                }
                                pfrag += 4;
                                ptempl += 4;
                                nextSize--;
                                if (nextSize < 0 || (*pfrag) != (*ptempl)) {
                                    handler = null;
                                    resource = null;
                                    return false;
                                }
                                pfrag++;
                                ptempl++;
                                switch (*pfrag) {
                                    case (byte)'t':
                                        nextSize -= 8;
                                        if (nextSize < 0 || (*(UInt64*)pfrag) != (*(UInt64*)ptempl)) {
                                            break;
                                        }
                                        pfrag += 8;
                                        ptempl += 8;
                                        nextSize--;
                                        if (nextSize < 0 || (*pfrag) != (*ptempl)) {
                                            break;
                                        }
                                        pfrag++;
                                        ptempl++;
                                        if (Sub4.Process((IntPtr)pfrag, nextSize, invoke, request, out handler, out resource))
                                            return true;
                                        break;
                                    case (byte)'d':
                                        nextSize -= 8;
                                        if (nextSize < 0 || (*(UInt64*)pfrag) != (*(UInt64*)ptempl)) {
                                            break;
                                        }
                                        pfrag += 8;
                                        ptempl += 8;
                                        if (Sub5.Process((IntPtr)pfrag, nextSize, invoke, request, out handler, out resource))
                                            return true;
                                        break;
                                }
                                break;
                        }
                        break;
                    case (byte)'D':
                        if (Sub6.Process((IntPtr)pfrag, nextSize, invoke, request, out handler, out resource))
                            return true;
                        break;
                }
            }
            handler = null;
            resource = null;
            return false;
        }

        /// <summary>
        /// Class Sub0Processor
        /// </summary>
        public class Sub0Processor : SingleRequestProcessor<int> {

            /// <summary>
            /// Processes the specified fragment.
            /// </summary>
            /// <param name="fragment">The fragment.</param>
            /// <param name="size">The size.</param>
            /// <param name="invoke">if set to <c>true</c> [invoke].</param>
            /// <param name="request">The request.</param>
            /// <param name="handler">The handler.</param>
            /// <param name="resource">The resource.</param>
            /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
            public override bool Process(IntPtr fragment, int size, bool invoke, HttpRequest request, out SingleRequestProcessorBase handler, out object resource) {
                unsafe {
                    byte* pfrag = (byte*)fragment;
                    byte* ptempl = (byte*)PointerVerificationBytes;
                    int nextSize = size;
                    int val;
                    if (ParseUriInt(fragment, size, out val)) {
                        handler = this;
                        if (!invoke)
                            resource = null;
                        else
                            resource = Code.Invoke(val);
                        return true;
                    }
                }
                handler = null;
                resource = null;
                return false;
            }
        }

        /// <summary>
        /// Class Sub1Processor
        /// </summary>
        public class Sub1Processor : SingleRequestProcessor<string> {

            /// <summary>
            /// Processes the specified fragment.
            /// </summary>
            /// <param name="fragment">The fragment.</param>
            /// <param name="size">The size.</param>
            /// <param name="invoke">if set to <c>true</c> [invoke].</param>
            /// <param name="request">The request.</param>
            /// <param name="handler">The handler.</param>
            /// <param name="resource">The resource.</param>
            /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
            public override bool Process(IntPtr fragment, int size, bool invoke, HttpRequest request, out SingleRequestProcessorBase handler, out object resource) {
                unsafe {
                    byte* pfrag = (byte*)fragment;
                    byte* ptempl = (byte*)PointerVerificationBytes;
                    int nextSize = size;
                    string val;
                    if (ParseUriString(fragment, size, out val)) {
                        handler = this;
                        if (!invoke)
                            resource = null;
                        else
                            resource = Code.Invoke(val);
                        return true;
                    }
                }
                handler = null;
                resource = null;
                return false;
            }
        }

        /// <summary>
        /// Class Sub2Processor
        /// </summary>
        public class Sub2Processor : SingleRequestProcessor<int> {

            /// <summary>
            /// Processes the specified fragment.
            /// </summary>
            /// <param name="fragment">The fragment.</param>
            /// <param name="size">The size.</param>
            /// <param name="invoke">if set to <c>true</c> [invoke].</param>
            /// <param name="request">The request.</param>
            /// <param name="handler">The handler.</param>
            /// <param name="resource">The resource.</param>
            /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
            public override bool Process(IntPtr fragment, int size, bool invoke, HttpRequest request, out SingleRequestProcessorBase handler, out object resource) {
                unsafe {
                    byte* pfrag = (byte*)fragment;
                    byte* ptempl = (byte*)PointerVerificationBytes;
                    int nextSize = size;
                    int val;
                    if (ParseUriInt(fragment, size, out val)) {
                        handler = this;
                        if (!invoke)
                            resource = null;
                        else
                            resource = Code.Invoke(val);
                        return true;
                    }
                }
                handler = null;
                resource = null;
                return false;
            }
        }

        /// <summary>
        /// Class Sub3Processor
        /// </summary>
        public class Sub3Processor : SingleRequestProcessor<int> {

            /// <summary>
            /// Processes the specified fragment.
            /// </summary>
            /// <param name="fragment">The fragment.</param>
            /// <param name="size">The size.</param>
            /// <param name="invoke">if set to <c>true</c> [invoke].</param>
            /// <param name="request">The request.</param>
            /// <param name="handler">The handler.</param>
            /// <param name="resource">The resource.</param>
            /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
            public override bool Process(IntPtr fragment, int size, bool invoke, HttpRequest request, out SingleRequestProcessorBase handler, out object resource) {
                unsafe {
                    byte* pfrag = (byte*)fragment;
                    byte* ptempl = (byte*)PointerVerificationBytes;
                    int nextSize = size;
                    int val;
                    if (ParseUriInt(fragment, size, out val)) {
                        handler = this;
                        if (!invoke)
                            resource = null;
                        else
                            resource = Code.Invoke(val);
                        return true;
                    }
                }
                handler = null;
                resource = null;
                return false;
            }
        }

        /// <summary>
        /// Class Sub4Processor
        /// </summary>
        public class Sub4Processor : SingleRequestProcessor<int> {

            /// <summary>
            /// Processes the specified fragment.
            /// </summary>
            /// <param name="fragment">The fragment.</param>
            /// <param name="size">The size.</param>
            /// <param name="invoke">if set to <c>true</c> [invoke].</param>
            /// <param name="request">The request.</param>
            /// <param name="handler">The handler.</param>
            /// <param name="resource">The resource.</param>
            /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
            public override bool Process(IntPtr fragment, int size, bool invoke, HttpRequest request, out SingleRequestProcessorBase handler, out object resource) {
                unsafe {
                    byte* pfrag = (byte*)fragment;
                    byte* ptempl = (byte*)PointerVerificationBytes;
                    int nextSize = size;
                    int val;
                    if (ParseUriInt(fragment, size, out val)) {
                        handler = this;
                        if (!invoke)
                            resource = null;
                        else
                            resource = Code.Invoke(val);
                        return true;
                    }
                }
                handler = null;
                resource = null;
                return false;
            }
        }

        /// <summary>
        /// Class Sub5Processor
        /// </summary>
        public class Sub5Processor : SingleRequestProcessor<int> {

            /// <summary>
            /// Processes the specified fragment.
            /// </summary>
            /// <param name="fragment">The fragment.</param>
            /// <param name="size">The size.</param>
            /// <param name="invoke">if set to <c>true</c> [invoke].</param>
            /// <param name="request">The request.</param>
            /// <param name="handler">The handler.</param>
            /// <param name="resource">The resource.</param>
            /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
            public override bool Process(IntPtr fragment, int size, bool invoke, HttpRequest request, out SingleRequestProcessorBase handler, out object resource) {
                unsafe {
                    byte* pfrag = (byte*)fragment;
                    byte* ptempl = (byte*)PointerVerificationBytes;
                    int nextSize = size;
                    int val;
                    if (ParseUriInt(fragment, size, out val)) {
                        handler = this;
                        if (!invoke)
                            resource = null;
                        else
                            resource = Code.Invoke(val);
                        return true;
                    }
                }
                handler = null;
                resource = null;
                return false;
            }
        }

        /// <summary>
        /// Class Sub6Processor
        /// </summary>
        public class Sub6Processor : SingleRequestProcessor {

            /// <summary>
            /// Processes the specified fragment.
            /// </summary>
            /// <param name="fragment">The fragment.</param>
            /// <param name="size">The size.</param>
            /// <param name="invoke">if set to <c>true</c> [invoke].</param>
            /// <param name="request">The request.</param>
            /// <param name="handler">The handler.</param>
            /// <param name="resource">The resource.</param>
            /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
            public override bool Process(IntPtr fragment, int size, bool invoke, HttpRequest request, out SingleRequestProcessorBase handler, out object resource) {
                handler = this;
                if (!invoke)
                    resource = null;
                else
                    resource = Code.Invoke();
                return true;
            }
        }
    }
}


